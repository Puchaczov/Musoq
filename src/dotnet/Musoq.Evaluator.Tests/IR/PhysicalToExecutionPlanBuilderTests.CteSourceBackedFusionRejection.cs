using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void CteSourceBackedSiblingFusion_WhenMappedExpressionIsUnsupported_ShouldKeepMaterializedSourceCte()
    {
        var ageField = new FieldBinding(
            "Age",
            "raw.Age",
            0,
            typeof(int),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess("Age"));
        var rawShape = new GeneratedRowShape("Cte0Row0", [ageField]);
        var sourceRows = new ExecutionVariable("pRows", typeof(IEnumerable<Person>));
        var sourceScan = new ExecutionSourceScan(
            new ExecutionVariable("pSource", typeof(Person)),
            sourceRows,
            new ExecutionSourceBinding(
                "test",
                "data",
                "p:1",
                0,
                [],
                [ageField],
                SourceType: ExecutionClrBindingFactory.FromClr(typeof(Person))));
        var rawTable = new ExecutionVariable("cte0", typeof(Table), rawShape.TypeName);
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var appendWithCall = new ExecutionAppendRow(
            rawTable,
            rawShape,
            [new ExecutionRowValue(
                "Age",
                new ExecutionMethodCall(
                    method,
                    [new ExecutionLiteral(-42, typeof(int))],
                    null,
                    typeof(int)))]);
        var builtNodes = new List<ExecutionNode>
        {
            sourceScan,
            new ExecutionCreateTable(rawTable, rawShape),
            new ExecutionForEach(
                new ExecutionVariable("p", typeof(Person)),
                new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                new ExecutionBlock([appendWithCall])),
            new ExecutionStoreTable(rawTable, 0)
        };

        var producer = CreateSourceBackedProducer(rawShape, ageField);
        var shapes = new List<RowShape> { rawShape };

        var rewritten = CteSourceBackedSiblingFusion.TryRewrite(
            builtNodes,
            shapes,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["raw"] = 0 },
            new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase)
            {
                ["raw"] = new("raw", ReferenceCount: 1, CteOutputFlags.None)
            },
            new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal)
            {
                ["p:1"] = CreateSafeSourceInteractionPlan()
            },
            producer);

        Assert.IsNull(rewritten);
        Assert.IsTrue(builtNodes.Exists(static node => node is ExecutionCreateTable));
        Assert.IsTrue(builtNodes.Exists(static node => node is ExecutionStoreTable { TableIndex: 0 }));
        Assert.IsTrue(shapes.Contains(rawShape));
    }

    private static ExecutionFusedCteProducer CreateSourceBackedProducer(
        GeneratedRowShape rawShape,
        FieldBinding ageField)
    {
        var hash = new ExecutionVariable("ageHash", typeof(Dictionary<int, HashJoinBucket<object>>));
        var payload = new ExecutionVariable("payload", typeof(object), "Payload0");

        return new ExecutionFusedCteProducer(
            [new ExecutionFusedCteOutput(1, new ExecutionVariable("cte1", typeof(Table), "Cte1Row0"), rawShape, StoreRows: false)],
            new ExecutionBlock(
            [
                new ExecutionCreateHash(hash, typeof(int), typeof(object), new ExecutionStoredTableCountCapacityHint(0), "Payload0"),
                new ExecutionForEach(
                    new ExecutionVariable("raw", typeof(object), rawShape.TypeName),
                    new ExecutionStoredTableRows(0, rawShape),
                    new ExecutionBlock(
                    [
                        new ExecutionCreateHashPayload(
                            payload,
                            new HashPayloadShape("Payload0", [ageField]),
                            [new ExecutionRowValue("Age", new ExecutionFieldRead("raw", "Age", typeof(int), new GeneratedFieldAccess("Age")))]),
                        new ExecutionHashAdd(
                            hash,
                            new ExecutionFieldRead("raw", "Age", typeof(int), new GeneratedFieldAccess("Age")),
                            payload,
                            typeof(int),
                            typeof(object),
                            "Payload0")
                    ])),
                new ExecutionStoreCteIndex(hash, 0, ExecutionCteSidecarIndexKind.Hash, typeof(int), typeof(object), "Payload0")
            ]));
    }
}
