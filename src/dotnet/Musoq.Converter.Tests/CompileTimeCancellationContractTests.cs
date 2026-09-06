using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CompileTimeCancellationContractTests
{
    [TestMethod]
    public void SemanticCacheSnapshot_ShouldClearTransientInvocationTokens()
    {
        if (Debugger.IsAttached)
            return;

        var query = $"select i.Value from #artifact.items() i where i.Value = '{Guid.NewGuid():N}'";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("snapshot"));
        var options = new CompilationOptions();
        using var cancellation = new CancellationTokenSource();

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"CancellationSnapshot_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options,
            cancellation.Token);

        try
        {
            Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
            Assert.IsNotNull(result.BuildItems?.SemanticArtifacts);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }

        var key = SemanticTemplateCache.CreateKey(
            CreateSemanticCacheInput(query, provider, options),
            CancellationToken.None);
        Assert.IsTrue(key.HasValue);
        Assert.IsTrue(SemanticTemplateCache.TryGet(key.Value, CancellationToken.None, out var snapshot));
        Assert.IsNotEmpty(snapshot.SourcePlanRequestsPerSchema);
        Assert.IsTrue(snapshot.SourcePlanRequestsPerSchema.Values.All(
            static request => request.CancellationToken == CancellationToken.None));
        Assert.IsTrue(snapshot.Phase.Metadata.SourcePlanRequestsPerSchema.Values.All(
            static request => request.CancellationToken == CancellationToken.None));
    }

    [TestMethod]
    public void SemanticCacheKey_ShouldNotVaryWithInvocationToken()
    {
        if (Debugger.IsAttached)
            return;

        var query = $"select i.Value from #artifact.items() i where i.Value = '{Guid.NewGuid():N}'";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("key"));
        var options = new CompilationOptions();
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();

        var first = SemanticTemplateCache.CreateKey(
            CreateSemanticCacheInput(query, provider, options),
            firstCancellation.Token);
        var second = SemanticTemplateCache.CreateKey(
            CreateSemanticCacheInput(query, provider, options),
            secondCancellation.Token);

        Assert.IsTrue(first.HasValue);
        Assert.IsTrue(second.HasValue);
        Assert.AreEqual(first.Value, second.Value);
    }

    [TestMethod]
    public void ExecutionCacheKey_ShouldNotVaryWithInvocationToken()
    {
        if (Debugger.IsAttached)
            return;

        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("key"));
        var options = new CompilationOptions();
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();

        var first = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            options,
            ExecutionTargetIds.CSharpClr,
            TargetRenderProfile.ExecutionFast,
            firstCancellation.Token);
        var second = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            options,
            ExecutionTargetIds.CSharpClr,
            TargetRenderProfile.ExecutionFast,
            secondCancellation.Token);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void CanonicalExecutionContract_ShouldNotVaryWithInvocationToken()
    {
        if (Debugger.IsAttached)
            return;

        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("contract"));
        var options = new CompilationOptions();
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"CancellationContract_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options,
            firstCancellation.Token);

        try
        {
            Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
            Assert.IsNotNull(result.BuildItems);

            var first = InstanceCreator.CreateCanonicalExecutionContractForTests(
                result.BuildItems!,
                provider,
                firstCancellation.Token);
            var second = InstanceCreator.CreateCanonicalExecutionContractForTests(
                result.BuildItems!,
                provider,
                secondCancellation.Token);

            Assert.AreEqual(first, second);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private static SemanticTemplateCacheInput CreateSemanticCacheInput(
        string query,
        ISchemaProvider provider,
        CompilationOptions options)
    {
        return new SemanticTemplateCacheInput(
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
            typeof(SchemaRegistry).AssemblyQualifiedName!);
    }
}
