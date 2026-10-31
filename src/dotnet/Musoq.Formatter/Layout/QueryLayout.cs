using System.Collections.Generic;
using System.Linq;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal sealed class QueryLayout
{
    private readonly LayoutContext _context;
    private readonly ExpressionLayout _expressions;

    internal QueryLayout(LayoutContext context)
    {
        _context = context;
        _expressions = new ExpressionLayout(context, Build, Clause);
    }

    internal Document Sequence(IReadOnlyList<LayoutElement> elements) => _expressions.Sequence(elements);

    internal Document Build(IReadOnlyList<LayoutElement> elements)
    {
        var leading = elements.TakeWhile(element => element.Token.Role == SyntaxTokenRole.Comment).ToArray();
        if (leading.Length > 0 && leading.Length < elements.Count)
            return Document.Concat(Sequence(leading), Document.Line, Build(elements.Skip(leading.Length).ToArray()));
        var parts = new List<Document>();
        var start = 0;
        for (var index = 1; index <= elements.Count; index++)
        {
            if (index != elements.Count && !IsClause(elements[index]))
                continue;
            if (parts.Count > 0)
                parts.Add(Document.Line);
            parts.Add(Clause(elements.Skip(start).Take(index - start).ToArray()));
            start = index;
        }
        return Document.Join(parts);
    }

    private Document Clause(IReadOnlyList<LayoutElement> elements)
    {
        if (elements.Count == 0)
            return Document.Text("");
        var first = elements[0];
        if (!first.IsKeyword)
            return Sequence(elements);
        var body = elements.Skip(1).ToArray();
        var keyword = _context.Tokens.Of(first.Token);
        return ClauseType(first) switch
        {
            TokenType.Select or TokenType.GroupBy or TokenType.OrderBy or TokenType.PartitionBy => Document.Group(Document.Concat(keyword,
                Document.Indent(Document.Concat(Document.Break(), _expressions.Lists.Build(body))))),
            TokenType.Where or TokenType.Having or TokenType.Qualify => Document.Concat(keyword,
                Document.Indent(Document.Concat(Document.Line, Sequence(body)))),
            TokenType.On => Document.Indent(Document.Concat(keyword, Document.Text(" "), Sequence(body))),
            TokenType.Window => Document.Concat(keyword,
                Document.Indent(Document.Concat(Document.Line, _expressions.Lists.Build(body, Document.Line)))),
            TokenType.With => Document.Concat(keyword, Document.Text(" "), _expressions.Lists.Build(body, Document.Blank)),
            _ => Document.Concat(keyword, Document.Text(" "), Sequence(body))
        };
    }

    private TokenType ClauseType(LayoutElement element)
    {
        if (_context.Syntax.IsQualifyStart(element.Start))
            return TokenType.Qualify;
        return element.Token.TokenType;
    }

    private bool IsClause(LayoutElement element) => element.IsKeyword && ClauseType(element) is
        TokenType.Select or TokenType.From or TokenType.Where or TokenType.GroupBy or TokenType.Having or TokenType.OrderBy
        or TokenType.Window or TokenType.Qualify or TokenType.Skip or TokenType.Take or TokenType.On
        or TokenType.InnerJoin or TokenType.OuterJoin or TokenType.CrossJoin or TokenType.AsOfJoin or TokenType.SemiJoin
        or TokenType.AntiJoin or TokenType.CrossApply or TokenType.OuterApply or TokenType.Union or TokenType.UnionAll
        or TokenType.Except or TokenType.Intersect;

}
