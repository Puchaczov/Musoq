#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Recovery;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for the diagnostic infrastructure classes.
/// </summary>
[TestClass]
public class DiagnosticTests
{
    #region Core Diagnostic Types Tests

    [TestMethod]
    public void SourceLocation_ShouldHaveCorrectProperties()
    {
        var location = new SourceLocation(10, 2, 5);

        Assert.AreEqual(10, location.Offset);
        Assert.AreEqual(2, location.Line);
        Assert.AreEqual(5, location.Column);
        Assert.AreEqual(1, location.Line0);
        Assert.AreEqual(4, location.Column0);
        Assert.IsTrue(location.IsValid);
    }

    [TestMethod]
    public void SourceLocation_None_ShouldHaveNegativeOffset()
    {
        var invalid = SourceLocation.None;

        Assert.IsFalse(invalid.IsValid);
        Assert.AreEqual(-1, invalid.Offset);
    }

    [TestMethod]
    public void SourceLocation_Comparison_ShouldWorkCorrectly()
    {
        var loc1 = new SourceLocation(10, 2, 5);
        var loc2 = new SourceLocation(20, 3, 1);
        var loc3 = new SourceLocation(10, 2, 5);

        Assert.IsLessThan(0, loc1.CompareTo(loc2));
        Assert.IsGreaterThan(0, loc2.CompareTo(loc1));
        Assert.AreEqual(0, loc1.CompareTo(loc3));
        Assert.AreEqual(loc1, loc3);
    }

    [TestMethod]
    public void SourceText_ShouldCalculateLineAndColumn()
    {
        var source = "SELECT\nName,\nAge\nFROM table";
        var sourceText = new SourceText(source, "test.sql");


        var loc1 = sourceText.GetLocation(7);
        Assert.AreEqual(7, loc1.Offset);
        Assert.AreEqual(2, loc1.Line);
        Assert.AreEqual(1, loc1.Column);


        var loc2 = sourceText.GetLocation(13);
        Assert.AreEqual(13, loc2.Offset);
        Assert.AreEqual(3, loc2.Line);
        Assert.AreEqual(1, loc2.Column);
    }

    [TestMethod]
    public void SourceText_GetLineText_ShouldReturnCorrectLine()
    {
        var source = "SELECT\nName,\nAge\nFROM table";
        var sourceText = new SourceText(source);

        Assert.AreEqual("SELECT", sourceText.GetLineText(1));
        Assert.AreEqual("Name,", sourceText.GetLineText(2));
        Assert.AreEqual("Age", sourceText.GetLineText(3));
        Assert.AreEqual("FROM table", sourceText.GetLineText(4));
    }

