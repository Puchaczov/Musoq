using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteDependencyGraphBuilderTests
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
        Assert.AreEqual(0, graph.DeadCtes.Count);
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
        Assert.AreEqual(1, graph.DeadCtes.Count);
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
        Assert.AreEqual(0, graph.DeadCtes.Count);
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
        Assert.AreEqual(1, graph.DeadCtes.Count);
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
        Assert.AreEqual(0, graph.DeadCtes.Count);
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
        Assert.AreEqual(0, nodeA.Dependencies.Count);
        // cteA is depended upon by cteB
        Assert.IsTrue(nodeA.Dependents.Contains("cteB"));

        // cteB depends on cteA
        Assert.IsTrue(nodeB.Dependencies.Contains("cteA"));
        // cteB is depended upon by outer query
        Assert.IsTrue(nodeB.Dependents.Contains(CteGraphNode.OuterQueryNodeName));
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
        Assert.AreEqual(0, graph.DeadCtes.Count);
    }

    #endregion

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
        Assert.IsTrue(graph.OuterQuery.Dependencies.Contains("cteA"));
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
        var joinAB =
            new JoinInMemoryWithSourceTableFromNode("cteA", joinFromB, joinCond1, JoinType.Inner, typeof(object));

        var joinFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCond2 = new BooleanNode(true);
        var joinFromOuter = new JoinFromNode(joinAB, joinFromC, joinCond2, JoinType.Inner, typeof(object));
        var outerQuery = new JoinNode(joinFromOuter, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(3, graph.OuterQuery.Dependencies.Count);
        Assert.IsTrue(graph.OuterQuery.Dependencies.Contains("cteA"));
        Assert.IsTrue(graph.OuterQuery.Dependencies.Contains("cteB"));
        Assert.IsTrue(graph.OuterQuery.Dependencies.Contains("cteC"));
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
        var sourceTable = new SchemaFromNode("schema", "method", new ArgsListNode([]), null, typeof(object), 0);
        var outerQuery = new ApplyInMemoryWithSourceTableFromNode("cteA", sourceTable, ApplyType.Cross, typeof(object));

        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
        Assert.IsTrue(graph.OuterQuery.Dependencies.Contains("cteA"));
    }

    [TestMethod]
    public void Build_CteReferencedViaOuterApply_ShouldBeMarkedAsReachable()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");

        var sourceTable = new SchemaFromNode("schema", "method", new ArgsListNode([]), null, typeof(object), 0);
        var outerQuery = new ApplyInMemoryWithSourceTableFromNode("cteA", sourceTable, ApplyType.Outer, typeof(object));

        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.IsTrue(graph.GetCte("cteA").IsReachable);
    }

    #endregion

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
        Assert.IsTrue(graph.GetCte("cteA").Dependencies.Contains("cteA")); // Self-reference tracked
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
        Assert.IsTrue(graph.GetCte("cteA").Dependencies.Contains("cteB"));
        Assert.IsTrue(graph.GetCte("cteB").Dependencies.Contains("cteA"));
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
        var joinBC = new JoinInMemoryWithSourceTableFromNode("cteB", fromC, joinCond1, JoinType.Inner, typeof(object));

        var fromD = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var joinCond2 = new BooleanNode(true);
        var joinFromBCD = new JoinFromNode(joinBC, fromD, joinCond2, JoinType.Inner, typeof(object));
        var joinBCD = new JoinNode(joinFromBCD, typeof(object));

        var fromE = new InMemoryTableFromNode("cteE", "e", typeof(object));
        var joinCond3 = new BooleanNode(true);
        var joinFromBCDE = new JoinFromNode(joinBCD, fromE, joinCond3, JoinType.Inner, typeof(object));
        var cteFValue = new JoinNode(joinFromBCDE, typeof(object));

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
        Assert.AreEqual(0, graph.DeadCtes.Count);
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
        Assert.AreEqual(2, graph.DeadCtes.Count);
        Assert.IsFalse(graph.GetCte("cteA").IsReachable);
        Assert.IsFalse(graph.GetCte("cteB").IsReachable);
        Assert.AreEqual(0, graph.OuterQuery.Dependencies.Count);
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

    #region Workflow Pattern Tests (Parallelization Scenarios)

    [TestMethod]
    public void Build_MultipleIndependentBranches_ShouldComputeCorrectLevels()
    {
        // Arrange - Workflow pattern:
        // a, b independent; c depends on a, b
        // d, e independent; f depends on d, e  
        // g depends on c, f
        // Expected: Level 0: [a, b, d, e], Level 1: [c, f], Level 2: [g]
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteDValue = new IntegerNode("3");
        var cteEValue = new IntegerNode("4");

        // c depends on a and b
        var cteCFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteCJoinCond = new BooleanNode(true);
        var cteCValue =
            new JoinInMemoryWithSourceTableFromNode("cteA", cteCFromB, cteCJoinCond, JoinType.Inner, typeof(object));

        // f depends on d and e
        var cteFFromE = new InMemoryTableFromNode("cteE", "e", typeof(object));
        var cteFJoinCond = new BooleanNode(true);
        var cteFValue =
            new JoinInMemoryWithSourceTableFromNode("cteD", cteFFromE, cteFJoinCond, JoinType.Inner, typeof(object));

        // g depends on c and f
        var cteGFromF = new InMemoryTableFromNode("cteF", "f", typeof(object));
        var cteGJoinCond = new BooleanNode(true);
        var cteGValue =
            new JoinInMemoryWithSourceTableFromNode("cteC", cteGFromF, cteGJoinCond, JoinType.Inner, typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var cteE = new CteInnerExpressionNode(cteEValue, "cteE");
        var cteF = new CteInnerExpressionNode(cteFValue, "cteF");
        var cteG = new CteInnerExpressionNode(cteGValue, "cteG");

        var outerQuery = new InMemoryTableFromNode("cteG", "g", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD, cteE, cteF, cteG], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert - Check levels
        Assert.AreEqual(7, graph.CteCount);
        Assert.AreEqual(0, graph.DeadCtes.Count);

        // Level 0: a, b, d, e (all independent)
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("cteD").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("cteE").ExecutionLevel);

        // Level 1: c, f (depend on level 0)
        Assert.AreEqual(1, graph.GetCte("cteC").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteF").ExecutionLevel);

        // Level 2: g (depends on level 1)
        Assert.AreEqual(2, graph.GetCte("cteG").ExecutionLevel);

        // Verify parallelization is possible
        Assert.IsTrue(graph.CanParallelize);
        Assert.AreEqual(3, graph.ExecutionLevels.Count);
        Assert.AreEqual(4, graph.ExecutionLevels[0].Count); // Level 0 has 4 CTEs
        Assert.AreEqual(2, graph.ExecutionLevels[1].Count); // Level 1 has 2 CTEs
        Assert.AreEqual(1, graph.ExecutionLevels[2].Count); // Level 2 has 1 CTE
    }

    [TestMethod]
    public void Build_DoubleDiamond_ShouldHaveAlternatingParallelLevels()
    {
        // Arrange - Double diamond pattern:
        // a → [b, c] → d → [e, f] → g
        // Level 0: [a]
        // Level 1: [b, c] (parallel)
        // Level 2: [d]
        // Level 3: [e, f] (parallel)
        // Level 4: [g]
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));

        // d depends on b and c
        var cteDFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var cteDJoinCond = new BooleanNode(true);
        var cteDValue =
            new JoinInMemoryWithSourceTableFromNode("cteB", cteDFromC, cteDJoinCond, JoinType.Inner, typeof(object));

        var cteEValue = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var cteFValue = new InMemoryTableFromNode("cteD", "d", typeof(object));

        // g depends on e and f
        var cteGFromF = new InMemoryTableFromNode("cteF", "f", typeof(object));
        var cteGJoinCond = new BooleanNode(true);
        var cteGValue =
            new JoinInMemoryWithSourceTableFromNode("cteE", cteGFromF, cteGJoinCond, JoinType.Inner, typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var cteE = new CteInnerExpressionNode(cteEValue, "cteE");
        var cteF = new CteInnerExpressionNode(cteFValue, "cteF");
        var cteG = new CteInnerExpressionNode(cteGValue, "cteG");

        var outerQuery = new InMemoryTableFromNode("cteG", "g", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD, cteE, cteF, cteG], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(7, graph.CteCount);
        Assert.AreEqual(0, graph.DeadCtes.Count);

        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("cteC").ExecutionLevel);
        Assert.AreEqual(2, graph.GetCte("cteD").ExecutionLevel);
        Assert.AreEqual(3, graph.GetCte("cteE").ExecutionLevel);
        Assert.AreEqual(3, graph.GetCte("cteF").ExecutionLevel);
        Assert.AreEqual(4, graph.GetCte("cteG").ExecutionLevel);

        Assert.IsTrue(graph.CanParallelize);
        Assert.AreEqual(5, graph.ExecutionLevels.Count);
    }

    [TestMethod]
    public void Build_TransitiveDeadCtes_ShouldMarkAllAsUnreachable()
    {
        // Arrange - Chain of dead CTEs:
        // dead1 → dead2 → dead3 (all unreachable)
        // live (referenced by outer)
        var liveValue = new IntegerNode("1");
        var dead1Value = new IntegerNode("2");
        var dead2Value = new InMemoryTableFromNode("dead1", "d1", typeof(object));
        var dead3Value = new InMemoryTableFromNode("dead2", "d2", typeof(object));

        var live = new CteInnerExpressionNode(liveValue, "live");
        var dead1 = new CteInnerExpressionNode(dead1Value, "dead1");
        var dead2 = new CteInnerExpressionNode(dead2Value, "dead2");
        var dead3 = new CteInnerExpressionNode(dead3Value, "dead3");

        var outerQuery = new InMemoryTableFromNode("live", "l", typeof(object));
        var cteExpression = new CteExpressionNode([live, dead1, dead2, dead3], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(4, graph.CteCount);
        Assert.AreEqual(3, graph.DeadCtes.Count);

        Assert.IsTrue(graph.GetCte("live").IsReachable);
        Assert.IsFalse(graph.GetCte("dead1").IsReachable);
        Assert.IsFalse(graph.GetCte("dead2").IsReachable);
        Assert.IsFalse(graph.GetCte("dead3").IsReachable);

        // Only 1 reachable CTE, so no parallelization
        Assert.IsFalse(graph.CanParallelize);
        Assert.AreEqual(1, graph.ExecutionLevels.Count);
    }

    [TestMethod]
    public void Build_VeryWideTwentyCtes_ShouldAllBeAtLevel0()
    {
        // Arrange - 20 independent CTEs all at level 0
        var ctes = new List<CteInnerExpressionNode>();
        for (var i = 1; i <= 20; i++)
        {
            var value = new IntegerNode(i.ToString());
            ctes.Add(new CteInnerExpressionNode(value, $"cte{i}"));
        }

        // Outer query references all 20 via nested joins
        // Start with cte1, then join each subsequent CTE
        Node outerQuery = new InMemoryTableFromNode("cte1", "c1", typeof(object));
        for (var i = 2; i <= 20; i++)
        {
            var nextFrom = new InMemoryTableFromNode($"cte{i}", $"c{i}", typeof(object));
            var joinCond = new BooleanNode(true);
            var joinFrom = new JoinFromNode((FromNode)outerQuery, nextFrom, joinCond, JoinType.Inner, typeof(object));
            outerQuery = new JoinNode(joinFrom, typeof(object));
        }

        var cteExpression = new CteExpressionNode(ctes.ToArray(), outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(20, graph.CteCount);
        Assert.AreEqual(0, graph.DeadCtes.Count);

        // All 20 should be at level 0
        for (var i = 1; i <= 20; i++)
        {
            Assert.AreEqual(0, graph.GetCte($"cte{i}").ExecutionLevel, $"cte{i} should be at level 0");
            Assert.IsTrue(graph.GetCte($"cte{i}").IsReachable, $"cte{i} should be reachable");
        }

        Assert.IsTrue(graph.CanParallelize);
        Assert.AreEqual(1, graph.ExecutionLevels.Count);
        Assert.AreEqual(20, graph.ExecutionLevels[0].Count);
    }

    [TestMethod]
    public void Build_IndependentBranchWithJoin_ShouldParallelizeCorrectly()
    {
        // Arrange - Your example:
        // a, b independent; c depends on a, b; d independent
        // Expected: Level 0: [a, b, d], Level 1: [c]
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteDValue = new IntegerNode("3"); // Independent!

        // c depends on a and b
        var cteCFromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteCJoinCond = new BooleanNode(true);
        var cteCValue =
            new JoinInMemoryWithSourceTableFromNode("cteA", cteCFromB, cteCJoinCond, JoinType.Inner, typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");

        // Outer query: c JOIN d
        var outerFromD = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var outerJoinCond = new BooleanNode(true);
        var outerQuery =
            new JoinInMemoryWithSourceTableFromNode("cteC", outerFromD, outerJoinCond, JoinType.Inner, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(4, graph.CteCount);
        Assert.AreEqual(0, graph.DeadCtes.Count);

        // Level 0: a, b, d (all independent - d can run parallel with a and b!)
        Assert.AreEqual(0, graph.GetCte("cteA").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("cteB").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("cteD").ExecutionLevel);

        // Level 1: c (depends on a and b)
        Assert.AreEqual(1, graph.GetCte("cteC").ExecutionLevel);

        Assert.IsTrue(graph.CanParallelize);
        Assert.AreEqual(2, graph.ExecutionLevels.Count);
        Assert.AreEqual(3, graph.ExecutionLevels[0].Count); // a, b, d at level 0
        Assert.AreEqual(1, graph.ExecutionLevels[1].Count); // c at level 1
    }

    [TestMethod]
    public void Build_EtlPipelinePattern_ShouldHaveCorrectParallelLevels()
    {
        // Arrange - ETL pipeline with 8 CTEs across 4 levels:
        // Level 0: raw_orders, raw_customers, raw_products (3 parallel)
        // Level 1: clean_orders (depends on raw_orders), clean_customers (depends on raw_customers), clean_products (depends on raw_products)
        // Level 2: enriched_orders (depends on clean_orders, clean_customers)
        // Level 3: order_summary (depends on enriched_orders, clean_products)

        // Level 0 - raw extracts
        var rawOrdersValue = new IntegerNode("1");
        var rawCustomersValue = new IntegerNode("2");
        var rawProductsValue = new IntegerNode("3");

        // Level 1 - cleaning (each depends on its raw)
        var cleanOrdersValue = new InMemoryTableFromNode("raw_orders", "ro", typeof(object));
        var cleanCustomersValue = new InMemoryTableFromNode("raw_customers", "rc", typeof(object));
        var cleanProductsValue = new InMemoryTableFromNode("raw_products", "rp", typeof(object));

        // Level 2 - enriched_orders depends on clean_orders and clean_customers
        var enrichedFromCustomers = new InMemoryTableFromNode("clean_customers", "cc", typeof(object));
        var enrichedJoinCond = new BooleanNode(true);
        var enrichedOrdersValue = new JoinInMemoryWithSourceTableFromNode("clean_orders", enrichedFromCustomers,
            enrichedJoinCond, JoinType.Inner, typeof(object));

        // Level 3 - order_summary depends on enriched_orders and clean_products
        var summaryFromProducts = new InMemoryTableFromNode("clean_products", "cp", typeof(object));
        var summaryJoinCond = new BooleanNode(true);
        var orderSummaryValue = new JoinInMemoryWithSourceTableFromNode("enriched_orders", summaryFromProducts,
            summaryJoinCond, JoinType.Inner, typeof(object));

        var rawOrders = new CteInnerExpressionNode(rawOrdersValue, "raw_orders");
        var rawCustomers = new CteInnerExpressionNode(rawCustomersValue, "raw_customers");
        var rawProducts = new CteInnerExpressionNode(rawProductsValue, "raw_products");
        var cleanOrders = new CteInnerExpressionNode(cleanOrdersValue, "clean_orders");
        var cleanCustomers = new CteInnerExpressionNode(cleanCustomersValue, "clean_customers");
        var cleanProducts = new CteInnerExpressionNode(cleanProductsValue, "clean_products");
        var enrichedOrders = new CteInnerExpressionNode(enrichedOrdersValue, "enriched_orders");
        var orderSummary = new CteInnerExpressionNode(orderSummaryValue, "order_summary");

        var outerQuery = new InMemoryTableFromNode("order_summary", "os", typeof(object));
        var cteExpression = new CteExpressionNode([
            rawOrders, rawCustomers, rawProducts,
            cleanOrders, cleanCustomers, cleanProducts,
            enrichedOrders, orderSummary
        ], outerQuery);

        var builder = new CteDependencyGraphBuilder();

        // Act
        var graph = builder.Build(cteExpression);

        // Assert
        Assert.AreEqual(8, graph.CteCount);
        Assert.AreEqual(0, graph.DeadCtes.Count);

        // Level 0: raw extracts (3 parallel)
        Assert.AreEqual(0, graph.GetCte("raw_orders").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("raw_customers").ExecutionLevel);
        Assert.AreEqual(0, graph.GetCte("raw_products").ExecutionLevel);

        // Level 1: clean transformations (3 parallel)
        Assert.AreEqual(1, graph.GetCte("clean_orders").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("clean_customers").ExecutionLevel);
        Assert.AreEqual(1, graph.GetCte("clean_products").ExecutionLevel);

        // Level 2: enriched_orders
        Assert.AreEqual(2, graph.GetCte("enriched_orders").ExecutionLevel);

        // Level 3: order_summary
        Assert.AreEqual(3, graph.GetCte("order_summary").ExecutionLevel);

        Assert.IsTrue(graph.CanParallelize);
        Assert.AreEqual(4, graph.ExecutionLevels.Count);
        Assert.AreEqual(3, graph.ExecutionLevels[0].Count); // 3 at level 0
        Assert.AreEqual(3, graph.ExecutionLevels[1].Count); // 3 at level 1
        Assert.AreEqual(1, graph.ExecutionLevels[2].Count); // 1 at level 2
        Assert.AreEqual(1, graph.ExecutionLevels[3].Count); // 1 at level 3
    }

    #endregion
}
