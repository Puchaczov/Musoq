// ReSharper disable UnusedAutoPropertyAccessor.Local
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyMixedTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void CrossApply_SchemaAndProperty_WithNestedProperty()
    {
        const string query = @"
        select
            a.City,
            b.Country,
            c.StreetName,
            c.HouseNumber
        from #schema.first() a
        cross apply #schema.second(a.Country) b
        cross apply b.Addresses c";

        var firstSource = new CrossApplyClass1[]
        {
            new() { Country = "USA", City = "New York" },
            new() { Country = "USA", City = "Los Angeles" }
        };

        var secondSource = new CrossApplyClass2[]
        {
            new()
            {
                Country = "USA",
                Addresses =
                [
                    new ComplexType1 { StreetName = "Broadway", HouseNumber = 123 },
                    new ComplexType1 { StreetName = "Fifth Avenue", HouseNumber = 456 }
                ]
            },
            new()
            {
                Country = "Canada",
                Addresses =
                [
                    new ComplexType1 { StreetName = "Yonge Street", HouseNumber = 789 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray()
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Country", typeof(string)),
            ("c.StreetName", typeof(string)),
            ("c.HouseNumber", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["New York", "USA", "Broadway", 123],
            ["New York", "USA", "Fifth Avenue", 456],
            ["Los Angeles", "USA", "Broadway", 123],
            ["Los Angeles", "USA", "Fifth Avenue", 456]);
    }

    [TestMethod]
    public void CrossApply_SchemaAndMethod_WithComplexObjects()
    {
        const string query = @"
        select
            a.Department,
            a.Budget,
            b.Name,
            b.Salary,
            c.Value
        from #schema.first() a
        cross apply #schema.second(a.Department) b
        cross apply b.Distinct(b.Skills) c";

        var firstSource = new CrossApplyClass3[]
        {
            new() { Department = "IT", Budget = 500000 },
            new() { Department = "HR", Budget = 300000 }
        };

        var secondSource = new CrossApplyClass4[]
        {
            new() { Department = "IT", Name = "John Doe", Salary = 50000, Skills = ["C#", "JavaScript", "C#"] },
            new() { Department = "IT", Name = "Jane Smith", Salary = 60000, Skills = ["C#", "JavaScript"] },
            new() { Department = "HR", Name = "John Doe", Salary = 50000, Skills = ["Communication", "Negotiation"] },
            new()
            {
                Department = "HR", Name = "Jane Smith", Salary = 60000,
                Skills = ["Communication", "Negotiation", "Communication"]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Department == RequireParameter<string>(parameters, 0))
                    .ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Department", typeof(string)),
            ("a.Budget", typeof(int)),
            ("b.Name", typeof(string)),
            ("b.Salary", typeof(int)),
            ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["IT", 500000, "John Doe", 50000, "C#"],
            ["IT", 500000, "John Doe", 50000, "JavaScript"],
            ["IT", 500000, "Jane Smith", 60000, "C#"],
            ["IT", 500000, "Jane Smith", 60000, "JavaScript"],
            ["HR", 300000, "John Doe", 50000, "Communication"],
            ["HR", 300000, "John Doe", 50000, "Negotiation"],
            ["HR", 300000, "Jane Smith", 60000, "Communication"],
            ["HR", 300000, "Jane Smith", 60000, "Negotiation"]);
    }

    [TestMethod]
    public void CrossApply_PropertyAndMethod_WithFiltering()
    {
        const string query = @"
    select
        a.Department,
        b.Name,
        c.Value
    from #schema.first() a
    cross apply a.Employees b
    cross apply a.Distinct(b.Skills) c
    where a.Budget > 400000";

        var firstSource = new CrossApplyClass5[]
        {
            new()
            {
                Department = "IT",
                Budget = 500000,
                Employees =
                [
                    new ComplexType2
                        { Name = "John Doe", Skills = ["C#", "C#"] },
                    new ComplexType2
                        { Name = "Jane Smith", Skills = ["Java"] }
                ]
            },
            new()
            {
                Department = "HR",
                Budget = 300000,
                Employees =
                [
                    new ComplexType2
                    {
                        Name = "Alice Johnson",
                        Skills = ["Communication", "Negotiation", "Communication", "Negotiation"]
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Department", typeof(string)),
            ("b.Name", typeof(string)),
            ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["IT", "John Doe", "C#"],
            ["IT", "Jane Smith", "Java"]);
    }

    [TestMethod]
    public void CrossApply_PropertyAndMethod_GroupBy_WithFiltering()
    {
        const string query = @"
    select
        a.Department,
        Count(a.Department)
    from #schema.first() a
    cross apply a.Employees b
    cross apply a.Distinct(b.Skills) c
    where a.Budget > 400000
    group by a.Department";

        var firstSource = new CrossApplyClass5[]
        {
            new()
            {
                Department = "IT",
                Budget = 500000,
                Employees =
                [
                    new ComplexType2
                        { Name = "John Doe", Skills = ["C#", "C#"] },
                    new ComplexType2
                        { Name = "Jane Smith", Skills = ["Java"] }
                ]
            },
            new()
            {
                Department = "HR",
                Budget = 300000,
                Employees =
                [
                    new ComplexType2
                    {
                        Name = "Alice Johnson",
                        Skills = ["Communication", "Negotiation", "Communication", "Negotiation"]
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Department", typeof(string)),
            ("Count(a.Department)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["IT", 2L]);
    }

    [TestMethod]
    public void CrossApply_InnerJoinAndUseProperty_ShouldPass()
    {
        const string query = @"
    select
        a.Name,
        a.Surname,
        c.Value
    from #schema.first() a
    inner join #schema.second() b on a.Id = b.Id
    cross apply b.Skills c";

        var firstSource = new CrossApplyClass6[]
        {
            new() { Name = "John", Surname = "Doe", Id = 1 },
            new() { Name = "Jane", Surname = "Smith", Id = 2 },
            new() { Name = "Alice", Surname = "Johnson", Id = 3 }
        };

        var secondSource = new CrossApplyClass7[]
        {
            new() { Id = 1, Skills = ["C#", "JavaScript"] },
            new() { Id = 2, Skills = ["Java"] },
            new() { Id = 3, Skills = ["Communication", "Negotiation"] }
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource, secondSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Surname", typeof(string)),
            ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "Doe", "C#"],
            ["John", "Doe", "JavaScript"],
            ["Jane", "Smith", "Java"],
            ["Alice", "Johnson", "Communication"],
            ["Alice", "Johnson", "Negotiation"]);
    }

    [TestMethod]
    public void CrossApply_LeftJoinAndUseProperty_ShouldPass()
    {
        const string query = @"
    select
        a.Name,
        a.Surname,
        c.Value
    from #schema.first() a
    left outer join #schema.second() b on a.Id = b.Id
    cross apply b.Skills c";

        var firstSource = new CrossApplyClass6[]
        {
            new() { Name = "John", Surname = "Doe", Id = 1 },
            new() { Name = "Jane", Surname = "Smith", Id = 2 },
            new() { Name = "Alice", Surname = "Johnson", Id = 3 }
        };

        var secondSource = new CrossApplyClass7[]
        {
            new() { Id = 1, Skills = ["C#", "JavaScript"] },
            new() { Id = 2, Skills = ["Java"] },
            new() { Id = 3, Skills = ["Communication", "Negotiation"] }
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource, secondSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Surname", typeof(string)),
            ("c.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "Doe", "C#"],
            ["John", "Doe", "JavaScript"],
            ["Jane", "Smith", "Java"],
            ["Alice", "Johnson", "Communication"],
            ["Alice", "Johnson", "Negotiation"]);
    }

    [TestMethod]
    public void CrossApply_WhenTwoMethodsReturnsEntities_AndThenUseColumnOfEntity_ShouldPass()
    {
        const string query =
            @"select a.Value, d.Name from #schema.first() a cross apply a.GetOne() b cross apply a.GetSpecialCaseComplexType2(b.Value) c cross apply c.Employees d";

        var vm = CreateAndRunVirtualMachine<SpecialCaseEmptyType, SpecialCaseLibrary1>(query,
            [new SpecialCaseEmptyType()]);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Value", typeof(string)),
            ("d.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Value", "John"]);
    }

    [TestMethod]
    public void WhenCteHasTheSameAliasWithinDifferentQueries_ShouldNotThrow()
    {
        const string query =
            "with p as (select 1 from #schema.first() a cross apply a.Split('a,b', ',') b), r as (select 1 from #schema.first() a cross apply a.Split('a,b', ',') b) select * from p";
        var firstSource = Array.Empty<CrossApplyClass5>();
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);

        vm.Run(TestContext.CancellationToken);
    }

    private sealed class CrossApplyClass1
    {
        public string City { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType1
    {
        public string StreetName { get; set; } = string.Empty;

        public int HouseNumber { get; set; }

        [BindablePropertyAsTable] public ComplexType1[] Addresses { get; set; } = [];
    }

    private sealed class CrossApplyClass2
    {
        public string Country { get; set; } = string.Empty;

        [BindablePropertyAsTable] public ComplexType1[] Addresses { get; set; } = [];
    }

    private sealed class CrossApplyClass3
    {
        public string Department { get; set; } = string.Empty;

        public int Budget { get; set; }
    }

    private sealed class CrossApplyClass4
    {
        public string Department { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Salary { get; set; }

        public string[] Skills { get; set; } = [];
    }

    private sealed class CrossApplyClass5
    {
        public string Department { get; set; } = string.Empty;
        public int Budget { get; set; }

        [BindablePropertyAsTable] public ComplexType2[] Employees { get; set; } = [];
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType2
    {
        public string Name { get; set; } = string.Empty;

        public string[] Skills { get; set; } = [];
    }

    private sealed class CrossApplyClass6
    {
        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public int Id { get; set; }
    }

    private sealed class CrossApplyClass7
    {
        public int Id { get; set; }

        [BindablePropertyAsTable] public string[] Skills { get; set; } = [];
    }

    public class SpecialCaseEmptyType
    {
        public string Value => "Value";
    }

    public class SpecialCaseLibrary1 : GenericLibrary
    {
        [BindableMethod]
        public IEnumerable<string> GetOne()
        {
            return ["One"];
        }

        [BindableMethod]
        public IEnumerable<SpecialCaseComplexType2> GetSpecialCaseComplexType2(string whatever)
        {
            return
            [
                new SpecialCaseComplexType2()
            ];
        }
    }

    public class SpecialCaseComplexType1
    {
        public string Name => "John";
    }

    public class SpecialCaseComplexType2
    {
        [BindablePropertyAsTable] public IEnumerable<SpecialCaseComplexType1> Employees => [new()];
    }
}
