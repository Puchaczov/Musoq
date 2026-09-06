using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CompileTimeCancellationArtifactTests
{
    private const string Query = "select i.Value from #cooperative.items() i";
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public async Task CreateExecutableFromArtifactWithDiagnostics_WhenValidationIsCancelled_ShouldRethrowOriginalCancellation()
    {
        var artifact = CompileArtifact();
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.TableLookup);

        var token = await CancelAtStageAsync(
            fixture,
            cancellationToken => InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
                Query,
                artifact,
                fixture.Provider,
                _loggerResolver,
                new CompiledQueryArtifactLoadOptions
                {
                    ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
                },
                fixture.CreateCompilationOptions(),
                cancellationToken));

        Assert.IsTrue(fixture.MetadataTokens.All(received => received == token));
        Assert.IsFalse(fixture.StageCompleted);
    }

    [TestMethod]
    public async Task CreateExecutableFromArtifactWithDiagnostics_WhenCustomLoaderIsCancelledAfterLoading_ShouldDisposeLifetimeOwner()
    {
        var artifact = CompileArtifact();
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        using var cancellation = new CancellationTokenSource();
        var loaderEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loaderRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TestLifetimeOwner? owner = null;
        CancellationToken? observedToken = null;

        var operation = Task.Run(() => InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            Query,
            artifact,
            fixture.Provider,
            _loggerResolver,
            CompiledQueryArtifactLoadOptions.Default,
            fixture.CreateCompilationOptions(),
            loader: (loadedArtifact, token) =>
            {
                observedToken = token;
                owner = new TestLifetimeOwner();
                var runnableType = Assembly.Load(loadedArtifact.AssemblyBytes)
                    .GetType(loadedArtifact.RunnableTypeName)!;
                loaderEntered.TrySetResult(true);
                loaderRelease.Task.GetAwaiter().GetResult();
                return new CompiledQueryArtifactLoadResult(runnableType, owner);
            },
            cancellation.Token));

        try
        {
            var firstCompleted = await Task.WhenAny(loaderEntered.Task, operation);
            Assert.AreSame(loaderEntered.Task, firstCompleted);
            Assert.IsFalse(operation.IsCompleted);

            var token = cancellation.Token;
            cancellation.Cancel();
            loaderRelease.TrySetResult(true);

            try
            {
                _ = await operation;
                Assert.Fail("The cancelled artifact load returned normally.");
            }
            catch (OperationCanceledException exception)
            {
                Assert.AreEqual(token, exception.CancellationToken);
            }
            Assert.AreEqual(token, observedToken);
            Assert.IsNotNull(owner);
            Assert.IsTrue(owner.Disposed);
        }
        finally
        {
            cancellation.Cancel();
            loaderRelease.TrySetResult(true);
            if (!operation.IsCompleted)
            {
                try
                {
                    await operation;
                }
                catch (OperationCanceledException exception)
                {
                    Assert.AreEqual(cancellation.Token, exception.CancellationToken);
                }
            }
        }
    }

    [TestMethod]
    public void LoadTypedArtifact_WhenCancelledAfterTypeLoading_ShouldDisposeLifetimeOwner()
    {
        var artifact = CompileTypedArtifact();
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        using var cancellation = new CancellationTokenSource();
        ClrAssemblyExecutableActivator.ClrLoadedRunnableType? loadedRunnableType = null;
        using var hook = ClrAssemblyExecutableActivator.SetRunnableTypeLoadedTestHookForTests(loaded =>
        {
            loadedRunnableType = loaded;
            cancellation.Cancel();
        });

        var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.LoadTypedArtifact<TypedOutput>(
            artifact,
            fixture.Provider,
            _loggerResolver,
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.IsNotNull(loadedRunnableType);
        Assert.IsFalse(loadedRunnableType.HasLifetimeOwner);
    }

    [TestMethod]
    public void CompiledArtifact_WhenCreatedWithCancellation_ShouldNotPersistCancellationStateInMetadata()
    {
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var firstArtifact = CompileArtifact(firstCancellation.Token, "CancellationArtifactMetadata");
        var secondArtifact = CompileArtifact(secondCancellation.Token, "CancellationArtifactMetadata");

        Assert.AreEqual(firstArtifact.Metadata.Count, secondArtifact.Metadata.Count);
        foreach (var entry in firstArtifact.Metadata)
        {
            Assert.IsTrue(secondArtifact.Metadata.TryGetValue(entry.Key, out var secondValue));
            Assert.AreEqual(entry.Value, secondValue);
        }

        Assert.IsFalse(firstArtifact.Metadata.Keys.Any(key =>
            key.Contains("Cancellation", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(firstArtifact.Metadata.Values.Any(value =>
            value.Contains("CancellationToken", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(firstArtifact.Metadata.Values.Any(value =>
            value.Contains("cooperative", StringComparison.OrdinalIgnoreCase)));
    }

    private ICompiledQueryArtifact CompileArtifact(
        CancellationToken cancellationToken = default,
        string? assemblyName = null)
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            Query,
            assemblyName ?? $"CancellationArtifact_{Guid.NewGuid():N}",
            fixture.Provider,
            _loggerResolver,
            fixture.CreateCompilationOptions(),
            cancellationToken);

        Assert.IsTrue(
            result.Succeeded,
            string.Join(
                Environment.NewLine,
                result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        return result.Artifact!;
    }

    private ICompiledTypedQueryArtifact CompileTypedArtifact()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        return InstanceCreator.CompileForTypedArtifact<TypedOutput>(
            "select i.Value as Value from #cooperative.items() i",
            $"TypedCancellationArtifact_{Guid.NewGuid():N}",
            fixture.Provider,
            _loggerResolver,
            fixture.CreateCompilationOptions());
    }

    public sealed record TypedOutput(string Value);

    private static async Task<CancellationToken> CancelAtStageAsync(
        CooperativeCompileTimeFixture fixture,
        Func<CancellationToken, object?> operation)
    {
        using var cancellation = new CancellationTokenSource();
        var operationTask = Task.Run(() => operation(cancellation.Token));

        try
        {
            var firstCompleted = await Task.WhenAny(fixture.Entered, operationTask);
            Assert.AreSame(fixture.Entered, firstCompleted);
            Assert.IsFalse(operationTask.IsCompleted);

            var token = cancellation.Token;
            cancellation.Cancel();

            try
            {
                _ = await operationTask;
                Assert.Fail("The cancelled artifact validation returned normally.");
            }
            catch (OperationCanceledException exception)
            {
                Assert.AreEqual(token, exception.CancellationToken);
            }
            return token;
        }
        finally
        {
            cancellation.Cancel();
            fixture.Release();
            if (!operationTask.IsCompleted)
            {
                try
                {
                    await operationTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}
