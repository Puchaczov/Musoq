using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private bool TryBuildTypedOrderTable(
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        RowShape sourceShape,
        int schemaFromIndex,
        out TableBuildResult result)
    {
        result = TableBuildResult.Unsupported(string.Empty);

        if (!CanUseTypedOrderPipeline(pipeline, resultTableName, sourceShape))
            return false;

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var publicShape = CreateGeneratedShape(resultShapeName, pipeline.Project.Fields, sourceLookup);
        var hiddenFields = PostOperationProjectionPlanner.CreateHiddenSortFields(
            publicShape,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            sourceLookup);
        if (!hiddenFields.Supported)
        {
            result = TableBuildResult.Unsupported(hiddenFields.UnsupportedReason);
            return true;
        }

        if (!CanPruneHiddenSortFields(pipeline.PostOperations, hiddenFields.Value, out var pruningUnsupportedReason))
        {
            result = TableBuildResult.Unsupported(pruningUnsupportedReason);
            return true;
        }

        var materializedFields = pipeline.Project.Fields.Concat(hiddenFields.Value).ToArray();
        var workingShape = CreateGeneratedShape(
            PostOperationProjectionPlanner.CreateSortWorkingShapeName(resultShapeName),
            materializedFields,
            sourceLookup);
        var recordShape = CreateOrderRecordShape(workingShape, IsBoundedOrderPostOperation(pipeline.PostOperations[0]));

        if (!CanUseTypedOrderRecordShape(recordShape))
            return false;

        var postOperations = PostOperationProjectionPlanner.ReplaceSortProjectedFields(pipeline.PostOperations, materializedFields);
        if (!TryCreateOrderRecordOperation(postOperations[0], workingShape, out var orderFields, out var selection, out var unsupportedReason))
        {
            if (!string.IsNullOrWhiteSpace(unsupportedReason))
            {
                result = TableBuildResult.Unsupported(unsupportedReason);
                return true;
            }

            return false;
        }

        var sourceRowsScope = CreateSourceRowsScope(resultTableName);
        var source = CreateSourceVariable(pipeline.Source, sourceShape, cteShapesByName);
        var sourceSetup = CreateSourceSetup(pipeline.Source, sourceShape, source, schemaFromIndex, cteIndexes, sourceRowsScope);
        var sourceRows = CreateSourceRowsExpression(pipeline.Source, sourceShape, cteIndexes, cteShapesByName, sourceRowsScope);
        var recordList = new ExecutionVariable(CreateOrderRecordListName(resultTableName), typeof(object));
        var appendValues = materializedFields
            .Select(field => new ExecutionRowValue(field.OutputName, ConvertProjectedExpression(field, sourceLookup)))
            .ToArray();
        var appendRecord = new ExecutionAppendRecord(recordList, recordShape, appendValues);
        var loopBody = CreateLoopBody(pipeline.Filter, appendRecord, sourceShape);
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, loopBody);
        var finalTable = new ExecutionVariable(resultTableName, typeof(object));
        var renumberFieldIndexes = ResolveRenumberFieldIndexes(materializedFields, workingShape);

        var boundedSelection = IsBoundedOrderSelection(selection);
        ExecutionCapacityHint materializeCapacityHint = selection switch
        {
            ExecutionTakeOrderRecordSelection take => ExecutionCapacityHintCandidates.CreateConstantCandidate(finalTable, take.Count),
            ExecutionSkipTakeOrderRecordSelection skipTake => ExecutionCapacityHintCandidates.CreateConstantCandidate(finalTable, skipTake.TakeCount),
            _ => ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(finalTable, recordList)
        };

        var nodes = new List<ExecutionNode>(sourceSetup.Count + 4);
        nodes.AddRange(sourceSetup);
        nodes.Add(boundedSelection
            ? new ExecutionCreateBoundedRecordList(recordList, recordShape, orderFields, selection)
            : new ExecutionCreateRecordList(recordList, recordShape));
        nodes.Add(loop);
        if (!boundedSelection)
            nodes.Add(new ExecutionOrderRecordList(recordList, recordShape, orderFields, selection));
        nodes.Add(new ExecutionMaterializeRecordListToTable(
            recordList,
            finalTable,
            recordShape,
            publicShape,
            Enumerable.Range(0, pipeline.Project.Fields.Length).ToArray(),
            renumberFieldIndexes,
            materializeCapacityHint,
            SerialAppendMode));

        result = TableBuildResult.Success(
            [sourceShape, recordShape, publicShape],
            nodes,
            finalTable,
            publicShape);
        return true;
    }

    private static bool IsBoundedOrderSelection(ExecutionOrderRecordSelection selection) =>
        selection is ExecutionTakeOrderRecordSelection or ExecutionSkipTakeOrderRecordSelection;

    private static bool IsBoundedOrderPostOperation(PostOperation operation) =>
        operation is TopNOperation or TopOffsetOperation;

    private bool CanPruneHiddenSortFields(
        IReadOnlyList<PostOperation> postOperations,
        IReadOnlyList<ProjectedField> hiddenFields,
        out string unsupportedReason)
    {
        unsupportedReason = string.Empty;

        if (hiddenFields.Count == 0 || !ExecutionStrategies.HasRowWidthPruningPlans)
            return true;

        var kind = ResolveOrderedBoundaryKind(postOperations);
        if (kind is null)
        {
            unsupportedReason = "Execution IR row-width pruning requires an ordered boundary strategy before hidden order keys can be pruned.";
            return false;
        }

        if (!ExecutionStrategies.HasRowWidthPruningPlan(kind.Value) ||
            ExecutionStrategies.GetAppliedRowWidthPruning(kind.Value) != null)
            return true;

        unsupportedReason = $"Execution IR row-width pruning requires an applied RowWidthPruningPlan for {kind.Value} before hidden order keys can be pruned.";
        return false;
    }

    private static BoundaryRowShapeKind? ResolveOrderedBoundaryKind(IReadOnlyList<PostOperation> postOperations)
    {
        foreach (var operation in postOperations)
        {
            BoundaryRowShapeKind? kind = operation switch
            {
                SortOperation => BoundaryRowShapeKind.Sort,
                TopNOperation => BoundaryRowShapeKind.TopN,
                TopOffsetOperation => BoundaryRowShapeKind.TopOffset,
                _ => null
            };

            if (kind != null)
                return kind;
        }

        return null;
    }

    private static bool CanUseTypedOrderPipeline(
        SupportedPipeline pipeline,
        string resultTableName,
        RowShape sourceShape)
    {
        if (!string.Equals(resultTableName, "result", StringComparison.Ordinal))
            return false;

        if (pipeline.Project.IsDistinct ||
            pipeline.PostOperations.Count != 1 ||
            pipeline.PostOperations[0] is not (SortOperation or TopNOperation or TopOffsetOperation))
        {
            return false;
        }

        return sourceShape is SourceEntityShape source &&
               source.EntityType != typeof(object) &&
               RowShapeLookup.CanReferenceType(source.EntityType) &&
               !RowShapeLookup.UsesReflectedMemberAccess(source);
    }

    private static bool CanUseTypedOrderRecordShape(GeneratedRecordShape recordShape)
    {
        return recordShape.Fields.All(static field =>
            field.Type != typeof(object) &&
            RowShapeLookup.CanReferenceType(field.Type));
    }

    private static GeneratedRecordShape CreateOrderRecordShape(GeneratedRowShape workingShape, bool emitAsValueType)
    {
        var ordinal = new FieldBinding(
            "__ordinal",
            "__ordinal",
            workingShape.Fields.Count,
            typeof(int),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess("__ordinal"));

        return new GeneratedRecordShape(
            workingShape.TypeName,
            [..workingShape.Fields, ordinal], emitAsValueType);
    }

    private static bool TryCreateOrderRecordOperation(
        PostOperation operation,
        GeneratedRowShape workingShape,
        out IReadOnlyList<ExecutionOrderField> orderFields,
        out ExecutionOrderRecordSelection selection,
        out string unsupportedReason)
    {
        orderFields = [];
        selection = new ExecutionFullOrderRecordSelection();
        unsupportedReason = string.Empty;

        var keys = GetTypedOrderKeys(operation);
        if (keys.Length == 0)
            return false;

        var projectedFields = GetTypedOrderProjectedFields(operation);
        var fields = new List<ExecutionOrderField>(keys.Length);
        foreach (var key in keys)
        {
            var field = RowShapeLookup.ResolveProjectedField(workingShape, key, projectedFields);
            if (field == null)
            {
                unsupportedReason = $"Execution IR typed order lowering cannot resolve order key '{IrExpressionPrinter.Print(key.Expression)}' in projected fields.";
                return false;
            }

            if (!IsSupportedTypedOrderKeyType(field.Type))
                return false;

            fields.Add(new ExecutionOrderField(field.Name, field.OutputIndex, field.Type, key.Descending, key.NullOrdering));
        }

        orderFields = fields;
        selection = operation switch
        {
            SortOperation => new ExecutionFullOrderRecordSelection(),
            TopNOperation topN => new ExecutionTakeOrderRecordSelection(topN.Count),
            TopOffsetOperation topOffset => new ExecutionSkipTakeOrderRecordSelection(topOffset.SkipCount, topOffset.TakeCount),
            _ => throw UnsupportedShape.Of($"Typed ORDER BY operation '{operation.GetType().Name}'")
        };
        return true;
    }

    private static OrderField[] GetTypedOrderKeys(PostOperation operation)
    {
        return operation switch
        {
            SortOperation sort => sort.Keys,
            TopNOperation topN => topN.Keys,
            TopOffsetOperation topOffset => topOffset.Keys,
            _ => []
        };
    }

    private static IReadOnlyList<ProjectedField> GetTypedOrderProjectedFields(PostOperation operation)
    {
        return operation switch
        {
            SortOperation sort => sort.ProjectedFields,
            TopNOperation topN => topN.ProjectedFields,
            TopOffsetOperation topOffset => topOffset.ProjectedFields,
            _ => []
        };
    }

    private static bool IsSupportedTypedOrderKeyType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type.IsPrimitive;
    }

    private static string CreateOrderRecordListName(string resultTableName) =>
        $"{resultTableName}OrderRecords";
}
