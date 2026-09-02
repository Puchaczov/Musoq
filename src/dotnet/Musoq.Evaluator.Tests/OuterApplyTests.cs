// ReSharper disable UnusedAutoPropertyAccessor.Local
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyTests : GenericEntityTestBase
{

    [TestMethod]
    public void OuterApply_NoMatchesShouldReturnNull_ShouldPass()
    {
        const string query =
            "select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<OuterApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
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
            ("b.Money", typeof(decimal?)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Country1", 100, "Country1", 1000m, "January"],
            ["City2", "Country1", 200, "Country1", 1000m, "January"],
            ["City3", "Country2", 300, null, null, null]);
    }

    [TestMethod]
    public void OuterApply_MultipleMatches_ShouldPass()
    {
        const string query =
            "select a.City, a.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 }
        }.ToArray();

        var secondSource = new List<OuterApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" }
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
            ("b.Money", typeof(decimal?)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Country1", 1000m, "January"],
            ["City1", "Country1", 2000m, "February"],
            ["City2", "Country1", 1000m, "January"],
            ["City2", "Country1", 2000m, "February"]);
    }

    [TestMethod]
    public void OuterApply_NoMatches_ShouldPass()
    {
        const string query =
            "select a.City, a.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<OuterApplyClass2>
        {
            new() { Country = "Country2", Money = 1000, Month = "January" }
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
            ("b.Money", typeof(decimal?)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", "Country1", null, null]);
    }

    [TestMethod]
    public void OuterApply_TripleApply_ShouldPass()
    {
        const string query = @"
            select a.City, a.Country, b.Money, b.Month, c.Address 
            from #schema.first() a 
            outer apply #schema.second(a.Country) b
            outer apply #schema.third(a.Country) c";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country2", Population = 200 }
        }.ToArray();

        var secondSource = new List<OuterApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country2", Money = 2000, Month = "February" }
        }.ToArray();

        var thirdSource = new List<OuterApplyClass3>
        {
            new() { Country = "Country1", Address = "Address1" },
            new() { Country = "Country3", Address = "Address3" }
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
            ("a.City", typeof(string)),
            ("a.Country", typeof(string)),
            ("b.Money", typeof(decimal?)),
            ("b.Month", typeof(string)),
            ("c.Address", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Country1", 1000m, "January", "Address1"],
            ["City2", "Country2", 2000m, "February", null]);
    }

    [TestMethod]
    public void OuterApply_WithAggregation_ShouldPass()
    {
        const string query = @"
            select a.Country, b.Sum(b.Money) as TotalMoney, b.Count(b.Money) as TransactionCount
            from #schema.first() a 
            outer apply #schema.second(a.Country) b
            group by a.Country";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 },
            new() { City = "City4", Country = "Country3", Population = 400 }
        }.ToArray();

        var secondSource = new List<OuterApplyClass2>
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
            ("a.Country", typeof(string)),
            ("TotalMoney", typeof(decimal?)),
            ("TransactionCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Country1", 6000m, 4L],
            ["Country2", 3000m, 1L],
            ["Country3", null, 0L]);
    }

    [TestMethod]
    public void OuterApply_WithWhereClause_ShouldPass()
    {
        const string query = @"
            select a.City, a.Country, b.Money, b.Month 
            from #schema.first() a 
            outer apply #schema.second(a.Country) b
            where b.Money > 1500 or b.Money is null";

        var firstSource = new List<OuterApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country2", Population = 200 },
            new() { City = "City3", Country = "Country3", Population = 300 }
        }.ToArray();

        var secondSource = new List<OuterApplyClass2>
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
            ("b.Money", typeof(decimal?)),
            ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Country1", 2000m, "February"],
            ["City2", "Country2", 3000m, "March"],
            ["City3", "Country3", null, null]);
    }

    public sealed class OuterApplyClass1
    {
        public string City { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public int Population { get; set; }
    }

    public sealed class OuterApplyClass2
    {
        public string Country { get; set; } = string.Empty;

        public decimal Money { get; set; }

        public string Month { get; set; } = string.Empty;
    }

    public sealed class OuterApplyClass3
    {
        public string Country { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
