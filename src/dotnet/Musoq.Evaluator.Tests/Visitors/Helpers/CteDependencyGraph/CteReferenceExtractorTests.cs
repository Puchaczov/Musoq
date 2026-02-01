using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteReferenceExtractorTests
{
    [TestMethod]
    public void FoundReferences_WhenNoVisits_ShouldBeEmpty()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);

        // Assert
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_InMemoryTableFromNode_MatchingCte_ShouldAddToReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var node = new InMemoryTableFromNode("cteA", "alias", typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
        Assert.Contains("cteA", extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_InMemoryTableFromNode_NonMatchingCte_ShouldNotAddToReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var node = new InMemoryTableFromNode("cteC", "alias", typeof(object)); // Unknown CTE

        // Act
        extractor.Visit(node);

        // Assert
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_JoinInMemoryWithSourceTableFromNode_MatchingCte_ShouldAddToReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var right = new InMemoryTableFromNode("another", "r", typeof(object));
        var condition = new BooleanNode(true);
        var node = new JoinInMemoryWithSourceTableFromNode("cteA", right, condition, JoinType.Inner, typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
        Assert.Contains("cteA", extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_ApplyInMemoryWithSourceTableFromNode_MatchingCte_ShouldAddToReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var sourceTable = new InMemoryTableFromNode("other", "l", typeof(object));
        var node = new ApplyInMemoryWithSourceTableFromNode("cteB", sourceTable, ApplyType.Cross, typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
        Assert.Contains("cteB", extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_MultipleNodes_ShouldAccumulateReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB", "cteC"]);
        var node1 = new InMemoryTableFromNode("cteA", "alias1", typeof(object));
        var node2 = new InMemoryTableFromNode("cteB", "alias2", typeof(object));

        // Act
        extractor.Visit(node1);
        extractor.Visit(node2);

        // Assert
        Assert.HasCount(2, extractor.FoundReferences);
        Assert.Contains("cteA", extractor.FoundReferences);
        Assert.Contains("cteB", extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_DuplicateReferences_ShouldNotDuplicate()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var node1 = new InMemoryTableFromNode("cteA", "alias1", typeof(object));
        var node2 = new InMemoryTableFromNode("cteA", "alias2", typeof(object));

        // Act
        extractor.Visit(node1);
        extractor.Visit(node2);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
    }

    [TestMethod]
    public void Clear_ShouldResetFoundReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var node = new InMemoryTableFromNode("cteA", "alias", typeof(object));
        extractor.Visit(node);
        Assert.HasCount(1, extractor.FoundReferences);

        // Act
        extractor.Clear();

        // Assert
        Assert.IsEmpty(extractor.FoundReferences);
    }

    #region Edge Case Tests

    [TestMethod]
    public void Constructor_EmptyKnownCteNames_ShouldWork()
    {
        // Arrange & Act
        var extractor = new CteReferenceExtractor([]);

        // Assert
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_InMemoryTableFromNode_EmptyVariableName_ShouldNotThrow()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var node = new InMemoryTableFromNode("", "alias", typeof(object));

        // Act
        extractor.Visit(node);

        // Assert - Should not add empty string
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_JoinInMemoryWithSourceTableFromNode_NonMatchingCte_ShouldNotAdd()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var right = new InMemoryTableFromNode("other", "r", typeof(object));
        var condition = new BooleanNode(true);
        var node = new JoinInMemoryWithSourceTableFromNode("unknownCte", right, condition, JoinType.Inner,
            typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_ApplyInMemoryWithSourceTableFromNode_NonMatchingCte_ShouldNotAdd()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var sourceTable = new InMemoryTableFromNode("other", "l", typeof(object));
        var node = new ApplyInMemoryWithSourceTableFromNode("unknownCte", sourceTable, ApplyType.Cross, typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_AllNodeTypes_ShouldAccumulateReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB", "cteC"]);

        var inMemoryNode = new InMemoryTableFromNode("cteA", "a", typeof(object));

        var right = new InMemoryTableFromNode("other", "r", typeof(object));
        var condition = new BooleanNode(true);
        var joinNode =
            new JoinInMemoryWithSourceTableFromNode("cteB", right, condition, JoinType.Inner, typeof(object));

        var sourceTable = new InMemoryTableFromNode("another", "l", typeof(object));
        var applyNode = new ApplyInMemoryWithSourceTableFromNode("cteC", sourceTable, ApplyType.Outer, typeof(object));

        // Act
        extractor.Visit(inMemoryNode);
        extractor.Visit(joinNode);
        extractor.Visit(applyNode);

        // Assert
        Assert.HasCount(3, extractor.FoundReferences);
        Assert.Contains("cteA", extractor.FoundReferences);
        Assert.Contains("cteB", extractor.FoundReferences);
        Assert.Contains("cteC", extractor.FoundReferences);
    }

    [TestMethod]
    public void Clear_ThenVisitAgain_ShouldWork()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var node1 = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var node2 = new InMemoryTableFromNode("cteB", "b", typeof(object));

        extractor.Visit(node1);
        Assert.HasCount(1, extractor.FoundReferences);

        // Act
        extractor.Clear();
        extractor.Visit(node2);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
        Assert.Contains("cteB", extractor.FoundReferences);
        Assert.DoesNotContain("cteA", extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_CaseSensitiveMatching_ShouldMatchExactly()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["CteA"]);
        var node = new InMemoryTableFromNode("ctea", "a", typeof(object)); // lowercase

        // Act
        extractor.Visit(node);

        // Assert - Should not match because case differs
        Assert.IsEmpty(extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_ApplyType_OuterApply_ShouldWork()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var sourceTable = new InMemoryTableFromNode("other", "l", typeof(object));
        var node = new ApplyInMemoryWithSourceTableFromNode("cteA", sourceTable, ApplyType.Outer, typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
        Assert.Contains("cteA", extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_JoinType_LeftJoin_ShouldWork()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var right = new InMemoryTableFromNode("other", "r", typeof(object));
        var condition = new BooleanNode(true);
        var node = new JoinInMemoryWithSourceTableFromNode("cteA", right, condition, JoinType.OuterLeft,
            typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
    }

    [TestMethod]
    public void Visit_JoinType_RightJoin_ShouldWork()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var right = new InMemoryTableFromNode("other", "r", typeof(object));
        var condition = new BooleanNode(true);
        var node = new JoinInMemoryWithSourceTableFromNode("cteA", right, condition, JoinType.OuterRight,
            typeof(object));

        // Act
        extractor.Visit(node);

        // Assert
        Assert.HasCount(1, extractor.FoundReferences);
    }

    #endregion
}
