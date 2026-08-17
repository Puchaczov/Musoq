using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Sources;

internal static class JoinSourceLookupBuilder
{
    public static Dictionary<string, RowShape> Clone(IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return new Dictionary<string, RowShape>(sourceLookup, StringComparer.OrdinalIgnoreCase);
    }

    public static Dictionary<string, RowShape> Extend(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        RowShape sourceShape)
    {
        return new Dictionary<string, RowShape>(
            RowShapeLookup.CreateSourceShapeLookup(sourceLookup, sourceShape),
            StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryAdd(
        IDictionary<string, RowShape> sourceLookup,
        RowShape sourceShape)
    {
        var alias = RowShapeLookup.ResolveSourceAlias(sourceShape);
        if (sourceLookup.ContainsKey(alias))
            return false;

        sourceLookup[alias] = sourceShape;
        return true;
    }

    public static void AddShapes(
        List<RowShape> shapes,
        JoinSource source)
    {
        shapes.AddRange(source.Shapes);
    }
}
