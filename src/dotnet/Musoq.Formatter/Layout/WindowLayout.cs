using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal sealed class WindowLayout(LayoutContext context,
    Func<IReadOnlyList<LayoutElement>, ExpressionMode, Document> sequence,
    Func<IReadOnlyList<LayoutElement>, Document> clauseLayout)
{
    internal Document Build(LayoutElement group)
    {
        var clauses = new List<Document>();
        var start = 0;
        for (var index = 1; index <= group.Children.Count; index++)
        {
            if (index != group.Children.Count && group.Children[index].Token.TokenType is not
                (TokenType.PartitionBy or TokenType.OrderBy or TokenType.Rows or TokenType.Range) &&
                !context.Syntax.IsWindowFrameStart(group.Children[index].Start))
                continue;
            if (clauses.Count > 0)
                clauses.Add(Document.Break());
            var clause = group.Children.Skip(start).Take(index - start).ToArray();
            if (clause[0].Token.TokenType is TokenType.PartitionBy or TokenType.OrderBy)
                clauses.Add(clauseLayout(clause));
            else
                clauses.Add(sequence(clause, ExpressionMode.WindowFrame));
            start = index;
        }
        return Document.Group(Document.Concat(context.Tokens.Of(group.Token),
            Document.Indent(Document.Concat(Document.Break(""), Document.Join(clauses))),
            Document.Break(""), context.Tokens.Of(group.Close!)));
    }

}
