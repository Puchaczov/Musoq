using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class PivotDiagnosticTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_WithNonAggregateUsingFunction_ShouldReportPivotUsingError()
    {
        const string query = ("pivot #A.Entities()\n" + "on Month in ('Jan' as Jan)\n" + "using ToUpper(Name) as Name");

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        MusoqExceptionAssertions.AssertSingleError(
            exception,
            DiagnosticCode.MQ3051_FilterOnNonAggregate,
            DiagnosticPhase.Bind,
            "PIVOT USING accepts aggregate function calls only"
        );
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return CreateSingleSource(new BasicEntity { Name = "Alice", Month = "Jan" });
    }
}
