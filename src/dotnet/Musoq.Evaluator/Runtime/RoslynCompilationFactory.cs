using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes.From;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Runtime;

internal sealed class RoslynCompilationFactory : ICSharpCompilationFactory
{
    private readonly IRuntimeReferenceProvider _runtimeReferenceProvider;
    private readonly IMetadataReferenceCache _referenceCache;
    private readonly ThreadLocal<AdhocWorkspace> _threadLocalWorkspace;
    private readonly ThreadLocal<SyntaxGenerator> _threadLocalGenerator;
    private readonly Lazy<CSharpCompilation> _templateCompilation;
    private readonly HashSet<string> _preloadedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _preloadedPathsGate = new();

    public RoslynCompilationFactory(
        IRuntimeReferenceProvider runtimeReferenceProvider,
        IMetadataReferenceCache referenceCache)
    {
        _runtimeReferenceProvider = runtimeReferenceProvider ?? throw new ArgumentNullException(nameof(runtimeReferenceProvider));
        _referenceCache = referenceCache ?? throw new ArgumentNullException(nameof(referenceCache));
        _threadLocalWorkspace = new ThreadLocal<AdhocWorkspace>(() => new AdhocWorkspace(), false);
        _threadLocalGenerator = new ThreadLocal<SyntaxGenerator>(
            () => SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp),
            false);
        _templateCompilation = new Lazy<CSharpCompilation>(CreateTemplateCompilation);
    }

    public IReadOnlySet<string> PreloadedAssemblyPaths
    {
        get
        {
            _ = _templateCompilation.Value;
            lock (_preloadedPathsGate)
            {
                return _preloadedPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public AdhocWorkspace Workspace => _threadLocalWorkspace.Value ??
                                       throw new InvalidOperationException("Roslyn workspace was not initialized for the current thread.");

    public SyntaxGenerator Generator => _threadLocalGenerator.Value ??
                                        throw new InvalidOperationException("Roslyn syntax generator was not initialized for the current thread.");

    public CSharpCompilation CreateCompilation(string assemblyName)
    {
        return _templateCompilation.Value.WithAssemblyName(assemblyName);
    }

    private CSharpCompilation CreateTemplateCompilation()
    {
        var compilation = CSharpCompilation.Create("__template__");
        compilation = compilation.AddReferences(_runtimeReferenceProvider.References);

        var coreReferences = CollectCoreTypeReferences();
        if (coreReferences.Count > 0)
            compilation = compilation.AddReferences(coreReferences);

        return compilation.WithOptions(
            new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                    deterministic: false)
                .WithConcurrentBuild(false)
                .WithMetadataImportOptions(MetadataImportOptions.Public)
                .WithPlatform(Platform.AnyCpu));
    }

    private List<MetadataReference> CollectCoreTypeReferences()
    {
        var coreTypes = new[]
        {
            typeof(object),
            typeof(CancellationToken),
            typeof(ISchema),
            typeof(LibraryBase),
            typeof(Table),
            typeof(SyntaxFactory),
            typeof(ExpandoObject),
            typeof(SchemaFromNode),
            typeof(ILogger)
        };

        var references = new List<MetadataReference>(coreTypes.Length + 2);

        var abstractionDll = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Microsoft.Extensions.Logging.Abstractions.dll");

        if (File.Exists(abstractionDll) && AddPreloadedPath(abstractionDll))
            references.Add(_referenceCache.GetOrCreate(abstractionDll));

        foreach (var type in coreTypes)
        {
            var location = type.Assembly.Location;
            if (!string.IsNullOrEmpty(location) && AddPreloadedPath(location))
                references.Add(_referenceCache.GetOrCreate(location));
        }

        return references;
    }

    private bool AddPreloadedPath(string path)
    {
        lock (_preloadedPathsGate)
        {
            return _preloadedPaths.Add(path);
        }
    }
}
