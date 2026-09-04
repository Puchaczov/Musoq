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
    public void TargetArtifactPackageManifest_ShouldFingerprintCompleteAbiDefinitions()
    {
        var first = CreatePackageWithAbiAttribute("first");
        var second = CreatePackageWithAbiAttribute("second");

        var firstManifest = TargetArtifactPackageManifestSerializer.Serialize(first);
        var secondManifest = TargetArtifactPackageManifestSerializer.Serialize(second);

        Assert.AreNotEqual(firstManifest, secondManifest);
        StringAssert.Contains(firstManifest, "abi-definition:");
        StringAssert.Contains(secondManifest, "abi-definition:");
    }

    [TestMethod]
    public void TargetHostAbiInventory_ShouldCollapseEquivalentImportsAndRejectConflicts()
    {
        var import = TargetHostAbiImport.CreateCustom(
            TargetHostAbiImportKind.Diagnostics,
            "diagnostics",
            "diagnostics-v1");

        var inventory = new TargetHostAbiInventory([import, import]);
        Assert.HasCount(1, inventory.Imports);

        var conflicting = TargetHostAbiImport.CreateCustom(
            TargetHostAbiImportKind.Diagnostics,
            "diagnostics",
            "diagnostics-v1",
            attributes: new Dictionary<string, string> { ["mode"] = "conflicting" });
        var exception = Assert.Throws<ArgumentException>(() =>
            new TargetHostAbiInventory([import, conflicting]));
        StringAssert.Contains(exception.Message, "conflicting ABI import definitions");
        StringAssert.Contains(exception.Message, "canonical definition");
        Assert.Throws<ArgumentOutOfRangeException>(() => new TargetHostAbiInventory([], contractVersion: 0));
    }

    [TestMethod]
    public void TargetHostAbiInventory_ShouldCollapseEquivalentCustomImportsForEveryImportKind()
    {
        foreach (var kind in Enum.GetValues<TargetHostAbiImportKind>())
        {
            var import = TargetHostAbiImport.CreateCustom(
                kind,
                $"{kind}:same",
                $"{kind}-v1",
                attributes: new Dictionary<string, string>
                {
                    ["z"] = "last",
                    ["a"] = "first"
                });
            var equivalent = TargetHostAbiImport.CreateCustom(
                kind,
                $"{kind}:same",
                $"{kind}-v1",
                attributes: new Dictionary<string, string>
                {
                    ["a"] = "first",
                    ["z"] = "last"
                });

            var inventory = new TargetHostAbiInventory([import, equivalent]);

            Assert.HasCount(1, inventory.Imports, $"Duplicate {kind} import was not collapsed.");
        }
    }

    [TestMethod]
    public void TargetHostAbiInventory_ShouldDetectConflictsHiddenBehindSummaryAttributes()
    {
        var stringType = new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.Primitive,
            "string",
            "string");
        var intType = new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.Primitive,
            "primitive:int32",
            "int");
        var firstDetails = new TargetSourceAccessAbiDetails(
            "schema-source",
            "source:1",
            "schema",
            "rows",
            "rows:type",
            ExecutionPortableSymbolPortability.Portable,
            "source:type",
            ExecutionPortableSymbolPortability.Portable,
            [],
            [new TargetSourceFieldAbiContract(0, "Value", stringType, stringType, "Unknown", null)],
            [],
            []);
        var secondDetails = new TargetSourceAccessAbiDetails(
            "schema-source",
            "source:1",
            "schema",
            "rows",
            "rows:type",
            ExecutionPortableSymbolPortability.Portable,
            "source:type",
            ExecutionPortableSymbolPortability.Portable,
            [],
            [new TargetSourceFieldAbiContract(0, "Value", intType, intType, "Unknown", null)],
            [],
            []);

        var first = new TargetHostAbiImport(
            TargetHostAbiImportKind.SourceAccess,
            "source-access",
            "source-access-v1",
            1,
            firstDetails);
        var second = new TargetHostAbiImport(
            TargetHostAbiImportKind.SourceAccess,
            "source-access",
            "source-access-v1",
            1,
            secondDetails);

        var exception = Assert.Throws<ArgumentException>(() => new TargetHostAbiInventory([first, second]));

        StringAssert.Contains(exception.Message, "conflicting ABI import definitions");
        StringAssert.Contains(exception.Message, "canonical definition");
    }

    [TestMethod]
    public void TargetSourceAccessAbiDetails_ShouldRejectDuplicateFieldIndices()
    {
        var type = new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.Primitive,
            "string",
            "string");

        Assert.Throws<ArgumentException>(() =>
            new TargetSourceAccessAbiDetails(
                "schema-source",
                "source:1",
                "schema",
                "rows",
                "rows:type",
                ExecutionPortableSymbolPortability.Portable,
                "source:type",
                ExecutionPortableSymbolPortability.Portable,
                [],
                [
                    new TargetSourceFieldAbiContract(0, "Who", type, type, "Unknown", null),
                    new TargetSourceFieldAbiContract(0, "Age", type, type, "Unknown", null)
                ],
                [],
                []));
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

    private static TargetArtifactPackage CreatePackageWithAbiAttribute(string value)
    {
        var inventory = new TargetHostAbiInventory(
        [
            TargetHostAbiImport.CreateCustom(
                TargetHostAbiImportKind.Diagnostics,
                "diagnostics",
                "diagnostics-v1",
                attributes: new Dictionary<string, string> { ["mode"] = value })
        ]);
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("query.js", "javascript", "export {}")],
            entrypoints: [new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")],
            runtimeServices: TargetRuntimeServiceRequirements.Create(TargetRuntimeServiceRequirementKind.Diagnostics),
            hostAbiInventory: inventory);

        return TargetArtifactPackage.CreatePortableExportPackage(
            TestExecutionTargetIds.TestOnlyNonClr,
            "PortableManifest",
            export,
            ExecutionSemanticsContract.Version1);
    }
}
