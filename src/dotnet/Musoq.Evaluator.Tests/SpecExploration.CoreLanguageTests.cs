using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests derived directly from the Musoq Core Language Specification (musoq-core-language-spec.md)
///     and TABLE/COUPLE Specification (musoq-table-couple-spec.md).
///     These tests verify that queries constructed from the specs work correctly
///     and that malformed queries produce meaningful error messages.
/// </summary>
[TestClass]
public partial class SpecExplorationCoreLanguageTests
{
    public TestContext TestContext { get; set; }

    #region §6 FROM Clause

    [TestMethod]
    public void Spec_From_WithoutHashPrefix_ShouldBeEquivalent()
    {
        var query = "select Name from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    #endregion

    #region §Appendix G: GROUP BY References

    [TestMethod]
    public void Spec_ExplicitGroupByColumn_ShouldReturnGroupKey()
    {
        var query = "select Country, Count(Name) from #A.Entities() group by Country";
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

        var countries = new HashSet<object> { table[0][0], table[1][0] };
        Assert.Contains("POLAND", countries);
        Assert.Contains("GERMANY", countries);
    }

    #endregion

    #region §5.6 RowNumber

    [TestMethod]
    public void Spec_RowNumber_Basic()
    {
        var query = "select RowNumber(), Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);


        var rowNumbers = table.Select(r => (int)r.Values[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(1, rowNumbers[0]);
        Assert.AreEqual(2, rowNumbers[1]);
        Assert.AreEqual(3, rowNumbers[2]);
    }

    #endregion

    #region §19 String Comparison Semantics

    [TestMethod]
    public void Spec_StringComparison_EqualityIsOrdinal()
    {
        var query = "select Name from #A.Entities() where Name = 'alice'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("alice")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count, "= should be case-sensitive per spec");
        Assert.AreEqual("alice", table[0][0]);
    }

    #endregion

    #region §7.9 Implicit Boolean Conversion

    [TestMethod]
    public void Spec_ImplicitBoolConversion_InWhere()
    {
        var query = "select Name from #A.Entities() where Match('\\d+', Name)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test123"),
                    new BasicEntity("nope")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test123", table[0][0]);
    }

    #endregion

}
