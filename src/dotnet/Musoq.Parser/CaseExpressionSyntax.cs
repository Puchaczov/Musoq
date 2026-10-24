using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Parser;

internal static class CaseExpressionSyntax
{
    internal static Node CreateImplicitElse(TextSpan endSpan)
    {
        var span = new TextSpan(endSpan.Start, 0);
        return new ElseNode(new NullNode(span)).WithSpan(span);
    }

    internal static (Node When, Node Then) CreateBranch(Node condition, Node result) =>
        (new WhenNode(condition), new ThenNode(result));

    internal static void ThrowMissingWhen(string queryPart, TextSpan caseSpan) =>
        throw new SyntaxException(
            "CASE is reserved and starts a CASE expression here, but no WHEN branch or subject expression follows. Use [case] when the word is intended as an identifier.",
            queryPart,
            DiagnosticCode.MQ2027_MissingWhenClause,
            caseSpan);
}
