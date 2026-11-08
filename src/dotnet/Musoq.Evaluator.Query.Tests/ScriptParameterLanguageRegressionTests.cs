using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class ScriptParameterLanguageRegressionTests : BasicEntityTestBase
{

    [TestMethod]
    public void WhenParametersAreUsedInCteBodyAndConsumer_ShouldRun()
    {
        const string query = @"
            param(country: string, city: string)
            with filtered as (
                select Name, City, Country from #A.Entities() a where a.Country = $country
            )
            select Name from filtered where City = $city";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "country", "PL");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "city", "Warsaw");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenParametersAreUsedInJoinPredicateAndProjection_ShouldRun()
    {
        const string query = @"
            param(country: string, suffix: string = '!')
            select a.Name + $suffix, b.Name
            from #A.Entities() a
            inner join #B.Entities() b on a.City = b.City and b.Country = $country
            where a.Country = $country";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "country", "PL");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "suffix", "?");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice?", table[0].ReadValue(0));
        Assert.AreEqual("TargetWarsaw", table[0].ReadValue(1));
    }

    [TestMethod]
    public void WhenParametersAreUsedInGroupByAndHaving_ShouldRun()
    {
        const string query = @"
            param(suffix: string, minCount: int)
            select Country + $suffix, Count(Name)
            from #A.Entities()
            group by Country + $suffix
            having Count(Name) >= $minCount";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "suffix", "-group");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "minCount", 2);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PL-group", table[0].ReadValue(0));
        Assert.AreEqual(2, Convert.ToInt32(table[0].ReadValue(1)));
    }

    [TestMethod]
    public void WhenParametersAreUsedInValueTupleGroupByAndHaving_ShouldRun()
    {
        const string query = @"
            param(countrySuffix: string, citySuffix: string, minCount: int)
            select Country + $countrySuffix, City + $citySuffix, Count(Name)
            from #A.Entities()
            group by Country + $countrySuffix, City + $citySuffix
            having Count(Name) >= $minCount";

        var vm = CreateAndRunVirtualMachine(query, CreateValueTupleAggregateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "countrySuffix", "-country");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "citySuffix", "-city");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "minCount", 2);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PL-country", table[0].ReadValue(0));
        Assert.AreEqual("Warsaw-city", table[0].ReadValue(1));
        Assert.AreEqual(2, Convert.ToInt32(table[0].ReadValue(2)));
    }

    [TestMethod]
    public void WhenParametersAreUsedInParallelCteBlock_ShouldRun()
    {
        const string query = @"
            param(country: string, city: string)
            with filteredA as (
                select Name, $country as JoinKey
                from #A.Entities()
                where Country = $country
            ),
            filteredB as (
                select Name, $country as JoinKey, $city as RequestedCity
                from #B.Entities()
                where City = $city
            )
            select a.Name, b.Name, b.RequestedCity
            from filteredA a
            inner join filteredB b on a.JoinKey = b.JoinKey
            order by a.Name, b.Name";
        var sources = CreateSources();
        var options = new CompilationOptions(usePrimitiveTypeValidation: false, useCteParallelization: true);
        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            options);

        Assert.Contains("ParallelBlock", inspection.ExecutionPlanText, inspection.PlanningText);

        var vm = CreateAndRunVirtualMachine(query, sources, options);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "country", "PL");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "city", "Warsaw");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValue(0));
        Assert.AreEqual("TargetWarsaw", table[0].ReadValue(1));
        Assert.AreEqual("Warsaw", table[0].ReadValue(2));
        Assert.AreEqual("Cara", table[1].ReadValue(0));
        Assert.AreEqual("TargetWarsaw", table[1].ReadValue(1));
        Assert.AreEqual("Warsaw", table[1].ReadValue(2));
    }

    [TestMethod]
    public void WhenParametersAreUsedWithOrderByAndPagination_ShouldRun()
    {
        const string query = @"
            param(country: string)
            select Name
            from #A.Entities()
            where Country in ($country, 'DE')
            order by case when Country = $country then 0 else 1 end, Name
            skip 1
            take 1";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "country", "PL");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Cara", table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenParametersAreUsedInCaseWhen_ShouldRun()
    {
        const string query = @"
            param(minPopulation: decimal, label: string)
            select Name, case when Population >= $minPopulation then $label else 'small' end
            from #A.Entities()
            order by Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "minPopulation", 100m);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "label", "large");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("large", table.Single(row => (string)row.ReadValues()[0] == "Alice").ReadValues()[1]);
        Assert.AreEqual("small", table.Single(row => (string)row.ReadValues()[0] == "Bob").ReadValues()[1]);
        Assert.AreEqual("large", table.Single(row => (string)row.ReadValues()[0] == "Cara").ReadValues()[1]);
    }

    [TestMethod]
    public void WhenParametersAreUsedInInList_ShouldRun()
    {
        const string query = @"
            param(firstCity: string, secondCity: string)
            select Name from #A.Entities()
            where City in ($firstCity, $secondCity)
            order by Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "firstCity", "Warsaw");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "secondCity", "Berlin");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValue(0));
        Assert.AreEqual("Bob", table[1].ReadValue(0));
    }

    [TestMethod]
    public void WhenArrayParameterIsUsedInInPredicate_ShouldRun()
    {
        const string query = @"
            param(ids: int[])
            select Name from #A.Entities()
            where Id in $ids
            order by Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSourcesWithIds());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "ids", new[] { 1, 3 });

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice"], ["Cara"]);
    }

    [TestMethod]
    public void WhenListParameterIsUsedInInPredicate_ShouldRun()
    {
        const string query = @"
            param(ids: int[])
            select Name from #A.Entities()
            where Id in $ids
            order by Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSourcesWithIds());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "ids", new List<int> { 2, 4 });

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Bob"], ["Dora"]);
    }

    [TestMethod]
    public void WhenReadOnlyListParameterIsUsedInInPredicate_ShouldRun()
    {
        const string query = @"
            param(ids: int[])
            select Name from #A.Entities()
            where Id in $ids";

        var vm = CreateAndRunVirtualMachine(query, CreateSourcesWithIds());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "ids", Array.AsReadOnly(new[] { 3 }));

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Cara"]);
    }

    [TestMethod]
    public void WhenStringArrayParameterIsUsedInInPredicate_ShouldRun()
    {
        const string query = @"
            param(cities: string[])
            select Name from #A.Entities()
            where City in $cities
            order by Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSourcesWithIds());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "cities", new[] { "Warsaw", "Paris" });

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice"], ["Dora"]);
    }

    [TestMethod]
    public void WhenArrayParameterIsUsedInNotInPredicate_ShouldRun()
    {
        const string query = @"
            param(ids: int[])
            select Name from #A.Entities()
            where Id not in $ids
            order by Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSourcesWithIds());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "ids", new[] { 1, 3 });

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Bob"], ["Dora"]);
    }

    [TestMethod]
    public void WhenParametersAreUsedInWindowPartitionOrderAndProjection_ShouldRun()
    {
        const string query = @"
            param(country: string, suffix: string)
            select Name,
                   RowNumber() over (
                       partition by case when Country = $country then $country else Country end
                       order by Name + $suffix
                   ) as rn,
                   $suffix
            from #A.Entities()
            where Country in ($country, 'DE')";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "country", "PL");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "suffix", "-window");

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, RowNumberFor(table, "Alice"));
        Assert.AreEqual(2, RowNumberFor(table, "Cara"));
        Assert.AreEqual(1, RowNumberFor(table, "Bob"));
        Assert.IsTrue(table.All(row => (string)row.ReadValues()[2] == "-window"));
    }

    private static int RowNumberFor(Table table, string name)
    {
        return Convert.ToInt32(table.Single(row => (string)row.ReadValues()[0] == name).ReadValues()[1]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "Alice", Country = "PL", City = "Warsaw", Population = 120m },
                    new BasicEntity { Name = "Bob", Country = "DE", City = "Berlin", Population = 80m },
                    new BasicEntity { Name = "Cara", Country = "PL", City = "Krakow", Population = 200m },
                    new BasicEntity { Name = "Dora", Country = "FR", City = "Paris", Population = 50m }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "TargetWarsaw", Country = "PL", City = "Warsaw" },
                    new BasicEntity { Name = "TargetBerlin", Country = "DE", City = "Berlin" },
                    new BasicEntity { Name = "Other", Country = "US", City = "Austin" }
                ]
            }
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSourcesWithIds()
    {
        var sources = CreateSources();
        var rows = sources["#A"].ToArray();

        for (var index = 0; index < rows.Length; index++)
            rows[index].Id = index + 1;

        return sources;
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateValueTupleAggregateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "Alice", Country = "PL", City = "Warsaw", Population = 120m },
                    new BasicEntity { Name = "Ava", Country = "PL", City = "Warsaw", Population = 180m },
                    new BasicEntity { Name = "Bob", Country = "DE", City = "Berlin", Population = 80m }
                ]
            }
        };
    }
}
