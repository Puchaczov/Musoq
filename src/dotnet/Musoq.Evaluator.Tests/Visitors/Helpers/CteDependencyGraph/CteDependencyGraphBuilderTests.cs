using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public partial class CteDependencyGraphBuilderTests
{
    #region Single CTE Tests

    [TestMethod]
    public void Build_SingleCteReferencedByOuterQuery_ShouldMarkAsReachable()
    {
        // Arrange
        // WITH cteA AS (SELECT 1) SELECT * FROM cteA
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(1, graph.CteCount);
        Assert.IsTrue(graph.ContainsCte("cteA"));
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsEmpty(graph.DeadCtes);
    }

    [TestMethod]
    public void Build_SingleCteNotReferencedByOuterQuery_ShouldMarkAsDead()
    {
        // Arrange
        // WITH cteA AS (SELECT 1) SELECT 1
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new IntegerNode("1"); // Doesn't reference cteA
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(1, graph.CteCount);
        Assert.IsTrue(graph.ContainsCte("cteA"));
        Assert.IsFalse(graph.GetCte("cteA").IsReachable);
        Assert.HasCount(1, graph.DeadCtes);
        Assert.AreEqual("cteA", graph.DeadCtes[0].Name);
    }

    #endregion

    #region Multiple Independent CTEs Tests

    [TestMethod]
    public void Build_TwoIndependentCtesReferencedByOuterQuery_ShouldMarkBothAsReachable()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), cteB AS (SELECT 2) SELECT * FROM cteA, cteB
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        // Outer query references both CTEs via a join
        var outerFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCondition = new BooleanNode(true);
        var outerQuery =
            new JoinInMemoryWithSourceTableFromNode("cteA", outerFromB, joinCondition, JoinType.Inner, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.CteCount);
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsTrue(graph.GetCte("cteB").IsReachable);
        Assert.IsEmpty(graph.DeadCtes);
    }

    [TestMethod]
    public void Build_TwoIndependentCtes_OnlyOneReferenced_ShouldMarkOneAsDead()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), cteB AS (SELECT 2) SELECT * FROM cteA
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.CteCount);
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsFalse(graph.GetCte("cteB").IsReachable);
        Assert.HasCount(1, graph.DeadCtes);
        Assert.AreEqual("cteB", graph.DeadCtes[0].Name);
    }

    #endregion

    #region Dependent CTEs Tests

    [TestMethod]
    public void Build_CteDependent_CteBDependsOnCteA_ShouldMarkBothAsReachable()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), cteB AS (SELECT * FROM cteA) SELECT * FROM cteB
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object)); // cteB references cteA

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object)); // Outer references cteB
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.CteCount);
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsTrue(graph.GetCte("cteB").IsReachable);
        Assert.IsEmpty(graph.DeadCtes);
    }

    [TestMethod]
    public void Build_CteDependent_CteBDependsOnCteA_ShouldHaveCorrectDependencies()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), cteB AS (SELECT * FROM cteA) SELECT * FROM cteB
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
        var nodeA = graph.GetCte("cteA");
        var nodeB = graph.GetCte("cteB");

        // cteA has no dependencies
        Assert.IsEmpty(nodeA.Dependencies);
        // cteA is depended upon by cteB
        Assert.Contains("cteB", nodeA.Dependents);

        // cteB depends on cteA
        Assert.Contains("cteA", nodeB.Dependencies);
        // cteB is depended upon by outer query
        Assert.Contains(CteGraphNode.OuterQueryNodeName, nodeB.Dependents);
    }

    [TestMethod]
    public void Build_ChainOfThreeCtes_ShouldTraverseReachabilityCorrectly()
    {
        // Arrange
        // WITH cteA AS (SELECT 1),
        //      cteB AS (SELECT * FROM cteA),
        //      cteC AS (SELECT * FROM cteB)
        // SELECT * FROM cteC
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
        Assert.AreEqual(3, graph.CteCount);
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsTrue(graph.GetCte("cteB").IsReachable);
        Assert.IsTrue(graph.GetCte("cteC").IsReachable);
        Assert.IsEmpty(graph.DeadCtes);
    }

    #endregion

}
