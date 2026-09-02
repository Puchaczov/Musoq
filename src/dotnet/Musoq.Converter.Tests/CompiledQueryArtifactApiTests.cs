using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests;

[TestClass]
public class CompiledQueryArtifactApiTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileArtifactWithDiagnostics_WhenQueryIsValid_ProducesArtifact()
    {
        const string query = "select i.Value from #artifact.items() i";
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "ArtifactBasic",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Artifact);
        Assert.IsTrue(result.Artifact.AssemblyBytes.Length > 0);
        Assert.AreEqual("ArtifactBasic.CompiledQuery", result.Artifact.RunnableTypeName);
        Assert.AreEqual(CompiledQueryArtifact.CurrentArtifactFormatVersion, result.Artifact.ArtifactFormatVersion);
        Assert.AreEqual("3", result.Artifact.ArtifactFormatVersion);
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("AssemblyName"));
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("ScriptSha256"));
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("SemanticShapeSha256"));
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("GeneratedCodeSha256"));
        Assert.AreEqual(
            RuntimeV2Contract.ContractSignature,
            result.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature]);
        Assert.AreEqual(
            "1",
            result.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion]);
        Assert.AreEqual(
            ExecutionTargetIds.CSharpClr.ToString(),
            result.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionTarget]);
        Assert.AreEqual(
            CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
            result.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutableArtifactKind]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenArtifactIsValid_RunsQuery()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful artifact load did not produce a compiled query.");
        var table = compiledQuery.Run();
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenFreshArtifactIsStrictlyValidated_ShouldMatchGeneratedCodeHash()
    {
        const string query = "select e.Name from #test.entities() e";
        var provider = new EntitySetSchemaProvider(
            new Dictionary<string, IReadOnlyList<EntitySetEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["#test"] =
                [
                    new EntitySetEntity
                    {
                        Name = "cached"
                    }
                ]
            });
        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "ArtifactStrictRoundTrip",
            provider,
            _loggerResolver);

        Assert.IsTrue(
            artifactResult.Succeeded,
            string.Join(
                Environment.NewLine,
                artifactResult.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        Assert.IsNotNull(artifactResult.Artifact);
        Assert.IsTrue(
            artifactResult.Artifact.Metadata.TryGetValue(
                CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256,
                out var storedGeneratedCodeHash));
        Assert.IsFalse(string.IsNullOrWhiteSpace(storedGeneratedCodeHash));

        var loadResult = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifactResult.Artifact,
            provider,
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            });

        Assert.IsTrue(
            loadResult.Succeeded,
            string.Join(
                Environment.NewLine,
                loadResult.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        Assert.IsNotNull(loadResult.BuildItems);
        var recomputedGeneratedCodeHash =
            CSharpClrArtifactCompatibility.ComputeGeneratedCodeHash(loadResult.BuildItems.RenderingArtifact);
        Assert.AreEqual(storedGeneratedCodeHash, recomputedGeneratedCodeHash);
        var compiledQuery = loadResult.CompiledQuery ??
            throw new AssertFailedException("Successful artifact validation did not produce a compiled query.");
        Assert.AreEqual("cached", compiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenProviderChanges_RebindsCurrentProvider()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("first")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("second")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful provider rebind did not produce a compiled query.");
        Assert.AreEqual("second", compiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenRuntimeSettingsChange_RebindsCurrentSettingsAndPlans()
    {
        const string query = "select i.Token from #settings.items() i";
        var compileProvider = new SettingsArtifactSchemaProvider();
        var compileOptions = new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("compile-token"));
        var artifact = CompileArtifact(query, compileProvider, compileOptions);

        var loadProvider = new SettingsArtifactSchemaProvider();
        var loadOptions = new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("load-token"));
        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            loadProvider,
            _loggerResolver,
            loadOptions);

        Assert.IsTrue(result.Succeeded);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful settings rebind did not produce a compiled query.");
        Assert.AreEqual("load-token", compiledQuery.Run()[0][0]);
        Assert.IsGreaterThanOrEqualTo(loadProvider.Schema.PlanCount, 1);
        Assert.IsGreaterThanOrEqualTo(loadProvider.Schema.DescribeRuntimeSettingsCount, 1);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenCustomLoaderIsProvided_UsesLoader()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var invoked = false;

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            typeLoader: loadedArtifact =>
            {
                invoked = true;
                var context = new AssemblyLoadContext($"artifact-test-{Guid.NewGuid()}", isCollectible: true);
                using var assemblyStream = new MemoryStream(loadedArtifact.AssemblyBytes);
                if (loadedArtifact.SymbolsBytes is { Length: > 0 } symbols)
                {
                    using var symbolsStream = new MemoryStream(symbols);
                    return context.LoadFromStream(assemblyStream, symbolsStream)
                        .GetType(loadedArtifact.RunnableTypeName)!;
                }

                return context.LoadFromStream(assemblyStream)
                    .GetType(loadedArtifact.RunnableTypeName)!;
            });

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(invoked);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful custom load did not produce a compiled query.");
        Assert.AreEqual("single", compiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenLifecycleLoaderIsProvided_DisposesOwnerWithQuery()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        TestLifetimeOwner? owner = null;

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions(),
            loader: loadedArtifact =>
            {
                owner = new TestLifetimeOwner();
                var assembly = Assembly.Load(loadedArtifact.AssemblyBytes);
                return new CompiledQueryArtifactLoadResult(
                    assembly.GetType(loadedArtifact.RunnableTypeName)!,
                    owner);
            });

        Assert.IsTrue(result.Succeeded);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful lifecycle load did not produce a compiled query.");
        Assert.AreEqual("single", compiledQuery.Run()[0][0]);
        compiledQuery.Dispose();
        Assert.IsTrue(owner!.Disposed);
        Assert.Throws<ObjectDisposedException>(() => compiledQuery.Run());
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenDefaultLoaderIsUsed_LoadsCollectibleContext()
    {
        var weakReference = CreateDefaultLoadedContextWeakReference();

        for (var i = 0; i < 10 && weakReference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.IsFalse(weakReference.IsAlive);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenOptionsMismatch_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i where true";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            new CompilationOptions(useConstantFolding: false));

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("compilation options signature", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenScriptMismatch_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            "select i.Value from #artifact.items() i where i.Value = 'single'",
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("ScriptSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenRuntimeV2ContractSignatureMismatch_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var metadata = CopyMetadata(artifact);
        metadata[CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature] = "runtime-v2=old";
        var tampered = CopyArtifact(artifact, metadata: metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            tampered,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("RuntimeV2ContractSignature", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenExecutionSemanticsVersionMismatch_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var metadata = CopyMetadata(artifact);
        metadata[CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion] = "2";
        var tampered = CopyArtifact(artifact, metadata: metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            tampered,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains(
            CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion,
            StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenExecutionTargetIsNonClr_ReturnsArtifactDiagnosticBeforeLoading()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var metadata = CopyMetadata(artifact);
        metadata[CompiledQueryArtifactSupport.MetadataExecutionTarget] = TestExecutionTargetIds.TestOnlyNonClr.ToString();
        var tampered = CopyArtifact(artifact, metadata: metadata);
        var loaderInvoked = false;

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            tampered,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            loadOptions: new CompiledQueryArtifactLoadOptions(),
            loader: _ =>
            {
                loaderInvoked = true;
                return null!;
            });

        AssertArtifactFailure(result);
        Assert.IsFalse(loaderInvoked);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains(
            CompiledQueryArtifactSupport.MetadataExecutionTarget,
            StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenSchemaShapeChanges_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactIntSchema(1)),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("SemanticShapeSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenReadModifiersChange_ReturnsSemanticShapeDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactReadModifierSchema("utf-8")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactReadModifierSchema("ascii")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("SemanticShapeSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenIntendedTypeChanges_ReturnsSemanticShapeDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactIntendedTypeSchema(typeof(string).FullName!)));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactIntendedTypeSchema(typeof(object).FullName!)),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("SemanticShapeSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenGeneratedHashIsMissing_FastModeStillLoads()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var metadata = CopyMetadataWithout(artifact, "GeneratedCodeSha256");
        var withoutGeneratedHash = CopyArtifact(artifact, metadata: metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            withoutGeneratedHash,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful hash validation did not produce a compiled query.");
        Assert.AreEqual("single", compiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenStrictModeGeneratedHashIsMissing_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var metadata = CopyMetadataWithout(artifact, "GeneratedCodeSha256");
        var withoutGeneratedHash = CopyArtifact(artifact, metadata: metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            withoutGeneratedHash,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            });

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("GeneratedCodeSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenStrictModeGeneratedHashDiffers_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var metadata = CopyMetadata(artifact);
        metadata["GeneratedCodeSha256"] = "BAD";
        var tampered = CopyArtifact(artifact, metadata: metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            tampered,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            });

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("GeneratedCodeSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutionCompilationCacheKey_WhenRuntimeV2ContractIsPresent_ShouldIncludeSignature()
    {
        var signature = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            "select i.Value from #artifact.items() i",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            new CompilationOptions());

        StringAssert.Contains(signature, RuntimeV2Contract.ContractSignature);
        StringAssert.Contains(signature, ExecutionSemanticsContract.Version1.Fingerprint);
        StringAssert.Contains(signature, "ExecutionTarget = CSharpClr");
    }

    [TestMethod]
    public void CreateExecutionCompilationCacheKey_WhenExecutionTargetChanges_ShouldChangeSignature()
    {
        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));
        var options = new CompilationOptions();

        var csharp = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            options,
            ExecutionTargetIds.CSharpClr);
        var nonClr = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            options,
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.AreNotEqual(csharp, nonClr);
        StringAssert.Contains(csharp, "ExecutionTarget = CSharpClr");
        StringAssert.Contains(nonClr, "ExecutionTarget = TestOnlyNonClr");
    }

    [TestMethod]
    public void CreateExecutionCompilationCacheKey_WhenProviderContractChanges_ShouldUseDifferentBucket()
    {
        const string query = "select i.Value from #artifact.items() i";

        var first = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            new ArtifactSchemaProvider(new ArtifactSchema("first")),
            new CompilationOptions());
        var second = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            new ArtifactSchemaProvider(new ArtifactSchema("second")),
            new CompilationOptions());

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void CreateExecutionCompilationCacheKey_WhenCteSidecarIndexesChange_ShouldChangeSignature()
    {
        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));

        var withoutSidecars = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            new CompilationOptions(useCteSidecarIndexes: false));
        var withSidecars = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            new CompilationOptions(useCteSidecarIndexes: true));

        Assert.AreNotEqual(withoutSidecars, withSidecars);
        StringAssert.Contains(
            withoutSidecars,
            CompilationOptionsFingerprint.Compute(new CompilationOptions(useCteSidecarIndexes: false)));
        StringAssert.Contains(
            withSidecars,
            CompilationOptionsFingerprint.Compute(new CompilationOptions(useCteSidecarIndexes: true)));
    }

    [TestMethod]
    public void StabilityAwareScalarReuse_WhenChanged_ShouldSeparateCacheAndArtifactSignatures()
    {
        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));
        var disabled = new CompilationOptions().WithStabilityAwareScalarReuse(false);
        var enabled = disabled.WithStabilityAwareScalarReuse();

        var disabledCacheSignature = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            disabled);
        var enabledCacheSignature = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            enabled);

        Assert.AreNotEqual(disabledCacheSignature, enabledCacheSignature);
        Assert.AreNotEqual(
            CompilationOptionsFingerprint.Compute(disabled),
            CompilationOptionsFingerprint.Compute(enabled));
        Assert.AreNotEqual(
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(disabled),
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(enabled));
    }

    [TestMethod]
    public void RecursiveCteLimits_WhenChanged_ShouldSeparateCacheAndArtifactSignatures()
    {
        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));
        var lower = new CompilationOptions().WithRecursiveCteLimits(new(20, 30, 40));
        var higher = new CompilationOptions().WithRecursiveCteLimits(new(20, 30, 400));

        var lowerCacheSignature = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            lower);
        var higherCacheSignature = InstanceCreator.CreateExecutionCompilationCacheKeyTestSignature(
            query,
            provider,
            higher);

        Assert.AreNotEqual(lowerCacheSignature, higherCacheSignature);
        StringAssert.Contains(lowerCacheSignature, CompilationOptionsFingerprint.Compute(lower));
        StringAssert.Contains(higherCacheSignature, CompilationOptionsFingerprint.Compute(higher));
        Assert.AreNotEqual(
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(lower),
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(higher));
        Assert.AreEqual(
            CompilationOptionsFingerprint.Compute(lower),
            CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(lower));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenLoaderReturnsWrongType_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            typeLoader: _ => typeof(string));

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("does not implement", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenAssemblyBytesAreInvalid_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var broken = new CompiledQueryArtifact(
            [1, 2, 3, 4],
            null,
            artifact.RunnableTypeName,
            artifact.EngineVersion,
            artifact.ArtifactFormatVersion,
            artifact.CompilationOptionsSignature,
            artifact.Metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            broken,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("type loading failed", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenArtifactFormatIsOld_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var oldFormat = CopyArtifact(artifact, artifactFormatVersion: "1");

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            oldFormat,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("artifact format version", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenAssemblyBytesAreEmpty_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var emptyBytes = new TestCompiledQueryArtifact(
            [],
            null,
            artifact.RunnableTypeName,
            artifact.EngineVersion,
            artifact.ArtifactFormatVersion,
            artifact.CompilationOptionsSignature,
            artifact.Metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            emptyBytes,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("assembly bytes are empty", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CompileArtifactWithDiagnostics_WhenQueryIsInvalid_ReturnsDiagnosticsWithoutArtifact()
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            "select Missing from #artifact.items() i",
            "ArtifactInvalid",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Artifact);
        Assert.IsTrue(result.Errors.Count > 0);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn));
    }

    [TestMethod]
    public void CompiledQueryArtifact_WhenConstructed_DefensivelyCopiesBytesAndMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["key"] = "value"
        };
        var assemblyBytes = new byte[] { 1, 2, 3 };
        var symbolsBytes = new byte[] { 4, 5 };
        var artifact = new CompiledQueryArtifact(
            assemblyBytes,
            symbolsBytes,
            "Runnable",
            "Engine",
            "1",
            "Options",
            metadata);

        assemblyBytes[0] = 9;
        symbolsBytes[0] = 9;
        metadata["key"] = "changed";
        var returnedAssembly = artifact.AssemblyBytes;
        returnedAssembly[1] = 9;

        Assert.AreEqual(1, artifact.AssemblyBytes[0]);
        Assert.AreEqual(2, artifact.AssemblyBytes[1]);
        Assert.AreEqual(4, artifact.SymbolsBytes![0]);
        Assert.AreEqual("value", artifact.Metadata["key"]);
    }

    private ICompiledQueryArtifact CompileArtifact(
        string query,
        ISchemaProvider provider,
        CompilationOptions? options = null)
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "ArtifactBasic",
            provider,
            _loggerResolver,
            options);

        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        return result.Artifact ?? throw new AssertFailedException("Successful artifact compilation produced no artifact.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference CreateDefaultLoadedContextWeakReference()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        var compiledQuery = result.CompiledQuery ??
            throw new AssertFailedException("Successful artifact load did not produce a compiled query.");
        var runnable = GetRunnable(compiledQuery);
        var loadContext = AssemblyLoadContext.GetLoadContext(runnable.GetType().Assembly);
        Assert.IsNotNull(loadContext);
        Assert.IsTrue(loadContext.IsCollectible);
        var weakReference = new WeakReference(loadContext);

        compiledQuery.Dispose();
        Assert.Throws<ObjectDisposedException>(() => compiledQuery.Run());

        return weakReference;
    }

    private static ITableRunnable GetRunnable(CompiledQuery compiledQuery)
    {
        var field = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (ITableRunnable)field.GetValue(compiledQuery)!;
    }

    private static Dictionary<string, string> CopyMetadata(ICompiledQueryArtifact artifact)
    {
        return new Dictionary<string, string>(artifact.Metadata, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> CopyMetadataWithout(ICompiledQueryArtifact artifact, string key)
    {
        var metadata = CopyMetadata(artifact);
        metadata.Remove(key);
        return metadata;
    }

    private static ICompiledQueryArtifact CopyArtifact(
        ICompiledQueryArtifact artifact,
        byte[]? assemblyBytes = null,
        byte[]? symbolsBytes = null,
        string? runnableTypeName = null,
        string? engineVersion = null,
        string? artifactFormatVersion = null,
        string? compilationOptionsSignature = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new CompiledQueryArtifact(
            assemblyBytes ?? artifact.AssemblyBytes,
            symbolsBytes ?? artifact.SymbolsBytes,
            runnableTypeName ?? artifact.RunnableTypeName,
            engineVersion ?? artifact.EngineVersion,
            artifactFormatVersion ?? artifact.ArtifactFormatVersion,
            compilationOptionsSignature ?? artifact.CompilationOptionsSignature,
            metadata ?? artifact.Metadata);
    }

    private static void AssertArtifactFailure(BuildResult result)
    {
        Assert.IsFalse(result.Succeeded);
        var diagnostic = result.Errors.Single(static item =>
            item.Code == DiagnosticCode.MQ8002_CompiledArtifactIncompatible);
        Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, diagnostic.SourceKind);
        Assert.IsFalse(diagnostic.Location.IsValid);
        Assert.IsFalse(diagnostic.EndLocation.IsValid);

        var envelope = result.ToEnvelopes().Single(item =>
            item.Code == DiagnosticCode.MQ8002_CompiledArtifactIncompatible);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, envelope.SourceKind);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.EndOffset);
        Assert.IsNull(envelope.Snippet);
    }
}

