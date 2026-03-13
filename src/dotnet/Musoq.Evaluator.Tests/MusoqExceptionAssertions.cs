using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Shared assertion helpers for verifying <see cref="MusoqQueryException" /> envelope contents.
///     Ensures every error the user sees has structured, meaningful diagnostics.
/// </summary>
internal static class MusoqExceptionAssertions
{
    /// <summary>
    ///     Asserts the primary envelope has the expected diagnostic code, severity, and phase.
    /// </summary>
    internal static void AssertErrorEnvelope(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase)
    {
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(expectedCode, envelope.Code,
            $"Expected diagnostic code {expectedCode} but got {envelope.Code}. Message: {envelope.Message}");
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(expectedPhase, envelope.Phase);
        Assert.IsNotNull(envelope.Snippet, "Error should include source snippet for user context");
    }

    /// <summary>
    ///     Asserts the primary envelope has the expected code, phase, and message substring.
    /// </summary>
    internal static void AssertErrorEnvelope(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase,
        string expectedMessageSubstring)
    {
        AssertErrorEnvelope(exception, expectedCode, expectedPhase);

        StringAssert.Contains(
            exception.PrimaryEnvelope.Message,
            expectedMessageSubstring,
            $"Expected message containing '{expectedMessageSubstring}' but got: '{exception.PrimaryEnvelope.Message}'");
    }

    /// <summary>
    ///     Asserts the primary envelope has explanation text and at least one suggested fix.
    /// </summary>
    internal static void AssertHasGuidance(MusoqQueryException exception)
    {
        var envelope = exception.PrimaryEnvelope;

        Assert.IsNotNull(envelope.Explanation, "Error should include an explanation for user guidance");
        Assert.IsGreaterThan(0, envelope.SuggestedFixes.Count, "Error should include at least one suggested fix");
    }

    /// <summary>
    ///     Asserts the primary envelope message contains the given substring.
    /// </summary>
    internal static void AssertMessageContains(MusoqQueryException exception, string substring)
    {
        StringAssert.Contains(
            exception.PrimaryEnvelope.Message,
            substring,
            $"Expected message containing '{substring}' but got: '{exception.PrimaryEnvelope.Message}'");
    }

    /// <summary>
    ///     Asserts a secondary envelope at the given index has the expected code and message substring.
    /// </summary>
    internal static void AssertSecondaryEnvelopeCode(
        MusoqQueryException exception,
        int envelopeIndex,
        DiagnosticCode expectedCode,
        string expectedMessageSubstring)
    {
        Assert.IsGreaterThan(envelopeIndex, exception.Envelopes.Count,
            $"Expected at least {envelopeIndex + 1} envelopes but got {exception.Envelopes.Count}");

        var envelope = exception.Envelopes[envelopeIndex];

        Assert.AreEqual(expectedCode, envelope.Code,
            $"Expected secondary code {expectedCode} but got {envelope.Code}. Message: {envelope.Message}");
        StringAssert.Contains(
            envelope.Message,
            expectedMessageSubstring,
            $"Expected secondary message containing '{expectedMessageSubstring}' but got: '{envelope.Message}'");
    }

    /// <summary>
    ///     Asserts that any envelope (primary or secondary) contains the expected diagnostic code.
    ///     Useful when the target diagnostic is correctly emitted but cascade errors from
    ///     error-recovery stack imbalances may sort before it due to TextSpan.Empty ordering.
    /// </summary>
    internal static void AssertAnyEnvelopeHasCode(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase)
    {
        var match = exception.Envelopes.FirstOrDefault(e => e.Code == expectedCode);

        Assert.IsNotNull(match,
            $"Expected any envelope to contain {expectedCode} but found: [{string.Join(", ", exception.Envelopes.Select(e => e.Code))}]");
        Assert.AreEqual(DiagnosticSeverity.Error, match.Severity);
        Assert.AreEqual(expectedPhase, match.Phase);
    }

