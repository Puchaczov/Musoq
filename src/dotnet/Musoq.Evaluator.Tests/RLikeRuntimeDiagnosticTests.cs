using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RLikeRuntimeDiagnosticTests : BasicEntityTestBase
{
    private const string ParameterQuery =
        "param(pattern: string) select Name rlike $pattern as Matched from #A.Entities()";

    [TestMethod]
    public void ParameterPattern_AfterInvalidRun_ShouldAcceptAValidReplacement()
    {
        using var compiled = CreateAndRunVirtualMachine(ParameterQuery, Rows());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "pattern", "[secret-rec129(");
        var failure = Assert.Throws<QueryExecutionException>(() => _ = compiled.Run(TestContext.CancellationToken).Count);
        AssertPatternEnvelope(failure);

        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "pattern", "^Alpha$");
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(true, result[0].ReadValues()[0]);
    }

    [TestMethod]
    public async Task ParameterPattern_RunAsync_ShouldReportSpecificRuntimeError()
    {
        using var compiled = CreateAndRunVirtualMachine(ParameterQuery, Rows());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "pattern", "[secret-rec129(");
        var failure = await Assert.ThrowsAsync<QueryExecutionException>(async () =>
        {
            using var result = await compiled.RunAsync(TestContext.CancellationToken);
            _ = result.Count;
        });
        AssertPatternEnvelope(failure);
    }

    [TestMethod]
    [DataRow("let pattern: string = '['; select Name rlike $pattern as Matched from #A.Entities()")]
    [DataRow("select Name not rlike City as Matched from #A.Entities()")]
    public void DynamicPattern_ShouldCompileAndFailOnlyAtExecution(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, Rows());
        AssertPatternEnvelope(Assert.Throws<QueryExecutionException>(() => _ = compiled.Run(TestContext.CancellationToken).Count));
    }

    [TestMethod]
    public void ParameterPattern_WhenNoRowsAreEvaluated_ShouldNotValidateRegex()
    {
        using var compiled = CreateAndRunVirtualMachine(ParameterQuery,
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] });
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "pattern", "[");
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(0, result.Count);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> Rows() =>
        new() { ["#A"] = [new BasicEntity { Name = "Alpha", City = "[secret-rec129(" }] };

    private static void AssertPatternEnvelope(QueryExecutionException failure)
    {
        var envelope = failure.Envelope;
        Assert.IsNotNull(envelope);
        Assert.AreEqual(DiagnosticCode.MQ7015_InvalidRuntimeRegex, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, envelope.SourceKind);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotNull(failure.InnerException);
        Assert.DoesNotContain("secret-rec129", failure.FormatJson());
    }
}
