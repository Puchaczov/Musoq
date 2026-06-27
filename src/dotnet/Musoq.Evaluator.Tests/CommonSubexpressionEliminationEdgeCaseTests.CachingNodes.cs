using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CommonSubexpressionEliminationEdgeCaseTests
{
    #region CSE Toggle Tests

    [TestMethod]
    public void WhenCseDisabled_ShouldStillProduceCorrectResults()
    {
        const string query = @"
            SELECT ToString(Population), ToString(Population) + '_suffix'
            FROM #A.Entities()
            WHERE ToString(Population) = '100'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 100)
                ]
            }
        };


        var compilationOptions = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "100"));
        Assert.IsTrue(table.All(row => (string)row.Values[1] == "100_suffix"));
    }

    [TestMethod]
    public void WhenCseEnabled_ShouldProduceSameResultsAsDisabled()
    {
        const string query = @"
            SELECT
                Length(Name) as Len1,
                Length(Name) + 10 as Len2,
                CASE WHEN Length(Name) > 3 THEN 'long' ELSE 'short' END as Category
            FROM #A.Entities()
            WHERE Length(Name) > 1
            ORDER BY Length(Name)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCDE"),
                    new BasicEntity("X")
                ]
            }
        };


        var compilationOptionsEnabled = new CompilationOptions(useCommonSubexpressionElimination: true);
        var vmEnabled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptionsEnabled);
        var tableEnabled = vmEnabled.Run(TestContext.CancellationToken);


        var compilationOptionsDisabled = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vmDisabled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptionsDisabled);
        var tableDisabled = vmDisabled.Run(TestContext.CancellationToken);


        Assert.AreEqual(tableDisabled.Count, tableEnabled.Count);
        for (var i = 0; i < tableEnabled.Count; i++)
        {
            Assert.AreEqual(tableDisabled[i][0], tableEnabled[i][0], $"Row {i}, Column 0 mismatch");
            Assert.AreEqual(tableDisabled[i][1], tableEnabled[i][1], $"Row {i}, Column 1 mismatch");
            Assert.AreEqual(tableDisabled[i][2], tableEnabled[i][2], $"Row {i}, Column 2 mismatch");
        }
    }

    [TestMethod]
    public void WhenCseDisabled_CaseWhenShouldStillWork()
    {
        const string query = @"
            SELECT
                CASE
                    WHEN Inc(Population) > 200 THEN Inc(Population) * 2
                    ELSE Inc(Population)
                END as Result
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 300)
                ]
            }
        };

        var compilationOptions = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => Convert.ToDecimal(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(101m, results[0]);
        Assert.AreEqual(602m, results[1]);
    }

    #endregion

    #region Column Access Caching Tests

    [TestMethod]
    public void WhenSameColumnAccessedMultipleTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Population, Population + 10, Population * 2
            FROM #A.Entities()
            WHERE Population > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 50),
                    new BasicEntity("USA", 200),
                    new BasicEntity("Germany", 150)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        var rows = table.OrderBy(r => (decimal)r[0]).ToList();
        Assert.AreEqual(150m, rows[0][0]);
        Assert.AreEqual(160m, rows[0][1]);
        Assert.AreEqual(300m, rows[0][2]);
        Assert.AreEqual(200m, rows[1][0]);
        Assert.AreEqual(210m, rows[1][1]);
        Assert.AreEqual(400m, rows[1][2]);
    }

    [TestMethod]
    public void WhenMultipleColumnsAccessedMultipleTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Country,
                Population,
                Country + '_suffix',
                Population + 100
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();
        Assert.AreEqual("Poland", rows[0][0]);
        Assert.AreEqual(100m, rows[0][1]);
        Assert.AreEqual("Poland_suffix", rows[0][2]);
        Assert.AreEqual(200m, rows[0][3]);
    }

    [TestMethod]
    public void WhenColumnInWhereAndCaseWhen_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                CASE
                    WHEN Population > 150 THEN 'High'
                    ELSE 'Low'
                END as Category
            FROM #A.Entities()
            WHERE Population > 50";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 200),
                    new BasicEntity("UK", 30)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[0]).ToList();
        Assert.AreEqual(100m, rows[0][0]);
        Assert.AreEqual("Low", rows[0][1]);
        Assert.AreEqual(200m, rows[1][0]);
        Assert.AreEqual("High", rows[1][1]);
    }

    [TestMethod]
    public void WhenColumnUsedWithMethodCall_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                Population + Inc(Population)
            FROM #A.Entities()
            WHERE Population > 0";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(101m, table[0][1]);
        Assert.AreEqual(201m, table[0][2]);
    }

    #endregion
}
