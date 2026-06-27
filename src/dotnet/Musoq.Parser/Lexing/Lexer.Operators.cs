using System.Runtime.CompilerServices;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token ScanSingleCharOperator()
    {
        var c = Input[Position];
        var start = Position;
        Position++;

        if (FastCharacterClassifier.TryGetSingleCharOperator(c, out var tokenType))
            return AssignToken(TokenFactory.Create(tokenType, start, FastCharacterClassifier.CharToString(c))
                               ?? new WordToken(FastCharacterClassifier.CharToString(c), new TextSpan(start, 1)));

        return AssignToken(new WordToken(FastCharacterClassifier.CharToString(c), new TextSpan(start, 1)));
    }

    private Token ScanMultiCharOperator()
    {
        var start = Position;
        var c = Input[Position];
        var hasNext = Position + 1 < Input.Length;
        var next = hasNext ? Input[Position + 1] : '\0';

        switch (c)
        {
            case '<':
                if (next == '=')
                {
                    Position += 2;
                    return AssignToken(new LessEqualToken(new TextSpan(start, 2)));
                }

                if (next == '<')
                {
                    Position += 2;
                    return AssignToken(new LeftShiftToken(new TextSpan(start, 2)));
                }

                if (next == '>')
                {
                    Position += 2;
                    return AssignToken(new DiffToken(new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new LessToken(new TextSpan(start, 1)));

            case '>':
                if (next == '=')
                {
                    Position += 2;
                    return AssignToken(new GreaterEqualToken(new TextSpan(start, 2)));
                }

                if (next == '>')
                {
                    Position += 2;
                    return AssignToken(new RightShiftToken(new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new GreaterToken(new TextSpan(start, 1)));

            case '=':
                if (next == '>')
                {
                    Position += 2;
                    return AssignToken(new FatArrowToken(new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new EqualityToken(new TextSpan(start, 1)));

            case '!':
                if (next == '=')
                {
                    Position += 2;
                    return AssignToken(new DiffToken("!=", new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new WordToken("!", new TextSpan(start, 1)));

            case '&':
                Position++;
                return AssignToken(new AmpersandToken(new TextSpan(start, 1)));

            case '|':
                Position++;
                return AssignToken(new PipeToken(new TextSpan(start, 1)));

            case '^':
                Position++;
                return AssignToken(new CaretToken(new TextSpan(start, 1)));
        }

        Position++;
        return AssignToken(new WordToken(FastCharacterClassifier.CharToString(c), new TextSpan(start, 1)));
    }

    private Token ScanHashFrom()
    {
        var start = Position;
        var match = HFromRegex.Match(Input, Position);

        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            return AssignToken(new WordToken(match.Value, new TextSpan(start, match.Length)));
        }

        Position++;
        return AssignToken(new WordToken("#", new TextSpan(start, 1)));
    }

    private Token ScanDash()
    {
        var start = Position;


        if (Position + 1 < Input.Length && Input[Position + 1] == '-')
        {
            var match = LineCommentRegex.Match(Input, Position);
            if (match.Success && match.Index == Position)
            {
                Position += match.Length;
                return AssignToken(new CommentToken(match.Value, new TextSpan(start, match.Length)));
            }
        }


        if (Position + 1 < Input.Length && FastCharacterClassifier.IsDigit(Input[Position + 1]))
        {
            Position++;
            var numToken = ScanNumber();

            var numText = "-" + numToken.Value;
            return AssignToken(numToken switch
            {
                DecimalToken => new DecimalToken(numText, new TextSpan(start, numToken.Span.End - start)),
                IntegerToken it => new IntegerToken(numText, new TextSpan(start, numToken.Span.End - start),
                    it.Abbreviation),
                _ => numToken
            });
        }


        Position++;
        return AssignToken(new HyphenToken(new TextSpan(start, 1)));
    }

    private Token ScanSlash()
    {
        var start = Position;


        if (Position + 1 < Input.Length && Input[Position + 1] == '*')
        {
            var match = BlockCommentRegex.Match(Input, Position);
            if (match.Success && match.Index == Position)
            {
                Position += match.Length;
                return AssignToken(new CommentToken(match.Value, new TextSpan(start, match.Length)));
            }

            var span = new TextSpan(start, Input.Length - start);

            if (RecoverOnError)
            {
                Diagnostics.AddError(DiagnosticCode.MQ1005_UnterminatedBlockComment, span, Input[start..]);
                Position = Input.Length;
                return AssignToken(new ErrorToken(Input[start], span));
            }

            Position = Input.Length;
            throw new LexerException(
                "Unterminated block comment. Expected closing '*/' but reached end of input.",
                start,
                DiagnosticCode.MQ1005_UnterminatedBlockComment);
        }


        Position++;
        return AssignToken(new FSlashToken(new TextSpan(start, 1)));
    }

    private Token ScanDot()
    {
        var start = Position;


        if (Position + 1 < Input.Length && FastCharacterClassifier.IsDigit(Input[Position + 1]))
        {
            Position++;
            while (Position < Input.Length && FastCharacterClassifier.IsDigit(Input[Position]))
                Position++;

            if (Position < Input.Length && (Input[Position] == 'd' || Input[Position] == 'D'))
                Position++;

            var text = Input[start..Position];
            return AssignToken(new DecimalToken(text, new TextSpan(start, Position - start)));
        }

        Position++;
        return AssignToken(new DotToken(new TextSpan(start, 1)));
    }

    private Token ScanSquareBracket()
    {
        var start = Position;


        var match = BracketedColumnRegex.Match(Input, Position);
        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            var text = match.Value;


            if (IsSchemaContext)
            {
                var innerValue = text[1..^1];
                _pendingSchemaTokens.Enqueue(
                    int.TryParse(innerValue, out _)
                        ? new IntegerToken(innerValue, new TextSpan(start + 1, innerValue.Length), "i")
                        : new WordToken(innerValue, new TextSpan(start + 1, innerValue.Length)));
                _pendingSchemaTokens.Enqueue(new RightSquareBracketToken(new TextSpan(start + text.Length - 1, 1)));
                return AssignToken(new LeftSquareBracketToken(new TextSpan(start, 1)));
            }


            var columnName = text[1..^1];
            return AssignToken(new ColumnToken(columnName, new TextSpan(start, text.Length)));
        }


        Position++;
        return AssignToken(new LeftSquareBracketToken(new TextSpan(start, 1)));
    }

    private Token ScanColon()
    {
        var start = Position;


        if (Position + 1 < Input.Length && Input[Position + 1] == ':')
        {
            Position += 2;
            return AssignToken(new DoubleColonToken(new TextSpan(start, 2)));
        }

        Position++;
        return AssignToken(TokenFactory.Create(TokenType.Colon, start, ":") ??
                           new WordToken(":", new TextSpan(start, 1)));
    }
}
