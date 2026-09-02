using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionQueryRowSourceTransferTests
{
    [TestMethod]
    public void SourceBinding_WhenTransferIsPresent_PreservesPortableShapeMetadata()
    {
        var transfer = CreateTransfer();
        var binding = CreateBinding(transfer);

        Assert.AreSame(transfer, binding.QueryRowSourceTransfer);
        Assert.AreEqual(ExecutionQueryRowLifetime.ScanLocal, binding.QueryRowSourceTransfer!.Lifetime);
        Assert.AreEqual(64, binding.QueryRowSourceTransfer.ShapeFingerprint.Length);
        Assert.AreEqual(0, binding.QueryRowSourceTransfer.Fields[0].Slot);
        Assert.AreEqual("Id", binding.QueryRowSourceTransfer.Fields[0].Name);
        Assert.AreEqual("primitive:int32", binding.QueryRowSourceTransfer.Fields[0].FieldType.StableId);
    }

    [TestMethod]
    public void ExecutionBindingInvariant_WhenTransferFieldsAreDense_AcceptsPlan()
    {
        var binding = CreateBinding(CreateTransfer());
        var plan = new ExecutionPlan(
            "query-row",
            [],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("source", typeof(object)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));

        ExecutionBindingInvariantValidator.Validate(plan);
    }

    [TestMethod]
    public void TargetContract_WhenTransferIsPresent_AdvertisesSeparateHostAbiImport()
    {
        var binding = CreateBinding(CreateTransfer());
        var plan = new ExecutionPlan(
            "query-row-abi",
            [],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("source", typeof(object)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));
        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        var contract = TargetRuntimeContractBuilder.Build(plan, report);
        var shapeFingerprint = binding.QueryRowSourceTransfer!.ShapeFingerprint;

        Assert.HasCount(1, contract.QueryRowSourceAccess);
        Assert.AreEqual(shapeFingerprint, contract.QueryRowSourceAccess[0].ShapeFingerprint);
        Assert.IsTrue(report.Requirements.Any(static requirement =>
            requirement.Kind == ExecutionTargetRequirementKind.QueryRowSourceAccess));

        var import = TargetHostAbiInventoryBuilder.Build(contract).Imports.Single(static item =>
            item.Kind == TargetHostAbiImportKind.QueryRowSourceAccess);
        Assert.AreEqual("query-row-source-access-v2", import.Contract);
        Assert.AreEqual(2, import.ContractVersion);
        Assert.AreEqual(shapeFingerprint, import.Attributes["shapeFingerprint"]);
        Assert.AreEqual("ReadonlyStruct", import.Attributes["carrier"]);
        Assert.AreEqual("ScanLocal", import.Attributes["lifetime"]);
    }

    [TestMethod]
    public void TargetContract_WhenTransferContainsEnum_ShouldPreserveSourceTypeAndPortableDescriptor()
    {
        var descriptor = EnumTypeDescriptor.FromClrEnum(typeof(NativeStatus));
        var fieldType = ExecutionClrBindingFactory.FromClr(typeof(int));
        var sourceReadType = ExecutionClrBindingFactory.FromClr(typeof(NativeStatus));
        var transfer = new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            CreateFingerprint(),
            [new ExecutionQueryRowField(0, 0, "Status", fieldType, sourceReadType, descriptor, false)]);
        var field = new FieldBinding(
            "Status",
            "source.Status",
            0,
            fieldType,
            FieldNullability.NotNullable,
            new GeneratedFieldAccess(QueryRowSourceNaming.CreateFieldName(0)),
            publicType: fieldType,
            sourceReadType: sourceReadType,
            enumType: descriptor);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "source:enum",
            0,
            [],
            [field],
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(object)),
            QueryRowSourceTransfer: transfer);
        var plan = CreatePlan(binding);

        var contract = TargetRuntimeContractBuilder.Build(
            plan,
            ExecutionTargetCompatibilityAnalyzer.Analyze(plan));
        var sourceField = contract.SourceAccess.Single().Fields.Single();
        var queryRowField = contract.QueryRowSourceAccess.Single().Fields.Single();

        Assert.AreEqual(sourceReadType.StableId, sourceField.SourceReadType.StableName);
        Assert.AreEqual(sourceReadType.StableId, queryRowField.SourceReadType.StableName);
        Assert.AreEqual(descriptor.Fingerprint, sourceField.EnumType!.Fingerprint);
        Assert.AreEqual(descriptor.Fingerprint, queryRowField.EnumType!.Fingerprint);
        Assert.AreEqual("Queued", sourceField.EnumType.Members[0].CanonicalName);
        Assert.AreEqual("Queued", sourceField.EnumType.Members[1].CanonicalName);

        var imports = TargetHostAbiInventoryBuilder.Build(contract).Imports;
        var sourceDetails = Assert.IsInstanceOfType<TargetSourceAccessAbiDetails>(
            imports.Single(static import => import.Kind == TargetHostAbiImportKind.SourceAccess).Details);
        var queryRowDetails = Assert.IsInstanceOfType<TargetQueryRowSourceAccessAbiDetails>(
            imports.Single(static import => import.Kind == TargetHostAbiImportKind.QueryRowSourceAccess).Details);
        Assert.AreEqual(descriptor.Fingerprint, sourceDetails.Fields[0].EnumType!.Fingerprint);
        Assert.AreEqual(descriptor.Fingerprint, queryRowDetails.Fields[0].EnumType!.Fingerprint);
        StringAssert.Contains(sourceDetails.CanonicalDefinition, descriptor.Fingerprint);
        StringAssert.Contains(queryRowDetails.CanonicalDefinition, descriptor.Fingerprint);
    }

    [TestMethod]
    public void Transfer_WhenReadonlyStructEscapesScan_ShouldRejectLifetime()
    {
        Assert.Throws<ArgumentException>(() => new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            ExecutionQueryRowLifetime.EscapesScan,
            CreateFingerprint(),
            []));
    }

    [TestMethod]
    public void QueryRowNaming_WhenShapeUsesBothCarriers_ShouldProduceDistinctTypeNames()
    {
        var structName = QueryRowSourceNaming.CreateCarrierTypeName(
            CreateFingerprint(),
            SourceQueryRowCarrier.ReadonlyStruct);
        var className = QueryRowSourceNaming.CreateCarrierTypeName(
            CreateFingerprint(),
            SourceQueryRowCarrier.SealedClass);

        Assert.AreNotEqual(structName, className);
    }

    [TestMethod]
    public void QueryRowNaming_WhenShapeIsImmutable_ShouldProduceOneCarrierIndependentMetadataField()
    {
        var fingerprint = CreateFingerprint();

        var fieldName = QueryRowSourceNaming.CreateShapeFieldName(fingerprint);

        Assert.AreEqual("__queryRowShape_AAAAAAAAAAAA", fieldName);
    }

    [TestMethod]
    public void CSharpClrCapabilities_WhenTransferRequirementIsReported_AcceptsIt()
    {
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.QueryRowSourceAccess,
                "source:0:test.rows:shape")
        ]);

        var result = ExecutionTargetCapabilities.CSharpClr.Validate(report);

        Assert.IsTrue(result.IsSupported);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("ABC")]
    [DataRow("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void Transfer_WhenFingerprintIsMalformed_ShouldRejectIt(string fingerprint)
    {
        Assert.Throws<ArgumentException>(() => new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            fingerprint,
            []));
    }

    [TestMethod]
    public void Transfer_WhenSlotsAreNotDense_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            CreateFingerprint(),
            [CreateField(1, 0)]));
    }

    [TestMethod]
    public void Transfer_WhenSlotsAreDuplicated_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            CreateFingerprint(),
            [CreateField(0, 0), CreateField(0, 1)]));
    }

    [TestMethod]
    public void Transfer_WhenSourceOrdinalsAreDuplicated_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            CreateFingerprint(),
            [CreateField(0, 0), CreateField(1, 0)]));
    }

    [TestMethod]
    public void BindingInvariant_WhenTransferUsesUnsupportedClrType_ShouldRejectPlan()
    {
        var type = ExecutionClrBindingFactory.FromClr(typeof(HiddenQueryRowField));
        var transfer = new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            CreateFingerprint(),
            [new ExecutionQueryRowField(0, 0, "Hidden", type, false)]);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "source:0",
            0,
            [],
            [new FieldBinding(
                "Hidden",
                "source.Hidden",
                0,
                typeof(HiddenQueryRowField),
                FieldNullability.NotNullable,
                new GeneratedFieldAccess(QueryRowSourceNaming.CreateFieldName(0)))],
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(object)),
            QueryRowSourceTransfer: transfer);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionBindingInvariantValidator.Validate(CreatePlan(binding)));

        StringAssert.Contains(exception.Message, "unsupported CLR type");
    }

    [TestMethod]
    public void BindingInvariant_WhenGeneratedBindingDoesNotMatchTransfer_ShouldRejectPlan()
    {
        var transfer = CreateTransfer();
        var binding = CreateBinding(transfer) with
        {
            Fields =
            [
                new FieldBinding(
                    "Id",
                    "source.Id",
                    0,
                    typeof(int),
                    FieldNullability.NotNullable,
                    new GeneratedFieldAccess("WrongField"))
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionBindingInvariantValidator.Validate(CreatePlan(binding)));

        StringAssert.Contains(exception.Message, "incompatible with its generated source binding");
    }

    [TestMethod]
    public void BindingInvariant_WhenShapeFingerprintDoesNotMatchFields_ShouldRejectPlan()
    {
        var valid = CreateTransfer();
        var corrupted = new ExecutionQueryRowSourceTransfer(
            valid.Carrier,
            valid.Lifetime,
            CreateFingerprint(),
            valid.Fields);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionBindingInvariantValidator.Validate(CreatePlan(CreateBinding(corrupted))));

        StringAssert.Contains(exception.Message, "shape fingerprint");
    }

    [TestMethod]
    public void TargetContract_WhenFingerprintIsMalformed_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new TargetQueryRowSourceAccessContract(
            "source:0",
            "test",
            "rows",
            ExecutionQueryRowCarrier.ReadonlyStruct,
            ExecutionQueryRowLifetime.ScanLocal,
            "not-a-fingerprint",
            []));
    }

    [TestMethod]
    public void TargetContract_WhenSlotsAreNotDense_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new TargetQueryRowSourceAccessContract(
            "source:0",
            "test",
            "rows",
            ExecutionQueryRowCarrier.ReadonlyStruct,
            ExecutionQueryRowLifetime.ScanLocal,
            CreateFingerprint(),
            [CreateTargetField(1, 0)]));
    }

    [TestMethod]
    public void TargetAbiDetails_WhenSourceOrdinalsAreDuplicated_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new TargetQueryRowSourceAccessAbiDetails(
            "source:0",
            "test",
            "rows",
            "ReadonlyStruct",
            "ScanLocal",
            CreateFingerprint(),
            [CreateAbiField(0, 0), CreateAbiField(1, 0)]));
    }

    [TestMethod]
    public void TargetAbiDetails_WhenFingerprintIsMalformed_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new TargetQueryRowSourceAccessAbiDetails(
            "source:0",
            "test",
            "rows",
            "ReadonlyStruct",
            "ScanLocal",
            new string('G', 64),
            []));
    }

    [TestMethod]
    public void TargetCapabilities_WhenQueryRowFeatureIsUnavailable_ShouldRejectPlan()
    {
        var featureReport = ExecutionTargetFeatureAnalyzer.Analyze(CreatePlan(CreateBinding(CreateTransfer())));

        var result = ExecutionTargetCapabilities.Create().Validate(featureReport);

        Assert.IsFalse(result.IsSupported);
        Assert.IsTrue(result.UnsupportedFeatures.Any(static feature =>
            feature.Kind == ExecutionTargetFeatureKind.QueryRowSourceAccess));
    }

    [TestMethod]
    public void HostAbiInventory_WhenQueryRowRuntimeServiceHasNoImport_ShouldRejectIt()
    {
        var runtimeServices = TargetRuntimeServiceRequirements.Create(
            TargetRuntimeServiceRequirementKind.QueryRowSourceAccess);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TargetHostAbiInventory.Empty.ValidateRuntimeServices(runtimeServices));

        StringAssert.Contains(exception.Message, "QueryRowSourceAccess");
        StringAssert.Contains(exception.Message, "missing");
    }

    private static ExecutionSourceBinding CreateBinding(ExecutionQueryRowSourceTransfer transfer)
    {
        var field = new FieldBinding(
            "Id",
            "source.Id",
            0,
            typeof(int),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess(QueryRowSourceNaming.CreateFieldName(0)));
        return new ExecutionSourceBinding(
            "test",
            "rows",
            "source:0",
            0,
            [],
            [field],
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(object)),
            QueryRowSourceTransfer: transfer);
    }

    private static ExecutionQueryRowSourceTransfer CreateTransfer()
    {
        var shape = new QueryRowShape(
        [
            new QueryRowField(0, 0, "Id", typeof(int), false)
        ]);
        return new ExecutionQueryRowSourceTransfer(
            ExecutionQueryRowCarrier.ReadonlyStruct,
            shape.Fingerprint,
            [
                new ExecutionQueryRowField(
                    0,
                    0,
                    "Id",
                    ExecutionClrBindingFactory.FromClr(typeof(int)),
                    false)
            ]);
    }

    private static ExecutionPlan CreatePlan(ExecutionSourceBinding binding)
    {
        return new ExecutionPlan(
            "query-row",
            [],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("source", typeof(object)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));
    }

    private static ExecutionQueryRowField CreateField(int slot, int sourceColumnIndex)
    {
        return new ExecutionQueryRowField(
            slot,
            sourceColumnIndex,
            $"F{slot}",
            ExecutionClrBindingFactory.FromClr(typeof(int)),
            false);
    }

    private static TargetQueryRowFieldContract CreateTargetField(int slot, int sourceColumnIndex)
    {
        return new TargetQueryRowFieldContract(
            slot,
            sourceColumnIndex,
            $"F{slot}",
            ExecutionClrBindingFactory.FromClr(typeof(int)).Descriptor,
            false,
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static TargetQueryRowFieldAbiContract CreateAbiField(int slot, int sourceColumnIndex)
    {
        return new TargetQueryRowFieldAbiContract(
            slot,
            sourceColumnIndex,
            $"F{slot}",
            ExecutionClrBindingFactory.FromClr(typeof(int)).Descriptor,
            false,
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static string CreateFingerprint() => new('A', 64);

    private sealed class HiddenQueryRowField;

    public enum NativeStatus
    {
        Queued = 10,
        Waiting = Queued,
        Running = 20
    }
}
