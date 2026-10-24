using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal sealed class SchemaTokenStream(ILexer lexer)
{
    private readonly Queue<Token> _lookahead = new();
    private Token? _current;

    internal Token Current => _current ?? lexer.Current();

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

    internal void Advance()
    {
        if (_lookahead.Count > 0)
        {
            _current = _lookahead.Dequeue();
            return;
        }
        _current = null;
        lexer.Next();
    }
}
