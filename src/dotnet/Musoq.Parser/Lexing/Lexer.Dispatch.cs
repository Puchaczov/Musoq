using System.Runtime.CompilerServices;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer
{

    private Token NextInternal()
    {
        if (Position >= Input.Length)
            return AssignToken(new EndOfFileToken(new TextSpan(Input.Length, 0)));

        var category = FastCharacterClassifier.GetCategory(Input.AsSpan(), Position);

        return category switch
        {
            FastCharacterClassifier.CharCategory.Whitespace => ScanWhitespace(),
            FastCharacterClassifier.CharCategory.Identifier => ScanIdentifierOrKeyword(),
            FastCharacterClassifier.CharCategory.Digit => ScanNumber(),
            FastCharacterClassifier.CharCategory.Quote => ScanStringLiteral(),
            FastCharacterClassifier.CharCategory.SingleCharOperator => ScanSingleCharOperator(),
            FastCharacterClassifier.CharCategory.MultiCharOperator => ScanMultiCharOperator(),
            FastCharacterClassifier.CharCategory.Hash => ScanHashFrom(),
            FastCharacterClassifier.CharCategory.Dash => ScanDash(),
            FastCharacterClassifier.CharCategory.Slash => ScanSlash(),
            FastCharacterClassifier.CharCategory.Dot => ScanDot(),
            FastCharacterClassifier.CharCategory.SquareBracket => ScanSquareBracket(),
            FastCharacterClassifier.CharCategory.Colon => ScanColon(),
            FastCharacterClassifier.CharCategory.Dollar => ScanParameterReference(),
            FastCharacterClassifier.CharCategory.QuestionMark => ScanQuestionMark(),
            _ => ScanUnknown()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token ScanWhitespace()
    {
        var start = Position;
        while (Position < Input.Length && FastCharacterClassifier.IsWhitespace(Input[Position]))
            Position++;

        return AssignToken(new WhiteSpaceToken(new TextSpan(start, Position - start)));
    }
}
