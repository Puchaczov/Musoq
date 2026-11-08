using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CallableResolutionDiagnosticsTests : Schema.NegativeTests.NegativeTestsBase
{
    [TestMethod]
    public void UnknownCallable_ReportsNameAndSpellingFacts()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select MissingFunction(Name) from #test.people()"));

        AssertSingleError(exception, DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind, "MissingFunction");
        AssertHasGuidance(exception);
        Assert.AreEqual("MissingFunction", exception.PrimaryEnvelope.Arguments["callable"]);
        Assert.IsTrue(exception.PrimaryEnvelope.Arguments.ContainsKey("actualTypes"));
    }

    [TestMethod]
    public void KnownCallableWithWrongArity_ReportsAcceptedCounts()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select Substring(Name) from #test.people()"));

        AssertSingleError(exception, DiagnosticCode.MQ3087_InvalidCallableArity, DiagnosticPhase.Bind, "Substring");
        Assert.AreEqual("String", exception.PrimaryEnvelope.Arguments["actualTypes"]);
        Assert.AreEqual("2, 3", exception.PrimaryEnvelope.Arguments["expectedCounts"]);
        Assert.IsTrue(exception.PrimaryEnvelope.Arguments["candidateSignatures"].Contains("Substring", StringComparison.Ordinal));
    }

    [TestMethod]
    public void KnownCallableWithWrongTypes_ReportsActualTypesAndCandidates()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select Substring(Name, 'zero', 5) from #test.people()"));

        AssertSingleError(exception, DiagnosticCode.MQ3088_NoMatchingCallableOverload, DiagnosticPhase.Bind, "Substring");
        Assert.AreEqual("String, String, Int32", exception.PrimaryEnvelope.Arguments["actualTypes"]);
        Assert.IsTrue(exception.PrimaryEnvelope.Arguments["candidateSignatures"].Contains("Substring", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AggregateWithWrongTypes_UsesOverloadDiagnostic()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select Sum(Name) from #test.people()"));

        AssertSingleError(exception, DiagnosticCode.MQ3088_NoMatchingCallableOverload, DiagnosticPhase.Bind, "Sum");
        Assert.AreEqual("String", exception.PrimaryEnvelope.Arguments["actualTypes"]);
    }

    [TestMethod]
    public void ValidCallableResolution_RemainsExecutable()
    {
        var compiled = CompileQuery("select Substring(Name, 0, 2) from #test.people()");
        var rows = compiled.Run(TokenSource.Token);

        Assert.IsGreaterThan(0, rows.Count);
    }
}
