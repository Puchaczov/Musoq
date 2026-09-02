using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CompiledQueryArtifactSupportTargetTests
{
    [TestMethod]
    public void TargetExportArtifact_WhenHostImportedServiceHasNoAbi_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TargetExportArtifact.Create(
                TestExecutionTargetIds.TestOnlyNonClr,
                runtimeServices: TargetRuntimeServiceRequirements.Create(
                    TargetRuntimeServiceRequirementKind.SourceAccess)));

        Assert.Contains("host-imported", exception.Message);
        Assert.Contains(nameof(TargetHostAbiImportKind.SourceAccess), exception.Message);
    }

    [TestMethod]
    public void TargetExportArtifact_WhenServiceIsTargetProvided_ShouldNotRequireHostAbiImport()
    {
        var artifact = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            runtimeServices: TargetRuntimeServiceRequirements.CreateTargetProvided(
                TargetRuntimeServiceRequirementKind.SourceAccess));

        Assert.AreEqual(
            TargetRuntimeServiceFulfillmentKind.TargetProvided,
            artifact.RuntimeServices.GetFulfillment(TargetRuntimeServiceRequirementKind.SourceAccess));
        Assert.IsEmpty(artifact.HostAbiInventory.Imports);
    }

    [TestMethod]
    public void RenderBuildContribution_WhenNonCSharpTargetDoesNotProvideHash_ShouldLeaveGeneratedCodeHashEmpty()
    {
        var artifact = new NonCSharpRenderedArtifact();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor();
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var contribution = ExecutionTargetCatalog.CreateRenderBuildContribution(artifact);

        Assert.IsNull(contribution.GeneratedCodeSha256);
    }

    [TestMethod]
    public void CreateClrAssemblyPackage_WhenDllBlobIsMissing_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CSharpClrTargetPackageFactory.CreateClrAssemblyPackage(
                CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
                CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
                ExecutionSemanticsContract.Version1,
                CreateCSharpMetadata(),
                binaryBlobs: [],
                entrypoints: [CreateTableEntrypoint()],
                hostAbiInventory: TargetHostAbiInventory.Empty,
                assemblyBlobName: CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
                generatedCodeSha256MetadataKey: CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256));

        Assert.Contains("required CLR assembly blob", exception.Message);
    }

    [TestMethod]
    public void CreateClrAssemblyPackage_WhenGeneratedCodeHashIsMissing_ShouldReject()
    {
        var metadata = CreateCSharpMetadata();
        metadata.Remove(CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CSharpClrTargetPackageFactory.CreateClrAssemblyPackage(
                CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
                CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
                ExecutionSemanticsContract.Version1,
                metadata,
                binaryBlobs: [CreateAssemblyBlob()],
                entrypoints: [CreateTableEntrypoint()],
                hostAbiInventory: TargetHostAbiInventory.Empty,
                assemblyBlobName: CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
                generatedCodeSha256MetadataKey: CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256));

        Assert.Contains(CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256, exception.Message);
    }

    [TestMethod]
    public void CreateClrAssemblyPackage_WhenTableEntrypointIsMissing_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CSharpClrTargetPackageFactory.CreateClrAssemblyPackage(
                CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
                CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
                ExecutionSemanticsContract.Version1,
                CreateCSharpMetadata(),
                binaryBlobs: [CreateAssemblyBlob()],
                entrypoints: [],
                hostAbiInventory: TargetHostAbiInventory.Empty,
                assemblyBlobName: CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
                generatedCodeSha256MetadataKey: CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256));

        Assert.Contains("table-query entrypoint", exception.Message);
    }

    [TestMethod]
    public void CreatePortableExportPackage_WhenExportHasNoSourceOrBinary_ShouldReject()
    {
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            entrypoints: [CreateTableEntrypoint()],
            runtimeServices: TargetRuntimeServiceRequirements.CreateTargetProvided(
                TargetRuntimeServiceRequirementKind.SourceAccess));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TargetArtifactPackage.CreatePortableExportPackage(
                TestExecutionTargetIds.TestOnlyNonClr,
                "FakeExport",
                export,
                ExecutionSemanticsContract.Version1));

        Assert.Contains("source file or binary blob", exception.Message);
    }

    [TestMethod]
    public void CreatePortableExportPackage_WhenEntrypointIsMissing_ShouldReject()
    {
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("query.js", "javascript", "export function run() {}")],
            runtimeServices: TargetRuntimeServiceRequirements.CreateTargetProvided(
                TargetRuntimeServiceRequirementKind.SourceAccess));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TargetArtifactPackage.CreatePortableExportPackage(
                TestExecutionTargetIds.TestOnlyNonClr,
                "FakeExport",
                export,
                ExecutionSemanticsContract.Version1));

        Assert.Contains("table-query or typed-query entrypoint", exception.Message);
    }

    [TestMethod]
    public void CreatePortableExportPackage_WhenRuntimeServicesAreMissing_ShouldAllowSelfContainedExport()
    {
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("query.js", "javascript", "export function run() {}")],
            entrypoints: [CreateTableEntrypoint()]);

        var package = TargetArtifactPackage.CreatePortableExportPackage(
            TestExecutionTargetIds.TestOnlyNonClr,
            "FakeExport",
            export,
            ExecutionSemanticsContract.Version1);

        Assert.IsEmpty(package.RuntimeServices.RequiredServices);
    }

    [TestMethod]
    public void CreatePortableExportPackage_WhenOnlyTypedEntrypointExists_ShouldAccept()
    {
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("query.js", "javascript", "export function runTyped() {}")],
            entrypoints:
            [
                new TargetRuntimeEntrypoint("runTyped", TargetRuntimeEntrypointKind.TypedQuery, "runTyped")
            ]);

        var package = TargetArtifactPackage.CreatePortableExportPackage(
            TestExecutionTargetIds.TestOnlyNonClr,
            "FakeTypedExport",
            export,
            ExecutionSemanticsContract.Version1);

        Assert.HasCount(1, package.Entrypoints);
        Assert.AreEqual(TargetRuntimeEntrypointKind.TypedQuery, package.Entrypoints[0].Kind);
        Assert.IsEmpty(package.RuntimeServices.RequiredServices);
    }

    [TestMethod]
    public void CreatePortableExportPackage_WhenTargetDoesNotMatch_ShouldReject()
    {
        var export = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles: [new TargetExportSourceFile("query.js", "javascript", "export function run() {}")],
            entrypoints: [CreateTableEntrypoint()],
            runtimeServices: TargetRuntimeServiceRequirements.CreateTargetProvided(
                TargetRuntimeServiceRequirementKind.SourceAccess));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TargetArtifactPackage.CreatePortableExportPackage(
                ExecutionTargetIds.CSharpClr,
                "FakeExport",
                export,
                ExecutionSemanticsContract.Version1));

        Assert.Contains("does not match package target", exception.Message);
    }

    [TestMethod]
    public void TargetArtifactSemanticFacts_WhenConstructed_ShouldExposePortableSemanticViews()
    {
        var source = new SchemaFromNode(
            "test",
            "rows",
            new ArgsListNode([]),
            "r",
            typeof(object),
            7);
        var column = new SchemaColumn(
            "Name",
            1,
            typeof(string),
            "text",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["encoding"] = "utf8"
            });
        var sourcePlan = new SourcePlanRequest
        {
            Identity = new SourceIdentity("test", "rows", "source:1", "r"),
            RequiredColumns =
            [
                new SourceColumnRef(
                    "Name",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["source.index"] = "1"
                    })
            ],
            Skip = 1,
            Take = 2
        };

        var facts = new TargetArtifactSemanticFacts(
            QueryResultMode.TypedEnumerable,
            typeof(string),
            [new ScriptParameterDefinition("name", typeof(string), false, null)],
            [new ScriptVariableDefinition("limit", typeof(int), 5, true)],
            new Dictionary<SchemaFromNode, ISchemaColumn[]> { [source] = [column] },
            new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal) { ["r"] = [column] },
            new Dictionary<SchemaFromNode, SourcePlanRequest> { [source] = sourcePlan });

        StringAssert.Contains(facts.PortableOutputTypeName, "System.String");
        Assert.HasCount(1, facts.PortableScriptParameters);
        Assert.AreEqual("name", facts.PortableScriptParameters[0].Name);
        StringAssert.Contains(facts.PortableScriptParameters[0].TypeName, "System.String");
        Assert.HasCount(1, facts.PortableScriptVariables);
        Assert.AreEqual("limit", facts.PortableScriptVariables[0].Name);
        StringAssert.Contains(facts.PortableScriptVariables[0].TypeName, "System.Int32");
        Assert.HasCount(1, facts.PortableUsedColumns);
        Assert.AreEqual("test", facts.PortableUsedColumns[0].Source.Schema);
        Assert.AreEqual("rows", facts.PortableUsedColumns[0].Source.Method);
        Assert.AreEqual("r", facts.PortableUsedColumns[0].Source.Alias);
        Assert.AreEqual("7", facts.PortableUsedColumns[0].Source.QueryId);
        Assert.AreEqual("Name", facts.PortableUsedColumns[0].Columns[0].ColumnName);
        Assert.AreEqual(facts.PortableUsedColumns[0].Columns[0].ColumnTypeName,
            facts.PortableUsedColumns[0].Columns[0].SourceReadTypeName);
        Assert.AreEqual("<null>", facts.PortableUsedColumns[0].Columns[0].EnumTypeFingerprint);
        Assert.AreEqual("utf8", facts.PortableUsedColumns[0].Columns[0].ReadModifiers["encoding"]);
        Assert.IsNotNull(facts.PortablePipelineInferredColumns);
        Assert.AreEqual("r", facts.PortablePipelineInferredColumns[0].Alias);
        Assert.HasCount(1, facts.PortableSourcePlanSignatures);
        Assert.AreEqual("source:1", facts.PortableSourcePlanSignatures[0].IdentitySourceContextId);
        Assert.AreEqual("1", facts.PortableSourcePlanSignatures[0].Skip);
        Assert.AreEqual("2", facts.PortableSourcePlanSignatures[0].Take);
        Assert.AreEqual("Name", facts.PortableSourcePlanSignatures[0].RequiredColumns[0].Name);
        Assert.AreEqual("1", facts.PortableSourcePlanSignatures[0].RequiredColumns[0].ReadModifiers["source.index"]);
    }

    [TestMethod]
    public void SemanticShapeHash_ShouldIncludeEnumIdentityAndExactSourceReadType()
    {
        var source = new SchemaFromNode(
            "test",
            "rows",
            new ArgsListNode([]),
            "r",
            typeof(object),
            7);
        var firstDescriptor = new EnumTypeDescriptor(
            "FirstStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            [new EnumMemberDescriptor("Ready", EnumScalarValue.FromInt32(1))]);
        var secondDescriptor = new EnumTypeDescriptor(
            "SecondStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            [new EnumMemberDescriptor("Ready", EnumScalarValue.FromInt32(1))]);
        var nativeDescriptor = EnumTypeDescriptor.FromClrEnum(typeof(NativeStatus));

        var firstHash = CreateColumnSemanticHash(source,
            new SchemaColumn("Status", 0, typeof(int), typeof(int), firstDescriptor));
        var secondHash = CreateColumnSemanticHash(source,
            new SchemaColumn("Status", 0, typeof(int), typeof(int), secondDescriptor));
        var nativeReadHash = CreateColumnSemanticHash(source,
            new SchemaColumn("Status", 0, typeof(int), typeof(NativeStatus), nativeDescriptor));
        var carrierReadHash = CreateColumnSemanticHash(source,
            new SchemaColumn("Status", 0, typeof(int), typeof(int), nativeDescriptor));

        Assert.AreNotEqual(firstHash, secondHash);
        Assert.AreNotEqual(nativeReadHash, carrierReadHash);
    }

    private static string CreateColumnSemanticHash(SchemaFromNode source, ISchemaColumn column)
    {
        var facts = new TargetArtifactSemanticFacts(
            QueryResultMode.Table,
            null,
            [],
            [],
            new Dictionary<SchemaFromNode, ISchemaColumn[]> { [source] = [column] },
            null,
            new Dictionary<SchemaFromNode, SourcePlanRequest>());

        return CompiledQueryArtifactSupport.ComputeSemanticShapeHash(facts, "Runnable");
    }

    public enum NativeStatus
    {
        Ready = 1
    }

    private sealed record NonCSharpRenderedArtifact()
        : RenderedQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

    private static Dictionary<string, string> CreateCSharpMetadata()
    {
        return new Dictionary<string, string>
        {
            [CompiledQueryArtifactSupport.MetadataArtifactKind] = CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
            [CompiledQueryArtifactSupport.MetadataAssemblyName] = "TestAssembly",
            [CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature] = RuntimeV2Contract.ContractSignature,
            [CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion] = "1",
            [CompiledQueryArtifactSupport.MetadataExecutionTarget] = ExecutionTargetIds.CSharpClr.ToString(),
            [CompiledQueryArtifactSupport.MetadataExecutableArtifactKind] = CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
            [CompiledQueryArtifactSupport.MetadataScriptSha256] = "script",
            [CompiledQueryArtifactSupport.MetadataSemanticShapeSha256] = "semantic",
            [CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256] = "generated"
        };
    }

    private static TargetExportBinaryBlob CreateAssemblyBlob()
    {
        return new TargetExportBinaryBlob(
            CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
            [1, 2, 3],
            CompiledQueryArtifactSupport.CSharpClrAssemblyContentType);
    }

    private static TargetRuntimeEntrypoint CreateTableEntrypoint()
    {
        return new TargetRuntimeEntrypoint(
            "CompiledQuery",
            TargetRuntimeEntrypointKind.TableQuery,
            "TestAssembly.CompiledQuery");
    }
}
