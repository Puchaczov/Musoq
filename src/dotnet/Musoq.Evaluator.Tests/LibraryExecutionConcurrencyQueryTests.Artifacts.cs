using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CompiledArtifactQuery_WhenSourcePermissionsChange_ShouldRejectIncompatibleExecutable(bool sourceAllowed)
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, sourceAllowed);
        const string query = "select Position + 1 as Value from #schema.first()";
        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.IsTrue(artifactResult.Succeeded);
        var artifact = artifactResult.Artifact ?? throw new AssertFailedException("No permission-qualified artifact was produced.");
        var matching = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(query, artifact, provider, _logger, ParallelOptions);
        Assert.IsTrue(matching.Succeeded, string.Join(Environment.NewLine, matching.Diagnostics));
        using (var compiled = matching.CompiledQuery)
        using (var result = compiled.Run(TestContext.CancellationToken))
        {
            var rows = result.ToArray();
            TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
            Assert.HasCount(4096, rows);
            for (var index = 0; index < rows.Length; index++)
                Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
        }
        var changed = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(query, artifact,
            CreateProvider(probe, !sourceAllowed), _logger, ParallelOptions);
        Assert.IsFalse(changed.Succeeded);
        Assert.IsTrue(changed.Diagnostics.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ8002_CompiledArtifactIncompatible));
        Assert.IsTrue(changed.Diagnostics.Any(static diagnostic => diagnostic.Message.Contains("SemanticShape", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("Provider")]
    [DataRow("Logger")]
    [DataRow("QueryContext")]
    public void SerialGroupedQuery_WhenColumnMatchesRuntimeParameter_ShouldKeepTypedLocalsDistinct(string column)
    {
        var provider = LibraryExecutionFixture.CreateProvider(
            new[] { new ReservedColumnRow("A", "A", "A", "A"), new ReservedColumnRow("A", "A", "A", "A") });
        var query = $"select {column} as Value, Count({column}) as Total from #schema.first() group by {column}";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(string)), ("Total", typeof(long)));
        Assert.HasCount(1, rows);
        Assert.AreEqual("A", rows[0].GetValue<string>(0));
        Assert.AreEqual(2L, rows[0].GetValue<long>(1));
    }

    public sealed record ReservedColumnRow(string Token, string Provider, string Logger, string QueryContext);
}
