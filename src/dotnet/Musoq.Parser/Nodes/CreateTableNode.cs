using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes;

public class CreateTableNode : Node
{
    public CreateTableNode(string name, (string ColumnName, string TypeName)[] tableTypePairs)
        : this(
            name,
            tableTypePairs
                .Select(pair => new CreateTableColumnDefinition(
                    pair.ColumnName,
                    pair.TypeName,
                    []))
                .ToArray())
    {
    }

    public CreateTableNode(string name, IReadOnlyList<CreateTableColumnDefinition> columns)
    {
        Name = name;
        Columns = columns.ToArray();
        TableTypePairs = Columns
            .Select(column => (column.ColumnName, column.TypeName))
            .ToArray();
        Id = $"{nameof(CreateTableNode)}{name}";
    }

    public string Name { get; }

    public IReadOnlyList<CreateTableColumnDefinition> Columns { get; }

    public (string ColumnName, string TypeName)[] TableTypePairs { get; }

    public override Type? ReturnType => null;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var cols = new StringBuilder();

        if (Columns.Count == 0)
            return $"table {Name} {{}};";

        if (Columns.Count == 1)
            return $"table {Name} {{ {FormatColumn(Columns[0])} }};";

        cols.Append(FormatColumn(Columns[0]));

        for (var i = 1; i < Columns.Count - 1; ++i)
        {
            cols.Append(", ");
            cols.Append(FormatColumn(Columns[i]));
        }

        cols.Append(", ");
        cols.Append(FormatColumn(Columns[^1]));

        return $"table {Name} {{ {cols} }};";
    }

    private static string FormatColumn(CreateTableColumnDefinition column)
    {
        if (column.ReadModifiers.Count == 0)
            return $"{column.ColumnName}: {column.TypeName}";

        var builder = new StringBuilder($"{column.ColumnName}: {column.TypeName}");

        foreach (var modifier in column.ReadModifiers)
        {
            builder.Append(' ');
            builder.Append(FormatModifier(modifier));
        }

        return builder.ToString();
    }

    private static string FormatModifier(CreateTableColumnModifier modifier)
    {
        if (modifier.Key.Equals("trim", StringComparison.Ordinal))
            return "trim";

        if (modifier.Key.StartsWith("source.", StringComparison.Ordinal))
            return $"source {modifier.Key["source.".Length..]} '{EscapeLiteral(modifier.Value)}'";

        return $"{modifier.Key} '{EscapeLiteral(modifier.Value)}'";
    }

    private static string EscapeLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
