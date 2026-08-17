using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private AccessMethodNode ComposeAccessMethod(string alias, bool allowNamedArguments = false)
    {
        ArgsListNode args;
        bool isDistinct;

        if (Current is FunctionToken func)
        {
            Consume(TokenType.Function);
            (args, isDistinct) = ComposeArgsWithDistinct(allowNamedArguments);
            var (hasFilter, filterExpression, filterExpressionText) = TryApplyFilterClause();
            return new AccessMethodNode(func, args, null, false, null, alias, isDistinct)
            {
                HasFilter = hasFilter,
                FilterExpression = filterExpression,
                FilterExpressionText = filterExpressionText
            };
        }

        if (Current is MethodAccessToken)
        {
            Consume(TokenType.MethodAccess);
            Consume(TokenType.Dot);
            var token = (FunctionToken)ConsumeAndGetToken(TokenType.Function);
            (args, isDistinct) = ComposeArgsWithDistinct(allowNamedArguments);
            var (hasFilter2, filterExpression2, filterExpressionText2) = TryApplyFilterClause();

            return new AccessMethodNode(token, args, null, false,
                null, alias, default, isDistinct)
            {
                HasFilter = hasFilter2,
                FilterExpression = filterExpression2,
                FilterExpressionText = filterExpressionText2
            };
        }

        throw new SyntaxException(
            $"Unrecognized token for ComposeAccessMethod(), the token was {Current.TokenType}.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }


    private (bool HasFilter, Node? FilterExpression, string? FilterExpressionText) TryApplyFilterClause()
    {
        if (!IsContextualKeyword("filter"))
            return (false, null, null);

        Consume(Current.TokenType);
        Consume(TokenType.LeftParenthesis);
        Consume(TokenType.Where);
        var filterExpression = ComposeOperations();
        Consume(TokenType.RightParenthesis);

        return (true, filterExpression, filterExpression.ToString());
    }
}
