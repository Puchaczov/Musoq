using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CompileTimeCancellationCacheTests
{
    [TestMethod]
    public void SemanticCache_WhenOwnerCancelsBeforeFinalCommit_ShouldNotPublishOrBlockNextOwner()
    {
        var query = $"select d.Dummy from #system.dual() d where d.Dummy = '{Guid.NewGuid():N}'";
        var provider = new SystemSchemaProvider();
        var key = CreateSemanticCacheKey(query, provider, new CompilationOptions());
        using var cancellation = new CancellationTokenSource();
        var loggerResolver = new CancellingLoggerResolver(cancellation);

        var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.CompileWithDiagnostics(
            query,
            $"SemanticCancellation_{Guid.NewGuid():N}",
            provider,
            loggerResolver,
            new CompilationOptions(),
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.IsFalse(SemanticTemplateCache.TryGet(key, CancellationToken.None, out _));

        using var nextOwner = SemanticTemplateCache.Acquire(key, CancellationToken.None);
    }

    [TestMethod]
    public void SemanticCache_WhenCancelledWorkFollowsExistingEntry_ShouldPreserveEntry()
    {
        var query = $"select i.Value from #artifact.items() i where i.Value = '{Guid.NewGuid():N}'";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("same"));
        var options = new CompilationOptions();
        var key = CreateSemanticCacheKey(query, provider, options);

        var initial = InstanceCreator.CompileWithDiagnostics(
            query,
            $"SemanticCancellationExisting_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options);
        try
        {
            Assert.IsTrue(initial.Succeeded, string.Join(Environment.NewLine, initial.Errors));
        }
        finally
        {
            initial.CompiledQuery?.Dispose();
        }

        Assert.IsTrue(SemanticTemplateCache.TryGet(key, CancellationToken.None, out _),
            $"Semantic cache entry was not available (count={SemanticTemplateCache.Snapshot.Count}).");

        using var cancellation = new CancellationTokenSource();
        var loggerResolver = new CancellingLoggerResolver(cancellation);
        var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.CompileWithDiagnostics(
            query,
            $"SemanticCancellationExistingCancelled_{Guid.NewGuid():N}",
            provider,
            loggerResolver,
            options,
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.IsTrue(SemanticTemplateCache.TryGet(key, CancellationToken.None, out _));

        var next = InstanceCreator.CompileWithDiagnostics(
            query,
            $"SemanticCancellationExistingNext_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options);
        try
        {
            Assert.IsTrue(next.Succeeded, string.Join(Environment.NewLine, next.Errors));
        }
        finally
        {
            next.CompiledQuery?.Dispose();
        }
    }

    [TestMethod]
    public void ExecutionCache_WhenOwnerCancelsBeforeCommit_ShouldNotPublishEntry()
    {
        if (System.Diagnostics.Debugger.IsAttached)
            return;

        var query = $"select d.Dummy from #system.dual() d where d.Dummy = '{Guid.NewGuid():N}'";
        var provider = new SystemSchemaProvider();
        var options = new CompilationOptions();
        using var cancellation = new CancellationTokenSource();
        using var commitHook = InstanceCreator.SetExecutionCompilationCommitHookForTests(cancellation.Cancel);

        var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.CompileWithDiagnostics(
            query,
            $"ExecutionCancellation_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options,
            cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.IsFalse(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, options));
    }

    [TestMethod]
    public void ExecutionCache_WhenCanonicalAliasOwnerCancelsBeforeCommit_ShouldPreserveExistingEntry()
    {
        if (System.Diagnostics.Debugger.IsAttached)
            return;

        var suffix = Guid.NewGuid().ToString("N");
        const string firstQueryFormat = "select i.Value from #artifact.items() i where i.Value = '{0}'";
        const string secondQueryFormat = "select  i.Value  from  #artifact.items()  i  where  i.Value = '{0}'";
        var firstQuery = string.Format(firstQueryFormat, suffix);
        var secondQuery = string.Format(secondQueryFormat, suffix);
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("canonical-cancellation"));
        var options = new CompilationOptions();
        var logger = new TestsLoggerResolver();

        var first = InstanceCreator.CompileWithDiagnostics(
            firstQuery,
            $"CanonicalCancellationFirst_{suffix}",
            provider,
            logger,
            options);
        try
        {
            Assert.IsTrue(first.Succeeded, string.Join(Environment.NewLine, first.Errors));
            Assert.IsNotNull(first.BuildItems);
            Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(firstQuery, provider, options));
            var firstEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems, provider);
            Assert.AreNotEqual(0, firstEntry);

            using var cancellation = new CancellationTokenSource();
            using var commitHook = InstanceCreator.SetExecutionCompilationCommitHookForTests(cancellation.Cancel);
            var exception = Assert.Throws<OperationCanceledException>(() => InstanceCreator.CompileWithDiagnostics(
                secondQuery,
                $"CanonicalCancellationSecond_{suffix}",
                provider,
                logger,
                options,
                cancellation.Token));

            Assert.AreEqual(cancellation.Token, exception.CancellationToken);
            Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(firstQuery, provider, options));
            Assert.IsFalse(InstanceCreator.HasExecutionCompilationCacheEntryForTests(secondQuery, provider, options));
            Assert.AreEqual(firstEntry, InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems, provider));
        }
        finally
        {
            first.CompiledQuery?.Dispose();
        }
    }

    [TestMethod]
    public void ExecutionCache_WhenEvictingEntries_ShouldDisposeArtifactsOnce()
    {
        if (System.Diagnostics.Debugger.IsAttached)
            return;

        InstanceCreator.ClearExecutionCompilationCacheForTests();
        using var limit = InstanceCreator.SetExecutionCompilationCacheLimitForTests(1);
        var provider = new SystemSchemaProvider();
        var options = new CompilationOptions();
        var firstQuery = $"cache-disposal-first-{Guid.NewGuid():N}";
        var secondQuery = $"cache-disposal-second-{Guid.NewGuid():N}";
        var firstArtifact = new TrackingExecutableArtifact();
        var secondArtifact = new TrackingExecutableArtifact();
        IDisposable? reader = null;

        try
        {
            InstanceCreator.SeedExecutionCompilationCacheForTests(
                firstQuery,
                provider,
                options,
                firstArtifact,
                "first");
            reader = InstanceCreator.AcquireExecutionCompilationReaderForTests(firstQuery, provider, options);
            Assert.IsNotNull(reader);

            InstanceCreator.SeedExecutionCompilationCacheForTests(
                secondQuery,
                provider,
                options,
                secondArtifact,
                "second");

            Assert.AreEqual(0, firstArtifact.DisposeCount);
            reader.Dispose();
            reader = null;
            Assert.AreEqual(1, firstArtifact.DisposeCount);
        }
        finally
        {
            reader?.Dispose();
            InstanceCreator.ClearExecutionCompilationCacheForTests();
            Assert.AreEqual(1, firstArtifact.DisposeCount);
            Assert.AreEqual(1, secondArtifact.DisposeCount);
        }
    }

    [TestMethod]
    public void ProviderSignature_WhenDictionaryTraversalIsCancelled_ShouldRethrowOriginalCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var values = new CancellingDictionary(cancellation);
        values.Add("first", "value");
        values.Add("second", "value");
        var provider = new SignatureSchemaProvider(values);

        var exception = Assert.Throws<OperationCanceledException>(() =>
            InstanceCreator.CreateProviderSignatureForTests(provider, cancellation.Token));

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
    }

    [TestMethod]
    public async Task SemanticCache_WhenWaiterIsCancelled_ShouldLeaveOwningFlightUsable()
    {
        var key = CreateSemanticCacheKey();
        using var owner = SemanticTemplateCache.Acquire(key, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var waiterRegistered = NewSignal();
        var waiter = Task.Run(() => SemanticTemplateCache.AcquireForTests(
            key,
            () => waiterRegistered.TrySetResult(true),
            cancellation.Token));

        await waiterRegistered.Task;
        var token = cancellation.Token;
        cancellation.Cancel();

        var exception = await AwaitCancellationAsync(waiter);
        Assert.AreEqual(token, exception.CancellationToken);

        owner.Dispose();
        using var nextOwner = SemanticTemplateCache.Acquire(key, CancellationToken.None);
    }

    [TestMethod]
    public async Task ExecutionCache_WhenWaiterIsCancelled_ShouldLeaveOwningFlightUsable()
    {
        using var fixture = new CooperativeCompileTimeFixture(CooperativeCompileTimeStage.None);
        var script = $"select Value from #cooperative.items()_{Guid.NewGuid():N}";
        var options = new CompilationOptions();
        using var owner = InstanceCreator.AcquireExecutionCompilationFlightForTests(
            script,
            fixture.Provider,
            options,
            static () => { },
            CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var waiterRegistered = NewSignal();
        var waiter = Task.Run(() => InstanceCreator.AcquireExecutionCompilationFlightForTests(
            script,
            fixture.Provider,
            options,
            () => waiterRegistered.TrySetResult(true),
            cancellation.Token));

        await waiterRegistered.Task;
        var token = cancellation.Token;
        cancellation.Cancel();

        var exception = await AwaitCancellationAsync(waiter);
        Assert.AreEqual(token, exception.CancellationToken);

        owner.Dispose();
        using var nextOwner = InstanceCreator.AcquireExecutionCompilationFlightForTests(
            script,
            fixture.Provider,
            options,
            static () => { },
            CancellationToken.None);
    }

    [TestMethod]
    public async Task CanonicalCache_WhenWaiterIsCancelled_ShouldLeaveOwningFlightUsable()
    {
        var contract = CreateCanonicalContract();
        using var owner = InstanceCreator.AcquireCanonicalExecutionCompilationFlightForTests(
            contract,
            static () => { },
            CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var waiterRegistered = NewSignal();
        var waiter = Task.Run(() => InstanceCreator.AcquireCanonicalExecutionCompilationFlightForTests(
            contract,
            () => waiterRegistered.TrySetResult(true),
            cancellation.Token));

        await waiterRegistered.Task;
        var token = cancellation.Token;
        cancellation.Cancel();

        var exception = await AwaitCancellationAsync(waiter);
        Assert.AreEqual(token, exception.CancellationToken);

        owner.Dispose();
        using var nextOwner = InstanceCreator.AcquireCanonicalExecutionCompilationFlightForTests(
            contract,
            static () => { },
            CancellationToken.None);
    }

    private static TaskCompletionSource<bool> NewSignal()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static async Task<OperationCanceledException> AwaitCancellationAsync(
        Task<IDisposable> operation)
    {
        try
        {
            _ = await operation;
            Assert.Fail("The cancelled cache waiter returned normally.");
        }
        catch (OperationCanceledException exception)
        {
            return exception;
        }

        throw new AssertFailedException("The cancelled cache waiter did not throw.");
    }

    private static SemanticTemplateCacheKey CreateSemanticCacheKey(
        string? query = null,
        ISchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        if (provider is not null)
        {
            return SemanticTemplateCache.CreateKey(new SemanticTemplateCacheInput(
                query ?? throw new ArgumentNullException(nameof(query)),
                provider,
                options ?? new CompilationOptions(),
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
                typeof(SchemaRegistry).AssemblyQualifiedName!))!.Value;
        }

        return new SemanticTemplateCacheKey(
            query ?? $"cache-flight-{Guid.NewGuid():N}",
            "parser",
            "runtime",
            "semantics",
            provider?.GetType().AssemblyQualifiedName ?? "provider",
            provider is null
                ? "provider-contract"
                : InstanceCreator.CreateSemanticProviderContractSignatureForCache(provider),
            options is null ? "options" : CompilationOptionsFingerprint.Compute(options),
            ExecutionTargetIds.CSharpClr,
            QueryResultMode.Table,
            string.Empty,
            string.Empty,
            "registry");
    }

    private sealed class CancellingLoggerResolver(CancellationTokenSource cancellation) : ILoggerResolver
    {
        public ILogger ResolveLogger()
        {
            cancellation.Cancel();
            return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }

        public ILogger<T> ResolveLogger<T>()
        {
            return Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
        }
    }

    private sealed record TrackingExecutableArtifact()
        : ExecutableQueryArtifact(ExecutionTargetIds.CSharpClr), IDisposable
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
        }
    }

    private sealed class SignatureSchemaProvider(CancellingDictionary values) : ISchemaProvider
    {
        private readonly CancellingDictionary _values = values;

        public ISchema GetSchema(string schema)
        {
            throw new NotSupportedException(schema);
        }
    }

    private sealed class CancellingDictionary(CancellationTokenSource cancellation) : IDictionary
    {
        private readonly Hashtable _values = new();
        private readonly CancellationTokenSource _cancellation = cancellation;

        public object? this[object key]
        {
            get => _values[key];
            set => _values[key] = value;
        }

        public ICollection Keys => _values.Keys;

        public ICollection Values => _values.Values;

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public int Count => _values.Count;

        public object SyncRoot => _values.SyncRoot;

        public bool IsSynchronized => false;

        public void Add(object key, object? value)
        {
            _values.Add(key, value);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Contains(object key)
        {
            return _values.Contains(key);
        }

        public void CopyTo(Array array, int index)
        {
            _values.CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new CancellingEnumerator(_values.GetEnumerator(), _cancellation);
        }

        public void Remove(object key)
        {
            _values.Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class CancellingEnumerator(
        IDictionaryEnumerator inner,
        CancellationTokenSource cancellation) : IDictionaryEnumerator
    {
        public object Key => inner.Key;

        public object? Value => inner.Value;

        public DictionaryEntry Entry => inner.Entry;

        public object Current => inner.Current;

        public bool MoveNext()
        {
            cancellation.Cancel();
            return inner.MoveNext();
        }

        public void Reset()
        {
            inner.Reset();
        }
    }

    private static CanonicalExecutionArtifactContract CreateCanonicalContract()
    {
        var identity = new CSharpGeneratedSyntaxIdentity("cache-flight", "cache-flight");
        return new CanonicalExecutionArtifactContract(
            identity,
            "semantic",
            "runtime",
            "execution",
            ExecutionTargetIds.CSharpClr.ToString(),
            TargetRenderProfile.ExecutionFast.ToString(),
            TargetRenderProfileContract.Version,
            QueryResultMode.Table.ToString(),
            string.Empty,
            "options",
            "references",
            "provider",
            "interpreter");
    }
}
