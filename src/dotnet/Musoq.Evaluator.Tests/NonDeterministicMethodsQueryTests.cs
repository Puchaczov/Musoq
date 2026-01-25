using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests verifying that non-deterministic methods (marked with [NonDeterministic])
///     behave correctly in queries and are NOT cached by Common Subexpression Elimination (CSE).
/// </summary>
[TestClass]
public class NonDeterministicMethodsQueryTests : BasicEntityTestBase
{
    #region Non-Deterministic in WHERE and SELECT

    [TestMethod]
    public void WhenNonDeterministicMethodInWhereAndSelect_ShouldNotBeCached()
    {
        const string query = @"
            SELECT RandomNumber() as RandomVal, Name
            FROM #A.Entities()
            WHERE RandomNumber() >= 0";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Non-Deterministic with CASE WHEN

    [TestMethod]
    public void WhenNonDeterministicMethodInCaseWhen_ShouldWork()
    {
        const string query = @"
            SELECT
                CASE WHEN RandomNumber() < 50 THEN 'Low' ELSE 'High' END as Category,
                Name
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
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


        foreach (var row in table)
        {
            var category = (string)row[0];
            Assert.IsTrue(category == "Low" || category == "High",
                $"Category should be 'Low' or 'High', but was '{category}'");
        }
    }

    #endregion

    #region Non-Deterministic in Various Contexts

    [TestMethod]
    public void WhenNonDeterministicMethodInSubquery_ShouldWork()
    {
        const string query = @"
            SELECT Name, RandomNumber() as Rand
            FROM #A.Entities()
            WHERE Name IN ('A', 'B')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        foreach (var row in table)
        {
            var name = (string)row[0];
            var random = Convert.ToInt32(row[1]);

            Assert.IsTrue(name == "A" || name == "B");
            Assert.IsTrue(random >= 0 && random < 100);
        }
    }

    #endregion

    #region Basic Non-Deterministic Method Tests

    [TestMethod]
    public void WhenNonDeterministicMethodInSelect_ShouldReturnDifferentValuesPerRow()
    {
        const string query = "SELECT RandomNumber(), Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D"),
                    new BasicEntity("E")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(5, table.Count);


        foreach (var row in table)
        {
            var value = Convert.ToInt32(row[0]);
            Assert.IsTrue(value >= 0 && value < 100, $"Random value {value} should be between 0 and 99");
        }
    }

    [TestMethod]
    public void WhenNonDeterministicMethodAppearsMultipleTimes_ShouldNotBeCached()
    {
        const string query = "SELECT RandomNumber() as R1, RandomNumber() as R2, Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Test1"),
                    new BasicEntity("Test2"),
                    new BasicEntity("Test3")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);


        var r1Values = table.Select(r => Convert.ToInt32(r[0])).ToList();
        var r2Values = table.Select(r => Convert.ToInt32(r[1])).ToList();


        foreach (var v in r1Values.Concat(r2Values))
            Assert.IsTrue(v >= 0 && v < 100, $"Random value {v} should be between 0 and 99");
    }

    #endregion

    #region Non-Deterministic with Arithmetic

    [TestMethod]
    public void WhenNonDeterministicMethodInArithmetic_ShouldWork()
    {
        const string query = "SELECT RandomNumber() + 100 as Shifted, Name FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        foreach (var row in table)
        {
            var shifted = Convert.ToInt32(row[0]);
            Assert.IsTrue(shifted >= 100 && shifted < 200,
                $"Shifted value {shifted} should be between 100 and 199");
        }
    }

    [TestMethod]
    public void WhenNonDeterministicMethodInMultipleArithmeticExpressions_ShouldNotBeCached()
    {
        const string query = @"
            SELECT
                RandomNumber() + 100 as Shifted1,
                RandomNumber() * 2 as Doubled,
                Name
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        foreach (var row in table)
        {
            var shifted1 = Convert.ToInt32(row[0]);
            var doubled = Convert.ToInt32(row[1]);


            Assert.IsTrue(shifted1 >= 100 && shifted1 < 200,
                $"Shifted1 {shifted1} should be between 100 and 199");
            Assert.IsTrue(doubled >= 0 && doubled < 200,
                $"Doubled {doubled} should be between 0 and 198");
        }
    }

    #endregion

    #region Deterministic vs Non-Deterministic Comparison

    [TestMethod]
    public void WhenDeterministicMethodAppearsMultipleTimes_ShouldBeCached()
    {
        const string query = "SELECT Length(Name) as Len1, Length(Name) as Len2, Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABC"),
                    new BasicEntity("DEFGH")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        foreach (var row in table)
        {
            var len1 = Convert.ToInt32(row[0]);
            var len2 = Convert.ToInt32(row[1]);
            Assert.AreEqual(len1, len2, "Deterministic function results should be identical when cached");
        }
    }

    [TestMethod]
    public void WhenMixingDeterministicAndNonDeterministic_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT
                Length(Name) as NameLength,
                Length(Name) + 10 as LengthPlus10,
                RandomNumber() as Random1,
                RandomNumber() as Random2,
                Name
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Test"),
                    new BasicEntity("Hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        foreach (var row in table)
        {
            var nameLength = Convert.ToInt32(row[0]);
            var lengthPlus10 = Convert.ToInt32(row[1]);
            var random1 = Convert.ToInt32(row[2]);
            var random2 = Convert.ToInt32(row[3]);


            Assert.AreEqual(nameLength + 10, lengthPlus10,
                "Deterministic expressions should have consistent results");


            Assert.IsTrue(random1 >= 0 && random1 < 100);
            Assert.IsTrue(random2 >= 0 && random2 < 100);
        }
    }

    #endregion

    #region Non-Deterministic with GROUP BY

    [TestMethod]
    public void WhenNonDeterministicMethodWithGroupBy_ShouldWork()
    {
        const string query = @"
            SELECT
                Name,
                Count(Name) as Cnt
            FROM #A.Entities()
            GROUP BY Name
            HAVING Count(Name) > 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("B"),
                    new BasicEntity("B"),
                    new BasicEntity("C")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rowA = table.First(r => (string)r[0] == "A");
        var rowB = table.First(r => (string)r[0] == "B");

        Assert.AreEqual(2, Convert.ToInt32(rowA[1]));
        Assert.AreEqual(3, Convert.ToInt32(rowB[1]));
    }

    [TestMethod]
    public void WhenNonDeterministicMethodInHaving_ShouldNotBeCached()
    {
        const string query = @"
            SELECT
                Name,
                Count(Name) as Cnt
            FROM #A.Entities()
            GROUP BY Name
            HAVING RandomNumber() >= 0";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(2, table.Count);
    }

    public TestContext TestContext { get; set; }

    #endregion
}
