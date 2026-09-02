using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetPipelineEndToEndTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CSharpClrTarget_ShouldCompilePackageLoadExecuteAndInspect()
    {
        const string query = "select i.Value from #artifact.items() i";
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));

        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "TargetPipelineE2E",
            provider,
            _loggerResolver);

        Assert.IsTrue(artifactResult.Succeeded, FormatDiagnostics(artifactResult.Diagnostics));
        Assert.IsNotNull(artifactResult.Artifact);
        Assert.IsTrue(artifactResult.Artifact.AssemblyBytes.Length > 0);
        Assert.AreEqual("TargetPipelineE2E.CompiledQuery", artifactResult.Artifact.RunnableTypeName);
        Assert.AreEqual(
            ExecutionTargetIds.CSharpClr.ToString(),
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionTarget]);
        Assert.AreEqual(
            CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutableArtifactKind]);
        Assert.AreEqual(
            "1",
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion]);

        var packageResult = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            "TargetPipelineE2EPackage",
            provider,
            _loggerResolver,
            ExecutionTargetIds.CSharpClr);

        Assert.IsTrue(packageResult.Succeeded, FormatDiagnostics(packageResult.Diagnostics));
        Assert.IsNotNull(packageResult.BuildItems);
        Assert.IsNotNull(packageResult.BuildItems.ExecutionTargetCompatibilityReport);
        Assert.IsNotNull(packageResult.BuildItems.TargetRuntimeContract);
        Assert.IsNotNull(packageResult.BuildItems.ExecutionTargetReadinessReport);
        Assert.IsNotNull(packageResult.Package);
        Assert.AreEqual(1, packageResult.Package.SemanticsContract.Version);

        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifactResult.Artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("loaded")),
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            });

        Assert.IsTrue(loaded.Succeeded, FormatDiagnostics(loaded.Diagnostics));
        Assert.IsNotNull(loaded.BuildItems);
        Assert.AreEqual(
            artifactResult.Artifact.Metadata[CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256],
            CSharpClrArtifactCompatibility.ComputeGeneratedCodeHash(loaded.BuildItems.RenderingArtifact));
        var loadedCompiledQuery = loaded.CompiledQuery ??
            throw new AssertFailedException("Successful artifact load did not produce a compiled query.");
        var table = loadedCompiledQuery.Run();
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("loaded", table[0][0]);

        var inspection = InstanceCreator.CompileForInspection(
            query,
            "TargetPipelineE2EInspection",
            provider,
            _loggerResolver);

        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));
        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.ExecutionPlanText));
        Assert.IsNotNull(inspection.ExecutionPlan);
        StringAssert.Contains(inspection.GeneratedCSharpCode, "CompiledQuery");
    }

    [TestMethod]
    public void CSharpClrTarget_WhenCompilationFails_ShouldReturnDiagnosticsWithoutArtifact()
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            "select Missing from #artifact.items() i",
            "TargetPipelineInvalid",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Artifact);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn));
    }

    [TestMethod]
    public void CSharpClrTarget_WhenExecutionCompilationCacheIsWarm_ShouldActivateFromCachedArtifactAfterPlanning()
    {
        if (Debugger.IsAttached)
            return;

        var query = $"select d.Dummy from #system.dual() d where d.Dummy = 'single'";
        var provider = new SystemSchemaProvider();
        var options = new CompilationOptions();

        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineCacheFirst",
            provider,
            _loggerResolver,
            options);
        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsNotNull(first.BuildItems);

        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineCacheSecond",
            provider,
            _loggerResolver,
            options);

        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(second.BuildItems);
        Assert.IsTrue(second.BuildItems.StopAfterPlanning);
        var secondCompiledQuery = second.CompiledQuery ??
            throw new AssertFailedException("Successful cache activation did not produce a compiled query.");
        Assert.AreEqual("single", secondCompiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CanonicalExecutionCache_WhenWhitespaceChanges_ShouldReuseArtifactButRebindCurrentProvider()
    {
        if (Debugger.IsAttached)
            return;

        var suffix = Guid.NewGuid().ToString("N");
        var first = InstanceCreator.CompileWithDiagnostics(
            "select i.Value from #artifact.items() i",
            $"CanonicalWhitespaceFirst_{suffix}",
            new ArtifactSchemaProvider(new ArtifactSchema("first")),
            _loggerResolver,
            new CompilationOptions());
        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsNotNull(first.BuildItems);

        var firstEntryIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            first.BuildItems!,
            new ArtifactSchemaProvider(new ArtifactSchema("first")));
        Assert.AreNotEqual(0, firstEntryIdentity);

        var secondProvider = new ArtifactSchemaProvider(new ArtifactSchema("second"));
        var second = InstanceCreator.CompileWithDiagnostics(
            "select  i.Value  from  #artifact.items()  i",
            $"CanonicalWhitespaceSecond_{suffix}",
            secondProvider,
            _loggerResolver,
            new CompilationOptions());
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(second.BuildItems);

        var secondEntryIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            second.BuildItems,
            secondProvider);
        var firstContract = InstanceCreator.CreateCanonicalExecutionContractForTests(
            first.BuildItems!,
            new ArtifactSchemaProvider(new ArtifactSchema("first")));
        var secondContract = InstanceCreator.CreateCanonicalExecutionContractForTests(
            second.BuildItems!,
            secondProvider);
        Assert.AreEqual(firstContract.NormalizedGeneratedSyntax, secondContract.NormalizedGeneratedSyntax);
        Assert.AreEqual(firstContract.RuntimeContractFingerprint, secondContract.RuntimeContractFingerprint);
        Assert.AreEqual(firstContract.ExecutionSemanticsFingerprint, secondContract.ExecutionSemanticsFingerprint);
        Assert.AreEqual(firstContract.ExecutionTarget, secondContract.ExecutionTarget);
        Assert.AreEqual(firstContract.ResultMode, secondContract.ResultMode);
        Assert.AreEqual(firstContract.OutputType, secondContract.OutputType);
        Assert.AreEqual(firstContract.CompilationOptionsFingerprint, secondContract.CompilationOptionsFingerprint);
        Assert.AreEqual(firstContract.OrderedReferenceIdentities, secondContract.OrderedReferenceIdentities);
        Assert.AreEqual(firstContract.ProviderContractFingerprint, secondContract.ProviderContractFingerprint);
        Assert.AreEqual(firstContract.InterpreterState, secondContract.InterpreterState);
        Assert.AreEqual(firstContract.SemanticContractFingerprint, secondContract.SemanticContractFingerprint);
        Assert.AreEqual(firstEntryIdentity, secondEntryIdentity);

        using var firstTable = first.CompiledQuery!.Run();
        using var secondTable = second.CompiledQuery!.Run();
        Assert.AreEqual("first", firstTable[0][0]);
        Assert.AreEqual("second", secondTable[0][0]);
    }

    [TestMethod]
    public async Task CanonicalExecutionCache_WhenEquivalentRendersStartTogether_ShouldSingleFlight()
    {
        if (Debugger.IsAttached)
            return;

        var suffix = Guid.NewGuid().ToString("N");
        var providers = new[]
        {
            new ArtifactSchemaProvider(new ArtifactSchema("concurrent-first")),
            new ArtifactSchemaProvider(new ArtifactSchema("concurrent-second"))
        };
        var queries = new[]
        {
            $"select i.Value from #artifact.items() i where i.Value = '{suffix}'",
            $"select  i.Value  from  #artifact.items()  i  where  i.Value = '{suffix}'"
        };

        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(index => Task.Run(() =>
            InstanceCreator.CompileWithDiagnostics(
                queries[index],
                $"CanonicalConcurrent_{suffix}_{index}",
                providers[index],
                _loggerResolver,
                new CompilationOptions()))));

        Assert.IsTrue(results.All(static result => result.Succeeded));
        Assert.IsNotNull(results[0].BuildItems);
        Assert.IsNotNull(results[1].BuildItems);
        var firstIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(results[0].BuildItems!, providers[0]);
        var secondIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(results[1].BuildItems!, providers[1]);
        Assert.AreNotEqual(0, firstIdentity);
        Assert.AreEqual(firstIdentity, secondIdentity);

        using var firstTable = results[0].CompiledQuery!.Run();
        using var secondTable = results[1].CompiledQuery!.Run();
        Assert.AreEqual(0, firstTable.Count);
        Assert.AreEqual(0, secondTable.Count);
    }

    [TestMethod]
    public void CanonicalExecutionCache_WhenGeneratedLiteralChanges_ShouldNotReuseArtifact()
    {
        if (Debugger.IsAttached)
            return;

        var suffix = Guid.NewGuid().ToString("N");
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("literal"));
        var first = InstanceCreator.CompileWithDiagnostics(
            $"select i.Value from #artifact.items() i where i.Value = 'literal-a-{suffix}'",
            $"CanonicalLiteralFirst_{suffix}",
            provider,
            _loggerResolver,
            new CompilationOptions());
        var second = InstanceCreator.CompileWithDiagnostics(
            $"select i.Value from #artifact.items() i where i.Value = 'literal-b-{suffix}'",
            $"CanonicalLiteralSecond_{suffix}",
            provider,
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(first.BuildItems);
        Assert.IsNotNull(second.BuildItems);
        var firstIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems!, provider);
        var secondIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(second.BuildItems!, provider);
        Assert.AreNotEqual(0, firstIdentity);
        Assert.AreNotEqual(firstIdentity, secondIdentity);
    }

    [TestMethod]
    public void CanonicalExecutionCache_WhenInstrumentationIsEnabled_ShouldStayIneligible()
    {
        if (Debugger.IsAttached)
            return;

        var result = InstanceCreator.CompileWithDiagnostics(
            "select i.Value from #artifact.items() i",
            $"CanonicalInstrumentation_{Guid.NewGuid():N}",
            new ArtifactSchemaProvider(new ArtifactSchema("instrumented")),
            _loggerResolver,
            new CompilationOptions().WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries));

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.BuildItems);
        Assert.AreEqual(
            0,
            InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
                result.BuildItems!,
                new ArtifactSchemaProvider(new ArtifactSchema("instrumented"))));
    }

    [TestMethod]
    public async Task ExecutionCompilationCache_WhenIdenticalCompilationsStartTogether_ShouldEmitOnce()
    {
        if (Debugger.IsAttached)
            return;

        var query = $"select d.Dummy from #system.dual() d where d.Dummy = '{Guid.NewGuid():N}'";
        var results = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(index => Task.Run(() => InstanceCreator.CompileWithDiagnostics(
                query,
                $"TargetPipelineConcurrent{index}",
                new SystemSchemaProvider(),
                _loggerResolver,
                new CompilationOptions()))));

        Assert.IsTrue(results.All(static result => result.Succeeded));
        Assert.AreEqual(1, results.Count(static result => result.BuildItems is { StopAfterPlanning: false }));
        Assert.IsTrue(results.All(static result => result.CompiledQuery!.Run().Count == 0));
    }

    [TestMethod]
    public async Task ExecutionCompilationCache_WhenBindingsDifferConcurrently_ShouldKeepRowsIsolated()
    {
        if (Debugger.IsAttached)
            return;

        const string query = "select i.Value from #artifact.items() i";
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(index => Task.Run(() =>
            {
                var provider = new ArtifactSchemaProvider(new ArtifactSchema($"binding-{index}"));
                var result = InstanceCreator.CompileWithDiagnostics(
                    query,
                    $"TargetPipelineBindingIsolation{index}",
                    provider,
                    _loggerResolver,
                    new CompilationOptions());

                Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
                using var table = result.CompiledQuery!.Run();
                return (Index: index, Value: (string)table[0][0]);
            })));

        Assert.HasCount(8, results);
        foreach (var result in results)
            Assert.AreEqual($"binding-{result.Index}", result.Value);
    }

    [TestMethod]
    public void ExecutionCompilationCache_WhenSourceSettingsResolverChanges_ShouldNotReuseCachedExecution()
    {
        if (Debugger.IsAttached)
            return;

        const string query = "select i.Token from #settings.items() i";
        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineSettingsFirst",
            new SettingsArtifactSchemaProvider(),
            _loggerResolver,
            new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("first")));
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "TargetPipelineSettingsSecond",
            new SettingsArtifactSchemaProvider(),
            _loggerResolver,
            new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("second")));

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(second.BuildItems);
        Assert.IsFalse(second.BuildItems.StopAfterPlanning);
        var secondCompiledQuery = second.CompiledQuery ??
            throw new AssertFailedException("Successful settings rebind did not produce a compiled query.");
        Assert.AreEqual("second", secondCompiledQuery.Run()[0][0]);
    }

    private static string FormatDiagnostics(System.Collections.Generic.IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}
