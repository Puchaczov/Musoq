using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionIrAnalysisTests
{
    [TestMethod]
    public void CollectNodes_WhenBlockContainsNestedNodes_ShouldReturnNestedNodesInTraversalOrder()
    {
        var let = new ExecutionLet(
            new ExecutionVariable("value", typeof(int)),
            new ExecutionLiteral(1, typeof(int)));
        var branch = new ExecutionIf(
            new ExecutionLiteral(true, typeof(bool)),
            new ExecutionBlock([let]));
        var block = new ExecutionBlock([branch]);

        var nodeNames = ExecutionIrAnalysis
            .CollectNodes<ExecutionNode>(block)
            .Select(static node => node.GetType().Name)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { nameof(ExecutionIf), nameof(ExecutionLet) },
            nodeNames);
    }

    [TestMethod]
    public void FlattenExpressions_WhenBlockContainsNestedNodeExpressions_ShouldReturnAllNodeAndChildExpressions()
    {
        var sourceRows = new ExecutionStoredTableRows(7);
        var constantSet = new ExecutionConstantInSet(
            typeof(int),
            [1, 2],
            ExecutionConstantInSetKind.Array);
        var predicate = new ExecutionInCheck(
            new ExecutionVariableRead(new ExecutionVariable("item", typeof(int))),
            [new ExecutionLiteral(1, typeof(int))],
            typeof(bool),
            constantSet);
        var contextParameter = new ExecutionScriptParameterRead("context", typeof(string));
        var rowShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(bool), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
        var appendRow = new ExecutionAppendRow(
            new ExecutionVariable("result", typeof(object)),
            rowShape,
            [new ExecutionRowValue("Value", predicate)],
            [new ExecutionRowContextsRead(new ExecutionVariable("row", typeof(object)))],
            ExecutionAppendMode.Checked,
            new ExecutionContextLayout(
            [
                new ExecutionContextSegment(ExecutionContextSegmentKind.Single, contextParameter, 1)
            ]));
        var block = new ExecutionBlock(
        [
            new ExecutionForEach(
                new ExecutionVariable("item", typeof(int)),
                sourceRows,
                new ExecutionBlock([appendRow]))
        ]);

        var expressions = ExecutionIrAnalysis.FlattenExpressions(block).ToArray();

        Assert.IsTrue(expressions.Contains(sourceRows));
        Assert.IsTrue(expressions.Contains(predicate));
        Assert.IsTrue(expressions.Contains(contextParameter));
        Assert.AreEqual(
            7,
            ExecutionIrAnalysis.CollectExpressions<ExecutionStoredTableRows>(block).Single().TableIndex);
        Assert.AreSame(
            constantSet,
            ExecutionIrAnalysis.CollectExpressions<ExecutionInCheck>(block).Single().ConstantSet);
    }

    [TestMethod]
    public void ContainsVariableUse_WhenProbeNodesContainReferences_ShouldFindDirectExpressionsAndBodies()
    {
        var hash = Var("hash");
        var hashMatches = Var("hashMatches");
        var hashKey = Var("hashKey", typeof(int));
        var hashBodyTable = Var("hashBodyTable");
        var hashMissTable = Var("hashMissTable");
        var hashFound = Var("hashFound", typeof(bool));
        var keySet = Var("keySet");
        var keySetKey = Var("keySetKey", typeof(int));
        var keySetBodyTable = Var("keySetBodyTable");
        var keySetMissTable = Var("keySetMissTable");
        var keySetFound = Var("keySetFound", typeof(bool));
        var asOfMatch = Var("asOfMatch");
        var asOfCandidate = Var("asOfCandidate");
        var asOfRows = Var("asOfRows");
        var asOfLeft = Var("asOfLeft");
        var asOfRight = Var("asOfRight");
        var asOfProbeKey = Var("asOfProbeKey", typeof(int));
        var asOfCandidateKey = Var("asOfCandidateKey", typeof(int));
        var asOfBodyTable = Var("asOfBodyTable");
        var asOfMissTable = Var("asOfMissTable");
        var asOfIndex = Var("asOfIndex");
        var rangeMatch = Var("rangeMatch");
        var rangeIndex = Var("rangeIndex");
        var rangeProbeKey = Var("rangeProbeKey", typeof(int));
        var rangeBodyTable = Var("rangeBodyTable");
        var block = new ExecutionBlock(
        [
            new ExecutionHashProbe(
                hash,
                hashMatches,
                Read(hashKey),
                typeof(int),
                typeof(object),
                new ExecutionBlock([new ExecutionReturnTable(hashBodyTable)]),
                new ExecutionBlock([new ExecutionReturnTable(hashMissTable)]),
                hashFound),
            new ExecutionKeySetProbe(
                keySet,
                Read(keySetKey),
                typeof(int),
                new ExecutionBlock([new ExecutionReturnTable(keySetBodyTable)]),
                new ExecutionBlock([new ExecutionReturnTable(keySetMissTable)]),
                keySetFound),
            new ExecutionAsOfProbe(
                asOfMatch,
                asOfCandidate,
                new ExecutionRowStream(asOfRows, ExecutionRowStreamKind.Chunks),
                [new ExecutionAsOfEqualityKey(Read(asOfLeft), Read(asOfRight))],
                Read(asOfProbeKey),
                Read(asOfCandidateKey),
                BinaryOpKind.GreaterOrEqual,
                new ExecutionBlock([new ExecutionReturnTable(asOfBodyTable)]),
                new ExecutionBlock([new ExecutionReturnTable(asOfMissTable)]),
                asOfIndex),
            new ExecutionRangeProbe(
                rangeMatch,
                rangeIndex,
                Read(rangeProbeKey),
                typeof(int),
                new ExecutionBlock([new ExecutionReturnTable(rangeBodyTable)]))
        ]);

        AssertUses(
            block,
            hash,
            hashMatches,
            hashKey,
            hashBodyTable,
            hashMissTable,
            hashFound,
            keySet,
            keySetKey,
            keySetBodyTable,
            keySetMissTable,
            keySetFound,
            asOfMatch,
            asOfRows,
            asOfLeft,
            asOfRight,
            asOfProbeKey,
            asOfCandidateKey,
            asOfBodyTable,
            asOfMissTable,
            asOfIndex,
            rangeMatch,
            rangeIndex,
            rangeProbeKey,
            rangeBodyTable);
        AssertDoesNotUse(block, asOfCandidate);
        AssertDoesNotUse(block, Var("missing"));
    }

    [TestMethod]
    public void ContainsVariableUse_WhenRowConstructionContainsContexts_ShouldFindRowValuesAndLayout()
    {
        var rowShape = CreateRowShape();
        var generatedRow = Var("generatedRow");
        var generatedValue = Var("generatedValue");
        var generatedContextRow = Var("generatedContextRow");
        var generatedLayout = Var("generatedLayout");
        var table = Var("table");
        var appendValue = Var("appendValue");
        var appendContextRow = Var("appendContextRow");
        var appendLayout = Var("appendLayout");
        var block = new ExecutionBlock(
        [
            new ExecutionCreateGeneratedRow(
                generatedRow,
                rowShape,
                [new ExecutionRowValue("Value", Read(generatedValue))],
                [new ExecutionRowContextsRead(generatedContextRow)],
                new ExecutionContextLayout(
                [
                    new ExecutionContextSegment(ExecutionContextSegmentKind.Single, Read(generatedLayout), 1)
                ])),
            new ExecutionAppendRow(
                table,
                rowShape,
                [new ExecutionRowValue("Value", Read(appendValue))],
                [new ExecutionRowContextsRead(appendContextRow)],
                ExecutionAppendMode.Checked,
                new ExecutionContextLayout(
                [
                    new ExecutionContextSegment(ExecutionContextSegmentKind.Single, Read(appendLayout), 1)
                ]))
        ]);

        AssertUses(
            block,
            generatedRow,
            generatedValue,
            generatedContextRow,
            generatedLayout,
            table,
            appendValue,
            appendContextRow,
            appendLayout);
    }

    [TestMethod]
    public void ContainsVariableUse_WhenAggregateGroupNodesContainReferences_ShouldFindGroupVariablesAndKeys()
    {
        var shape = new AggregateGroupShape("Group0", [], [], []);
        var plan = new AggregateGroupPlan(shape, [new AggregateGroupLevelPlan(0, shape)]);
        var rootGroup = Var("rootGroup");
        var groups = Var("groups");
        var groupsToFinalize = Var("groupsToFinalize");
        var nullGroup = Var("nullGroup");
        var group = Var("group");
        var key = Var("key", typeof(int));
        var block = new ExecutionBlock(
        [
            new ExecutionCreateSingleKeyAggregateContext(
                rootGroup,
                groups,
                groupsToFinalize,
                nullGroup,
                typeof(int),
                plan),
            new ExecutionGetOrAddSingleKeyAggregateGroup(
                rootGroup,
                groups,
                groupsToFinalize,
                group,
                Read(key),
                "Key",
                typeof(int),
                nullGroup,
                plan)
        ]);

        AssertUses(block, rootGroup, groups, groupsToFinalize, nullGroup, group, key);
    }

    [TestMethod]
    public void IsVariableUsedAfter_WhenPostOperationUsesTableAndCapacityHint_ShouldFindLaterReferences()
    {
        var source = Var("source");
        var target = Var("target");
        var capacityCollection = Var("capacityCollection");
        var nodes = new ExecutionNode[]
        {
            new ExecutionStoreTable(source, 0),
            new ExecutionTopOffsetTable(
                source,
                target,
                [],
                1,
                2,
                [],
                ExecutionTopOffsetStrategy.OrderedSlice,
                new ExecutionSkipTakeCapacityHint(capacityCollection, 1, 2))
        };
        var block = new ExecutionBlock(nodes);

        AssertUses(block, source, target, capacityCollection);
        Assert.IsTrue(ExecutionIrAnalysis.IsVariableUsedAfter(nodes, 0, source.Name));
        Assert.IsFalse(ExecutionIrAnalysis.IsVariableUsedAfter(nodes, 1, source.Name));
    }

    private static ExecutionVariable Var(string name, Type? type = null)
    {
        return new ExecutionVariable(name, type ?? typeof(object));
    }

    private static ExecutionVariableRead Read(ExecutionVariable variable)
    {
        return new ExecutionVariableRead(variable);
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(object), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
    }

    private static void AssertUses(ExecutionBlock block, params ExecutionVariable[] variables)
    {
        foreach (var variable in variables)
            Assert.IsTrue(ExecutionIrAnalysis.ContainsVariableUse(block, variable.Name), $"Expected {variable.Name} to be used.");
    }

    private static void AssertDoesNotUse(ExecutionBlock block, ExecutionVariable variable)
    {
        Assert.IsFalse(ExecutionIrAnalysis.ContainsVariableUse(block, variable.Name), $"Expected {variable.Name} to be unused.");
    }
}
