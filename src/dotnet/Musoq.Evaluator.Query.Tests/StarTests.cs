using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StarTests : BasicEntityTestBase
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0191")]
    public void InlineJoinedQualifiedStarExposesSourceColumnName()
    {
        const string query = "select a.* from values { (Value: 7) } a inner join values { (Value: 9) } b on a.Value < b.Value";
        AssertIntegerStarNameAndValue(query);
    }

    [TestMethod]
    public void InlineJoinedQualifiedStarExplicitRename_ShouldSelectOnlyLeftSource()
    {
        const string query = "select a.* rename (Value as Value) from values { (Value: 7) } a inner join values { (Value: 9) } b on a.Value < b.Value";
        AssertIntegerStarNameAndValue(query);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0190")]
    public void InlineStarAndExplicitProjectionPreserveDuplicateNames()
    {
        const string query = "select *, Value as Value from values { (Value: 7) } p";
        AssertMixedStarProjection(query, "Value");
    }

    [TestMethod]
    public void InlineRenamedStarAndExplicitProjection_ShouldPreserveBothValues()
    {
        const string query = "select * rename (Value as Expanded), Value as Value from values { (Value: 7) } p";
        AssertMixedStarProjection(query, "Expanded");
    }

    private void AssertMixedStarProjection(string query, string wildcardName)
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, (wildcardName, typeof(int)), ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7, 7 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0189")]
    public void InlineIntegerStarExposesSourceColumnName()
    {
        const string query = "select * from values { (Value: 7) } p";
        AssertIntegerStarNameAndValue(query);
    }

    [TestMethod]
    public void InlineIntegerStarExplicitRename_ShouldExposeDeclaredNameAndValue()
    {
        const string query = "select * rename (Value as Value) from values { (Value: 7) } p";
        AssertIntegerStarNameAndValue(query);
    }

    private void AssertIntegerStarNameAndValue(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-003")]
    public void InlineValuesStarUsesUnqualifiedOutputName()
    {
        const string query = "select * from values { (Name: 'Ada') } v";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>();

        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "Ada" });
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-StarExpansionUsesTheUnqualifiedSourceColumnName")]
    public void IntegerInlineValuesStarUsesUnqualifiedOutputName()
    {
        const string query = "select * from values { (N: 0) } v";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>();

        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("N", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 0 });
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleColumns_AndExplicitColumnIsUsedWithinWhere_ShouldPass()
    {
        const string query = @"select * from #A.entities() a where a.Month = 'january'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }

    [TestMethod]
    public void WhenMultipleStarsUnfoldToMultipleColumns_ShouldPass()
    {
        const string query = @"select *, * from #A.entities() a where a.Month = 'january'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(18, table.Columns.Count());

        Assert.AreEqual("Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(50m, table[0].ReadValues()[4]);

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);

        Assert.AreEqual("Money", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual(50m, table[0].ReadValues()[13]);
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleColumns_AndExplicitColumnIsUsedAsAnotherColumn_ShouldPass()
    {
        const string query = @"select *, a.Month, Month from #A.entities() a where a.Month = 'january'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(11, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);

        Assert.AreEqual("Month", table.Columns.ElementAt(9).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[9]);

        Assert.AreEqual("Month", table.Columns.ElementAt(10).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[10]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_ShouldPass()
    {
        const string query = @"select a.* from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(9, table.Columns.Count());
    }

    [TestMethod]
    public void WhenAliasedStarsUnfoldToMultipleColumns_ShouldPass()
    {
        const string query = @"select a.*, a.* from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(18, table.Columns.Count());
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndExplicitColumnUsed_ShouldPass()
    {
        const string query = @"select a.*, a.Month from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(10, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(9).ColumnName);
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleOfTwoTablesColumns_TwoTablesUsed_ShouldPass()
    {
        const string query = @"select * from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(18, table.Columns.Count());

        Assert.AreEqual("Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual("Money", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(14).ColumnName);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_TwoTablesUsed_ShouldPass()
    {
        const string query = @"select a.*, b.* from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(18, table.Columns.Count());

        Assert.AreEqual("Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual("Money", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(14).ColumnName);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_FirstTableUnfolded_ShouldPass()
    {
        const string query = @"select a.* from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual(50m, table[0].ReadValues()[4]);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_SecondTableUnfolded_ShouldPass()
    {
        const string query = @"select b.* from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual(150m, table[0].ReadValues()[4]);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndExplicitColumnUsed_TwoTablesUsed_ShouldPass()
    {
        const string query =
            @"select a.*, a.Month, b.*, b.Month from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20, table.Columns.Count());

        Assert.AreEqual("Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(9).ColumnName);

        Assert.AreEqual("Money", table.Columns.ElementAt(14).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(15).ColumnName);
        Assert.AreEqual("Month", table.Columns.ElementAt(19).ColumnName);

        Assert.AreEqual(50m, table[0].ReadValues()[4]);
        Assert.AreEqual("january", table[0].ReadValues()[5]);

        Assert.AreEqual(150m, table[0].ReadValues()[14]);
        Assert.AreEqual("january", table[0].ReadValues()[15]);

        Assert.AreEqual("january", table[0].ReadValues()[9]);
        Assert.AreEqual("january", table[0].ReadValues()[19]);
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleColumns_AndStarIsUsedWithinSelect_ShouldPass()
    {
        const string query = @"with p as (select * from #A.entities() a where a.Month = 'january') select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndStarIsUsedWithinSelect_ShouldPass()
    {
        const string query = @"with p as (select a.* from #A.entities() a where a.Month = 'january') select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndAliasedStarIsUsedWithinSelect_ShouldPass()
    {
        const string query = @"with p as (select a.* from #A.entities() a where a.Month = 'january') select p.* from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndAliasedStarIsUsedWithinAliasedFrom_ShouldPass()
    {
        const string query =
            @"with p as (select a.* from #A.entities() a where a.Month = 'january') select p.* from p p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].ReadValues()[5]);
    }
}
