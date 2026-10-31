using Musoq.Parser.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeSliceCount(string clause)
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.PagingClause, Current);
        if (Current.TokenType == TokenType.ParameterReference)
        {
            var reference = ConsumeAndGetToken(TokenType.ParameterReference);
            return new ParameterReferenceNode(reference.Value, null, reference.Span);
        }

        if (Current.TokenType != TokenType.Integer)
            throw InvalidSliceCount(clause, Current.Span);

        var countToken = Current;
        var count = ComposeInteger();
        if (countToken.Value.StartsWith("-", StringComparison.Ordinal) || IsNegativeInteger(count))
            throw InvalidSliceCount(clause, countToken.Span);

        return count;
    }

    private static bool IsNegativeInteger(IntegerNode intNode)
    {
        return intNode.ObjValue switch
        {
            int i => i < 0,
            long l => l < 0,
            short s => s < 0,
            sbyte sb => sb < 0,
            _ => false
        };
    }

    private SyntaxException InvalidSliceCount(string clause, TextSpan span)
    {
        return new SyntaxException(
            $"{clause} count must be a non-negative integer.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2038_InvalidSliceCount,
            span);
    }
}
