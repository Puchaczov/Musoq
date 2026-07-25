using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Musoq.Converter.Build;
using Musoq.Converter.Diagnostics;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver)
    {
        return CreateForAnalyze(script, assemblyName, provider, loggerResolver, null);
    }

    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver, CompilationOptions? compilationOptions)
    {
        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, provider, diagnosticContext);
        items.EmitExecutionPlanText = true;
        if (compilationOptions != null)
            items.CompilationOptions = compilationOptions;

        Build(items, CreateExecutableBuildChain(loggerResolver));

        return items;
    }

    public static QueryInspectionResult CompileForInspection(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver)
    {
        return CompileForInspection(script, assemblyName, schemaProvider, loggerResolver, null);
    }

    public static QueryInspectionResult CompileForInspection(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions)
    {
        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitExecutionPlanText = true;

        if (compilationOptions != null)
            items.CompilationOptions = compilationOptions;

        Build(items, CreateInspectionBuildChain(loggerResolver));

        return CreateInspectionResult(items);
    }

    public static string GetLogicalPlanText(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver)
    {
        return CompileForInspection(script, assemblyName, schemaProvider, loggerResolver).LogicalPlanText;
    }

    public static string GetPhysicalPlanText(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver)
    {
        return CompileForInspection(script, assemblyName, schemaProvider, loggerResolver).PhysicalPlanText;
    }

    public static string GetGeneratedCSharpCode(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver)
    {
        return CompileForInspection(script, assemblyName, schemaProvider, loggerResolver).GeneratedCSharpCode;
    }

    public static (byte[] DllFile, byte[] PdbFile) CompileForStore(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver)
    {
        var items = CreateForAnalyze(script, assemblyName, provider, loggerResolver);

        return (
            items.DllFile ?? throw new InvalidOperationException("Compilation did not produce a DLL file."),
            items.PdbFile ?? throw new InvalidOperationException("Compilation did not produce a PDB file."));
    }

    public static Task<(byte[] DllFile, byte[] PdbFile)> CompileForStoreAsync(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver)
    {
        return Task.Run(() => CompileForStore(script, assemblyName, provider, loggerResolver));
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        var result = CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver);

        if (result.Succeeded)
            return result.CompiledQuery;

        throw result.CaughtException != null
            ? new MusoqQueryException(result.ToEnvelopes(), result.CaughtException)
            : new MusoqQueryException(result.ToEnvelopes());
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, CompilationOptions compilationOptions)
    {
        var result = CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver, compilationOptions);

        if (result.Succeeded)
            return result.CompiledQuery;

        throw result.CaughtException != null
            ? new MusoqQueryException(result.ToEnvelopes(), result.CaughtException)
            : new MusoqQueryException(result.ToEnvelopes());
    }

    public static CompiledTypedQuery<TOut> CompileForTypedExecution<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return CompileForTypedExecution<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            new CompilationOptions());
    }

    public static CompiledTypedQuery<TOut> CompileForTypedExecution<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions)
    {
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            [],
            CreateExecutableBuildChain);

        var runnable = CreateTypedRunnable<TOut>(items);
        runnable.Logger = loggerResolver.ResolveLogger();

        return new CompiledTypedQuery<TOut>(runnable);
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, Func<BuildChain> createChain, Action<BuildItems> modifyBuildItems)
    {
        ArgumentNullException.ThrowIfNull(loggerResolver);
        ArgumentNullException.ThrowIfNull(createChain);
        ArgumentNullException.ThrowIfNull(modifyBuildItems);
        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);

        modifyBuildItems(items);

        var compiled = true;

        var chain =
            createChain.Invoke() ??
            new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), loggerResolver)
            );

        CompilationException? compilationError = null;
        try
        {
            chain.Build(items);
        }
        catch (CompilationException ce)
        {
            compilationError = ce;
            compiled = false;
        }


        ITableRunnable runnable;
        if (compiled && !Debugger.IsAttached)
        {
            runnable = CreateRunnable(items);
            runnable.Logger = loggerResolver.ResolveLogger();

            return new CompiledQuery(runnable);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "Musoq");
        var tempFileName = Guid.NewGuid().ToString();
        var assemblyPath = Path.Combine(tempPath, $"{tempFileName}.dll");
        var pdbPath = Path.Combine(tempPath, $"{tempFileName}.pdb");
        var csPath = Path.Combine(tempPath, $"{tempFileName}.cs");

        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        using (var file = new StreamWriter(File.Open(csPath, FileMode.Create)))
        {
            file.Write(InspectGeneratedCSharpCode(items.RenderingArtifact));
        }

        if (items.DllFile is { Length: > 0 })
        {
            using var file = new BinaryWriter(File.Open(assemblyPath, FileMode.Create));
            if (items.DllFile != null)
                file.Write(items.DllFile);
        }

        if (items.PdbFile is { Length: > 0 })
        {
            using var file = new BinaryWriter(File.Open(pdbPath, FileMode.Create));
            if (items.PdbFile != null)
                file.Write(items.PdbFile);
        }

        if (!compiled && compilationError != null)
            throw compilationError;

        var assemblyLoadContext = new DebugAssemblyLoadContext();
        runnable = new RunnableDebugDecorator(
            CreateRunnableForDebug(items, () => assemblyLoadContext.LoadFromAssemblyPath(assemblyPath)),
            assemblyLoadContext,
            csPath,
            assemblyPath,
            pdbPath);

        return new CompiledQuery(runnable);
    }

    /// <summary>
    ///     Compiles a query using the diagnostic-collection path. Never throws for query errors;
    ///     instead, all errors are collected in the returned <see cref="BuildResult" />.
    ///     This is the preferred API for new consumers replacing the exception-throwing path.
    /// </summary>
    public static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver)
    {
        return CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver, null);
    }

    /// <summary>
    ///     Compiles a query using the diagnostic-collection path with custom compilation options.
    ///     Never throws for query errors; instead, all errors are collected in the returned <see cref="BuildResult" />.
    /// </summary>
    public static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions)
    {
        return CompileWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            requireExecutionPlan: false);
    }

    internal static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        bool requireExecutionPlan)
    {
        return CompileWithDiagnosticsCore(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            requireExecutionPlan);
    }

    /// <summary>
    ///     Asynchronous version of
    ///     <see cref="CompileWithDiagnostics(string, string, ISchemaProvider, ILoggerResolver, CompilationOptions)" />.
    /// </summary>
    public static Task<BuildResult> CompileWithDiagnosticsAsync(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions compilationOptions)
    {
        return Task.Run(() =>
            CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver, compilationOptions));
    }
}
