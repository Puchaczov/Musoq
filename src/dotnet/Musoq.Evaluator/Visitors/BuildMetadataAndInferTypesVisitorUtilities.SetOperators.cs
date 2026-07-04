using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    private static readonly SemanticSetOperatorFactService SetOperatorFactService = new();

    public static int[] CreateSetOperatorPositionIndexes(QueryNode node, string[] keys)
    {
        return SetOperatorFactService.CreatePositionIndexes(node, keys);
    }

    public static Type[] CreateSetOperatorPositionTypes(QueryNode node, string[] keys)
    {
        return SetOperatorFactService.CreatePositionTypes(node, keys);
    }

    public static bool TryGetSetOperatorFieldPosition(QueryNode node, string key, out int position)
    {
        return SetOperatorFactService.TryGetFieldPosition(node, key, out position);
    }
}
