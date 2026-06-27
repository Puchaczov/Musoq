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
            var (hasFilter, filteredArgs) = TryApplyFilterClause(args);
            return new AccessMethodNode(func, filteredArgs, null, false, null, alias, isDistinct) { HasFilter = hasFilter };
        }

        if (Current is MethodAccessToken)
        {
            Consume(TokenType.MethodAccess);
            Consume(TokenType.Dot);
            var token = (FunctionToken)ConsumeAndGetToken(TokenType.Function);
            (args, isDistinct) = ComposeArgsWithDistinct();
            var (hasFilter2, filteredArgs2) = TryApplyFilterClause(args);

            return new AccessMethodNode(token, filteredArgs2, null, false,
                null, alias, default, isDistinct) { HasFilter = hasFilter2 };
        }

        throw new NotSupportedException(
            $"Unrecognized token for ComposeAccessMethod(), the token was {Current.TokenType}");
    }


    private (bool HasFilter, ArgsListNode Args) TryApplyFilterClause(ArgsListNode args)
    {
        if (!IsContextualKeyword("filter"))
            return (false, args);

        Consume(Current.TokenType);
        Consume(TokenType.LeftParenthesis);
        Consume(TokenType.Where);
        var filterExpression = ComposeOperations();
        Consume(TokenType.RightParenthesis);

        var wrappedArgs = new Node[args.Args.Length];
        for (var i = 0; i < args.Args.Length; i++)
        {
            wrappedArgs[i] = new CaseNode(
                [(new WhenNode(filterExpression), new ThenNode(args.Args[i]))],
                new ElseNode(new NullNode()));
        }

        return (true, new ArgsListNode(wrappedArgs));
    }

}
