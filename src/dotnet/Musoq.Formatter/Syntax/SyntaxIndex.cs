using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Syntax;

namespace Musoq.Formatter.Syntax;

internal sealed class SyntaxIndex(SyntaxParseResult syntax)
{
    private IReadOnlyList<SyntaxNode> Regions { get; } = Flatten(syntax.Root!);

    internal bool Contains(SyntaxKind kind, int position) =>
        Regions.Any(n => n.Kind == kind && n.Span.Start <= position && position < n.Span.End);

    internal IReadOnlyList<TextSpan> Statements()
    {
        var spans = Regions.Where(n => n.Kind == SyntaxKind.Statement).Select(n => n.Span).Distinct().OrderBy(s => s.Start).ToArray();
        return spans.Where(span => !spans.Any(parent => parent.Start <= span.Start && parent.End >= span.End && !parent.Equals(span))).ToArray();
    }

    internal bool IsSchemaBody(int position)
    {
        var region = Regions.Where(n => n.Span.Start <= position && position < n.Span.End)
            .Where(n => n.Kind is SyntaxKind.BinarySchema or SyntaxKind.TextSchema or SyntaxKind.TableDeclaration
                or SyntaxKind.EnumDeclaration or SyntaxKind.ArrayLiteral or SyntaxKind.RecordLiteral or SyntaxKind.TypeSyntax)
            .OrderBy(n => n.Span.Length).FirstOrDefault();
        return region?.Kind is SyntaxKind.BinarySchema or SyntaxKind.TextSchema or SyntaxKind.TableDeclaration
            or SyntaxKind.EnumDeclaration or SyntaxKind.TypeSyntax;
    }

    internal bool IsQueryGroup(LayoutElement group)
    {
        var first = group.Children.FirstOrDefault(element => element.Token.Role != SyntaxTokenRole.Comment);
        return first != null && Regions.Any(region =>
            region.Kind is SyntaxKind.Query or SyntaxKind.SetExpression or SyntaxKind.Cte &&
            region.Span.Start == first.Token.Span.Start && region.Span.End <= group.Close!.Span.Start);
    }

    internal SyntaxNode? EmbeddedQuery(int start, int end) => Regions
        .Where(region => region.Kind == SyntaxKind.Subquery && region.Span.Start > start && region.Span.End <= end)
        .OrderBy(region => region.Span.Start).FirstOrDefault();

    internal bool IsWindowStart(int start) => Starts(SyntaxKind.WindowSpecification, start);
    internal bool IsWindowFrameStart(int start) => Starts(SyntaxKind.WindowFrame, start);
    internal bool IsQualifyStart(int start) => Starts(SyntaxKind.QualifyClause, start);
    internal TextSpan CaseSpan(int start) => Regions.Where(region => region.Kind == SyntaxKind.CaseExpression && region.Span.Start == start)
        .OrderByDescending(region => region.Span.Length).First().Span;
    private bool Starts(SyntaxKind kind, int start) => Regions.Any(region => region.Kind == kind && region.Span.Start == start);

    private static IReadOnlyList<SyntaxNode> Flatten(SyntaxNode root)
    {
        var nodes = new List<SyntaxNode>();
        var pending = new Stack<SyntaxNode>();
        pending.Push(root);
        while (pending.TryPop(out var node))
        {
            nodes.Add(node);
            for (var index = node.Children.Count - 1; index >= 0; index--)
                pending.Push(node.Children[index]);
        }
        return nodes.AsReadOnly();
    }
}
