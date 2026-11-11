using System.Linq;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Planning;
using Musoq.Targets.Execution.Analysis;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ByteBufferExecutionContractTests
{
    [TestMethod]
    [DataRow("with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f) select Length(b.Content) as Size, FromBytesToInt32(b.Content) as Value from bytes b", true)]
    [DataRow("with bytes as (select MergeArrays(f.Content, f.Content) as Content from #schema.first() f) select Length(b.Content) as Size, FromBytesToInt32(b.Content) as Value from bytes b", false)]
    [DataRow("select Length(FromBase64(ToBase64(f.Content, 0, 2))) as Size from #schema.first() f", true)]
    [DataRow("select Length(EnumerableToArray(f.Content)) as Size from #schema.first() f", false)]
    [DataRow("select FromBytesToInt32(EnumerableToArray(Skip(f.Content, 1))) as Value from #schema.first() f", true)]
    [DataRow("select MergeArrays(f.Content, f.Content) as Value from #schema.first() f", false)]
    public void PlannedPrograms_DeclareExactlyTheirExistingSnapshotService(string sql, bool requiresSnapshot)
    {
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([new()]),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        var plan = inspection.ExecutionPlan!;
        var runtime = TargetRuntimeContractBuilder.Build(plan, ExecutionTargetCompatibilityAnalyzer.Analyze(plan));
        Assert.AreEqual(requiresSnapshot, runtime.RequiresByteBufferSnapshots);
        var inventory = TargetHostAbiInventoryBuilder.Build(runtime);
        CollectionAssert.AreEqual(requiresSnapshot ? new[] { "byte-buffer-snapshot-v1" } : Array.Empty<string>(),
            inventory.Imports.Where(import => import.Kind == TargetHostAbiImportKind.ByteBufferSnapshot).Select(import => import.Contract).ToArray());
        inventory.ValidateRuntimeServices(inventory.CreateServiceRequirements(TargetRuntimeServiceFulfillmentKind.HostImport));
    }

    [TestMethod]
    public void OwnedSnapshot_ShouldRemainIdentitySensitiveAndDeclareItsRuntimeImport()
    {
        ExecutionExpression[] arguments = [new ExecutionLiteral(null, typeof(byte[]))];
        var snapshot = new ExecutionByteBufferSnapshot(arguments,
            new(BinaryBufferInputKind.Snapshot, 8, BinaryBufferEmptyResult.EmptyArray, false, "test"));
        arguments[0] = new ExecutionLiteral(null, typeof(string));
        Assert.AreEqual(typeof(byte[]), snapshot.Arguments[0].ReturnType.ResolveClrType());
        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(snapshot));
        var plan = new ExecutionPlan("Snapshot", [], new ExecutionBlock([
            new ExecutionLet(new ExecutionVariable("copy", typeof(byte[])), snapshot)]));
        var compatibility = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        var runtime = TargetRuntimeContractBuilder.Build(plan, compatibility);
        var inventory = TargetHostAbiInventoryBuilder.Build(runtime);
        var import = inventory.Imports.Single(item => item.Kind == TargetHostAbiImportKind.ByteBufferSnapshot);
        Assert.AreEqual("byte-buffer-snapshot-v1", import.Contract);
        Assert.AreEqual("independent-array", import.Attributes["ownership"]);
        inventory.ValidateRuntimeServices(inventory.CreateServiceRequirements(TargetRuntimeServiceFulfillmentKind.HostImport));
    }
}
