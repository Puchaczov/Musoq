using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CompileErrorTests : NegativeTestsBase
{
    #region 4.5 Misc Compilation Edge Cases

    [TestMethod]
    public void CE040_SelectStarWithGroupBy_ShouldThrowErrorForNonGroupedColumns()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "GROUP BY");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 4.1 GROUP BY Semantic Violations

    [TestMethod]
    public void CE001_NonAggregatedNonGroupedColumnInSelect_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, City, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "Name");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void CE002_MultipleNonAggregatedColumnsMissingFromGroupBy_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, Age, City, Count(1) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "Name");
        AssertHasGuidance(ex);
        AssertSecondaryEnvelopeCode(ex, 1, DiagnosticCode.MQ3012_NonAggregateInSelect, "Age");
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
    public void CE010_SelectAliasUsedInWhere_ShouldCompileAndFilterByAlias()
    {
        var vm = CompileQuery("SELECT Name AS FileName FROM #test.people() WHERE FileName = 'Alice'");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void CE011_SelectAliasUsedInGroupBy_ShouldCompileAndGroupByAlias()
    {
        var vm = CompileQuery("SELECT Length(Name) AS NameLen, Count(1) FROM #test.people() GROUP BY NameLen");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
    }

    #endregion
}
