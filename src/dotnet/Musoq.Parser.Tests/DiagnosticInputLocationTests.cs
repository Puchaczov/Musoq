using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticInputLocationTests
{
    [TestMethod]
    public void RuntimeInputFailure_ShouldPreserveControlledReasonAndInputLocation()
    {
        var innerException = new InvalidDataException("secret record payload and provider path");
        var exception = new RuntimeInputDiagnosticException(
            RuntimeInputFailureReason.MalformedJson,
            inputLocation: new DiagnosticInputLocation(line: 2, column: 4, byteOffset: 19),
            innerException: innerException);

        var wrapped = new InvalidOperationException("wrapper detail", exception);
        var diagnostic = wrapped.ToDiagnosticOrGeneric();
        var envelope = MusoqErrorEnvelope.FromException(wrapped);
        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);

        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, diagnostic.SourceKind);
        Assert.AreEqual("malformed-json", diagnostic.Arguments["inputReason"]);
        Assert.AreEqual(2L, diagnostic.InputLocation?.Line);
        Assert.AreEqual(19L, diagnostic.InputLocation?.ByteOffset);
        Assert.AreEqual(DiagnosticCode.MQ7014_MalformedRuntimeInput, envelope.Code);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.Details);
        StringAssert.Contains(json, "\"inputLocation\":{\"line\":2,\"column\":4,\"byteOffset\":19}");
        Assert.DoesNotContain("\"location\"", json);
        Assert.DoesNotContain("\"stackTrace\"", json);
        Assert.DoesNotContain("secret record payload", json);
        Assert.DoesNotContain("provider path", json);
    }

    [TestMethod]
    public void RuntimeInputFailure_VerboseEnvelope_ShouldIncludeTrustedInnerDetails()
    {
        const string sensitiveDetail = "trusted-only payload detail";
        var exception = new RuntimeInputDiagnosticException(
            RuntimeInputFailureReason.UnterminatedQuotedField,
            DiagnosticSourceKind.DataSource,
            new DiagnosticInputLocation(byteOffset: 41),
            new InvalidDataException(sensitiveDetail));

        var safe = MusoqErrorEnvelope.FromException(exception);
        var verbose = MusoqErrorEnvelope.FromExceptionVerbose(exception);

        Assert.AreEqual("The final quoted field is not terminated.", safe.Message);
        Assert.IsNull(safe.Details);
        Assert.IsNotNull(verbose.Details);
        StringAssert.Contains(verbose.Details, sensitiveDetail);
    }
}
