using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteGraphNodeTests
{
    [TestMethod]
    public void CteGraphNode_WhenCreated_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var cteValue = new IntegerNode("1");
        var cteInner = new CteInnerExpressionNode(cteValue, "myCte");
        var node = new CteGraphNode("myCte", cteInner);

        // Assert
        Assert.AreEqual("myCte", node.Name);
    }

    [TestMethod]
    public void CteGraphNode_WhenCreated_ShouldHaveAstNode()
    {
        // Arrange
        var cteValue = new IntegerNode("1");
        var cteInner = new CteInnerExpressionNode(cteValue, "myCte");

        // Act
        var node = new CteGraphNode("myCte", cteInner);

        // Assert
        Assert.IsNotNull(node.AstNode);
        Assert.AreSame(cteInner, node.AstNode);
    }

    [TestMethod]
    public void CteGraphNode_WhenCreated_ShouldHaveEmptyDependencies()
    {
        // Arrange & Act
        var node = new CteGraphNode("myCte", null);

        // Assert
        Assert.AreEqual(0, node.Dependencies.Count);
        Assert.IsFalse(node.HasDependencies);
    }

    [TestMethod]
    public void CteGraphNode_WhenCreated_ShouldHaveEmptyDependents()
    {
        // Arrange & Act
        var node = new CteGraphNode("myCte", null);

        // Assert
        Assert.AreEqual(0, node.Dependents.Count);
        Assert.IsFalse(node.HasDependents);
    }

    [TestMethod]
    public void CteGraphNode_WhenCreated_ShouldNotBeReachable()
    {
        // Arrange & Act
        var node = new CteGraphNode("myCte", null);

        // Assert
        Assert.IsFalse(node.IsReachable);
    }

    [TestMethod]
    public void CteGraphNode_WhenCreated_ShouldHaveUndefinedExecutionLevel()
    {
        // Arrange & Act
        var node = new CteGraphNode("myCte", null);

        // Assert
        Assert.AreEqual(-1, node.ExecutionLevel);
    }

    [TestMethod]
    public void CteGraphNode_WithOuterQueryName_ShouldBeOuterQuery()
    {
        // Arrange & Act
        var node = new CteGraphNode(CteGraphNode.OuterQueryNodeName, null);

        // Assert
        Assert.IsTrue(node.IsOuterQuery);
    }

    [TestMethod]
    public void CteGraphNode_WithRegularName_ShouldNotBeOuterQuery()
    {
        // Arrange & Act
        var node = new CteGraphNode("myCte", null);

        // Assert
        Assert.IsFalse(node.IsOuterQuery);
    }

    [TestMethod]
    public void CteGraphNode_Dependencies_WhenAdded_ShouldReflectInHasDependencies()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.Dependencies.Add("otherCte");

        // Assert
        Assert.IsTrue(node.HasDependencies);
        Assert.AreEqual(1, node.Dependencies.Count);
        Assert.IsTrue(node.Dependencies.Contains("otherCte"));
    }

    [TestMethod]
    public void CteGraphNode_Dependents_WhenAdded_ShouldReflectInHasDependents()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.Dependents.Add("consumerCte");

        // Assert
        Assert.IsTrue(node.HasDependents);
        Assert.AreEqual(1, node.Dependents.Count);
        Assert.IsTrue(node.Dependents.Contains("consumerCte"));
    }

    [TestMethod]
    public void CteGraphNode_IsReachable_CanBeSetToTrue()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.IsReachable = true;

        // Assert
        Assert.IsTrue(node.IsReachable);
    }

    [TestMethod]
    public void CteGraphNode_ExecutionLevel_CanBeSet()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.ExecutionLevel = 2;

        // Assert
        Assert.AreEqual(2, node.ExecutionLevel);
    }

    [TestMethod]
    public void CteGraphNode_ToString_ShouldIncludeRelevantInfo()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);
        node.IsReachable = true;
        node.ExecutionLevel = 1;
        node.Dependencies.Add("dep1");

        // Act
        var result = node.ToString();

        // Assert
        Assert.IsTrue(result.Contains("myCte"));
        Assert.IsTrue(result.Contains("reachable"));
        Assert.IsTrue(result.Contains("level 1"));
        Assert.IsTrue(result.Contains("dep1"));
    }

    [TestMethod]
    public void CteGraphNode_ToString_WhenDead_ShouldShowDead()
    {
        // Arrange
        var node = new CteGraphNode("deadCte", null);
        node.IsReachable = false;

        // Act
        var result = node.ToString();

        // Assert
        Assert.IsTrue(result.Contains("dead"));
    }

    [TestMethod]
    public void CteGraphNode_ToString_WhenLevelUnknown_ShouldShowLevelUnknown()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);
        // ExecutionLevel defaults to -1

        // Act
        var result = node.ToString();

        // Assert
        Assert.IsTrue(result.Contains("level unknown"));
    }

    #region Multiple Dependencies Tests

    [TestMethod]
    public void CteGraphNode_MultipleDependencies_ShouldBeTracked()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.Dependencies.Add("dep1");
        node.Dependencies.Add("dep2");
        node.Dependencies.Add("dep3");

        // Assert
        Assert.AreEqual(3, node.Dependencies.Count);
        Assert.IsTrue(node.HasDependencies);
        Assert.IsTrue(node.Dependencies.Contains("dep1"));
        Assert.IsTrue(node.Dependencies.Contains("dep2"));
        Assert.IsTrue(node.Dependencies.Contains("dep3"));
    }

    [TestMethod]
    public void CteGraphNode_MultipleDependents_ShouldBeTracked()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.Dependents.Add("consumer1");
        node.Dependents.Add("consumer2");
        node.Dependents.Add("consumer3");

        // Assert
        Assert.AreEqual(3, node.Dependents.Count);
        Assert.IsTrue(node.HasDependents);
        Assert.IsTrue(node.Dependents.Contains("consumer1"));
        Assert.IsTrue(node.Dependents.Contains("consumer2"));
        Assert.IsTrue(node.Dependents.Contains("consumer3"));
    }

    [TestMethod]
    public void CteGraphNode_DuplicateDependency_ShouldNotAdd()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.Dependencies.Add("dep1");
        node.Dependencies.Add("dep1"); // Duplicate

        // Assert
        Assert.AreEqual(1, node.Dependencies.Count);
    }

    [TestMethod]
    public void CteGraphNode_DuplicateDependent_ShouldNotAdd()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.Dependents.Add("consumer1");
        node.Dependents.Add("consumer1"); // Duplicate

        // Assert
        Assert.AreEqual(1, node.Dependents.Count);
    }

    #endregion

    #region AstNode Tests

    [TestMethod]
    public void CteGraphNode_WithNullAstNode_ShouldBeValid()
    {
        // Arrange & Act
        var node = new CteGraphNode("myCte", null);

        // Assert
        Assert.IsNull(node.AstNode);
        Assert.AreEqual("myCte", node.Name);
    }

    [TestMethod]
    public void CteGraphNode_OuterQuery_ShouldHaveNullAstNode()
    {
        // Arrange & Act
        var node = new CteGraphNode(CteGraphNode.OuterQueryNodeName, null);

        // Assert
        Assert.IsNull(node.AstNode);
        Assert.IsTrue(node.IsOuterQuery);
    }

    #endregion

    #region ExecutionLevel Edge Cases

    [TestMethod]
    public void CteGraphNode_ExecutionLevel_CanBeSetToZero()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.ExecutionLevel = 0;

        // Assert
        Assert.AreEqual(0, node.ExecutionLevel);
    }

    [TestMethod]
    public void CteGraphNode_ExecutionLevel_CanBeSetToHighValue()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);

        // Act
        node.ExecutionLevel = 100;

        // Assert
        Assert.AreEqual(100, node.ExecutionLevel);
    }

    #endregion

    #region ToString Edge Cases

    [TestMethod]
    public void CteGraphNode_ToString_WithMultipleDeps_ShouldListAll()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);
        node.IsReachable = true;
        node.ExecutionLevel = 0;
        node.Dependencies.Add("dep1");
        node.Dependencies.Add("dep2");

        // Act
        var result = node.ToString();

        // Assert
        Assert.IsTrue(result.Contains("myCte"));
        Assert.IsTrue(result.Contains("dep1"));
        Assert.IsTrue(result.Contains("dep2"));
    }

    [TestMethod]
    public void CteGraphNode_ToString_WithNoDeps_ShouldShowEmptyDeps()
    {
        // Arrange
        var node = new CteGraphNode("myCte", null);
        node.IsReachable = true;
        node.ExecutionLevel = 0;

        // Act
        var result = node.ToString();

        // Assert
        Assert.IsTrue(result.Contains("deps=[]"));
    }

    #endregion

    #region OuterQueryNodeName Constant Tests

    [TestMethod]
    public void CteGraphNode_OuterQueryNodeName_ShouldBeExpectedValue()
    {
        // Assert
        Assert.AreEqual("__OUTER__", CteGraphNode.OuterQueryNodeName);
    }

    [TestMethod]
    public void CteGraphNode_RegularNameMatchingOuterPattern_ShouldNotBeOuterQuery()
    {
        // Arrange - use a name that starts similar but isn't exact
        var node = new CteGraphNode("__OUTER", null);

        // Assert
        Assert.IsFalse(node.IsOuterQuery);
    }

    #endregion
}
