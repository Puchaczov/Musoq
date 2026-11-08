using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalNodeTests
{
    #region Unary operator nodes (Step 2.3)

    [TestMethod]
    public void FilterNode_WhenConstructed_ShouldPassThroughSchema()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(10, typeof(int)),
            typeof(bool));
        var filter = new FilterNode(predicate, scan);

        Assert.AreEqual(scan.OutputSchema, filter.OutputSchema);
        Assert.HasCount(1, filter.Children);
        Assert.AreEqual(scan, filter.Children[0]);
    }

    [TestMethod]
    public void ProjectNode_WhenConstructed_ShouldDeriveSchemaFromFields()
    {
        var scan = CreateScan();
        var fields = new[]
        {
            new ProjectedField("Name", new ColumnRef("t", "Name", typeof(string)), 0),
            new ProjectedField("IdSquared", new Literal(0, typeof(int)), 1)
        };
        var project = new ProjectNode(fields, scan);

        Assert.HasCount(2, project.OutputSchema.Columns);
        Assert.AreEqual("Name", project.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(string), project.OutputSchema.Columns[0].Type);
        Assert.AreEqual("IdSquared", project.OutputSchema.Columns[1].Name);
        Assert.AreEqual(typeof(int), project.OutputSchema.Columns[1].Type);
        Assert.HasCount(1, project.Children);
    }

    [TestMethod]
    public void AggregateNode_WhenConstructed_ShouldDeriveSchemaFromKeysAndBindings()
    {
        var scan = CreateScan();
        var setMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
        var getMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var bindings = new[]
        {
            new AggregateBinding("Count", "Count", setMethod, [], getMethod, [], typeof(int))
        };

        var aggNode = new AggregateNode(
            [new ColumnRef("t", "Name", typeof(string))],
            ["Name"],
            [typeof(string)],
            bindings,
            scan);

        Assert.HasCount(2, aggNode.OutputSchema.Columns);
        Assert.AreEqual("Name", aggNode.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(string), aggNode.OutputSchema.Columns[0].Type);
        Assert.AreEqual("Count", aggNode.OutputSchema.Columns[1].Name);
        Assert.AreEqual(typeof(int), aggNode.OutputSchema.Columns[1].Type);
        Assert.HasCount(1, aggNode.Children);
    }

    [TestMethod]
    public void HavingFilterNode_WhenConstructed_ShouldPassThroughSchema()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(5, typeof(int)),
            typeof(bool));
        var having = new HavingFilterNode(predicate, scan);

        Assert.AreEqual(scan.OutputSchema, having.OutputSchema);
        Assert.HasCount(1, having.Children);
    }

    [TestMethod]
    public void SortNode_WhenConstructed_ShouldPassThroughSchema()
    {
        var scan = CreateScan();
        var keys = new[] { new OrderField(new ColumnRef("t", "Name", typeof(string)), false) };
        var sort = new SortNode(keys, scan);

        Assert.AreEqual(scan.OutputSchema, sort.OutputSchema);
        Assert.HasCount(1, sort.Children);
    }

    [TestMethod]
    public void SkipNode_WhenConstructed_ShouldPassThroughSchema()
    {
        var scan = CreateScan();
        var skip = new SkipNode(10, scan);

        Assert.AreEqual(scan.OutputSchema, skip.OutputSchema);
        Assert.AreEqual(10, skip.Count);
        Assert.HasCount(1, skip.Children);
    }

    [TestMethod]
    public void TakeNode_WhenConstructed_ShouldPassThroughSchema()
    {
        var scan = CreateScan();
        var take = new TakeNode(5, scan);

        Assert.AreEqual(scan.OutputSchema, take.OutputSchema);
        Assert.AreEqual(5, take.Count);
        Assert.HasCount(1, take.Children);
    }

    [TestMethod]
    public void WindowNode_WhenConstructed_ShouldExtendSchema()
    {
        var scan = CreateScan();
        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var registrations = new[]
        {
            new WindowRegistration(method, method.Name, [], [], [], 0, typeof(decimal))
        };
        var window = new WindowNode(registrations, scan);

        Assert.HasCount(3, window.OutputSchema.Columns);
        Assert.AreEqual("Id", window.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Name", window.OutputSchema.Columns[1].Name);
        Assert.AreEqual("__window_0", window.OutputSchema.Columns[2].Name);
        Assert.AreEqual(typeof(decimal), window.OutputSchema.Columns[2].Type);
        Assert.HasCount(1, window.Children);
    }

    [TestMethod]
    public void QualifyFilterNode_WhenConstructed_ShouldPassThroughSchema()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(BinaryOpKind.GreaterThan,
            new ColumnRef("t", "Id", typeof(int)),
            new Literal(1, typeof(int)),
            typeof(bool));
        var qualify = new QualifyFilterNode(predicate, scan);

        Assert.AreEqual(scan.OutputSchema, qualify.OutputSchema);
        Assert.HasCount(1, qualify.Children);
    }

    #endregion

    #region Binary operator nodes (Step 2.4)

    [TestMethod]
    public void JoinNode_WhenConstructed_ShouldMergeSchemas()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("Name", typeof(string)));
        var on = new BinaryOp(BinaryOpKind.Equal,
            new ColumnRef("a", "Id", typeof(int)),
            new ColumnRef("b", "Id", typeof(int)),
            typeof(bool));

        var join = new JoinNode(JoinKind.Inner, on, left, right);

        Assert.HasCount(2, join.OutputSchema.Columns);
        Assert.AreEqual("Id", join.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Name", join.OutputSchema.Columns[1].Name);
        Assert.HasCount(2, join.Children);
        Assert.AreEqual(left, join.Children[0]);
        Assert.AreEqual(right, join.Children[1]);
    }

    [TestMethod]
    public void JoinNode_WhenLeftOuter_ShouldPreserveKind()
    {
        var left = CreateScan("a");
        var right = CreateScan("b");
        var on = new Literal(true, typeof(bool));

        var join = new JoinNode(JoinKind.LeftOuter, on, left, right);

        Assert.AreEqual(JoinKind.LeftOuter, join.Kind);
    }

    [TestMethod]
    public void ApplyNode_WhenConstructed_ShouldMergeSchemas()
    {
        var left = CreateScan("a", ("Id", typeof(int)));
        var right = CreateScan("b", ("Value", typeof(string)));

        var apply = new ApplyNode(ApplyKind.Cross, left, right);

        Assert.HasCount(2, apply.OutputSchema.Columns);
        Assert.AreEqual("Id", apply.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Value", apply.OutputSchema.Columns[1].Name);
        Assert.HasCount(2, apply.Children);
    }

    [TestMethod]
    public void ApplyNode_WhenOuter_ShouldPreserveKind()
    {
        var left = CreateScan("a");
        var right = CreateScan("b");

        var apply = new ApplyNode(ApplyKind.Outer, left, right);

        Assert.AreEqual(ApplyKind.Outer, apply.Kind);
    }

    [TestMethod]
    public void SetOperationNode_WhenConstructed_ShouldUseLeftSchema()
    {
        var left = CreateScan("a", ("Id", typeof(int)), ("Name", typeof(string)));
        var right = CreateScan("b", ("Id", typeof(int)), ("Name", typeof(string)));

        var setOp = new SetOperationNode(SetOpKind.Union, left, right, []);

        Assert.AreEqual(left.OutputSchema, setOp.OutputSchema);
        Assert.HasCount(2, setOp.Children);
        Assert.AreEqual(left, setOp.Children[0]);
        Assert.AreEqual(right, setOp.Children[1]);
    }

    [TestMethod]
    public void SetOperationNode_WhenUnionAll_ShouldPreserveKind()
    {
        var left = CreateScan("a");
        var right = CreateScan("b");

        var setOp = new SetOperationNode(SetOpKind.UnionAll, left, right, []);

        Assert.AreEqual(SetOpKind.UnionAll, setOp.Kind);
    }

    #endregion
}
