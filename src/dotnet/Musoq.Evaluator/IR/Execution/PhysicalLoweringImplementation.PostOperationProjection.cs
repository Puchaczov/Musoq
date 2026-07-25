using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static string CreateGeneratedFieldName(
        string outputName,
        int outputIndex,
        HashSet<string> usedFieldNames)
    {
        return ExecutionSymbolicNamePolicy.CreateGeneratedFieldName(outputName, outputIndex, usedFieldNames);
    }

    private static string TrimGeneratedIdentifier(string identifier, int reservedSuffixLength)
    {
        return ExecutionSymbolicNamePolicy.TrimIdentifier(identifier, reservedSuffixLength);
    }

    private static string CreateIdentifierCandidate(string outputName, int outputIndex)
    {
        return ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(outputName, outputIndex);
    }

    private LoweringAttempt<PostOperationProjection> CreatePostOperationProjection(
        string resultTableName,
        string resultShapeName,
        ProjectedField[] publicFields,
        IReadOnlyList<PostOperation> postOperations,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return new PostOperationProjectionPlanner(
                CreateGeneratedShape,
                ValidateHiddenSortFieldPruning)
            .Create(
                resultTableName,
                resultShapeName,
                publicFields,
                postOperations,
                sourceLookup);
    }

    private string? ValidateHiddenSortFieldPruning(
        IReadOnlyList<PostOperation> postOperations,
        IReadOnlyList<ProjectedField> hiddenFields)
    {
        return CanPruneHiddenSortFields(postOperations, hiddenFields, out var unsupportedReason)
            ? null
            : unsupportedReason;
    }
}
