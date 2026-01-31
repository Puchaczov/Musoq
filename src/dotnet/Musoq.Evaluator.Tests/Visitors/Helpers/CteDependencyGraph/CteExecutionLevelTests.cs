using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteExecutionLevelTests
{
    [TestMethod]
    public void CteExecutionLevel_WhenCreated_ShouldHaveCorrectLevel()
    {
        // Arrange
        var node = new CteGraphNode("cteA", null);
        var ctes = new List<CteGraphNode> { node };

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.AreEqual(0, level.Level);
    }

    [TestMethod]
    public void CteExecutionLevel_WhenCreated_ShouldContainCtes()
    {
        // Arrange
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var ctes = new List<CteGraphNode> { node1, node2 };

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.AreEqual(2, level.Ctes.Count);
        Assert.AreEqual("cteA", level.Ctes[0].Name);
        Assert.AreEqual("cteB", level.Ctes[1].Name);
    }

    [TestMethod]
    public void CteExecutionLevel_Count_ShouldMatchCteCount()
    {
        // Arrange
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var node3 = new CteGraphNode("cteC", null);
        var ctes = new List<CteGraphNode> { node1, node2, node3 };

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.AreEqual(3, level.Count);
    }

    [TestMethod]
    public void CteExecutionLevel_SingleCte_CannotParallelize()
    {
        // Arrange
        var node = new CteGraphNode("cteA", null);
        var ctes = new List<CteGraphNode> { node };

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.IsFalse(level.CanParallelize);
    }

    [TestMethod]
    public void CteExecutionLevel_MultipleCtes_CanParallelize()
    {
        // Arrange
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var ctes = new List<CteGraphNode> { node1, node2 };

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.IsTrue(level.CanParallelize);
    }

    [TestMethod]
    public void CteExecutionLevel_ToString_ShouldIncludeRelevantInfo()
    {
        // Arrange
        var node1 = new CteGraphNode("cteA", null);
        var node2 = new CteGraphNode("cteB", null);
        var ctes = new List<CteGraphNode> { node1, node2 };
        var level = new CteExecutionLevel(1, ctes);

        // Act
        var result = level.ToString();

        // Assert
        Assert.IsTrue(result.Contains("Level 1"));
        Assert.IsTrue(result.Contains("cteA"));
        Assert.IsTrue(result.Contains("cteB"));
        Assert.IsTrue(result.Contains("parallel=True"));
    }

    #region Edge Cases

    [TestMethod]
    public void CteExecutionLevel_EmptyCtesList_ShouldNotThrow()
    {
        // Arrange
        var ctes = new List<CteGraphNode>();

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.AreEqual(0, level.Count);
        Assert.IsFalse(level.CanParallelize);
    }

    [TestMethod]
    public void CteExecutionLevel_HighLevelNumber_ShouldWork()
    {
        // Arrange
        var node = new CteGraphNode("cteA", null);
        var ctes = new List<CteGraphNode> { node };

        // Act
        var level = new CteExecutionLevel(100, ctes);

        // Assert
        Assert.AreEqual(100, level.Level);
    }

    [TestMethod]
    public void CteExecutionLevel_ZeroLevel_ShouldWork()
    {
        // Arrange
        var node = new CteGraphNode("cteA", null);
        var ctes = new List<CteGraphNode> { node };

        // Act
        var level = new CteExecutionLevel(0, ctes);

        // Assert
        Assert.AreEqual(0, level.Level);
    }

    [TestMethod]
    public void CteExecutionLevel_ToString_SingleCte_ShouldShowParallelFalse()
    {
        // Arrange
        var node = new CteGraphNode("cteA", null);
        var ctes = new List<CteGraphNode> { node };
        var level = new CteExecutionLevel(0, ctes);

        // Act
        var result = level.ToString();

        // Assert
        Assert.IsTrue(result.Contains("parallel=False"));
    }

    [TestMethod]
    public void CteExecutionLevel_ManyCtes_ShouldAllBeAccessible()
    {
        // Arrange
        var nodes = new List<CteGraphNode>
        {
            new("cte1", null),
            new("cte2", null),
            new("cte3", null),
            new("cte4", null),
            new("cte5", null)
        };

        // Act
        var level = new CteExecutionLevel(0, nodes);

        // Assert
        Assert.AreEqual(5, level.Count);
        Assert.IsTrue(level.CanParallelize);
        Assert.AreEqual("cte1", level.Ctes[0].Name);
        Assert.AreEqual("cte5", level.Ctes[4].Name);
    }

    [TestMethod]
    public void CteExecutionLevel_ToString_EmptyList_ShouldNotThrow()
    {
        // Arrange
        var ctes = new List<CteGraphNode>();
        var level = new CteExecutionLevel(0, ctes);

        // Act
        var result = level.ToString();

        // Assert
        Assert.IsTrue(result.Contains("Level 0"));
        Assert.IsTrue(result.Contains("[]"));
    }

    #endregion
}
