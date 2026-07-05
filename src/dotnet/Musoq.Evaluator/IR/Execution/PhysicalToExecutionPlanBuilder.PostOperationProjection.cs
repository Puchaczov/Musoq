using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static string CreateGeneratedFieldName(
        string outputName,
        int outputIndex,
        HashSet<string> usedFieldNames)
    {
        return GeneratedRowNamingPolicy.CreateGeneratedFieldName(outputName, outputIndex, usedFieldNames);
    }

    private static string TrimGeneratedIdentifier(string identifier, int reservedSuffixLength)
    {
        return GeneratedRowNamingPolicy.TrimIdentifier(identifier, reservedSuffixLength);
    }

    private static string CreateIdentifierCandidate(string outputName, int outputIndex)
    {
        return GeneratedRowNamingPolicy.CreateLoweringIdentifierCandidate(outputName, outputIndex);
    }

    private BuildResult<PostOperationProjection> CreatePostOperationProjection(
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
