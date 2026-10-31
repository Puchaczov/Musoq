using System.Collections.Generic;
using System.Linq;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal sealed class ScriptLayout(LayoutContext context)
{
    private readonly QueryLayout _query = new(context);

    internal Document Build(IReadOnlyList<LayoutElement> elements)
    {
        var statements = context.Syntax.Statements();
        if (statements.Count == 0)
            return _query.Sequence(elements);
        var buckets = Enumerable.Range(0, statements.Count).Select(_ => new List<LayoutElement>()).ToArray();
        var current = 0;
        LayoutElement? previous = null;
        foreach (var element in elements)
        {
            while (current + 1 < statements.Count && element.Token.Span.Start >= statements[current + 1].Start)
                current++;
            var destination = current;
            if (element.Token.Role == SyntaxTokenRole.Comment && current + 1 < statements.Count &&
                element.Token.Span.Start >= statements[current].End && previous != null &&
                context.Comments.IsStandalone(previous, element))
                destination++;
            buckets[destination].Add(element);
            previous = element;
        }
        var documents = new List<Document>();
        foreach (var bucket in buckets.Where(b => b.Count > 0))
        {
            if (documents.Count > 0)
                documents.Add(Document.Blank);
            documents.Add(Statement(bucket));
        }
        return Document.Join(documents);
    }

    private Document Statement(IReadOnlyList<LayoutElement> elements)
    {
        var layout = _query;
        var first = elements.FirstOrDefault(e => e.Token.Role != SyntaxTokenRole.Comment);
        if (first == null)
            return layout.Sequence(elements);
        if (first.Token.TokenType is TokenType.Select or TokenType.From or TokenType.With or TokenType.Pivot or TokenType.Unpivot)
            return layout.Build(elements);
        var leading = elements.TakeWhile(e => e.Token.Role == SyntaxTokenRole.Comment).ToArray();
        var body = elements.Skip(leading.Length).ToArray();
        var embedded = context.Syntax.EmbeddedQuery(first.Start, elements[^1].End);
        var split = -1;
        if (embedded != null)
            split = Array.FindIndex(body, e => e.Token.Span.Start == embedded.Span.Start);
        Document content;
        if (split > 0)
            content = Document.Concat(layout.Sequence(body.Take(split).ToArray()), Document.Line,
                Document.Indent(layout.Build(body.Skip(split).ToArray())));
        else
            content = layout.Sequence(body);
        if (leading.Length == 0)
            return content;
        return Document.Concat(layout.Sequence(leading), Document.Line, content);
    }
}
