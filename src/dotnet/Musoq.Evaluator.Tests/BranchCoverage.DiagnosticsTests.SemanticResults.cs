using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region SemanticAnalysisException Branch Coverage

    [TestMethod]
    public void SemanticAnalysisException_WhenCreatedWithDiagnostic_ShouldSetProperties()
    {
        var diagnostic = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "type error", new TextSpan(0, 5));
        var ex = new SemanticAnalysisException("analysis failed", diagnostic);

        Assert.AreEqual(diagnostic, ex.PrimaryDiagnostic);
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
        Assert.AreEqual("analysis failed", ex.Message);
    }

    [TestMethod]
    public void SemanticAnalysisException_WhenCreatedWithInnerException_ShouldPreserveInner()
    {
        var diagnostic = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "error", new TextSpan(0, 5));
        var inner = new InvalidOperationException("inner");
        var ex = new SemanticAnalysisException("msg", diagnostic, inner);

        Assert.AreEqual(inner, ex.InnerException);
        Assert.AreEqual(diagnostic.Location, ex.Location);
    }

    #endregion

    #region SemanticAnalysisResult Branch Coverage

    [TestMethod]
    public void SemanticAnalysisResult_WhenCreatedWithNullDiagnostics_ShouldCreateEmptyList()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasErrors);
        Assert.IsFalse(result.HasWarnings);
        Assert.AreEqual(0, result.ErrorCount);
        Assert.AreEqual(0, result.WarningCount);
    }

    [TestMethod]
    public void SemanticAnalysisResult_WhenCreatedWithErrors_ShouldReportFailure()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "err", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(1, result.Errors.Count());
    }

    [TestMethod]
    public void SemanticAnalysisResult_WhenCreatedWithWarnings_ShouldReportWarnings()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Warning(DiagnosticCode.MQ3005_TypeMismatch, "warn", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.HasWarnings);
        Assert.AreEqual(1, result.WarningCount);
        Assert.AreEqual(1, result.Warnings.Count());
    }

    [TestMethod]
    public void SemanticAnalysisResult_AddDiagnostic_ShouldAppendToList()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);
        var diag = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "err", new TextSpan(0, 5));

        result.AddDiagnostic(diag);

        Assert.HasCount(1, result.Diagnostics);
        Assert.IsTrue(result.HasErrors);
    }

    #endregion

    #region SemanticAnalysisResult — Additional Branch Coverage

    [TestMethod]
    public void SemanticAnalysisResult_AddDiagnostics_ShouldAppendAll()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);
        var diags = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "err1", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5003_ImplicitTypeConversion, "warn1", new TextSpan(5, 10))
        };

        result.AddDiagnostics(diags);

        Assert.HasCount(2, result.Diagnostics);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(1, result.WarningCount);
    }

    [TestMethod]
    public void SemanticAnalysisResult_ThrowIfErrors_WhenNoErrors_ShouldNotThrow()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);

        result.ThrowIfErrors();
    }

    [TestMethod]
    public void SemanticAnalysisResult_ThrowIfErrors_WhenHasErrors_ShouldThrow()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "unknown col", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        var ex = Assert.Throws<SemanticAnalysisException>(result.ThrowIfErrors);
        StringAssert.Contains(ex.Message, "unknown col");
    }

    [TestMethod]
    public void SemanticAnalysisResult_ThrowIfErrors_WhenOnlyWarnings_ShouldNotThrow()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Warning(DiagnosticCode.MQ5003_ImplicitTypeConversion, "warn", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        result.ThrowIfErrors();
    }

    [TestMethod]
    public void SemanticAnalysisResult_GetDiagnosticsAt_ShouldReturnMatchingDiagnostics()
    {
        var node = new IntegerNode("1", "i");
        var diag1 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 10));
        var diag2 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(20, 30));
        var result = new SemanticAnalysisResult(node, [diag1, diag2]);

        var atFive = result.GetDiagnosticsAt(5).ToList();

        Assert.HasCount(1, atFive);
        Assert.AreEqual("err1", atFive[0].Message);
    }

    [TestMethod]
    public void SemanticAnalysisResult_GetDiagnosticsAt_WhenNoMatch_ShouldReturnEmpty()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        var atHundred = result.GetDiagnosticsAt(100).ToList();

        Assert.HasCount(0, atHundred);
    }

    [TestMethod]
    public void SemanticAnalysisResult_GetDiagnosticsIn_ShouldReturnOverlapping()
    {
        var node = new IntegerNode("1", "i");
        var diag1 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 10));
        var diag2 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(20, 30));
        var result = new SemanticAnalysisResult(node, [diag1, diag2]);

        var overlapping = result.GetDiagnosticsIn(new TextSpan(5, 25)).ToList();

        Assert.IsGreaterThanOrEqualTo(1, overlapping.Count);
    }

    #endregion
}