public sealed class ArtifactSchemaProvider(ISchema schema) : ISchemaProvider
{
    public ISchema GetSchema(string schemaName)
    {
        return schema;
    }
}

public sealed class TestLifetimeOwner : IDisposable
{
    public bool Disposed { get; private set; }

    public void Dispose()
    {
        Disposed = true;
    }
}

public sealed class TestCompiledQueryArtifact(
    byte[] assemblyBytes,
    byte[]? symbolsBytes,
    string runnableTypeName,
    string engineVersion,
    string artifactFormatVersion,
    string compilationOptionsSignature,
    IReadOnlyDictionary<string, string> metadata)
    : ICompiledQueryArtifact
{
    public byte[] AssemblyBytes { get; } = assemblyBytes;

    public byte[]? SymbolsBytes { get; } = symbolsBytes;

    public string RunnableTypeName { get; } = runnableTypeName;

    public string EngineVersion { get; } = engineVersion;

    public string ArtifactFormatVersion { get; } = artifactFormatVersion;

    public string CompilationOptionsSignature { get; } = compilationOptionsSignature;

    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;
}

public sealed class ArtifactSchema(string value) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactTable(typeof(ArtifactRow), typeof(string));
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource(value));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class ArtifactReadModifierSchema(string modifierValue) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactReadModifierTable(modifierValue);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource("single"));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class ArtifactIntendedTypeSchema(string intendedTypeName) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactIntendedTypeTable(intendedTypeName);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource("single"));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class ArtifactIntSchema(int value) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactTable(typeof(ArtifactIntRow), typeof(int));
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactIntRow>(name, new ArtifactIntRowSource(value));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactIntRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class SettingsArtifactSchemaProvider : ISchemaProvider
{
    public SettingsArtifactSchema Schema { get; } = new();

    public ISchema GetSchema(string schemaName)
    {
        return Schema;
    }
}

