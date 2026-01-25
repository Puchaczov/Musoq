using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplySelfPropertyTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void CrossApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Values = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void CrossApplyProperty_WithPrimitiveArray_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Values = [1] },
            new() { City = "City2", Values = [2, 3] },
            new() { City = "City3", Values = [4, 5, 6] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(6, table.Count, "Table should have 6 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "City1" &&
            Math.Abs((double)entry.Values[1] - 1d) < 0.0001
        ), "First entry should be City1 with value 1");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "City2" &&
            Math.Abs((double)entry.Values[1] - 2d) < 0.0001
        ), "First City2 entry should be City2 with value 2");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "City2" &&
            Math.Abs((double)entry.Values[1] - 3d) < 0.0001
        ), "Second City2 entry should be City2 with value 3");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "City3" &&
            Math.Abs((double)entry.Values[1] - 4d) < 0.0001
        ), "First City3 entry should be City3 with value 4");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "City3" &&
            Math.Abs((double)entry.Values[1] - 5d) < 0.0001
        ), "Second City3 entry should be City3 with value 5");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "City3" &&
            Math.Abs((double)entry.Values[1] - 6d) < 0.0001
        ), "Third City3 entry should be City3 with value 6");
    }

    [TestMethod]
    public void CrossApplyProperty_WithPrimitiveList_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass2>
        {
            new() { City = "City1", Values = [1] },
            new() { City = "City2", Values = [2, 3] },
            new() { City = "City3", Values = [4, 5, 6] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(6, table.Count, "Table should have 6 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City1" &&
                Math.Abs(Convert.ToDouble(entry.Values[1]) - 1d) < 0.0001),
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City2" &&
                Math.Abs(Convert.ToDouble(entry.Values[1]) - 2d) < 0.0001),
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City2" &&
                Math.Abs(Convert.ToDouble(entry.Values[1]) - 3d) < 0.0001),
            "Third entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City3" &&
                Math.Abs(Convert.ToDouble(entry.Values[1]) - 4d) < 0.0001),
            "Fourth entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City3" &&
                Math.Abs(Convert.ToDouble(entry.Values[1]) - 5d) < 0.0001),
            "Fifth entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City3" &&
                Math.Abs(Convert.ToDouble(entry.Values[1]) - 6d) < 0.0001),
            "Sixth entry should match expected values");
    }

    [TestMethod]
    public void CrossApplyProperty_WithComplexArray_ShouldPass()
    {
        const string query = "select a.City, b.Value1, b.Value2 from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass3>
        {
            new() { City = "City1", Values = [new ComplexType1 { Value1 = "Value1", Value2 = 1 }] },
            new()
            {
                City = "City2",
                Values =
                [
                    new ComplexType1 { Value1 = "Value2", Value2 = 2 },
                    new ComplexType1 { Value1 = "Value3", Value2 = 3 }
                ]
            },
            new()
            {
                City = "City3",
                Values =
                [
                    new ComplexType1 { Value1 = "Value4", Value2 = 4 },
                    new ComplexType1 { Value1 = "Value5", Value2 = 5 },
                    new ComplexType1 { Value1 = "Value6", Value2 = 6 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Value1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Value2", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual(6, table.Count, "Table should contain 6 rows");

        Assert.AreEqual(1,
            table.Count(row =>
                (string)row.Values[0] == "City1" &&
                (string)row.Values[1] == "Value1" &&
                (int)row.Values[2] == 1), "Expected data for City1 not found");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row.Values[0] == "City2" &&
                new[] { "Value2", "Value3" }.Contains((string)row.Values[1]) &&
                ((int)row.Values[2] == 2 || (int)row.Values[2] == 3)), "Expected data for City2 not found");

        Assert.AreEqual(3,
            table.Count(row =>
                (string)row.Values[0] == "City3" &&
                new[] { "Value4", "Value5", "Value6" }.Contains((string)row.Values[1]) &&
                ((int)row.Values[2] == 4 || (int)row.Values[2] == 5 || (int)row.Values[2] == 6)),
            "Expected data for City3 not found");
    }

    [TestMethod]
    public void CrossApplyProperty_WithComplexList_ShouldPass()
    {
        const string query = "select a.City, b.Value1, b.Value2 from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass4>
        {
            new() { City = "City1", Values = [new ComplexType1 { Value1 = "Value1", Value2 = 1 }] },
            new()
            {
                City = "City2",
                Values =
                [
                    new ComplexType1 { Value1 = "Value2", Value2 = 2 },
                    new ComplexType1 { Value1 = "Value3", Value2 = 3 }
                ]
            },
            new()
            {
                City = "City3",
                Values =
                [
                    new ComplexType1 { Value1 = "Value4", Value2 = 4 },
                    new ComplexType1 { Value1 = "Value5", Value2 = 5 },
                    new ComplexType1 { Value1 = "Value6", Value2 = 6 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Value1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Value2", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual(6, table.Count, "Table should have 6 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City1" &&
                (string)entry.Values[1] == "Value1" &&
                Convert.ToInt32(entry.Values[2]) == 1),
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City2" &&
                (string)entry.Values[1] == "Value2" &&
                Convert.ToInt32(entry.Values[2]) == 2),
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City2" &&
                (string)entry.Values[1] == "Value3" &&
                Convert.ToInt32(entry.Values[2]) == 3),
            "Third entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City3" &&
                (string)entry.Values[1] == "Value4" &&
                Convert.ToInt32(entry.Values[2]) == 4),
            "Fourth entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City3" &&
                (string)entry.Values[1] == "Value5" &&
                Convert.ToInt32(entry.Values[2]) == 5),
            "Fifth entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "City3" &&
                (string)entry.Values[1] == "Value6" &&
                Convert.ToInt32(entry.Values[2]) == 6),
            "Sixth entry should match expected values");
    }

    [TestMethod]
    public void CrossApplyProperty_MultiplePrimitiveArrays_ShouldPass()
    {
        const string query =
            "select b.Value, c.Value from #schema.first() a cross apply a.Values1 as b cross apply a.Values2 as c";

        var firstSource = new List<CrossApplyClass5>
        {
            new() { City = "City1", Values1 = [1], Values2 = [1.1] },
            new() { City = "City2", Values1 = [2, 3], Values2 = [2.1, 2.2, 3.3] },
            new() { City = "City3", Values1 = [4, 5, 6], Values2 = [4.1, 5.1, 6.1] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("b.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(double), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(double), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(16, table.Count, "Result should contain exactly 16 value pairs");

        var actualPairs = table
            .Select(row => (First: (double)row.Values[0], Second: (double)row.Values[1]))
            .ToList();

        var expectedPairs = new List<(double First, double Second)>
        {
            (1.0, 1.1),


            (2.0, 2.1),
            (2.0, 2.2),
            (2.0, 3.3),


            (3.0, 2.1),
            (3.0, 2.2),
            (3.0, 3.3),


            (4.0, 4.1),
            (4.0, 5.1),
            (4.0, 6.1),


            (5.0, 4.1),
            (5.0, 5.1),
            (5.0, 6.1),


            (6.0, 4.1),
            (6.0, 5.1),
            (6.0, 6.1)
        };

        foreach (var expected in expectedPairs)
        {
            var matchCount = actualPairs.Count(actual =>
                Math.Abs(actual.First - expected.First) < 0.0001 &&
                Math.Abs(actual.Second - expected.Second) < 0.0001
            );

            Assert.AreEqual(1, matchCount,
                $"Pair ({expected.First}, {expected.Second}) should appear exactly once");
        }

        var firstValues = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };

        foreach (var firstValue in firstValues)
        {
            var expectedCount = Math.Abs(firstValue - 1.0) < 0.00001 ? 1 : 3;
            var actualCount = actualPairs.Count(p => Math.Abs(p.First - firstValue) < 0.0001);

            Assert.AreEqual(expectedCount, actualCount,
                $"First value {firstValue} should appear {expectedCount} times");
        }
    }

    [TestMethod]
    public void CrossApplyProperty_MultipleComplexArrays_ShouldPass()
    {
        const string query =
            "select d.Value from #schema.first() a cross apply a.Values as b cross apply b.Values as c cross apply c.Values as d";

        var firstSource = new List<CrossApplyClass6>
        {
            new()
            {
                Values =
                [
                    new ComplexType2
                    {
                        Values =
                        [
                            new ComplexType3
                            {
                                Values = [new ComplexType4 { Value = "Value1" }, new ComplexType4 { Value = "Value2" }]
                            },
                            new ComplexType3
                            {
                                Values = [new ComplexType4 { Value = "Value3" }, new ComplexType4 { Value = "Value4" }]
                            }
                        ]
                    },

                    new ComplexType2
                    {
                        Values =
                        [
                            new ComplexType3
                            {
                                Values = [new ComplexType4 { Value = "Value5" }, new ComplexType4 { Value = "Value6" }]
                            },
                            new ComplexType3
                            {
                                Values = [new ComplexType4 { Value = "Value7" }, new ComplexType4 { Value = "Value8" }]
                            }
                        ]
                    }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("d.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(8, table.Count, "Result should contain exactly 8 values");

        var actualValues = table
            .Select(row => row.Values[0])
            .ToList();

        var expectedValues = Enumerable.Range(1, 8)
            .Select(i => $"Value{i}")
            .ToList();

        foreach (var expectedValue in expectedValues)
            Assert.AreEqual(
                1,
                actualValues.Count(value => (string)value == expectedValue),
                $"Value '{expectedValue}' should appear exactly once in the results"
            );

        foreach (var actualValue in actualValues)
            Assert.IsTrue(
                expectedValues.Contains(actualValue),
                $"Found unexpected value '{actualValue}' in results. Only values following the pattern 'Value1' through 'Value8' should be present."
            );
    }

    [TestMethod]
    public void CrossApplyProperty_AliasClashingWithCte_ShouldThrow()
    {
        const string query =
            """
            with a as (
                select 1 from #schema.first()
            )
            select d.Value from #schema.first() a cross apply a.Values as b cross apply b.Values as c cross apply c.Values as d
            """;

        var firstSource = new List<CrossApplyClass6>().ToArray();

        Assert.Throws<AliasAlreadyUsedException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(
                query,
                firstSource
            );

            vm.Run(TestContext.CancellationToken);
        });
    }

    [TestMethod]
    public void WhenApplyChainedProperty_WithPrimitiveList_ShouldPass()
    {
        const string query = """
                             select
                                b.Value
                             from #schema.first() a
                             cross apply a.ComplexType.PrimitiveValues as b
                             """;

        var firstSource = new List<CrossApplyClass7>
        {
            new()
            {
                ComplexType = new ComplexType5
                {
                    PrimitiveValues = [1, 2]
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");
        Assert.IsTrue(table.Any(row => (int)row.Values[0] == 1) && table.Any(row => (int)row.Values[0] == 2),
            "Expected values 1 and 2 not found");
    }

    [TestMethod]
    public void WhenApplyChainedProperty_WithComplexList_ShouldPass()
    {
        const string query = """
                             select
                                b.Value
                             from #schema.first() a
                             cross apply a.ComplexType.ComplexValues as b
                             """;

        var firstSource = new List<CrossApplyClass7>
        {
            new()
            {
                ComplexType = new ComplexType5
                {
                    ComplexValues = [new ComplexType6 { Value = 1 }, new ComplexType6 { Value = 2 }]
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 1), "First entry should be 1");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 2), "Second entry should be 2");
    }

    [TestMethod]
    public void WhenGroupByAndOrderByWithAccessMethod_ShouldPass()
    {
        const string query = """
                             select
                                b.GetTypeName(b.Value)
                             from #schema.first() a
                             cross apply a.ComplexType.ComplexValues as b
                             group by b.GetTypeName(b.Value)
                             order by b.GetTypeName(b.Value)
                             """;

        var firstSource = new List<CrossApplyClass7>
        {
            new()
            {
                ComplexType = new ComplexType5
                {
                    ComplexValues = [new ComplexType6 { Value = 1 }, new ComplexType6 { Value = 2 }]
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("System.Int32", table[0].Values[0]);
    }

    private class CrossApplyClass1
    {
        public string City { get; set; }

        public double[] Values { get; set; }
    }

    private class CrossApplyClass2
    {
        public string City { get; set; }

        public List<double> Values { get; set; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType1
    {
        public string Value1 { get; set; }

        public int Value2 { get; set; }
    }

    private class CrossApplyClass3
    {
        public string City { get; set; }

        [BindablePropertyAsTable] public ComplexType1[] Values { get; set; }
    }

    private class CrossApplyClass4
    {
        public string City { get; set; }

        [BindablePropertyAsTable] public List<ComplexType1> Values { get; set; }
    }

    private class CrossApplyClass5
    {
        public string City { get; set; }

        public double[] Values1 { get; set; }

        public double[] Values2 { get; set; }
    }

    public class ComplexType4
    {
        public string Value { get; set; }
    }

    public class ComplexType3
    {
        public List<ComplexType4> Values { get; set; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType2
    {
        [BindablePropertyAsTable] public List<ComplexType3> Values { get; set; }
    }

    private class CrossApplyClass6
    {
        [BindablePropertyAsTable] public List<ComplexType2> Values { get; set; }
    }

    private class CrossApplyClass7
    {
        public ComplexType5 ComplexType { get; set; }
    }

    public class ComplexType5
    {
        [BindablePropertyAsTable] public List<int> PrimitiveValues { get; set; }

        [BindablePropertyAsTable] public List<ComplexType6> ComplexValues { get; set; }
    }

    public class ComplexType6
    {
        public int Value { get; set; }
    }
}
