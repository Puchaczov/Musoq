using Musoq.Parser.Nodes;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

internal sealed record LiteralOrigin(
    Node Node,
    string Value,
    TextSpan Span,
    string Source,
    int ContentStart,
    int ContentLength,
    bool IsRaw)
{
    public ReadOnlySpan<char> Content => Source.AsSpan(ContentStart - Span.Start, ContentLength);

    public TextSpan ContentSpan(int relativeStart, int length) =>
        new(ContentStart + relativeStart, length);
}
