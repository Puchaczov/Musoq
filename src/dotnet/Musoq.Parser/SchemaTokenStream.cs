using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Lexing;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal sealed class SchemaTokenStream(ILexer lexer, Syntax.SourceCapture? capture)
{
    private readonly Queue<Token> _lookahead = new();
    private Token? _current;

    private int _pendingGreater;
    private int _pendingGreaterPosition;

    internal Token Current => _pendingGreater > 0 ? new GreaterToken(TextSpan.Empty) : _current ?? lexer.Current();

    internal Token Peek(int distance)
    {
        _current ??= lexer.Current();
        while (_lookahead.Count < distance)
        {
            lexer.Next();
            _lookahead.Enqueue(lexer.Current());
        }
        return _lookahead.ElementAt(distance - 1);
    }

    internal void Advance(TextSpan? extent = null)
    {
        if (_pendingGreater > 0)
        {
            capture?.Consume(new GreaterToken(new TextSpan(_pendingGreaterPosition++, 1)));
            _pendingGreater--;
            return;
        }
        capture?.Consume(Current, extent);
        if (_lookahead.Count > 0)
        {
            _current = _lookahead.Dequeue();
            return;
        }
        _current = null;
        lexer.Next();
    }

    internal void ConsumeGenericGreater()
    {
        if (Current.TokenType == TokenType.Greater)
        {
            Advance();
            return;
        }
        if (Current.TokenType != TokenType.RightShift)
            throw new SyntaxException($"Expected token '{TokenType.Greater}' but found '{Current.TokenType}'", lexer.AlreadyResolvedQueryPart);
        var start = Current.Span.Start;
        Advance(new TextSpan(start, 1));
        _pendingGreaterPosition = start + 1;
        _pendingGreater = 1;
    }

}
