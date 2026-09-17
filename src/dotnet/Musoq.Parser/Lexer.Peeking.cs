using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
   /// <summary>
    ///     Peeks ahead without consuming a token or changing lexer history.
    /// </summary>
    public Token Peek(int lookahead = 1)
    {
        if (lookahead < 1)
            throw new ArgumentOutOfRangeException(nameof(lookahead));

        var position = Position;
        var current = _currentToken;
        var last = _lastToken;
        var resolved0 = _resolvedToken0;
        var resolved1 = _resolvedToken1;
        var resolved2 = _resolvedToken2;
        var resolved3 = _resolvedToken3;
        var resolved4 = _resolvedToken4;
        var resolvedCount = _resolvedTokenCount;
        var nextResolvedSlot = _nextResolvedTokenSlot;
        var pendingSchemaTokens = _pendingSchemaTokens.Count == 0
            ? null
            : _pendingSchemaTokens.ToArray();
        var commentsCount = _comments.Count;
        var recoverOnError = RecoverOnError;

        RecoverOnError = false;
        try
        {
            Token token = current;
            for (var index = 0; index < lookahead; index++)
                token = Next();

            return token;
        }
        finally
        {
            Position = position;
            _currentToken = current;
            _lastToken = last;
            _resolvedToken0 = resolved0;
            _resolvedToken1 = resolved1;
            _resolvedToken2 = resolved2;
            _resolvedToken3 = resolved3;
            _resolvedToken4 = resolved4;
            _resolvedTokenCount = resolvedCount;
            _nextResolvedTokenSlot = nextResolvedSlot;
            if (_pendingSchemaTokens.Count > 0)
                _pendingSchemaTokens.Clear();
            if (pendingSchemaTokens != null)
                foreach (var pendingToken in pendingSchemaTokens)
                    _pendingSchemaTokens.Enqueue(pendingToken);
            if (_comments.Count > commentsCount)
                _comments.RemoveRange(commentsCount, _comments.Count - commentsCount);
            RecoverOnError = recoverOnError;
        }
    }
}
