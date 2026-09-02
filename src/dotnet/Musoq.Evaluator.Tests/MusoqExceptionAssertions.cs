using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
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
        Assert.AreEqual(ExpectedSourceKind(expectedCode), envelope.SourceKind);
        AssertEnvelopeLocationConsistency(envelope);
        // A diagnostic may legitimately have no source location (for example, a
        // schema/runtime failure reported after the query text is no longer the
        // failing domain).  In that case the envelope must keep the location
        // unknown instead of manufacturing a SQL snippet.  Known query
        // locations still require the contextual snippet contract.
        if (envelope.SourceKind == DiagnosticSourceKind.Query && envelope.Offset.HasValue)
        {
            Assert.IsNotNull(envelope.Snippet, "Known query errors should include a source snippet");
        }
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
                $"Error at index {i}: expected {expectedCodes[i]} but got {actualCodes[i]}. " +
                $"Actual envelopes: [{string.Join(", ", exception.Envelopes.Select(e => $"{e.Code}: {e.Message}"))}]");
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
    ///     Asserts a runtime failure has one structured expression envelope.
    /// </summary>
    internal static void AssertRuntimeError(
        QueryExecutionException exception,
        DiagnosticCode expectedCode)
    {
        Assert.IsNotNull(exception.Envelope, "Expected a structured runtime diagnostic envelope.");
        if (expectedCode == DiagnosticCode.MQ9002_InternalExecutionError)
        {
            Assert.AreEqual(DiagnosticPhase.Internal, exception.Envelope.Phase);
            Assert.AreEqual(DiagnosticSeverity.Error, exception.Envelope.Severity);
            return;
        }

        Assert.AreEqual(expectedCode, exception.Envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, exception.Envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Runtime, exception.Envelope.Phase);
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
                $"Error at index {i}: expected {expectedCodes[i]} but got {actualCodes[i]}. " +
                $"Actual errors: [{string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}]");
    }

    private static DiagnosticSourceKind ExpectedSourceKind(DiagnosticCode code)
    {
        var value = (int)code;

        return value switch
        {
            >= 4001 and <= 4016 => DiagnosticSourceKind.Schema,
            >= 7003 and <= 7009 => DiagnosticSourceKind.Runtime,
            >= 7010 and <= 7012 => DiagnosticSourceKind.DataSource,
            >= 8001 and <= 8002 => DiagnosticSourceKind.GeneratedSource,
            >= 9001 and <= 9002 => DiagnosticSourceKind.Internal,
            _ => DiagnosticSourceKind.Query
        };
    }

    private static void AssertEnvelopeLocationConsistency(MusoqErrorEnvelope envelope)
    {
        var hasLocation = envelope.Offset.HasValue ||
                          envelope.EndOffset.HasValue ||
                          envelope.Length.HasValue ||
                          envelope.Line.HasValue ||
                          envelope.Column.HasValue ||
                          envelope.EndLine.HasValue ||
                          envelope.EndColumn.HasValue;

        if (!hasLocation)
            return;

        Assert.IsTrue(envelope.Offset.HasValue, "A located diagnostic must include its start offset.");
        Assert.IsTrue(envelope.EndOffset.HasValue, "A located diagnostic must include its end offset.");
        Assert.IsTrue(envelope.Length.HasValue, "A located diagnostic must include its span length.");
        Assert.IsTrue(envelope.Line.HasValue, "A located diagnostic must include its start line.");
        Assert.IsTrue(envelope.Column.HasValue, "A located diagnostic must include its start column.");
        Assert.IsTrue(envelope.EndLine.HasValue, "A located diagnostic must include its end line.");
        Assert.IsTrue(envelope.EndColumn.HasValue, "A located diagnostic must include its end column.");
        Assert.IsNotNull(envelope.Offset);
        Assert.IsTrue(envelope.Offset.Value >= 0, "A diagnostic start offset cannot be negative.");
        Assert.IsNotNull(envelope.Length);
        Assert.IsTrue(envelope.Length.Value >= 0, "A diagnostic span length cannot be negative.");
        Assert.IsNotNull(envelope.Line);
        Assert.IsTrue(envelope.Line.Value >= 1, "A diagnostic start line must be one-based.");
        Assert.IsNotNull(envelope.Column);
        Assert.IsTrue(envelope.Column.Value >= 1, "A diagnostic start column must be one-based.");
        Assert.IsNotNull(envelope.EndLine);
        Assert.IsTrue(envelope.EndLine.Value >= 1, "A diagnostic end line must be one-based.");
        Assert.IsNotNull(envelope.EndColumn);
        Assert.IsTrue(envelope.EndColumn.Value >= 1, "A diagnostic end column must be one-based.");
        Assert.IsNotNull(envelope.EndOffset);
        Assert.AreEqual(
            envelope.Offset.Value + envelope.Length.Value,
            envelope.EndOffset.Value,
            "A diagnostic span must end at offset plus length.");
    }
}