    /// <summary>
    ///     Asserts the exception contains exactly one error envelope with the expected code and phase.
    ///     Verifies no unexpected cascading or duplicate errors are present.
    /// </summary>
    internal static void AssertSingleError(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase)
    {
        Assert.HasCount(1, exception.Envelopes,
            $"Expected exactly 1 error but got {exception.Envelopes.Count}: [{string.Join(", ", exception.Envelopes.Select(e => $"{e.Code}: {e.Message}"))}]");

        AssertErrorEnvelope(exception, expectedCode, expectedPhase);
    }

    /// <summary>
    ///     Asserts the exception contains exactly one error envelope with the expected code, phase, and message substring.
    ///     Verifies no unexpected cascading or duplicate errors are present.
    /// </summary>
    internal static void AssertSingleError(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase,
        string expectedMessageSubstring)
    {
        Assert.HasCount(1, exception.Envelopes,
            $"Expected exactly 1 error but got {exception.Envelopes.Count}: [{string.Join(", ", exception.Envelopes.Select(e => $"{e.Code}: {e.Message}"))}]");

        AssertErrorEnvelope(exception, expectedCode, expectedPhase, expectedMessageSubstring);
    }

    /// <summary>
    ///     Asserts the exception contains exactly the expected set of error codes in order.
    ///     Verifies no unexpected cascading or duplicate errors are present.
    /// </summary>
    internal static void AssertExactErrors(
        MusoqQueryException exception,
        params DiagnosticCode[] expectedCodes)
    {
        var actualCodes = exception.Envelopes.Select(e => e.Code).ToArray();

        Assert.HasCount(expectedCodes.Length, actualCodes,
            $"Expected {expectedCodes.Length} error(s) [{string.Join(", ", expectedCodes)}] but got {actualCodes.Length}: [{string.Join(", ", exception.Envelopes.Select(e => $"{e.Code}: {e.Message}"))}]");

        for (var i = 0; i < expectedCodes.Length; i++)
            Assert.AreEqual(expectedCodes[i], actualCodes[i],
                $"Error at index {i}: expected {expectedCodes[i]} but got {actualCodes[i]}. Message: {exception.Envelopes[i].Message}");
    }

    /// <summary>
    ///     Asserts the <see cref="BuildResult" /> contains exactly one error with the expected code.
    ///     Verifies no unexpected cascading or duplicate errors are present.
    /// </summary>
    internal static void AssertSingleError(
        BuildResult result,
        DiagnosticCode expectedCode)
    {
        Assert.IsFalse(result.Succeeded, "Expected build to fail");
        Assert.HasCount(1, result.Errors,
            $"Expected exactly 1 error but got {result.Errors.Count}: [{string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}]");
        Assert.AreEqual(expectedCode, result.Errors[0].Code,
            $"Expected error code {expectedCode} but got {result.Errors[0].Code}. Message: {result.Errors[0].Message}");
    }

    /// <summary>
    ///     Asserts the <see cref="BuildResult" /> contains exactly the expected set of error codes.
    ///     Verifies no unexpected cascading or duplicate errors are present.
    /// </summary>
    internal static void AssertExactErrors(
        BuildResult result,
        params DiagnosticCode[] expectedCodes)
    {
        Assert.IsFalse(result.Succeeded, "Expected build to fail");

        var actualCodes = result.Errors.Select(e => e.Code).ToArray();

        Assert.HasCount(expectedCodes.Length, actualCodes,
            $"Expected {expectedCodes.Length} error(s) [{string.Join(", ", expectedCodes)}] but got {actualCodes.Length}: [{string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}]");

        for (var i = 0; i < expectedCodes.Length; i++)
            Assert.AreEqual(expectedCodes[i], actualCodes[i],
                $"Error at index {i}: expected {expectedCodes[i]} but got {actualCodes[i]}. Message: {result.Errors[i].Message}");
    }
}
