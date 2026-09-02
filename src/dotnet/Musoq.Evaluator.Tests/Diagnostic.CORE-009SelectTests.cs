using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCore009SelectTests : BasicEntityTestBase
{
    [TestMethod]
    public void SelectPropertyAndIndexedAccess_ShouldEvaluateProjectionForms()
    {
        const string query =
            "select ToUpperInvariant(Name) as UpperName, Self.Name as EntityName, Self.Array[2] as ThirdItem from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("Alice")]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("UpperName", typeof(string)),
            ("EntityName", typeof(string)),
            ("ThirdItem", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["ALICE", "Alice", 2]);
    }

    [TestMethod]
    public void SelectStarThroughCte_ShouldExposeUnqualifiedSourceColumnNames()
    {
        const string query = @"
            with p as (
                select a.* from #A.Entities() a
            )
            select * from p";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("Alice")]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[] { "Name", "City", "Country", "Population", "Money", "Month", "Time", "Id", "NullableValue" },
            table.Columns.Select(column => column.ColumnName).ToArray());
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void SelectAliasesWithDuplicateNames_ShouldResolveTheFirstAlias()
    {
        const string query =
            "select Name as Duplicate, City as Duplicate from #A.Entities() where Duplicate = 'Alice'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Alice") { City = "Warsaw" },
                    new BasicEntity("Bob") { City = "Berlin" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
    }

    [TestMethod]
    public void SelectDistinct_ShouldTreatCaseVariantsAsDistinctValues()
    {
        const string query = "select distinct Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("one") { Country = "POLAND" },
                    new BasicEntity("two") { Country = "poland" },
                    new BasicEntity("three") { Country = "POLAND" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        Assert.HasCount(2, table);
        CollectionAssert.AreEquivalent(
            new object[] { "POLAND", "poland" },
            table.Select(row => row[0]).ToArray());
    }

    [TestMethod]
    public void RowNumber_ShouldBeAssignedBeforeOrderedSkipAndTake()
    {
        const string query =
            "select Country, RowNumber() as rn from #A.Entities() order by Country skip 1 take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Country = "Sweden" },
                    new BasicEntity { Country = "Germany" },
                    new BasicEntity { Country = "Poland" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("rn", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", 2]);
    }
}