    [TestMethod]
    public void Diagnostic_Error_ShouldHaveCorrectSeverity()
    {
        var span = new TextSpan(10, 5);
        var diagnostic = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "Unexpected token", span);

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.IsTrue(diagnostic.IsError);
        Assert.IsFalse(diagnostic.IsWarning);
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual("MQ2001", diagnostic.CodeString);
    }

    [TestMethod]
    public void Diagnostic_Warning_ShouldHaveCorrectSeverity()
    {
        var span = new TextSpan(10, 5);
        var diagnostic = Diagnostic.Warning(DiagnosticCode.MQ5002_SelectStar, "Consider using explicit columns", span);

        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.IsFalse(diagnostic.IsError);
        Assert.IsTrue(diagnostic.IsWarning);
    }

    [TestMethod]
    public void DiagnosticBag_ShouldCollectDiagnostics()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "Error 1", new TextSpan(0, 5));
        bag.AddError(DiagnosticCode.MQ2002_MissingToken, "Error 2", new TextSpan(10, 3));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "Warning 1", new TextSpan(20, 4));

        Assert.AreEqual(3, bag.Count);
        Assert.AreEqual(2, bag.ErrorCount);
        Assert.AreEqual(1, bag.WarningCount);
        Assert.IsTrue(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_ShouldRespectMaxErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 3 };

        for (var i = 0; i < 5; i++)
            bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, $"Error {i}", new TextSpan(i * 10, 5));


        Assert.IsTrue(bag.HasTooManyErrors);

        Assert.IsGreaterThanOrEqualTo(bag.MaxErrors, bag.ErrorCount);
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_ShouldReturnFormattedMessage()
    {
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ2001_UnexpectedToken, "SELECT");

        Assert.IsNotNull(message);
        Assert.IsTrue(message.Contains("SELECT") || message.Contains("Unexpected"));
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_ShouldSuggestSimilarWord()
    {
        var candidates = new[] { "Name", "Age", "Address" };

        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("Nme", candidates);

        Assert.IsNotNull(suggestion);
        Assert.Contains("Name", suggestion);
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_ShouldReturnNullForNoMatch()
    {
        var candidates = new[] { "Name", "Age", "Address" };

        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("XYZ123", candidates);

        Assert.IsNull(suggestion);
    }

    [TestMethod]
    public void TextSpan_Contains_ShouldWorkCorrectly()
    {
        var span = new TextSpan(10, 20);

        Assert.IsTrue(span.Contains(10));
        Assert.IsTrue(span.Contains(15));
        Assert.IsTrue(span.Contains(29));
        Assert.IsFalse(span.Contains(9));
        Assert.IsFalse(span.Contains(30));
    }

    [TestMethod]
    public void TextSpan_Overlaps_ShouldWorkCorrectly()
    {
        var span1 = new TextSpan(10, 20);
        var span2 = new TextSpan(20, 10);
        var span3 = new TextSpan(30, 10);
        var span4 = new TextSpan(5, 10);

        Assert.IsTrue(span1.Overlaps(span2));
        Assert.IsFalse(span1.Overlaps(span3));
        Assert.IsTrue(span1.Overlaps(span4));
    }

    [TestMethod]
    public void TextSpan_Through_ShouldCreateSpanningBothSpans()
    {
        var span1 = new TextSpan(10, 5);
        var span2 = new TextSpan(25, 5);

        var combined = span1.Through(span2);

        Assert.AreEqual(10, combined.Start);
        Assert.AreEqual(30, combined.End);
        Assert.AreEqual(20, combined.Length);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_ShouldProduceReadableOutput()
    {
        var source = "SELECT FROM table";
        var sourceText = new SourceText(source);
        var bag = new DiagnosticBag { SourceText = sourceText };
        bag.AddError(DiagnosticCode.MQ2005_InvalidSelectList, "Missing column list after SELECT", new TextSpan(7, 4));

        var formatter = new DiagnosticFormatter { UseColor = false };
        var diagnostic = bag.ToSortedList().First();
        var output = formatter.Format(diagnostic);

        Assert.IsNotNull(output);
        Assert.Contains("MQ2005", output);
        Assert.Contains("Missing column list", output);
    }

    [TestMethod]
    public void DiagnosticFormatter_FormatAsJson_ShouldProduceLspCompatibleOutput()
    {
        var span = new TextSpan(10, 5);
        var diagnostic = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "Unexpected token", span);

        var formatter = new DiagnosticFormatter();
        var json = formatter.FormatAsJson(diagnostic);

        Assert.IsNotNull(json);
        Assert.Contains("\"range\"", json);
        Assert.Contains("\"severity\"", json);
        Assert.Contains("\"code\"", json);
        Assert.Contains("\"message\"", json);
    }

    #endregion

    #region Exception Enhancement Tests

    [TestMethod]
    public void SyntaxException_UnexpectedToken_ShouldImplementIDiagnosticException()
    {
        var span = new TextSpan(0, 5);
        var exception = SyntaxException.UnexpectedToken("foo", "bar", "SELECT foo FROM test", span);

        Assert.IsInstanceOfType(exception, typeof(IDiagnosticException));
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, exception.Code);
        Assert.AreEqual(span, exception.Span);
    }

    [TestMethod]
    public void SyntaxException_MissingToken_ShouldCreateCorrectDiagnostic()
    {
        var span = new TextSpan(10, 0);
        var exception = SyntaxException.MissingToken("FROM", "SELECT * table", span);

        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticCode.MQ2002_MissingToken, diagnostic.Code);
        Assert.Contains("FROM", diagnostic.Message);
    }

    [TestMethod]
    public void SyntaxException_UnclosedString_ShouldHaveCorrectCode()
    {
        var span = new TextSpan(5, 10);
        var exception = SyntaxException.UnclosedString("SELECT 'unclosed", span);

        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, exception.Code);
    }

    [TestMethod]
    public void SyntaxException_UnclosedBracket_ShouldHaveCorrectCode()
    {
        var span = new TextSpan(5, 10);
        var exception = SyntaxException.UnclosedBracket("(", "SELECT (a + b", span);

        Assert.AreEqual(DiagnosticCode.MQ2010_MissingClosingParenthesis, exception.Code);
        Assert.Contains("(", exception.Message);
    }

    [TestMethod]
    public void ParserValidationException_ForNullInput_ShouldHaveCorrectCode()
    {
        var exception = ParserValidationException.ForNullInput();

        Assert.AreEqual(DiagnosticCode.MQ2016_IncompleteStatement, exception.Code);
    }

    [TestMethod]
    public void IDiagnosticException_TryToDiagnostic_ShouldReturnDiagnostic()
    {
        var span = new TextSpan(0, 5);
        var exception = SyntaxException.InvalidExpression("test", "test query", span);
        SourceText? sourceText = null;

        var result = exception.TryToDiagnostic(sourceText, out var diagnostic);

        Assert.IsTrue(result);
        Assert.IsNotNull(diagnostic);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic!.Severity);
    }

    [TestMethod]
    public void IDiagnosticException_TryToDiagnostic_ShouldUnwrapInnerDiagnosticException()
    {
        var span = new TextSpan(2, 3);
        var innerException = SyntaxException.InvalidExpression("nested", "SELECT", span);
        var wrapperException = new InvalidOperationException("wrapper", innerException);

        var result = wrapperException.TryToDiagnostic(null, out var diagnostic);

        Assert.IsTrue(result);
        Assert.IsNotNull(diagnostic);
        Assert.AreEqual(DiagnosticCode.MQ2003_InvalidExpression, diagnostic!.Code);
        Assert.AreEqual(innerException.Message, diagnostic.Message);
    }

    [TestMethod]
    public void IDiagnosticException_ToDiagnosticOrGeneric_ShouldFallbackForNonDiagnosticException()
    {
        var regularException = new Exception("Test error");

        var diagnostic = regularException.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ9999_Unknown, diagnostic.Code);
        Assert.Contains("Test error", diagnostic.Message);
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_ShouldUnwrapInnerDiagnosticException()
    {
        var span = new TextSpan(1, 4);
        var innerException = SyntaxException.InvalidExpression("inner", "SELECT", span);
        var wrapperException = new Exception("outer", innerException);

        var diagnostic = wrapperException.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2003_InvalidExpression, diagnostic.Code);
        Assert.AreEqual(innerException.Message, diagnostic.Message);
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_KeyNotFoundException_ShouldProvideUserFriendlyMessage()
    {
        var exception = new KeyNotFoundException("The given key 'testAlias123' was not present in the dictionary.");

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ3003_UnknownTable, diagnostic.Code);
        Assert.Contains("testAlias123", diagnostic.Message, "Should mention the key");
        Assert.Contains("could not be resolved", diagnostic.Message, "Should explain the issue");
        Assert.DoesNotContain("was not present in the dictionary",
            diagnostic.Message, "Should not show raw .NET message");
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_NullReferenceException_ShouldProvideUserFriendlyMessage()
    {
        var exception = new NullReferenceException();

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
        Assert.Contains("null reference", diagnostic.Message, "Should explain the issue");
        Assert.Contains("query", diagnostic.Message, "Should provide context");
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_StackEmptyInvalidOperation_ShouldMapToUnsupportedSyntax()
    {
        var exception = new InvalidOperationException("Stack empty.");

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
        Assert.Contains("Stack empty", diagnostic.Message);
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_UnterminatedStringMessage_ShouldMapToUnterminatedString()
    {
        var exception =
            new Exception("Token ''' was unrecognized. Rest of the unparsed query is 'Unterminated string literal'");

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, diagnostic.Code);
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_KeyNotFound_ShouldMapToUnknownTable()
    {
        var exception = new KeyNotFoundException("The given key 'First' was not present in the dictionary.");

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ3003_UnknownTable, diagnostic.Code);
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_DuplicateKeyArgument_ShouldMapToDuplicateAlias()
    {
        var exception = new ArgumentException("An item with the same key has already been added. Key: MyData");

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, diagnostic.Code);
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_ArgumentNullException_ShouldIncludeParameterName()
    {
        var exception = new ArgumentNullException("queryText");

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
        Assert.Contains("queryText", diagnostic.Message, "Should mention the parameter name");
    }

    [TestMethod]
    public void ToDiagnosticOrGeneric_IndexOutOfRangeException_ShouldProvideContext()
    {
        var exception = new IndexOutOfRangeException();

        var diagnostic = exception.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
        Assert.Contains("index", diagnostic.Message, "Should mention index issue");
        Assert.Contains("range", diagnostic.Message, "Should mention range issue");
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [TestMethod]
    public void DiagnosticBag_MaxErrors_ShouldStopCollectingAfterLimit()
    {
        var bag = new DiagnosticBag { MaxErrors = 3 };

        for (var i = 0; i < 10; i++)
            bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, $"Error {i}", new TextSpan(i, 1));

        Assert.IsTrue(bag.HasTooManyErrors);
        Assert.AreEqual(3, bag.GetErrors().Count());
    }

    [TestMethod]
    public void DiagnosticBag_Clear_ShouldRemoveAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "Error 1", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "Warning 1", new TextSpan(5, 1));

        bag.Clear();

        Assert.IsFalse(bag.HasErrors);
        Assert.IsEmpty(bag.ToSortedList());
    }

    [TestMethod]
    public void SourceText_EmptyText_ShouldReturnEmptyLines()
    {
        var sourceText = new SourceText("");

        Assert.AreEqual(0, sourceText.Length);
    }

    [TestMethod]
    public void SourceText_SingleLine_ShouldGetCorrectLineText()
    {
        var sourceText = new SourceText("SELECT * FROM table");

        var line = sourceText.GetLineText(1);

        Assert.AreEqual("SELECT * FROM table", line);
    }

    [TestMethod]
    public void Diagnostic_WithSuggestedFixes_ShouldIncludeCodeFixes()
    {
        var span = new TextSpan(0, 5);
        var action = DiagnosticAction.QuickFix("Replace with correct syntax", span, "correct");
        var location = new SourceLocation(0, 1, 1);

        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "Unexpected token",
            location,
            suggestedFixes: new[] { action });

        Assert.HasCount(1, diagnostic.SuggestedFixes);
        Assert.AreEqual("Replace with correct syntax", diagnostic.SuggestedFixes[0].Title);
    }

    [TestMethod]
    public void TextSpan_Default_ShouldBeEmpty()
    {
        var span = default(TextSpan);

        Assert.AreEqual(0, span.Start);
        Assert.AreEqual(0, span.Length);
        Assert.IsTrue(span.IsEmpty);
    }

    [TestMethod]
    public void TextSpan_Equality_ShouldWorkCorrectly()
    {
        var span1 = new TextSpan(10, 5);
        var span2 = new TextSpan(10, 5);
        var span3 = new TextSpan(10, 6);

        Assert.AreEqual(span1, span2);
        Assert.IsTrue(span1 == span2);
        Assert.IsFalse(span1 == span3);
        Assert.IsTrue(span1 != span3);
    }

    #endregion

    #region Error Recovery Tests

    [TestMethod]
    public void ErrorRecoveryManager_ShouldExist()
    {
        var managerType = typeof(ErrorRecoveryManager);

        Assert.IsNotNull(managerType);
    }

    [TestMethod]
    public void PanicModeRecovery_ShouldExist()
    {
        var recoveryType = typeof(PanicModeRecovery);

        Assert.IsNotNull(recoveryType);
    }

    [TestMethod]
    public void PhraseLevelRecovery_ShouldExist()
    {
        var recoveryType = typeof(PhraseLevelRecovery);

        Assert.IsNotNull(recoveryType);
    }

    [TestMethod]
    public void ErrorNode_ShouldBeCreatable()
    {
        var span = new TextSpan(0, 10);
        var errorNode = new ErrorNode("Test error message", span);

        Assert.AreEqual(span, errorNode.Span);
        Assert.AreEqual("Test error message", errorNode.Message);
    }

    [TestMethod]
    public void MissingNode_ShouldIndicateExpectedDescription()
    {
        var span = new TextSpan(10, 0);
        var missingNode = new MissingNode("FROM keyword", span);

        Assert.AreEqual("FROM keyword", missingNode.ExpectedDescription);
        Assert.AreEqual(span, missingNode.Span);
    }

    #endregion
}
