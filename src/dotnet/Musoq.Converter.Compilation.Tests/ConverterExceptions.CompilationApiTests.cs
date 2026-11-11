using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

/// <summary>
///     Tests for CompileForExecution, CompileWithDiagnostics, and BuildResult APIs
/// </summary>
[TestClass]
public class ConverterExceptionsCompilationApiTests
{
    #region CompileForExecution Envelope Tests

    [TestMethod]
    public void CompileForExecution_WhenQueryIsInvalid_ShouldThrowMusoqQueryException()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                "SELECT nonexistent FROM #system.dual()",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()
            )
        );

        Assert.HasCount(
            1,
            exception.Envelopes,
            $"Expected exactly 1 error but got {exception.Envelopes.Count}: [{string.Join(", ", exception.Envelopes.Select(e => $"{e.Code}: {e.Message}"))}]"
        );
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void CompileForExecution_WhenQueryIsValid_ShouldCompileSuccessfully()
    {
        using var compiled = InstanceCreator.CompileForExecution(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver()
        );

        using var result = compiled.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1, result[0].ReadValue(0));
    }

    [TestMethod]
    public void CompileForExecution_WhenInvalid_ShouldHaveFormattedOutput()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                "SELECTE BADD SYNTAKS",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()
            )
        );

        var text = exception.FormatText();
        var json = exception.FormatJson();

        Assert.IsFalse(string.IsNullOrWhiteSpace(text));
        Assert.IsFalse(string.IsNullOrWhiteSpace(json));
        Assert.Contains("MQ", text);
    }

    [TestMethod]
    public void CompileForExecution_WhenSyntaxError_ShouldPreserveInnerException()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                "SELECTE BADD SYNTAKS",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()
            )
        );

        Assert.IsNotNull(exception.InnerException);
    }

    [TestMethod]
    public void CompileForExecution_WhenKeywordIsMistyped_ShouldIncludeDidYouMeanGuidance()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                "SELECTE BADD SYNTAKS",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()
            )
        );

        Assert.Contains("Did you mean 'SELECT'?", exception.Message);
        Assert.Contains("Try:", exception.Message);
    }

    [TestMethod]
    public void CompileForExecution_WhenDialectKeywordUsed_ShouldSuggestMusoqEquivalent()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                "SELECT 1 FROM #system.dual() LIMIT 5",
                Guid.NewGuid().ToString(),
                new SystemSchemaProvider(),
                new TestsLoggerResolver()
            )
        );

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
            new TestsLoggerResolver()
        );

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
            new TestsLoggerResolver()
        );

        using var table = result.CompiledQuery!.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0].ReadValue(0));
        result.CompiledQuery.Dispose();
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryHasSemanticError_ShouldReturnFailedResult()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECT nonexistent FROM #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver()
        );

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
            new TestsLoggerResolver()
        );

        Assert.HasCount(
            1,
            result.Errors,
            $"Expected exactly 1 error but got {result.Errors.Count}: [{string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}]"
        );
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, result.Errors[0].Code);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryHasSyntaxError_ShouldReturnFailedResult()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECTE BADD SYNTAKS",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver()
        );

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
            new TestsLoggerResolver()
        );

        var envelopes = result.ToEnvelopes();

        Assert.HasCount(
            1,
            envelopes,
            $"Expected exactly 1 envelope but got {envelopes.Count}: [{string.Join(", ", envelopes.Select(e => $"{e.Code}: {e.Message}"))}]"
        );
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelopes[0].Code);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenQueryIsValid_ErrorsShouldBeEmpty()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver()
        );

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
            options
        );

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
            new TestsLoggerResolver()
        );

        Assert.IsEmpty(result.Warnings);
        Assert.IsEmpty(result.Diagnostics);
        result.CompiledQuery!.Dispose();
    }

    [TestMethod]
    public void BuildResult_WhenFailed_CompiledQueryShouldBeNull()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "SELECT nonexistent FROM #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver()
        );

        Assert.IsNull(result.CompiledQuery);
        Assert.IsFalse(result.Succeeded);
    }

    #endregion
}
