using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Plugins;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class PostOperationPlanner
{
    public static PostOperationPlanner Default { get; } = new();

    private readonly Func<string, int, string> _createIdentifierCandidate;

    public PostOperationPlanner(Func<string, int, string>? createIdentifierCandidate = null)
    {
        _createIdentifierCandidate = createIdentifierCandidate ??
                                     GeneratedRowNamingPolicy.CreateLoweringIdentifierCandidate;
    }

    public PostOperationResult CreatePostOperation(
        PostOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        return operation switch
        {
            SortOperation sort => CreateSortOperation(sort, sourceTable, rowShape),
            TopNOperation topN => CreateTopNOperation(topN, sourceTable, rowShape),
            TopOffsetOperation topOffset => CreateTopOffsetOperation(topOffset, sourceTable, rowShape),
            SkipOperation skip => CreateSkipOperation(skip, sourceTable, rowShape),
            TakeOperation take => CreateTakeOperation(take, sourceTable, rowShape),
            SliceOperation slice => CreateSliceOperation(slice, sourceTable, rowShape),
            _ => PostOperationResult.Unsupported($"Execution IR post operation '{operation.GetType().Name}' is not supported.")
        };
    }

    public IReadOnlyList<PostOperation> CreatePostOperations(
        List<PostOperation> operations,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        operations.Reverse();

        var projectedOperations = operations.Any(operation => operation is SortOperation or TopNOperation or TopOffsetOperation)
            ? operations
                .Select(operation => operation switch
                {
                    SortOperation sort => sort with { ProjectedFields = projectedFields },
                    TopNOperation topN => topN with { ProjectedFields = projectedFields },
                    TopOffsetOperation topOffset => topOffset with { ProjectedFields = projectedFields },
                    _ => operation
                })
                .ToArray()
            : (IReadOnlyList<PostOperation>)operations;

        return CombineAdjacentSkipTakeOperations(projectedOperations);
    }

    public IReadOnlyList<PostOperation> CombineAdjacentSkipTakeOperations(
        IReadOnlyList<PostOperation> operations)
    {
        List<PostOperation>? combined = null;

        for (var index = 0; index < operations.Count; index++)
        {
            if (index + 1 < operations.Count &&
                operations[index] is SkipOperation skip &&
                operations[index + 1] is TakeOperation take)
            {
                combined ??= operations.Take(index).ToList();
                combined.Add(new SliceOperation(skip.Count, take.Count));
                index++;
                continue;
            }

            combined?.Add(operations[index]);
        }

        return combined ?? operations;
    }

    public StreamingSlice? TryCreateStreamingSlice(
        string resultTableName,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct,
        TableProjection? finalProjection,
        IReadOnlyList<ProjectedField> projectedFields,
        out IReadOnlyList<PostOperation> remainingPostOperations)
    {
        remainingPostOperations = postOperations;

        if (isDistinct ||
            finalProjection != null ||
            postOperations.Count != 1 ||
            projectedFields.Any(static field => IsRowNumberProjection(field.Expression)))
        {
            return null;
        }

        var skipCount = 0;
        int? takeCount = null;

        switch (postOperations[0])
        {
            case SkipOperation skip:
                skipCount = skip.Count;
                break;
            case TakeOperation take:
                takeCount = take.Count;
                break;
            case SliceOperation slice:
                skipCount = slice.SkipCount;
                takeCount = slice.TakeCount;
                break;
            default:
                return null;
        }

        if (skipCount <= 0 && takeCount == null)
        {
            remainingPostOperations = [];
            return null;
        }

        var skipRemaining = skipCount > 0
            ? new ExecutionVariable(_createIdentifierCandidate($"__{resultTableName}SkipRemaining", 0), typeof(int))
            : null;
        var takeRemaining = takeCount != null
            ? new ExecutionVariable(_createIdentifierCandidate($"__{resultTableName}TakeRemaining", 0), typeof(int))
            : null;

        remainingPostOperations = [];
        return new StreamingSlice(skipCount, takeCount, skipRemaining, takeRemaining);
    }

    public ExecutionCapacityHint? CreateStreamingSliceCapacityCandidate(
        ExecutionVariable target,
        StreamingSlice? streamingSlice)
    {
        return streamingSlice?.TakeCount is { } takeCount
            ? ExecutionCapacityHintCandidates.CreateConstantCandidate(target, takeCount)
            : null;
    }

    public IEnumerable<ExecutionNode> CreateStreamingSliceCounterDeclarations(StreamingSlice? streamingSlice)
    {
        if (streamingSlice == null)
            yield break;

        if (streamingSlice.SkipRemaining != null)
        {
            yield return new ExecutionLet(
                streamingSlice.SkipRemaining,
                new ExecutionLiteral(streamingSlice.SkipCount, typeof(int)));
        }

        if (streamingSlice is { TakeRemaining: not null, TakeCount: { } takeCount })
        {
            yield return new ExecutionLet(
                streamingSlice.TakeRemaining,
                new ExecutionLiteral(takeCount, typeof(int)));
        }
    }

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
                ExecutionAppendMode.Direct,
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
                ExecutionAppendMode.Direct,
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
                ExecutionAppendMode.Direct,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateSkipOperation(
        SkipOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var target = new ExecutionVariable($"{sourceTable.Name}Skipped", typeof(object));
        return PostOperationResult.Success(
            new ExecutionSkipTable(
                sourceTable,
                target,
                operation.Count,
                ExecutionCapacityHintCandidates.CreateSkipCandidate(target, sourceTable, operation.Count),
                ExecutionAppendMode.Direct,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateTakeOperation(
        TakeOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var target = new ExecutionVariable($"{sourceTable.Name}Taken", typeof(object));
        return PostOperationResult.Success(
            new ExecutionTakeTable(
                sourceTable,
                target,
                operation.Count,
                ExecutionCapacityHintCandidates.CreateTakeCandidate(target, sourceTable, operation.Count),
                ExecutionAppendMode.Direct,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateSliceOperation(
        SliceOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var target = new ExecutionVariable($"{sourceTable.Name}Sliced", typeof(object));
        return PostOperationResult.Success(
            new ExecutionSliceTable(
                sourceTable,
                target,
                operation.SkipCount,
                operation.TakeCount,
                ExecutionCapacityHintCandidates.CreateSkipTakeCandidate(
                    target,
                    sourceTable,
                    operation.SkipCount,
                    operation.TakeCount),
                ExecutionAppendMode.Direct,
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
        return new ExecutionColumnMetadata(
            target.Name,
            rowShape.Fields
                .Select(static field => ExecutionColumnMetadataFields.FromFieldBinding(field))
                .ToArray(),
            ExecutionColumnMetadataKind.TableColumns);
    }

    private static bool CanUseBoundedTopOffset(IReadOnlyList<ExecutionOrderField> keys)
    {
        return keys.Count > 0 && keys.All(static key => IsSupportedTopOffsetKeyType(key.Type.ClrType));
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

    public static IReadOnlyList<int> ResolveRenumberFieldIndexes(
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

    public static bool IsRowNumberMethod(MethodInfo method)
    {
        if (!string.Equals(method.Name, "RowNumber", StringComparison.Ordinal))
            return false;

        var declaringType = method.DeclaringType;
        return declaringType is not null && typeof(LibraryBase).IsAssignableFrom(declaringType);
    }
}
