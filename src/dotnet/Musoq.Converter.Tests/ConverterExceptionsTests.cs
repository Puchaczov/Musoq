using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

/// <summary>
///     Tests for Converter exception classes
/// </summary>
[TestClass]
public class ConverterExceptionsTests
{
    #region AstValidationException Tests

    [TestMethod]
    public void AstValidationException_Constructor_WithNodeTypeContextMessage_ShouldSetProperties()
    {
        // Arrange
        var nodeType = "SelectNode";
        var context = "query parsing";
        var message = "Invalid structure detected";

        // Act
        var exception = new AstValidationException(nodeType, context, message);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void AstValidationException_Constructor_WithInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var nodeType = "JoinNode";
        var context = "join processing";
        var message = "Failed to process join";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new AstValidationException(nodeType, context, message, innerException);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    [TestMethod]
    public void AstValidationException_IsInvalidOperationException()
    {
        // Arrange & Act
        var exception = new AstValidationException("Node", "Context", "Message");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void AstValidationException_ForNullNode_ShouldCreateProperException()
    {
        // Arrange
        var expectedNodeType = "WhereClauseNode";
        var context = "filter processing";

        // Act
        var exception = AstValidationException.ForNullNode(expectedNodeType, context);

        // Assert
        Assert.AreEqual(expectedNodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.Contains(expectedNodeType, exception.Message);
        Assert.Contains(context, exception.Message);
        Assert.Contains("null", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForNullNode_ShouldContainHelpfulMessage()
    {
        // Arrange & Act
        var exception = AstValidationException.ForNullNode("SelectNode", "column selection");

        // Assert
        Assert.Contains("SQL query structure", exception.Message);
        Assert.Contains("check your query syntax", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForInvalidNodeStructure_ShouldCreateProperException()
    {
        // Arrange
        var nodeType = "GroupByNode";
        var context = "aggregation processing";
        var issue = "missing grouping columns";

        // Act
        var exception = AstValidationException.ForInvalidNodeStructure(nodeType, context, issue);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.Contains(nodeType, exception.Message);
        Assert.Contains(context, exception.Message);
        Assert.Contains(issue, exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForInvalidNodeStructure_ShouldContainHelpfulMessage()
    {
        // Arrange & Act
        var exception = AstValidationException.ForInvalidNodeStructure("JoinNode", "join", "missing ON clause");

        // Assert
        Assert.Contains("invalid structure", exception.Message);
        Assert.Contains("SQL query", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForUnsupportedNode_ShouldCreateProperException()
    {
        // Arrange
        var nodeType = "PivotNode";
        var context = "query transformation";

        // Act
        var exception = AstValidationException.ForUnsupportedNode(nodeType, context);

        // Assert
        Assert.AreEqual(nodeType, exception.NodeType);
        Assert.AreEqual(context, exception.ValidationContext);
        Assert.Contains(nodeType, exception.Message);
        Assert.Contains(context, exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_ForUnsupportedNode_ShouldContainHelpfulMessage()
    {
        // Arrange & Act
        var exception = AstValidationException.ForUnsupportedNode("RecursiveCTE", "CTE processing");

        // Assert
        Assert.Contains("not supported", exception.Message);
        Assert.Contains("documentation", exception.Message);
    }

    [TestMethod]
    public void AstValidationException_CanBeThrown_AndCaught()
    {
        // Arrange & Act
        AstValidationException caughtException = null;
        try
        {
            throw AstValidationException.ForNullNode("TestNode", "testing");
        }
        catch (AstValidationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.AreEqual("TestNode", caughtException.NodeType);
    }

    [TestMethod]
    public void AstValidationException_CanBeCaughtAsInvalidOperationException()
    {
        // Arrange & Act
        InvalidOperationException caughtException = null;
        try
        {
            throw new AstValidationException("Node", "Context", "Message");
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
    }

    #endregion

    #region CompilationException Tests

    [TestMethod]
    public void CompilationException_Constructor_ShouldSetMessage()
    {
        // Arrange
        var message = "Failed to compile the query";

        // Act
        var exception = new CompilationException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void CompilationException_IsException()
    {
        // Arrange & Act
        var exception = new CompilationException("Test message");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void CompilationException_CanBeThrown_AndCaught()
    {
        // Arrange
        var message = "Compilation failed due to syntax error";
        CompilationException caughtException = null;

        // Act
        try
        {
            throw new CompilationException(message);
        }
        catch (CompilationException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.AreEqual(message, caughtException.Message);
    }

    [TestMethod]
    public void CompilationException_WithEmptyMessage_ShouldWork()
    {
        // Arrange & Act
        var exception = new CompilationException(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, exception.Message);
    }

    [TestMethod]
    public void CompilationException_WithDetailedMessage_ShouldPreserveMessage()
    {
        // Arrange
        var message = "Failed to compile query:\n" +
                      "  Error at line 3, column 15\n" +
                      "  Unexpected token 'INVALID'\n" +
                      "  Expected: identifier, keyword, or expression";

        // Act
        var exception = new CompilationException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.Contains("line 3", exception.Message);
        Assert.Contains("INVALID", exception.Message);
    }

    #endregion

    #region MusoqQueryException Tests

    [TestMethod]
    public void MusoqQueryException_WhenSingleEnvelope_ShouldSetMessageFromEnvelope()
    {
        var envelope = CreateTestEnvelope("Missing alias for data source");

        var exception = new MusoqQueryException(envelope);

        Assert.Contains("MQ9999", exception.Message);
        Assert.Contains("Message: Missing alias for data source", exception.Message);
        Assert.HasCount(1, exception.Envelopes);
        Assert.AreSame(envelope, exception.PrimaryEnvelope);
    }

    [TestMethod]
    public void MusoqQueryException_WhenSingleEnvelopeWithInner_ShouldPreserveInnerException()
    {
        var envelope = CreateTestEnvelope("Query failed");
        var inner = new InvalidOperationException("internal error");

        var exception = new MusoqQueryException(envelope, inner);

        Assert.Contains("Message: Query failed", exception.Message);
        Assert.AreSame(inner, exception.InnerException);
    }

    [TestMethod]
    public void MusoqQueryException_WhenMultipleEnvelopes_ShouldUseFormattedEnvelopeText()
    {
        var envelopes = new List<MusoqErrorEnvelope>
        {
            CreateTestEnvelope("First error"),
            CreateTestEnvelope("Second error"),
            CreateTestEnvelope("Third error")
        };

        var exception = new MusoqQueryException(envelopes);

        Assert.Contains("Message: First error", exception.Message);
        Assert.Contains("Message: Second error", exception.Message);
        Assert.Contains("Message: Third error", exception.Message);
        Assert.Contains("---", exception.Message);
        Assert.HasCount(3, exception.Envelopes);
        Assert.AreSame(envelopes[0], exception.PrimaryEnvelope);
    }

    [TestMethod]
    public void MusoqQueryException_WhenTwoEnvelopes_ShouldSeparateFormattedErrors()
    {
        var envelopes = new List<MusoqErrorEnvelope>
        {
            CreateTestEnvelope("First error"),
            CreateTestEnvelope("Second error")
        };

        var exception = new MusoqQueryException(envelopes);

        Assert.Contains("Message: First error", exception.Message);
        Assert.Contains("Message: Second error", exception.Message);
        Assert.Contains("---", exception.Message);
    }

    [TestMethod]
    public void MusoqQueryException_Message_ShouldMatchFormatText()
    {
        var envelopes = new List<MusoqErrorEnvelope>
        {
            CreateTestEnvelope("First error"),
            CreateTestEnvelope("Second error")
        };

        var exception = new MusoqQueryException(envelopes);

        Assert.AreEqual(exception.FormatText(), exception.Message);
    }

    [TestMethod]
    public void MusoqQueryException_FormatText_WhenSingleEnvelope_ShouldFormatCorrectly()
    {
        var envelope = new MusoqErrorEnvelope(
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Missing alias",
            2, 5, null, null,
            "Aliases are required",
            ["Add alias after source"],
            "Core Spec §Aliasing",
            null);

        var exception = new MusoqQueryException(envelope);
        var text = exception.FormatText();

        Assert.Contains("MQ3022", text);
        Assert.Contains("[error]", text);
        Assert.Contains("[bind]", text);
        Assert.Contains("Missing alias", text);
        Assert.Contains("At: line 2, column 5", text);
    }

    [TestMethod]
    public void MusoqQueryException_FormatText_WhenMultipleEnvelopes_ShouldSeparateWithDashes()
    {
        var envelopes = new List<MusoqErrorEnvelope>
        {
            CreateTestEnvelope("Error one"),
            CreateTestEnvelope("Error two")
        };

        var exception = new MusoqQueryException(envelopes);
        var text = exception.FormatText();

        Assert.Contains("Error one", text);
        Assert.Contains("---", text);
        Assert.Contains("Error two", text);
    }

    [TestMethod]
    public void MusoqQueryException_FormatJson_WhenSingleEnvelope_ShouldReturnJsonObject()
    {
        var envelope = CreateTestEnvelope("Test error");
        var exception = new MusoqQueryException(envelope);
        var json = exception.FormatJson();

        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
        Assert.Contains("\"code\":", json);
        Assert.Contains("\"message\":\"Test error\"", json);
    }

    [TestMethod]
    public void MusoqQueryException_FormatJson_WhenMultipleEnvelopes_ShouldReturnJsonArray()
    {
        var envelopes = new List<MusoqErrorEnvelope>
        {
            CreateTestEnvelope("Error one"),
            CreateTestEnvelope("Error two")
        };

        var exception = new MusoqQueryException(envelopes);
        var json = exception.FormatJson();

        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
    }

    [TestMethod]
    public void MusoqQueryException_WhenNullEnvelopes_ShouldThrowArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MusoqQueryException((IReadOnlyList<MusoqErrorEnvelope>)null!));
    }

    [TestMethod]
    public void MusoqQueryException_IsException()
    {
        var envelope = CreateTestEnvelope("Test");
        var exception = new MusoqQueryException(envelope);

        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    private static MusoqErrorEnvelope CreateTestEnvelope(string message)
    {
        return new MusoqErrorEnvelope(
            DiagnosticCode.MQ9999_Unknown,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Runtime,
            message,
            null, null, null, null, null,
            Array.Empty<string>(), null, null);
    }

    #endregion

    #region CompileForExecution Envelope Tests

    [TestMethod]
    public void CompileForExecution_WhenQueryIsInvalid_ShouldThrowMusoqQueryException()
    {
        var exception = Assert.Throws<MusoqQueryException>(
            () => InstanceCreator.CompileForExecution(
                "SELECT nonexistent FROM #system.dual()",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()));

        Assert.HasCount(1, exception.Envelopes,
            $"Expected exactly 1 error but got {exception.Envelopes.Count}: [{string.Join(", ", exception.Envelopes.Select(e => $"{e.Code}: {e.Message}"))}]");
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void CompileForExecution_WhenQueryIsValid_ShouldCompileSuccessfully()
    {
        var compiled = InstanceCreator.CompileForExecution(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var result = compiled.Run();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CompileForExecution_WhenInvalid_ShouldHaveFormattedOutput()
    {
        var exception = Assert.Throws<MusoqQueryException>(
            () => InstanceCreator.CompileForExecution(
                "SELECTE BADD SYNTAKS",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()));

        var text = exception.FormatText();
        var json = exception.FormatJson();

        Assert.IsFalse(string.IsNullOrWhiteSpace(text));
        Assert.IsFalse(string.IsNullOrWhiteSpace(json));
        Assert.Contains("MQ", text);
    }

    [TestMethod]
    public void CompileForExecution_WhenSyntaxError_ShouldPreserveInnerException()
    {
        var exception = Assert.Throws<MusoqQueryException>(
            () => InstanceCreator.CompileForExecution(
                "SELECTE BADD SYNTAKS",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()));

        Assert.IsNotNull(exception.InnerException);
    }

    [TestMethod]
    public void CompileForExecution_WhenKeywordIsMistyped_ShouldIncludeDidYouMeanGuidance()
    {
        var exception = Assert.Throws<MusoqQueryException>(
            () => InstanceCreator.CompileForExecution(
                "SELECTE BADD SYNTAKS",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()));

        Assert.Contains("Did you mean 'SELECT'?", exception.Message);
        Assert.Contains("Try:", exception.Message);
    }

    [TestMethod]
    public void CompileForExecution_WhenDialectKeywordUsed_ShouldSuggestMusoqEquivalent()
    {
        var exception = Assert.Throws<MusoqQueryException>(
            () => InstanceCreator.CompileForExecution(
                "SELECT 1 FROM #system.dual() LIMIT 5",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()));

        Assert.Contains("Musoq uses TAKE instead of LIMIT", exception.Message);
        Assert.Contains("TAKE", exception.Message);
    }

    #endregion

    #region CompileWithDiagnostics Tests

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryIsValid_ShouldReturnSucceededResult()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.HasErrors);
        Assert.IsNotNull(result.CompiledQuery);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryIsValid_ShouldProduceRunnableQuery()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var table = result.CompiledQuery!.Run();

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryHasSemanticError_ShouldReturnFailedResult()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECT nonexistent FROM #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.HasErrors);
        Assert.IsNull(result.CompiledQuery);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryHasSemanticError_ShouldCollectDiagnostics()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECT nonexistent FROM #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.HasCount(1, result.Errors,
            $"Expected exactly 1 error but got {result.Errors.Count}: [{string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}]");
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, result.Errors[0].Code);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryHasSyntaxError_ShouldReturnFailedResult()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECTE BADD SYNTAKS",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.HasErrors);
        Assert.IsNull(result.CompiledQuery);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryHasError_ToEnvelopesShouldReturnEnvelopes()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECT nonexistent FROM #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var envelopes = result.ToEnvelopes();

        Assert.HasCount(1, envelopes,
            $"Expected exactly 1 envelope but got {envelopes.Count}: [{string.Join(", ", envelopes.Select(e => $"{e.Code}: {e.Message}"))}]");
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelopes[0].Code);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryIsValid_ErrorsShouldBeEmpty()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.IsEmpty(result.Errors);
        Assert.IsEmpty(result.ToEnvelopes());
    }

    [TestMethod]
    public void CompileWithDiagnostics_WithCompilationOptions_ShouldRespectOptions()
    {
        var options = new CompilationOptions(ParallelizationMode.Full);

        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            options);

        Assert.IsTrue(result.Succeeded);
    }

    #endregion

    #region BuildResult Tests

    [TestMethod]
    public void BuildResult_WhenSucceeded_WarningsShouldBeAccessible()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.IsNotNull(result.Warnings);
        Assert.IsNotNull(result.Diagnostics);
    }

    [TestMethod]
    public void BuildResult_WhenFailed_CompiledQueryShouldBeNull()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECT nonexistent FROM #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        Assert.IsNull(result.CompiledQuery);
        Assert.IsFalse(result.Succeeded);
    }

    #endregion

    #region Error Position Tests

    [TestMethod]
    public void WhenUnknownColumn_ShouldReportCorrectPosition()
    {
        var query = "SELECT nonexistent FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(8, env.Column);
        Assert.AreEqual(11, env.Length);
    }

    [TestMethod]
    public void WhenMultilineQueryWithUnknownColumn_ShouldReportCorrectLine()
    {
        var query = "SELECT\n  1 as valid,\n  nonexistent_column\nFROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(3, env.Line);
        Assert.AreEqual(3, env.Column);
        Assert.AreEqual(18, env.Length);
    }

    [TestMethod]
    public void WhenDotAccessUnknownProperty_ShouldReportPosition()
    {
        var query = "SELECT a.nonexistent FROM #system.dual() a";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsGreaterThanOrEqualTo(1, envelopes.Count,
            $"Expected at least 1 error, got {envelopes.Count}");

        var env = envelopes[0];
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(10, env.Column);
        Assert.AreEqual(11, env.Length);
    }

    [TestMethod]
    public void WhenUnknownColumnAfterValidColumn_ShouldReportCorrectPosition()
    {
        var query = "SELECT 1 as a, bad_column FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(16, env.Column);
        Assert.AreEqual(10, env.Length);
    }

    [TestMethod]
    public void WhenTwoUnknownColumns_ShouldReportBothPositions()
    {
        var query = "SELECT bad1, bad2 FROM #system.dual()";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.HasCount(2, envelopes);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelopes[0].Code);
        Assert.AreEqual(1, envelopes[0].Line);
        Assert.AreEqual(8, envelopes[0].Column);
        Assert.AreEqual(4, envelopes[0].Length);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelopes[1].Code);
        Assert.AreEqual(1, envelopes[1].Line);
        Assert.AreEqual(14, envelopes[1].Column);
        Assert.AreEqual(4, envelopes[1].Length);
    }

    [TestMethod]
    public void WhenUnknownColumnInWhereClause_ShouldReportCorrectPosition()
    {
        var query = "SELECT Dummy FROM #system.dual() WHERE missing = 1";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(40, env.Column);
        Assert.AreEqual(7, env.Length);
    }

    [TestMethod]
    public void WhenUnknownColumnInOrderBy_ShouldReportCorrectPosition()
    {
        var query = "SELECT Dummy FROM #system.dual() ORDER BY nonexistent";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(43, env.Column);
        Assert.AreEqual(11, env.Length);
    }

    [TestMethod]
    public void WhenILikeUsed_ShouldReportPositionOfILikeToken()
    {
        var query = "SELECT Dummy FROM #system.dual() WHERE Dummy ILIKE 'x'";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(46, env.Column);
        Assert.AreEqual(5, env.Length);
    }

    [TestMethod]
    public void WhenNotEqualOperatorUsed_ShouldReportPositionOfBangEquals()
    {
        var query = "SELECT 1 FROM #system.dual() WHERE 1 != 2";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ2019_InvalidOperator, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(38, env.Column);
        Assert.AreEqual(2, env.Length);
    }

    [TestMethod]
    public void WhenMultilineQuery_ErrorOnSecondLine_ShouldReportLine2()
    {
        var query = "SELECT\n  missing_col\nFROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(2, env.Line);
        Assert.AreEqual(3, env.Column);
    }

    [TestMethod]
    public void WhenMultilineQuery_ErrorOnLastLine_ShouldReportCorrectLine()
    {
        var query = "SELECT\n  Dummy\nFROM #system.dual()\nORDER BY bad_col";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(4, env.Line);
        Assert.AreEqual(10, env.Column);
        Assert.AreEqual(7, env.Length);
    }

    [TestMethod]
    public void WhenDivisionByZeroLiteral_ShouldReportCorrectPosition()
    {
        var query = "SELECT 1 / 0 FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3008_DivisionByZero, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(8, env.Column);
        Assert.AreEqual(5, env.Length);
    }

    [TestMethod]
    public void WhenModuloByZeroLiteral_ShouldReportCorrectPosition()
    {
        var query = "SELECT 1 % 0 FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3008_DivisionByZero, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(8, env.Column);
        Assert.AreEqual(5, env.Length);
    }

    [TestMethod]
    public void WhenDuplicateAliasInJoin_ShouldReportCorrectPosition()
    {
        var query = "SELECT a.Dummy FROM #system.dual() a INNER JOIN #system.dual() a ON 1 = 1";
        var env = CompileAndGetSingleEnvelope(query);
        
        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(64, env.Column);
        Assert.AreEqual(1, env.Length);
    }

    [TestMethod]
    public void WhenUnknownColumnInHaving_ShouldReportCorrectPosition()
    {
        var query = "SELECT Dummy, 1 FROM #system.dual() GROUP BY Dummy HAVING Dummy = 'x' AND missing = 1";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsGreaterThanOrEqualTo(1, envelopes.Count,
            $"Expected at least 1 error but got {envelopes.Count}");

        var unknownColEnvelope = envelopes.FirstOrDefault(e => e.Code == DiagnosticCode.MQ3001_UnknownColumn);
        Assert.IsNotNull(unknownColEnvelope);
        Assert.AreEqual(1, unknownColEnvelope.Line);
        Assert.AreEqual(75, unknownColEnvelope.Column);
        Assert.AreEqual(7, unknownColEnvelope.Length);
    }

    [TestMethod]
    public void WhenValidQuery_ShouldProduceNoEnvelopes()
    {
        var query = "SELECT Dummy FROM #system.dual()";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsEmpty(envelopes);
    }

    [TestMethod]
    public void WhenValidQueryWithAlias_ShouldProduceNoEnvelopes()
    {
        var query = "SELECT a.Dummy FROM #system.dual() a";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsEmpty(envelopes);
    }

    private static MusoqErrorEnvelope CompileAndGetSingleEnvelope(string query)
    {
        var envelopes = CompileAndGetEnvelopes(query);
        Assert.HasCount(1, envelopes,
            $"Expected 1 error but got {envelopes.Count}: [{string.Join(", ", envelopes.Select(e => $"{e.CodeString}: {e.Message}"))}]");
        return envelopes[0];
    }

    private static IReadOnlyList<MusoqErrorEnvelope> CompileAndGetEnvelopes(string query)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
        return result.ToEnvelopes();
    }

    #endregion
}
