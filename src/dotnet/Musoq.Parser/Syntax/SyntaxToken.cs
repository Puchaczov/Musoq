using Musoq.Parser.Tokens;

namespace Musoq.Parser.Syntax;

/// <summary>An immutable original lexeme, including trivia and its exact source extent.</summary>
public sealed record SyntaxToken(TokenType TokenType, SyntaxTokenRole Role, TextSpan Span, string Text);
