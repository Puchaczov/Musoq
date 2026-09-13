using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class StructuredInputArtifactQualificationTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void StructuredArtifact_CompilesLoadsAndRunsWithHostOverride()
    {
        const string query =
            "param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[]) " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: $patterns) m";
        var provider = new StructuredInputsSchemaProvider();

        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "StructuredArtifact",
            provider,
            _loggerResolver);

        Assert.IsTrue(artifactResult.Succeeded, FormatDiagnostics(artifactResult.Diagnostics));
        var artifact = artifactResult.Artifact ?? throw new AssertFailedException("No structured artifact was produced.");
        Assert.AreEqual(RuntimeV2Contract.ContractSignature, artifact.Metadata[CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature]);
        Assert.AreEqual(ExecutionTargetIds.CSharpClr.ToString(), artifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionTarget]);

        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new StructuredInputsSchemaProvider(),
            _loggerResolver,
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            });

        Assert.IsTrue(loaded.Succeeded, FormatDiagnostics(loaded.Diagnostics));
        using var compiled = loaded.CompiledQuery ?? throw new AssertFailedException("No loaded structured query was produced.");
        compiled.Parameters["patterns"] = new List<Dictionary<string, object?>>(2)
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "fixme",
                ["Pattern"] = "FIXME"
            },
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "todo",
                ["Pattern"] = "TODO"
            }
        };

        using var table = compiled.Run();
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual("fixme", table.Rows[0][0]);
        Assert.AreEqual("todo", table.Rows[1][0]);
    }

    [TestMethod]
    public void StructuredArtifact_CanonicalCacheReusesShapeButKeepsRunsIndependent()
    {
        if (System.Diagnostics.Debugger.IsAttached)
            return;

        var query =
            $"param(patterns: (Id: string, Pattern: string)[]) select m.PatternId from #inputs.match('TODO FIXME {Guid.NewGuid():N}', patterns: $patterns) m";
        InstanceCreator.ClearExecutionCompilationCacheForTests();
        var firstProvider = new StructuredInputsSchemaProvider();
        var secondProvider = new StructuredInputsSchemaProvider();

        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            "StructuredCacheFirst",
            firstProvider,
            _loggerResolver,
            new CompilationOptions(ParallelizationMode.None));
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            "StructuredCacheSecond",
            secondProvider,
            _loggerResolver,
            new CompilationOptions(ParallelizationMode.None));

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.IsNotNull(first.BuildItems);
        Assert.IsNotNull(second.BuildItems);
        var firstEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems!, firstProvider);
        Assert.AreNotEqual(0, firstEntry);
        Assert.IsTrue(second.BuildItems!.StopAfterPlanning, "The equivalent structured compilation should activate the cached artifact after planning.");

        using var firstQuery = first.CompiledQuery ?? throw new AssertFailedException("First structured query was not compiled.");
        using var secondQuery = second.CompiledQuery ?? throw new AssertFailedException("Second structured query was not compiled.");
        firstQuery.Parameters["patterns"] = CreatePatterns(("todo", "TODO"));
        secondQuery.Parameters["patterns"] = CreatePatterns(("fixme", "FIXME"));

        using var firstTable = firstQuery.Run();
        using var secondTable = secondQuery.Run();
        CollectionAssert.AreEqual(new[] { "todo" }, firstTable.Rows.Select(static row => (string)row[0]!).ToArray());
        CollectionAssert.AreEqual(new[] { "fixme" }, secondTable.Rows.Select(static row => (string)row[0]!).ToArray());
    }

    [TestMethod]
    public void StructuredArtifact_ContractChangesWhenDeclarationShapeOrDefaultChanges()
    {
        const string firstQuery =
            "param(patterns: (Id: string, Pattern: string)[] = array { (Id: 'todo', Pattern: 'TODO') }) " +
            "select m.PatternId from #inputs.match('TODO', patterns: $patterns) m";
        const string secondQuery =
            "param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[] = array { (Id: 'todo', Pattern: 'TODO', Mode: 'regex') }) " +
            "select m.PatternId from #inputs.match('TODO', patterns: $patterns) m";
        var first = Compile(firstQuery, "StructuredContractFirst");
        var second = Compile(secondQuery, "StructuredContractSecond");

        var firstContract = InstanceCreator.CreateCanonicalExecutionContractForTests(first.BuildItems!, new StructuredInputsSchemaProvider());
        var secondContract = InstanceCreator.CreateCanonicalExecutionContractForTests(second.BuildItems!, new StructuredInputsSchemaProvider());
        Assert.AreNotEqual(firstContract.SemanticContractFingerprint, secondContract.SemanticContractFingerprint);
        Assert.AreNotEqual(firstContract.NormalizedGeneratedSyntax, secondContract.NormalizedGeneratedSyntax);
        Assert.AreNotEqual(first.BuildItems!.ScriptParameterDefinitions.Single().Contract.CanonicalTypeName,
            second.BuildItems!.ScriptParameterDefinitions.Single().Contract.CanonicalTypeName);

        first.CompiledQuery?.Dispose();
        second.CompiledQuery?.Dispose();
    }

    [TestMethod]
    public void StructuredArtifact_FingerprintStaysStableForRunsAndChangesForAuthoredContracts()
    {
        const string query =
            "param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[]) " +
            "select m.PatternId from #inputs.match('TODO FIXME', patterns: $patterns) m";
        var provider = new StructuredInputsSchemaProvider();
        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "StructuredFingerprintStable",
            provider,
            _loggerResolver);

        Assert.IsTrue(artifactResult.Succeeded, FormatDiagnostics(artifactResult.Diagnostics));
        var artifact = artifactResult.Artifact ?? throw new AssertFailedException("No fingerprint artifact was produced.");
        var fingerprint = artifact.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256];

        var firstLoaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(firstLoaded.Succeeded, FormatDiagnostics(firstLoaded.Diagnostics));
        using (var firstQuery = firstLoaded.CompiledQuery ?? throw new AssertFailedException("No first query was loaded."))
        {
            firstQuery.Parameters["patterns"] = CreatePatterns(("todo", "TODO"));
            using var firstTable = firstQuery.Run();
            Assert.AreEqual("todo", firstTable.Rows.Single()[0]);
        }

        var secondLoaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(secondLoaded.Succeeded, FormatDiagnostics(secondLoaded.Diagnostics));
        using (var secondQuery = secondLoaded.CompiledQuery ?? throw new AssertFailedException("No second query was loaded."))
        {
            secondQuery.Parameters["patterns"] = CreatePatterns(("fixme", "FIXME"));
            using var secondTable = secondQuery.Run();
            Assert.AreEqual("fixme", secondTable.Rows.Single()[0]);
        }

        Assert.AreEqual(fingerprint, artifact.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);

        var sameScript = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "StructuredFingerprintStable",
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(sameScript.Succeeded, FormatDiagnostics(sameScript.Diagnostics));
        Assert.AreEqual(
            fingerprint,
            sameScript.Artifact!.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);

        var literalChanged = InstanceCreator.CompileArtifactWithDiagnostics(
            query.Replace("'TODO FIXME'", "'FIXME TODO'", StringComparison.Ordinal),
            "StructuredFingerprintLiteralChanged",
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(literalChanged.Succeeded, FormatDiagnostics(literalChanged.Diagnostics));
        Assert.AreNotEqual(
            fingerprint,
            literalChanged.Artifact!.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);

        var shapeChanged = InstanceCreator.CompileArtifactWithDiagnostics(
            query.Replace("Mode: string = 'literal'", "Mode: string = 'regex'", StringComparison.Ordinal),
            "StructuredFingerprintDefaultChanged",
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(shapeChanged.Succeeded, FormatDiagnostics(shapeChanged.Diagnostics));
        Assert.AreNotEqual(
            fingerprint,
            shapeChanged.Artifact!.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);
    }

    [TestMethod]
    public void StructuredArtifact_PerRunCteResultsDoNotChangeTheArtifactFingerprint()
    {
        const string query =
            "param(suffix: string) with patterns as (" +
            "select p.Id, p.Pattern from values { (Id: 'todo', Pattern: $suffix) } p) " +
            "select m.PatternId from #inputs.match('TODO', patterns: patterns) m";
        var provider = new StructuredInputsSchemaProvider();
        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "StructuredFingerprintCte",
            provider,
            _loggerResolver);

        Assert.IsTrue(artifactResult.Succeeded, FormatDiagnostics(artifactResult.Diagnostics));
        var artifact = artifactResult.Artifact ?? throw new AssertFailedException("No CTE fingerprint artifact was produced.");
        var fingerprint = artifact.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256];

        var firstLoaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(firstLoaded.Succeeded, FormatDiagnostics(firstLoaded.Diagnostics));
        using (var firstQuery = firstLoaded.CompiledQuery ?? throw new AssertFailedException("No first CTE query was loaded."))
        {
            firstQuery.Parameters["suffix"] = "TODO";
            using var firstTable = firstQuery.Run();
            Assert.AreEqual(1, firstTable.Rows.Count);
        }

        var secondLoaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new StructuredInputsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(secondLoaded.Succeeded, FormatDiagnostics(secondLoaded.Diagnostics));
        using (var secondQuery = secondLoaded.CompiledQuery ?? throw new AssertFailedException("No second CTE query was loaded."))
        {
            secondQuery.Parameters["suffix"] = "FIXME";
            using var secondTable = secondQuery.Run();
            Assert.AreEqual(0, secondTable.Rows.Count);
        }

        Assert.AreEqual(fingerprint, artifact.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);
    }

    [TestMethod]
    public void StructuredArtifact_TargetPackageCarriesCurrentAbiAndIrVersions()
    {
        const string query = "select m.PatternId from #inputs.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO') }) m";
        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            "StructuredTargetPackage",
            new StructuredInputsSchemaProvider(),
            _loggerResolver,
            ExecutionTargetIds.CSharpClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        var package = result.Package ?? throw new AssertFailedException("No structured target package was produced.");
        Assert.AreEqual(TargetContractVersions.ExecutionIr, package.ExecutionIrVersion);
        Assert.AreEqual(TargetContractVersions.HostAbi, package.HostAbiVersion);
        var manifest = TargetArtifactPackageManifestSerializer.Serialize(package);
        StringAssert.Contains(manifest, $"execution-ir={TargetContractVersions.ExecutionIr}");
        StringAssert.Contains(manifest, $"host-abi={TargetContractVersions.HostAbi}");
    }

    [TestMethod]
    public void StructuredArtifact_DefaultLoaderUsesCollectibleContextAfterDispose()
    {
        var weakReference = CreateStructuredLoadedContextWeakReference();
        for (var index = 0; index < 10 && weakReference.IsAlive; index++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.IsFalse(weakReference.IsAlive);
    }

    private WeakReference CreateStructuredLoadedContextWeakReference()
    {
        const string query =
            "select m.PatternId from #inputs.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO') }) m";
        var provider = new StructuredInputsSchemaProvider();
        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(query, "StructuredUnload", provider, _loggerResolver);
        Assert.IsTrue(artifactResult.Succeeded, FormatDiagnostics(artifactResult.Diagnostics));
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(query, artifactResult.Artifact!, provider, _loggerResolver);
        Assert.IsTrue(loaded.Succeeded, FormatDiagnostics(loaded.Diagnostics));
        var compiled = loaded.CompiledQuery ?? throw new AssertFailedException("No structured query was loaded.");
        var runnableField = typeof(CompiledQuery).GetField("_runnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(runnableField);
        var runnable = (ITableRunnable)runnableField!.GetValue(compiled)!;
        var context = AssemblyLoadContext.GetLoadContext(runnable.GetType().Assembly);
        Assert.IsNotNull(context);
        Assert.IsTrue(context!.IsCollectible);
        var weakReference = new WeakReference(context);
        compiled.Dispose();
        return weakReference;
    }

    private BuildResult Compile(string query, string name)
    {
        var result = InstanceCreator.CompileWithDiagnostics(query, name, new StructuredInputsSchemaProvider(), _loggerResolver);
        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        return result;
    }

    private static List<Dictionary<string, object?>> CreatePatterns(params (string Id, string Pattern)[] values)
    {
        var result = new List<Dictionary<string, object?>>(values.Length);
        foreach (var (id, pattern) in values)
        {
            result.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = id,
                ["Pattern"] = pattern
            });
        }

        return result;
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics) =>
        string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
}
