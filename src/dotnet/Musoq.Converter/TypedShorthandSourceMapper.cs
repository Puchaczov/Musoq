using System.Collections.Generic;

namespace Musoq.Converter;

internal static class TypedShorthandSourceMapper
{
    private const string SourceName = "entities";
    private static readonly string[] SchemaNames = ["#A", "#B", "#C", "#D"];

    public static MusoqQueryBuilder AddSource<T>(
        MusoqQueryBuilder builder,
        int sourceIndex)
    {
        return builder.Source<T>(GetSchemaName(sourceIndex), SourceName);
    }

    public static MusoqQueryBuilder AddSource<T>(
        MusoqQueryBuilder builder,
        int sourceIndex,
        IEnumerable<IReadOnlyList<T>> chunks)
    {
        return builder.Source(GetSchemaName(sourceIndex), SourceName, chunks);
    }

    private static string GetSchemaName(int sourceIndex)
    {
        if ((uint)sourceIndex >= SchemaNames.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceIndex),
                sourceIndex,
                $"Typed shorthand supports source indexes 0 through {SchemaNames.Length - 1}.");
        }

        return SchemaNames[sourceIndex];
    }
}
