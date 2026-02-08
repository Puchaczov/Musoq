using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SemanticLogicalTests : NegativeTestsBase
{
    #region 6.1 Empty / Vacuous Queries

    [TestMethod]
    public void SL001_WhereAlwaysFalse_ShouldReturnZeroRows()
    {
        
        var vm = CompileQuery("SELECT * FROM #test.people() WHERE 1 = 0");
        var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SL002_WhereAlwaysTrue_ShouldReturnAllRows()
    {
        
        var vm = CompileQuery("SELECT * FROM #test.people() WHERE 1 = 1");
        var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(5, table.Count);
    }

    #endregion

    #region 6.2 Nonsensical but Valid Combinations

    [TestMethod]
    public void SL011_GroupByAllColumns_ShouldReturnAllRows()
    {
        
        var vm = CompileQuery("SELECT Name, Age, City FROM #test.people() GROUP BY Name, Age, City");
        var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void SL013_SkipMoreRowsThanExist_ShouldReturnZeroRows()
    {
        
        var vm = CompileQuery("SELECT * FROM #test.people() ORDER BY Name SKIP 999999");
        var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(0, table.Count);
    }

    #endregion

    #region 6.3 Cross-Source Edge Cases

    [TestMethod]
    public void SL020_InnerJoinWhereNoRowsMatch_ShouldReturnZeroRows()
    {
        
        var vm = CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.empty() e ON p.Id = e.Id");
        var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SL021_LeftOuterJoinWithEmptyRightSide_ShouldReturnAllLeftRows()
    {
        
        var vm = CompileQuery("SELECT p.Name, e.Name FROM #test.people() p LEFT OUTER JOIN #test.empty() e ON p.Id = e.Id");
        var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void SL022_SelfJoin_ShouldProduceCorrectResults()
    {
        
        var vm = CompileQuery("SELECT a.Name, b.Name FROM #test.people() a INNER JOIN #test.people() b ON a.City = b.City WHERE a.Name <> b.Name");
        var table = vm.Run(CancellationToken.None);
        
        Assert.AreEqual(4, table.Count);
    }

    #endregion
}
