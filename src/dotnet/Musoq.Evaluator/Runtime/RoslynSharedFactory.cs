using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

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

    /// <summary>
    ///     Gets a workspace for the current thread. This workspace is reused across multiple compilations.
    /// </summary>
    public static AdhocWorkspace Workspace => ThreadLocalWorkspace.Value;

    /// <summary>
    ///     Gets a syntax generator for the current thread. This generator is reused across multiple compilations.
    /// </summary>
    public static SyntaxGenerator Generator => ThreadLocalGenerator.Value;

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
}
