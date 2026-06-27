using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static PostOperationResult CreateSortOperation(
        SortOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var keys = CreatePostOperationOrderFields("sort", operation.Keys, operation.ProjectedFields, rowShape);
        if (!keys.Supported)
            return PostOperationResult.Unsupported(keys.UnsupportedReason);

        var target = new ExecutionVariable($"{sourceTable.Name}Sorted", typeof(object));
        var renumberFieldIndexes = ResolveRenumberFieldIndexes(operation.ProjectedFields, rowShape);
        return PostOperationResult.Success(
            new ExecutionSortTable(
                sourceTable,
                target,
                keys.Value,
                renumberFieldIndexes,
                ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(target, sourceTable),
                SerialAppendMode,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateTopNOperation(
        TopNOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var keys = CreatePostOperationOrderFields("top-n", operation.Keys, operation.ProjectedFields, rowShape);
        if (!keys.Supported)
            return PostOperationResult.Unsupported(keys.UnsupportedReason);

        var target = new ExecutionVariable($"{sourceTable.Name}TopN", typeof(object));
        var renumberFieldIndexes = ResolveRenumberFieldIndexes(operation.ProjectedFields, rowShape);
        return PostOperationResult.Success(
            new ExecutionTopNTable(
                sourceTable,
                target,
                keys.Value,
                operation.Count,
                renumberFieldIndexes,
                ExecutionCapacityHintCandidates.CreateTakeCandidate(target, sourceTable, operation.Count),
                SerialAppendMode,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateTopOffsetOperation(
        TopOffsetOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var keys = CreatePostOperationOrderFields("top-offset", operation.Keys, operation.ProjectedFields, rowShape);
        if (!keys.Supported)
            return PostOperationResult.Unsupported(keys.UnsupportedReason);

        var target = new ExecutionVariable($"{sourceTable.Name}TopOffset", typeof(object));
        var renumberFieldIndexes = ResolveRenumberFieldIndexes(operation.ProjectedFields, rowShape);
        var strategy = CanUseBoundedTopOffset(keys.Value)
            ? ExecutionTopOffsetStrategy.BoundedHeap
            : ExecutionTopOffsetStrategy.OrderedSlice;
        return PostOperationResult.Success(
            new ExecutionTopOffsetTable(
                sourceTable,
                target,
                keys.Value,
                operation.SkipCount,
                operation.TakeCount,
                renumberFieldIndexes,
                strategy,
                ExecutionCapacityHintCandidates.CreateSkipTakeCandidate(
                    target,
                    sourceTable,
                    operation.SkipCount,
                    operation.TakeCount),
                SerialAppendMode,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static BuildResult<IReadOnlyList<ExecutionOrderField>> CreatePostOperationOrderFields(
        string operationName,
        IReadOnlyList<OrderField> orderKeys,
        IReadOnlyList<ProjectedField> projectedFields,
        GeneratedRowShape rowShape)
    {
        var keys = new List<ExecutionOrderField>(orderKeys.Count);

        foreach (var key in orderKeys)
        {
            var field = RowShapeLookup.ResolveProjectedField(rowShape, key, projectedFields);
            if (field == null)
            {
                return BuildResult<IReadOnlyList<ExecutionOrderField>>.Unsupported(
                    $"Execution IR {operationName} lowering cannot resolve order key '{IrExpressionPrinter.Print(key.Expression)}' in projected fields.");
            }

            keys.Add(new ExecutionOrderField(field.Name, field.OutputIndex, field.Type, key.Descending, key.NullOrdering));
        }

        return BuildResult<IReadOnlyList<ExecutionOrderField>>.Success(keys);
    }

    private static ExecutionColumnMetadata CreateTablePostOperationMetadata(
        ExecutionVariable target,
        GeneratedRowShape rowShape)
    {
        return CreateColumnMetadata(target.Name, rowShape.Fields, ExecutionColumnMetadataKind.TableColumns);
    }

    private static bool CanUseBoundedTopOffset(IReadOnlyList<ExecutionOrderField> keys)
    {
        return keys.Count > 0 && keys.All(static key => IsSupportedTopOffsetKeyType(key.Type));
    }

    private static bool IsSupportedTopOffsetKeyType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type.IsPrimitive;
    }

    private static IReadOnlyList<int> ResolveRenumberFieldIndexes(
        IReadOnlyList<ProjectedField> projectedFields,
        GeneratedRowShape rowShape)
    {
        List<int>? indexes = null;

        foreach (var field in projectedFields)
        {
            if (!IsRowNumberProjection(field.Expression))
                continue;

            var rowField = RowShapeLookup.ResolveProjectedField(rowShape, field.OutputName);
            if (rowField == null)
                continue;

            indexes ??= [];
            indexes.Add(rowField.OutputIndex);
        }

        return indexes ?? (IReadOnlyList<int>)Array.Empty<int>();
    }

    private static bool IsRowNumberProjection(IrExpression expression)
    {
        return expression is MethodCall methodCall && IsRowNumberMethod(methodCall.Method);
    }

    private static bool IsRowNumberMethod(MethodInfo method)
    {
        if (!string.Equals(method.Name, "RowNumber", StringComparison.Ordinal))
            return false;

        var declaringType = method.DeclaringType;
        return declaringType is not null && typeof(LibraryBase).IsAssignableFrom(declaringType);
    }
}
