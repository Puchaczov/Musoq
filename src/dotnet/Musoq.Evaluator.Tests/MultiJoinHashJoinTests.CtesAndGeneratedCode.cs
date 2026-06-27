using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultiJoinHashJoinTests
{
    #region CTE-Based Multi-Join Tests

    /// <summary>
    ///     Tests three-way join using CTEs.
    /// </summary>
    [TestMethod]
    public void ThreeWayCteInnerJoin_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities()),
                cteC AS (SELECT Name, Population FROM C.Entities())
            SELECT a.Name, b.Name, c.Name
            FROM cteA a
            INNER JOIN cteB b ON a.Population = b.Population
            INNER JOIN cteC c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Should have 1 matching row");
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
    }

    /// <summary>
    ///     Tests four-way CTE join with mixed join types.
    /// </summary>
    [TestMethod]
    public void FourWayCteJoin_MixedTypes_WithHashJoinEnabled_ShouldProduceCorrectResults()
    {
        const string query = @"
            WITH
                cteA AS (SELECT Name, Population FROM A.Entities()),
                cteB AS (SELECT Name, Population FROM B.Entities()),
                cteC AS (SELECT Name, Population FROM C.Entities()),
                cteD AS (SELECT Name, Population FROM D.Entities())
            SELECT a.Name, b.Name, c.Name, d.Name
            FROM cteA a
            INNER JOIN cteB b ON a.Population = b.Population
            LEFT OUTER JOIN cteC c ON b.Population = c.Population
            LEFT OUTER JOIN cteD d ON a.Population = d.Population
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A1", Population = 100 }, new BasicEntity { Name = "A2", Population = 200 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "B1", Population = 100 }, new BasicEntity { Name = "B2", Population = 200 }
                ]
            },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] },
            { "#D", [new BasicEntity { Name = "D1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");


        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
        Assert.AreEqual("D1", table[0][3]);


        Assert.AreEqual("A2", table[1][0]);
        Assert.AreEqual("B2", table[1][1]);
        Assert.IsNull(table[1][2]);
        Assert.IsNull(table[1][3]);
    }

    #endregion

    #region Code Generation Verification Tests - Hash Join Works for All Joins

    /// <summary>
    ///     Verifies that hash join is used for ALL joins in a multi-join query.
    ///     For a query like: A JOIN B ON a.x = b.x JOIN C ON b.y = c.y
    ///     - The A-B join uses hash join (creates bHashed dictionary)
    ///     - The (AB)-C join also uses hash join (creates abHashed dictionary)
    ///     This was fixed by allowing chained joins with prefixed column names in
    ///     JoinInMemoryWithSourceTableNodeProcessor.TryGetHashJoinKeys.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Three-Way Inner Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");


        var hashDictionaryCount = CountHashDictionaryDeclarations(generatedCode);


        Assert.AreEqual(2, hashDictionaryCount,
            $"Expected 2 hash dictionaries for a three-way join, but found {hashDictionaryCount}. " +
            "Both the A-B join and the AB-C join should use hash join.");


        AssertContainsAny(
            generatedCode,
            "First join should create a hash dictionary for b",
            "bHashed = new Dictionary<",
            "bHash = new Dictionary<",
            "BHash = new Dictionary<");
        AssertContainsAny(
            generatedCode,
            "Second join should create a hash dictionary for the intermediate result",
            "abHashed = new Dictionary<",
            "abHash = new Dictionary<",
            "AbHash = new Dictionary<");
    }

    /// <summary>
    ///     Verifies that hash join is used for ALL joins in a four-way join query.
    /// </summary>
    [TestMethod]
    public void FourWayInnerJoin_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name, d.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population
            INNER JOIN #D.Entities() d ON c.Population = d.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] },
            { "#D", [new BasicEntity { Name = "D1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Four-Way Inner Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount = CountHashDictionaryDeclarations(generatedCode);


        Assert.AreEqual(3, hashDictionaryCount,
            $"Expected 3 hash dictionaries for a four-way join, but found {hashDictionaryCount}. " +
            "All three joins should use hash join.");
    }

    /// <summary>
    ///     Verifies that hash join is used for ALL joins in a three-way left outer join query.
    ///     Now that hash join is properly implemented for chained outer joins, all joins use hash join.
    /// </summary>
    [TestMethod]
    public void ThreeWayLeftOuterJoin_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            LEFT OUTER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Three-Way Left Outer Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount = CountHashDictionaryDeclarations(generatedCode);


        Assert.AreEqual(2, hashDictionaryCount,
            $"Expected 2 hash dictionaries (all joins use hash join), but found {hashDictionaryCount}.");
    }

    /// <summary>
    ///     Verifies that for mixed joins (inner + outer), inner joins use hash join but chained outer joins fall back.
    /// </summary>
    [TestMethod]
    public void MixedJoins_GeneratedCode_AllJoinsUseHashJoin()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            LEFT OUTER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code for Mixed Join Types ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount = CountHashDictionaryDeclarations(generatedCode);


        Assert.AreEqual(2, hashDictionaryCount,
            $"Expected 2 hash dictionaries (all joins use hash join), but found {hashDictionaryCount}.");
    }

    /// <summary>
    ///     Verifies that WITHOUT hash join enabled, the code uses nested loops instead.
    /// </summary>
    [TestMethod]
    public void ThreeWayInnerJoin_WithoutHashJoin_ShouldNotContainHashDictionaries()
    {
        const string query = @"
            SELECT a.Name, b.Name, c.Name
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Population = b.Population
            INNER JOIN #C.Entities() c ON b.Population = c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 100 }] },
            { "#C", [new BasicEntity { Name = "C1", Population = 100 }] }
        };

        var generatedCode = CompileAndGetGeneratedCode(query, sources,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        Console.WriteLine("=== Generated Code WITHOUT Hash Join ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("=== End of Generated Code ===");

        var hashDictionaryCount = CountHashDictionaryDeclarations(generatedCode);

        Assert.AreEqual(0, hashDictionaryCount,
            $"Expected 0 hash dictionaries when hash join is disabled, but found {hashDictionaryCount}. " +
            $"\n\nGenerated code:\n{generatedCode}");
    }

    #endregion
}
