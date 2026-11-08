using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalNodeTests
{
    #region Composite nodes (Step 2.5)

    [TestMethod]
    public void CteNode_WhenConstructed_ShouldUsQuerySchema()
    {
        var defPlan = CreateScan("d", ("X", typeof(int)));
        var queryPlan = CreateScan("q", ("Y", typeof(string)));

        var cte = new CteNode(
            [new CteDefinition("myCte", defPlan)],
            queryPlan);

        Assert.AreEqual(queryPlan.OutputSchema, cte.OutputSchema);
    }

    [TestMethod]
    public void CteNode_WhenConstructed_ShouldIncludeAllChildrenInOrder()
    {
        var def1 = CreateScan("d1", ("A", typeof(int)));
        var def2 = CreateScan("d2", ("B", typeof(int)));
        var query = CreateScan("q", ("C", typeof(int)));

        var cte = new CteNode(
            [new CteDefinition("cte1", def1), new CteDefinition("cte2", def2)],
            query);

        Assert.HasCount(3, cte.Children);
        Assert.AreEqual(def1, cte.Children[0]);
        Assert.AreEqual(def2, cte.Children[1]);
        Assert.AreEqual(query, cte.Children[2]);
    }

    [TestMethod]
    public void DescNode_WhenConstructed_ShouldHaveEmptyChildren()
    {
        var schema = CreateSchema(("ColumnName", typeof(string)), ("ColumnType", typeof(string)));
        var desc = new DescNode("test", "data", DescType.Table, "*", [], "desc-source", schema);

        Assert.IsEmpty(desc.Children);
        Assert.AreEqual("test", desc.SchemaName);
        Assert.AreEqual(DescType.Table, desc.Type);
        Assert.IsEmpty(desc.Arguments);
    }

    [TestMethod]
    public void MultiStatementNode_WhenConstructed_ShouldUseLastStatementSchema()
    {
        var stmt1 = CreateScan("a", ("X", typeof(int)));
        var stmt2 = CreateScan("b", ("Y", typeof(string)));

        var multi = new MultiStatementNode([stmt1, stmt2]);

        Assert.AreEqual(stmt2.OutputSchema, multi.OutputSchema);
        Assert.HasCount(2, multi.Children);
    }

    [TestMethod]
    public void MultiStatementNode_WhenEmpty_ShouldUseEmptySchema()
    {
        var multi = new MultiStatementNode([]);

        Assert.IsEmpty(multi.OutputSchema.Columns);
        Assert.IsEmpty(multi.Children);
    }

    #endregion
}
