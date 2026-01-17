using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DistinctTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }
    
    [TestMethod]
    public void DistinctToGroupByVisitor_TransformsQueryCorrectly()
    {
        // Arrange
        var query = "select distinct Name from #A.Entities()";
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var root = parser.ComposeAll();
        
        // Navigate the tree structure: RootNode -> StatementsArrayNode -> StatementNode[] -> SingleSetNode -> QueryNode
        var statementsArray = root.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray, "Root.Expression should be StatementsArrayNode");
        Assert.HasCount(1, statementsArray.Statements, "Should have one statement");
        
        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet, "First statement should be SingleSetNode");
        
        var originalQueryNode = singleSet.Query;
        Assert.IsNotNull(originalQueryNode, "SingleSetNode should contain QueryNode");
        Assert.IsTrue(originalQueryNode.Select.IsDistinct, "Original should have IsDistinct = true");
        Assert.IsNull(originalQueryNode.GroupBy, "Original should not have GROUP BY");
        
        // Act
        var visitor = new DistinctToGroupByVisitor();
        var traverser = new DistinctToGroupByTraverseVisitor(visitor);
        root.Accept(traverser);
        var transformedRoot = traverser.Root;
        
        // Navigate the transformed tree - the visitor clones the tree so structure should be preserved
        var transformedStatementsArray = transformedRoot.Expression as StatementsArrayNode;
        Assert.IsNotNull(transformedStatementsArray, "Transformed Root.Expression should be StatementsArrayNode");
        Assert.HasCount(1, transformedStatementsArray.Statements, "Should still have one statement");
        
        // The statement node could be QueryNode directly or wrapped in SingleSetNode
        QueryNode queryNode = null;
        var statementNode = transformedStatementsArray.Statements[0].Node;
        if (statementNode is SingleSetNode transformedSingleSet)
        {
            queryNode = transformedSingleSet.Query;
        }
        else if (statementNode is QueryNode qn)
        {
            queryNode = qn;
        }
        
        Assert.IsNotNull(queryNode, $"Could not find QueryNode in transformed tree. StatementNode type: {statementNode?.GetType().Name ?? "null"}");
        Assert.IsNotNull(queryNode.GroupBy, "DISTINCT should have been converted to GROUP BY");
        Assert.IsFalse(queryNode.Select.IsDistinct, "IsDistinct flag should be cleared after conversion");
        Assert.HasCount(1, queryNode.GroupBy.Fields, "GROUP BY should have one field matching SELECT");
    }
    
    [TestMethod]
    public void SelectDistinct_RemovesDuplicateNames()
    {
        var query = "select distinct Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        
        var names = table.Select(row => row.Values[0]?.ToString()).OrderBy(n => n).ToList();
        CollectionAssert.AreEqual(new[] { "ABBA", "BABBA", "CECCA" }, names);
    }

    [TestMethod]
    public void SelectDistinct_MultipleColumns_UniquesCombinations()
    {
        var query = "select distinct City, Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("cracow", "Poland", 1),
                    new BasicEntity("cracow", "Poland", 2),
                    new BasicEntity("warsaw", "Poland", 3),
                    new BasicEntity("cracow", "Germany", 4),
                    new BasicEntity("warsaw", "Poland", 5)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        
        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_AllUnique_ReturnsAllRows()
    {
        var query = "select distinct Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_AllSame_ReturnsSingleRow()
    {
        var query = "select distinct Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("SAME"),
                    new BasicEntity("SAME"),
                    new BasicEntity("SAME"),
                    new BasicEntity("SAME")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("SAME", table[0].Values[0]);
    }

    [TestMethod]
    public void SelectDistinct_WithWhere_FiltersFirst()
    {
        var query = "select distinct City from #A.Entities() where Population > 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("cracow", "jan", 150m) { Population = 200 },
                    new BasicEntity("cracow", "jan", 150m) { Population = 300 },
                    new BasicEntity("warsaw", "jan", 150m) { Population = 50 }, 
                    new BasicEntity("warsaw", "jan", 150m) { Population = 400 },
                    new BasicEntity("lodz", "jan", 150m) { Population = 80 } 
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        var cities = table.Select(row => row.Values[0]?.ToString()).OrderBy(n => n).ToList();
        CollectionAssert.AreEqual(new[] { "cracow", "warsaw" }, cities);
    }

    [TestMethod]
    public void SelectDistinct_WithOrderBy_OrdersResults()
    {
        var query = "select distinct Name from #A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("BABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("CECCA", table[0].Values[0]);
        Assert.AreEqual("BABBA", table[1].Values[0]);
        Assert.AreEqual("ABBA", table[2].Values[0]);
    }

    [TestMethod]
    public void SelectDistinct_WithExpression_Works()
    {
        var query = "select distinct ToUpperInvariant(Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("abba"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("Abba"),
                    new BasicEntity("babba")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_WithGroupBy_GroupByTakesPrecedence()
    {
        
        var query = "select distinct Name, Count(Name) from #A.Entities() group by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_EmptyResult_ReturnsEmpty()
    {
        var query = "select distinct Name from #A.Entities() where 1 = 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_SingleRow_ReturnsSingleRow()
    {
        var query = "select distinct Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ONLY")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ONLY", table[0].Values[0]);
    }

    [TestMethod]
    public void SelectDistinct_NumericColumn_Works()
    {
        var query = "select distinct Population from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100 },
                    new BasicEntity { Population = 200 },
                    new BasicEntity { Population = 100 },
                    new BasicEntity { Population = 300 },
                    new BasicEntity { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_WithSkipTake_Works()
    {
        var query = "select distinct Name from #A.Entities() order by Name skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("DEDDA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("BABBA", table[0].Values[0]);
        Assert.AreEqual("CECCA", table[1].Values[0]);
    }

    [TestMethod]
    public void SelectDistinct_UpperCase_Works()
    {
        var query = "SELECT DISTINCT Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void SelectDistinct_ReorderedQuery_Works()
    {
        var query = "from #A.Entities() select distinct Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }
}
