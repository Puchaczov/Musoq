using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PrecisionDiagnosticTests
{
    private const string RegexPhaseCandidateQuery =
        "select p.Value as Value from values { (Value: 'x') } p where p.Value rlike '['";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0251")]
    public void InvalidConstantRegex_ShouldFailDuringBinding()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CompileRegexPhaseCandidate());

        MusoqExceptionAssertions.AssertSingleError(exception, DiagnosticCode.MQ3094_InvalidConstantRegex, DiagnosticPhase.Bind, "regex");
        MusoqExceptionAssertions.AssertHasGuidance(exception);
        Assert.AreEqual(75, exception.PrimaryEnvelope.Offset);
        Assert.AreEqual(3, exception.PrimaryEnvelope.Length);
    }

    private CompiledQuery CompileRegexPhaseCandidate()
    {
        return CreateAndRunVirtualMachine(RegexPhaseCandidateQuery, new Dictionary<string, IEnumerable<BasicEntity>>());
    }
}
