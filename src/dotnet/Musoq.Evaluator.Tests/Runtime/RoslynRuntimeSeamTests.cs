using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class RoslynRuntimeSeamTests
{
    [TestMethod]
    public void MetadataReferenceCache_WhenSamePathIsRequestedTwice_ShouldReuseReference()
    {
        var cache = new DefaultMetadataReferenceCache();
        var assemblyPath = typeof(object).Assembly.Location;

        var first = cache.GetOrCreate(assemblyPath);
        var second = cache.GetOrCreate(assemblyPath);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, cache.Count);

        cache.Clear();

        Assert.AreEqual(0, cache.Count);
    }

    [TestMethod]
    public void MetadataReferenceCache_WhenFileIdentityChanges_ShouldCreateNewReference()
    {
        using var directory = new TempDirectory();
        var assemblyPath = directory.CopyFile("changed.dll", typeof(object).Assembly.Location);
        var cache = new DefaultMetadataReferenceCache();

        var first = cache.GetOrCreate(assemblyPath);
        File.SetLastWriteTimeUtc(assemblyPath, File.GetLastWriteTimeUtc(assemblyPath).AddMinutes(1));

        var second = cache.GetOrCreate(assemblyPath);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void MetadataReferenceCache_WhenBoundIsReached_ShouldEvictOldestReference()
    {
        var cache = new DefaultMetadataReferenceCache(1);
        var first = cache.GetOrCreate(typeof(object).Assembly.Location);

        _ = cache.GetOrCreate(typeof(Enumerable).Assembly.Location);
        var reloadedFirst = cache.GetOrCreate(typeof(object).Assembly.Location);

        Assert.AreEqual(1, cache.Count);
        Assert.AreNotSame(first, reloadedFirst);
    }

    [TestMethod]
    public void MetadataReferenceCache_WhenUsedConcurrently_ShouldCreateOneReferencePerIdentity()
    {
        var cache = new DefaultMetadataReferenceCache();
        var references = new ConcurrentBag<MetadataReference>();

        Parallel.For(0, 32, _ => references.Add(cache.GetOrCreate(typeof(object).Assembly.Location)));

        var first = references.First();
        Assert.IsTrue(references.All(reference => ReferenceEquals(first, reference)));
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void MetadataReferenceCache_WhenInstancesAreSeparate_ShouldNotShareEntries()
    {
        var firstCache = MetadataReferenceCache.CreateScoped();
        var secondCache = MetadataReferenceCache.CreateScoped();

        var first = firstCache.GetOrCreate(typeof(object).Assembly.Location);
        var second = secondCache.GetOrCreate(typeof(object).Assembly.Location);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(1, firstCache.Count);
        Assert.AreEqual(1, secondCache.Count);
    }

    [TestMethod]
    public void RuntimeReferenceProvider_WhenAssembliesAreMissingOrBad_ShouldSkipInvalidReferences()
    {
        using var directory = new TempDirectory();
        directory.CreateFile("System.Runtime.dll");
        directory.CreateFile("System.Collections.dll");
        var goodReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var cache = new DelegateMetadataReferenceCache(path =>
        {
            if (Path.GetFileName(path).Equals("System.Collections.dll", StringComparison.OrdinalIgnoreCase))
                throw new BadImageFormatException("Not a managed assembly.");

            return goodReference;
        });
        var provider = new RuntimeReferenceProvider(
            cache,
            () => directory.DirectoryPath,
            ["System.Runtime.dll", "System.Collections.dll", "System.Linq.dll"]);

        var references = provider.References;

        Assert.HasCount(1, references);
        Assert.AreSame(goodReference, references[0]);
        Assert.AreEqual(1, cache.RequestCount("System.Runtime.dll"));
        Assert.AreEqual(1, cache.RequestCount("System.Collections.dll"));
        Assert.AreEqual(0, cache.RequestCount("System.Linq.dll"));
    }

    [TestMethod]
    public void RuntimeReferenceProvider_WhenReferencesAreRequestedConcurrently_ShouldLoadOnce()
    {
        using var directory = new TempDirectory();
        directory.CreateFile("System.Runtime.dll");
        directory.CreateFile("System.Collections.dll");
        var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var cache = new DelegateMetadataReferenceCache(_ =>
        {
            Thread.Sleep(20);
            return reference;
        });
        var provider = new RuntimeReferenceProvider(
            cache,
            () => directory.DirectoryPath,
            ["System.Runtime.dll", "System.Collections.dll"]);
        var results = new ConcurrentBag<MetadataReference[]>();

        Parallel.For(0, 16, _ => results.Add(provider.References));

        var first = results.First();
        Assert.AreEqual(2, first.Length);
        Assert.IsTrue(results.Skip(1).All(result => !ReferenceEquals(first, result)));
        Assert.IsTrue(results.All(result =>
            result.Length == first.Length &&
            result.Zip(first).All(static pair => ReferenceEquals(pair.First, pair.Second))));
        Assert.AreEqual(1, cache.RequestCount("System.Runtime.dll"));
        Assert.AreEqual(1, cache.RequestCount("System.Collections.dll"));
    }

    [TestMethod]
    public void RoslynCompilationFactory_WhenCompilationIsCreatedRepeatedly_ShouldReuseTemplateReferences()
    {
        var runtimeProvider = new CountingRuntimeReferenceProvider(
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        using var factory = new RoslynCompilationFactory(runtimeProvider, new DefaultMetadataReferenceCache());

        var first = factory.CreateCompilation("first");
        var second = factory.CreateCompilation("second");
        var preloadedPaths = factory.PreloadedAssemblyPaths;

        Assert.AreEqual("first", first.AssemblyName);
        Assert.AreEqual("second", second.AssemblyName);
        Assert.AreNotSame(first, second);
        Assert.AreEqual(1, runtimeProvider.ReferencesAccessCount);
        Assert.IsTrue(preloadedPaths.Count > 0);
    }

    [TestMethod]
    public void EvaluatorRuntimeEnvironment_WhenReferenceArrayIsMutated_ShouldKeepOwnedReferencesIsolated()
    {
        using var environment = new EvaluatorRuntimeEnvironment();

        var copy = environment.References;
        Assert.IsNotEmpty(copy);
        var replacement = MetadataReference.CreateFromFile(typeof(string).Assembly.Location);
        copy[0] = replacement;

        var freshCopy = environment.References;

        Assert.AreNotSame(replacement, freshCopy[0]);
    }

    [TestMethod]
    public void EvaluatorRuntimeEnvironment_WhenInstancesAreSeparate_ShouldNotShareRuntimeReferences()
    {
        using var firstEnvironment = new EvaluatorRuntimeEnvironment();
        using var secondEnvironment = new EvaluatorRuntimeEnvironment();

        var first = firstEnvironment.GetOrCreateMetadataReference(typeof(object).Assembly.Location);
        var second = secondEnvironment.GetOrCreateMetadataReference(typeof(object).Assembly.Location);

        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void EvaluatorRuntimeEnvironment_WhenDisposed_ShouldRejectRoslynAccess()
    {
        var environment = new EvaluatorRuntimeEnvironment();
        _ = environment.Workspace;
        _ = environment.Generator;
        _ = environment.CreateCompilation("before-dispose");

        environment.Dispose();
        environment.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = environment.References);
        Assert.Throws<ObjectDisposedException>(() => _ = environment.Workspace);
        Assert.Throws<ObjectDisposedException>(() => _ = environment.Generator);
        Assert.Throws<ObjectDisposedException>(() => environment.CreateCompilation("after-dispose"));
        Assert.Throws<ObjectDisposedException>(() => environment.GetOrCreateMetadataReference(typeof(object).Assembly.Location));
    }

    [TestMethod]
    [DoNotParallelize]
    public void RuntimeLibraries_WhenDefaultEnvironmentIsDisposed_ShouldRejectAccessUntilReset()
    {
        RuntimeLibraries.ResetDefaultEnvironment();

        try
        {
            RuntimeLibraries.DisposeDefaultEnvironment();

            Assert.Throws<ObjectDisposedException>(() => RuntimeLibraries.CreateReferences());
            Assert.Throws<ObjectDisposedException>(() => _ = RoslynSharedFactory.Workspace);
            Assert.Throws<ObjectDisposedException>(() => _ = MetadataReferenceCache.Count);
        }
        finally
        {
            RuntimeLibraries.ResetDefaultEnvironment();
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void RuntimeLibraries_WhenResetConcurrentlyWithStaticOperations_ShouldRemainSafe()
    {
        RuntimeLibraries.ResetDefaultEnvironment();
        var failures = new ConcurrentQueue<Exception>();

        try
        {
            Parallel.Invoke(
                () =>
                {
                    for (var index = 0; index < 8; index++)
                        RuntimeLibraries.ResetDefaultEnvironment();
                },
                () =>
                {
                    for (var index = 0; index < 32; index++)
                    {
                        try
                        {
                            RuntimeLibraries.CreateReferences();
                            _ = RoslynSharedFactory.CreateCompilation($"concurrent-{index}");
                            _ = MetadataReferenceCache.Count;
                        }
                        catch (Exception exception)
                        {
                            failures.Enqueue(exception);
                        }
                    }
                });

            Assert.IsEmpty(failures);
        }
        finally
        {
            RuntimeLibraries.ResetDefaultEnvironment();
        }
    }

    [TestMethod]
    public void ParameterSnapshot_MutableDictionaryContracts_ShouldReturnMutableCopies()
    {
        var empty = ParameterSnapshot.EmptyDictionary;
        empty["value"] = 1;
        Assert.AreEqual(1, empty["value"]);

        var snapshot = ParameterSnapshot.CaptureDictionaryOrEmpty(
            new Dictionary<string, object?> { ["value"] = 2 });
        snapshot["value"] = 3;

        Assert.AreEqual(3, snapshot["value"]);
    }

    [TestMethod]
    public void RoslynCompilationFactory_WhenDisposed_ShouldDisposeThreadLocalWorkspaces()
    {
        var factory = new RoslynCompilationFactory(
            new RuntimeReferenceProvider(new DefaultMetadataReferenceCache()),
            new DefaultMetadataReferenceCache());
        _ = factory.Workspace;
        _ = factory.Generator;

        factory.Dispose();
        factory.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = factory.Workspace);
        Assert.Throws<ObjectDisposedException>(() => _ = factory.Generator);
        Assert.Throws<ObjectDisposedException>(() => factory.CreateCompilation("after-dispose"));
    }

    private sealed class DelegateMetadataReferenceCache(Func<string, MetadataReference> getOrCreate) : IMetadataReferenceCache
    {
        private readonly ConcurrentDictionary<string, int> _requests = new(StringComparer.OrdinalIgnoreCase);

        public int Count => _requests.Count;

        public MetadataReference GetOrCreate(string assemblyPath)
        {
            _requests.AddOrUpdate(assemblyPath, 1, static (_, count) => count + 1);

            return getOrCreate(assemblyPath);
        }

        public void Clear()
        {
            _requests.Clear();
        }

        public int RequestCount(string assemblyName)
        {
            return _requests
                .Where(pair => Path.GetFileName(pair.Key).Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                .Sum(static pair => pair.Value);
        }
    }

    private sealed class CountingRuntimeReferenceProvider(MetadataReference[] references) : IRuntimeReferenceProvider
    {
        private int _referencesAccessCount;

        public int ReferencesAccessCount => _referencesAccessCount;

        public MetadataReference[] References
        {
            get
            {
                Interlocked.Increment(ref _referencesAccessCount);

                return references;
            }
        }

        public void CreateReferences()
        {
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "Musoq.Evaluator.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void CreateFile(string fileName)
        {
            File.WriteAllText(Path.Combine(DirectoryPath, fileName), "not a managed assembly");
        }

        public string CopyFile(string fileName, string sourcePath)
        {
            var destinationPath = Path.Combine(DirectoryPath, fileName);
            File.Copy(sourcePath, destinationPath);
            return destinationPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
