using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Converter.Build;
using Musoq.Schema;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SemanticTemplateCacheTests
{
    [TestMethod]
    public void SemanticTemplateCache_WhenContractIsReused_ShouldCloneSemanticAstPerCompilation()
    {
        var query = $"select i.Value from #artifact.items() i where i.Value = '{Guid.NewGuid():N}'";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("same"));
        var logger = new TestsLoggerResolver();
        var keyBefore = CreateKey(query, provider, new CompilationOptions());

        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "SemanticTemplateFirst",
            provider,
            logger,
            new CompilationOptions());
        Assert.IsTrue(first.Succeeded, string.Join(Environment.NewLine, first.Errors));
        Assert.IsNotNull(first.BuildItems);
        Assert.IsNotNull(first.BuildItems.SemanticArtifacts);

        var countAfterFirst = SemanticTemplateCache.Snapshot.Count;
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "SemanticTemplateSecond",
            provider,
            logger,
            new CompilationOptions());
        var keyAfter = CreateKey(query, provider, new CompilationOptions());

        Assert.IsTrue(second.Succeeded, string.Join(Environment.NewLine, second.Errors));
        Assert.IsNotNull(second.BuildItems);
        Assert.IsNotNull(second.BuildItems.SemanticArtifacts);
        Assert.IsTrue(SemanticTemplateCache.Snapshot.Count >= countAfterFirst);
        Assert.AreEqual(keyBefore, keyAfter);
        Assert.AreNotSame(
            first.BuildItems.SemanticArtifacts!.TransformedQueryTree,
            second.BuildItems.SemanticArtifacts!.TransformedQueryTree);
        Assert.AreNotSame(
            first.BuildItems.SemanticArtifacts.Phase.MetadataQuery,
            second.BuildItems.SemanticArtifacts.Phase.MetadataQuery);
    }

    private static SemanticTemplateCacheKey CreateKey(
        string query,
        ISchemaProvider provider,
        CompilationOptions options)
    {
        return SemanticTemplateCache.CreateKey(new SemanticTemplateCacheInput(
            query,
            provider,
            options,
            CompilationPurpose.Execution,
            ExecutionTargetIds.CSharpClr,
            QueryResultMode.Table,
            null,
            false,
            null,
            false,
            false,
            false,
            [],
            string.Empty))!.Value;
    }

    [TestMethod]
    public void SemanticTemplateCache_WhenOptionsDiffer_ShouldUseDistinctContracts()
    {
        var query = $"select d.Dummy from #system.dual() d where d.Dummy = '{Guid.NewGuid():N}'";
        var provider = new SystemSchemaProvider();
        var logger = new TestsLoggerResolver();
        var defaultKey = CreateKey(query, provider, new CompilationOptions());
        var noConstantFoldingKey = CreateKey(query, provider, new CompilationOptions(useConstantFolding: false));

        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "SemanticTemplateOptionsFirst",
            provider,
            logger,
            new CompilationOptions());
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "SemanticTemplateOptionsSecond",
            provider,
            logger,
            new CompilationOptions(useConstantFolding: false));

        Assert.IsTrue(first.Succeeded, string.Join(Environment.NewLine, first.Errors));
        Assert.IsTrue(second.Succeeded, string.Join(Environment.NewLine, second.Errors));
        Assert.AreNotEqual(defaultKey, noConstantFoldingKey);
    }
}
