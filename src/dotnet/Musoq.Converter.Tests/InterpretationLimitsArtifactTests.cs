using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class InterpretationLimitsArtifactTests
{
    private const string Query = "binary Rec { X: byte } binary S { Records: Rec repeat until eof } " +
        "select Count(r.X) as N from #system.dual() d cross apply Interpret<S>(FromHexToBytes('010101')) s cross apply s.Records r";

    [TestMethod]
    public void ExecutionCache_ShouldSeparateConfiguredLimitsWithoutChangingEligibility()
    {
        var query = $"select d.Dummy from #system.dual() d where d.Dummy = '{Guid.NewGuid():N}'";
        var provider = new SystemSchemaProvider();
        var logger = new TestsLoggerResolver();
        var lower = new CompilationOptions().WithInterpretationLimits(new(2));
        var higher = new CompilationOptions().WithInterpretationLimits(new(3));
        using var first = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, logger, lower);

        Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, lower));
        Assert.IsFalse(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, higher));
        Assert.AreNotEqual(InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(query, provider, lower),
            InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(query, provider, higher));
        using var second = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, logger, higher);
        Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, higher));
    }

    [TestMethod]
    public void StoredArtifact_ShouldRetainItsConfiguredLimit()
    {
        var options = new CompilationOptions().WithInterpretationLimits(new(3));
        var provider = new SystemSchemaProvider();
        var logger = new TestsLoggerResolver();
        var result = InstanceCreator.CompileArtifactWithDiagnostics(Query, Guid.NewGuid().ToString(), provider, logger, options);
        Assert.IsTrue(result.Succeeded, string.Join("\n", result.Diagnostics));
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(Query, result.Artifact!, provider, logger, options);
        Assert.IsTrue(loaded.Succeeded, string.Join("\n", loaded.Diagnostics));
        using var executable = loaded.CompiledQuery!;
        using var table = executable.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3L, table[0].ReadValue(0));
        Assert.AreEqual(CompilationOptionsFingerprint.Compute(options), result.Artifact!.CompilationOptionsSignature);
    }

    [TestMethod]
    public void StoredArtifact_ShouldRejectAHostWithDifferentLimits()
    {
        var options = new CompilationOptions().WithInterpretationLimits(new(3));
        var provider = new SystemSchemaProvider();
        var logger = new TestsLoggerResolver();
        var result = InstanceCreator.CompileArtifactWithDiagnostics(Query, Guid.NewGuid().ToString(), provider, logger, options);
        Assert.IsTrue(result.Succeeded, string.Join("\n", result.Diagnostics));
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(Query, result.Artifact!, provider, logger,
            options.WithInterpretationLimits(new(2)));

        Assert.IsFalse(loaded.Succeeded);
        Assert.AreEqual(DiagnosticCode.MQ8002_CompiledArtifactIncompatible, loaded.Errors.Single().Code);
    }
}
