namespace Musoq.Evaluator.Visitors;

internal static class SchemaConcatenationNames
{
    internal static string Create(SchemaRegistry registry, string intrinsicName)
    {
        var name = intrinsicName + "Concat";
        while (registry.TryGetSchema(name, out _))
            name += "_";
        return name;
    }
}
