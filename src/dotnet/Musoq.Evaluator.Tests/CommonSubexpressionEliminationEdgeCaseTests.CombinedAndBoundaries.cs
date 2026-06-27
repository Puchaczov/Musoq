using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CommonSubexpressionEliminationEdgeCaseTests
{
    #region Complex Combined Scenarios

    [TestMethod]
    public void WhenComplexQueryWithMultipleCsePatterns_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Name,
                Length(Name) as Len,
                ToUpper(Name) as Upper,
                CASE
                    WHEN Length(Name) > 4 THEN 'Very Long'
                    WHEN Length(Name) > 2 THEN 'Long'
                    ELSE 'Short'
                END as Category,
                Length(Name) * 10 as LenTimes10
            FROM #A.Entities()
            WHERE Length(Name) >= 2 AND ToUpper(Name) LIKE 'A%'
            ORDER BY Length(Name) DESC";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("a"),
                    new BasicEntity("ab"),
                    new BasicEntity("Xyz"),
                    new BasicEntity("Abcdef"),
                    new BasicEntity("ABC")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);


        Assert.AreEqual("Abcdef", table[0][0]);
        Assert.AreEqual(6, Convert.ToInt32(table[0][1]));
        Assert.AreEqual("ABCDEF", table[0][2]);
        Assert.AreEqual("Very Long", table[0][3]);
        Assert.AreEqual(60, Convert.ToInt32(table[0][4]));


        Assert.AreEqual("ABC", table[1][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[1][1]));
        Assert.AreEqual("ABC", table[1][2]);
        Assert.AreEqual("Long", table[1][3]);
        Assert.AreEqual(30, Convert.ToInt32(table[1][4]));


        Assert.AreEqual("ab", table[2][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[2][1]));
        Assert.AreEqual("AB", table[2][2]);
        Assert.AreEqual("Short", table[2][3]);
        Assert.AreEqual(20, Convert.ToInt32(table[2][4]));
    }

    [TestMethod]
    public void WhenSubqueryWithSameCsePattern_ShouldIsolateCorrectly()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) > 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("XY")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCD", table[0][0]);
        Assert.AreEqual(4, Convert.ToInt32(table[0][1]));
    }

    #endregion

    #region Boundary Values and Special Cases

    [TestMethod]
    public void WhenExpressionReturnsEmptyString_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT
                Name,
                Length(Name) as Len,
                Length(Name) * 2 as DoubleLlen
            FROM #A.Entities()
            WHERE Length(Name) >= 0";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity(""),
                    new BasicEntity("A"),
                    new BasicEntity("ABC")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => row[0] is string text && text.Length == 0 && Convert.ToInt32(row[1]) == 0));
        Assert.IsTrue(table.Any(row => (string)row[0] == "A" && Convert.ToInt32(row[1]) == 1));
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABC" && Convert.ToInt32(row[1]) == 3));
    }

    [TestMethod]
    public void WhenExpressionWithLargeValues_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT Population, Population * Population as Squared
            FROM #A.Entities()
            WHERE Population * Population > 1000000";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 1000),
                    new BasicEntity("C", 10000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10000m, (decimal)table[0][0]);
        Assert.AreEqual(100000000m, (decimal)table[0][1]);
    }

    #endregion
}
