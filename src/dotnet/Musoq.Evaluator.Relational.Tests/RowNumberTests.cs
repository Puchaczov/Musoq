using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class RowNumberTests : BasicEntityTestBase
{
    [TestMethod]
    public void QualifiedResultPosition_ShouldOnlySortRowsPassingQualify()
    {
        const string sql =
            "select K, RowNumber()+10 as S from values { (K:0),(K:1),(K:2) } v "
            + "qualify RowNumber() over (order by K)>1 order by 4 / K";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, [2, 11], [1, 12]);
    }

    [TestMethod]
    public void PagingDistinctOrdinalExpression_ShouldSkipTheDeduplicatedResult()
    {
        const string sql = """
            select distinct case when RowNumber() > 0 then 1 else 0 end as S
            from values { (K: 1), (K: 2), (K: 3) } v group by K skip 1
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ScalarRowNumberWithQualify_ShouldNumberOnlyTheSurvivingWindowRows()
    {
        const string sql = """
            select K, RowNumber() + 10 as R, RowNumber() over (order by K) as W
            from values { (K: 2), (K: 1) } v qualify W > 1 order by K
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, [2, 11, 2L]);
    }

    [TestMethod]
    public void ScalarSubqueryRowNumber_ShouldRestartForEachCorrelationKey()
    {
        const string sql = """
            select K,(select RowNumber()+10 from values { (K:1),(K:2) } b where b.K=a.K) as S
            from values { (K:1),(K:2) } a
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 11], [2, 11]);
    }

    [TestMethod]
    public void SlicedScalarSubqueryRowNumber_ShouldCountBeforeSkippingWithinTheBinding()
    {
        const string sql = """
            select K,(select RowNumber() from values { (K:1,N:2),(K:1,N:1),(K:2,N:4) } b
                      where b.K=a.K order by b.N skip 1 take 1) as S
            from values { (K:1),(K:2) } a
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 2], [2, null]);
    }

    [TestMethod]
    public void RowNumberInStoredCte_ShouldNumberTheCteRows()
    {
        const string sql = """
            with t as (select RowNumber() as R from values { (K: 1), (K: 1) } v)
            select R from t
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1], [2]);
    }

    [TestMethod]
    public void RowNumberInArithmetic_ShouldUseTheFinalSortOrder()
    {
        const string sql = "select K, RowNumber() + 10 as R from values { (K: 2), (K: 1) } v order by K";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, [1, 11], [2, 12]);
    }

    [TestMethod]
    public void RepeatedRowNumberCalls_ShouldReadTheSameOutputPosition()
    {
        const string sql = "select RowNumber() + RowNumber() as R from values { (K: 1) } v";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [2]);
    }

    [TestMethod]
    public void TwoRowNumberColumns_ShouldReadTheSameOutputPosition()
    {
        const string sql = "select RowNumber() as A, RowNumber() as B from values { (K: 1) } v";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 1]);
    }

    [TestMethod]
    public void WhenRowNumberSimpleTest_ShouldPass()
    {
        var query = "select RowNumber() from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Country = "Poland" }, new BasicEntity { Country = "Brazil" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var orderedNumbers = table.Select(f => (int)f.ReadValues()[0]).OrderBy(f => f).ToList();

        Assert.AreEqual(1, orderedNumbers[0]);
        Assert.AreEqual(2, orderedNumbers[1]);
    }

    // -- create
    // CREATE TABLE Cities (
    //     City TEXT NOT NULL,
    //     Country TEXT NOT NULL,
    //     Population INT NOT NULL
    // );
    //
    // -- insert
    //     INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    //     INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    //
    // -- fetch
    // SELECT Country, row_number() over (ORDER BY Country) FROM Cities order by Country;
    [TestMethod]
    public void WhenRowNumberWithOrderBySimpleTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from #A.entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Country = "Poland" }, new BasicEntity { Country = "Germany" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0].ReadValues()[1]);
        Assert.AreEqual(2, table[1].ReadValues()[1]);

        Assert.AreEqual("Germany", table[0].ReadValues()[0]);
        Assert.AreEqual("Poland", table[1].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenRowNumberWithWhereTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from #A.entities() where Country = 'Poland'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" },
                    new BasicEntity { Country = "Poland" },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(
            table.Any(row => (string)row.ReadValues()[0] == "Poland" && (int)row.ReadValues()[1] == 1),
            "Expected row with Poland, 1 not found"
        );
        Assert.IsTrue(
            table.Any(row => (string)row.ReadValues()[0] == "Poland" && (int)row.ReadValues()[1] == 2),
            "Expected row with Poland, 2 not found"
        );
    }

    [TestMethod]
    public void WhenRowNumberWithSkipTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from #A.entities() where Country = 'Poland' skip 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" },
                    new BasicEntity { Country = "Poland" },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(2, table[0].ReadValues()[1]);
        Assert.AreEqual("Poland", table[0].ReadValues()[0]);
    }

    // -- create
    //     CREATE TABLE Cities (
    //     City TEXT NOT NULL,
    //     Country TEXT NOT NULL,
    //     Population INT NOT NULL
    // );
    //
    // -- insert
    // INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    // INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    // INSERT INTO Cities VALUES ('CZESTOCHOWA', 'POLAND', 250);
    //
    // -- fetch
    // SELECT Country, row_number() over (ORDER BY Country) FROM Cities group by Country order by Country offset 1;
    [TestMethod]
    public void WhenRowNumberWithGroupByAndSkipTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from #A.entities() group by Country order by Country skip 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" },
                    new BasicEntity { Country = "Poland" },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(2, table[0].ReadValues()[1]);
        Assert.AreEqual("Poland", table[0].ReadValues()[0]);
    }

    // -- create
    // CREATE TABLE Cities (
    //     City TEXT NOT NULL,
    //     Country TEXT NOT NULL,
    //     Population INT NOT NULL
    // );
    //
    // -- insert
    // INSERT INTO Cities VALUES ('WARSAW', 'POLAND', 500);
    // INSERT INTO Cities VALUES ('MUNICH', 'GERMANY', 350);
    // INSERT INTO Cities VALUES ('CZESTOCHOWA', 'POLAND', 250);
    //
    // -- fetch
    // SELECT Country, row_number() over (ORDER BY Country) FROM Cities group by Country order by Country limit 1;
    [TestMethod]
    public void WhenRowNumberWithGroupByAndTakeTest_ShouldPass()
    {
        var query = "select Country, RowNumber() from #A.entities() group by Country order by Country take 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" },
                    new BasicEntity { Country = "Poland" },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(1, table[0].ReadValues()[1]);
        Assert.AreEqual("Germany", table[0].ReadValues()[0]);
    }
}
