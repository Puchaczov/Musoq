using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Wildcard;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    public void ExcludeUnknownColumn_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * exclude (Missing) from #wildcard.rows() a",
            DiagnosticCode.MQ3041_StarExcludeColumnNotFound,
            "Missing");
    }

    [TestMethod]
    public void LikeWithNoMatches_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * like 'Z%' from #wildcard.rows() a",
            DiagnosticCode.MQ3045_StarLikeMatchedNoColumns,
            "Z%");
    }

    [TestMethod]
    public void DuplicateExcludeColumn_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * exclude (Name, name) from #wildcard.rows() a",
            DiagnosticCode.MQ3046_StarExcludeDuplicateColumn,
            "name");
    }

    [TestMethod]
    public void DuplicateReplaceColumn_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * replace (1 as Name, 2 as Name) from #wildcard.rows() a",
            DiagnosticCode.MQ3047_StarReplaceDuplicateColumn,
            "Name");
    }

    [TestMethod]
    public void ReplaceRemovedColumn_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * exclude (Other) replace (1 as Other) from #wildcard.rows() a",
            DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace,
            "Other");
    }

    [TestMethod]
    public void RenameDuplicateTarget_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * rename (Name as Label, Other as Label) from #wildcard.rows() a",
            DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
            "Label");
    }

    [TestMethod]
    public void RenameUnknownColumn_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * rename (Missing as DisplayName) from #wildcard.rows() a",
            DiagnosticCode.MQ3070_StarRenameColumnNotFound,
            "Missing");
    }

    [TestMethod]
    public void ModifierOutOfOrder_ShouldReportTheExistingDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * replace (1 as Name) like 'N%' from #wildcard.rows() a",
            DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "Duplicate or out-of-order star modifier",
            DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void ExcludeAllDynamicColumns_ShouldReportTheExistingDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * exclude (Id, Name, Other, Score) from #wildcard.rows() a",
            DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns,
            null);
    }

    [TestMethod]
    public void ReplaceUnknownColumn_ShouldReportDynamicSchemaDiagnostic()
    {
        AssertDynamicDiagnostic(
            "select * replace (1 as Missing) from #wildcard.rows() a",
            DiagnosticCode.MQ3042_StarReplaceColumnNotFound,
            "Missing");
    }

    private static void AssertDynamicDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string? expectedMessage,
        DiagnosticPhase expectedPhase = DiagnosticPhase.Bind)
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var exception = Assert.Throws<MusoqQueryException>(() => Compile(query, recorder).Run());

        if (expectedMessage is null)
            AssertErrorEnvelope(exception, expectedCode, expectedPhase);
        else
            AssertErrorEnvelope(exception, expectedCode, expectedPhase, expectedMessage);
    }
}
