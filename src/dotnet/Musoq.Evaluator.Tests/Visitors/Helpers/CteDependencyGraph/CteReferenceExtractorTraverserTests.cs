using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public class CteReferenceExtractorTraverserTests
{
    [TestMethod]
    public void Traverse_SimpleQuery_ShouldFindCteReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Create a simple query that references cteA
        var cteRef = new InMemoryTableFromNode("cteA", "a", typeof(object));

        // Act
        cteRef.Accept(traverser);

        // Assert
        Assert.AreEqual(1, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
    }

    [TestMethod]
    public void Traverse_JoinQuery_ShouldFindBothCteReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Create a join query that references both CTEs
        var fromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCondition = new BooleanNode(true);
        var joinNode =
            new JoinInMemoryWithSourceTableFromNode("cteA", fromB, joinCondition, JoinType.Inner, typeof(object));

        // Act
        joinNode.Accept(traverser);

        // Assert
        Assert.AreEqual(2, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
        Assert.IsTrue(extractor.FoundReferences.Contains("cteB"));
    }

    [TestMethod]
    public void Traverse_NestedJoins_ShouldFindAllCteReferences()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB", "cteC"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Create nested joins: (cteA JOIN cteB) JOIN cteC
        var fromB = new InMemoryTableFromNode("cteB", "b", typeof(object));
        var joinCond1 = new BooleanNode(true);
        var joinAB = new JoinInMemoryWithSourceTableFromNode("cteA", fromB, joinCond1, JoinType.Inner, typeof(object));

        var fromC = new InMemoryTableFromNode("cteC", "c", typeof(object));
        var joinCond2 = new BooleanNode(true);
        var joinFromABC = new JoinFromNode(joinAB, fromC, joinCond2, JoinType.Inner, typeof(object));
        var joinABC = new JoinNode(joinFromABC, typeof(object));

        // Act
        joinABC.Accept(traverser);

        // Assert
        Assert.AreEqual(3, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
        Assert.IsTrue(extractor.FoundReferences.Contains("cteB"));
        Assert.IsTrue(extractor.FoundReferences.Contains("cteC"));
    }

    [TestMethod]
    public void Traverse_QueryWithNonCteFromNode_ShouldNotFindIt()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Create a schema from node (not a CTE reference)
        var schemaFrom = new SchemaFromNode("schema", "method", new ArgsListNode([]), null, typeof(object), 0);

        // Act
        schemaFrom.Accept(traverser);

        // Assert
        Assert.AreEqual(0, extractor.FoundReferences.Count);
    }

    [TestMethod]
    public void Traverse_MixedQuery_ShouldOnlyFindKnownCtes()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]); // Only cteA is known
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Create a join between known CTE and unknown table
        var unknownTable = new InMemoryTableFromNode("unknownCte", "u", typeof(object));
        var joinCond = new BooleanNode(true);
        var join = new JoinInMemoryWithSourceTableFromNode("cteA", unknownTable, joinCond, JoinType.Inner,
            typeof(object));

        // Act
        join.Accept(traverser);

        // Assert
        Assert.AreEqual(1, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
    }

    [TestMethod]
    public void Traverse_ApplyNode_ShouldFindCteReference()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        var sourceTable = new SchemaFromNode("schema", "method", new ArgsListNode([]), null, typeof(object), 0);
        var applyNode = new ApplyInMemoryWithSourceTableFromNode("cteA", sourceTable, ApplyType.Cross, typeof(object));

        // Act
        applyNode.Accept(traverser);

        // Assert
        Assert.AreEqual(1, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
    }

    [TestMethod]
    public void Traverse_SelectQuery_ShouldTraverseFromClause()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Create a simple select with CTE in FROM clause
        var fromCte = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var selectFields = new[]
        {
            new FieldNode(new IntegerNode("1"), 0, "col1")
        };
        var selectNode = new SelectNode(selectFields);
        var queryNode = new QueryNode(selectNode, fromCte, null, null, null, null, null);

        // Act
        queryNode.Accept(traverser);

        // Assert
        Assert.AreEqual(1, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
    }

    [TestMethod]
    public void Traverse_EmptyExtractor_ShouldFindNothing()
    {
        // Arrange
        var extractor = new CteReferenceExtractor([]); // No known CTEs
        var traverser = new CteReferenceExtractorTraverser(extractor);

        var cteRef = new InMemoryTableFromNode("cteA", "a", typeof(object));

        // Act
        cteRef.Accept(traverser);

        // Assert
        Assert.AreEqual(0, extractor.FoundReferences.Count);
    }

    [TestMethod]
    public void Traverse_MultiplePasses_ShouldAccumulate()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA", "cteB"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        var cteRefA = new InMemoryTableFromNode("cteA", "a", typeof(object));
        var cteRefB = new InMemoryTableFromNode("cteB", "b", typeof(object));

        // Act - Traverse two separate nodes
        cteRefA.Accept(traverser);
        cteRefB.Accept(traverser);

        // Assert
        Assert.AreEqual(2, extractor.FoundReferences.Count);
        Assert.IsTrue(extractor.FoundReferences.Contains("cteA"));
        Assert.IsTrue(extractor.FoundReferences.Contains("cteB"));
    }

    [TestMethod]
    public void Traverse_SameCteMultipleTimes_ShouldNotDuplicate()
    {
        // Arrange
        var extractor = new CteReferenceExtractor(["cteA"]);
        var traverser = new CteReferenceExtractorTraverser(extractor);

        // Join that references cteA twice (once in alias, once in source table)
        var fromA1 = new InMemoryTableFromNode("cteA", "a1", typeof(object));
        var joinCond = new BooleanNode(true);
        var join = new JoinInMemoryWithSourceTableFromNode("cteA", fromA1, joinCond, JoinType.Inner, typeof(object));

        // Act
        join.Accept(traverser);

        // Assert - Should have cteA only once despite two references
        Assert.AreEqual(1, extractor.FoundReferences.Count);
    }
}
