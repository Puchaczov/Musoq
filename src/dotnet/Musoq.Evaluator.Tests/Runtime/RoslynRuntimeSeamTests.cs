using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        Assert.IsTrue(results.All(result => ReferenceEquals(first, result)));
        Assert.AreEqual(1, cache.RequestCount("System.Runtime.dll"));
        Assert.AreEqual(1, cache.RequestCount("System.Collections.dll"));
    }

    [TestMethod]
    public void RoslynCompilationFactory_WhenCompilationIsCreatedRepeatedly_ShouldReuseTemplateReferences()
    {
        var runtimeProvider = new CountingRuntimeReferenceProvider(
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var factory = new RoslynCompilationFactory(runtimeProvider, new DefaultMetadataReferenceCache());

        var first = factory.CreateCompilation("first");
        var second = factory.CreateCompilation("second");
        var preloadedPaths = factory.PreloadedAssemblyPaths;

        Assert.AreEqual("first", first.AssemblyName);
        Assert.AreEqual("second", second.AssemblyName);
        Assert.AreNotSame(first, second);
        Assert.AreEqual(1, runtimeProvider.ReferencesAccessCount);
        Assert.IsTrue(preloadedPaths.Count > 0);
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

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
