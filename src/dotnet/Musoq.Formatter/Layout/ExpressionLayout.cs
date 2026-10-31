using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Layout;

internal sealed class ExpressionLayout
{
    private readonly LayoutContext _context;
    private readonly Func<IReadOnlyList<LayoutElement>, Document> _query;
    private readonly CaseLayout _cases;
    private readonly WindowLayout _windows;

    internal ExpressionLayout(LayoutContext context, Func<IReadOnlyList<LayoutElement>, Document> query,
        Func<IReadOnlyList<LayoutElement>, Document> clause)
    {
        _context = context;
        _query = query;
        _cases = new CaseLayout(context, elements => Sequence(elements));
        _windows = new WindowLayout(context, Sequence, clause);
        Lists = new ListLayout(context, elements => Sequence(elements));
    }

    internal ListLayout Lists { get; }

    internal Document Sequence(IReadOnlyList<LayoutElement> elements, ExpressionMode mode = ExpressionMode.Expression)
    {
        var parts = new List<Document>();
        LayoutElement? previous = null;
        var previousWasUnary = false;
        var between = false;
        for (var index = 0; index < elements.Count; index++)
        {
            var element = elements[index];
            var type = element.Token.TokenType;
            var logicalBreak = element.IsKeyword && type is TokenType.And or TokenType.Or &&
                               mode != ExpressionMode.WindowFrame && !between && _context.Syntax.Contains(SyntaxKind.Predicate, element.Token.Span.Start);
            if (logicalBreak)
                parts.Add(Document.Line);
            else if (SourceSpacing.NeedsSpace(previous, element, previousWasUnary))
                parts.Add(Document.Text(" "));
            var part = SequencePart(elements, index, previous);
            parts.Add(part.Content);
            index = part.LastIndex;
            if (type == TokenType.Between)
                between = true;
            else if (type == TokenType.And)
                between = false;
            previousWasUnary = SourceSpacing.IsUnary(element, previous);
            previous = element;
        }
        return Document.Join(parts);
    }

    private (Document Content, int LastIndex) SequencePart(IReadOnlyList<LayoutElement> elements, int index, LayoutElement? previous)
    {
        var element = elements[index];
        if (element.Token.Role == SyntaxTokenRole.Comment)
        {
            return (_context.Comments.Build(element, previous), index);
        }
        if (element.IsKeyword && element.Token.TokenType == TokenType.Case)
        {
            var end = _cases.End(elements, index);
            return (_cases.Build(elements.Skip(index).Take(end - index).ToArray()), end - 1);
        }
        return (Element(element), index);
    }

    private Document Element(LayoutElement element)
    {
        if (!element.IsGroup)
            return _context.Tokens.Of(element.Token);
        var opening = _context.Tokens.Of(element.Token);
        var closing = _context.Tokens.Of(element.Close!);
        if (element.Children.Count == 0)
            return Document.Concat(opening, closing);
        if (_context.Syntax.IsQueryGroup(element))
            return Document.Concat(opening, Document.Indent(Document.Concat(Document.Line, _query(element.Children))), Document.Line, closing);
        if (_context.Syntax.IsWindowStart(element.Start))
            return _windows.Build(element);
        var schema = element.Token.TokenType == TokenType.LBracket && _context.Syntax.IsSchemaBody(element.Token.Span.Start);
        if (schema)
            return Document.Concat(opening, Document.Indent(Document.Concat(Document.Line, Lists.Build(element.Children, Document.Line))), Document.Line, closing);
        var padding = element.Token.TokenType == TokenType.LBracket ? " " : "";
        return Document.Group(Document.Concat(opening,
            Document.Indent(Document.Concat(Document.Break(padding), Lists.Build(element.Children))),
            Document.Break(padding), closing));
    }

}

internal enum ExpressionMode { Expression, WindowFrame }
