using System.Collections.Generic;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ArgsListNode ComposeArgs()
    {
        var args = new List<Node>();

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                args.Add(ComposeEqualityOperators());
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return new ArgsListNode(args.ToArray());
    }


    private ArgsListNode ComposeNonEmptyArgs(string operatorName)
    {
        var args = ComposeArgs();

        if (args.Args.Length != 0)
            return args;

        throw new SyntaxException(
            $"{operatorName} requires at least one argument inside parentheses.",
            _lexer.AlreadyResolvedQueryPart);
    }


    private (ArgsListNode Args, bool IsDistinct) ComposeArgsWithDistinct()
    {
        var args = new List<Node>();
        var isDistinct = false;

        Consume(TokenType.LeftParenthesis);


        if (Current.TokenType == TokenType.Distinct)
        {
            Consume(TokenType.Distinct);
            isDistinct = true;
        }

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                args.Add(ComposeEqualityOperators());
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return (new ArgsListNode(args.ToArray()), isDistinct);
    }

}
