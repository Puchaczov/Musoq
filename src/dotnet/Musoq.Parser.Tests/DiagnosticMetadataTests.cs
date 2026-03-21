using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

/// <summary>
///     Branch coverage tests for Diagnostic, DiagnosticAction, and DiagnosticBag.
/// </summary>
[TestClass]
public class DiagnosticMetadataTests
{
    #region Diagnostic Branch Coverage

    [TestMethod]
    public void Diagnostic_WithRelatedInfo_ShouldAddInfo()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        var withInfo = diag.WithRelatedInfo("extra info");

        Assert.HasCount(1, withInfo.RelatedInfo);
        Assert.AreEqual("extra info", withInfo.RelatedInfo[0]);
    }

    [TestMethod]
    public void Diagnostic_WithSuggestedFix_ShouldAddFix()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));
        var fix = DiagnosticAction.QuickFix("Fix it", new TextSpan(0, 5), "fixed");

        var withFix = diag.WithSuggestedFix(fix);

        Assert.HasCount(1, withFix.SuggestedFixes);
        Assert.AreEqual("Fix it", withFix.SuggestedFixes[0].Title);
    }

    [TestMethod]
    public void Diagnostic_WithExplanation_ShouldAddExplanation()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        var withExplanation = diag.WithExplanation("This happened because...");

        Assert.AreEqual("This happened because...", withExplanation.Explanation);
    }

    [TestMethod]
    public void Diagnostic_WithDocsReference_ShouldAddReference()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        var withDocs = diag.WithDocsReference("docs/errors.md#MQ2001");

        Assert.AreEqual("docs/errors.md#MQ2001", withDocs.DocsReference);
    }

    [TestMethod]
    public void Diagnostic_Info_ShouldHaveInfoSeverity()
    {
        var diag = Diagnostic.Info(DiagnosticCode.MQ5001_UnusedAlias, "info msg", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticSeverity.Info, diag.Severity);
    }

    [TestMethod]
    public void Diagnostic_Hint_ShouldHaveHintSeverity()
    {
        var diag = Diagnostic.Hint(DiagnosticCode.MQ5001_UnusedAlias, "hint msg", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticSeverity.Hint, diag.Severity);
    }

    [TestMethod]
    public void Diagnostic_Phase_ShouldMapFromCode()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticPhase.Parse, diag.Phase);
    }

    [TestMethod]
    public void Diagnostic_Span_ShouldBeDerivedFromLocations()
    {
        var start = new SourceLocation(5, 1, 6);
        var end = new SourceLocation(10, 1, 11);
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", start, end);

        var span = diag.Span;

        Assert.AreEqual(5, span.Start);
        Assert.AreEqual(5, span.Length);
    }

    #endregion

    #region DiagnosticAction Branch Coverage

    [TestMethod]
    public void DiagnosticAction_QuickFix_ShouldCreateQuickFixAction()
    {
        var action = DiagnosticAction.QuickFix("Fix it", new TextSpan(0, 5), "fixed");

        Assert.AreEqual("Fix it", action.Title);
        Assert.AreEqual(DiagnosticActionKind.QuickFix, action.Kind);
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual("fixed", action.TextEdit.NewText);
    }

    [TestMethod]
    public void DiagnosticAction_Refactor_ShouldCreateRefactorAction()
    {
        var action = DiagnosticAction.Refactor("Refactor it", new TextSpan(0, 5), "refactored");

        Assert.AreEqual("Refactor it", action.Title);
        Assert.AreEqual(DiagnosticActionKind.Refactor, action.Kind);
        Assert.IsNotNull(action.TextEdit);
    }

    [TestMethod]
    public void DiagnosticAction_Suggestion_ShouldCreateSuggestionAction()
    {
        var action = DiagnosticAction.Suggestion("Consider this");

        Assert.AreEqual("Consider this", action.Title);
        Assert.AreEqual(DiagnosticActionKind.Suggestion, action.Kind);
        Assert.IsNull(action.TextEdit);
    }

    #endregion

    #region DiagnosticBag Branch Coverage

    [TestMethod]
    public void DiagnosticBag_Add_NullDiagnostic_ShouldThrow()
    {
        var bag = new DiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => bag.Add(null));
    }

    [TestMethod]
    public void DiagnosticBag_Add_WhenMaxErrorsReached_ShouldRejectErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 2 };

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error 1", new TextSpan(0, 1));
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error 2", new TextSpan(1, 1));
        var added = bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error 3", new TextSpan(2, 1));

        Assert.IsFalse(added);
        Assert.IsTrue(bag.HasTooManyErrors);
        Assert.AreEqual(2, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_Add_WarningsNotLimited_ShouldAlwaysAdd()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        Assert.AreEqual(1, bag.ErrorCount);
        Assert.AreEqual(1, bag.WarningCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithSourceText_ShouldIncludeContextSnippet()
    {
        var bag = new DiagnosticBag
        {
            SourceText = new SourceText("SELECT * FROM table")
        };

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 6));

        var diagnostic = bag.First();
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithoutSourceText_ShouldUseFallbackLocations()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(5, 3));

        var diagnostic = bag.First();
        Assert.AreEqual(5, diagnostic.Location.Offset);
    }

    [TestMethod]
    public void DiagnosticBag_AddInfo_ShouldAddInfoDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddInfo(DiagnosticCode.MQ2001_UnexpectedToken, "info message", new TextSpan(0, 1));

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddHint_ShouldAddHintDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddHint(DiagnosticCode.MQ2001_UnexpectedToken, "hint message", new TextSpan(0, 1));

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddErrorWithFormattedArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, new TextSpan(0, 1), "SELECT");

        Assert.IsTrue(bag.HasErrors);
        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddWarningWithFormattedArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, new TextSpan(0, 1), "myAlias");

        Assert.AreEqual(1, bag.WarningCount);
    }

    [TestMethod]
    public void DiagnosticBag_GetErrors_ShouldReturnOnlyErrors()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        var errors = bag.GetErrors().ToArray();

        Assert.HasCount(1, errors);
        Assert.IsTrue(errors[0].IsError);
    }

    [TestMethod]
    public void DiagnosticBag_GetWarnings_ShouldReturnOnlyWarnings()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        var warnings = bag.GetWarnings().ToArray();

        Assert.HasCount(1, warnings);
        Assert.IsTrue(warnings[0].IsWarning);
    }

    [TestMethod]
    public void DiagnosticBag_ToSortedList_ShouldSortByLocationThenSeverity()
    {
        var bag = new DiagnosticBag();
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning at 10", new TextSpan(10, 1));
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error at 0", new TextSpan(0, 1));

        var sorted = bag.ToSortedList();

        Assert.AreEqual(0, sorted[0].Location.Offset);
    }

    [TestMethod]
    public void DiagnosticBag_Clear_ShouldRemoveAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        bag.Clear();

        Assert.AreEqual(0, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
        Assert.AreEqual(0, bag.WarningCount);
        Assert.IsFalse(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_ShouldStopWhenMaxErrorsReached()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error 1", new TextSpan(0, 1)),
            Diagnostic.Error(DiagnosticCode.MQ2002_MissingToken, "error 2", new TextSpan(1, 1)),
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error 3", new TextSpan(2, 1))
        };

        bag.AddRange(diagnostics);

        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_FromOtherBag_ShouldMerge()
    {
        var bag1 = new DiagnosticBag();
        bag1.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error from bag1", new TextSpan(0, 1));

        var bag2 = new DiagnosticBag();
        bag2.AddRange(bag1);

        Assert.AreEqual(1, bag2.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_Enumerate_ShouldReturnAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        var count = 0;
        foreach (var _ in bag)
            count++;

        Assert.AreEqual(2, count);
    }

    #endregion

    #region Diagnostic Additional Branch Coverage

    [TestMethod]
    public void Diagnostic_IsError_ShouldReturnTrueForErrors()
    {
        var error = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5));

        Assert.IsTrue(error.IsError);
        Assert.IsFalse(error.IsWarning);
    }

    [TestMethod]
    public void Diagnostic_IsWarning_ShouldReturnTrueForWarnings()
    {
        var warning = Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(0, 5));

        Assert.IsTrue(warning.IsWarning);
        Assert.IsFalse(warning.IsError);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_Basic_ShouldFormatCorrectly()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test error", new TextSpan(0, 5));

        var detailed = diag.ToDetailedString();

        Assert.Contains("error", detailed);
        Assert.Contains("test error", detailed);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_WithContextSnippet_ShouldIncludeSnippet()
    {
        var diag = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "test error",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6),
            "SELECT bad syntax");

        var detailed = diag.ToDetailedString();

        Assert.Contains("SELECT bad syntax", detailed);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_WithRelatedInfo_ShouldIncludeNotes()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5))
            .WithRelatedInfo("related note");

        var detailed = diag.ToDetailedString();

        Assert.Contains("related note", detailed);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_WithSuggestedFix_ShouldIncludeHelp()
    {
        var fix = DiagnosticAction.QuickFix("Fix it", new TextSpan(0, 5), "fixed");
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5))
            .WithSuggestedFix(fix);

        var detailed = diag.ToDetailedString();

        Assert.Contains("Fix it", detailed);
    }

    [TestMethod]
    public void Diagnostic_ToString_ShouldFormatCorrectly()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test error", new TextSpan(0, 5));

        var str = diag.ToString();

        Assert.Contains("error", str);
        Assert.Contains("test error", str);
    }

    [TestMethod]
    public void Diagnostic_Info_FactoryMethod_ShouldCreateInfoDiagnostic()
    {
        var diag = Diagnostic.Info(DiagnosticCode.MQ5001_UnusedAlias, "info message", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticSeverity.Info, diag.Severity);
        Assert.AreEqual("info message", diag.Message);
    }

    [TestMethod]
    public void Diagnostic_Error_WithSourceLocation_ShouldUseLocations()
    {
        var start = new SourceLocation(5, 2, 3);
        var end = new SourceLocation(10, 2, 8);

        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", start, end);

        Assert.AreEqual(5, diag.Location.Offset);
        Assert.AreEqual(10, diag.EndLocation.Offset);
    }

    [TestMethod]
    public void Diagnostic_Warning_WithSourceLocation_ShouldUseLocations()
    {
        var start = new SourceLocation(5, 2, 3);
        var end = new SourceLocation(10, 2, 8);

        var diag = Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", start, end);

        Assert.AreEqual(DiagnosticSeverity.Warning, diag.Severity);
        Assert.AreEqual(5, diag.Location.Offset);
    }

    [TestMethod]
    public void Diagnostic_Info_WithSourceLocation_ShouldUseLocations()
    {
        var start = new SourceLocation(5, 2, 3);
        var end = new SourceLocation(10, 2, 8);

        var diag = Diagnostic.Info(DiagnosticCode.MQ5001_UnusedAlias, "info", start, end);

        Assert.AreEqual(DiagnosticSeverity.Info, diag.Severity);
        Assert.AreEqual(5, diag.Location.Offset);
    }

    #endregion
}
