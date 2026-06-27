using System.Collections.Generic;
using System.Dynamic;
using System.IO;
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

/// <summary>
///     Provides shared instances of expensive Roslyn objects to avoid repeated initialization.
///     Thread-safe through lazy initialization and thread-local storage.
/// </summary>
public static class RoslynSharedFactory
{
    /// <summary>
    ///     Thread-local workspace to avoid contention. Each thread gets its own workspace.
    /// </summary>
    private static readonly ThreadLocal<AdhocWorkspace> ThreadLocalWorkspace =
        new(() => new AdhocWorkspace(), false);

    /// <summary>
    ///     Thread-local syntax generator matching the thread's workspace.
    /// </summary>
    private static readonly ThreadLocal<SyntaxGenerator> ThreadLocalGenerator =
        new(() => SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp), false);

    /// <summary>
    ///     Pre-built template compilation with all core references and options.
    ///     Creating CSharpCompilation + AddReferences + WithOptions is expensive;
    ///     by caching the template, per-query cost is just a cheap WithAssemblyName() call.
    /// </summary>
    private static readonly Lazy<CSharpCompilation> TemplateCompilation = new(CreateTemplateCompilation);

    private static readonly HashSet<string> PreloadedPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the set of assembly paths already included in the template compilation.
    ///     Populated during template creation; safe to read after first call to CreateCompilation.
    /// </summary>
    public static IReadOnlySet<string> PreloadedAssemblyPaths
    {
        get
        {
            _ = TemplateCompilation.Value;
            return PreloadedPaths;
        }
    }

    /// <summary>
    ///     Gets a workspace for the current thread. This workspace is reused across multiple compilations.
    /// </summary>
    public static AdhocWorkspace Workspace => ThreadLocalWorkspace.Value ??
                                              throw new InvalidOperationException("Roslyn workspace was not initialized for the current thread.");

    /// <summary>
    ///     Gets a syntax generator for the current thread. This generator is reused across multiple compilations.
    /// </summary>
    public static SyntaxGenerator Generator => ThreadLocalGenerator.Value ??
                                               throw new InvalidOperationException("Roslyn syntax generator was not initialized for the current thread.");

    /// <summary>
    ///     Creates a new CSharpCompilation with all common references already added.
    ///     Uses a pre-built template — the only per-query cost is WithAssemblyName().
    /// </summary>
    /// <param name="assemblyName">The name for the assembly.</param>
    /// <returns>A pre-configured CSharpCompilation.</returns>
    public static CSharpCompilation CreateCompilation(string assemblyName)
    {
        return TemplateCompilation.Value.WithAssemblyName(assemblyName);
    }

    private static CSharpCompilation CreateTemplateCompilation()
    {
        var compilation = CSharpCompilation.Create("__template__");
        compilation = compilation.AddReferences(RuntimeLibraries.References);

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

    private static List<MetadataReference> CollectCoreTypeReferences()
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

        if (File.Exists(abstractionDll) && PreloadedPaths.Add(abstractionDll))
            references.Add(MetadataReferenceCache.GetOrCreate(abstractionDll));

        foreach (var type in coreTypes)
        {
            var location = type.Assembly.Location;
            if (!string.IsNullOrEmpty(location) && PreloadedPaths.Add(location))
                references.Add(MetadataReferenceCache.GetOrCreate(location));
        }

        return references;
    }
}
