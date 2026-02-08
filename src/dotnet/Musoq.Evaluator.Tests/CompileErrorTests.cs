using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CompileErrorTests : NegativeTestsBase
{
    #region 4.1 GROUP BY Semantic Violations

    [TestMethod]
    public void CE001_NonAggregatedNonGroupedColumnInSelect_ShouldThrowError()
    {
        
        
        Assert.Throws<NonAggregatedColumnInSelectException>(() =>
            CompileQuery("SELECT Name, City, Count(1) FROM #test.people() GROUP BY City"));
    }

    [TestMethod]
    public void CE002_MultipleNonAggregatedColumnsMissingFromGroupBy_ShouldThrowError()
    {
        
        Assert.Throws<NonAggregatedColumnInSelectException>(() =>
            CompileQuery("SELECT Name, Age, City, Count(1) FROM #test.people() GROUP BY City"));
    }

    [TestMethod]
    public void CE008_ParentAggregationWithInvalidSkipLevel_CompilesSuccessfully()
    {
        
        
        
        
        var vm = CompileQuery("SELECT City, Sum(3, Age) FROM #test.people() GROUP BY City");
        Assert.IsNotNull(vm, "Sum with high parent level compiles without error.");
    }

    #endregion

    #region 4.2 Alias / Scope Violations in Compiled Code

    [TestMethod]
    public void CE010_SelectAliasUsedInWhere_ShouldThrowCompileError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT Name AS FileName FROM #test.people() WHERE FileName = 'test'"));
    }

    [TestMethod]
    public void CE011_SelectAliasUsedInGroupBy_ShouldThrowError()
    {
        Assert.Throws<UnknownColumnOrAliasException>(() =>
            CompileQuery("SELECT Length(Name) AS NameLen FROM #test.people() GROUP BY NameLen"));
    }

    #endregion

    #region 4.5 Misc Compilation Edge Cases

    [TestMethod]
    public void CE040_SelectStarWithGroupBy_ShouldThrowErrorForNonGroupedColumns()
    {
        
        
        Assert.Throws<NonAggregatedColumnInSelectException>(() =>
            CompileQuery("SELECT * FROM #test.people() GROUP BY City"));
    }

    #endregion
}
