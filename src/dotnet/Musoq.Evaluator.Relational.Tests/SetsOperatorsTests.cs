using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class SetsOperatorsTests : BasicEntityTestBase
{











































    [TestMethod]
    public void WhenWrongTypeBetweenExcept_ShouldFail()
    {
        var query = @"select Name from #A.Entities() except (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenWrongTypeBetweenIntersect_ShouldFail()
    {
        var query = @"select Name from #A.Entities() intersect (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }




}
