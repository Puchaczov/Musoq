using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalPlanBuilderTests
{

    #region Expression conversion in context

    [TestMethod]
    public void WhenSelectWithLiteral_ShouldConvertToLiteralExpression()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(new IntegerNode("42", "i"), 0, "Value"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        Assert.AreEqual("Value", project.Fields[0].OutputName);
        Assert.IsInstanceOfType<Literal>(project.Fields[0].Expression);
    }

    [TestMethod]
    public void WhenSelectWithColumnRef_ShouldConvertToColumnRef()
    {
        var from = CreateSchemaFrom();
        var select = CreateSelect(Field(Column("Name", "t", typeof(string)), 0, "Name"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));

        var result = Build(root);

        var project = (ProjectNode)result;
        var colRef = (ColumnRef)project.Fields[0].Expression;
        Assert.AreEqual("t", colRef.Alias);
        Assert.AreEqual("Name", colRef.ColumnName);
    }

    #endregion

    #region Traverse visitor Result property

    [TestMethod]
    public void WhenNoNodeVisited_ResultShouldBeNull()
    {
        var builder = new LogicalPlanBuilder();
        var traverser = new LogicalPlanBuildTraverseVisitor(builder);

        Assert.IsNull(traverser.Result);
    }

    #endregion

    private sealed class TestInterpreterRow
    {
        public string Timestamp { get; init; } = string.Empty;

        public string Level { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public string SchemaName => "LogLine";
    }
}
