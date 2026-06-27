using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private PhysicalToExecutionPlanBuilder.TableBuildResult BuildUnpivotTable(
        PhysicalUnpivotNode unpivot,
        PhysicalToExecutionPlanBuilder.SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex)
    {
        if (ResolveSourceShape(unpivot, cteIndexes, cteShapesByName) is not ValuesRowShape unpivotShape)
        {
            return PhysicalToExecutionPlanBuilder.TableBuildResult.Unsupported(
                $"Execution IR unpivot lowering cannot resolve generated row shape for alias '{unpivot.Alias}'.");
        }

        var sourceRowsScope = PhysicalToExecutionPlanBuilder.CreateSourceRowsScope(resultTableName);
        var inputSource = BuildUnpivotInputSource(
            unpivot.Source,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            sourceRowsScope);
        if (!inputSource.Supported)
            return PhysicalToExecutionPlanBuilder.TableBuildResult.Unsupported(inputSource.UnsupportedReason);

        var unpivotLookup = RowShapeLookup.CreateSourceShapeLookup(unpivotShape);
        var projection = CreatePostOperationProjection(
            resultTableName,
            resultShapeName,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            unpivotLookup);
        if (!projection.Supported)
            return PhysicalToExecutionPlanBuilder.TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection = projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultShape = postOperationProjection.WorkingShape;
        var resultTable = postOperationProjection.WorkingTable;
        var appendRow = CreateAppendRow(resultTable, resultShape, postOperationProjection.MaterializedFields, unpivotShape);
        var expandedRowBody = CreateLoopBody(pipeline.Filter, appendRow, unpivotShape);
        var inputLookup = RowShapeLookup.CreateSourceShapeLookup(inputSource.Source.Shape);
        var loopBody = CreateUnpivotExpansionBlock(unpivot, unpivotShape, inputLookup, expandedRowBody);
        var loop = PhysicalToExecutionPlanBuilder.CreateSourceLoop(inputSource.Source.Shape, inputSource.Source.Rows, inputSource.Source.Variable, loopBody);
        var nodes = new List<ExecutionNode>(inputSource.Source.Setup.Count + unpivot.Entries.Count + 2);

        nodes.AddRange(inputSource.Source.Setup);
        nodes.Add(PhysicalToExecutionPlanBuilder.CreateTable(resultTable, resultShape));
        nodes.Add(loop);

        return PhysicalToExecutionPlanBuilder.CompleteTableBuild(
            [..inputSource.Source.Shapes, unpivotShape, ..postOperationProjection.Shapes],
            nodes,
            resultTable,
            resultShape,
            postOperationProjection.PostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection);
    }

    private PhysicalToExecutionPlanBuilder.SourceBuildResult BuildUnpivotInputSource(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        string? sourceRowsScope)
    {
        if (source is PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode)
            return BuildNestedJoinSource(source, cteIndexes, cteShapesByName, schemaFromIndex);

        var inputShape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (inputShape == null)
        {
            return PhysicalToExecutionPlanBuilder.SourceBuildResult.Unsupported(
                $"Execution IR unpivot lowering cannot resolve source shape for {source.GetType().Name}.");
        }

        var input = PhysicalToExecutionPlanBuilder.CreateSourceVariable(source, inputShape, cteShapesByName);
        var setup = CreateSourceSetup(source, inputShape, input, schemaFromIndex, cteIndexes, sourceRowsScope, cteShapesByName);
        var rows = PhysicalToExecutionPlanBuilder.CreateSourceRowsExpression(source, inputShape, cteIndexes, cteShapesByName, sourceRowsScope);
        var schemaSourceCount = source is PhysicalSchemaScanNode ? 1 : PhysicalToExecutionPlanBuilder.CountSchemaScans(source);

        return PhysicalToExecutionPlanBuilder.SourceBuildResult.Success(new PhysicalToExecutionPlanBuilder.JoinSource(
            source,
            inputShape,
            input,
            setup,
            rows,
            [inputShape],
            schemaSourceCount));
    }

    private static ValuesRowShape CreateUnpivotRowShape(PhysicalUnpivotNode unpivot)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);
        var fields = unpivot.OutputSchema.Columns.Select(column => new FieldBinding(
            column.Name,
            $"{unpivot.Alias}.{column.Name}",
            column.Index,
            column.Type,
            FieldNullability.Unknown,
            new GeneratedFieldAccess(PhysicalToExecutionPlanBuilder.CreateGeneratedFieldName(column.Name, column.Index, usedFieldNames)))).ToArray();

        return new ValuesRowShape(
            unpivot.Alias,
            new GeneratedRowShape(CreateUnpivotRowTypeName(unpivot), fields));
    }

    private static ExecutionBlock CreateUnpivotExpansionBlock(
        PhysicalUnpivotNode unpivot,
        ValuesRowShape unpivotShape,
        IReadOnlyDictionary<string, RowShape> inputLookup,
        ExecutionBlock expandedRowBody)
    {
        var generatedRow = new ExecutionVariable(
            unpivot.Alias,
            typeof(object),
            unpivotShape.GeneratedShape.TypeName);

        return new ExecutionBlock(unpivot.Entries
            .Select(entry => new ExecutionScopedBlock(new ExecutionBlock(
            [
                new ExecutionCreateGeneratedRow(
                    generatedRow,
                    unpivotShape.GeneratedShape,
                    CreateUnpivotRowValues(unpivot, entry, inputLookup),
                    []),
                ..expandedRowBody.Nodes
            ])))
            .ToArray());
    }

    private static IReadOnlyList<ExecutionRowValue> CreateUnpivotRowValues(
        PhysicalUnpivotNode unpivot,
        UnpivotEntry entry,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var values = new List<ExecutionRowValue>(unpivot.KeepFields.Count + 2);

        foreach (var keepField in unpivot.KeepFields)
            values.Add(new ExecutionRowValue(keepField.OutputName, PhysicalToExecutionPlanBuilder.ConvertProjectedExpression(keepField, sourceLookup)));

        values.Add(new ExecutionRowValue(unpivot.NameColumn, new ExecutionLiteral(entry.NameValue, typeof(string))));
        values.Add(new ExecutionRowValue(unpivot.ValueColumn, ExecutionExpressionConverter.Convert(entry.Value, sourceLookup)));

        return values;
    }

    private static string CreateUnpivotRowTypeName(PhysicalUnpivotNode unpivot)
    {
        var hash = ComputeUnpivotShapeHash(unpivot).ToString("X8", CultureInfo.InvariantCulture);
        return PhysicalToExecutionPlanBuilder.TrimGeneratedIdentifier(
            PhysicalToExecutionPlanBuilder.CreateIdentifierCandidate($"{unpivot.Alias}Unpivot{hash}Row0", 0),
            0);
    }

    private static uint ComputeUnpivotShapeHash(PhysicalUnpivotNode unpivot)
    {
        unchecked
        {
            var hash = 2166136261u;
            Add(unpivot.Alias);
            foreach (var column in unpivot.OutputSchema.Columns)
            {
                Add(column.Name);
                Add(column.Type.FullName ?? column.Type.Name);
            }

            foreach (var entry in unpivot.Entries)
                Add(entry.NameValue);

            return hash;

            void Add(string value)
            {
                foreach (var character in value)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
            }
        }
    }
}
