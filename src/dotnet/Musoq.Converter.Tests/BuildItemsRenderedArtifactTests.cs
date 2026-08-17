using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class BuildItemsRenderedArtifactTests
{
    [TestMethod]
    public void RenderingArtifact_WhenSetToCSharpArtifact_ShouldPopulateLegacyFields()
    {
        var compilation = CSharpCompilation.Create("ArtifactSync");
        var artifact = new CSharpRenderedQueryArtifact(compilation, "ArtifactSync.CompiledQuery");
        var items = new BuildItems
        {
            RenderingArtifact = artifact
        };

        Assert.AreSame(artifact, items.RenderingArtifact);
        Assert.AreSame(compilation, items.Compilation);
        Assert.AreEqual("ArtifactSync.CompiledQuery", items.AccessToClassPath);
    }

    [TestMethod]
    public void LegacyRenderingFields_WhenBothSet_ShouldRefreshCSharpArtifact()
    {
        var compilation = CSharpCompilation.Create("LegacySync");
        var items = new BuildItems
        {
            Compilation = compilation,
            AccessToClassPath = "LegacySync.CompiledQuery"
        };

        var artifact = (CSharpRenderedQueryArtifact)items.RenderingArtifact;

        Assert.AreSame(compilation, artifact.Compilation);
        Assert.AreEqual("LegacySync.CompiledQuery", artifact.AccessToClassPath);
    }

    [TestMethod]
    public void RenderingArtifacts_WhenSet_ShouldPreserveArtifactAndMetadata()
    {
        var compilation = CSharpCompilation.Create("StageSync");
        var artifact = new CSharpRenderedQueryArtifact(compilation, "StageSync.CompiledQuery");
        var metadata = new QueryMethodRenderMetadata(
            FinalResultSinkKind.TableRowsMaterialized,
            QueryResultRowPathKind.MaterializedTableRows,
            RequiresComputeTableMethod: true);
        var items = new BuildItems
        {
            RenderingArtifacts = new RenderingBuildArtifacts(artifact)
            {
                QueryMethodRenderMetadata = metadata
            }
        };

        Assert.AreSame(artifact, items.RenderingArtifact);
        Assert.AreEqual(metadata, items.QueryMethodRenderMetadata);
        Assert.AreSame(compilation, items.RenderingArtifacts.Compilation);
        Assert.AreEqual("StageSync.CompiledQuery", items.RenderingArtifacts.AccessToClassPath);
    }

    [TestMethod]
    public void RenderingArtifact_WhenSetToNonCSharpArtifact_ShouldNotPopulateLegacyCSharpFields()
    {
        var artifact = new TestOnlyRenderedQueryArtifact("test-only-source");
        var items = new BuildItems
        {
            RenderingArtifact = artifact
        };

        Assert.AreSame(artifact, items.RenderingArtifact);
        Assert.Throws<KeyNotFoundException>(() => _ = items.Compilation);
        Assert.Throws<KeyNotFoundException>(() => _ = items.AccessToClassPath);
        Assert.Throws<InvalidOperationException>(() => _ = items.RenderingArtifacts.Compilation);
        Assert.Throws<InvalidOperationException>(() => _ = items.RenderingArtifacts.AccessToClassPath);
    }

    [TestMethod]
    public void ExecutableArtifact_WhenSetToNonClrArtifact_ShouldNotPopulateLegacyDllOrPdbFields()
    {
        var artifact = new TestOnlyExecutableQueryArtifact("test-only-executable");
        var items = new BuildItems
        {
            CompilationArtifacts = CompilationBuildArtifacts.From(new TestOnlyFinalizationResult(artifact))
        };

        Assert.AreSame(artifact, items.ExecutableArtifact);
        Assert.IsNull(items.DllFile);
        Assert.IsNull(items.PdbFile);
        Assert.IsNull(items.CompilationArtifacts.DllFile);
        Assert.IsNull(items.CompilationArtifacts.PdbFile);
    }

    [TestMethod]
    public void ExecutableArtifact_WhenSetToPortableExportArtifact_ShouldPreservePortableMetadataWithoutClrBytes()
    {
        var artifact = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles:
            [
                new TargetExportSourceFile("query.js", "javascript", "export function run() {}")
            ],
            binaryBlobs:
            [
                new TargetExportBinaryBlob("query.wasm", [0, 97, 115, 109], "application/wasm")
            ],
            entrypoints:
            [
                new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")
            ],
            runtimeServices: TargetRuntimeServiceRequirements.Create(
                TargetRuntimeServiceRequirementKind.SourceAccess,
                TargetRuntimeServiceRequirementKind.Diagnostics),
            hostAbiInventory: new TargetHostAbiInventory(
            [
                CreateSourceAbiImport(),
                new TargetHostAbiImport(
                    TargetHostAbiImportKind.Diagnostics,
                    "build+runtime",
                    "diagnostics-v1",
                    1,
                    new TargetDiagnosticsAbiDetails(true, false, true))
            ]),
            diagnosticsMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["targetFamily"] = "test-only-portable"
            });
        var items = new BuildItems
        {
            CompilationArtifacts = CompilationBuildArtifacts.From(new TestOnlyFinalizationResult(artifact))
        };

        Assert.AreSame(artifact, items.ExecutableArtifact);
        Assert.IsNull(items.DllFile);
        Assert.IsNull(items.PdbFile);
        Assert.HasCount(1, artifact.SourceFiles);
        Assert.HasCount(1, artifact.BinaryBlobs);
        Assert.IsTrue(artifact.RuntimeServices.Requires(TargetRuntimeServiceRequirementKind.SourceAccess));
        Assert.AreEqual("test-only-portable", artifact.DiagnosticsMetadata["targetFamily"]);
    }

    [TestMethod]
    public void TargetExportArtifact_WhenInputsAreMutated_ShouldRemainStable()
    {
        var sourceFiles = new List<TargetExportSourceFile>
        {
            new("query.js", "javascript", "export function run() {}")
        };
        var blobBytes = new byte[] { 0, 97, 115, 109 };
        var binaryBlobs = new List<TargetExportBinaryBlob>
        {
            new("query.wasm", blobBytes, "application/wasm")
        };
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["targetFamily"] = "test-only-portable"
        };
        var services = new HashSet<TargetRuntimeServiceRequirementKind>
        {
            TargetRuntimeServiceRequirementKind.SourceAccess
        };

        var artifact = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: sourceFiles,
            binaryBlobs: binaryBlobs,
            runtimeServices: new TargetRuntimeServiceRequirements(services),
            hostAbiInventory: new TargetHostAbiInventory([CreateSourceAbiImport()]),
            diagnosticsMetadata: metadata);

        sourceFiles.Clear();
        binaryBlobs.Clear();
        blobBytes[0] = 255;
        metadata["targetFamily"] = "mutated";
        services.Clear();
        var exposedBlob = artifact.BinaryBlobs[0].Content;
        exposedBlob[1] = 255;

        Assert.HasCount(1, artifact.SourceFiles);
        Assert.HasCount(1, artifact.BinaryBlobs);
        Assert.AreEqual(0, artifact.BinaryBlobs[0].Content[0]);
        Assert.AreEqual(97, artifact.BinaryBlobs[0].Content[1]);
        Assert.AreEqual("test-only-portable", artifact.DiagnosticsMetadata["targetFamily"]);
        Assert.IsTrue(artifact.RuntimeServices.Requires(TargetRuntimeServiceRequirementKind.SourceAccess));
    }

    private static TargetHostAbiImport CreateSourceAbiImport()
    {
        return new TargetHostAbiImport(
            TargetHostAbiImportKind.SourceAccess,
            "schema-source:source:test.rows",
            "source-access-v1",
            1,
            new TargetSourceAccessAbiDetails(
                "schema-source",
                "source",
                "test",
                "rows",
                "object",
                ExecutionPortableSymbolPortability.Portable,
                string.Empty,
                null,
                [],
                [],
                [],
                []));
    }

    private static CSharpCompilation CreateValidCompilation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            "namespace NonClrArtifactSmoke { public sealed class Placeholder { } }");
        var references = new[]
        {
            Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "NonClrArtifactSmoke",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed record TestOnlyRenderedQueryArtifact(string Source)
        : RenderedQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

    private sealed record TestOnlyExecutableQueryArtifact(string Payload)
        : ExecutableQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

    private sealed record TestOnlyFinalizationResult(ExecutableQueryArtifact ExecutableArtifact)
        : TargetFinalizationResult(
            TestExecutionTargetIds.TestOnlyNonClr,
            true,
            [],
            ExecutableArtifact);
}
