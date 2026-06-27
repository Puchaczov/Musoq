using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    #region §5 SELECT Clause

    [TestMethod]
    public void Spec_Select_LiteralValue()
    {
        TestMethodTemplate("1", 1);
    }

    [TestMethod]
    public void Spec_Select_ColumnReference()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice") { City = "NYC", Country = "USA", Population = 100m }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Select_QualifiedColumnReference()
    {
        var query = "select a.Name from #A.Entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Select_ArithmeticExpression()
    {
        TestMethodTemplate("1 + 2 * 3", 7);
    }

    [TestMethod]
    public void Spec_Select_ColumnAlias_WithAs()
    {
        var query = "select Name as FullName from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("FullName", table.Columns.ElementAt(0).ColumnName);
    }

    [TestMethod]
    public void Spec_Select_StringConcatenation()
    {
        TestMethodTemplate("'Hello' + ' ' + 'World'", "Hello World");
    }

    [TestMethod]
    public void Spec_Select_Distinct_ShouldRemoveDuplicates()
    {
        var query = "select distinct Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" },
                    new BasicEntity("b") { Country = "POLAND" },
                    new BasicEntity("c") { Country = "GERMANY" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Select_Star_ShouldExpandAllColumns()
    {
        var query = "select * from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsGreaterThan(1, table.Columns.Count(), "Star should expand to multiple columns");
    }

    #endregion

    #region §7 WHERE Clause

    [TestMethod]
    public void Spec_Where_ComparisonGreaterThan()
    {
        var query = "select Name from #A.Entities() where Population > 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Population = 100m },
                    new BasicEntity("b") { Population = 300m },
                    new BasicEntity("c") { Population = 500m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_LogicalAnd()
    {
        var query = "select Name from #A.Entities() where Country = 'POLAND' and Population > 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND", Population = 100m },
                    new BasicEntity("b") { Country = "POLAND", Population = 300m },
                    new BasicEntity("c") { Country = "GERMANY", Population = 500m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_LogicalOr()
    {
        var query = "select Name from #A.Entities() where City = 'WARSAW' or City = 'BERLIN'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "WARSAW" },
                    new BasicEntity("b") { City = "BERLIN" },
                    new BasicEntity("c") { City = "PARIS" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_IsNull()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { NullableValue = null },
                    new BasicEntity("b") { NullableValue = 42 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_IsNotNull()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { NullableValue = null },
                    new BasicEntity("b") { NullableValue = 42 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_Like_CaseInsensitive()
    {
        var query = "select Name from #A.Entities() where Name like '%lic%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("MALICE")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count, "LIKE should be case-insensitive, matching both 'Alice' and 'MALICE'");
    }

    [TestMethod]
    public void Spec_Where_In_SetMembership()
    {
        var query = "select Name from #A.Entities() where Population in (100, 300)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Population = 100m },
                    new BasicEntity("b") { Population = 200m },
                    new BasicEntity("c") { Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_AngleBracketForm()
    {
        var query = "select Name from #A.Entities() where Name <> 'Bob'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_ExclamationForm_ShouldSucceed()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #A.Entities() where Name != 'Bob'",
            sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_ExclamationForm_WithNumeric_ShouldSucceed()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Population = 100m },
                    new BasicEntity("b") { Population = 200m },
                    new BasicEntity("c") { Population = 300m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #A.Entities() where Population != 200",
            sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_ExclamationForm_WithAnd_ShouldSucceed()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC" },
                    new BasicEntity("Bob") { City = "LA" },
                    new BasicEntity("Carol") { City = "NYC" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #A.Entities() where Name != 'Bob' and City != 'LA'",
            sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Spec_Where_NotEqual_BothForms_ProduceSameResults()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Carol")
                ]
            }
        };

        var vm1 = CreateAndRunVirtualMachine(
            "select Name from #A.Entities() where Name <> 'Bob'",
            sources);
        var table1 = vm1.Run(TokenSource.Token);

        var vm2 = CreateAndRunVirtualMachine(
            "select Name from #A.Entities() where Name != 'Bob'",
            sources);
        var table2 = vm2.Run(TokenSource.Token);

        Assert.AreEqual(table1.Count, table2.Count);

        var sorted1 = table1.Select(r => (string)r[0]).OrderBy(x => x).ToList();
        var sorted2 = table2.Select(r => (string)r[0]).OrderBy(x => x).ToList();

        for (var i = 0; i < sorted1.Count; i++)
            Assert.AreEqual(sorted1[i], sorted2[i]);
    }

    #endregion
}
