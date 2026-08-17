using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LoweringAttemptTests
{
    [TestMethod]
    public void NoMatch_ShouldNotBeTerminalOrContainAValue()
    {
        var attempt = LoweringAttempt<string>.NoMatch();

        Assert.AreEqual(LoweringAttemptKind.NoMatch, attempt.Kind);
        Assert.IsFalse(attempt.IsTerminal);
        Assert.ThrowsExactly<InvalidOperationException>(() => attempt.RequireValue());
    }

    [TestMethod]
    public void Built_ShouldBeTerminalAndExposeItsValue()
    {
        var attempt = LoweringAttempt<string>.Built("built");

        Assert.AreEqual(LoweringAttemptKind.Built, attempt.Kind);
        Assert.IsTrue(attempt.IsTerminal);
        Assert.AreEqual("built", attempt.RequireValue());
    }

    [TestMethod]
    public void OptionalValue_ShouldRepresentAnOptionalBuiltPayload()
    {
        var attempt = LoweringAttempt<OptionalValue<string>>.Built(OptionalValue<string>.None());

        Assert.AreEqual(LoweringAttemptKind.Built, attempt.Kind);
        Assert.IsTrue(attempt.IsBuilt);
        Assert.IsFalse(attempt.Value.HasValue);
    }

    [TestMethod]
    public void Unsupported_ShouldBeTerminalAndExposeItsDiagnostic()
    {
        var attempt = LoweringAttempt<string>.Unsupported("unsupported");

        Assert.AreEqual(LoweringAttemptKind.Unsupported, attempt.Kind);
        Assert.IsTrue(attempt.IsTerminal);
        Assert.IsTrue(attempt.IsUnsupported);
        Assert.AreEqual("unsupported", attempt.RequireUnsupportedReason());
        Assert.ThrowsExactly<InvalidOperationException>(() => attempt.RequireValue());
    }

    [TestMethod]
    public void TerminalAttempts_ShouldRejectNullValues()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => LoweringAttempt<string>.Built(null!));
        Assert.ThrowsExactly<ArgumentException>(() => LoweringAttempt<string>.Unsupported(string.Empty));
    }
}
