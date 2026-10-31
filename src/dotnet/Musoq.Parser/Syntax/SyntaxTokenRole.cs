namespace Musoq.Parser.Syntax;

/// <summary>Describes the lexical or contextual role of original source text.</summary>
public enum SyntaxTokenRole
{
    Keyword, Identifier, Literal, TypeName, Operator, Punctuation, Whitespace, Comment, Unknown
}
