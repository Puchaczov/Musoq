using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ClrAssemblyExecutableActivatorTests
{
    [TestMethod]
    public void Resolve_WhenTargetIsCSharpClr_ShouldReturnClrAssemblyActivator()
    {
        var activator = ExecutionTargetCatalog.ResolveActivator(ExecutionTargetIds.CSharpClr);

        Assert.IsInstanceOfType<ClrAssemblyExecutableActivator>(activator);
    }

    [TestMethod]
    public void ActivateTable_WhenArtifactIsNotClrAssembly_ShouldReject()
    {
        var activator = new ClrAssemblyExecutableActivator();
        var artifact = new TestOnlyExecutableQueryArtifact("payload");

        var exception = Assert.Throws<InvalidOperationException>(
            () => activator.ActivateTable(artifact, CreateBinding()));

        Assert.Contains("CLR executable artifact", exception.Message);
        Assert.Contains("TestOnlyNonClr", exception.Message);
    }

    [TestMethod]
    public void LoadRunnableType_WhenArtifactIsLoadedClrArtifact_ShouldReturnLoadedType()
    {
        var activator = new ClrAssemblyExecutableActivator();
        var artifact = new ClrLoadedExecutableArtifact(typeof(object));

        var type = activator.LoadRunnableType(artifact);

        Assert.AreEqual(typeof(object), type);
    }

    [TestMethod]
    public void CreateLoadedExecutableArtifact_ShouldReturnCSharpClrLoadedArtifact()
    {
        var activator = new ClrAssemblyExecutableActivator();

        var artifact = activator.CreateLoadedExecutableArtifact(typeof(object));

        Assert.AreEqual(ExecutionTargetIds.CSharpClr, artifact.TargetId);
        var loadedArtifact = Assert.IsInstanceOfType<ClrLoadedExecutableArtifact>(artifact);
        Assert.AreEqual(typeof(object), loadedArtifact.RunnableType);
    }

    [TestMethod]
    public void CompileForExecutionBatch_WhenQueriesAreCompatible_ShouldShareContextAndKeepBindingsIndependent()
    {
        var results = CompileEntityBatch(
            ("first", "alpha"),
            ("second", "beta"));

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(static result => result.Result.Succeeded));

        var first = results[0].Result.CompiledQuery!;
        var second = results[1].Result.CompiledQuery!;
        try
        {
            var firstContext = GetGeneratedLoadContext(first);
            var secondContext = GetGeneratedLoadContext(second);

            Assert.AreSame(firstContext, secondContext);
            Assert.IsTrue(firstContext.IsCollectible);

            using var firstTable = first.Run();
            using var secondTable = second.Run();
            Assert.AreEqual("alpha", firstTable[0][0]);
            Assert.AreEqual("beta", secondTable[0][0]);
        }
        finally
        {
            first.Dispose();
            second.Dispose();
        }
    }

    [TestMethod]
    public void ActivateTableBatch_WhenOneTypeIsMissing_ShouldKeepSuccessfulSibling()
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            "select d.Dummy as Value from #system.dual() d",
            $"BatchActivation_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
        Assert.IsTrue(build.Succeeded);
        try
        {
            var items = build.BuildItems!;
            var activator = new ClrAssemblyExecutableActivator();
            var requests = new[]
            {
                new ClrBatchTableActivationRequest(items.AccessToClassPath, CreateBinding()),
                new ClrBatchTableActivationRequest("Missing.Batch.Runnable", CreateBinding())
            };

            var results = activator.ActivateTableBatch(items.ExecutableArtifact!, requests);

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[0].Succeeded);
            Assert.IsFalse(results[1].Succeeded);
            Assert.IsInstanceOfType<InvalidOperationException>(results[1].Exception);
            (results[0].Runnable as IDisposable)?.Dispose();
        }
        finally
        {
            build.CompiledQuery!.Dispose();
        }
    }

    [TestMethod]
    public void CompileForExecutionBatch_WhenQueriesDisposeInOrder_ShouldUnloadAfterLastLease()
    {
        var weakContext = CreateAndDisposeBatchQueries();

        ForceCollection(weakContext);

        Assert.IsFalse(weakContext.IsAlive, "The shared batch load context remained alive after its final lease was disposed.");
    }

    private static QueryRuntimeBinding CreateBinding()
    {
        return new QueryRuntimeBinding(
            new SystemSchemaProvider(),
            new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>(),
            new Dictionary<string, SourceExecutionPlan>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeBatchQueries()
    {
        var results = CompileEntityBatch(
            ("first", "alpha"),
            ("second", "beta"));
        var first = results[0].Result.CompiledQuery!;
        var second = results[1].Result.CompiledQuery!;
        var context = GetGeneratedLoadContext(first);
        Assert.AreSame(context, GetGeneratedLoadContext(second));
        var weakContext = new WeakReference(context);

        first.Dispose();
        Assert.IsTrue(weakContext.IsAlive);
        using (var table = second.Run())
            Assert.AreEqual("beta", table[0][0]);
        second.Dispose();

        return weakContext;
    }

    private static IReadOnlyList<ExecutionBatchCompilationResult> CompileEntityBatch(
        params (string Key, string Value)[] cases)
    {
        const string query = "select e.Name from #data.entities() e";
        var requests = cases
            .Select((item, index) => new ExecutionBatchCompilationRequest(
                item.Key,
                query,
                $"BatchContext_{index}_{Guid.NewGuid():N}",
                new EntitySetSchemaProvider(
                    new Dictionary<string, IReadOnlyList<EntitySetEntity>>(StringComparer.Ordinal)
                    {
                        ["#data"] = [new EntitySetEntity { Name = item.Value }]
                    }),
                new TestsLoggerResolver(),
                new CompilationOptions(),
                ConsumerFamily: "converter-activator-batch",
                ConsumerTestName: nameof(CompileEntityBatch),
                BatchOrigin: "converter-activator-batch"))
            .ToArray();

        return InstanceCreator.CompileForExecutionBatch(requests);
    }

    private static AssemblyLoadContext GetGeneratedLoadContext(CompiledQuery query)
    {
        var runnableField = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(runnableField);
        var runnable = (ITableRunnable)runnableField.GetValue(query)!;
        var currentType = runnable.GetType();
        PropertyInfo? innerProperty = null;
        while (currentType is not null && innerProperty is null)
        {
            innerProperty = currentType.GetProperty("Inner", BindingFlags.Instance | BindingFlags.NonPublic);
            currentType = currentType.BaseType;
        }

        Assert.IsNotNull(innerProperty);
        var inner = (ITableRunnable)innerProperty.GetValue(runnable)!;
        var context = AssemblyLoadContext.GetLoadContext(inner.GetType().Assembly);
        Assert.IsNotNull(context);
        return context;
    }

    private static void ForceCollection(WeakReference weakReference)
    {
        for (var attempt = 0; attempt < 10 && weakReference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(20);
        }
    }

    private sealed record TestOnlyExecutableQueryArtifact(string Payload)
        : ExecutableQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);
}
