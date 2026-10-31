using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private void ConsumeWhiteSpaces()
    {
        while (Current.TokenType == TokenType.WhiteSpace)
            Consume(TokenType.WhiteSpace);
    }


    private void Consume(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token is {tokenType} but received {Current.TokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        _sourceCapture?.Consume(Current);
        Previous = Current;
        _hasReplacedToken = false;
        _lexer.Next();
    }



    private Token ConsumeAndGetToken(TokenType expected)
    {
        var token = Current;
        Consume(expected);
        return token;
    }


    private Token ConsumeAndGetToken()
    {
        return ConsumeAndGetToken(Current.TokenType);
    }





    private TNode ComposeAndSkipIfPresent<TNode>(Func<Parser, TNode> parserAction, TokenType type)
    {
        var node = Compose(parserAction);
        if (Current.TokenType == type)
            Consume(type);

        return node;
    }


    private TNode Compose<TNode>(Func<Parser, TNode> parserAction)
    {
        ArgumentNullException.ThrowIfNull(parserAction);

        var node = parserAction(this);
        return node;
    }

}
