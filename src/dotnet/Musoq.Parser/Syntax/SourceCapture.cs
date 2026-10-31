using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Syntax;

internal sealed class SourceCapture(SourceText source)
{
    private readonly Dictionary<TextSpan, Token> _observations = [];
    private readonly Dictionary<TextSpan, SyntaxTokenRole> _roles = [];
    private readonly List<SyntaxNode> _roots = [];
    private readonly Stack<Region> _regions = [];
    private int _consumedEnd;
    private readonly Dictionary<int, SyntaxParseResult> _embedded = [];

    internal void Include(SyntaxParseResult syntax, int offset)
    {
        _embedded.Add(offset, syntax);
        foreach (var token in syntax.Tokens)
            _roles[new TextSpan(token.Span.Start + offset, token.Span.Length)] = token.Role;
    }


    internal Region Begin(SyntaxKind kind, Token token)
    {
        var region = new Region(this, kind, token.Span.Start);
        _regions.Push(region);
        if (kind is SyntaxKind.ParameterBlock or SyntaxKind.VariableDeclaration or SyntaxKind.EnumDeclaration
            or SyntaxKind.UtilityStatement or SyntaxKind.ValuesSource or SyntaxKind.ArrayLiteral)
            Mark(token, SyntaxTokenRole.Keyword);
        return region;
    }

    internal void Observe(Token token)
    {
        if (token.Span.Length > 0 && token.Span.Start >= 0 && token.Span.End <= source.Length)
            _observations[token.Span] = token;
    }

    internal void Consume(Token token, TextSpan? extent = null)
    {
        Observe(token);
        _consumedEnd = Math.Max(_consumedEnd, extent?.End ?? token.Span.End);
        if (_regions.TryPeek(out var typeRegion) && typeRegion.Kind == SyntaxKind.TypeArgumentList &&
            token.TokenType is TokenType.Less or TokenType.Greater or TokenType.RightShift)
            Mark(token, SyntaxTokenRole.Punctuation);
        if (_regions.TryPeek(out var keywordRegion) && keywordRegion.Kind is SyntaxKind.Keyword or SyntaxKind.WindowFrame &&
            SyntaxTokenRoles.Of(token) is SyntaxTokenRole.Identifier or SyntaxTokenRole.Keyword)
            Mark(token, SyntaxTokenRole.Keyword);
        if (_regions.TryPeek(out var region) && region.Kind is SyntaxKind.Identifier or SyntaxKind.Alias)
        {
            if (token.TokenType != TokenType.As && SyntaxTokenRoles.Of(token) is not
                (SyntaxTokenRole.Operator or SyntaxTokenRole.Punctuation or SyntaxTokenRole.Comment or SyntaxTokenRole.Whitespace))
                Mark(token, SyntaxTokenRole.Identifier);
        }
    }

    internal void Mark(Token token, SyntaxTokenRole role) => _roles[token.Span] = role;

    internal SyntaxNode Root() => new(SyntaxKind.Document, new TextSpan(0, source.Length), source, SourceComposition.Attach(_roots, _embedded, source));

    internal IReadOnlyList<SyntaxToken> Tokens()
    {
        var tokens = new List<SyntaxToken>();
        var position = 0;
        foreach (var token in _observations.Values.OrderBy(t => t.Span.Start).ThenBy(t => t.Span.Length))
        {
            if (token.Span.Start < position)
                continue;
            AddGap(tokens, position, token.Span.Start);
            var role = _roles.GetValueOrDefault(token.Span, SyntaxTokenRoles.Of(token));
            tokens.Add(new SyntaxToken(token.TokenType, role, token.Span, source.GetText(token.Span)));
            position = token.Span.End;
        }
        AddGap(tokens, position, source.Length);
        return tokens.AsReadOnly();
    }

    private void AddGap(List<SyntaxToken> tokens, int start, int end)
    {
        if (end <= start)
            return;
        var span = new TextSpan(start, end - start);
        var text = source.GetText(span);
        var role = string.IsNullOrWhiteSpace(text) ? SyntaxTokenRole.Whitespace : SyntaxTokenRole.Unknown;
        tokens.Add(new SyntaxToken(TokenType.WhiteSpace, role, span, text));
    }

    private void End(Region region)
    {
        if (_regions.Pop() != region)
            throw new InvalidOperationException("Source regions must close in nesting order.");
        if (_consumedEnd <= region.Start)
            return;
        var node = new SyntaxNode(region.Kind, new TextSpan(region.Start, _consumedEnd - region.Start), source, region.Children);
        if (_regions.TryPeek(out var parent))
            parent.Children.Add(node);
        else
            _roots.Add(node);
    }

    internal sealed class Region(SourceCapture owner, SyntaxKind kind, int start) : IDisposable
    {
        internal SyntaxKind Kind { get; } = kind;
        internal int Start { get; } = start;
        internal List<SyntaxNode> Children { get; } = [];
        public void Dispose() => owner.End(this);
    }
}
