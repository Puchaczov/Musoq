using System;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal static class DialectSyntaxFacts
{
    public static bool IsUnsupportedNumericPrefix(Node left, Token token, string queryPart)
    {
        if (token.TokenType == TokenType.Integer && left is IdentifierNode identifier &&
            (identifier.Name.Equals("TOP", StringComparison.OrdinalIgnoreCase) ||
             identifier.Name.Equals("FIRST", StringComparison.OrdinalIgnoreCase)))
            throw new SyntaxException(
                $"Musoq does not use {identifier.Name} in the SELECT list. Use TAKE after the FROM clause instead.",
                queryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                left.Span);

        return false;
    }
}
