// ReSharper disable UnusedAutoPropertyAccessor.Local

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyCteTests : GenericEntityTestBase
{

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select
        a.City as City,
        a.Country as SourceCountry,
        a.Population as Population,
        b.Country as AppliedCountry,
        b.Money as Money,
        b.Month as Month
    from #schema.first() a cross apply #schema.second(a.Country) b
)
select City, SourceCountry, Population, AppliedCountry, Money, Month from p";

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
            ("City", typeof(string)), ("SourceCountry", typeof(string)),
            ("Population", typeof(int)), ("AppliedCountry", typeof(string)),
            ("Money", typeof(decimal)), ("Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Country1", 100, "Country1", 1000m, "January"],
            ["City1", "Country1", 100, "Country1", 2000m, "February"],
            ["City2", "Country1", 200, "Country1", 1000m, "January"],
            ["City2", "Country1", 200, "Country1", 2000m, "February"],
            ["City3", "Country2", 300, "Country2", 3000m, "March"]);
    }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesCte_ShouldPass()
    {
        const string query = @"
with p as (
    select 
        f.City as City, 
        f.Country as Country, 
        f.Population as Population 
    from #schema.first() f
)
select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from p a cross apply #schema.second(a.Country) b";

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
            ("a.City", typeof(string)), ("a.Country", typeof(string)),
            ("a.Population", typeof(int)), ("b.Country", typeof(string)),
            ("b.Money", typeof(decimal)), ("b.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["City1", "Country1", 100, "Country1", 1000m, "January"],
            ["City1", "Country1", 100, "Country1", 2000m, "February"],
            ["City2", "Country1", 200, "Country1", 1000m, "January"],
            ["City2", "Country1", 200, "Country1", 2000m, "February"],
            ["City3", "Country2", 300, "Country2", 3000m, "March"]);
    }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedSelfProperty_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.Name, b.Value from #schema.first() a cross apply a.Skills b
)
select Name, Value from p";

        var firstSource = new List<CrossApplyClass3>
        {
            new() { Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] },
            new() { Name = "Name2", Skills = ["Skill4", "Skill5", "Skill6"] },
            new() { Name = "Name3", Skills = ["Skill7", "Skill8", "Skill9"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Name1", "Skill1"], ["Name1", "Skill2"], ["Name1", "Skill3"],
            ["Name2", "Skill4"], ["Name2", "Skill5"], ["Name2", "Skill6"],
            ["Name3", "Skill7"], ["Name3", "Skill8"], ["Name3", "Skill9"]);
    }

    [TestMethod]
    public void WhenSchemaMethodCrossAppliedSelfProperty_UsesCte_ShouldPass()
    {
        const string query = @"
with first as (
    select a.Name as Name, a.Skills as Skills from #schema.first() a
)
select a.Name, b.Value from first a cross apply a.Skills b";

        var firstSource = new List<CrossApplyClass3>
        {
            new() { Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] },
            new() { Name = "Name2", Skills = ["Skill4", "Skill5", "Skill6"] },
            new() { Name = "Name3", Skills = ["Skill7", "Skill8", "Skill9"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("a.Name", typeof(string)), ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Name1", "Skill1"], ["Name1", "Skill2"], ["Name1", "Skill3"],
            ["Name2", "Skill4"], ["Name2", "Skill5"], ["Name2", "Skill6"],
            ["Name3", "Skill7"], ["Name3", "Skill8"], ["Name3", "Skill9"]);
    }

    [TestMethod]
    public void WhenCrossApplyComponentsMustInjectMultipleEntities_ShouldNotThrow()
    {
        var query = """
                    with first as (
                        select 
                            r.AggregateValues(r.Name) as Name1,
                            r.AggregateValues(r.Name) as Name2
                        from #schema.first() r
                        cross apply r.JustReturnArrayOfString() b
                        cross apply r.JustReturnArrayOfString() c
                        group by 'fake'
                    )
                    select
                        b.Name1,
                        b.Name2,
                        p.Value
                    from first b
                    inner join #schema.first() r on 1 = 1
                    cross apply r.MethodArrayOfStrings(r.TestMethodWithInjectEntityAndParameter(b.Name1), r.TestMethodWithInjectEntityAndParameter(b.Name2)) p
                    """;

        var firstSource = new List<CrossApplyClass4>
        {
            new() { Name = "Name1" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Name1", typeof(string)), ("b.Name2", typeof(string)), ("p.Value", typeof(string)));
        var aggregateResult = string.Join(",", Enumerable.Repeat("Name1", 9));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [aggregateResult, aggregateResult, aggregateResult],
            [aggregateResult, aggregateResult, aggregateResult]);
    }

    [TestMethod]
    public void WhenCrossApplyAndMethodWithDefaultParameterUsed_ShouldPass()
    {
        var query = """
                    select
                        p.Value,
                        np.Value
                    from #schema.first() sln
                    cross apply sln.Skills p
                    cross apply p.MethodArrayOfStringsWithDefaultParameter() np
                    """;

        var firstSource = new List<CrossApplyClass3>
        {
            new() { Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("p.Value", typeof(string)), ("np.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Skill1", "one"], ["Skill1", "two"],
            ["Skill2", "one"], ["Skill2", "two"],
            ["Skill3", "one"], ["Skill3", "two"]);
    }

    [TestMethod]
    public void WhenCrossApplyAndMethodWithExplicitParameterUsed_ShouldPass()
    {
        var query = """
                    select
                        p.Value,
                        np.Value
                    from #schema.first() sln
                    cross apply sln.Skills p
                    cross apply p.MethodArrayOfStringsWithDefaultParameter(true) np
                    """;

        var firstSource = new List<CrossApplyClass3>
        {
            new() { Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("p.Value", typeof(string)), ("np.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Skill1", "one"], ["Skill1", "two"],
            ["Skill2", "one"], ["Skill2", "two"],
            ["Skill3", "one"], ["Skill3", "two"]);
    }

    [TestMethod]
    public void WhenCrossApplyAndMethodWithOneParameterAndDefaultParameterUsed_ShouldPass()
    {
        var query = """
                    select
                        p.Value,
                        np.Value
                    from #schema.first() sln
                    cross apply sln.Skills p
                    cross apply p.MethodArrayOfStringsWithOneParamAndDefaultParameter('value') np
                    """;

        var firstSource = new List<CrossApplyClass3>
        {
            new() { Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("p.Value", typeof(string)), ("np.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Skill1", "one"], ["Skill1", "two"],
            ["Skill2", "one"], ["Skill2", "two"],
            ["Skill3", "one"], ["Skill3", "two"]);
    }

    [TestMethod]
    public void WhenCrossApplyAndMethodWithOneParameterAndExplicitParameterUsed_ShouldPass()
    {
        var query = """
                    select
                        p.Value,
                        np.Value
                    from #schema.first() sln
                    cross apply sln.Skills p
                    cross apply p.MethodArrayOfStringsWithOneParamAndDefaultParameter('value', true) np
                    """;

        var firstSource = new List<CrossApplyClass3>
        {
            new() { Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("p.Value", typeof(string)), ("np.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Skill1", "one"], ["Skill1", "two"],
            ["Skill2", "one"], ["Skill2", "two"],
            ["Skill3", "one"], ["Skill3", "two"]);
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
        public string Name { get; set; } = string.Empty;

        [BindablePropertyAsTable] public string[] Skills { get; set; } = [];
    }

    public class CrossApplyClass4
    {
        public string Name { get; set; } = string.Empty;
    }
}
