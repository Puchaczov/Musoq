using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CreateForAnalyze(script, assemblyName, provider, loggerResolver, null, cancellationToken);

    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, provider, diagnosticContext, cancellationToken);
        items.CompilationPurpose = CompilationPurpose.Inspection;
        items.EmitExecutionPlanText = true;
        if (compilationOptions != null)
            items.CompilationOptions = compilationOptions;

        Build(items, CreateExecutableBuildChain(loggerResolver));
        cancellationToken.ThrowIfCancellationRequested();
        return items;
    }

    public static QueryInspectionResult CompileForInspection(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CompileForInspection(script, assemblyName, schemaProvider, loggerResolver, null, cancellationToken);

    public static QueryInspectionResult CompileForInspection(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext, cancellationToken);
        items.CompilationPurpose = CompilationPurpose.Inspection;
        items.EmitExecutionPlanText = true;
        if (compilationOptions != null)
            items.CompilationOptions = compilationOptions;

        Build(items, CreateInspectionBuildChain(loggerResolver));
        cancellationToken.ThrowIfCancellationRequested();
        return CreateInspectionResult(items);
    }

    public static string GetLogicalPlanText(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CompileForInspection(script, assemblyName, schemaProvider, loggerResolver, cancellationToken).LogicalPlanText;

    public static string GetPhysicalPlanText(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CompileForInspection(script, assemblyName, schemaProvider, loggerResolver, cancellationToken).PhysicalPlanText;

    public static string GetGeneratedCSharpCode(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CompileForInspection(script, assemblyName, schemaProvider, loggerResolver, cancellationToken).GeneratedCSharpCode;

    public static (byte[] DllFile, byte[] PdbFile) CompileForStore(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver, CancellationToken cancellationToken)
    {
        var items = CreateForAnalyze(script, assemblyName, provider, loggerResolver, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return (
            items.DllFile ?? throw new InvalidOperationException("Compilation did not produce a DLL file."),
            items.PdbFile ?? throw new InvalidOperationException("Compilation did not produce a PDB file."));
    }

    public static Task<(byte[] DllFile, byte[] PdbFile)> CompileForStoreAsync(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        Task.Run(
            () => CompileForStore(script, assemblyName, provider, loggerResolver, cancellationToken),
            cancellationToken);

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, CancellationToken cancellationToken)
    {
        var result = CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver, cancellationToken);
        if (result.Succeeded)
            return result.CompiledQuery;

        throw result.CaughtException != null
            ? new MusoqQueryException(result.ToEnvelopes(), result.CaughtException)
            : new MusoqQueryException(result.ToEnvelopes());
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, CompilationOptions compilationOptions,
        CancellationToken cancellationToken)
    {
        var result = CompileWithDiagnostics(
            script, assemblyName, schemaProvider, loggerResolver, compilationOptions, cancellationToken);
        if (result.Succeeded)
            return result.CompiledQuery;

        throw result.CaughtException != null
            ? new MusoqQueryException(result.ToEnvelopes(), result.CaughtException)
            : new MusoqQueryException(result.ToEnvelopes());
    }

    public static CompiledTypedQuery<TOut> CompileForTypedExecution<TOut>(
        string script, string assemblyName, ISchemaProvider schemaProvider, ILoggerResolver loggerResolver,
        CancellationToken cancellationToken) =>
        CompileForTypedExecution<TOut>(
            script, assemblyName, schemaProvider, loggerResolver, new CompilationOptions(), cancellationToken);

    public static CompiledTypedQuery<TOut> CompileForTypedExecution<TOut>(
        string script, string assemblyName, ISchemaProvider schemaProvider, ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions, CancellationToken cancellationToken)
    {
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            [],
            CreateExecutableBuildChain,
            cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        using var loadedRunnableType = LoadRunnableTypeWithLifetime(items);
        ITypedRunnable<TOut>? runnable = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            runnable = RequireClrActivator(ExecutionTargetIds.CSharpClr).ActivateTyped<TOut>(
                loadedRunnableType.RunnableType,
                CreateRuntimeBinding(items));
            cancellationToken.ThrowIfCancellationRequested();
            runnable.Logger = loggerResolver.ResolveLogger();
            cancellationToken.ThrowIfCancellationRequested();
            var query = new CompiledTypedQuery<TOut>(runnable);
            runnable = null;
            loadedRunnableType.RetainForTypeLifetime();
            return query;
        }
        catch (OperationCanceledException)
        {
            (runnable as IDisposable)?.Dispose();
            throw;
        }
        catch
        {
            (runnable as IDisposable)?.Dispose();
            throw;
        }
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, Func<BuildChain> createChain, Action<BuildItems> modifyBuildItems,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(loggerResolver);
        ArgumentNullException.ThrowIfNull(createChain);
        ArgumentNullException.ThrowIfNull(modifyBuildItems);
        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext, cancellationToken);
        modifyBuildItems(items);
        items.RetainSemanticCacheState = true;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
        var chain = createChain.Invoke() ??
                    new CreateTree(new TransformTree(new TurnQueryIntoRunnableCode(null), loggerResolver));
        CompilationException? compilationError = null;
        try
        {
            Build(items, chain);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CompilationException ce)
        {
            compilationError = ce;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (compilationError == null && !Debugger.IsAttached)
        {
            ITableRunnable? runnable = null;
            CompiledQuery? compiledQuery = null;
            try
            {
                runnable = CreateRunnable(items);
                cancellationToken.ThrowIfCancellationRequested();
                runnable.Logger = loggerResolver.ResolveLogger();
                cancellationToken.ThrowIfCancellationRequested();
                compiledQuery = new CompiledQuery(runnable);
                runnable = null;
                cancellationToken.ThrowIfCancellationRequested();
                CompleteSemanticCache(items, commit: true);
                return compiledQuery;
            }
            catch (OperationCanceledException)
            {
                compiledQuery?.Dispose();
                (runnable as IDisposable)?.Dispose();
                throw;
            }
            catch
            {
                compiledQuery?.Dispose();
                (runnable as IDisposable)?.Dispose();
                throw;
            }
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "Musoq");
        var tempFileName = Guid.NewGuid().ToString();
        var assemblyPath = Path.Combine(tempPath, $"{tempFileName}.dll");
        var pdbPath = Path.Combine(tempPath, $"{tempFileName}.pdb");
        var csPath = Path.Combine(tempPath, $"{tempFileName}.cs");
        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        cancellationToken.ThrowIfCancellationRequested();
        File.WriteAllText(csPath, InspectGeneratedCSharpCode(items.RenderingArtifact, cancellationToken));
        if (items.DllFile is { Length: > 0 })
            File.WriteAllBytes(assemblyPath, items.DllFile);
        if (items.PdbFile is { Length: > 0 })
            File.WriteAllBytes(pdbPath, items.PdbFile);
        cancellationToken.ThrowIfCancellationRequested();
        if (compilationError != null)
            throw compilationError;

        var assemblyLoadContext = new DebugAssemblyLoadContext();
        try
        {
            var runnable = new RunnableDebugDecorator(
                CreateRunnableForDebug(items, () => assemblyLoadContext.LoadFromAssemblyPath(assemblyPath)),
                assemblyLoadContext,
                csPath,
                assemblyPath,
                pdbPath);
            cancellationToken.ThrowIfCancellationRequested();
            return new CompiledQuery(runnable);
        }
        catch (OperationCanceledException)
        {
            assemblyLoadContext.Unload();
            throw;
        }
        }
        finally
        {
            CompleteSemanticCache(items, commit: false);
        }
    }

    public static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver, null, cancellationToken);

    public static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        CancellationToken cancellationToken) =>
        CompileWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            requireExecutionPlan: false,
            cancellationToken);

    internal static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        bool requireExecutionPlan, CancellationToken cancellationToken) =>
        CompileWithDiagnosticsCore(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            requireExecutionPlan,
            cancellationToken);

    public static Task<BuildResult> CompileWithDiagnosticsAsync(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CancellationToken cancellationToken) =>
        CompileWithDiagnosticsAsync(
            script, assemblyName, schemaProvider, loggerResolver, null, cancellationToken);

    public static Task<BuildResult> CompileWithDiagnosticsAsync(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        CancellationToken cancellationToken) =>
        Task.Run(
            () => CompileWithDiagnostics(
                script, assemblyName, schemaProvider, loggerResolver, compilationOptions, cancellationToken),
            cancellationToken);
}
