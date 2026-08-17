// ReSharper disable UnusedAutoPropertyAccessor.Local

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", 1d],
            ["City2", 2d],
            ["City2", 3d],
            ["City3", 4d],
            ["City3", 5d],
            ["City3", 6d]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", 1d],
            ["City2", 2d],
            ["City2", 3d],
            ["City3", 4d],
            ["City3", 5d],
            ["City3", 6d]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value1", typeof(string)),
            ("b.Value2", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Value1", 1],
            ["City2", "Value2", 2],
            ["City2", "Value3", 3],
            ["City3", "Value4", 4],
            ["City3", "Value5", 5],
            ["City3", "Value6", 6]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value1", typeof(string)),
            ("b.Value2", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Value1", 1],
            ["City2", "Value2", 2],
            ["City2", "Value3", 3],
            ["City3", "Value4", 4],
            ["City3", "Value5", 5],
            ["City3", "Value6", 6]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Value", typeof(double)),
            ("c.Value", typeof(double)));
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
    public void CrossApplyProperty_MultipleComplexArrays_ShouldPass()
    {
        const string query =
            "select d.Value from #schema.first() a cross apply a.Values as b cross apply b.Values as c cross apply c.Values as d";

        var firstSource = new List<CrossApplyClass6>
        {
            new()
            {
                Values =
                {
                    new ComplexType2
                    {
                        Values =
                        {
                            new ComplexType3
                            {
                                Values = { new ComplexType4 { Value = "Value1" }, new ComplexType4 { Value = "Value2" } }
                            },
                            new ComplexType3
                            {
                                Values = { new ComplexType4 { Value = "Value3" }, new ComplexType4 { Value = "Value4" } }
                            }
                        }
                    },

                    new ComplexType2
                    {
                        Values =
                        {
                            new ComplexType3
                            {
                                Values = { new ComplexType4 { Value = "Value5" }, new ComplexType4 { Value = "Value6" } }
                            },
                            new ComplexType3
                            {
                                Values = { new ComplexType4 { Value = "Value7" }, new ComplexType4 { Value = "Value8" } }
                            }
                        }
                    }
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("d.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Value1"], ["Value2"], ["Value3"], ["Value4"],
            ["Value5"], ["Value6"], ["Value7"], ["Value8"]);
    }

    [TestMethod]
    public void CrossApplyProperty_AliasMatchingUnusedCte_ShouldPass()
    {
        const string query =
            """
            with a as (
                select 1 from #schema.first()
            )
            select d.Value from #schema.first() a cross apply a.Values as b cross apply b.Values as c cross apply c.Values as d
            """;

        var firstSource = new List<CrossApplyClass6>().ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("d.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
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
                    PrimitiveValues = { 1, 2 }
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [2]);
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
                    ComplexValues = { new ComplexType6 { Value = 1 }, new ComplexType6 { Value = 2 } }
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [2]);
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
                    ComplexValues = { new ComplexType6 { Value = 1 }, new ComplexType6 { Value = 2 } }
                }
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.GetTypeName(b.Value)", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["System.Int32"]);
    }

    public sealed class CrossApplyClass1
    {
        public string City { get; set; } = string.Empty;

        public double[] Values { get; set; } = [];
    }

    public sealed class CrossApplyClass2
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

    public sealed class CrossApplyClass3
    {
        public string City { get; set; } = string.Empty;

        [BindablePropertyAsTable] public ComplexType1[] Values { get; set; } = [];
    }

    public sealed class CrossApplyClass4
    {
        public string City { get; set; } = string.Empty;

        [BindablePropertyAsTable] public List<ComplexType1> Values { get; set; } = [];
    }

    public sealed class CrossApplyClass5
    {
        public string City { get; set; } = string.Empty;

        public double[] Values1 { get; set; } = [];
        public double[] Values2 { get; set; } = [];
    }

    public class ComplexType4
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ComplexType3
    {
        public List<ComplexType4> Values { get; } = [];
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType2
    {
        [BindablePropertyAsTable] public List<ComplexType3> Values { get; } = [];
    }

    public sealed class CrossApplyClass6
    {
        [BindablePropertyAsTable] public List<ComplexType2> Values { get; set; } = [];
    }

    public sealed class CrossApplyClass7
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
