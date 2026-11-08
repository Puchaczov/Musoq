using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region ErrorCatalog Branch Coverage

    [TestMethod]
    public void ErrorCatalog_GetTemplate_WhenCodeExists_ShouldReturnTemplate()
    {
        var template = ErrorCatalog.GetTemplate(DiagnosticCode.MQ3001_UnknownColumn);

        StringAssert.Contains(template, "{0}");
    }

    [TestMethod]
    public void ErrorCatalog_GetTemplate_WhenCodeDoesNotExist_ShouldReturnFallback()
    {
        var template = ErrorCatalog.GetTemplate((DiagnosticCode)99999);

        StringAssert.Contains(template, "99999");
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_WithArgs_ShouldFormatMessage()
    {
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ3001_UnknownColumn, "MyCol");

        StringAssert.Contains(message, "MyCol");
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_WithNoArgs_ShouldReturnTemplate()
    {
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ1002_UnterminatedString);

        StringAssert.Contains(message, "Unterminated");
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_WithBadFormatArgs_ShouldReturnTemplate()
    {
        // MQ3002 expects 3 args ({0}, {1}, {2}), passing wrong number should fallback
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ3002_AmbiguousColumn);

        Assert.IsNotNull(message);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForWarning_ShouldReturnWarning()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ5003_ImplicitTypeConversion);

        Assert.AreEqual(DiagnosticSeverity.Warning, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForLexerError_ShouldReturnError()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ1001_UnknownToken);

        Assert.AreEqual(DiagnosticSeverity.Error, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForSemanticError_ShouldReturnError()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ3001_UnknownColumn);

        Assert.AreEqual(DiagnosticSeverity.Error, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForRuntimeError_ShouldReturnError()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ7010_DataSourceOpenFailed);

        Assert.AreEqual(DiagnosticSeverity.Error, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetCategory_ShouldReturnCorrectCategory()
    {
        Assert.AreEqual("Lexer", ErrorCatalog.GetCategory(DiagnosticCode.MQ1001_UnknownToken));
        Assert.AreEqual("Syntax", ErrorCatalog.GetCategory(DiagnosticCode.MQ2001_UnexpectedToken));
        Assert.AreEqual("Semantic", ErrorCatalog.GetCategory(DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual("Schema", ErrorCatalog.GetCategory(DiagnosticCode.MQ4001_InvalidBinarySchemaField));
        Assert.AreEqual("Warning", ErrorCatalog.GetCategory(DiagnosticCode.MQ5003_ImplicitTypeConversion));
        Assert.AreEqual("Runtime", ErrorCatalog.GetCategory(DiagnosticCode.MQ7010_DataSourceOpenFailed));
        Assert.AreEqual("CodeGeneration", ErrorCatalog.GetCategory(DiagnosticCode.MQ8001_CodeGenerationFailed));
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_WhenCloseMatch_ShouldSuggest()
    {
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("Nme", ["Name", "Age", "City"]);

        Assert.AreEqual("Name", suggestion);
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_WhenNoCloseMatch_ShouldReturnNull()
    {
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("zzzzzzz", ["Name", "Age", "City"]);

        Assert.IsNull(suggestion);
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_WithEmptyCandidates_ShouldReturnNull()
    {
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("Name", Array.Empty<string>());

        Assert.IsNull(suggestion);
    }

    #endregion

    #region DiagnosticExceptionExtensions Branch Coverage

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithDiagnosticException_ShouldReturnTrue()
    {
        Exception ex = new UnknownColumnOrAliasException("col");

        var result = ex.TryToDiagnostic(null, out var diagnostic);

        Assert.IsTrue(result);
        Assert.IsNotNull(diagnostic);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithRegularException_ShouldReturnFalse()
    {
        var ex = new InvalidOperationException("test");

        var result = ex.TryToDiagnostic(null, out var diagnostic);

        Assert.IsFalse(result);
        Assert.IsNull(diagnostic);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithWrappedDiagnosticException_ShouldReturnTrue()
    {
        var inner = new UnknownPropertyException("Age", "Person", new TextSpan(0, 5));
        Exception ex = new InvalidOperationException("wrapper", inner);

        var result = ex.TryToDiagnostic(null, out var diagnostic);

        Assert.IsTrue(result);
        var typedDiagnostic = diagnostic ?? throw new AssertFailedException("Expected a diagnostic.");
        Assert.AreEqual(DiagnosticCode.MQ3028_UnknownProperty, typedDiagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithDiagnosticException_ShouldReturnTyped()
    {
        Exception ex = new AmbiguousColumnException("Col", "a", "b");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithRegularException_ShouldReturnGeneric()
    {
        var ex = new InvalidOperationException("something went wrong");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithArgumentNullException_ShouldReturnInternal()
    {
        var ex = new ArgumentNullException("param");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithKeyNotFoundException_ShouldReturnInternal()
    {
        var ex = new KeyNotFoundException("test");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithNotSupportedException_ShouldReturnInternal()
    {
        var ex = new NotSupportedException("not supported");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((Exception)null!).TryToDiagnostic(null, out _));
    }

    #endregion
}
