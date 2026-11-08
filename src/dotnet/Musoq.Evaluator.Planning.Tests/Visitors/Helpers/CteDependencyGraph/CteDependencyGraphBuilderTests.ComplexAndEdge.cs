using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

public partial class CteDependencyGraphBuilderTests
{
    #region Complex Scenarios Tests

    [TestMethod]
    public void Build_SelfReferencingPattern_CteDependsOnItself_ShouldNotCauseInfiniteLoop()
    {
        // Arrange
        // Note: In practice, this would be a recursive CTE, but our implementation
        // tracks dependencies. The CTE references itself.
        // WITH cteA AS (SELECT * FROM cteA) SELECT * FROM cteA
        // This is logically invalid SQL, but the builder should handle it gracefully.
        var cteAValue = new InMemoryTableFromNode("cteA", "a", typeof(object)); // References itself
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert - Should complete without hanging
        Assert.AreEqual(1, graph.CteCount);
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.Contains("cteA", graph.GetCte("cteA").Dependencies); // Self-reference tracked
    }

    [TestMethod]
    public void Build_MutuallyDependentCtes_ShouldHandleGracefully()
    {
        // Arrange
        // WITH cteA AS (SELECT * FROM cteB), cteB AS (SELECT * FROM cteA) SELECT * FROM cteA
        // This is logically invalid SQL (circular dependency), but builder should handle it.
        var cteAValue = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert - Should complete without hanging
        Assert.AreEqual(2, graph.CteCount);
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsTrue(graph.GetCte("cteB").IsReachable);
        Assert.Contains("cteB", graph.GetCte("cteA").Dependencies);
        Assert.Contains("cteA", graph.GetCte("cteB").Dependencies);
    }

    [TestMethod]
    public void Build_WideDiamondWithManyParallelCtes_ShouldHaveCorrectLevels()
    {
        // Arrange
        // cteA (level 0)
        // cteB, cteC, cteD, cteE all depend on cteA (level 1 - 4 CTEs can run in parallel)
        // cteF depends on all of cteB, cteC, cteD, cteE (level 2)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteDValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteEValue = new InMemoryTableFromNode("cteA", "a", typeof(object));

        // cteF joins cteB, cteC, cteD, cteE
        var fromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCond1 = new BooleanNode(true);
        var joinBc = new JoinInMemoryWithSourceTableFromNode("cteB", fromC, joinCond1, JoinType.Inner, typeof(object));

        var fromD = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var joinCond2 = new BooleanNode(true);
        var joinFromBcd = new JoinFromNode(joinBc, fromD, joinCond2, JoinType.Inner, typeof(object));
        var joinBcd = new JoinNode(joinFromBcd, typeof(object));

        var fromE = new InMemoryTableFromNode("cteE", "e", typeof(object));
        var joinCond3 = new BooleanNode(true);
        var joinFromBcde = new JoinFromNode(joinBcd, fromE, joinCond3, JoinType.Inner, typeof(object));
        var cteFValue = new JoinNode(joinFromBcde, typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var cteE = new CteInnerExpressionNode(cteEValue, "cteE");
        var cteF = new CteInnerExpressionNode(cteFValue, "cteF");

        var outerQuery = new InMemoryTableFromNode("cteF", "f", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD, cteE, cteF], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(6, graph.CteCount);
        Assert.IsEmpty(graph.DeadCtes);
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteC").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteD").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteE").ExecutionLevel);
        Assert.AreEqual(2, graph.GetCte("cteF").ExecutionLevel);
        Assert.IsTrue(graph.CanParallelize);
    }

    [TestMethod]
    public void Build_LongChain_ShouldHaveCorrectExecutionLevels()
    {
        // Arrange - chain of 5 CTEs
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteDValue = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var cteEValue = new InMemoryTableFromNode("cteD", "d", typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var cteE = new CteInnerExpressionNode(cteEValue, "cteE");

        var outerQuery = new InMemoryTableFromNode("cteE", "e", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD, cteE], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(5, graph.CteCount);
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(2, graph.GetCte("cteC").ExecutionLevel);
        Assert.AreEqual(3, graph.GetCte("cteD").ExecutionLevel);
        Assert.AreEqual(4, graph.GetCte("cteE").ExecutionLevel);
        Assert.IsFalse(graph.CanParallelize); // No level has multiple CTEs
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void Build_OuterQueryWithNoCteReferences_AllCtesShouldBeDead()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new IntegerNode("42"); // No CTE references

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(2, graph.CteCount);
        Assert.HasCount(2, graph.DeadCtes);
        Assert.IsFalse(graph.GetCte("cteA").IsReachable);
        Assert.IsFalse(graph.GetCte("cteB").IsReachable);
        Assert.IsEmpty(graph.OuterQuery.Dependencies);
    }

    [TestMethod]
    public void Build_CteWithNoValue_ShouldHandleGracefully()
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
        Assert.IsNotNull(graph.GetCte("cteA").AstNode);
        Assert.AreSame(cteA, graph.GetCte("cteA").AstNode);
    }

    #endregion
}
