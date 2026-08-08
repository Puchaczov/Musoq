using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Runtime;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

/// <summary>
///     Manages compilation context: namespaces, assembly references, type tracking.
/// </summary>
public sealed class CompilationContextManager
{
    private readonly HashSet<string> _loadedAssemblies = new(20, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _namespaces = new(16);
    private readonly EvaluatorRuntimeEnvironment? _runtimeEnvironment;
    private CSharpCompilation _compilation;

    /// <summary>
    ///     Creates a new CompilationContextManager with the given initial compilation.
    /// </summary>
    /// <param name="initialCompilation">The initial compilation to build upon.</param>
    public CompilationContextManager(CSharpCompilation initialCompilation)
        : this(initialCompilation, null)
    {
    }

    /// <summary>
    ///     Creates a compilation context owned by an explicit evaluator runtime environment.
    /// </summary>
    public CompilationContextManager(
        CSharpCompilation initialCompilation,
        EvaluatorRuntimeEnvironment? runtimeEnvironment)
    {
        _compilation = initialCompilation ?? throw new ArgumentNullException(nameof(initialCompilation));
        _runtimeEnvironment = runtimeEnvironment;

        foreach (var path in _runtimeEnvironment?.PreloadedAssemblyPaths ??
                             GetPreloadedAssemblyPaths(initialCompilation))
            _loadedAssemblies.Add(path);
    }

    /// <summary>
    ///     Initializes the context with default namespaces and common assemblies.
    /// </summary>
    public void InitializeDefaults()
    {
    }

    /// <summary>
    ///     Initializes core type references required for code generation.
    ///     Core Musoq types are already in the template compilation; this only adds plugin assemblies.
    /// </summary>
    /// <param name="assemblies">Plugin assemblies to reference.</param>
    public void InitializeCoreReferences(IEnumerable<Assembly> assemblies)
    {
        var assemblyArray = assemblies as Assembly[] ?? [.. assemblies];
        var newReferences = new List<MetadataReference>(assemblyArray.Length);

        foreach (var assembly in assemblyArray)
        {
            if (string.IsNullOrEmpty(assembly.Location))
                continue;

            if (_loadedAssemblies.Contains(assembly.Location))
                continue;

            try
            {
                ValidateMetadataImage(assembly.Location);
                newReferences.Add(GetMetadataReference(assembly.Location));
            }
            catch (Exception exception) when (IsMetadataReferenceFailure(exception))
            {
                throw CSharpClrReferenceDiscoveryException.ForMetadataReference(
                    assembly,
                    "execution-plan CLR reference",
                    exception);
            }

            _loadedAssemblies.Add(assembly.Location);
        }

        if (newReferences.Count > 0)
            _compilation = _compilation.AddReferences(newReferences);
    }

    #region INamespaceTracker

    public void TrackNamespace(string ns)
    {
        if (!string.IsNullOrEmpty(ns)) _namespaces.Add(ns);
    }

    public void TrackNamespace(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.Namespace != null) TrackNamespace(type.Namespace);
    }

    public void TrackNamespaces(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        foreach (var type in types) TrackNamespace(type);
    }

    public IReadOnlyCollection<string> GetNamespaces()
    {
        return _namespaces;
    }

    #endregion

    #region ITypeReferenceTracker

    public void TrackTypes(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        foreach (var type in types) TrackType(type);
    }

    public void AddAssemblyReference(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            return;

        if (_loadedAssemblies.Contains(assemblyPath))
            return;

        _loadedAssemblies.Add(assemblyPath);
        _compilation = _compilation.AddReferences(
            GetMetadataReference(assemblyPath));
    }

    public void AddAssemblyReferences(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        var newReferences = new List<MetadataReference>(assemblies.Length);

        foreach (var assembly in assemblies)
        {
            if (string.IsNullOrEmpty(assembly.Location))
                continue;

            if (_loadedAssemblies.Contains(assembly.Location))
                continue;

            _loadedAssemblies.Add(assembly.Location);
            newReferences.Add(GetMetadataReference(assembly.Location));
        }

        if (newReferences.Count > 0) _compilation = _compilation.AddReferences(newReferences);
    }

    private void TrackType(Type type)
    {
        TrackNamespace(type);
        AddAssemblyReference(type.Assembly);
    }

    private void AddAssemblyReference(Assembly assembly)
    {
        if (string.IsNullOrEmpty(assembly.Location))
            return;

        if (!_loadedAssemblies.Add(assembly.Location))
            return;

        _compilation = _compilation.AddReferences(
            GetMetadataReference(assembly.Location));
    }

    private MetadataReference GetMetadataReference(string assemblyPath)
    {
        var reference = _runtimeEnvironment?.GetOrCreateMetadataReference(assemblyPath) ??
                        MetadataReference.CreateFromFile(assemblyPath);
        return reference;
    }

    private static void ValidateMetadataImage(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
            throw new BadImageFormatException("The assembly file does not contain CLR metadata.");

        _ = peReader.GetMetadata();
    }

    private static bool IsMetadataReferenceFailure(Exception exception) =>
        exception is ArgumentException or
            BadImageFormatException or
            FileLoadException or
            FileNotFoundException or
            IOException or
            UnauthorizedAccessException;

    private static IEnumerable<string> GetPreloadedAssemblyPaths(CSharpCompilation compilation)
    {
        return compilation.References
            .OfType<PortableExecutableReference>()
            .Select(static reference => reference.FilePath)
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(static path => path!);
    }

    #endregion

    #region Compilation Access

    /// <summary>
    ///     Gets the current CSharp compilation.
    /// </summary>
    public CSharpCompilation GetCompilation()
    {
        return _compilation;
    }

    /// <summary>
    ///     Updates the compilation with a new syntax tree.
    /// </summary>
    public void AddSyntaxTree(SyntaxTree syntaxTree)
    {
        _compilation = _compilation.AddSyntaxTrees(syntaxTree);
    }

    #endregion
}
