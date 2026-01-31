using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteParallelizationAnalyzerTests
{
    #region GetExecutionLevelNames Tests

    [TestMethod]
    public void GetExecutionLevelNames_ShouldReturnCteNamesGroupedByLevel()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");

        var outerFromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCondition = new BooleanNode(true);
        var outerQuery =
            new JoinInMemoryWithSourceTableFromNode("cteB", outerFromC, joinCondition, JoinType.Inner, typeof(object));

        var cteExpression = new CteExpressionNode([cteA, cteB, cteC], outerQuery);

        // Act
        var levelNames = CteParallelizationAnalyzer.GetExecutionLevelNames(cteExpression);

        // Assert
        Assert.AreEqual(2, levelNames.Count);

        // Level 0: cteA, cteB (parallel - both have no CTE dependencies)
        Assert.AreEqual(2, levelNames[0].Count);
        Assert.IsTrue(levelNames[0].Contains("cteA"));
        Assert.IsTrue(levelNames[0].Contains("cteB"));

        // Level 1: cteC (depends on cteA)
        Assert.AreEqual(1, levelNames[1].Count);
        Assert.AreEqual("cteC", levelNames[1][0]);
    }

    #endregion

    #region CanBenefitFromParallelization Tests

    [TestMethod]
    public void CanBenefitFromParallelization_SingleCte_ShouldReturnFalse()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var canParallelize = CteParallelizationAnalyzer.CanBenefitFromParallelization(cteExpression);

        // Assert
        Assert.IsFalse(canParallelize);
    }

    [TestMethod]
    public void CanBenefitFromParallelization_TwoIndependentCtes_ShouldReturnTrue()
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
        var canParallelize = CteParallelizationAnalyzer.CanBenefitFromParallelization(cteExpression);

        // Assert
        Assert.IsTrue(canParallelize);
    }

    [TestMethod]
    public void CanBenefitFromParallelization_TwoDependentCtes_ShouldReturnFalse()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object)); // depends on cteA
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var canParallelize = CteParallelizationAnalyzer.CanBenefitFromParallelization(cteExpression);

        // Assert
        Assert.IsFalse(canParallelize);
    }

    #endregion

    #region CreatePlan Tests

    [TestMethod]
    public void CreatePlan_SingleCte_ShouldHaveSingleLevel()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(1, plan.LevelCount);
        Assert.AreEqual(1, plan.TotalCteCount);
        Assert.IsFalse(plan.CanParallelize);
    }

    [TestMethod]
    public void CreatePlan_TwoIndependentCtes_ShouldHaveSingleLevelWithTwoCtes()
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
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(1, plan.LevelCount);
        Assert.AreEqual(2, plan.TotalCteCount);
        Assert.IsTrue(plan.CanParallelize);
        Assert.AreEqual(2, plan.MaxParallelism);

        // Both should be at level 0
        Assert.AreEqual(2, plan.Levels[0].Count);
    }

    [TestMethod]
    public void CreatePlan_DependentCtes_ShouldHaveMultipleLevels()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(2, plan.LevelCount);
        Assert.AreEqual(2, plan.TotalCteCount);
        Assert.IsFalse(plan.CanParallelize); // Each level has only 1 CTE

        // Level 0: cteA
        Assert.AreEqual(1, plan.Levels[0].Count);
        Assert.AreEqual("cteA", plan.Levels[0].Ctes[0].Name);

        // Level 1: cteB
        Assert.AreEqual(1, plan.Levels[1].Count);
        Assert.AreEqual("cteB", plan.Levels[1].Ctes[0].Name);
    }

    [TestMethod]
    public void CreatePlan_DiamondDependency_ShouldHaveCorrectLevels()
    {
        // Arrange
        // cteA (level 0)
        // cteB depends on cteA (level 1)
        // cteC depends on cteA (level 1) - parallel with cteB
        // cteD depends on cteB and cteC (level 2)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));

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

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(3, plan.LevelCount);
        Assert.AreEqual(4, plan.TotalCteCount);
        Assert.IsTrue(plan.CanParallelize); // Level 1 has 2 CTEs
        Assert.AreEqual(2, plan.MaxParallelism);

        // Level 0: cteA (1 CTE)
        Assert.AreEqual(1, plan.Levels[0].Count);
        Assert.AreEqual("cteA", plan.Levels[0].Ctes[0].Name);

        // Level 1: cteB, cteC (2 CTEs - can parallelize)
        Assert.AreEqual(2, plan.Levels[1].Count);
        var level1Names = plan.Levels[1].Ctes.Select(c => c.Name).ToHashSet();
        Assert.IsTrue(level1Names.Contains("cteB"));
        Assert.IsTrue(level1Names.Contains("cteC"));

        // Level 2: cteD (1 CTE)
        Assert.AreEqual(1, plan.Levels[2].Count);
        Assert.AreEqual("cteD", plan.Levels[2].Ctes[0].Name);
    }

    #endregion

    #region Empty/Edge Case Tests

    [TestMethod]
    public void CreatePlan_DeadCtesExcluded_ShouldOnlyIncludeReachable()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2"); // Not referenced
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object)); // Only references cteA

        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(1, plan.TotalCteCount); // Only cteA is reachable
        Assert.AreEqual(1, plan.LevelCount);
        Assert.AreEqual("cteA", plan.Levels[0].Ctes[0].Name);
    }

    [TestMethod]
    public void CreatePlan_AllDeadCtes_ShouldReturnEmptyPlan()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new IntegerNode("42"); // References nothing

        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(0, plan.TotalCteCount);
        Assert.IsTrue(plan.IsEmpty);
        Assert.IsFalse(plan.CanParallelize);
    }

    [TestMethod]
    public void CanBenefitFromParallelization_WithGraph_ShouldWork()
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
        var graph = builder.Build(cteExpression);

        // Act
        var canParallelize = CteParallelizationAnalyzer.CanBenefitFromParallelization(graph);

        // Assert
        Assert.IsTrue(canParallelize);
    }

    [TestMethod]
    public void CanBenefitFromParallelization_WithGraph_NoBenefit_ShouldReturnFalse()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteBValue = new InMemoryTableFromNode("cteA", "a", typeof(object)); // depends on cteA
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");

        var outerQuery = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB], outerQuery);

        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression);

        // Act
        var canParallelize = CteParallelizationAnalyzer.CanBenefitFromParallelization(graph);

        // Assert
        Assert.IsFalse(canParallelize);
    }

    [TestMethod]
    public void CreatePlan_WithGraph_ShouldWork()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        var builder = new CteDependencyGraphBuilder();
        var graph = builder.Build(cteExpression);

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(graph);

        // Assert
        Assert.AreEqual(1, plan.TotalCteCount);
        Assert.AreSame(graph, plan.Graph);
    }

    [TestMethod]
    public void GetExecutionLevelNames_SingleCte_ShouldReturnSingleLevel()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var levelNames = CteParallelizationAnalyzer.GetExecutionLevelNames(cteExpression);

        // Assert
        Assert.AreEqual(1, levelNames.Count);
        Assert.AreEqual(1, levelNames[0].Count);
        Assert.AreEqual("cteA", levelNames[0][0]);
    }

    [TestMethod]
    public void GetExecutionLevelNames_AllDead_ShouldReturnEmptyList()
    {
        // Arrange
        var cteAValue = new IntegerNode("1");
        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var outerQuery = new IntegerNode("42"); // References nothing
        var cteExpression = new CteExpressionNode([cteA], outerQuery);

        // Act
        var levelNames = CteParallelizationAnalyzer.GetExecutionLevelNames(cteExpression);

        // Assert
        Assert.AreEqual(0, levelNames.Count);
    }

    [TestMethod]
    public void CreatePlan_ComplexDiamond_ShouldHaveCorrectStructure()
    {
        // Arrange
        // cteA, cteB (level 0 - both independent)
        // cteC depends on cteA (level 1)
        // cteD depends on cteB (level 1)
        // cteE depends on cteC and cteD (level 2)
        var cteAValue = new IntegerNode("1");
        var cteBValue = new IntegerNode("2");
        var cteCValue = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteDValue = new InMemoryTableFromNode("cteB", "b", typeof(object));

        var fromD = new InMemoryTableFromNode("cteD", "d", typeof(object));
        var joinCond = new BooleanNode(true);
        var cteEValue =
            new JoinInMemoryWithSourceTableFromNode("cteC", fromD, joinCond, JoinType.Inner, typeof(object));

        var cteA = new CteInnerExpressionNode(cteAValue, "cteA");
        var cteB = new CteInnerExpressionNode(cteBValue, "cteB");
        var cteC = new CteInnerExpressionNode(cteCValue, "cteC");
        var cteD = new CteInnerExpressionNode(cteDValue, "cteD");
        var cteE = new CteInnerExpressionNode(cteEValue, "cteE");

        var outerQuery = new InMemoryTableFromNode("cteE", "e", typeof(object));
        var cteExpression = new CteExpressionNode([cteA, cteB, cteC, cteD, cteE], outerQuery);

        // Act
        var plan = CteParallelizationAnalyzer.CreatePlan(cteExpression);

        // Assert
        Assert.AreEqual(5, plan.TotalCteCount);
        Assert.AreEqual(3, plan.LevelCount);
        Assert.IsTrue(plan.CanParallelize);
        Assert.AreEqual(2, plan.MaxParallelism);

        // Level 0: cteA, cteB (2 CTEs)
        Assert.AreEqual(2, plan.Levels[0].Count);

        // Level 1: cteC, cteD (2 CTEs)
        Assert.AreEqual(2, plan.Levels[1].Count);

        // Level 2: cteE (1 CTE)
        Assert.AreEqual(1, plan.Levels[2].Count);
    }

    #endregion
}
