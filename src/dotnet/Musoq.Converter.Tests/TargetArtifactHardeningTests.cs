using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetArtifactHardeningTests
{
    [TestMethod]
    public void TargetExportArtifact_ShouldNormalizeAndRequireUniqueArtifactPaths()
    {
        var artifact = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("generated\\query.js", "javascript", "export {}")],
            entrypoints: [new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")]);

        Assert.AreEqual("generated/query.js", artifact.SourceFiles[0].Path);
        Assert.Throws<ArgumentException>(() => TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles:
            [
                new TargetExportSourceFile("generated\\query.js", "javascript", "a"),
                new TargetExportSourceFile("generated/query.js", "javascript", "b")
            ]));
        Assert.Throws<ArgumentException>(() =>
            new TargetExportSourceFile("../query.js", "javascript", "bad"));
        Assert.Throws<ArgumentException>(() => TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("query.bin", "binary-source", "text")],
            binaryBlobs: [new TargetExportBinaryBlob("query.bin", [1], "application/octet-stream")]));
    }

    [TestMethod]
    public void TargetExportArtifact_ShouldValidateContentTypesAndUniqueEntrypoints()
    {
        Assert.Throws<ArgumentException>(() =>
            new TargetExportBinaryBlob("query.bin", [1], "binary"));
        Assert.Throws<ArgumentException>(() => TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            entrypoints:
            [
                new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run1"),
                new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TypedQuery, "run2")
            ]));
        Assert.Throws<ArgumentException>(() => TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            entrypoints:
            [
                new TargetRuntimeEntrypoint("run1", TargetRuntimeEntrypointKind.TableQuery, "run"),
                new TargetRuntimeEntrypoint("run2", TargetRuntimeEntrypointKind.TypedQuery, "run")
            ]));
    }

    [TestMethod]
    public void TargetArtifactPackageManifest_WhenInputsHaveDifferentOrder_ShouldBeDeterministicAndVersioned()
    {
        var first = CreatePackage(reverse: false);
        var second = CreatePackage(reverse: true);

        var firstManifest = TargetArtifactPackageManifestSerializer.Serialize(first);
        var secondManifest = TargetArtifactPackageManifestSerializer.Serialize(second);

        Assert.AreEqual(firstManifest, secondManifest);
        StringAssert.Contains(firstManifest, $"package-format={TargetContractVersions.PackageFormat}");
        StringAssert.Contains(firstManifest, $"execution-ir={TargetContractVersions.ExecutionIr}");
        StringAssert.Contains(firstManifest, $"execution-semantics={ExecutionSemanticsContract.Version1.Version}");
        StringAssert.Contains(firstManifest, $"execution-semantics-fingerprint={ExecutionSemanticsContract.Version1.Fingerprint}");
        StringAssert.Contains(firstManifest, $"host-abi={TargetContractVersions.HostAbi}");
    }

    [TestMethod]
    public void TargetArtifactPackage_ShouldExposeExplicitContractVersions()
    {
        var package = CreatePackage(reverse: false);

        Assert.AreEqual(TargetContractVersions.ExecutionIr, package.ExecutionIrVersion);
        Assert.AreEqual(ExecutionSemanticsContract.Version1.Version, package.SemanticsContract.Version);
        Assert.AreEqual(TargetContractVersions.HostAbi, package.HostAbiVersion);
        Assert.AreEqual(TargetContractVersions.PackageFormat, package.PackageFormatVersion);
    }

    [TestMethod]
    public void TargetHostAbiInventory_ShouldRejectDuplicateImportsAndInvalidVersion()
    {
        var import = TargetHostAbiImport.CreateCustom(
            TargetHostAbiImportKind.Diagnostics,
            "diagnostics",
            "diagnostics-v1");

        Assert.Throws<ArgumentException>(() => new TargetHostAbiInventory([import, import]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TargetHostAbiInventory([], contractVersion: 0));
    }

    private static TargetArtifactPackage CreatePackage(bool reverse)
    {
        var sourceFiles = reverse
            ? new[]
            {
                new TargetExportSourceFile("b.js", "javascript", "b"),
                new TargetExportSourceFile("a.js", "javascript", "a")
            }
            : new[]
            {
                new TargetExportSourceFile("a.js", "javascript", "a"),
                new TargetExportSourceFile("b.js", "javascript", "b")
            };
        var metadata = reverse
            ? new Dictionary<string, string>(StringComparer.Ordinal) { ["z"] = "last", ["a"] = "first" }
            : new Dictionary<string, string>(StringComparer.Ordinal) { ["a"] = "first", ["z"] = "last" };
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: sourceFiles,
            entrypoints: [new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")],
            diagnosticsMetadata: metadata);
        return TargetArtifactPackage.CreatePortableExportPackage(
            TestExecutionTargetIds.TestOnlyNonClr,
            "PortableManifest",
            export,
            ExecutionSemanticsContract.Version1,
            metadata);
    }
}
