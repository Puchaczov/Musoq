// ReSharper disable UnusedAutoPropertyAccessor.Local
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplySelfPropertyTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void OuterApplyProperty_NoMatch_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Values = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["City1", null]);
    }

    [TestMethod]
    public void OuterApplyProperty_WithPrimitiveArray_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b";

        var firstSource = new List<OuterApplyClass1>
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", 1d],
            ["City2", 2d], ["City2", 3d],
            ["City3", 4d], ["City3", 5d], ["City3", 6d]);
    }

    [TestMethod]
    public void OuterApplyProperty_WithWhere_ShouldPass()
    {
        const string query =
            "select a.City, b.Value from #schema.first() a outer apply a.Values as b where b.Value >= 2";

        var firstSource = new List<OuterApplyClass1>
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City2", 2d], ["City2", 3d],
            ["City3", 4d], ["City3", 5d], ["City3", 6d]);
    }

    [TestMethod]
    public void OuterApplyProperty_WithGroupBy_ShouldPass()
    {
        const string query =
            "select a.City, a.Sum(b.Value) from #schema.first() a outer apply a.Values as b group by a.City";

        var firstSource = new List<OuterApplyClass1>
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("a.Sum(b.Value)", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", 1d], ["City2", 5d], ["City3", 15d]);
    }

    [TestMethod]
    public void OuterApplyProperty_WithPrimitiveList_ShouldPass()
    {
        const string query = "select a.City, b.Value from #schema.first() a outer apply a.Values as b";

        var firstSource = new List<OuterApplyClass2>
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", 1d],
            ["City2", 2d], ["City2", 3d],
            ["City3", 4d], ["City3", 5d], ["City3", 6d]);
    }

    [TestMethod]
    public void OuterApplyProperty_WithComplexArray_ShouldPass()
    {
        const string query = "select a.City, b.Value1, b.Value2 from #schema.first() a outer apply a.Values as b";

        var firstSource = new List<OuterApplyClass3>
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value1", typeof(string)),
            ("b.Value2", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Value1", 1],
            ["City2", "Value2", 2], ["City2", "Value3", 3],
            ["City3", "Value4", 4], ["City3", "Value5", 5], ["City3", "Value6", 6]);
    }

    [TestMethod]
    public void OuterApplyProperty_WithComplexList_ShouldPass()
    {
        const string query = "select a.City, b.Value1, b.Value2 from #schema.first() a outer apply a.Values as b";

        var firstSource = new List<OuterApplyClass4>
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value1", typeof(string)),
            ("b.Value2", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Value1", 1],
            ["City2", "Value2", 2], ["City2", "Value3", 3],
            ["City3", "Value4", 4], ["City3", "Value5", 5], ["City3", "Value6", 6]);
    }

    [TestMethod]
    public void OuterApplyProperty_MultiplePrimitiveArrays_ShouldPass()
    {
        const string query =
            "select b.Value, c.Value from #schema.first() a outer apply a.Values1 as b outer apply a.Values2 as c";

        var firstSource = new List<OuterApplyClass5>
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


        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Value", typeof(double?)),
            ("c.Value", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1d, 1.1d],
            [2d, 2.1d], [2d, 2.2d], [2d, 3.3d],
            [3d, 2.1d], [3d, 2.2d], [3d, 3.3d],
            [4d, 4.1d], [4d, 5.1d], [4d, 6.1d],
            [5d, 4.1d], [5d, 5.1d], [5d, 6.1d],
            [6d, 4.1d], [6d, 5.1d], [6d, 6.1d]);
    }

    [TestMethod]
    public void WhenApplyChainedProperty_WithPrimitiveList_ShouldPass()
    {
        const string query = """
                             select
                                b.Value
                             from #schema.first() a
                             outer apply a.ComplexType.PrimitiveValues as b
                             """;

        var firstSource = new List<OuterApplyClass7>
        {
            new()
            {
                ComplexType = new ComplexType5
                {
                    PrimitiveValues = { 1, 2 }
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [2]);
    }

    [TestMethod]
    public void WhenApplyChainedProperty_WithComplexList_ShouldPass()
    {
        const string query = """
                             select
                                b.Value
                             from #schema.first() a
                             outer apply a.ComplexType.ComplexValues as b
                             """;

        var firstSource = new List<OuterApplyClass7>
        {
            new()
            {
                ComplexType = new ComplexType5
                {
                    ComplexValues = { new ComplexType6 { Value = 1 }, new ComplexType6 { Value = 2 } }
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [2]);
    }

    public sealed class OuterApplyClass1
    {
        public string City { get; set; } = string.Empty;

        public double[] Values { get; set; } = [];
    }

    public sealed class OuterApplyClass2
    {
        public string City { get; set; } = string.Empty;

        public List<double> Values { get; set; } = [];
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType1
    {
        public string Value1 { get; set; } = string.Empty;

        public int Value2 { get; set; }
    }

    public sealed class OuterApplyClass3
    {
        public string City { get; set; } = string.Empty;

        [BindablePropertyAsTable] public ComplexType1[] Values { get; set; } = [];
    }

    public sealed class OuterApplyClass4
    {
        public string City { get; set; } = string.Empty;

        [BindablePropertyAsTable] public List<ComplexType1> Values { get; set; } = [];
    }

    public sealed class OuterApplyClass5
    {
        public string City { get; set; } = string.Empty;

        public double[] Values1 { get; set; } = [];

        public double[] Values2 { get; set; } = [];
    }

    public sealed class OuterApplyClass7
    {
        public ComplexType5? ComplexType { get; set; }
    }

    public class ComplexType5
    {
        [BindablePropertyAsTable] public List<int> PrimitiveValues { get; } = [];

        [BindablePropertyAsTable] public List<ComplexType6> ComplexValues { get; } = [];
    }

    public class ComplexType6
    {
        public int Value { get; set; }
    }
}
