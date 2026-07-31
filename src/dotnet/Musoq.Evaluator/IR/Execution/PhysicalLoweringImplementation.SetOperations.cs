using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private ExecutionPlanBuildResult BuildSetOperation(
        SetOperationPipeline pipeline,
        string identifier,
        LoweringScope scope)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var table = BuildSetOperationTable(
            pipeline,
            "result",
            "ResultRow0",
            cteIndexes,
            cteShapesByName: null,
            schemaFromIndex: DefaultSchemaFromIndex,
            scope: scope);
        if (!table.IsBuilt)
            return ExecutionPlanBuildResult.CreateUnsupported(table.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, table));
    }

    private TableBuildResult BuildSetOperationTable(
        SetOperationPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var table = BuildSetOperationTable(
            pipeline.SetOperation,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);

        if (!table.IsBuilt)
            return table;

        return CompleteTableBuild(
            table.Shapes,
            table.Nodes.ToList(),
            table.Table,
            table.RowShape,
            pipeline.PostOperations);
    }

    private TableBuildResult BuildSetOperationTable(
        PhysicalSetOperationNode setOperation,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var strategy = ExecutionStrategies.GetSetOperationStrategy(setOperation);
        var streamingUnionAll = TryBuildStreamingUnionAllTable(
            strategy,
            setOperation,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);
        if (streamingUnionAll != null)
            return streamingUnionAll;

        var leftArm = UnwrapSingleStatement(setOperation.Left);
        var rightArm = UnwrapSingleStatement(setOperation.Right);
        var armNames = CreateSetOperationArmNames(resultTableName, resultShapeName);

        var left = BuildPlanTable(
            leftArm,
            armNames.LeftTableName,
            armNames.LeftShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scopeAggregateVariables: true,
            scope: scope);
        if (!left.IsBuilt)
            return TableBuildResult.Unsupported(left.UnsupportedReason);

        var rightSchemaFromIndex = CountSchemaScans(leftArm);
        var rightShapeName = CanShareSetOperationCarrier(leftArm, rightArm)
            ? armNames.LeftShapeName
            : armNames.RightShapeName;
        var right = BuildPlanTable(
            rightArm,
            armNames.RightTableName,
            rightShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex + rightSchemaFromIndex,
            scopeAggregateVariables: true,
            scope: scope);
        if (!right.IsBuilt)
            return TableBuildResult.Unsupported(right.UnsupportedReason);

        var result = new ExecutionVariable(resultTableName, typeof(object));
        var nodes = new List<ExecutionNode>(left.Nodes.Count + right.Nodes.Count + 2);

        nodes.AddRange(left.Nodes);
        nodes.AddRange(right.Nodes);
        nodes.Add(new ExecutionSetOperation(
            result,
            left.Table,
            right.Table,
            setOperation.Kind,
            setOperation.FieldIndexes,
            setOperation.FieldTypes,
            ResolveSetOperationStrategy(strategy)));

        var resultShape = left.RowShape with { Contexts = [], SupportsGeneratedFieldAccess = false };
        return TableBuildResult.Success([..left.Shapes, ..right.Shapes], nodes, result, resultShape);
    }

    private static bool CanShareSetOperationCarrier(PhysicalNode left, PhysicalNode right)
    {
        if (!ContainsPhysicalNode<PhysicalWindowNode>(left) ||
            !ContainsPhysicalNode<PhysicalWindowNode>(right) ||
            !ContainsPhysicalNode<PhysicalCteRefNode>(left) ||
            !ContainsPhysicalNode<PhysicalSchemaScanNode>(right))
        {
            return false;
        }

        var leftColumns = left.OutputSchema.Columns;
        var rightColumns = right.OutputSchema.Columns;
        if (leftColumns.Length != rightColumns.Length)
            return false;

        for (var index = 0; index < leftColumns.Length; index++)
        {
            var leftColumn = leftColumns[index];
            var rightColumn = rightColumns[index];
            if (leftColumn.Index != rightColumn.Index ||
                !string.Equals(leftColumn.Name, rightColumn.Name, StringComparison.Ordinal) ||
                leftColumn.Type != rightColumn.Type ||
                !string.Equals(leftColumn.IntendedTypeName, rightColumn.IntendedTypeName, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsPhysicalNode<TNode>(PhysicalNode node)
        where TNode : PhysicalNode
    {
        return node is TNode || node.Children.Any(ContainsPhysicalNode<TNode>);
    }

    private TableBuildResult? TryBuildStreamingUnionAllTable(
        SetOperationStrategyDecision strategy,
        PhysicalSetOperationNode setOperation,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        if (!strategy.CanStreamUnionAll)
            return null;

        var leftPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(setOperation.Left));
        var rightPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(setOperation.Right));
        if (leftPipeline == null || rightPipeline == null)
            return TableBuildResult.Unsupported(
                "Planner selected streaming UnionAll, but Execution IR lowering could not decompose both arms into supported streaming pipelines.");

        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var resultShape = CreateGeneratedShape(resultShapeName, leftPipeline.Project.Fields);
        var left = BuildStreamingUnionAllArm(
            leftPipeline,
            "left",
            resultTable,
            resultShape,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);
        if (!left.IsBuilt)
            return TableBuildResult.Unsupported(
                $"Planner selected streaming UnionAll, but the left arm could not be lowered: {left.UnsupportedReason}");

        var rightSchemaFromIndex = schemaFromIndex + CountSchemaScans(UnwrapSingleStatement(setOperation.Left));
        var right = BuildStreamingUnionAllArm(
            rightPipeline,
            "right",
            resultTable,
            resultShape,
            cteIndexes,
            cteShapesByName,
            rightSchemaFromIndex,
            scope);
        if (!right.IsBuilt)
            return TableBuildResult.Unsupported(
                $"Planner selected streaming UnionAll, but the right arm could not be lowered: {right.UnsupportedReason}");

        var nodes = new List<ExecutionNode>(
            left.Value.Setup.Count + right.Value.Setup.Count + 3)
        {
            CreateTable(resultTable, resultShape)
        };
        nodes.AddRange(left.Value.Setup);
        nodes.Add(left.Value.Loop);
        nodes.AddRange(right.Value.Setup);
        nodes.Add(right.Value.Loop);

        return CompleteTableBuild(
            [left.Value.SourceShape, right.Value.SourceShape, resultShape],
            nodes,
            resultTable,
            resultShape,
            []);
    }

    private LoweringAttempt<StreamingUnionAllArm> BuildStreamingUnionAllArm(
        CteSupportedPipeline pipeline,
        string sourceRowsScope,
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var sourceShape = ResolveSourceShape(pipeline.Source, cteIndexes, cteShapesByName);
        if (sourceShape == null)
            return LoweringAttempt<StreamingUnionAllArm>.Unsupported(
                $"Execution IR lowering cannot resolve source shape for {pipeline.Source.GetType().Name}.");

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var source = CreateSourceVariable(pipeline.Source, sourceShape, cteShapesByName);
        var setup = CreateSourceSetup(
            pipeline.Source,
            sourceShape,
            source,
            schemaFromIndex,
            cteIndexes,
            sourceRowsScope);
        var sourceRows = CreateSourceRowsExpression(pipeline.Source, sourceShape, cteIndexes, cteShapesByName, sourceRowsScope, scope);
        var appendRow = CreateContextFreeAppendRow(
            resultTable,
            resultShape,
            pipeline.Project.Fields,
            sourceLookup);
        var loopBody = CreateLoopBody(pipeline.Filter, appendRow, sourceShape);
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, loopBody);

        return LoweringAttempt<StreamingUnionAllArm>.Built(new StreamingUnionAllArm(
            sourceShape,
            setup,
            loop));
    }

    private static ExecutionAppendRow CreateContextFreeAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var values = fields
            .Select(field => new ExecutionRowValue(field.OutputName, ConvertProjectedExpression(field, sourceLookup)))
            .ToArray();

        return new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            [],
            SerialAppendMode);
    }

    private static ExecutionSetOperationStrategy ResolveSetOperationStrategy(SetOperationStrategyDecision strategy)
    {
        return strategy.TableStrategy switch
        {
            SetOperationTableStrategy.AppendLoop => ExecutionSetOperationStrategy.AppendLoop,
            SetOperationTableStrategy.HashSet => ExecutionSetOperationStrategy.HashSet,
            SetOperationTableStrategy.GeneratedEqualityLoop => ExecutionSetOperationStrategy.GeneratedEqualityLoop,
            _ => throw new NotSupportedException($"Unsupported set-operation table strategy: {strategy.TableStrategy}")
        };
    }
}
