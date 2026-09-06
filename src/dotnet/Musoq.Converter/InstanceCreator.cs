using System;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

/// <summary>Creates executable, inspectable, profiled, and reusable query products.</summary>
/// <remarks>
/// Cancellation-aware overloads accept a cooperative <see cref="CancellationToken"/> as their
/// final parameter. Cancellation is reported as <see cref="OperationCanceledException"/>; a
/// provider or compiler component that ignores the token cannot be forcibly terminated.
/// </remarks>
public static partial class InstanceCreator
{
    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver) =>
        CreateForAnalyze(script, assemblyName, provider, loggerResolver, null, CancellationToken.None);

    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver, CompilationOptions? compilationOptions) =>
        CreateForAnalyze(script, assemblyName, provider, loggerResolver, compilationOptions, CancellationToken.None);

    public static QueryInspectionResult CompileForInspection(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        CompileForInspection(script, assemblyName, schemaProvider, loggerResolver, null, CancellationToken.None);

    public static QueryInspectionResult CompileForInspection(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions) =>
        CompileForInspection(
            script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);

    public static string GetLogicalPlanText(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        GetLogicalPlanText(script, assemblyName, schemaProvider, loggerResolver, CancellationToken.None);

    public static string GetPhysicalPlanText(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        GetPhysicalPlanText(script, assemblyName, schemaProvider, loggerResolver, CancellationToken.None);

    public static string GetGeneratedCSharpCode(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        GetGeneratedCSharpCode(script, assemblyName, schemaProvider, loggerResolver, CancellationToken.None);

    public static (byte[] DllFile, byte[] PdbFile) CompileForStore(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver) =>
        CompileForStore(script, assemblyName, provider, loggerResolver, CancellationToken.None);

    public static Task<(byte[] DllFile, byte[] PdbFile)> CompileForStoreAsync(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver) =>
        CompileForStoreAsync(script, assemblyName, provider, loggerResolver, CancellationToken.None);

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver) =>
        CompileForExecution(script, assemblyName, schemaProvider, loggerResolver, CancellationToken.None);

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, CompilationOptions compilationOptions) =>
        CompileForExecution(
            script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);

    public static CompiledTypedQuery<TOut> CompileForTypedExecution<TOut>(
        string script, string assemblyName, ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        CompileForTypedExecution<TOut>(
            script, assemblyName, schemaProvider, loggerResolver, new CompilationOptions(), CancellationToken.None);

    public static CompiledTypedQuery<TOut> CompileForTypedExecution<TOut>(
        string script, string assemblyName, ISchemaProvider schemaProvider, ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions) =>
        CompileForTypedExecution<TOut>(
            script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, Func<BuildChain> createChain, Action<BuildItems> modifyBuildItems) =>
        CompileForExecution(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            createChain,
            modifyBuildItems,
            CancellationToken.None);

    /// <summary>
    /// Compiles a query using the diagnostic-collection path. Query errors are returned in the result;
    /// cancellation is propagated as <see cref="OperationCanceledException" />.
    /// </summary>
    public static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        CompileWithDiagnostics(script, assemblyName, schemaProvider, loggerResolver, null, CancellationToken.None);

    public static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions) =>
        CompileWithDiagnostics(
            script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);

    internal static BuildResult CompileWithDiagnostics(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions,
        bool requireExecutionPlan) =>
        CompileWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            requireExecutionPlan,
            CancellationToken.None);

    /// <summary>
    /// Asynchronously compiles a query and propagates cooperative cancellation to compile-time work.
    /// </summary>
    public static Task<BuildResult> CompileWithDiagnosticsAsync(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver) =>
        CompileWithDiagnosticsAsync(
            script, assemblyName, schemaProvider, loggerResolver, null, CancellationToken.None);

    public static Task<BuildResult> CompileWithDiagnosticsAsync(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver, CompilationOptions? compilationOptions) =>
        CompileWithDiagnosticsAsync(
            script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);
}
