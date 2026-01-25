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
    private readonly List<string> _loadedAssemblies = new(20);
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
        AddAssemblyReference(abstractionDll);


        TrackTypes(
            typeof(object),
            typeof(CancellationToken),
            typeof(ISchema),
            typeof(LibraryBase),
            typeof(Table),
            typeof(SyntaxFactory),
            typeof(ExpandoObject),
            typeof(SchemaFromNode),
            typeof(ILogger));


        AddAssemblyReferences(assemblies is Assembly[] arr ? arr : [.. assemblies]);
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

    public void TrackType(Type type)
    {
        TrackNamespace(type);
        AddAssemblyReference(type.Assembly);
    }

    public void TrackTypes(params Type[] types)
    {
        foreach (var type in types) TrackType(type);
    }

    public void AddAssemblyReference(Assembly assembly)
    {
        if (string.IsNullOrEmpty(assembly.Location))
            return;

        if (_loadedAssemblies.Contains(assembly.Location))
            return;

        _loadedAssemblies.Add(assembly.Location);
        _compilation = _compilation.AddReferences(
            MetadataReferenceCache.GetOrCreate(assembly.Location));
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

    /// <summary>
    ///     Checks if an assembly is already loaded.
    /// </summary>
    public bool IsAssemblyLoaded(string location)
    {
        return _loadedAssemblies.Contains(location);
    }

    #endregion
}
