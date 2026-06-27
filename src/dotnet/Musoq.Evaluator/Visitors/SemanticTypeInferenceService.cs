using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SemanticTypeInferenceService
{
    public static Type FindGreatestCommonSubtype(IEnumerable<Type> candidateTypes)
    {
        var types = candidateTypes
            .Where(type => type != NullNode.NullType.Instance)
            .Select(BuildMetadataAndInferTypesVisitorUtilities.StripNullable)
            .Distinct()
            .ToArray();

        if (types.Length == 0)
            return typeof(object);

        var greatestCommonSubtype = types[0];

        foreach (var currentType in types.Skip(1))
        {
            if (greatestCommonSubtype.IsAssignableTo(currentType))
            {
                greatestCommonSubtype = currentType;
                continue;
            }

            if (currentType.IsAssignableTo(greatestCommonSubtype))
                continue;

            greatestCommonSubtype =
                BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(greatestCommonSubtype, currentType);
        }

        return greatestCommonSubtype;
    }
}
