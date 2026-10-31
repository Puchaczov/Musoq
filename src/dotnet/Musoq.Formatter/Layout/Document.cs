using System.Collections.Generic;
using System.Linq;

namespace Musoq.Formatter.Layout;

internal abstract record Document(int FlatWidth)
{
    internal static Document Text(string value) => new TextDocument(value, TextKind.Generated);
    internal static Document SourceText(string value) => new TextDocument(value, TextKind.Source);
    internal static Document Break(string inline = " ") => new BreakDocument(inline, BreakKind.Optional);
    internal static Document Line { get; } = new BreakDocument("", BreakKind.Line);
    internal static Document Blank { get; } = new BreakDocument("", BreakKind.BlankLine);
    internal static Document Concat(params Document[] parts) => Join(parts);
    internal static Document Join(IEnumerable<Document> parts) => new SequenceDocument(Array.AsReadOnly(parts.ToArray()));
    internal static Document Indent(Document content) => new IndentedDocument(content);
    internal static Document Group(Document content) => new GroupedDocument(content);
}

internal sealed record TextDocument(string Value, TextKind Kind) : Document(Width(Value))
{
    private static int Width(string value) => value.Contains('\n') || value.Contains('\r') ? int.MaxValue : value.Length;
}

internal sealed record BreakDocument(string Inline, BreakKind Kind)
    : Document(Kind == BreakKind.Optional ? Inline.Length : int.MaxValue);
internal sealed record IndentedDocument(Document Content) : Document(Content.FlatWidth);
internal sealed record GroupedDocument(Document Content) : Document(Content.FlatWidth);
internal sealed record SequenceDocument(IReadOnlyList<Document> Parts) : Document(Width(Parts))
{
    private static int Width(IEnumerable<Document> parts)
    {
        long total = 0;
        foreach (var part in parts)
        {
            total += part.FlatWidth;
            if (total >= int.MaxValue)
                return int.MaxValue;
        }
        return (int)total;
    }
}

internal enum TextKind { Generated, Source }
internal enum BreakKind { Optional, Line, BlankLine }
