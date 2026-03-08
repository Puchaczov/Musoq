using System;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Exception thrown when the lexer encounters an unrecognized token.
/// </summary>
public class UnknownTokenException : LexerException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UnknownTokenException" /> class.
    /// </summary>
    /// <param name="position">The position in the input where the unknown token was found.</param>
    /// <param name="character">The unrecognized character.</param>
    /// <param name="remainingInput">The remaining unparsed input.</param>
    public UnknownTokenException(int position, char character, string remainingInput)
        : base(
            $"Token '{character}' that starts at position {position} was unrecognized. Rest of the unparsed query is '{remainingInput}'",
            position,
            DiagnosticCode.MQ1001_UnknownToken)
    {
        Character = character;
        RemainingInput = remainingInput;
    }

    /// <summary>
    ///     Gets the unrecognized character.
    /// </summary>
    public char Character { get; }

    /// <summary>
    ///     Gets the remaining unparsed input.
    /// </summary>
    public string RemainingInput { get; }

    /// <inheritdoc />
    public override TextSpan? Span => new TextSpan(Position, 1);

    /// <inheritdoc />
    public override Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? new TextSpan(Position, 1);
        var remainingInputPreview = RemainingInput[..Math.Min(50, RemainingInput.Length)];
        var message =
            $"Unknown token '{Character}'. Remove the unsupported character or rewrite this part using valid Musoq syntax.";
        var relatedInfo = new[] { $"Remaining input: {remainingInputPreview}..." };

        if (sourceText is null)
            return SyntaxDiagnosticEnhancer.EnhanceLexerDiagnostic(Code, message, span, sourceText, relatedInfo);

        return SyntaxDiagnosticEnhancer.EnhanceLexerDiagnostic(Code, message, span, sourceText, relatedInfo);
    }
}
