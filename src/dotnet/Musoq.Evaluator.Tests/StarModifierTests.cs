using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class StarModifierTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenStarExcludeSingleColumn_ShouldRemoveColumn()
    {
        const string query = "select * exclude (City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                    {
                        City = "London", Country = "UK", Population = 9000000m, Id = 1
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Name")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarExcludeMultipleColumns_ShouldRemoveColumns()
    {
        const string query = "select * exclude (City, Country, Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 9000000m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(6, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Population")));
    }

    [TestMethod]
    public void WhenAliasedStarExclude_ShouldRemoveColumn()
    {
        const string query = "select a.* exclude (City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
    }

    [TestMethod]
    public void WhenStarReplaceSingleColumn_ShouldSubstituteExpression()
    {
        const string query = "select * replace (Population * 2 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(200m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarReplaceMultipleColumns_ShouldSubstituteExpressions()
    {
        const string query =
            "select * replace (Population * 2 as Population, Money + 10 as Money) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        var moneyIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Money")).i;

        Assert.AreEqual(200m, table[0].Values[populationIdx]);
        Assert.AreEqual(60m, table[0].Values[moneyIdx]);
    }

    [TestMethod]
    public void WhenStarLikePattern_ShouldFilterColumns()
    {
        const string query = "select * like 'C%' from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsTrue(columnNames.All(c => c.Contains("City") || c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarNotLikePattern_ShouldExcludeMatchingColumns()
    {
        const string query = "select * not like 'C%' from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));
    }

    [TestMethod]
    public void WhenStarLikeWithExclude_ShouldCompose()
    {
        const string query = "select * like '%o%' exclude (Money, Month) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 9000000m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("Money")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Month")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Country")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Population")));
    }

    [TestMethod]
    public void WhenStarExcludeWithReplace_ShouldCompose()
    {
        const string query =
            "select * exclude (City, Country) replace (Population * 2 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { City = "London", Country = "UK", Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, table.Columns.Count());

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")));
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(200m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarLikeExcludeReplace_ShouldComposeAll()
    {
        const string query =
            "select * like '%o%' exclude (Country) replace (Population * 3 as Population) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m) { Country = "UK", Population = 100m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")));
        Assert.IsTrue(columnNames.Any(c => c.Contains("Population")));

        var populationIdx = table.Columns.Select((c, i) => (c, i))
            .First(x => x.c.ColumnName.Contains("Population")).i;
        Assert.AreEqual(300m, table[0].Values[populationIdx]);
    }

    [TestMethod]
    public void WhenStarExcludeNonExistentColumn_ShouldThrow()
    {
        const string query = "select * exclude (NonExistent) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3041_StarExcludeColumnNotFound, DiagnosticPhase.Bind, "NonExistent");
    }

    [TestMethod]
    public void WhenStarExcludeAllColumns_ShouldThrow()
    {
        const string query =
            "select * exclude (Name, City, Country, Population, Money, Month, Time, Id, NullableValue) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenStarReplaceNonExistentColumn_ShouldThrow()
    {
        const string query = "select * replace (1 as NonExistent) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3042_StarReplaceColumnNotFound, DiagnosticPhase.Bind, "NonExistent");
    }

    [TestMethod]
    public void WhenStarColumnInBothExcludeAndReplace_ShouldThrow()
    {
        const string query = "select * exclude (City) replace (1 as City) from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("january", 50m)] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace, DiagnosticPhase.Bind, "City");
    }

}
