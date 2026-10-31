using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal sealed class ListLayout(LayoutContext context, Func<IReadOnlyList<LayoutElement>, Document> sequence)
{
    internal Document Build(IReadOnlyList<LayoutElement> elements, Document? separator = null)
    {
        var parts = new List<Document>();
        var start = 0;
        for (var index = 0; index <= elements.Count; index++)
        {
            if (index != elements.Count && elements[index].Token.TokenType != TokenType.Comma)
                continue;
            parts.Add(sequence(elements.Skip(start).Take(index - start).ToArray()));
            if (index < elements.Count)
            {
                parts.Add(context.Tokens.Of(elements[index].Token));
                if (index + 1 < elements.Count && context.Comments.IsTrailingLineComment(elements[index].Token, elements[index + 1].Token))
                {
                    parts.Add(Document.Text(" "));
                    parts.Add(context.Tokens.Of(elements[++index].Token));
                    parts.Add(Document.Line);
                }
                else
                    parts.Add(separator ?? Document.Break());
            }
            start = index + 1;
        }
        return Document.Join(parts);
    }

}
