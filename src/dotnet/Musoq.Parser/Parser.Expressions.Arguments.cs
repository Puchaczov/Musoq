using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ArgsListNode ComposeArgs(bool allowNamedArguments = false)
    {
        var args = new System.Collections.Generic.List<Node>();
        var argumentNames = new System.Collections.Generic.List<ArgumentName?>();
        var hasNamedArgument = false;
        var openingToken = ConsumeAndGetToken(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);
                var argument = ComposeArgument(allowNamedArguments, hasNamedArgument, args.Count, out var argumentName);
                if (argumentName.HasValue)
                    hasNamedArgument = true;
                args.Add(argument);
                argumentNames.Add(argumentName);
            } while (Current.TokenType == TokenType.Comma);
        var closingToken = ConsumeClosingParenthesis(
            DiagnosticCode.MQ2021_UnclosedFunctionCall,
            "A function call is missing its closing parenthesis.");

        return new ArgsListNode(
            args.ToArray(),
            argumentNames.Count == 0 ? null : argumentNames.ToArray(),
            openingToken.Span.Through(closingToken.Span));
    }

    private ArgsListNode ComposeNonEmptyArgs(string operatorName)
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            throw ParserDiagnosticFacts.MissingToken($"{operatorName} requires a parenthesized value list.",
                _lexer.AlreadyResolvedQueryPart, new TextSpan(Current.Span.Start, 0));

        var args = ComposeArgs();

        if (args.Args.Length != 0)
            return args;

        throw ParserDiagnosticFacts.EmptyPredicateList($"{operatorName} requires at least one argument inside parentheses.",
            _lexer.AlreadyResolvedQueryPart, args.Span.Length > 0 ? new TextSpan(args.Span.End - 1, 1) : Current.Span);
    }

    private (ArgsListNode Args, bool IsDistinct) ComposeArgsWithDistinct(bool allowNamedArguments = false)
    {
        var args = new System.Collections.Generic.List<Node>();
        var argumentNames = new System.Collections.Generic.List<ArgumentName?>();
        var hasNamedArgument = false;
        var isDistinct = false;
        var openingToken = ConsumeAndGetToken(TokenType.LeftParenthesis);

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

                var argument = ComposeArgument(allowNamedArguments, hasNamedArgument, args.Count, out var argumentName);
                if (argumentName.HasValue)
                    hasNamedArgument = true;

                args.Add(argument);
                argumentNames.Add(argumentName);
            } while (Current.TokenType == TokenType.Comma);

        var closingToken = ConsumeClosingParenthesis(
            DiagnosticCode.MQ2021_UnclosedFunctionCall,
            "A function call is missing its closing parenthesis.");

        return (
            new ArgsListNode(
                args.ToArray(),
                argumentNames.Count == 0 ? null : argumentNames.ToArray(),
                openingToken.Span.Through(closingToken.Span)),
            isDistinct);
    }

    private Node ComposeArgument(
        bool allowNamedArguments,
        bool hasNamedArgument, int argumentIndex,
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
                    argument.Span, ParserDiagnosticFacts.PositionalAfterNamed(argumentIndex));

            return argument;
        }

        if (!allowNamedArguments)
            throw new SyntaxException(
                "Named arguments are supported only for datasource source calls.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                labelToken.Span, ParserDiagnosticFacts.NamedOutsideDatasource(labelToken.Value));

        if (labelToken.TokenType is not (TokenType.Word or TokenType.Identifier) ||
            argument is not (WordNode or IdentifierNode))
            throw new SyntaxException(
                "A datasource argument name must be a simple identifier.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                argument.Span, ParserDiagnosticFacts.InvalidArgumentName(labelToken.Value));

        Consume(TokenType.Colon);
        if (Current.TokenType is TokenType.RightParenthesis or TokenType.Comma or TokenType.EndOfFile)
            throw new SyntaxException(
                $"Datasource argument '{labelToken.Value}' is missing a value.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                new TextSpan(Current.Span.Start, 0), ParserDiagnosticFacts.MissingArgumentValue(labelToken.Value));

        argumentName = new ArgumentName(labelToken.Value, labelToken.Span);
        return ComposeEqualityOperators();
    }

}
