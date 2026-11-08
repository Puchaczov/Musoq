using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterLanguageRegressionTests
{
    private const string NullableCollectionMembershipQuery =
        "param(ids: int[]? = null)\r\nselect Name from #A.Entities() where Id in $ids";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0027")]
    public void NullableCollectionParameterCompilesAndRejectsANullMembershipValue()
    {
        using var compiled = CompileNullableCollectionMembership();

        var failure = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });
        Assert.AreEqual(DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed, failure.Envelope?.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, failure.Envelope?.Phase);
    }

    [TestMethod]
    [DataRow("in", 1)]
    [DataRow("not in", 0)]
    public void NullableCollectionMembership_WithOverride_ShouldUseCapturedValues(string predicate, int expectedCount)
    {
        using var compiled = CompileNullableCollectionMembership(predicate);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "ids", new[] { 1 });
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(expectedCount, result.Count);
    }

    private CompiledQuery CompileNullableCollectionMembership(string predicate = "in")
    {
        var entities = new[] { new BasicEntity { Name = "one", Id = 1 } };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = entities,
            ["#A"] = entities
        };

        return InstanceCreator.CompileForExecution(NullableCollectionMembershipQuery.Replace("Id in", $"Id {predicate}", StringComparison.Ordinal),
            Guid.NewGuid().ToString(), new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
    }
}
