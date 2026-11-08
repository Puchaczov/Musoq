using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using ApplyNode = Musoq.Evaluator.IR.Logical.Nodes.ApplyNode;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalPlanBuilderTests
{
    [TestMethod]
    public void WhenInnerJoinReadsLeftRowProperty_ShouldApplyPerRowAndFilterMatches()
    {
        var left = CreateSchemaFrom("a");
        var right = new PropertyFromNode("b", "a", ["Values"]);
        var condition = new EqualityNode(Column("Id", "a"), Column("Value", "b"));
        var from = new JoinSourcesTableFromNode(left, right, condition, JoinType.Inner, typeof(object));
        var query = CreateQuery(CreateSelect(Field(Column("Value", "b"), 0, "Value")), from);

        var project = (ProjectNode)Build(new RootNode(new SingleSetNode(query)));

        Assert.IsInstanceOfType<FilterNode>(project.Input);
        var filter = (FilterNode)project.Input;
        Assert.IsInstanceOfType<BinaryOp>(filter.Predicate);
        var predicate = (BinaryOp)filter.Predicate;
        Assert.AreEqual(BinaryOpKind.Equal, predicate.Kind);
        Assert.AreEqual(new ColumnRef("a", "Id", typeof(int)), predicate.Left);
        Assert.AreEqual(new ColumnRef("b", "Value", typeof(int)), predicate.Right);
        Assert.IsInstanceOfType<ApplyNode>(filter.Input);
        var apply = (ApplyNode)filter.Input;
        Assert.AreEqual(ApplyKind.Cross, apply.Kind);
        Assert.IsInstanceOfType<SchemaScanNode>(apply.Left);
        Assert.IsInstanceOfType<PropertySourceNode>(apply.Right);
        Assert.AreEqual("a", ((PropertySourceNode)apply.Right).SourceAlias);
    }
}
