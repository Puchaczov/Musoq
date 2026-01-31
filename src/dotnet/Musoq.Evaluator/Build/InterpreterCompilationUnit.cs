#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Runtime;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Build;

/// <summary>
///     Compiles generated interpreter C# code into executable assemblies using Roslyn.
/// </summary>
public class InterpreterCompilationUnit
{
    private byte[]? _assemblyBytes;
    private CSharpCompilation? _compilation;

    /// <summary>
    ///     Creates a new compilation unit for interpreter code.
    /// </summary>
    /// <param name="assemblyName">The name for the generated assembly.</param>
    /// <param name="sourceCode">The C# source code to compile.</param>
    public InterpreterCompilationUnit(string assemblyName, string sourceCode)
    {
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        SourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
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
    public bool IsSuccess => Diagnostics?.All(d => d.Severity != DiagnosticSeverity.Error) ?? false;

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
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceCode);


        _compilation = RoslynSharedFactory.CreateCompilation(AssemblyName);
        _compilation = _compilation.AddSyntaxTrees(syntaxTree);


        _compilation = _compilation.AddReferences(GetInterpreterReferences());


        using var ms = new MemoryStream();
        var result = _compilation.Emit(ms);

        Diagnostics = result.Diagnostics.ToList();

        if (!result.Success) return false;

        _assemblyBytes = ms.ToArray();
        CompiledAssembly = Assembly.Load(_assemblyBytes);

        return true;
    }

    /// <summary>
    ///     Gets the compiled interpreter type by name.
    /// </summary>
    /// <param name="schemaName">The schema name (class name).</param>
    /// <returns>The compiled type, or null if not found.</returns>
    public Type? GetInterpreterType(string schemaName)
    {
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
        if (Diagnostics == null)
            return Enumerable.Empty<string>();

        return Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString());
    }

    private static IEnumerable<MetadataReference> GetInterpreterReferences()
    {
        var references = new List<MetadataReference>();


        var schemaAssembly = typeof(IBytesInterpreter<>).Assembly;
        references.Add(MetadataReference.CreateFromFile(schemaAssembly.Location));


        var memoryAssemblyPath = Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!,
            "System.Memory.dll");
        if (File.Exists(memoryAssemblyPath)) references.Add(MetadataReference.CreateFromFile(memoryAssemblyPath));


        var buffersAssemblyPath = Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!,
            "System.Buffers.dll");
        if (File.Exists(buffersAssemblyPath)) references.Add(MetadataReference.CreateFromFile(buffersAssemblyPath));

        return references;
    }

    /// <summary>
    ///     Gets the raw assembly bytes for serialization or other purposes.
    /// </summary>
    public byte[]? GetAssemblyBytes()
    {
        return _assemblyBytes;
    }
}
