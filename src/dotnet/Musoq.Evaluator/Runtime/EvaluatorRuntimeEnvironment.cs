using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Runtime;

/// <summary>
///     Owns the Roslyn and runtime-reference lifetime for one evaluator environment.
/// </summary>
public sealed class EvaluatorRuntimeEnvironment : IDisposable
{
    private int _disposed;

    public EvaluatorRuntimeEnvironment()
    {
        MetadataReferenceCache = new DefaultMetadataReferenceCache();
        ReferenceProvider = new RuntimeReferenceProvider(MetadataReferenceCache);
        CompilationFactory = new RoslynCompilationFactory(ReferenceProvider, MetadataReferenceCache);
    }

    internal IMetadataReferenceCache MetadataReferenceCache { get; }

    internal IRuntimeReferenceProvider ReferenceProvider { get; }

    internal ICSharpCompilationFactory CompilationFactory { get; }

    /// <summary>
    ///     Gets a copy of the runtime references owned by this environment.
    /// </summary>
    public MetadataReference[] References
    {
        get
        {
            ThrowIfDisposed();
            return ReferenceProvider.References;
        }
    }

    /// <summary>
    ///     Eagerly loads the runtime references owned by this environment.
    /// </summary>
    public void CreateReferences()
    {
        ThrowIfDisposed();
        ReferenceProvider.CreateReferences();
    }

    /// <summary>
    ///     Creates a compilation using the environment's reference and workspace lifetime.
    /// </summary>
    public CSharpCompilation CreateCompilation(string assemblyName)
    {
        ThrowIfDisposed();
        return CompilationFactory.CreateCompilation(assemblyName);
    }

    /// <summary>
    ///     Gets a workspace for the current thread.
    /// </summary>
    public AdhocWorkspace Workspace
    {
        get
        {
            ThrowIfDisposed();
            return CompilationFactory.Workspace;
        }
    }

    /// <summary>
    ///     Gets a syntax generator for the current thread.
    /// </summary>
    public SyntaxGenerator Generator
    {
        get
        {
            ThrowIfDisposed();
            return CompilationFactory.Generator;
        }
    }

    /// <summary>
    ///     Gets the assembly paths already included in this environment's template compilation.
    /// </summary>
    public IReadOnlySet<string> PreloadedAssemblyPaths
    {
        get
        {
            ThrowIfDisposed();
            return CompilationFactory.PreloadedAssemblyPaths;
        }
    }

    /// <summary>
    ///     Gets or creates a metadata reference in this environment's cache.
    /// </summary>
    public MetadataReference GetOrCreateMetadataReference(string assemblyPath)
    {
        ThrowIfDisposed();
        return MetadataReferenceCache.GetOrCreate(assemblyPath);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        ((IDisposable)CompilationFactory).Dispose();
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }
}
