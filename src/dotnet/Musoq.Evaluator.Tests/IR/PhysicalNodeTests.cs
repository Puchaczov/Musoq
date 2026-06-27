using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class PhysicalNodeTests
{
    private static OutputSchema CreateSchema(params (string Name, Type Type)[] cols)
    {
        var columns = new ColumnSchema[cols.Length];
        for (var i = 0; i < cols.Length; i++)
            columns[i] = new ColumnSchema(cols[i].Name, cols[i].Type, i);
        return new OutputSchema(columns);
    }

    private static PhysicalSchemaScanNode CreateScan(string alias = "t", params (string Name, Type Type)[] cols)
    {
        if (cols.Length == 0)
            cols = [("Id", typeof(int)), ("Name", typeof(string))];

        return new PhysicalSchemaScanNode("test", "data", [], alias, [], [], CreateSchema(cols));
    }

    private static AggregateBinding CreateAggBinding(string name = "Count")
    {
        var setMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
        var getMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        return new AggregateBinding(name, name, setMethod, [], getMethod, [], typeof(int));
    }

    #region Leaf nodes

    [TestMethod]
    public void PhysicalSchemaScan_WhenConstructed_ShouldHaveNoChildren()
    {
        var scan = CreateScan();

        Assert.IsEmpty(scan.Children);
        Assert.AreEqual("test", scan.SchemaName);
        Assert.AreEqual("data", scan.MethodName);
        Assert.AreEqual("t", scan.Alias);
        Assert.HasCount(2, scan.OutputSchema.Columns);
    }

    [TestMethod]
    public void PhysicalSchemaScan_WhenPushedPredicates_ShouldStorePredicates()
    {
        var pred = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("t", "Id", typeof(int)), new Literal(1, typeof(int)), typeof(bool));
        var scan = new PhysicalSchemaScanNode("test", "data", [], "t", [pred], ["Id"], CreateSchema(("Id", typeof(int))));

        Assert.HasCount(1, scan.PushedPredicates);
        Assert.HasCount(1, scan.ProjectedColumns);
        Assert.AreEqual("Id", scan.ProjectedColumns[0]);
    }

    [TestMethod]
    public void PhysicalInterpretSource_WhenConstructed_ShouldHaveNoChildren()
    {
        var node = new PhysicalInterpretSourceNode(
            "binary",
            InterpretSourceKind.Interpret,
            [],
            "b",
            typeof(object),
            ApplyKind.Cross,
            CreateSchema(("Data", typeof(byte[]))));

        Assert.IsEmpty(node.Children);
        Assert.AreEqual("binary", node.SchemaName);
        Assert.AreEqual(InterpretSourceKind.Interpret, node.Kind);
        Assert.AreEqual("b", node.Alias);
    }

    [TestMethod]
    public void PhysicalCteRef_WhenConstructed_ShouldHaveNoChildren()
    {
        var node = new PhysicalCteRefNode("MyCte", "c", CreateSchema(("Id", typeof(int))));

        Assert.IsEmpty(node.Children);
        Assert.AreEqual("MyCte", node.CteName);
        Assert.AreEqual("c", node.Alias);
    }

    [TestMethod]
    public void PhysicalDesc_WhenConstructed_ShouldHaveNoChildren()
    {
        var node = new PhysicalDescNode("test", "data", DescType.Table, null, [], "desc-source", OutputSchema.Empty);

        Assert.IsEmpty(node.Children);
        Assert.AreEqual("test", node.SchemaName);
        Assert.AreEqual(DescType.Table, node.Type);
        Assert.IsEmpty(node.Arguments);
    }

    #endregion

    #region Unary operator nodes

    [TestMethod]
    public void PhysicalFilter_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var pred = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Id", typeof(int)), new Literal(5, typeof(int)), typeof(bool));
        var filter = new PhysicalFilterNode(pred, scan);

        Assert.HasCount(1, filter.Children);
        Assert.AreSame(scan.OutputSchema, filter.OutputSchema);
    }

    [TestMethod]
    public void PhysicalProject_WhenConstructed_ShouldDeriveSchemaFromFields()
    {
        var scan = CreateScan();
        var fields = new[] { new ProjectedField("Name", new ColumnRef("t", "Name", typeof(string)), 0) };
        var project = new PhysicalProjectNode(fields, scan);

        Assert.HasCount(1, project.Children);
        Assert.HasCount(1, project.OutputSchema.Columns);
        Assert.AreEqual("Name", project.OutputSchema.Columns[0].Name);
    }

    [TestMethod]
    public void PhysicalHavingFilter_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var pred = new Literal(true, typeof(bool));
        var having = new PhysicalHavingFilterNode(pred, scan);

        Assert.HasCount(1, having.Children);
        Assert.AreSame(scan.OutputSchema, having.OutputSchema);
    }

    [TestMethod]
    public void PhysicalQualifyFilter_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var pred = new Literal(true, typeof(bool));
        var qualify = new PhysicalQualifyFilterNode(pred, scan);

        Assert.HasCount(1, qualify.Children);
        Assert.AreSame(scan.OutputSchema, qualify.OutputSchema);
    }

    [TestMethod]
    public void PhysicalSort_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var keys = new[] { new OrderField(new ColumnRef("t", "Name", typeof(string)), Descending: true) };
        var sort = new PhysicalSortNode(keys, scan);

        Assert.HasCount(1, sort.Children);
        Assert.AreSame(scan.OutputSchema, sort.OutputSchema);
        Assert.IsTrue(sort.Keys[0].Descending);
    }

    [TestMethod]
    public void PhysicalSkip_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var skip = new PhysicalSkipNode(5, scan);

        Assert.HasCount(1, skip.Children);
        Assert.AreEqual(5, skip.Count);
        Assert.AreSame(scan.OutputSchema, skip.OutputSchema);
    }

    [TestMethod]
    public void PhysicalTake_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var take = new PhysicalTakeNode(10, scan);

        Assert.HasCount(1, take.Children);
        Assert.AreEqual(10, take.Count);
        Assert.AreSame(scan.OutputSchema, take.OutputSchema);
    }

    #endregion

    #region Aggregate strategy nodes

    [TestMethod]
    public void PhysicalSingleKeyAggregate_WhenConstructed_ShouldDeriveSchemaFromKeyAndBindings()
    {
        var scan = CreateScan();
        var key = new ColumnRef("t", "Name", typeof(string));
        var binding = CreateAggBinding();
        var agg = new PhysicalSingleKeyAggregateNode(key, "Name", typeof(string), [binding], scan);

        Assert.HasCount(1, agg.Children);
        Assert.HasCount(2, agg.OutputSchema.Columns);
        Assert.AreEqual("Name", agg.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(string), agg.OutputSchema.Columns[0].Type);
        Assert.AreEqual("Count", agg.OutputSchema.Columns[1].Name);
    }

    [TestMethod]
    public void PhysicalValueTupleAggregate_WhenConstructed_ShouldDeriveSchemaFromKeysAndBindings()
    {
        var scan = CreateScan();
        var keys = new IrExpression[]
        {
            new ColumnRef("t", "Name", typeof(string)),
            new ColumnRef("t", "Id", typeof(int))
        };
        var binding = CreateAggBinding();
        var agg = new PhysicalValueTupleAggregateNode(keys, ["Name", "Id"], [typeof(string), typeof(int)], [binding], scan);

        Assert.HasCount(1, agg.Children);
        Assert.HasCount(3, agg.OutputSchema.Columns);
        Assert.AreEqual("Name", agg.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Id", agg.OutputSchema.Columns[1].Name);
        Assert.AreEqual("Count", agg.OutputSchema.Columns[2].Name);
    }

    [TestMethod]
    public void PhysicalAggregateOnly_WhenConstructed_ShouldDeriveSchemaFromBindingsOnly()
    {
        var scan = CreateScan();
        var binding = CreateAggBinding();
        var agg = new PhysicalAggregateOnlyNode([binding], scan);

        Assert.HasCount(1, agg.Children);
        Assert.HasCount(1, agg.OutputSchema.Columns);
        Assert.AreEqual("Count", agg.OutputSchema.Columns[0].Name);
    }

    #endregion

    #region Join strategy nodes

    [TestMethod]
    public void PhysicalHashJoin_WhenConstructed_ShouldMergeSchemas()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("UserId", typeof(int)));
        var buildKey = new ColumnRef("a", "Id", typeof(int));
        var probeKey = new ColumnRef("b", "UserId", typeof(int));

        var join = new PhysicalHashJoinNode(JoinKind.Inner, [buildKey], [probeKey], null, left, right);

        Assert.HasCount(2, join.Children);
        Assert.HasCount(2, join.OutputSchema.Columns);
        Assert.AreEqual("Id", join.OutputSchema.Columns[0].Name);
        Assert.AreEqual("UserId", join.OutputSchema.Columns[1].Name);
        Assert.AreEqual(JoinKind.Inner, join.Kind);
    }

    [TestMethod]
    public void PhysicalNestedLoopJoin_WhenConstructed_ShouldMergeSchemas()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("Value", typeof(string)));
        var pred = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("a", "Id", typeof(int)), new ColumnRef("b", "Value", typeof(string)), typeof(bool));

        var join = new PhysicalNestedLoopJoinNode(JoinKind.LeftOuter, pred, left, right);

        Assert.HasCount(2, join.Children);
        Assert.HasCount(2, join.OutputSchema.Columns);
        Assert.AreEqual(JoinKind.LeftOuter, join.Kind);
    }

    [TestMethod]
    public void PhysicalNestedLoopApply_WhenConstructed_ShouldMergeSchemas()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("Child", typeof(string)));

        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, left, right);

        Assert.HasCount(2, apply.Children);
        Assert.HasCount(2, apply.OutputSchema.Columns);
        Assert.AreEqual(ApplyKind.Cross, apply.Kind);
    }

    #endregion

    #region Physical-only nodes

    [TestMethod]
    public void PhysicalTopN_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var keys = new[] { new OrderField(new ColumnRef("t", "Name", typeof(string)), false) };
        var topN = new PhysicalTopNNode(10, keys, scan);

        Assert.HasCount(1, topN.Children);
        Assert.AreEqual(10, topN.N);
        Assert.HasCount(1, topN.Keys);
        Assert.AreSame(scan.OutputSchema, topN.OutputSchema);
    }

    [TestMethod]
    public void PhysicalTopOffset_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var keys = new[] { new OrderField(new ColumnRef("t", "Name", typeof(string)), false) };
        var topOffset = new PhysicalTopOffsetNode(2, 10, keys, scan);

        Assert.HasCount(1, topOffset.Children);
        Assert.AreEqual(2, topOffset.Skip);
        Assert.AreEqual(10, topOffset.Take);
        Assert.HasCount(1, topOffset.Keys);
        Assert.AreSame(scan.OutputSchema, topOffset.OutputSchema);
    }

    [TestMethod]
    public void PhysicalMaterialize_WhenConstructed_ShouldPreserveInputSchema()
    {
        var scan = CreateScan();
        var mat = new PhysicalMaterializeNode(scan);

        Assert.HasCount(1, mat.Children);
        Assert.AreSame(scan.OutputSchema, mat.OutputSchema);
    }

    [TestMethod]
    public void PhysicalWindow_WhenConstructed_ShouldExtendInputSchema()
    {
        var scan = CreateScan();
        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var reg = new WindowRegistration(method, method.Name, [], [], [], 0, typeof(int));
        var window = new PhysicalWindowNode([reg], scan);

        Assert.HasCount(1, window.Children);
        Assert.HasCount(3, window.OutputSchema.Columns);
        Assert.AreEqual("Id", window.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Name", window.OutputSchema.Columns[1].Name);
        Assert.AreEqual("__window_0", window.OutputSchema.Columns[2].Name);
    }

    #endregion

    #region Composite nodes

    [TestMethod]
    public void PhysicalSetOperation_WhenConstructed_ShouldUseLeftSchema()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("Id", typeof(int)));

        var setOp = new PhysicalSetOperationNode(SetOpKind.UnionAll, left, right, [0], [typeof(int)]);

        Assert.HasCount(2, setOp.Children);
        Assert.HasCount(1, setOp.OutputSchema.Columns);
        Assert.AreEqual(SetOpKind.UnionAll, setOp.Kind);
    }

    [TestMethod]
    public void PhysicalCte_WhenConstructed_ShouldIncludeDefinitionsAndQuery()
    {
        var defPlan = CreateScan("inner");
        var queryPlan = new PhysicalCteRefNode("MyCte", "c", CreateSchema(("Id", typeof(int))));
        var cte = new PhysicalCteNode(
            [new PhysicalCteDefinition("MyCte", defPlan)],
            queryPlan);

        Assert.HasCount(2, cte.Children);
        Assert.HasCount(1, cte.Definitions);
        Assert.AreEqual("MyCte", cte.Definitions[0].Name);
        Assert.AreSame(queryPlan.OutputSchema, cte.OutputSchema);
    }

    [TestMethod]
    public void PhysicalMultiStatement_WhenConstructed_ShouldUseLastStatementSchema()
    {
        var first = CreateScan("a", ("Id", typeof(int)));
        var second = CreateScan("b", ("Name", typeof(string)));
        var multi = new PhysicalMultiStatementNode([first, second]);

        Assert.HasCount(2, multi.Children);
        Assert.AreSame(second.OutputSchema, multi.OutputSchema);
    }

    #endregion

    #region Printer

    [TestMethod]
    public void Printer_WhenSimpleProjectOverScan_ShouldProduceExpectedOutput()
    {
        var scan = CreateScan();
        var fields = new[] { new ProjectedField("Name", new ColumnRef("t", "Name", typeof(string)), 0) };
        var project = new PhysicalProjectNode(fields, scan);

        var output = PhysicalPlanPrinter.Print(project);

        Assert.AreEqual(
            "PhysicalProject [t.Name as Name]\r\n  PhysicalSchemaScan [#test.data() as t]",
            output);
    }

    [TestMethod]
    public void Printer_WhenFilterBetweenScanAndProject_ShouldProduceExpectedOutput()
    {
        var scan = CreateScan();
        var pred = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Id", typeof(int)), new Literal(5, typeof(int)), typeof(bool));
        var filter = new PhysicalFilterNode(pred, scan);
        var fields = new[] { new ProjectedField("Id", new ColumnRef("t", "Id", typeof(int)), 0) };
        var project = new PhysicalProjectNode(fields, filter);

        var output = PhysicalPlanPrinter.Print(project);

        Assert.AreEqual(
            "PhysicalProject [t.Id as Id]\r\n  PhysicalFilter [(t.Id > 5)]\r\n    PhysicalSchemaScan [#test.data() as t]",
            output);
    }

    [TestMethod]
    public void Printer_WhenHashJoin_ShouldShowStrategy()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("UserId", typeof(int)));
        var join = new PhysicalHashJoinNode(JoinKind.Inner, [new ColumnRef("a", "Id", typeof(int))], [new ColumnRef("b", "UserId", typeof(int))], null, left, right);

        var output = PhysicalPlanPrinter.Print(join);

        Assert.AreEqual(
            "PhysicalHashJoin [Inner] [build: a.Id] [probe: b.UserId]\r\n  PhysicalSchemaScan [#test.data() as a]\r\n  PhysicalSchemaScan [#test.data() as b]",
            output);
    }

    [TestMethod]
    public void Printer_WhenSingleKeyAggregate_ShouldShowKeyType()
    {
        var scan = CreateScan();
        var key = new ColumnRef("t", "Name", typeof(string));
        var binding = CreateAggBinding();
        var agg = new PhysicalSingleKeyAggregateNode(key, "Name", typeof(string), [binding], scan);

        var output = PhysicalPlanPrinter.Print(agg);

        Assert.AreEqual(
            "PhysicalSingleKeyAggregate [key: Name (String)] [aggs: Count]\r\n  PhysicalSchemaScan [#test.data() as t]",
            output);
    }

    [TestMethod]
    public void Printer_WhenMaterializeAndWindow_ShouldShowBoth()
    {
        var scan = CreateScan();
        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var reg = new WindowRegistration(method, method.Name, [], [], [], 0, typeof(int));
        var mat = new PhysicalMaterializeNode(scan);
        var window = new PhysicalWindowNode([reg], mat);

        var output = PhysicalPlanPrinter.Print(window);

        Assert.AreEqual(
            "PhysicalWindow [ToUpper(idx:0)]\r\n  PhysicalMaterialize\r\n    PhysicalSchemaScan [#test.data() as t]",
            output);
    }

    [TestMethod]
    public void Printer_WhenTopN_ShouldShowNAndKeys()
    {
        var scan = CreateScan();
        var keys = new[] { new OrderField(new ColumnRef("t", "Name", typeof(string)), false) };
        var topN = new PhysicalTopNNode(10, keys, scan);

        var output = PhysicalPlanPrinter.Print(topN);

        Assert.AreEqual(
            "PhysicalTopN [10] [t.Name]\r\n  PhysicalSchemaScan [#test.data() as t]",
            output);
    }

    [TestMethod]
    public void Printer_WhenCte_ShouldShowDefinitionsAndQuery()
    {
        var defScan = CreateScan("inner");
        var defFields = new[] { new ProjectedField("Id", new ColumnRef("inner", "Id", typeof(int)), 0) };
        var defProject = new PhysicalProjectNode(defFields, defScan);
        var queryRef = new PhysicalCteRefNode("MyCte", "c", CreateSchema(("Id", typeof(int))));
        var cte = new PhysicalCteNode([new PhysicalCteDefinition("MyCte", defProject)], queryRef);

        var output = PhysicalPlanPrinter.Print(cte);

        Assert.AreEqual(
            "PhysicalCte\r\n  Definition [MyCte]\r\n    PhysicalProject [inner.Id as Id]\r\n      PhysicalSchemaScan [#test.data() as inner]\r\n  Query\r\n    PhysicalCteRef [MyCte as c]",
            output);
    }

    #endregion
}
