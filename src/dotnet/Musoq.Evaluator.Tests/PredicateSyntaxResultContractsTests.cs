using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PredicateSyntaxResultContractsTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void Contains_ShouldReturnMatchingValuesWithDuplicateListEntriesIgnored()
    {
        const string query = "select Name from #A.Entities() where Name contains ('ABC', 'CDA', 'CDA', 'DDABC')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABC"),
                    new BasicEntity("XXX"),
                    new BasicEntity("CDA"),
                    new BasicEntity("DDABC")
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["ABC"],
            ["CDA"],
            ["DDABC"]);
    }

    [TestMethod]
    [FeatureEvidence("contains-null-semantics", FeatureEvidenceKind.RuntimePositive)]
    public void Contains_ShouldTreatNullLeftAsAbsentUnlessNullIsListed()
    {
        const string query = "select Name from #A.Entities() where Name contains (null, 'a')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "a" },
                    new BasicEntity { Name = "b" },
                    new BasicEntity { Name = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [null],
            ["a"],
            [null]);
    }

    [TestMethod]
    public void Contains_ShouldExcludeNullLeftWhenNullIsNotListed()
    {
        const string query = "select Name from #A.Entities() where Name contains ('a')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "a" },
                    new BasicEntity { Name = "b" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"]);
    }

    [TestMethod]
    [FeatureEvidence("regex-null-semantics", FeatureEvidenceKind.RuntimePositive)]
    public void RLike_ShouldExcludeNullLeftValues()
    {
        const string query = "select Name from #A.Entities() where Name rlike '^test.*$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "test123" },
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "testValue" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["test123"], ["testValue"]);
    }

    [TestMethod]
    public void RLike_ShouldExcludeRowsWithNullPattern()
    {
        const string query = "select Name from #A.Entities() where Name rlike City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "test", City = null },
                    new BasicEntity { Name = "abc", City = "a.*" },
                    new BasicEntity { Name = "xyz", City = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["abc"]);
    }

    [TestMethod]
    public void NotRLike_ShouldRetainNullLeftValuesAndNonMatches()
    {
        const string query = "select Name from #A.Entities() where Name not rlike '^test.*$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "test123" },
                    new BasicEntity { Name = "other" },
                    new BasicEntity { Name = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null], ["other"], [null]);
    }
}
