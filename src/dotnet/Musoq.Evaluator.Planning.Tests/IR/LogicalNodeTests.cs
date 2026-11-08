using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public partial class LogicalNodeTests
{
    private static OutputSchema CreateSchema(params (string Name, Type Type)[] cols)
    {
        var columns = new ColumnSchema[cols.Length];
        for (var i = 0; i < cols.Length; i++)
            columns[i] = new ColumnSchema(cols[i].Name, cols[i].Type, i);
        return new OutputSchema(columns);
    }

    private static SchemaScanNode CreateScan(string alias = "t", params (string Name, Type Type)[] cols)
    {
        if (cols.Length == 0)
            cols = [("Id", typeof(int)), ("Name", typeof(string))];

        return new SchemaScanNode("test", "data", [], alias, CreateSchema(cols));
    }

    #region Binding types (Step 2.1)

    [TestMethod]
    public void ProjectedField_WhenConstructed_ShouldStoreProperties()
    {
        var expr = new ColumnRef("t", "Name", typeof(string));
        var field = new ProjectedField("Name", expr, 0);

        Assert.AreEqual("Name", field.OutputName);
        Assert.AreEqual(expr, field.Expression);
        Assert.AreEqual(0, field.OutputIndex);
    }

    [TestMethod]
    public void AggregateBinding_WhenConstructed_ShouldStoreProperties()
    {
        var setMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
        var getMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var binding = new AggregateBinding("Count", "Count", setMethod, [], getMethod, [], typeof(int));

        Assert.AreEqual("Count", binding.Identifier);
        Assert.AreEqual(setMethod, binding.SetMethod);
        Assert.AreEqual(getMethod, binding.GetMethod);
        Assert.AreEqual(typeof(int), binding.ReturnType);
    }

    [TestMethod]
    public void OrderField_WhenConstructed_ShouldStoreProperties()
    {
        var expr = new ColumnRef("t", "Age", typeof(int));
        var field = new OrderField(expr, Descending: true);

        Assert.AreEqual(expr, field.Expression);
        Assert.IsTrue(field.Descending);
    }

    [TestMethod]
    public void WindowRegistration_WhenConstructed_ShouldStoreProperties()
    {
        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var reg = new WindowRegistration(method, method.Name, [], [], [], 0, typeof(int));

        Assert.AreEqual(method, reg.Function);
        Assert.AreEqual(0, reg.WindowIndex);
        Assert.AreEqual(typeof(int), reg.ReturnType);
    }

    [TestMethod]
    public void ColumnSchema_WhenConstructed_ShouldStoreProperties()
    {
        var col = new ColumnSchema("Id", typeof(int), 0);

        Assert.AreEqual("Id", col.Name);
        Assert.AreEqual(typeof(int), col.Type);
        Assert.AreEqual(0, col.Index);
    }

    [TestMethod]
    public void OutputSchema_WhenFindByName_ShouldReturnColumn()
    {
        var schema = CreateSchema(("Id", typeof(int)), ("Name", typeof(string)));

        var found = schema.FindByName("Name");

        Assert.IsNotNull(found);
        Assert.AreEqual("Name", found.Name);
        Assert.AreEqual(typeof(string), found.Type);
        Assert.AreEqual(1, found.Index);
    }

    [TestMethod]
    public void OutputSchema_WhenFindByName_ShouldBeCaseInsensitive()
    {
        var schema = CreateSchema(("Name", typeof(string)));

        Assert.IsNotNull(schema.FindByName("name"));
        Assert.IsNotNull(schema.FindByName("NAME"));
    }

    [TestMethod]
    public void OutputSchema_WhenFindByNameMissing_ShouldReturnNull()
    {
        var schema = CreateSchema(("Id", typeof(int)));

        Assert.IsNull(schema.FindByName("Missing"));
    }

    [TestMethod]
    public void OutputSchema_WhenMerged_ShouldCombineColumns()
    {
        var left = CreateSchema(("Id", typeof(int)));
        var right = CreateSchema(("Name", typeof(string)));

        var merged = left.Merge(right);

        Assert.HasCount(2, merged.Columns);
        Assert.AreEqual("Id", merged.Columns[0].Name);
        Assert.AreEqual(0, merged.Columns[0].Index);
        Assert.AreEqual("Name", merged.Columns[1].Name);
        Assert.AreEqual(1, merged.Columns[1].Index);
    }

    [TestMethod]
    public void OutputSchema_WhenEmpty_ShouldHaveNoColumns()
    {
        Assert.IsEmpty(OutputSchema.Empty.Columns);
    }

    [TestMethod]
    public void ProjectedField_WhenEqual_ShouldBeEqual()
    {
        var expr = new Literal(42, typeof(int));
        var a = new ProjectedField("X", expr, 0);
        var b = new ProjectedField("X", expr, 0);

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void OrderField_WhenEqual_ShouldBeEqual()
    {
        var expr = new ColumnRef("t", "Age", typeof(int));
        var a = new OrderField(expr, false);
        var b = new OrderField(expr, false);

        Assert.AreEqual(a, b);
    }

    #endregion

    #region Leaf nodes (Step 2.2)

    [TestMethod]
    public void SchemaScanNode_WhenConstructed_ShouldHaveEmptyChildren()
    {
        var scan = CreateScan();

        Assert.IsEmpty(scan.Children);
    }

    [TestMethod]
    public void SchemaScanNode_WhenConstructed_ShouldPreserveSchema()
    {
        var scan = CreateScan();

        Assert.HasCount(2, scan.OutputSchema.Columns);
        Assert.AreEqual("Id", scan.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Name", scan.OutputSchema.Columns[1].Name);
    }

    [TestMethod]
    public void SchemaScanNode_WhenConstructed_ShouldPreserveProperties()
    {
        var scan = CreateScan("a");

        Assert.AreEqual("test", scan.SchemaName);
        Assert.AreEqual("data", scan.MethodName);
        Assert.AreEqual("a", scan.Alias);
    }

    [TestMethod]
    public void InterpretSourceNode_WhenConstructed_ShouldHaveEmptyChildren()
    {
        var schema = CreateSchema(("Col1", typeof(string)));
        var node = new InterpretSourceNode(
            "csv",
            InterpretSourceKind.Parse,
            [],
            "c",
            typeof(object),
            ApplyKind.Cross,
            schema);

        Assert.IsEmpty(node.Children);
        Assert.AreEqual("csv", node.SchemaName);
        Assert.AreEqual(InterpretSourceKind.Parse, node.Kind);
        Assert.AreEqual("c", node.Alias);
    }

    [TestMethod]
    public void CteRefNode_WhenConstructed_ShouldHaveEmptyChildren()
    {
        var schema = CreateSchema(("X", typeof(int)));
        var node = new CteRefNode("myCte", "c", schema);

        Assert.IsEmpty(node.Children);
        Assert.AreEqual("myCte", node.CteName);
        Assert.AreEqual("c", node.Alias);
    }

    #endregion
}
