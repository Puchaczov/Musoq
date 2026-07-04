using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ExecutionPlanBuildResult BuildSetOperation(
        SetOperationPipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var table = BuildSetOperationTable(pipeline, "result", "ResultRow0", cteIndexes, session: session);
        if (!table.Supported)
            return ExecutionPlanBuildResult.CreateUnsupported(table.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, table));
    }

    private TableBuildResult BuildSetOperationTable(
        SetOperationPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null,
        int schemaFromIndex = DefaultSchemaFromIndex,
        PhysicalToExecutionLoweringSession? session = null)
    {
        session ??= new PhysicalToExecutionLoweringSession(ResolveExecutionStrategies());
        var table = BuildSetOperationTable(
            pipeline.SetOperation,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            session);

        if (!table.Supported)
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
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null,
        int schemaFromIndex = DefaultSchemaFromIndex,
        PhysicalToExecutionLoweringSession? session = null)
    {
        session ??= new PhysicalToExecutionLoweringSession(ResolveExecutionStrategies());
        var strategy = ExecutionStrategies.GetSetOperationStrategy(setOperation);
        var streamingUnionAll = TryBuildStreamingUnionAllTable(
            strategy,
            setOperation,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            session);
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
            session: session);
        if (!left.Supported)
            return TableBuildResult.Unsupported(left.UnsupportedReason);

        var rightSchemaFromIndex = CountSchemaScans(leftArm);
        var right = BuildPlanTable(
            rightArm,
            armNames.RightTableName,
            armNames.RightShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex + rightSchemaFromIndex,
            scopeAggregateVariables: true,
            session: session);
        if (!right.Supported)
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

    private TableBuildResult? TryBuildStreamingUnionAllTable(
        SetOperationStrategyDecision strategy,
        PhysicalSetOperationNode setOperation,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session)
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
            session);
        if (!left.Supported)
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
            session);
        if (!right.Supported)
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

    private BuildResult<StreamingUnionAllArm> BuildStreamingUnionAllArm(
        SupportedPipeline pipeline,
        string sourceRowsScope,
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session)
    {
        var sourceShape = ResolveSourceShape(pipeline.Source, cteIndexes, cteShapesByName);
        if (sourceShape == null)
            return BuildResult<StreamingUnionAllArm>.Unsupported(
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
        var sourceRows = CreateSourceRowsExpression(pipeline.Source, sourceShape, cteIndexes, cteShapesByName, sourceRowsScope);
        var appendRow = CreateContextFreeAppendRow(
            resultTable,
            resultShape,
            pipeline.Project.Fields,
            sourceLookup);
        var loopBody = CreateLoopBody(pipeline.Filter, appendRow, sourceShape);
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, loopBody);

        return BuildResult<StreamingUnionAllArm>.Success(new StreamingUnionAllArm(
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
