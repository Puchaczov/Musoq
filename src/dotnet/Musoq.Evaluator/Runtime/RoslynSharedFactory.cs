using System.Collections.Generic;
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
    internal static ICSharpCompilationFactory Default { get; } =
        new RoslynCompilationFactory(RuntimeLibraries.Default, MetadataReferenceCache.Default);

    /// <summary>
    ///     Gets the set of assembly paths already included in the template compilation.
    ///     Populated during template creation; safe to read after first call to CreateCompilation.
    /// </summary>
    public static IReadOnlySet<string> PreloadedAssemblyPaths => Default.PreloadedAssemblyPaths;

    /// <summary>
    ///     Gets a workspace for the current thread. This workspace is reused across multiple compilations.
    /// </summary>
    public static AdhocWorkspace Workspace => Default.Workspace;

    /// <summary>
    ///     Gets a syntax generator for the current thread. This generator is reused across multiple compilations.
    /// </summary>
    public static SyntaxGenerator Generator => Default.Generator;

    /// <summary>
    ///     Creates a new CSharpCompilation with all common references already added.
    ///     Uses a pre-built template - the only per-query cost is WithAssemblyName().
    /// </summary>
    /// <param name="assemblyName">The name for the assembly.</param>
    /// <returns>A pre-configured CSharpCompilation.</returns>
    public static CSharpCompilation CreateCompilation(string assemblyName)
    {
        return Default.CreateCompilation(assemblyName);
    }
}
