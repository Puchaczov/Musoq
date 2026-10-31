using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal sealed class CaseLayout(LayoutContext context, Func<IReadOnlyList<LayoutElement>, Document> sequence)
{
    internal Document Build(IReadOnlyList<LayoutElement> elements)
    {
        var parts = new List<Document>();
        var start = 0;
        for (var index = 1; index < elements.Count; index++)
        {
            if (elements[index].IsKeyword && elements[index].Token.TokenType == TokenType.Case)
            {
                index = End(elements, index) - 1;
                continue;
            }
            if (!elements[index].IsKeyword || elements[index].Token.TokenType is not (TokenType.When or TokenType.Else or TokenType.End))
                continue;
            if (start == 0)
            {
                parts.Add(Header(elements.Take(index).ToArray()));
            }
            else
                parts.Add(Document.Indent(Document.Concat(Document.Line, Branch(elements.Skip(start).Take(index - start).ToArray()))));
            start = index;
        }
        parts.Add(Document.Line);
        parts.Add(context.Tokens.Of(elements[^1].Token));
        return Document.Join(parts);
    }

    private Document Header(IReadOnlyList<LayoutElement> elements)
    {
        var keyword = context.Tokens.Of(elements[0].Token);
        if (elements.Count == 1)
            return keyword;
        return Document.Concat(keyword, Document.Text(" "), sequence(elements.Skip(1).ToArray()));
    }

    private Document Branch(IReadOnlyList<LayoutElement> elements)
    {
        var then = -1;
        for (var index = 1; index < elements.Count; index++)
        {
            if (elements[index].IsKeyword && elements[index].Token.TokenType == TokenType.Case)
            {
                index = End(elements, index) - 1;
                continue;
            }
            if (elements[index].Token.TokenType == TokenType.Then)
            {
                then = index;
                break;
            }
        }
        if (then < 0)
            return Document.Group(Document.Concat(context.Tokens.Of(elements[0].Token),
                Document.Indent(Document.Concat(Document.Break(), sequence(elements.Skip(1).ToArray())))));
        return Document.Group(Document.Concat(context.Tokens.Of(elements[0].Token),
            Document.Indent(Document.Concat(Document.Break(), sequence(elements.Skip(1).Take(then - 1).ToArray()))),
            Document.Break(), context.Tokens.Of(elements[then].Token),
            Document.Indent(Document.Concat(Document.Break(), sequence(elements.Skip(then + 1).ToArray())))));
    }

    internal int End(IReadOnlyList<LayoutElement> elements, int start)
    {
        var span = context.Syntax.CaseSpan(elements[start].Start);
        var end = start + 1;
        while (end < elements.Count && elements[end].Token.Span.Start < span.End)
            end++;
        return end;
    }

}
