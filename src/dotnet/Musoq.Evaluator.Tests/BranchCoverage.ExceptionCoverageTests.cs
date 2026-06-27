using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region Exception Branch Coverage — ConstructionNotYetSupported

    [TestMethod]
    public void ConstructionNotYetSupported_WhenCreatedWithMessage_ShouldSetCodeAndNullSpan()
    {
        var ex = new ConstructionNotYetSupported("test message");

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.AreEqual("test message", ex.Message);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_WhenCreatedWithSpan_ShouldSetCodeAndSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new ConstructionNotYetSupported("test", span);

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_ToDiagnostic_WhenSpanIsNull_ShouldUseEmptySpan()
    {
        var ex = new ConstructionNotYetSupported("test message");

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("test message", diagnostic.Message);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_ToDiagnostic_WhenSpanIsSet_ShouldUseSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new ConstructionNotYetSupported("test", span);

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, diagnostic.Code);
        Assert.AreEqual("test", diagnostic.Message);
    }

    #endregion
























}
