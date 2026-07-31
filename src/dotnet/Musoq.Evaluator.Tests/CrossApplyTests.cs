// ReSharper disable UnusedAutoPropertyAccessor.Local
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_ShouldPass()
    {
        const string query =
            "select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("a.Country", typeof(string)),
            ("a.Population", typeof(int)),
            ("b.Country", typeof(string)),
            ("b.Money", typeof(decimal)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", "Country1", 100, "Country1", 1000m, "January"],
            ["City1", "Country1", 100, "Country1", 2000m, "February"],
            ["City2", "Country1", 200, "Country1", 1000m, "January"],
            ["City2", "Country1", 200, "Country1", 2000m, "February"],
            ["City3", "Country2", 300, "Country2", 3000m, "March"]);
    }

    [TestMethod]
    public void
        WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_UseOnlyValuesOfCrossApplySchemaMethod_ShouldPass()
    {
        const string query =
            "select b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Country", typeof(string)),
            ("b.Money", typeof(decimal)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Country1", 1000m, "January"],
            ["Country1", 2000m, "February"],
            ["Country1", 1000m, "January"],
            ["Country1", 2000m, "February"],
            ["Country2", 3000m, "March"]);
    }

    [TestMethod]
    public void
        WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_FilterWithAValue_ShouldPass()
    {
        const string query =
            "select b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b where a.Country = 'Country2'";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Country", typeof(string)),
            ("b.Money", typeof(decimal)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Country2", 3000m, "March"]);
    }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSameSchemas_UsesValuesOfSchemaMethodWithinTableValue_ShouldPass()
    {
        const string query =
            "select b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b cross apply #schema.third(b.Country) c";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass3>
        {
            new() { Country = "Country1", Address = "Address1" },
            new() { Country = "Country1", Address = "Address2" },
            new() { Country = "Country2", Address = "Address3" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource,
            null,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray(),
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Country", typeof(string)),
            ("b.Money", typeof(decimal)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Country1", 1000m, "January"],
            ["Country1", 1000m, "January"],
            ["Country1", 1000m, "January"],
            ["Country1", 1000m, "January"],
            ["Country1", 2000m, "February"],
            ["Country1", 2000m, "February"],
            ["Country1", 2000m, "February"],
            ["Country1", 2000m, "February"],
            ["Country2", 3000m, "March"]);
    }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSameSchemas_GroupedByCountry_ShouldPass()
    {
        const string query =
            "select b.Country from #schema.first() a cross apply #schema.second(a.Country) b cross apply #schema.third(b.Country) c group by b.Country";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass3>
        {
            new() { Country = "Country1", Address = "Address1" },
            new() { Country = "Country1", Address = "Address2" },
            new() { Country = "Country2", Address = "Address3" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource,
            null,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray(),
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Country1"],
            ["Country2"]);
    }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSameSchemas_WithFilterAndGroupBy_ShouldPass()
    {
        const string query =
            "select b.Country from #schema.first() a cross apply #schema.second(a.Country) b cross apply #schema.third(b.Country) c where b.Country = 'Country1' group by b.Country";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass3>
        {
            new() { Country = "Country1", Address = "Address1" },
            new() { Country = "Country1", Address = "Address2" },
            new() { Country = "Country2", Address = "Address3" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource,
            null,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray(),
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Country1"]);
    }

    [TestMethod]
    public void
        WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_UsedWithinCte_ShouldPass()
    {
        const string query =
            """
            with rows as (
                select b.Country as Country, b.Money as Money, b.Month as Month from #schema.first() a cross apply #schema.second(a.Country) b
            )
            select Country, Money, Month from rows as p
            """;

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Money", typeof(decimal)),
            ("Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Country1", 1000m, "January"],
            ["Country1", 2000m, "February"],
            ["Country1", 1000m, "January"],
            ["Country1", 2000m, "February"],
            ["Country2", 3000m, "March"]);
    }

    public sealed class CrossApplyClass1
    {
        public string City { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public int Population { get; set; }
    }

    public sealed class CrossApplyClass2
    {
        public string Country { get; set; } = string.Empty;

        public decimal Money { get; set; }

        public string Month { get; set; } = string.Empty;
    }

    public sealed class CrossApplyClass3
    {
        public string Country { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
