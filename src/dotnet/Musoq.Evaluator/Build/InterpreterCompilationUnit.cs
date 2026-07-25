using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Build;

/// <summary>
///     Compiles generated interpreter C# code into executable assemblies using Roslyn.
/// </summary>
public class InterpreterCompilationUnit : IDisposable
{
    private readonly ICSharpCompilationFactory _compilationFactory;
    private readonly IInterpreterReferenceProvider _interpreterReferenceProvider;
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly EvaluatorRuntimeEnvironment? _ownedRuntimeEnvironment;
    private byte[]? _assemblyBytes;
    private CSharpCompilation? _compilation;
    private LoadedAssemblyHandle? _loadedAssembly;
    private bool _disposed;

    /// <summary>
    ///     Creates a new compilation unit for interpreter code.
    /// </summary>
    /// <param name="assemblyName">The name for the generated assembly.</param>
    /// <param name="sourceCode">The C# source code to compile.</param>
    public InterpreterCompilationUnit(string assemblyName, string sourceCode)
        : this(
            assemblyName,
            sourceCode,
            new EvaluatorRuntimeEnvironment(),
            DefaultAssemblyLoader.Instance)
    {
    }

    internal InterpreterCompilationUnit(
        string assemblyName,
        string sourceCode,
        EvaluatorRuntimeEnvironment runtimeEnvironment,
        IAssemblyLoader assemblyLoader)
    {
        ArgumentNullException.ThrowIfNull(runtimeEnvironment);
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        SourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
        _compilationFactory = runtimeEnvironment.CompilationFactory;
        _interpreterReferenceProvider = new DefaultInterpreterReferenceProvider(runtimeEnvironment.MetadataReferenceCache);
        _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));
        _ownedRuntimeEnvironment = runtimeEnvironment;
    }

    internal InterpreterCompilationUnit(
        string assemblyName,
        string sourceCode,
        ICSharpCompilationFactory compilationFactory,
        IInterpreterReferenceProvider interpreterReferenceProvider,
        IAssemblyLoader assemblyLoader)
    {
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        SourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
        _compilationFactory = compilationFactory ?? throw new ArgumentNullException(nameof(compilationFactory));
        _interpreterReferenceProvider = interpreterReferenceProvider ?? throw new ArgumentNullException(nameof(interpreterReferenceProvider));
        _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));
    }

    /// <summary>
    ///     Gets the generated assembly name.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    ///     Gets the source code being compiled.
    /// </summary>
    public string SourceCode { get; }

    /// <summary>
    ///     Gets the compilation diagnostics (errors and warnings).
    /// </summary>
    public IReadOnlyList<Diagnostic>? Diagnostics { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether compilation succeeded.
    /// </summary>
    public bool IsSuccess
    {
        get
        {
            ThrowIfDisposed();
            return Diagnostics?.All(d => d.Severity != DiagnosticSeverity.Error) ?? false;
        }
    }

    /// <summary>
    ///     Gets the compiled assembly, or null if compilation failed.
    /// </summary>
    public Assembly? CompiledAssembly { get; private set; }

    /// <summary>
    ///     Compiles the source code and loads the assembly.
    /// </summary>
    /// <returns>True if compilation succeeded; otherwise, false.</returns>
    public bool Compile()
    {
        ThrowIfDisposed();

        var syntaxTree = CSharpSyntaxTree.ParseText(SourceCode);

        _compilation = _compilationFactory.CreateCompilation(AssemblyName);
        _compilation = _compilation.AddSyntaxTrees(syntaxTree);

        var interpreterReferences = _interpreterReferenceProvider.GetReferences();
        if (interpreterReferences.Count > 0)
            _compilation = _compilation.AddReferences(interpreterReferences);

        using var ms = new MemoryStream();
        var result = _compilation.Emit(ms);

        Diagnostics = result.Diagnostics.ToList();

        if (!result.Success) return false;

        _assemblyBytes = ms.ToArray();
        var loadedAssembly = _assemblyLoader.Load(_assemblyBytes);
        _loadedAssembly?.Dispose();
        _loadedAssembly = loadedAssembly;
        CompiledAssembly = loadedAssembly.Assembly;

        return true;
    }

    /// <summary>
    ///     Gets the compiled interpreter type by name.
    /// </summary>
    /// <param name="schemaName">The schema name (class name).</param>
    /// <returns>The compiled type, or null if not found.</returns>
    public Type? GetInterpreterType(string schemaName)
    {
        ThrowIfDisposed();

        if (CompiledAssembly == null)
            return null;


        var typeName = $"Musoq.Generated.Interpreters.{schemaName}";
        var type = CompiledAssembly.GetType(typeName);

        if (type != null)
            return type;


        for (var arity = 1; arity <= 8; arity++)
        {
            var genericTypeName = $"Musoq.Generated.Interpreters.{schemaName}`{arity}";
            type = CompiledAssembly.GetType(genericTypeName);
            if (type != null)
                return type;
        }

        return null;
    }

    /// <summary>
    ///     Creates an instance of a compiled interpreter.
    /// </summary>
    /// <typeparam name="T">The interpreter type.</typeparam>
    /// <param name="schemaName">The schema name.</param>
    /// <returns>A new interpreter instance, or null if not found.</returns>
    public T? CreateInterpreterInstance<T>(string schemaName) where T : class
    {
        var type = GetInterpreterType(schemaName);
        if (type == null)
            return null;

        return Activator.CreateInstance(type) as T;
    }

    /// <summary>
    ///     Gets the error messages from failed compilation.
    /// </summary>
    /// <returns>Error messages, or empty if successful.</returns>
    public IEnumerable<string> GetErrorMessages()
    {
        ThrowIfDisposed();

        if (Diagnostics == null)
            return Enumerable.Empty<string>();

        return Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();
    }

    /// <summary>
    ///     Gets the raw assembly bytes for serialization or other purposes.
    /// </summary>
    public byte[]? GetAssemblyBytes()
    {
        ThrowIfDisposed();
        return _assemblyBytes;
    }

    /// <summary>
    ///     Unloads the generated assembly's collectible load context.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _loadedAssembly?.Dispose();
        _loadedAssembly = null;
        CompiledAssembly = null;
        _ownedRuntimeEnvironment?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
