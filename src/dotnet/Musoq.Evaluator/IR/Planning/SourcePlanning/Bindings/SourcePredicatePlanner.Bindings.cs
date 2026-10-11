using System.Collections.Generic;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourcePredicatePlanner
{
    private static IReadOnlyDictionary<string, ISchemaColumn[]> ResolveScopedColumns(
        SchemaFromNode source,
        IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns,
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]>? sourceColumns)
    {
        if (sourceColumns == null) return inferredColumns;
        var scoped = new Dictionary<string, ISchemaColumn[]>(inferredColumns, StringComparer.OrdinalIgnoreCase);
        foreach (var binding in sourceColumns)
        {
            if (binding.Key.QueryId == source.QueryId)
                scoped[binding.Key.Alias] = binding.Value;
        }
        return scoped;
    }
}
