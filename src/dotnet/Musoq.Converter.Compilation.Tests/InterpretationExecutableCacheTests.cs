using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class InterpretationExecutableCacheTests
{
    [TestMethod]
    public void CacheHit_ReusesLoadedArtifactWithFreshSchemaAndProvider()
    {
        var sql = CreateQuery();
        var firstProvider = Provider("  first  ");
        var secondProvider = Provider("  second  ");
        var semanticCount = SemanticTemplateCache.Snapshot.Count;
        var loads = 0;
        using var hook = ClrAssemblyExecutableActivator.SetRunnableTypeLoadedTestHookForTests(_ => loads++);

        var first = Compile(sql, firstProvider);
        using var firstQuery = first.CompiledQuery!;
        var second = Compile(sql, secondProvider);
        using var secondQuery = second.CompiledQuery!;

        Assert.AreEqual(1, loads);
        Assert.IsInstanceOfType<ClrLoadedExecutableArtifact>(second.BuildItems!.ExecutableArtifact);
        Assert.AreNotSame(first.BuildItems!.SchemaRegistry, second.BuildItems.SchemaRegistry);
        Assert.AreNotSame(
            first.BuildItems.SemanticArtifacts!.TransformedQueryTree,
            second.BuildItems.SemanticArtifacts!.TransformedQueryTree
        );
        Assert.AreEqual(semanticCount, SemanticTemplateCache.Snapshot.Count);
        Assert.IsFalse(
            InstanceCreator.HasExecutionCompilationCacheEntryForTests(sql, secondProvider, new CompilationOptions())
        );

        using var firstTable = firstQuery.Run();
        using var secondTable = secondQuery.Run();
        Assert.AreEqual("first", firstTable[0].ReadValue(0));
        Assert.AreEqual("second", secondTable[0].ReadValue(0));
    }

    [TestMethod]
    public void ChangedSchemaOrLimit_RequiresAnotherExecutable()
    {
        var sql = CreateQuery();
        var provider = Provider("  body  ");
        var loads = 0;
        using var hook = ClrAssemblyExecutableActivator.SetRunnableTypeLoadedTestHookForTests(_ => loads++);

        using var original = Compile(sql, provider).CompiledQuery!;
        using var changedSchema = Compile(
            sql.Replace("rest trim", "rest", StringComparison.Ordinal),
            provider
        ).CompiledQuery!;
        var limited = new CompilationOptions().WithInterpretationLimits(new InterpretationExecutionLimits(3));
        using var changedLimit = Compile(sql, provider, limited).CompiledQuery!;
        using var repeatedLimit = Compile(sql, provider, limited).CompiledQuery!;

        Assert.AreEqual(3, loads);
        using var originalTable = original.Run();
        using var changedTable = changedSchema.Run();
        Assert.AreEqual("body", originalTable[0].ReadValue(0));
        Assert.AreEqual("  body  ", changedTable[0].ReadValue(0));
    }

    [TestMethod]
    public void ChangedReferenceOrder_ChangesCanonicalIdentity()
    {
        var provider = Provider("value");
        var result = Compile(CreateQuery(), provider);
        using var query = result.CompiledQuery!;
        var items = result.BuildItems!;
        var original = InstanceCreator.CreateCanonicalExecutionContractForTests(items, provider);

        items.Compilation = items.Compilation.WithReferences(items.Compilation.References.Reverse());
        var reordered = InstanceCreator.CreateCanonicalExecutionContractForTests(items, provider);

        Assert.AreEqual(original.GeneratedSyntaxIdentity, reordered.GeneratedSyntaxIdentity);
        Assert.AreNotEqual(original.OrderedReferenceIdentities, reordered.OrderedReferenceIdentities);
    }

    [TestMethod]
    public async Task ConcurrentCompilations_ShareOnlyTheExecutable()
    {
        var sql = CreateQuery();
        var loads = 0;
        using var hook = ClrAssemblyExecutableActivator.SetRunnableTypeLoadedTestHookForTests(_ =>
            Interlocked.Increment(ref loads)
        );

        var values = await Task.WhenAll(
            Enumerable
                .Range(0, 4)
                .Select(index =>
                    Task.Run(() =>
                    {
                        using var query = Compile(sql, Provider($"value {index}")).CompiledQuery!;
                        using var table = query.Run();
                        return table[0].ReadValue(0);
                    })
                )
        );

        Assert.AreEqual(1, loads);
        CollectionAssert.AreEqual(new object[] { "value 0", "value 1", "value 2", "value 3" }, values);
    }

    [TestMethod]
    public void CancelledPublication_LeavesNoReusableArtifact()
    {
        var sql = CreateQuery();
        var provider = Provider("value");
        var loads = 0;
        using var hook = ClrAssemblyExecutableActivator.SetRunnableTypeLoadedTestHookForTests(_ => loads++);
        using var cancellation = new CancellationTokenSource();
        using (InstanceCreator.SetExecutionCompilationCommitHookForTests(cancellation.Cancel))
        {
            var exception = Assert.Throws<OperationCanceledException>(() =>
                InstanceCreator.CompileWithDiagnostics(
                    sql,
                    "CancelledInterpretation",
                    provider,
                    new TestsLoggerResolver(),
                    new CompilationOptions(),
                    cancellation.Token
                )
            );
            Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        }

        using var fresh = Compile(sql, provider).CompiledQuery!;
        using var hit = Compile(sql, provider).CompiledQuery!;
        Assert.AreEqual(2, loads);
    }

    [TestMethod]
    public void EvictedArtifact_RemainsAvailableToAnActiveQuery()
    {
        using var capacity = InstanceCreator.SetExecutionCompilationCacheLimitForTests(1);
        var sql = CreateQuery();
        var provider = Provider("  active  ");
        var loads = 0;
        using var hook = ClrAssemblyExecutableActivator.SetRunnableTypeLoadedTestHookForTests(_ => loads++);

        using var active = Compile(sql, provider).CompiledQuery!;
        using var other = Compile(CreateQuery(), provider).CompiledQuery!;
        using var recompiled = Compile(sql, provider).CompiledQuery!;

        Assert.AreEqual(3, loads);
        using var table = active.Run();
        Assert.AreEqual("active", table[0].ReadValue(0));
    }

    private static string CreateQuery() =>
        $$"""
            text Message { Value: rest trim };
            select p.Value, '{{Guid.NewGuid():N}}' as Marker
            from #artifact.items() i cross apply Parse<Message>(i.Value) p
            """;

    private static ISchemaProvider Provider(string value) => new ArtifactSchemaProvider(new ArtifactSchema(value));

    private static BuildResult Compile(string sql, ISchemaProvider provider, CompilationOptions? options = null)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            sql,
            $"Interpretation_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options ?? new CompilationOptions()
        );
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
        return result;
    }
}
