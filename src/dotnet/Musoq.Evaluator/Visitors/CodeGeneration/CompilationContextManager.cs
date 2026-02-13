#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes.From;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Manages compilation context: namespaces, assembly references, type tracking.
///     Extracted from ToCSharpRewriteTreeVisitor to improve separation of concerns.
/// </summary>
public sealed class CompilationContextManager
{
    private readonly HashSet<string> _loadedAssemblies = new(20, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _namespaces = new(16);
    private CSharpCompilation _compilation;

    /// <summary>
    ///     Creates a new CompilationContextManager with the given initial compilation.
    /// </summary>
    /// <param name="initialCompilation">The initial compilation to build upon.</param>
    public CompilationContextManager(CSharpCompilation initialCompilation)
    {
        _compilation = initialCompilation ?? throw new ArgumentNullException(nameof(initialCompilation));
    }

    /// <summary>
    ///     Initializes the context with default namespaces and common assemblies.
    /// </summary>
    public void InitializeDefaults()
    {
        TrackNamespace("System");
        TrackNamespace("System.Threading");
        TrackNamespace("System.Collections.Generic");
        TrackNamespace("System.Threading.Tasks");
        TrackNamespace("System.Linq");
        TrackNamespace("System.Dynamic");
        TrackNamespace("Microsoft.Extensions.Logging");
        TrackNamespace("Musoq.Plugins");
        TrackNamespace("Musoq.Schema");
        TrackNamespace("Musoq.Evaluator");
        TrackNamespace("Musoq.Evaluator.Tables");
        TrackNamespace("Musoq.Evaluator.Helpers");
        TrackNamespace("Musoq.Parser.Nodes.From");
        TrackNamespace("Musoq.Parser.Nodes");
    }

    /// <summary>
    ///     Initializes core type references required for code generation.
    ///     Call after InitializeDefaults() during visitor construction.
    /// </summary>
    /// <param name="assemblies">Plugin assemblies to reference.</param>
    public void InitializeCoreReferences(IEnumerable<Assembly> assemblies)
    {
        var abstractionDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Microsoft.Extensions.Logging.Abstractions.dll");

        // Batch all core type references into a single AddReferences call
        // instead of calling AddAssemblyReference individually per type
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

        var newReferences = new List<MetadataReference>(coreTypes.Length + 2);

        // Add the abstractions DLL
        if (!string.IsNullOrEmpty(abstractionDll) && _loadedAssemblies.Add(abstractionDll))
            newReferences.Add(MetadataReferenceCache.GetOrCreate(abstractionDll));

        // Track namespaces and collect assembly references for all core types
        foreach (var type in coreTypes)
        {
            TrackNamespace(type);
            var location = type.Assembly.Location;
            if (!string.IsNullOrEmpty(location) && _loadedAssemblies.Add(location))
                newReferences.Add(MetadataReferenceCache.GetOrCreate(location));
        }

        if (newReferences.Count > 0)
            _compilation = _compilation.AddReferences(newReferences);

        AddAssemblyReferences(assemblies as Assembly[] ?? [.. assemblies]);
    }

    #region INamespaceTracker

    public void TrackNamespace(string ns)
    {
        if (!string.IsNullOrEmpty(ns)) _namespaces.Add(ns);
    }

    public void TrackNamespace(Type type)
    {
        if (type.Namespace != null) TrackNamespace(type.Namespace);
    }

    public void TrackNamespaces(params Type[] types)
    {
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
            MetadataReferenceCache.GetOrCreate(assemblyPath));
    }

    public void AddAssemblyReferences(params Assembly[] assemblies)
    {
        var newReferences = new List<MetadataReference>(assemblies.Length);

        foreach (var assembly in assemblies)
        {
            if (string.IsNullOrEmpty(assembly.Location))
                continue;

            if (_loadedAssemblies.Contains(assembly.Location))
                continue;

            _loadedAssemblies.Add(assembly.Location);
            newReferences.Add(MetadataReferenceCache.GetOrCreate(assembly.Location));
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
            MetadataReferenceCache.GetOrCreate(assembly.Location));
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
