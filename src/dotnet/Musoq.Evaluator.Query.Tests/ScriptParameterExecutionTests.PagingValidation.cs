using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterExecutionTests
{
    [TestMethod]
    [DataRow("param(count: string)")]
    [DataRow("param(count: double)")]
    [DataRow("param(count: float)")]
    [DataRow("param(count: decimal)")]
    [DataRow("param(count: bool)")]
    [DataRow("param(count: int[])")]
    [DataRow("param(count: (value: int))")]
    [DataRow("let count: int = -1")]
    [DataRow("let count: long = 2147483648L")]
    public void Paging_InvalidCountDeclaration_ShouldReportBindingDiagnostic(string declaration)
    {
        var sql = $"{declaration} select Key from #EnvironmentVariables.All() take $count";
        var result = InstanceCreator.CompileWithDiagnostics(sql, Guid.NewGuid().ToString(), new EnvironmentVariablesSchemaProvider(), LoggerResolver);
        var diagnostic = result.ToEnvelopes().First(envelope => envelope.Code == DiagnosticCode.MQ3118_InvalidPagingCount);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, "TAKE count '$count'");
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow("take $count")]
    [DataRow("order by Key skip $count take 0")]
    [DataRow("order by Key skip 0 take $count")]
    public void Paging_InvalidRuntimeCount_ShouldFailBeforeOpeningSource(string paging)
    {
        var provider = new ParameterizedEnvironmentSchemaProvider();
        using var query = CompileWithProvider($"param(count: int = -1) select Key from #Parameterized.Items() {paging}", provider);
        Assert.ThrowsExactly<PagingCountException>(() => _ = query.Run(TestContext.CancellationToken).Count);
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void Paging_MissingParameter_ShouldPreserveBindingErrorBeforeOpeningSource()
    {
        var provider = new ParameterizedEnvironmentSchemaProvider();
        using var query = CompileWithProvider("param(count: int) select Key from #Parameterized.Items() take $count", provider);
        var exception = Assert.Throws<QueryExecutionException>(() => _ = query.Run(TestContext.CancellationToken).Count);
        Assert.AreEqual(DiagnosticCode.MQ7003_RequiredScriptParameterMissing, exception.Envelope!.Code);
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void Paging_UndeclaredReference_ShouldPreserveDeclarationDiagnostic()
    {
        var result = InstanceCreator.CompileWithDiagnostics("select Key from #EnvironmentVariables.All() take $missing", Guid.NewGuid().ToString(), new EnvironmentVariablesSchemaProvider(), LoggerResolver);
        Assert.AreEqual(DiagnosticCode.MQ3059_UndeclaredScriptParameter, result.ToEnvelopes().First().Code);
    }
    [TestMethod]
    [DataRow("take 2147483648L")]
    [DataRow("skip 18446744073709551615UL")]
    public void Paging_OversizedLiteral_ShouldReportBindingErrorInsteadOfWrapping(string paging)
    {
        var result = InstanceCreator.CompileWithDiagnostics($"select Key from #EnvironmentVariables.All() {paging}", Guid.NewGuid().ToString(), new EnvironmentVariablesSchemaProvider(), LoggerResolver);
        Assert.AreEqual(DiagnosticCode.MQ3118_InvalidPagingCount, result.ToEnvelopes().First().Code);
    }
}
