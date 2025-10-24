using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyCteTests : GenericEntityTestBase
{
    private class CrossApplyClass1
    {
        public string City { get; set; }
        
        public string Country { get; set; }
        
        public int Population { get; set; }
    }

    private class CrossApplyClass2
    {
        public string Country { get; set; }
        
        public decimal Money { get; set; }
        
        public string Month { get; set; }
    }
    
    private class CrossApplyClass3
    {
        public string Name { get; set; }
        
        [BindablePropertyAsTable]
        public string[] Skills { get; set; }
    }
    
    public class CrossApplyClass4
    {
        public string Name { get; set; }
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b
)
select [a.City], [a.Country], [a.Population], [b.Country], [b.Money], [b.Month] from p";
        
        var firstSource = new List<CrossApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<CrossApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
            new() {Country = "Country2", Money = 3000, Month = "March"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(6, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("a.Population", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("b.Country", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(5).ColumnType);
        
        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.AreEqual(2,
table.Count(row =>
                (string)row.Values[0] == "City1" &&
                (string)row.Values[1] == "Country1" &&
                (int)row.Values[2] == 100 &&
                (string)row.Values[3] == "Country1" &&
                ((decimal)row.Values[4] == 1000m || (decimal)row.Values[4] == 2000m)), "Expected data for City1 not found");

        Assert.AreEqual(2,
table.Count(row =>
                (string)row.Values[0] == "City2" &&
                (string)row.Values[1] == "Country1" &&
                (int)row.Values[2] == 200 &&
                (string)row.Values[3] == "Country1" &&
                ((decimal)row.Values[4] == 1000m || (decimal)row.Values[4] == 2000m)), "Expected data for City2 not found");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "City3" && 
                (string)row.Values[1] == "Country2" &&
                (int)row.Values[2] == 300 &&
                (string)row.Values[3] == "Country2" &&
                (decimal)row.Values[4] == 3000m),
            "Expected data for City3 not found");
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
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<CrossApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
            new() {Country = "Country2", Money = 3000, Month = "March"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(6, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("a.Population", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("b.Country", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(5).ColumnType);
        
        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.AreEqual(1,
table.Count(row =>
                (string)row.Values[0] == "City1" &&
                (string)row.Values[1] == "Country1" &&
                (int)row.Values[2] == 100 &&
                (string)row.Values[3] == "Country1" &&
                (decimal)row.Values[4] == 1000m), "Missing City1/Country1/100/Country1/1000 row");

        Assert.AreEqual(1,
table.Count(row =>
                (string)row.Values[0] == "City1" &&
                (string)row.Values[1] == "Country1" &&
                (int)row.Values[2] == 100 &&
                (string)row.Values[3] == "Country1" &&
                (decimal)row.Values[4] == 2000m), "Missing City1/Country1/100/Country1/2000 row");

        Assert.AreEqual(1,
table.Count(row =>
                (string)row.Values[0] == "City2" &&
                (string)row.Values[1] == "Country1" &&
                (int)row.Values[2] == 200 &&
                (string)row.Values[3] == "Country1" &&
                (decimal)row.Values[4] == 1000m), "Missing City2/Country1/200/Country1/1000 row");

        Assert.AreEqual(1,
table.Count(row =>
                (string)row.Values[0] == "City2" &&
                (string)row.Values[1] == "Country1" &&
                (int)row.Values[2] == 200 &&
                (string)row.Values[3] == "Country1" &&
                (decimal)row.Values[4] == 2000m), "Missing City2/Country1/200/Country1/2000 row");

        Assert.AreEqual(1,
table.Count(row =>
                (string)row.Values[0] == "City3" &&
                (string)row.Values[1] == "Country2" &&
                (int)row.Values[2] == 300 &&
                (string)row.Values[3] == "Country2" &&
                (decimal)row.Values[4] == 3000m), "Missing City3/Country2/300/Country2/3000 row");
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedSelfProperty_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.Name, b.Value from #schema.first() a cross apply a.Skills b
)
select [a.Name], [b.Value] from p";
        
        var firstSource = new List<CrossApplyClass3>
        {
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"]},
            new() {Name = "Name2", Skills = ["Skill4", "Skill5", "Skill6"]},
            new() {Name = "Name3", Skills = ["Skill7", "Skill8", "Skill9"]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        
        Assert.AreEqual("a.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("b.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(9, table.Count);

        var expectedPairs = new List<(string Name, string Skill)>
        {
            ("Name1", "Skill1"), ("Name1", "Skill2"), ("Name1", "Skill3"),
            ("Name2", "Skill4"), ("Name2", "Skill5"), ("Name2", "Skill6"),
            ("Name3", "Skill7"), ("Name3", "Skill8"), ("Name3", "Skill9")
        };

        var actualPairs = table
            .Select(row => (Name: row.Values[0], Skill: row.Values[1]))
            .ToList();

        foreach (var name in new[] { "Name1", "Name2", "Name3" })
        {
            // Get expected skills for this name
            var expectedSkills = expectedPairs
                .Where(p => p.Name == name)
                .Select(p => p.Skill)
                .ToList();

            // Get actual skills for this name
            var actualSkills = actualPairs
                .Where(p => (string) p.Name == name)
                .Select(p => p.Skill)
                .ToList();

            // Compare the skills
            CollectionAssert.AreEquivalent(
                expectedSkills,
                actualSkills,
                $"Skills for {name} do not match expected values"
            );

            // Verify count of appearances
            Assert.AreEqual(3, actualPairs.Count(p => (string)p.Name == name),
                $"{name} should appear exactly 3 times");
        }
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
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"]},
            new() {Name = "Name2", Skills = ["Skill4", "Skill5", "Skill6"]},
            new() {Name = "Name3", Skills = ["Skill7", "Skill8", "Skill9"]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        
        Assert.AreEqual("a.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("b.Value", table.Columns.ElementAt(1).ColumnName);
        
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);Assert.AreEqual(9, table.Count, "Table should contain 9 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name1" && (string)row.Values[1] == "Skill1"), "Missing Name1/Skill1 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name1" && (string)row.Values[1] == "Skill2"), "Missing Name1/Skill2 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name1" && (string)row.Values[1] == "Skill3"), "Missing Name1/Skill3 row");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name2" && (string)row.Values[1] == "Skill4"), "Missing Name2/Skill4 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name2" && (string)row.Values[1] == "Skill5"), "Missing Name2/Skill5 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name2" && (string)row.Values[1] == "Skill6"), "Missing Name2/Skill6 row");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name3" && (string)row.Values[1] == "Skill7"), "Missing Name3/Skill7 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name3" && (string)row.Values[1] == "Skill8"), "Missing Name3/Skill8 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Name3" && (string)row.Values[1] == "Skill9"), "Missing Name3/Skill9 row");
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
            new() {Name = "Name1"}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);

        try
        {
            vm.Run();
        }
        catch (Exception)
        {
            Assert.Fail($"Expected not to throw exception but got: ");
        }
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
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] },
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);

        var table = vm.Run();
            
        Assert.AreEqual(6, table.Count, "Table should contain 3 rows");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "one"), "Missing Skill1/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "two"), "Missing Skill1/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "one"), "Missing Skill2/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "two"), "Missing Skill2/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "one"), "Missing Skill3/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "two"), "Missing Skill3/two row");
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
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] },
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);

        var table = vm.Run();
            
        Assert.AreEqual(6, table.Count, "Table should contain 3 rows");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "one"), "Missing Skill1/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "two"), "Missing Skill1/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "one"), "Missing Skill2/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "two"), "Missing Skill2/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "one"), "Missing Skill3/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "two"), "Missing Skill3/two row");
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
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] },
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);

        var table = vm.Run();
            
        Assert.AreEqual(6, table.Count, "Table should contain 3 rows");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "one"), "Missing Skill1/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "two"), "Missing Skill1/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "one"), "Missing Skill2/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "two"), "Missing Skill2/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "one"), "Missing Skill3/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "two"), "Missing Skill3/two row");
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
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"] },
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);

        var table = vm.Run();
            
        Assert.AreEqual(6, table.Count, "Table should contain 3 rows");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "one"), "Missing Skill1/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill1" && (string)row.Values[1] == "two"), "Missing Skill1/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "one"), "Missing Skill2/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill2" && (string)row.Values[1] == "two"), "Missing Skill2/two row");
            
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "one"), "Missing Skill3/one row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Skill3" && (string)row.Values[1] == "two"), "Missing Skill3/two row");
    }
}
