using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter;

internal static class CSharpClrCompiledArtifactLoader
{
    public static bool TryGetRequiredMetadata(
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

    public static ClrLoadedExecutableArtifact CreateLoadedExecutableArtifact(
        CompiledQueryArtifactLoadResult? loadResult)
    {
        if (loadResult == null)
            throw new InvalidOperationException("Compiled artifact loader returned a null load result.");

        var artifact = ExecutionTargetCatalog
            .ResolveActivator(ExecutionTargetIds.CSharpClr)
            .CreateLoadedExecutableArtifact(loadResult.RunnableType, loadResult.LifetimeOwner);

        return CSharpClrArtifactCompatibility.RequireLoadedExecutable(
            artifact,
            "compiled artifact loader");
    }

    public static ClrLoadedExecutableArtifact LoadExecutableArtifactFromArtifactBytes(
        ICompiledQueryArtifact artifact)
    {
        var assemblyBytes = artifact is CompiledQueryArtifact ownedArtifact
            ? ownedArtifact.AssemblyBytesUnsafe
            : artifact.AssemblyBytes;
        var symbolsBytes = artifact is CompiledQueryArtifact ownedSymbolsArtifact
            ? ownedSymbolsArtifact.SymbolsBytesUnsafe
            : artifact.SymbolsBytes;

        if (assemblyBytes is not { Length: > 0 })
            throw new InvalidOperationException("Compiled artifact assembly bytes are empty.");

        var loadContext = new CompiledQueryArtifactAssemblyLoadContext($"musoq-artifact-{Guid.NewGuid()}");
        try
        {
            using var assemblyStream = new MemoryStream(assemblyBytes, writable: false);
            Assembly assembly;
            if (symbolsBytes is { Length: > 0 })
            {
                using var symbolsStream = new MemoryStream(symbolsBytes, writable: false);
                assembly = loadContext.LoadFromStream(assemblyStream, symbolsStream);
            }
            else
            {
                assembly = loadContext.LoadFromStream(assemblyStream);
            }

            var runnableType = assembly.GetType(artifact.RunnableTypeName)
                               ?? throw new InvalidOperationException(
                                   $"Type {artifact.RunnableTypeName} was not found in artifact assembly {assembly.FullName}.");
            var loadedArtifact = ExecutionTargetCatalog
                .ResolveActivator(ExecutionTargetIds.CSharpClr)
                .CreateLoadedExecutableArtifact(
                    runnableType,
                    new CompiledQueryArtifactLoadContextLifetime(loadContext));

            return CSharpClrArtifactCompatibility.RequireLoadedExecutable(
                loadedArtifact,
                "compiled artifact byte loading");
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    public static void ValidateArtifactCompatibility(
        string script,
        ICompiledQueryArtifact artifact,
        string assemblyName,
        CompilationOptions compilationOptions,
        CompiledQueryArtifactValidationMode validationMode,
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

        var expectedRunnableTypeName = CompiledQueryArtifactSupport.GetRunnableTypeName(assemblyName);
        ValidateEqual(
            "runnable type name",
            expectedRunnableTypeName,
            artifact.RunnableTypeName,
            diagnostics);

        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataArtifactKind,
            CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
            diagnostics);
        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature,
            RuntimeV2Contract.ContractSignature,
            diagnostics);
        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion,
            ExecutionSemanticsContract.Version1.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            diagnostics);
        ValidateOptionalMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataExecutionTarget,
            ExecutionTargetIds.CSharpClr.ToString(),
            diagnostics);
        ValidateOptionalMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataExecutableArtifactKind,
            CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
            diagnostics);
        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataScriptSha256,
            CompiledQueryArtifactSupport.ComputeHash(script),
            diagnostics);
        ValidateMetadataValue(
            artifact,
            CompiledQueryArtifactSupport.MetadataSemanticShapeSha256,
            CompiledQueryArtifactSupport.ComputeSemanticShapeHash(
                TargetArtifactSemanticFactsFactory.From(items),
                expectedRunnableTypeName),
            diagnostics);

        if (validationMode == CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash)
        {
            ValidateMetadataValue(
                artifact,
                CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256,
                CSharpClrArtifactCompatibility.ComputeGeneratedCodeHash(items.RenderingArtifact),
                diagnostics);
        }

        var assemblyBytes = artifact is CompiledQueryArtifact ownedArtifact
            ? ownedArtifact.AssemblyBytesUnsafe
            : artifact.AssemblyBytes;
        if (assemblyBytes is not { Length: > 0 })
            diagnostics.Add(CreateArtifactDiagnostic("Compiled artifact assembly bytes are empty."));
    }

    public static void ValidateLoadedRunnableType(
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

    public static void DisposeArtifactLifetime(IDisposable? lifetimeOwner)
    {
        try
        {
            lifetimeOwner?.Dispose();
        }
        catch
        {
            // The artifact load already failed; disposal best-effort avoids hiding the diagnostic cause.
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

    private static void ValidateOptionalMetadataValue(
        ICompiledQueryArtifact artifact,
        string key,
        string expected,
        ICollection<Diagnostic> diagnostics)
    {
        if (artifact.Metadata == null ||
            !artifact.Metadata.TryGetValue(key, out var actual) ||
            string.IsNullOrWhiteSpace(actual))
        {
            return;
        }

        ValidateEqual($"metadata '{key}'", expected, actual, diagnostics);
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

    private static Diagnostic CreateArtifactDiagnostic(string message)
    {
        return Diagnostic.Error(
            DiagnosticCode.MQ8002_CompiledArtifactIncompatible,
            message,
            TextSpan.Empty);
    }

    private sealed class CompiledQueryArtifactAssemblyLoadContext(string name)
        : AssemblyLoadContext(name, isCollectible: true)
    {
        private static readonly FrozenDictionary<string, Assembly> DefaultAssembliesByName =
            Default.Assemblies
                .Where(static assembly => assembly.GetName().Name is not null)
                .GroupBy(static assembly => assembly.GetName().Name!, StringComparer.OrdinalIgnoreCase)
                .ToFrozenDictionary(
                    static group => group.Key,
                    static group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is { } simpleName &&
                DefaultAssembliesByName.TryGetValue(simpleName, out var assembly) &&
                AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName))
            {
                return assembly;
            }

            try
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (FileLoadException)
            {
                return null;
            }

        }
    }

    private sealed class CompiledQueryArtifactLoadContextLifetime(AssemblyLoadContext loadContext) : IDisposable
    {
        private AssemblyLoadContext? _loadContext = loadContext;

        public void Dispose()
        {
            var context = _loadContext;
            if (context == null)
                return;

            _loadContext = null;
            context.Unload();
        }
    }
}
