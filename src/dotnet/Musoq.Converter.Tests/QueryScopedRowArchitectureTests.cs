using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class QueryScopedRowArchitectureTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void QueryRowTransfer_WhenCompiled_ShouldRemainIdenticalAcrossEveryPipelineBoundary()
    {
        var threshold = -Random.Shared.Next(1, int.MaxValue);
        var query = string.Create(
            CultureInfo.InvariantCulture,
            $"select p.Name, p.Value from #queryrows.items() p where p.Value > {threshold}");
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"query-row-architecture-{Guid.NewGuid():N}",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));

        try
        {
            var items = result.BuildItems ??
                        throw new AssertFailedException("Compilation did not retain build artifacts.");
            var planner = items.PlanningResult?.ExecutionArtifacts.SourceTransferPlansBySourceId?
                              .Values.Single() ??
                          throw new AssertFailedException("The planner did not expose a source-transfer decision.");
            var physical = FlattenPhysical(items.PhysicalPlan!).OfType<PhysicalSchemaScanNode>().Single();
            var execution = ExecutionIrAnalysis.FlattenNodes(items.ExecutionPlan!.Body)
                .OfType<ExecutionSourceScan>()
                .Single();
            var executionTransfer = execution.Binding.QueryRowSourceTransfer ??
                                    throw new AssertFailedException("Execution IR lost the query-row transfer.");
            var target = items.TargetRuntimeContract?.QueryRowSourceAccess.Single() ??
                         throw new AssertFailedException("The target contract lost the query-row transfer.");
            var hostImport = TargetHostAbiInventoryBuilder.Build(items.TargetRuntimeContract!)
                .Imports.Single(static import => import.Kind == TargetHostAbiImportKind.QueryRowSourceAccess);
            var abi = Assert.IsInstanceOfType<TargetQueryRowSourceAccessAbiDetails>(hostImport.Details);
            var generatedCode = ExecutionTargetCatalog.InspectArtifact(items.RenderingArtifact).GeneratedCSharpCode ??
                                throw new AssertFailedException("The rendered artifact has no generated C# inspection.");

            Assert.AreEqual(SourceTransferMode.QueryScopedRows, planner.Mode);
            Assert.AreSame(planner, physical.SourceTransferStrategy);
            Assert.AreEqual(planner.Shape!.Fingerprint, executionTransfer.ShapeFingerprint);
            Assert.AreEqual(planner.Shape.Fingerprint, target.ShapeFingerprint);
            Assert.AreEqual(planner.Shape.Fingerprint, abi.ShapeFingerprint);
            Assert.AreEqual(planner.Carrier!.Value.ToString(), executionTransfer.Carrier.ToString());
            Assert.AreEqual(planner.Carrier.Value.ToString(), target.Carrier.ToString());
            Assert.AreEqual(planner.Carrier.Value.ToString(), abi.Carrier);
            Assert.AreEqual(planner.Lifetime!.Value.ToString(), executionTransfer.Lifetime.ToString());
            Assert.AreEqual(planner.Lifetime.Value.ToString(), target.Lifetime.ToString());
            Assert.AreEqual(planner.Lifetime.Value.ToString(), abi.Lifetime);

            AssertFieldIdentity(planner, executionTransfer, target, abi);

            var carrierName = QueryRowSourceNaming.CreateCarrierTypeName(
                planner.Shape.Fingerprint,
                planner.Carrier.Value);
            StringAssert.Contains(generatedCode, $"GetQueryScopedRowSource<{carrierName},");
            StringAssert.Contains(generatedCode, planner.Shape.Fingerprint);
            StringAssert.Contains(generatedCode, "reader.Read<string>(0)");
            StringAssert.Contains(generatedCode, "reader.Read<int>(1)");
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private static void AssertFieldIdentity(
        SourceTransferStrategyPlan planner,
        ExecutionQueryRowSourceTransfer execution,
        TargetQueryRowSourceAccessContract target,
        TargetQueryRowSourceAccessAbiDetails abi)
    {
        Assert.AreEqual(planner.Shape!.Fields.Count, execution.Fields.Count);
        Assert.AreEqual(planner.Shape.Fields.Count, target.Fields.Count);
        Assert.AreEqual(planner.Shape.Fields.Count, abi.Fields.Count);
        for (var index = 0; index < planner.Shape.Fields.Count; index++)
        {
            var planned = planner.Shape.Fields[index];
            var lowered = execution.Fields[index];
            var contracted = target.Fields[index];
            var imported = abi.Fields[index];

            Assert.AreEqual(planned.Slot, lowered.Slot);
            Assert.AreEqual(planned.Slot, contracted.Slot);
            Assert.AreEqual(planned.Slot, imported.Slot);
            Assert.AreEqual(planned.SourceColumnIndex, lowered.SourceColumnIndex);
            Assert.AreEqual(planned.SourceColumnIndex, contracted.SourceColumnIndex);
            Assert.AreEqual(planned.SourceColumnIndex, imported.SourceColumnIndex);
            Assert.AreEqual(planned.Name, lowered.Name);
            Assert.AreEqual(planned.Name, contracted.Name);
            Assert.AreEqual(planned.Name, imported.Name);
            Assert.AreEqual(lowered.FieldType.Descriptor.StableName, contracted.Type.StableName);
            Assert.AreEqual(lowered.FieldType.Descriptor.StableName, imported.TypeSymbol.StableName);
            Assert.AreEqual(planned.IsNullable, lowered.IsNullable);
            Assert.AreEqual(planned.IsNullable, contracted.IsNullable);
            Assert.AreEqual(planned.IsNullable, imported.IsNullable);
        }
    }

    private static IEnumerable<PhysicalNode> FlattenPhysical(PhysicalNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in FlattenPhysical(child))
                yield return descendant;
        }
    }
}