public sealed class SettingsArtifactSchema() : SchemaBase("settings", CreateLibrary())
{
    public int DescribeRuntimeSettingsCount { get; private set; }

    public int PlanCount { get; private set; }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new SettingsArtifactTable();
    }

    public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        DescribeRuntimeSettingsCount++;
        return
        [
            new SourceRuntimeSettingRequirement(
                "TOKEN",
                Required: true,
                Secret: false,
                SourceRuntimeSettingPhase.All,
                "Token used by artifact tests.")
        ];
    }

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        PlanCount++;
        return SourcePlanResult.RejectAll(request);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        executionContext.SourceRuntimeSettings.TryGetValue("TOKEN", out var token);
        return EnsureSourceType<T, SettingsArtifactRow>(name, new SettingsArtifactRowSource(token ?? "<missing>"));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<SettingsArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class TokenSettingsResolver(string token) : ISourceRuntimeSettingsResolver
{
    public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TOKEN"] = token
        };
    }
}

public sealed class ArtifactTable(Type rowType, Type columnType) : ISchemaTable
{
    public ISchemaColumn[] Columns => [new SchemaColumn("Value", 0, columnType)];

    public SchemaTableMetadata Metadata { get; } = new(rowType);

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class ArtifactReadModifierTable(string modifierValue) : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(
            "Value",
            0,
            typeof(string),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["encoding"] = modifierValue
            })
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(ArtifactRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class ArtifactIntendedTypeTable(string intendedTypeName) : ISchemaTable
{
    public ISchemaColumn[] Columns => [new SchemaColumn("Value", 0, typeof(string), intendedTypeName)];

    public SchemaTableMetadata Metadata { get; } = new(typeof(ArtifactRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class SettingsArtifactTable : ISchemaTable
{
    public ISchemaColumn[] Columns => [new SchemaColumn("Token", 0, typeof(string))];

    public SchemaTableMetadata Metadata { get; } = new(typeof(SettingsArtifactRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class ArtifactRow(string value)
{
    public string Value { get; } = value;
}

public sealed class ArtifactIntRow(int value)
{
    public int Value { get; } = value;
}

public sealed class ArtifactObjectRow(object value)
{
    public object Value { get; } = value;
}

public sealed class SettingsArtifactRow(string token)
{
    public string Token { get; } = token;
}

public sealed class ArtifactRowSource() : RowSourceBase<ArtifactRow>
{
    private readonly string _value = string.Empty;

    public ArtifactRowSource(string value) : this()
    {
        _value = value;
    }

    protected override void CollectChunks(IChunkWriter<ArtifactRow> writer)
    {
        writer.Write([new ArtifactRow(_value)]);
    }
}

public sealed class ArtifactIntRowSource() : RowSourceBase<ArtifactIntRow>
{
    private readonly int _value;

    public ArtifactIntRowSource(int value) : this()
    {
        _value = value;
    }

    protected override void CollectChunks(IChunkWriter<ArtifactIntRow> writer)
    {
        writer.Write([new ArtifactIntRow(_value)]);
    }
}

public sealed class ArtifactObjectRowSource() : RowSourceBase<ArtifactObjectRow>
{
    private readonly object _value = string.Empty;

    public ArtifactObjectRowSource(object value) : this()
    {
        _value = value;
    }

    protected override void CollectChunks(IChunkWriter<ArtifactObjectRow> writer)
    {
        writer.Write([new ArtifactObjectRow(_value)]);
    }
}

public sealed class SettingsArtifactRowSource() : RowSourceBase<SettingsArtifactRow>
{
    private readonly string _token = string.Empty;

    public SettingsArtifactRowSource(string token) : this()
    {
        _token = token;
    }

    protected override void CollectChunks(IChunkWriter<SettingsArtifactRow> writer)
    {
        writer.Write([new SettingsArtifactRow(_token)]);
    }
}
