using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class InvalidQueryOutputTypeTests
{
    [TestMethod]
    public void WhenSelectComplexType_ExceptionMessageShouldContainColumnName()
    {
        var query = "select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
        Assert.Contains("SELECT", exception.Message, "Exception message should mention SELECT clause");
    }

    [TestMethod]
    public void WhenOrderByComplexType_ExceptionMessageShouldMentionOrderBy()
    {
        var query = "select Name from #A.Entities() order by Self";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
        AssertMessageContains(exception, "ORDER BY");
    }

    [TestMethod]
    public void WhenGroupByComplexType_ExceptionMessageShouldMentionGroupBy()
    {
        var query = "select Self from #A.Entities() group by Self";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
    }



    [TestMethod]
    public void WhenSelectDistinctWithPrimitiveTypes_ShouldSucceed()
    {
        var query = "select distinct City from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000), new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenSelectWithAlias_ShouldSucceed()
    {
        var query = "select Name as PersonName, City as Location from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenEmptyResult_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() where 1 = 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

}
