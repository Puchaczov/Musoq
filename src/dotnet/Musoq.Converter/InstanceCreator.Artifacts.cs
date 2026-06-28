using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Musoq.Converter.Build;
using Musoq.Converter.Diagnostics;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static ArtifactBuildResult CompileArtifactWithDiagnostics(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);

        if (string.IsNullOrWhiteSpace(script))
            return ArtifactBuildResult.Failure(CreateEmptyQueryDiagnostics(script));

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        if (DiagnosticSqlCommandParser.TryParse(script, out var diagnosticCommand, out var parserDiagnostics))
        {
            if (parserDiagnostics is { Count: > 0 })
                return ArtifactBuildResult.Failure(parserDiagnostics.ToArray());

            if (diagnosticCommand != null)
                return ArtifactBuildResult.Failure(
                    [CreateArtifactDiagnostic("Diagnostic SQL commands cannot be compiled into reusable artifacts.")]);
        }

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = Debugger.IsAttached;
        items.CompilationOptions = effectiveCompilationOptions;

        Exception? caughtException = null;
        try
        {
            Build(items, CreateExecutableBuildChain(loggerResolver));
            RejectUnsupportedMultiStatementQuery(items.RawQueryTree);
        }
        catch (CompilationException ce)
        {
            caughtException = ce;
            diagnosticContext.ReportException(ce);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        if (diagnosticContext.HasErrors)
            return ArtifactBuildResult.Failure(diagnostics, caughtException);

        if (items.DllFile is not { Length: > 0 } assemblyBytes)
        {
            diagnostics.Add(CreateArtifactDiagnostic("Compilation succeeded but did not produce artifact assembly bytes."));
            return ArtifactBuildResult.Failure(diagnostics);
        }

        var artifact = new CompiledQueryArtifact(
            assemblyBytes,
            items.PdbFile,
            items.AccessToClassPath,
            CompiledQueryArtifactSupport.CurrentEngineVersion,
            CompiledQueryArtifact.CurrentArtifactFormatVersion,
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(effectiveCompilationOptions),
            CompiledQueryArtifactSupport.CreateMetadata(
                assemblyName,
                script,
                items.Compilation));

        return ArtifactBuildResult.Success(artifact, diagnostics);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions = null,
        CompiledQueryArtifactTypeLoader? typeLoader = null)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);

        if (string.IsNullOrWhiteSpace(script))
            return BuildResult.Failure(CreateEmptyQueryDiagnostics(script), script ?? string.Empty);

        var artifactDiagnostics = new List<Diagnostic>();
        if (!TryGetRequiredMetadata(artifact, CompiledQueryArtifactSupport.MetadataAssemblyName, artifactDiagnostics, out var assemblyName))
            return BuildResult.Failure(artifactDiagnostics, script);

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        if (DiagnosticSqlCommandParser.TryParse(script, out var diagnosticCommand, out var parserDiagnostics))
        {
            if (parserDiagnostics is { Count: > 0 })
                return BuildResult.Failure(parserDiagnostics.ToArray(), script);

            if (diagnosticCommand != null)
                return BuildResult.Failure(
                    [CreateArtifactDiagnostic("Diagnostic SQL commands cannot be loaded from reusable artifacts.")],
                    script);
        }

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = false;
        items.CompilationOptions = effectiveCompilationOptions;

        Exception? caughtException = null;
        try
        {
            Build(items, CreateInspectionBuildChain(loggerResolver));
            RejectUnsupportedMultiStatementQuery(items.RawQueryTree);
        }
        catch (CompilationException ce)
        {
            caughtException = ce;
            diagnosticContext.ReportException(ce);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        ValidateArtifactCompatibility(script, artifact, effectiveCompilationOptions, items, diagnostics);
        if (diagnostics.Any(static diagnostic => diagnostic.IsError))
            return BuildResult.Failure(diagnostics, script, buildItems: items);

        Type runnableType;
        try
        {
            runnableType = typeLoader != null
                ? typeLoader(artifact)
                : LoadRunnableTypeFromArtifactBytes(artifact);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact type loading failed: {ex.Message}"));
            return BuildResult.Failure(diagnostics, script, ex, items);
        }

        ValidateLoadedRunnableType(artifact, runnableType, diagnostics);
        if (diagnostics.Any(static diagnostic => diagnostic.IsError))
            return BuildResult.Failure(diagnostics, script, buildItems: items);

        ITableRunnable runnable;
        try
        {
            runnable = CreateRunnable(
                runnableType,
                items.SchemaProvider,
                items.SourceRuntimeSettingsBySourceContextId,
                items.SourceRuntimeSettingDescriptionsBySourceContextId,
                CreateSourceExecutionPlans(items));
            runnable.Logger = loggerResolver.ResolveLogger();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact runnable creation failed: {ex.Message}"));
            return BuildResult.Failure(diagnostics, script, ex, items);
        }

        return BuildResult.Success(new CompiledQuery(runnable), diagnostics, script, items);
    }

    private static Type LoadRunnableTypeFromArtifactBytes(ICompiledQueryArtifact artifact)
    {
        var assemblyBytes = artifact is CompiledQueryArtifact ownedArtifact
            ? ownedArtifact.AssemblyBytesUnsafe
            : artifact.AssemblyBytes;
        var symbolsBytes = artifact is CompiledQueryArtifact ownedSymbolsArtifact
            ? ownedSymbolsArtifact.SymbolsBytesUnsafe
            : artifact.SymbolsBytes;

        if (assemblyBytes is not { Length: > 0 })
            throw new InvalidOperationException("Compiled artifact assembly bytes are empty.");

        var assembly = symbolsBytes is { Length: > 0 }
            ? Assembly.Load(assemblyBytes, symbolsBytes)
            : Assembly.Load(assemblyBytes);
        return assembly.GetType(artifact.RunnableTypeName)
               ?? throw new InvalidOperationException($"Type {artifact.RunnableTypeName} was not found in artifact assembly {assembly.FullName}.");
    }

    private static void ValidateArtifactCompatibility(
        string script,
        ICompiledQueryArtifact artifact,
        CompilationOptions compilationOptions,
        BuildItems items,
        ICollection<Diagnostic> diagnostics)
    {
        ValidateEqual(
            "artifact format version",
            CompiledQueryArtifact.CurrentArtifactFormatVersion,
            artifact.ArtifactFormatVersion,
            diagnostics);
        ValidateEqual(
            "engine version",
            CompiledQueryArtifactSupport.CurrentEngineVersion,
            artifact.EngineVersion,
            diagnostics);
        ValidateEqual(
            "compilation options signature",
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(compilationOptions),
            artifact.CompilationOptionsSignature,
            diagnostics);
        ValidateEqual(
            "runnable type name",
            items.AccessToClassPath,
            artifact.RunnableTypeName,
            diagnostics);

        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataArtifactKind,
            CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
            diagnostics);
        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataScriptSha256,
            CompiledQueryArtifactSupport.ComputeHash(script),
            diagnostics);
        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256,
            CompiledQueryArtifactSupport.ComputeGeneratedCodeHash(items.Compilation),
            diagnostics);

        var assemblyBytes = artifact is CompiledQueryArtifact ownedArtifact
            ? ownedArtifact.AssemblyBytesUnsafe
            : artifact.AssemblyBytes;
        if (assemblyBytes is not { Length: > 0 })
            diagnostics.Add(CreateArtifactDiagnostic("Compiled artifact assembly bytes are empty."));
    }

    private static void ValidateLoadedRunnableType(
        ICompiledQueryArtifact artifact,
        Type? runnableType,
        ICollection<Diagnostic> diagnostics)
    {
        if (runnableType == null)
        {
            diagnostics.Add(CreateArtifactDiagnostic("Compiled artifact loader returned a null runnable type."));
            return;
        }

        if (!typeof(ITableRunnable).IsAssignableFrom(runnableType))
        {
            diagnostics.Add(CreateArtifactDiagnostic(
                $"Compiled artifact type '{runnableType.FullName}' does not implement {nameof(ITableRunnable)}."));
            return;
        }

        if (!string.Equals(runnableType.FullName, artifact.RunnableTypeName, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateArtifactDiagnostic(
                $"Compiled artifact loader returned type '{runnableType.FullName}', but artifact expects '{artifact.RunnableTypeName}'."));
        }
    }

    private static void ValidateMetadataValue(
        ICompiledQueryArtifact artifact,
        string key,
        string expected,
        ICollection<Diagnostic> diagnostics)
    {
        if (!TryGetRequiredMetadata(artifact, key, diagnostics, out var actual))
            return;

        ValidateEqual($"metadata '{key}'", expected, actual, diagnostics);
    }

    private static bool TryGetRequiredMetadata(
        ICompiledQueryArtifact artifact,
        string key,
        ICollection<Diagnostic> diagnostics,
        out string value)
    {
        value = string.Empty;
        if (artifact.Metadata == null || !artifact.Metadata.TryGetValue(key, out value!) || string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact is missing required metadata '{key}'."));
            value = string.Empty;
            return false;
        }

        return true;
    }

    private static void ValidateEqual(
        string label,
        string expected,
        string actual,
        ICollection<Diagnostic> diagnostics)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            diagnostics.Add(CreateArtifactDiagnostic(
                $"Compiled artifact {label} is incompatible. Expected '{expected}', got '{actual}'."));
    }

    private static IReadOnlyList<Diagnostic> CreateEmptyQueryDiagnostics(string? script)
    {
        var emptySourceText = new SourceText(script ?? string.Empty);
        var emptyContext = new DiagnosticContext(emptySourceText);
        emptyContext.ReportError(
            DiagnosticCode.MQ2016_IncompleteStatement,
            "The query is empty. Provide a valid SQL query starting with SELECT, WITH, DESC, TABLE, COUPLE, or an optional param(...) block.",
            TextSpan.Empty);
        return emptyContext.Diagnostics.ToList();
    }

    private static Diagnostic CreateArtifactDiagnostic(string message)
    {
        return Diagnostic.Error(
            DiagnosticCode.MQ8002_CompiledArtifactIncompatible,
            message,
            TextSpan.Empty);
    }
}
