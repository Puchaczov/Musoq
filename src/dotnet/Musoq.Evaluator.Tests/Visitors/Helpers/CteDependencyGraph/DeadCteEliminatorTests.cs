using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class DeadCteEliminatorTests
{
    #region Order Preservation Tests

    [TestMethod]
    public void Eliminate_ShouldPreserveOriginalCteOrder()
    {
        // Arrange
        // WITH cteA AS (...), cteB AS (...), cteC AS (...) SELECT ... FROM cteA, cteC
        // cteB is dead, cteA and cteC should remain in original order
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new IntegerNode("3");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        var outerFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCondition = new BooleanNode(true);
        var outerQuery =
            new JoinInMemoryWithSourceTableFromNode("cteA", outerFromC, joinCondition, JoinType.Inner, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.AreEqual(1, result.EliminatedCount);

        var prunedCte = result.ResultNode as CteExpressionNode;
        Assert.IsNotNull(prunedCte);
        Assert.HasCount(2, prunedCte.InnerExpression);
        Assert.AreEqual("cteA", prunedCte.InnerExpression[0].Name); // First
        Assert.AreEqual("cteC", prunedCte.InnerExpression[1].Name); // Second (order preserved)
    }

    #endregion

    #region Graph Access Tests

    [TestMethod]
    public void Eliminate_ShouldReturnGraphForFurtherAnalysis()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsNotNull(result.Graph);
        Assert.AreEqual(1, result.Graph.CteCount);
        Assert.IsTrue(result.Graph.ContainsCte("cteA"));
    }

    #endregion

    #region No Dead CTEs Tests

    [TestMethod]
    public void Eliminate_SingleCteReferenced_ShouldNotEliminate()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsFalse(result.WereCTEsEliminated);
        Assert.IsFalse(result.AllCTEsEliminated);
        Assert.AreEqual(0, result.EliminatedCount);
        Assert.AreSame(cteExpression, result.ResultNode);
    }

    [TestMethod]
    public void Eliminate_AllCtesReferenced_ShouldNotEliminate()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsFalse(result.WereCTEsEliminated);
        Assert.AreEqual(0, result.EliminatedCount);
    }

    #endregion

    #region Single Dead CTE Tests

    [TestMethod]
    public void Eliminate_SingleCteNotReferenced_ShouldEliminateAll()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new IntegerNode("42"); // Does not reference cteA
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.IsTrue(result.AllCTEsEliminated);
        Assert.AreEqual(1, result.EliminatedCount);
        // Result should be just the outer query (no CTE wrapper)
        Assert.AreSame(outerQuery, result.ResultNode);
    }

    [TestMethod]
    public void Eliminate_TwoCtes_OneDead_ShouldEliminateOne()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Only references cteA
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.IsFalse(result.AllCTEsEliminated);
        Assert.AreEqual(1, result.EliminatedCount);

        // Result should be a CTE expression with only cteA
        var prunedCte = result.ResultNode as CteExpressionNode;
        Assert.IsNotNull(prunedCte);
        Assert.HasCount(1, prunedCte.InnerExpression);
        Assert.AreEqual("cteA", prunedCte.InnerExpression[0].Name);
    }

    #endregion

    #region Transitive Dependency Tests

    [TestMethod]
    public void Eliminate_TransitiveDependency_ShouldKeepAllReachable()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), cteB AS (SELECT * FROM cteA) SELECT * FROM cteB
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsFalse(result.WereCTEsEliminated);
        Assert.AreEqual(0, result.EliminatedCount);
    }

    [TestMethod]
    public void Eliminate_UnreferencedChain_ShouldEliminateAll()
    {
        // Arrange
        // WITH cteA AS (SELECT 1), cteB AS (SELECT * FROM cteA) SELECT 42
        // Neither cteA nor cteB is referenced by outer query
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new IntegerNode("42"); // Does not reference any CTE
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.IsTrue(result.AllCTEsEliminated);
        Assert.AreEqual(2, result.EliminatedCount);
        Assert.AreSame(outerQuery, result.ResultNode);
    }

    #endregion

    #region Analyze Tests

    [TestMethod]
    public void Analyze_ShouldReturnGraphWithoutModifying()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Only references cteA
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = DeadCteEliminator.Analyze(cteExpression);

        // Assert
        Assert.IsTrue(graph.HasDeadCtes);
        Assert.HasCount(1, graph.DeadCtes);
        Assert.AreEqual("cteB", graph.DeadCtes[0].Name);
    }

    [TestMethod]
    public void Analyze_NoDead_ShouldReturnGraphWithNoDeadCtes()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var graph = DeadCteEliminator.Analyze(cteExpression);

        // Assert
        Assert.IsFalse(graph.HasDeadCtes);
        Assert.IsEmpty(graph.DeadCtes);
    }

    [TestMethod]
    public void Analyze_AllDead_ShouldReturnGraphWithAllDead()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new IntegerNode("42"); // References nothing
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var graph = DeadCteEliminator.Analyze(cteExpression);

        // Assert
        Assert.IsTrue(graph.HasDeadCtes);
        Assert.HasCount(2, graph.DeadCtes);
    }

    #endregion

    #region Complex Elimination Scenarios

    [TestMethod]
    public void Eliminate_MultipleDeadCtes_ShouldEliminateAll()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new IntegerNode("3");
        var cteDValue = new IntegerNode("4");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Only references cteA
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.IsFalse(result.AllCTEsEliminated);
        Assert.AreEqual(3, result.EliminatedCount);

        var prunedCte = result.ResultNode as CteExpressionNode;
        Assert.IsNotNull(prunedCte);
        Assert.HasCount(1, prunedCte.InnerExpression);
        Assert.AreEqual("cteA", prunedCte.InnerExpression[0].Name);
    }

    [TestMethod]
    public void Eliminate_DeadChain_ShouldEliminateEntireChain()
    {
        // Arrange
        // Chain: cteA -> cteB -> cteC (all dead, not referenced by outer)
        // cteD is referenced by outer
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteDValue = new IntegerNode("4");

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");

        var outerQuery = new InMemoryTableFromNode("cteD", "d", typeof(object)); // Only references cteD
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.AreEqual(3, result.EliminatedCount);

        var prunedCte = result.ResultNode as CteExpressionNode;
        Assert.IsNotNull(prunedCte);
        Assert.HasCount(1, prunedCte.InnerExpression);
        Assert.AreEqual("cteD", prunedCte.InnerExpression[0].Name);
    }

    [TestMethod]
    public void Eliminate_PartialDiamondDead_ShouldPreserveReachableOnly()
    {
        // Arrange
        // cteA (reachable via cteB)
        // cteB (reachable, outer references it)
        // cteC depends on cteA but outer doesn't reference it (dead)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Also depends on cteA

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object)); // Only references cteB
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.AreEqual(1, result.EliminatedCount);

        var prunedCte = result.ResultNode as CteExpressionNode;
        Assert.IsNotNull(prunedCte);
        Assert.HasCount(2, prunedCte.InnerExpression);

        var names = prunedCte.InnerExpression.Select(c => c.Name).ToHashSet();
        Assert.Contains("cteA", names);
        Assert.Contains("cteB", names);
        Assert.DoesNotContain("cteC", names);
    }

    #endregion

    #region EliminationResult Tests

    [TestMethod]
    public void EliminationResult_Properties_ShouldBeCorrectlySet()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert all result properties
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.IsFalse(result.AllCTEsEliminated);
        Assert.AreEqual(1, result.EliminatedCount);
        Assert.IsNotNull(result.Graph);
        Assert.AreEqual(2, result.Graph.CteCount);
        Assert.IsInstanceOfType(result.ResultNode, typeof(CteExpressionNode));
    }

    [TestMethod]
    public void EliminationResult_AllEliminated_Properties_ShouldBeCorrectlySet()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new IntegerNode("42");
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsTrue(result.WereCTEsEliminated);
        Assert.IsTrue(result.AllCTEsEliminated);
        Assert.AreEqual(1, result.EliminatedCount);
        Assert.AreSame(outerQuery, result.ResultNode);
    }

    [TestMethod]
    public void EliminationResult_NoneEliminated_Properties_ShouldBeCorrectlySet()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var result = DeadCteEliminator.Eliminate(cteExpression);

        // Assert
        Assert.IsFalse(result.WereCTEsEliminated);
        Assert.IsFalse(result.AllCTEsEliminated);
        Assert.AreEqual(0, result.EliminatedCount);
        Assert.AreSame(cteExpression, result.ResultNode);
    }

    #endregion
}
