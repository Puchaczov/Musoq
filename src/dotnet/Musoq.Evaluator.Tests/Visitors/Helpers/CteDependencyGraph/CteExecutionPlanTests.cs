using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using CteDependencyGraphClass = Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph.CteDependencyGraph;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteExecutionPlanTests
{
    private static CteDependencyGraphClass CreateSimpleGraph()
    {
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();
        return builder.Build(cteExpression);
    }

    [TestMethod]
    public void CteExecutionPlan_WhenCreated_ShouldContainLevels()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node = new CteGraphNode("cteA", null);
        node.ExecutionLevel = 0;
        node.IsReachable = true;
        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(1, plan.LevelCount);
        Assert.IsNotNull(plan.Levels);
    }

    [TestMethod]
    public void CteExecutionPlan_Graph_ShouldBeAccessible()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var levels = new List<CteExecutionLevel>();

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreSame(graph, plan.Graph);
    }

    [TestMethod]
    public void CteExecutionPlan_TotalCteCount_ShouldSumAcrossLevels()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var node3 = new CteGraphNode("cteC", null);
        node1.ExecutionLevel = 0;
        node2.ExecutionLevel = 0;
        node3.ExecutionLevel = 1;

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1, node2 }),
            new(1, new List<CteGraphNode> { node3 })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(3, plan.TotalCteCount);
    }

    [TestMethod]
    public void CteExecutionPlan_MaxParallelism_ShouldReturnMaxCtesInAnyLevel()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var node3 = new CteGraphNode("cteC", null);
        var node4 = new CteGraphNode("cteD", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1, node2, node3 }), // 3 CTEs
            new(1, new List<CteGraphNode> { node4 }) // 1 CTE
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(3, plan.MaxParallelism);
    }

    [TestMethod]
    public void CteExecutionPlan_CanParallelize_WhenAnyLevelHasMultipleCtes_ShouldReturnTrue()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1, node2 })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.IsTrue(plan.CanParallelize);
    }

    [TestMethod]
    public void CteExecutionPlan_CanParallelize_WhenNoLevelHasMultipleCtes_ShouldReturnFalse()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1 }),
            new(1, new List<CteGraphNode> { node2 })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.IsFalse(plan.CanParallelize);
    }

    [TestMethod]
    public void CteExecutionPlan_IsEmpty_WhenNoLevels_ShouldReturnTrue()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var levels = new List<CteExecutionLevel>();

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.IsTrue(plan.IsEmpty);
    }

    [TestMethod]
    public void CteExecutionPlan_IsEmpty_WhenHasLevels_ShouldReturnFalse()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node = new CteGraphNode("cteA", null);
        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.IsFalse(plan.IsEmpty);
    }

    [TestMethod]
    public void CteExecutionPlan_MaxParallelism_WhenEmpty_ShouldReturnZero()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var levels = new List<CteExecutionLevel>();

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(0, plan.MaxParallelism);
    }

    [TestMethod]
    public void CteExecutionPlan_ToString_ShouldIncludeRelevantInfo()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1, node2 })
        };
        var plan = new CteExecutionPlan(levels, graph);

        // Act
        var result = plan.ToString();

        // Assert
        Assert.Contains("CteExecutionPlan", result);
        Assert.Contains("2 CTEs", result);
        Assert.Contains("1 levels", result);
        Assert.Contains("CanParallelize=True", result);
    }

    #region Edge Cases

    [TestMethod]
    public void CteExecutionPlan_SingleLevel_SingleCte_Properties()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node = new CteGraphNode("cteA", null);
        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(1, plan.LevelCount);
        Assert.AreEqual(1, plan.TotalCteCount);
        Assert.AreEqual(1, plan.MaxParallelism);
        Assert.IsFalse(plan.CanParallelize);
        Assert.IsFalse(plan.IsEmpty);
    }

    [TestMethod]
    public void CteExecutionPlan_MultipleLevels_Properties()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var node3 = new CteGraphNode("cteC", null);
        var node4 = new CteGraphNode("cteD", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1, node2 }),
            new(1, new List<CteGraphNode> { node3 }),
            new(2, new List<CteGraphNode> { node4 })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(3, plan.LevelCount);
        Assert.AreEqual(4, plan.TotalCteCount);
        Assert.AreEqual(2, plan.MaxParallelism); // Level 0 has 2 CTEs
        Assert.IsTrue(plan.CanParallelize);
    }

    [TestMethod]
    public void CteExecutionPlan_ToString_Empty_ShouldNotThrow()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var levels = new List<CteExecutionLevel>();
        var plan = new CteExecutionPlan(levels, graph);

        // Act
        var result = plan.ToString();

        // Assert
        Assert.Contains("CteExecutionPlan", result);
        Assert.Contains("0 CTEs", result);
        Assert.Contains("0 levels", result);
    }

    [TestMethod]
    public void CteExecutionPlan_ToString_MultipleLevels_ShouldListAll()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1 }),
            new(1, new List<CteGraphNode> { node2 })
        };
        var plan = new CteExecutionPlan(levels, graph);

        // Act
        var result = plan.ToString();

        // Assert
        Assert.Contains("Level 0", result);
        Assert.Contains("Level 1", result);
        Assert.Contains("cteA", result);
        Assert.Contains("cteB", result);
    }

    [TestMethod]
    public void CteExecutionPlan_EmptyLevels_AllProperties()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var levels = new List<CteExecutionLevel>();

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(0, plan.LevelCount);
        Assert.AreEqual(0, plan.TotalCteCount);
        Assert.AreEqual(0, plan.MaxParallelism);
        Assert.IsFalse(plan.CanParallelize);
        Assert.IsTrue(plan.IsEmpty);
    }

    [TestMethod]
    public void CteExecutionPlan_LevelsAreOrdered_ShouldPreserveOrder()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var node3 = new CteGraphNode("cteC", null);

        var levels = new List<CteExecutionLevel>
        {
            new(0, new List<CteGraphNode> { node1 }),
            new(1, new List<CteGraphNode> { node2 }),
            new(2, new List<CteGraphNode> { node3 })
        };

        // Act
        var plan = new CteExecutionPlan(levels, graph);

        // Assert
        Assert.AreEqual(0, plan.Levels[0].Level);
        Assert.AreEqual(1, plan.Levels[1].Level);
        Assert.AreEqual(2, plan.Levels[2].Level);
    }

    #endregion
}
