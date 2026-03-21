#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Recovery;

namespace Musoq.Parser.Tests;


[TestClass]
public class Diagnostic_MetadataAndPhasesTests
{
    #region DiagnosticPhase and DiagnosticPhaseMapping Tests

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ1xxxRange_ShouldReturnParse()
    {
        Assert.AreEqual(DiagnosticPhase.Parse, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ1001_UnknownToken));
        Assert.AreEqual(DiagnosticPhase.Parse, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ1002_UnterminatedString));
        Assert.AreEqual(DiagnosticPhase.Parse, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ1003_InvalidNumericLiteral));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ2xxxRange_ShouldReturnParse()
    {
        Assert.AreEqual(DiagnosticPhase.Parse, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ2001_UnexpectedToken));
        Assert.AreEqual(DiagnosticPhase.Parse, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ2004_MissingFromClause));
        Assert.AreEqual(DiagnosticPhase.Parse, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ2026_InvalidCaseExpression));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ3xxxRange_ShouldReturnBind()
    {
        Assert.AreEqual(DiagnosticPhase.Bind, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(DiagnosticPhase.Bind, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ3004_UnknownFunction));
        Assert.AreEqual(DiagnosticPhase.Bind, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ3022_MissingAlias));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ4xxxRange_ShouldReturnDataSource()
    {
        Assert.AreEqual(DiagnosticPhase.DataSource, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ4001_InvalidBinarySchemaField));
        Assert.AreEqual(DiagnosticPhase.DataSource, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ4003_UndefinedSchemaReference));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ5xxxRange_ShouldReturnBind()
    {
        Assert.AreEqual(DiagnosticPhase.Bind, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ5001_UnusedAlias));
        Assert.AreEqual(DiagnosticPhase.Bind, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ5003_ImplicitTypeConversion));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ6xxxRange_ShouldReturnFeatureGate()
    {
        Assert.AreEqual(DiagnosticPhase.FeatureGate, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ6001_CteUnavailable));
        Assert.AreEqual(DiagnosticPhase.FeatureGate, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ6003_SimpleCaseNotSupported));
        Assert.AreEqual(DiagnosticPhase.FeatureGate, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ6004_CoalesceWithLiteralNull));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeInMQ7xxxRange_ShouldReturnRuntime()
    {
        Assert.AreEqual(DiagnosticPhase.Runtime, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ7001_DataSourceBindingFailed));
        Assert.AreEqual(DiagnosticPhase.Runtime, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ7002_DataSourceIteratorError));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_WhenCodeIsMQ9999_ShouldReturnRuntime()
    {
        Assert.AreEqual(DiagnosticPhase.Runtime, DiagnosticPhaseMapping.FromCode(DiagnosticCode.MQ9999_Unknown));
    }

    [TestMethod]
    public void DiagnosticPhaseMapping_ToDisplayString_ShouldReturnLowercasePhaseNames()
    {
        Assert.AreEqual("parse", DiagnosticPhaseMapping.ToDisplayString(DiagnosticPhase.Parse));
        Assert.AreEqual("bind", DiagnosticPhaseMapping.ToDisplayString(DiagnosticPhase.Bind));
        Assert.AreEqual("runtime", DiagnosticPhaseMapping.ToDisplayString(DiagnosticPhase.Runtime));
        Assert.AreEqual("datasource", DiagnosticPhaseMapping.ToDisplayString(DiagnosticPhase.DataSource));
        Assert.AreEqual("feature-gate", DiagnosticPhaseMapping.ToDisplayString(DiagnosticPhase.FeatureGate));
    }

    [TestMethod]
    public void Diagnostic_Phase_ShouldReturnCorrectPhaseForCode()
    {
        var parseDiag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 1));
        var bindDiag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "test", new TextSpan(0, 1));

        Assert.AreEqual(DiagnosticPhase.Parse, parseDiag.Phase);
        Assert.AreEqual(DiagnosticPhase.Bind, bindDiag.Phase);
    }

    #endregion

    #region Diagnostic Explanation and DocsReference Tests

    [TestMethod]
    public void Diagnostic_WhenCreatedWithExplanation_ShouldPreserveExplanation()
    {
        var location = new SourceLocation(0, 1, 1);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column 'Foo'",
            location,
            explanation: "The column was not found in available sources.",
            docsReference: "Core Spec §Column References");

        Assert.AreEqual("The column was not found in available sources.", diagnostic.Explanation);
        Assert.AreEqual("Core Spec §Column References", diagnostic.DocsReference);
    }

    [TestMethod]
    public void Diagnostic_WithExplanation_ShouldReturnNewDiagnosticWithExplanation()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "Unknown column", new TextSpan(0, 5));

        var withExplanation = diag.WithExplanation("Column not found in any source.");

        Assert.IsNull(diag.Explanation);
        Assert.AreEqual("Column not found in any source.", withExplanation.Explanation);
        Assert.AreEqual(diag.Code, withExplanation.Code);
        Assert.AreEqual(diag.Message, withExplanation.Message);
    }

    [TestMethod]
    public void Diagnostic_WithDocsReference_ShouldReturnNewDiagnosticWithDocsRef()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "Unknown column", new TextSpan(0, 5));

        var withDocs = diag.WithDocsReference("Core Spec §Column References");

        Assert.IsNull(diag.DocsReference);
        Assert.AreEqual("Core Spec §Column References", withDocs.DocsReference);
        Assert.AreEqual(diag.Code, withDocs.Code);
    }

    [TestMethod]
    public void Diagnostic_WithRelatedInfo_ShouldPreserveExplanationAndDocs()
    {
        var location = new SourceLocation(0, 1, 1);
        var diag = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column",
            location,
            explanation: "Explanation text",
            docsReference: "docs-ref");

        var withInfo = diag.WithRelatedInfo("Some related info");

        Assert.AreEqual("Explanation text", withInfo.Explanation);
        Assert.AreEqual("docs-ref", withInfo.DocsReference);
        Assert.HasCount(1, withInfo.RelatedInfo);
    }

    [TestMethod]
    public void Diagnostic_WithSuggestedFix_ShouldPreserveExplanationAndDocs()
    {
        var location = new SourceLocation(0, 1, 1);
        var diag = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column",
            location,
            explanation: "Explanation text",
            docsReference: "docs-ref");

        var withFix = diag.WithSuggestedFix(DiagnosticAction.QuickFix("Fix it", new TextSpan(0, 1), "fixed"));

        Assert.AreEqual("Explanation text", withFix.Explanation);
        Assert.AreEqual("docs-ref", withFix.DocsReference);
        Assert.HasCount(1, withFix.SuggestedFixes);
    }

    #endregion

    #region ErrorMetadata and ErrorMetadataCatalog Tests

    [TestMethod]
    public void ErrorMetadataCatalog_WhenCodeExists_ShouldReturnMetadata()
    {
        var metadata = ErrorMetadataCatalog.Get(DiagnosticCode.MQ3001_UnknownColumn);

        Assert.IsNotNull(metadata);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, metadata!.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, metadata.Phase);
        Assert.IsFalse(string.IsNullOrEmpty(metadata.Explanation));
        Assert.IsNotEmpty(metadata.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrEmpty(metadata.DocsReference));
    }

    [TestMethod]
    public void ErrorMetadataCatalog_WhenCodeDoesNotExist_ShouldReturnNull()
    {
        var metadata = ErrorMetadataCatalog.Get(DiagnosticCode.MQ9999_Unknown);

        Assert.IsNull(metadata);
    }

    [TestMethod]
    public void ErrorMetadataCatalog_Get_WhenCodeExists_ShouldReturnNonNull()
    {
        var metadata = ErrorMetadataCatalog.Get(DiagnosticCode.MQ2001_UnexpectedToken);

        Assert.IsNotNull(metadata);
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, metadata!.Code);
    }

    [TestMethod]
    public void ErrorMetadataCatalog_Get_WhenCodeDoesNotExist_ShouldReturnNull()
    {
        var metadata = ErrorMetadataCatalog.Get(DiagnosticCode.MQ9999_Unknown);

        Assert.IsNull(metadata);
    }

    [TestMethod]
    public void ErrorMetadataCatalog_AllLexerCodes_ShouldHaveParsePhase()
    {
        AssertMetadataPhase(DiagnosticCode.MQ1001_UnknownToken, DiagnosticPhase.Parse);
        AssertMetadataPhase(DiagnosticCode.MQ1002_UnterminatedString, DiagnosticPhase.Parse);
        AssertMetadataPhase(DiagnosticCode.MQ1003_InvalidNumericLiteral, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void ErrorMetadataCatalog_AllFeatureGateCodes_ShouldHaveFeatureGatePhase()
    {
        AssertMetadataPhase(DiagnosticCode.MQ6001_CteUnavailable, DiagnosticPhase.FeatureGate);
        AssertMetadataPhase(DiagnosticCode.MQ6002_DescUnavailable, DiagnosticPhase.FeatureGate);
        AssertMetadataPhase(DiagnosticCode.MQ6003_SimpleCaseNotSupported, DiagnosticPhase.FeatureGate);
        AssertMetadataPhase(DiagnosticCode.MQ6004_CoalesceWithLiteralNull, DiagnosticPhase.FeatureGate);
    }

    [TestMethod]
    public void ErrorMetadataCatalog_AllRuntimeCodes_ShouldHaveRuntimePhase()
    {
        AssertMetadataPhase(DiagnosticCode.MQ7001_DataSourceBindingFailed, DiagnosticPhase.Runtime);
        AssertMetadataPhase(DiagnosticCode.MQ7002_DataSourceIteratorError, DiagnosticPhase.Runtime);
    }

    [TestMethod]
    public void ErrorMetadata_ShouldHaveValueEquality()
    {
        var a = new ErrorMetadata(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticPhase.Bind,
            "Explanation",
            ["Fix 1"],
            "Docs ref");

        var b = new ErrorMetadata(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticPhase.Bind,
            "Explanation",
            ["Fix 1"],
            "Docs ref");

        Assert.AreEqual(a.Code, b.Code);
        Assert.AreEqual(a.Phase, b.Phase);
        Assert.AreEqual(a.Explanation, b.Explanation);
        Assert.AreEqual(a.DocsReference, b.DocsReference);
    }

    #endregion

    private static void AssertMetadataPhase(DiagnosticCode code, DiagnosticPhase expectedPhase)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        Assert.IsNotNull(metadata, $"No metadata found for {code}");
        Assert.AreEqual(expectedPhase, metadata!.Phase, $"Phase mismatch for {code}");
    }

}
