using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private AccessMethodNode ComposeAccessMethod(string alias)
    {
        ArgsListNode args;
        bool isDistinct;

        if (Current is FunctionToken func)
        {
            Consume(TokenType.Function);
            (args, isDistinct) = ComposeArgsWithDistinct();
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
            (args, isDistinct) = ComposeArgsWithDistinct();
            var (hasFilter2, filterExpression2, filterExpressionText2) = TryApplyFilterClause();

            return new AccessMethodNode(token, args, null, false,
                null, alias, default, isDistinct)
            {
                HasFilter = hasFilter2,
                FilterExpression = filterExpression2,
                FilterExpressionText = filterExpressionText2
            };
        }

        throw new NotSupportedException(
            $"Unrecognized token for ComposeAccessMethod(), the token was {Current.TokenType}");
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
