using System;

namespace Musoq.Parser.Nodes;

/// <summary>
/// Represents string character access using square bracket notation (e.g., Name[0], f.Name[0])
/// This is distinct from array access and handles SQL string character operations
/// </summary>
public class StringCharacterAccessNode : IdentifierNode
{
    public StringCharacterAccessNode(string columnName, int index, string tableAlias = null, TextSpan span = default)
        : base(columnName)
    {
        ColumnName = columnName;
        Index = index;
        TableAlias = tableAlias;
        Span = span;
        Id = $"{nameof(StringCharacterAccessNode)}{columnName}{tableAlias}{index}";
    }

    public string ColumnName { get; }
    
    public int Index { get; }
    
    public string TableAlias { get; }
    
    public TextSpan Span { get; }

    public override Type ReturnType => typeof(string); // SQL compatible - character returns as string

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var prefix = string.IsNullOrEmpty(TableAlias) ? "" : $"{TableAlias}.";
        return $"{prefix}{ColumnName}[{Index}]";
    }
}