using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

public partial class CteDependencyGraphBuilderTests
{
    #region Execution Level Tests

    [TestMethod]
    public void Build_SingleCte_ShouldHaveExecutionLevel0()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
    }

    [TestMethod]
    public void Build_TwoIndependentCtes_BothShouldHaveExecutionLevel0()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCondition = new BooleanNode(true);
        var outerQuery =
            new JoinInMemoryWithSourceTableFromNode("cteA", outerFromB, joinCondition, JoinType.Inner, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("cteB").ExecutionLevel);
    }

    [TestMethod]
    public void Build_CteBDependsOnCteA_ShouldHaveDifferentExecutionLevels()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteB").ExecutionLevel);
    }

    [TestMethod]
    public void Build_ChainOfThreeCtes_ShouldHaveIncrementingExecutionLevels()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteB", "b", typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        var outerQuery = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(2, graph.GetCte("cteC").ExecutionLevel);
    }

    [TestMethod]
    public void Build_DiamondDependency_ShouldComputeMaxExecutionLevel()
    {
        // Arrange
        // cteA (level 0)
        // cteB depends on cteA (level 1)
        // cteC depends on cteA (level 1)
        // cteD depends on cteB and cteC (level 2)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));

        // cteD references both cteB and cteC via a join
        var cteDFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCondition = new BooleanNode(true);
        var cteDValue =
            new JoinInMemoryWithSourceTableFromNode("cteB", cteDFromC, joinCondition, JoinType.Inner, typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");

        var outerQuery = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteC").ExecutionLevel);
        Assert.AreEqual(2, graph.GetCte("cteD").ExecutionLevel);
    }

    #endregion

    #region Outer Query Tests

    [TestMethod]
    public void Build_OuterQuery_ShouldHaveCorrectDependencies()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.Contains("cteA", graph.OuterQuery.Dependencies);
        Assert.IsTrue(graph.OuterQuery.IsOuterQuery);
    }

    [TestMethod]
    public void Build_OuterQueryWithMultipleDependencies_ShouldTrackAll()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new IntegerNode("3");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        // Outer references cteA, cteB, cteC via nested joins
        var joinFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCond1 = new BooleanNode(true);
        var joinAb =
            new JoinInMemoryWithSourceTableFromNode("cteA", joinFromB, joinCond1, JoinType.Inner, typeof(object));

        var joinFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCond2 = new BooleanNode(true);
        var joinFromOuter = new JoinFromNode(joinAb, joinFromC, joinCond2, JoinType.Inner, typeof(object));
        var outerQuery = new JoinNode(joinFromOuter, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.HasCount(3, graph.OuterQuery.Dependencies);
        Assert.Contains("cteA", graph.OuterQuery.Dependencies);
        Assert.Contains("cteB", graph.OuterQuery.Dependencies);
        Assert.Contains("cteC", graph.OuterQuery.Dependencies);
    }

    #endregion

    #region Apply Node Tests

    [TestMethod]
    public void Build_CteReferencedViaApply_ShouldBeMarkedAsReachable()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");

        // Outer query uses CROSS APPLY with CTE
        var sourceTable = new SchemaFromNode("schema", "method", new ArgsListNode([]), string.Empty, typeof(object), 0);
        var outerQuery = new ApplyInMemoryWithSourceTableFromNode("cteA", sourceTable, ApplyType.Cross, typeof(object));

        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.Contains("cteA", graph.OuterQuery.Dependencies);
    }

    [TestMethod]
    public void Build_CteReferencedViaOuterApply_ShouldBeMarkedAsReachable()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");

        var sourceTable = new SchemaFromNode("schema", "method", new ArgsListNode([]), string.Empty, typeof(object), 0);
        var outerQuery = new ApplyInMemoryWithSourceTableFromNode("cteA", sourceTable, ApplyType.Outer, typeof(object));

        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
    }

    #endregion
}
