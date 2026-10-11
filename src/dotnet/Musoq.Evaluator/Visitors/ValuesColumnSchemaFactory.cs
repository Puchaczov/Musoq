using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

internal static class ValuesColumnSchemaFactory
{
    public static ISchemaColumn[] Create(ValuesFromNode node, IReadOnlyList<ValuesRowNode> rows)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
            throw new ArgumentException("Validated VALUES rows cannot be empty.", nameof(rows));

        var names = rows[0].Fields.Select(static field => field.Name).ToArray();
        var columns = new ISchemaColumn[names.Length];
        for (var index = 0; index < names.Length; index++)
        {
            var name = names[index];
            var expressions = rows.Select(row => row.Fields.Single(field =>
                    string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase)).Expression)
                .ToArray();
            var type = CommonColumnTypeResolver.Resolve(name, expressions,
                ValuesSourceSpanHelpers.GetColumnExpressionSpan(expressions, node), CommonColumnTypeDiagnosticKind.Values);
            columns[index] = new SchemaColumn(name, index, type);
        }

        return columns;
    }
}
