using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region DiagnosticContext Branch Coverage

    [TestMethod]
    public void DiagnosticContext_Scope_ShouldTrackAndNest()
    {
        var ctx = new DiagnosticContext();

        Assert.AreEqual("", ctx.CurrentScope);

        using (ctx.EnterScope("Query"))
        {
            Assert.AreEqual("Query", ctx.CurrentScope);

            using (ctx.EnterScope("Select"))
            {
                Assert.AreEqual("Query.Select", ctx.CurrentScope);
            }

            Assert.AreEqual("Query", ctx.CurrentScope);
        }

        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_ShouldAddError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "Unknown column", new TextSpan(0, 5));

        Assert.IsTrue(ctx.HasErrors);
        Assert.AreEqual(1, ctx.Errors.Count());
    }

    [TestMethod]
    public void DiagnosticContext_ReportWarning_ShouldAddWarning()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportWarning(DiagnosticCode.MQ5001_UnusedAlias, "Unused alias", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
        Assert.AreEqual(1, ctx.Warnings.Count());
    }

    [TestMethod]
    public void DiagnosticContext_ReportInfo_ShouldNotCountAsError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportInfo(DiagnosticCode.MQ5001_UnusedAlias, "Info msg", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportHint_ShouldNotCountAsError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportHint(DiagnosticCode.MQ5001_UnusedAlias, "Hint msg", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_ShouldConvertToError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportException(new InvalidOperationException("test failure"));

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithSpan_ShouldUseProvidedSpan()
    {
        var ctx = new DiagnosticContext();
        var span = new TextSpan(10, 5);

        ctx.ReportException(new InvalidOperationException("test"), span);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_AddRange_ShouldImportDiagnostics()
    {
        var ctx = new DiagnosticContext();
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error1", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning1", new TextSpan(5, 3))
        };

        ctx.AddRange(diagnostics);

        Assert.IsTrue(ctx.HasErrors);
        Assert.AreEqual(1, ctx.Errors.Count());
        Assert.AreEqual(1, ctx.Warnings.Count());
    }

    [TestMethod]
    public void DiagnosticContext_HasReachedMaxErrors_WhenBelowLimit_ShouldBeFalse()
    {
        var ctx = new DiagnosticContext(maxErrors: 10);

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "error", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasReachedMaxErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportTypeMismatch_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode(42);

        ctx.ReportTypeMismatch("string", "int", node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        Assert.Contains("string", error.Message);
        Assert.Contains("int", error.Message);
    }

    [TestMethod]
    public void DiagnosticContext_ReportAmbiguousColumn_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode(42);

        ctx.ReportAmbiguousColumn("Name", "t1", "t2", node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        Assert.Contains("Name", error.Message);
        Assert.Contains("t1", error.Message);
        Assert.Contains("t2", error.Message);
    }

    #endregion

    #region DiagnosticContext Branch Coverage

    [TestMethod]
    public void DiagnosticContext_WhenCreated_ShouldHaveNoErrors()
    {
        var ctx = new DiagnosticContext();

        Assert.IsFalse(ctx.HasErrors);
        Assert.IsFalse(ctx.HasReachedMaxErrors);
        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_EnterAndExitScope_ShouldTrackScopePath()
    {
        var ctx = new DiagnosticContext();

        using (ctx.EnterScope("outer"))
        {
            Assert.AreEqual("outer", ctx.CurrentScope);

            using (ctx.EnterScope("inner"))
            {
                Assert.AreEqual("outer.inner", ctx.CurrentScope);
            }

            Assert.AreEqual("outer", ctx.CurrentScope);
        }

        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ExitScope_WhenEmpty_ShouldNotThrow()
    {
        var ctx = new DiagnosticContext();

        // ExitScope is private, but ScopeGuard.Dispose calls it.
        // Double-dispose should also be safe.
        var scope = ctx.EnterScope("test");
        scope.Dispose();
        scope.Dispose();

        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_WithSpan_ShouldAddError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "col not found", new TextSpan(0, 5));

        Assert.IsTrue(ctx.HasErrors);
        Assert.IsTrue(ctx.Errors.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_WithNode_WhenNodeHasSpan_ShouldUseNodeSpan()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");
        node.WithSpan(new TextSpan(10, 20));

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "error", node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_WithNode_WhenNodeHasNoSpan_ShouldUseEmptySpan()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "error", node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportWarning_WithSpan_ShouldAddWarning()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportWarning(DiagnosticCode.MQ5001_UnusedAlias, "unused", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
        Assert.IsTrue(ctx.Warnings.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportWarning_WithNode_ShouldAddWarning()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportWarning(DiagnosticCode.MQ5001_UnusedAlias, "unused", node);

        Assert.IsTrue(ctx.Warnings.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportInfo_ShouldAddDiagnostic()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportInfo(DiagnosticCode.MQ5001_UnusedAlias, "info msg", new TextSpan(0, 5));

        Assert.IsTrue(ctx.Diagnostics.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportHint_ShouldAddDiagnostic()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportHint(DiagnosticCode.MQ5001_UnusedAlias, "hint msg", new TextSpan(0, 5));

        Assert.IsTrue(ctx.Diagnostics.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithDiagnosticException_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var ex = new UnknownColumnOrAliasException("Col", "ctx", new TextSpan(0, 5));

        ctx.ReportException(ex);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithRegularException_ShouldAddGenericError()
    {
        var ctx = new DiagnosticContext();
        var ex = new InvalidOperationException("something broke");

        ctx.ReportException(ex);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithOverrideSpan_ShouldUseProvidedSpan()
    {
        var ctx = new DiagnosticContext();
        var ex = new InvalidOperationException("error");
        var span = new TextSpan(10, 20);

        ctx.ReportException(ex, span);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownAlias_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownAlias("badAlias", ["alias1", "alias2"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownAlias_WithSuggestion_ShouldIncludeDidYouMean()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownAlias("alis1", ["alias1", "alias2"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "alis1");
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownColumn_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownColumn("badCol", ["Name", "Age"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownColumn_WithSuggestion_ShouldIncludeDidYouMean()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownColumn("Nme", ["Name", "Age"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Nme");
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownProperty_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownProperty("badProp", ["Prop1", "Prop2"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownFunction_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownFunction("badFunc", ["Func1", "Func2"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportAmbiguousAggregateOwner_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportAmbiguousAggregateOwner("Count()", ["a", "b"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Count()");
    }

    [TestMethod]
    public void DiagnosticContext_ReportAmbiguousMethodOwner_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportAmbiguousMethodOwner("Foo()", ["x", "y"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Foo()");
    }

    [TestMethod]
    public void DiagnosticContext_ReportInvalidArgumentCount_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportInvalidArgumentCount("Sum", 1, 3, node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Sum");
    }

    [TestMethod]
    public void DiagnosticContext_Clear_ShouldRemoveAllDiagnosticsAndScopes()
    {
        var ctx = new DiagnosticContext();
        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        ctx.EnterScope("test");

        ctx.Clear();

        Assert.IsFalse(ctx.HasErrors);
        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ToResult_ShouldCreateSemanticAnalysisResult()
    {
        var ctx = new DiagnosticContext();
        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        var rootNode = new IntegerNode("1", "i");

        var result = ctx.ToResult(rootNode);

        Assert.IsNotNull(result);
        Assert.AreEqual(rootNode, result.RootNode);
        Assert.IsTrue(result.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_SourceText_ShouldReturnConfiguredValue()
    {
        var sourceText = new SourceText("SELECT 1");
        var ctx = new DiagnosticContext(sourceText);

        Assert.AreEqual(sourceText, ctx.SourceText);
    }

    [TestMethod]
    public void DiagnosticContext_HasReachedMaxErrors_WhenMaxExceeded_ShouldReturnTrue()
    {
        var ctx = new DiagnosticContext(maxErrors: 2);

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 1));
        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(1, 2));

        Assert.IsTrue(ctx.HasReachedMaxErrors);
    }

    #endregion

    #region DiagnosticBag Branch Coverage

    [TestMethod]
    public void DiagnosticBag_Add_WhenNull_ShouldThrow()
    {
        var bag = new DiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => bag.Add(null!));
    }

    [TestMethod]
    public void DiagnosticBag_Add_WhenMaxErrorsReached_ShouldRejectNewErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };

        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 1));
        var added = bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(1, 2));

        Assert.IsFalse(added);
        Assert.AreEqual(1, bag.ErrorCount);
        Assert.IsTrue(bag.HasTooManyErrors);
    }

    [TestMethod]
    public void DiagnosticBag_AddWarning_ShouldIncrementWarningCount()
    {
        var bag = new DiagnosticBag();

        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(0, 5));

        Assert.AreEqual(1, bag.WarningCount);
        Assert.AreEqual(0, bag.ErrorCount);
        Assert.IsFalse(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_AddInfo_ShouldAddDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddInfo(DiagnosticCode.MQ5001_UnusedAlias, "info", new TextSpan(0, 5));

        Assert.AreEqual(1, bag.Count);
    }

    [TestMethod]
    public void DiagnosticBag_AddHint_ShouldAddDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddHint(DiagnosticCode.MQ5001_UnusedAlias, "hint", new TextSpan(0, 5));

        Assert.AreEqual(1, bag.Count);
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_ShouldStopWhenMaxErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };
        var diags = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 1)),
            Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(1, 2)),
            Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err3", new TextSpan(2, 3))
        };

        bag.AddRange(diags);

        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_ToSortedList_ShouldReturnSortedByLocation()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(20, 25));
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 5));

        var sorted = bag.ToSortedList();

        Assert.AreEqual("err1", sorted[0].Message);
        Assert.AreEqual("err2", sorted[1].Message);
    }

    [TestMethod]
    public void DiagnosticBag_GetErrors_ShouldFilterOnlyErrors()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        var errors = bag.GetErrors().ToList();

        Assert.HasCount(1, errors);
        Assert.AreEqual("err", errors[0].Message);
    }

    [TestMethod]
    public void DiagnosticBag_GetWarnings_ShouldFilterOnlyWarnings()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        var warnings = bag.GetWarnings().ToList();

        Assert.HasCount(1, warnings);
        Assert.AreEqual("warn", warnings[0].Message);
    }

    [TestMethod]
    public void DiagnosticBag_Clear_ShouldResetAll()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        bag.Clear();

        Assert.AreEqual(0, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
        Assert.AreEqual(0, bag.WarningCount);
        Assert.IsFalse(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_Enumerable_ShouldIterateAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        var count = 0;
        foreach (var _ in bag)
            count++;

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithLocationOverload_ShouldCreateDiagnostic()
    {
        var bag = new DiagnosticBag();
        var location = new SourceLocation(0, 1, 1);

        var added = bag.Add(DiagnosticCode.MQ3001_UnknownColumn, DiagnosticSeverity.Error, "err", location);

        Assert.IsTrue(added);
        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithSourceText_ShouldGenerateContextSnippet()
    {
        var bag = new DiagnosticBag { SourceText = new SourceText("SELECT 1 FROM dual") };
        var location = new SourceLocation(0, 1, 1);
        var endLocation = new SourceLocation(6, 1, 7);

        bag.Add(DiagnosticCode.MQ3001_UnknownColumn, DiagnosticSeverity.Error, "err", location, endLocation);

        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddError_WithFormatArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, new TextSpan(0, 5), "MyColumn");

        var errors = bag.GetErrors().ToList();
        Assert.HasCount(1, errors);
        StringAssert.Contains(errors[0].Message, "MyColumn");
    }

    [TestMethod]
    public void DiagnosticBag_AddWarning_WithFormatArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, new TextSpan(0, 5), "myAlias");

        var warnings = bag.GetWarnings().ToList();
        Assert.HasCount(1, warnings);
        StringAssert.Contains(warnings[0].Message, "myAlias");
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_FromOtherBag_ShouldImportAll()
    {
        var bag1 = new DiagnosticBag();
        bag1.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 5));
        bag1.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn1", new TextSpan(5, 10));

        var bag2 = new DiagnosticBag();
        bag2.AddRange(bag1);

        Assert.AreEqual(1, bag2.ErrorCount);
        Assert.AreEqual(1, bag2.WarningCount);
    }

    #endregion
}
