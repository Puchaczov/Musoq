using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void CteSourceBackedSiblingFusion_WhenSourceInteractionMetadataIsMissing_ShouldKeepMaterializedSourceCte()
    {
        var ageField = new FieldBinding(
            "Age",
            "raw.Age",
            0,
            typeof(int),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess("Age"));
        var rawShape = new GeneratedRowShape("Cte0Row0", [ageField]);
        var sourceItem = new ExecutionVariable("p", typeof(Person));
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
        var appendRaw = new ExecutionAppendRow(
            rawTable,
            rawShape,
            [new ExecutionRowValue("Age", new ExecutionFieldRead("p", "Age", typeof(int), new ClrPropertyAccess("Age")))]);
        var builtNodes = new List<ExecutionNode>
        {
            sourceScan,
            new ExecutionCreateTable(rawTable, rawShape),
            new ExecutionForEach(sourceItem, new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks), new ExecutionBlock([appendRaw])),
            new ExecutionStoreTable(rawTable, 0)
        };

        var hash = new ExecutionVariable("ageHash", typeof(Dictionary<int, HashJoinBucket<object>>));
        var payload = new ExecutionVariable("payload", typeof(object), "Payload0");
        var producer = new ExecutionFusedCteProducer(
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
        var shapes = new List<RowShape> { rawShape };

        var rewritten = CteSourceBackedSiblingFusion.TryRewrite(
            builtNodes,
            shapes,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["raw"] = 0 },
            new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase)
            {
                ["raw"] = new("raw", ReferenceCount: 1, CteOutputFlags.None)
            },
            new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal),
            producer);

        Assert.IsNull(rewritten);
        Assert.IsTrue(builtNodes.Exists(static node => node is ExecutionCreateTable));
        Assert.IsTrue(builtNodes.Exists(static node => node is ExecutionStoreTable { TableIndex: 0 }));
        Assert.IsTrue(shapes.Contains(rawShape));
    }

    [TestMethod]
    public void CteSourceBackedSiblingFusion_WhenSourceInteractionMetadataIsSafe_ShouldEmitReadOnceCandidate()
    {
        var ageField = new FieldBinding(
            "Age",
            "raw.Age",
            0,
            typeof(int),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess("Age"));
        var tagContext = new FieldBinding(
            "Tag",
            "raw.Tag",
            1,
            typeof(string),
            FieldNullability.NotNullable,
            new GeneratedRowContextAccess("Cte0Row0", 0));
        var rawShape = new GeneratedRowShape("Cte0Row0", [ageField], [tagContext]);
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
        var appendRaw = new ExecutionAppendRow(
            rawTable,
            rawShape,
            [new ExecutionRowValue("Age", new ExecutionFieldRead("p", "Age", typeof(int), new ClrPropertyAccess("Age")))],
            [new ExecutionLiteral("source-tag", typeof(string))]);
        var builtNodes = new List<ExecutionNode>
        {
            sourceScan,
            new ExecutionCreateTable(rawTable, rawShape),
            new ExecutionForEach(
                new ExecutionVariable("p", typeof(Person)),
                new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                new ExecutionBlock([appendRaw])),
            new ExecutionStoreTable(rawTable, 0)
        };
        var hash = new ExecutionVariable("ageHash", typeof(Dictionary<int, HashJoinBucket<object>>));
        var payload = new ExecutionVariable("payload", typeof(object), "Payload0");
        var producer = new ExecutionFusedCteProducer(
            [new ExecutionFusedCteOutput(1, new ExecutionVariable("cte1", typeof(Table), "Cte1Row0"), rawShape, StoreRows: false)],
            new ExecutionBlock(
            [
                new ExecutionCreateHash(hash, typeof(int), typeof(object), new ExecutionRowsCapacityHintCandidate(hash, new ExecutionStoredTableRows(0, rawShape)), "Payload0"),
                new ExecutionForEach(
                    new ExecutionVariable("raw", typeof(object), rawShape.TypeName),
                    new ExecutionStoredTableRows(0, rawShape),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            new ExecutionVariable("tag", typeof(string)),
                            new ExecutionFieldRead("raw", "Tag", typeof(string), new GeneratedRowContextAccess(rawShape.TypeName, 0))),
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

        Assert.IsNotNull(rewritten);
        Assert.IsTrue(builtNodes.Exists(static node => node is ExecutionCteReadOnceFusionCandidate { RelatedTableIndex: 0 }));
        Assert.IsFalse(builtNodes.Exists(static node => node is ExecutionCreateTable));
        Assert.IsFalse(shapes.Contains(rawShape));
        Assert.IsFalse(ExecutionIrAnalysis
            .CollectNodes<ExecutionSourceLoop>(rewritten.Body)
            .Any(static loop => loop.Source is ExecutionStoredTableRows));
        Assert.IsTrue(ExecutionIrAnalysis
            .CollectNodes<ExecutionHashAdd>(rewritten.Body)
            .Any(static add => add.Key is ExecutionFieldRead { Alias: "p", FieldName: "Age" }));
        Assert.IsTrue(ExecutionIrAnalysis
            .CollectNodes<ExecutionLet>(rewritten.Body)
            .Any(static let => let.Value is ExecutionLiteral literal &&
                               Equals(literal.Value.ToClrValue(), "source-tag")));
        Assert.IsFalse(ExecutionIrAnalysis
            .CollectNodes<ExecutionCreateHash>(rewritten.Body)
            .Any(static createHash => createHash.CapacityHint is ExecutionRowsCapacityHintCandidate
            {
                Rows: ExecutionRowStream { Kind: ExecutionRowStreamKind.Chunks }
            }));
    }

    private static SourceInteractionPlan CreateSafeSourceInteractionPlan()
    {
        return new SourceInteractionPlan(
            "p:1",
            "p",
            SourceShapeKind.KnownClr,
            SourceColumnContract.FullColumns,
            SourcePredicateContract.None,
            SourceArgumentMode.ConstantArguments,
            [],
            null,
            SourcePlanRequest.Empty(new SourceIdentity("test", "data", "p:1", "p")),
            PlanningConfidence.High,
            "known CLR source",
            "full columns",
            "no predicate",
            "empty request",
            "constant arguments");
    }

}
