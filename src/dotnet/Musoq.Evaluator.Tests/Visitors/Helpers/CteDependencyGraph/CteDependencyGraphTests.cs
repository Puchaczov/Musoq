using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using CteDependencyGraphClass = Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph.CteDependencyGraph;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteDependencyGraphTests
{
    private static CteDependencyGraphClass BuildGraph(
        CteExpressionNode cteExpression)
    {
        var builder = new CteDependencyGraphBuilder();
        return builder.Build(cteExpression);
    }

    #region Outer Query Tests

    [TestMethod]
    public void OuterQuery_ShouldBeAccessible()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsNotNull(graph.OuterQuery);
        Assert.IsTrue(graph.OuterQuery.IsOuterQuery);
    }

    #endregion

    #region CteCount Tests

    [TestMethod]
    public void CteCount_ShouldReturnNumberOfCtes()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new IntegerNode("3");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        var outerFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCondition = new BooleanNode(true);
        var outerQuery =
            new JoinInMemoryWithSourceTableFromNode("cteA", outerFromB, joinCondition, JoinType.Inner, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(3, graph.CteCount);
    }

    #endregion

    #region Node Access Tests

    [TestMethod]
    public void Nodes_ShouldContainAllCtes()
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

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.Nodes.Count);
        Assert.IsTrue(graph.Nodes.ContainsKey("cteA"));
        Assert.IsTrue(graph.Nodes.ContainsKey("cteB"));
    }

    [TestMethod]
    public void ContainsCte_ExistingCte_ShouldReturnTrue()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsTrue(graph.ContainsCte("cteA"));
    }

    [TestMethod]
    public void ContainsCte_NonExistingCte_ShouldReturnFalse()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsFalse(graph.ContainsCte("cteB"));
    }

    [TestMethod]
    public void GetCte_ExistingCte_ShouldReturnNode()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);
        var node = graph.GetCte("cteA");

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("cteA", node.Name);
    }

    [TestMethod]
    public void GetCte_NonExistingCte_ShouldThrow()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => graph.GetCte("nonExistent"));
    }

    [TestMethod]
    public void TryGetCte_ExistingCte_ShouldReturnTrueAndNode()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);
        var found = graph.TryGetCte("cteA", out var node);

        // Assert
        Assert.IsTrue(found);
        Assert.IsNotNull(node);
        Assert.AreEqual("cteA", node.Name);
    }

    [TestMethod]
    public void TryGetCte_NonExistingCte_ShouldReturnFalse()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);
        var found = graph.TryGetCte("nonExistent", out var node);

        // Assert
        Assert.IsFalse(found);
        Assert.IsNull(node);
    }

    #endregion

    #region Dead CTE Tests

    [TestMethod]
    public void DeadCtes_WhenAllReachable_ShouldBeEmpty()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.DeadCtes.Count);
        Assert.IsFalse(graph.HasDeadCtes);
    }

    [TestMethod]
    public void DeadCtes_WhenSomeUnreachable_ShouldContainThose()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Only references cteA

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(1, graph.DeadCtes.Count);
        Assert.AreEqual("cteB", graph.DeadCtes[0].Name);
        Assert.IsTrue(graph.HasDeadCtes);
    }

    [TestMethod]
    public void ReachableCtes_ShouldOnlyIncludeReachable()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(1, graph.ReachableCtes.Count);
        Assert.AreEqual("cteA", graph.ReachableCtes[0].Name);
    }

    #endregion

    #region Execution Levels Tests

    [TestMethod]
    public void ExecutionLevels_SingleCte_ShouldHaveOneLevel()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(1, graph.ExecutionLevels.Count);
        Assert.AreEqual(1, graph.ExecutionLevels[0].Count);
    }

    [TestMethod]
    public void ExecutionLevels_MultipleLevels_ShouldBeOrdered()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.ExecutionLevels.Count);
        Assert.AreEqual("cteA", graph.ExecutionLevels[0][0].Name);
        Assert.AreEqual("cteB", graph.ExecutionLevels[1][0].Name);
    }

    [TestMethod]
    public void CanParallelize_WhenSameLevelHasMultipleCtes_ShouldReturnTrue()
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

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsTrue(graph.CanParallelize);
    }

    [TestMethod]
    public void CanParallelize_WhenNoLevelHasMultipleCtes_ShouldReturnFalse()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsFalse(graph.CanParallelize);
    }

    #endregion

    #region ToString Tests

    [TestMethod]
    public void ToString_ShouldIncludeRelevantInfo()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);
        var result = graph.ToString();

        // Assert
        Assert.IsTrue(result.Contains("CteDependencyGraph"));
        Assert.IsTrue(result.Contains("1 CTEs"));
        Assert.IsTrue(result.Contains("0 dead"));
    }

    [TestMethod]
    public void ToString_WithDeadCtes_ShouldShowDeadCount()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Only references cteA

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);
        var result = graph.ToString();

        // Assert
        Assert.IsTrue(result.Contains("1 dead"));
    }

    [TestMethod]
    public void ToString_WithCanParallelize_ShouldShowParallelizeStatus()
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

        // Act
        var graph = BuildGraph(cteExpression);
        var result = graph.ToString();

        // Assert
        Assert.IsTrue(result.Contains("CanParallelize=True"));
    }

    #endregion

    #region Complex Dependency Graph Tests

    [TestMethod]
    public void Build_ComplexDiamondWithExtraDeadCte_ShouldHandleCorrectly()
    {
        // Arrange
        // cteA (level 0, reachable)
        // cteB depends on cteA (level 1, reachable)
        // cteC depends on cteA (level 1, reachable)
        // cteD depends on cteB and cteC (level 2, reachable)
        // cteE is dead (not referenced)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));

        var cteDFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCondition = new BooleanNode(true);
        var cteDValue =
            new JoinInMemoryWithSourceTableFromNode("cteB", cteDFromC, joinCondition, JoinType.Inner, typeof(object));

        var cteEValue = new IntegerNode("5"); // Dead CTE

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var cteE = new CteInnerExpressionNode(cteEValue, "cteE");

        var outerQuery = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD, cteE], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(5, graph.CteCount);
        Assert.AreEqual(1, graph.DeadCtes.Count);
        Assert.AreEqual("cteE", graph.DeadCtes[0].Name);
        Assert.AreEqual(4, graph.ReachableCtes.Count);
        Assert.IsTrue(graph.CanParallelize); // cteB and cteC can run in parallel
    }

    [TestMethod]
    public void Build_AllDeadCtes_ShouldMarkAllAsDead()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new IntegerNode("42"); // Doesn't reference any CTE

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.DeadCtes.Count);
        Assert.AreEqual(0, graph.ReachableCtes.Count);
        Assert.IsTrue(graph.HasDeadCtes);
        Assert.IsFalse(graph.CanParallelize); // No reachable CTEs to parallelize
    }

    [TestMethod]
    public void Build_ChainWithMiddleReferenced_ShouldMarkAllAsReachable()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), 
        //      cteB AS (SELECT * FROM cteA), 
        //      cteC AS (SELECT * FROM cteB) 
        // SELECT * FROM cteB  -- References middle of chain
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteB", "b", typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object)); // References middle
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsTrue(graph.GetCte("cteA").IsReachable); // Reachable via cteB
        Assert.IsTrue(graph.GetCte("cteB").IsReachable); // Directly referenced
        Assert.IsFalse(graph.GetCte("cteC").IsReachable); // Not reachable (depends on B but not referenced)
        Assert.AreEqual(1, graph.DeadCtes.Count);
        Assert.AreEqual("cteC", graph.DeadCtes[0].Name);
    }

    [TestMethod]
    public void Build_MultipleChainsWithOneDead_ShouldTrackCorrectly()
    {
        // Arrange
        // Chain 1: cteA -> cteB (both reachable)
        // Chain 2: cteC -> cteD (both dead)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new IntegerNode("3");
        var cteDValue = new InMemoryTableFromNode("cteC", "c", typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object)); // Only references chain 1
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsTrue(graph.GetCte("cteB").IsReachable);
        Assert.IsFalse(graph.GetCte("cteC").IsReachable);
        Assert.IsFalse(graph.GetCte("cteD").IsReachable);
        Assert.AreEqual(2, graph.DeadCtes.Count);
    }

    #endregion

    #region Execution Levels Edge Cases

    [TestMethod]
    public void ExecutionLevels_OnlyDeadCtes_ShouldBeEmpty()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new IntegerNode("42"); // Doesn't reference any CTE

        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(0, graph.ExecutionLevels.Count);
    }

    [TestMethod]
    public void ExecutionLevels_ThreeIndependentCtes_ShouldAllBeLevel0()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new IntegerNode("3");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        // Outer references all three via nested joins
        var joinFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCond1 = new BooleanNode(true);
        var joinAB =
            new JoinInMemoryWithSourceTableFromNode("cteA", joinFromB, joinCond1, JoinType.Inner, typeof(object));

        var joinFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCond2 = new BooleanNode(true);
        var joinFromOuter = new JoinFromNode(joinAB, joinFromC, joinCond2, JoinType.Inner, typeof(object));
        var outerQuery = new JoinNode(joinFromOuter, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        // Act
        var graph = BuildGraph(cteExpression);

        // Assert
        Assert.AreEqual(1, graph.ExecutionLevels.Count);
        Assert.AreEqual(3, graph.ExecutionLevels[0].Count);
        Assert.IsTrue(graph.CanParallelize);
    }

    #endregion
}
