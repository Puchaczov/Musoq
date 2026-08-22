using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{
    [TestMethod]
    [DataRow("union")]
    [DataRow("union (Result)")]
    [DataRow("union all")]
    [DataRow("except")]
    [DataRow("intersect")]
    [FeatureEvidence("set-result-modifiers", FeatureEvidenceKind.RuntimePositive)]
    public void ResultOrderBy_WithAliasesAndNullOrdering_ShouldApplyAfterTheSet(string setOperator)
    {
        var query = $"select Name as Result from #A.Entities() {setOperator} select Name as Other from #B.Entities() order by Result nulls last";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity(null), new BasicEntity("c"), new BasicEntity("a")],
            ["#B"] = [new BasicEntity("b"), new BasicEntity("a"), new BasicEntity(null)]
        };

        var compiled = CreateAndRunVirtualMachine(query, sources);
        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Result", typeof(string)));
        var expected = setOperator switch
        {
            "union" or "union (Result)" => new object?[][] { ["a"], ["b"], ["c"], [null] },
            "union all" => [["a"], ["a"], ["b"], ["c"], [null], [null]],
            "except" => [["c"]],
            "intersect" => [["a"], [null]],
            _ => throw new InvalidOperationException(setOperator)
        };
        TableMaterializationTestHelper.AssertRowsInOrder(table, expected);
    }

    [TestMethod]
    public void ChainedUnionAll_WithDescendingSkipAndTake_ShouldSliceTheCombinedResult()
    {
        const string query = @"
select Name as Label from #A.Entities()
union all select Name from #B.Entities()
union all select Name from #C.Entities()
order by Label desc skip 1 take 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("delta"), new BasicEntity("alpha")],
            ["#B"] = [new BasicEntity("charlie")],
            ["#C"] = [new BasicEntity("bravo"), new BasicEntity("echo")]
        };

        var compiled = CreateAndRunVirtualMachine(query, sources);
        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["delta"],
            ["charlie"],
            ["bravo"]);
    }

    [TestMethod]
    public void DerivedOperand_WithLocalSlice_ShouldRemainLocalWhileTrailingOrderIsGlobal()
    {
        const string query = @"
select sliced.Name as Label
from (select Name from #A.Entities() order by Name take 1) sliced
union all
select Name from #B.Entities()
order by Label desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("zulu"), new BasicEntity("alpha")],
            ["#B"] = [new BasicEntity("mike")]
        };

        var compiled = CreateAndRunVirtualMachine(query, sources);
        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["mike"], ["alpha"]);
    }

    [TestMethod]
    public void ResultModifiers_Inspection_ShouldPlaceOrderingAndSlicingAboveTheSet()
    {
        const string query = @"
select Name as Label from #A.Entities()
union all select Name from #B.Entities()
order by Label desc skip 1 take 2";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            }),
            new TestsLoggerResolver());

        AssertTextBefore("Take [2]", "Skip [1]", inspection.LogicalPlanText);
        AssertTextBefore("Skip [1]", "Sort [Label DESC]", inspection.LogicalPlanText);
        AssertTextBefore("Sort [Label DESC]", "SetOp [UnionAll]", inspection.LogicalPlanText);
        Assert.Contains("PhysicalTopOffset [skip 1, take 2]", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalSetOp [UnionAll]", inspection.PhysicalPlanText);
        Assert.Contains("TopOffset", inspection.ExecutionPlanText);
        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));
    }

    [TestMethod]
    public void CloneQueryVisitor_ShouldPreserveIndependentSetResultModifiers()
    {
        const string query = "select Name as Label from schemaA.methodA() union all select Name from schemaB.methodB() order by Label desc skip 1 take 2";
        var root = new Musoq.Parser.Parser(new Lexer(query, true)).ComposeAll();
        var visitor = new CloneQueryVisitor();

        root.Accept(new CloneTraverseVisitor(visitor));

        var original = GetSetOperator(root);
        var clone = GetSetOperator(visitor.Root);
        Assert.AreEqual(original.Id, clone.Id);
        Assert.AreEqual(original.ToString(), clone.ToString());
        Assert.AreNotSame(original, clone);
        Assert.AreNotSame(original.ResultOrderBy, clone.ResultOrderBy);
        Assert.AreNotSame(original.ResultSkip, clone.ResultSkip);
        Assert.AreNotSame(original.ResultTake, clone.ResultTake);
    }

    private static SetOperatorNode GetSetOperator(RootNode root)
    {
        var statements = (StatementsArrayNode)root.Expression;
        return (SetOperatorNode)statements.Statements[0].Node;
    }

    private static void AssertTextBefore(string first, string second, string text)
    {
        var firstIndex = text.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = text.IndexOf(second, StringComparison.Ordinal);
        Assert.IsTrue(firstIndex >= 0, text);
        Assert.IsTrue(secondIndex >= 0, text);
        Assert.IsTrue(firstIndex < secondIndex, text);
    }
}
