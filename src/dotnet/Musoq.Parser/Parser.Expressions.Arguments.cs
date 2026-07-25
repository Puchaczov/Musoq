using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ArgsListNode ComposeArgs(bool allowNamedArguments = false)
    {
        var args = new List<Node>();
        var argumentNames = new List<ArgumentName?>();
        var hasNamedArgument = false;

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                var argument = ComposeArgument(allowNamedArguments, hasNamedArgument, out var argumentName);
                if (argumentName.HasValue)
                    hasNamedArgument = true;

                args.Add(argument);
                argumentNames.Add(argumentName);
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return new ArgsListNode(
            args.ToArray(),
            argumentNames.Count == 0 ? null : argumentNames.ToArray(),
            default);
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


    private (ArgsListNode Args, bool IsDistinct) ComposeArgsWithDistinct(bool allowNamedArguments = false)
    {
        var args = new List<Node>();
        var argumentNames = new List<ArgumentName?>();
        var hasNamedArgument = false;
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

                var argument = ComposeArgument(allowNamedArguments, hasNamedArgument, out var argumentName);
                if (argumentName.HasValue)
                    hasNamedArgument = true;

                args.Add(argument);
                argumentNames.Add(argumentName);
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return (
            new ArgsListNode(
                args.ToArray(),
                argumentNames.Count == 0 ? null : argumentNames.ToArray(),
                default),
            isDistinct);
    }

    private Node ComposeArgument(
        bool allowNamedArguments,
        bool hasNamedArgument,
        out ArgumentName? argumentName)
    {
        argumentName = null;
        var labelToken = Current;
        var argument = ComposeEqualityOperators();

        if (Current.TokenType != TokenType.Colon)
        {
            if (hasNamedArgument)
                throw new SyntaxException(
                    "Positional datasource arguments must appear before named arguments.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                    argument.Span);

            return argument;
        }

        if (!allowNamedArguments)
            throw new SyntaxException(
                "Named arguments are supported only for datasource source calls.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                labelToken.Span);

        if (labelToken.TokenType is not (TokenType.Word or TokenType.Identifier) ||
            argument is not (WordNode or IdentifierNode))
            throw new SyntaxException(
                "A datasource argument name must be a simple identifier.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                argument.Span);

        Consume(TokenType.Colon);
        if (Current.TokenType is TokenType.RightParenthesis or TokenType.Comma or TokenType.EndOfFile)
            throw new SyntaxException(
                $"Datasource argument '{labelToken.Value}' is missing a value.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                labelToken.Span.Through(Current.Span));

        argumentName = new ArgumentName(labelToken.Value, labelToken.Span);
        return ComposeEqualityOperators();
    }

}
