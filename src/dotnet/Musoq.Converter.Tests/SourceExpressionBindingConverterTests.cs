using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SourceExpressionBindingConverterTests
{
    [TestMethod]
    [DataRow("system.dual(FromHexToBytes('2A000000')) s", DiagnosticCode.MQ3022_MissingAlias, "FromHexToBytes('2A000000')")]
    [DataRow("system.dual() s cross apply Interpret<Header>(missing.FromHexToBytes('2A000000')) h", DiagnosticCode.MQ3015_UnknownAlias, "missing.FromHexToBytes('2A000000')")]
    [DataRow("system.dual() s cross apply Interpret<Header>(MissingBytes('2A000000')) h", DiagnosticCode.MQ3086_UnknownCallable, "MissingBytes('2A000000')")]
    [DataRow("system.dual() s cross apply Interpret<Header>(FromHexToBytes()) h", DiagnosticCode.MQ3087_InvalidCallableArity, "FromHexToBytes()")]
    [DataRow("system.dual() s cross apply Interpret<Header>(upcoming.FromHexToBytes('2A000000')) h cross apply system.dual() upcoming", DiagnosticCode.MQ3015_UnknownAlias, "upcoming.FromHexToBytes('2A000000')")]
    public void InvalidSourceExpressionMethodOwner_ProducesBindDiagnosticOnBothSurfaces(
        string source,
        DiagnosticCode expectedCode,
        string expression)
    {
        var query = $"binary Header {{ Value: int le }}; select 1 from {source}";
        var analysis = new QueryAnalyzer(new SystemSchemaProvider()).Analyze(query);

        Assert.IsTrue(analysis.HasErrors);
        Assert.IsTrue(analysis.Errors.Any(error => error.Code == expectedCode),
            string.Join("; ", analysis.Errors.Select(error => $"{error.Code}: {error.Message}")));
        Assert.IsFalse(analysis.Errors.Any(error => error.Code == DiagnosticCode.MQ9001_InternalCompilerError));
        var diagnostic = analysis.Errors.First(error => error.Code == expectedCode);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsTrue(diagnostic.Span.Length > 0);
        var expressionStart = query.IndexOf(expression, StringComparison.Ordinal);
        Assert.IsTrue(expressionStart >= 0);
        Assert.IsTrue(diagnostic.Span.Start >= expressionStart);
        Assert.IsTrue(diagnostic.Span.End <= expressionStart + expression.Length);

        var exception = Assert.Throws<MusoqQueryException>(() => InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver()));
        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == expectedCode));
        Assert.IsFalse(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ9001_InternalCompilerError));
    }

    [TestMethod]
    public void QueryAnalyzer_AfterInvalidFirstSourceMethod_StillAcceptsValidApply()
    {
        const string valid = """
            binary Header { Value: int le };
            select h.Value from system.dual() s
            cross apply Interpret<Header>(FromHexToBytes('2A000000')) h
            """;
        const string invalid = "select 1 from system.dual(FromHexToBytes('2A000000')) s";
        var analyzer = new QueryAnalyzer(new SystemSchemaProvider());

        Assert.IsTrue(analyzer.Analyze(valid).IsSuccess);
        Assert.IsTrue(analyzer.Analyze(invalid).Errors.Any(error => error.Code == DiagnosticCode.MQ3022_MissingAlias));
        Assert.IsTrue(analyzer.Analyze(valid).IsSuccess);
    }

    [TestMethod]
    public void DirectBinaryInterpretation_WithUnqualifiedConversion_ReturnsFortyTwo()
    {
        const string query = """
            binary Header { Value: int le };

            SELECT h.Value AS Value
            FROM system.dual() s
            CROSS APPLY Interpret<Header>(FromHexToBytes('2A000000')) h
            """;

        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].ReadValue(0));
    }
}
