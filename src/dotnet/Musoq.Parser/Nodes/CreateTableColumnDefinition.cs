using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes;

public sealed record CreateTableColumnDefinition(
    string ColumnName,
    string TypeName,
    IReadOnlyList<CreateTableColumnModifier> ReadModifiers)
{
    public CreateTableColumnDefinition(
        string columnName,
        string typeName,
        IReadOnlyList<CreateTableColumnModifier> readModifiers,
        TextSpan span,
        TextSpan columnNameSpan)
        : this(columnName, typeName, readModifiers)
    {
        Span = span;
        ColumnNameSpan = columnNameSpan;
    }

    public TextSpan Span { get; init; }

    public TextSpan ColumnNameSpan { get; init; }

    public IReadOnlyDictionary<string, TextSpan> ReadModifierSpans { get; init; } =
        CreateReadModifierSpans(ReadModifiers);

    private static IReadOnlyDictionary<string, TextSpan> CreateReadModifierSpans(
        IReadOnlyList<CreateTableColumnModifier> readModifiers)
    {
        var result = new Dictionary<string, TextSpan>(StringComparer.Ordinal);

        foreach (var modifier in readModifiers)
        {
            if (modifier.Span.IsEmpty)
                continue;

            result[modifier.Key] = modifier.Span;
        }

        return result;
    }
}
