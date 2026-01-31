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
    public override TextSpan Span => new(Position, 1);

    /// <inheritdoc />
    public override Diagnostic ToDiagnostic(SourceText sourceText)
    {
        var location = sourceText.GetLocation(Position);
        var contextSnippet = sourceText.GetContextSnippet(Span);

        return new Diagnostic(
                Code,
                DiagnosticSeverity.Error,
                $"Unknown token '{Character}'",
                location,
                null,
                contextSnippet)
            .WithRelatedInfo($"Remaining input: {RemainingInput.Substring(0, Math.Min(50, RemainingInput.Length))}...");
    }
}
