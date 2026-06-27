using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private Token ScanQuestionMark()
    {
        var start = Position;

        if (Position + 1 < Input.Length && Input[Position + 1] == '?')
        {
            Position += 2;
            return AssignToken(new NullCoalescingToken(new TextSpan(start, 2)));
        }

        Position++;
        return AssignToken(new QuestionMarkToken(new TextSpan(start, 1)));
    }
}