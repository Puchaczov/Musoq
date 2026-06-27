using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class JoinSemiAntiCrossJoinDiagnosticsTests : BasicEntityTestBase
{
    [TestMethod]
    public void SemiJoin_WhenSelectReferencesRightSideColumn_ShouldRejectAlias()
    {
        const string query = "select b.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "b");
    }

    [TestMethod]
    public void AntiJoin_WhenSelectReferencesRightSideColumn_ShouldRejectAlias()
    {
        const string query = "select b.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "b");
    }

    [TestMethod]
    public void SemiJoin_WhenWhereReferencesRightSideColumn_ShouldRejectAlias()
    {
        const string query = @"
select a.Name
from #A.entities() a
semi join #B.entities() b on a.Id = b.Id
where b.Name = 'B1'";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "b");
    }

    [TestMethod]
    public void SemiJoin_WhenRowPresenceReferencesOutputAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Name
from #A.entities() a
semi join #B.entities() b on a.Id = b.Id
where a is missing";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "a");
        AssertMessageContains(ex, "always present");
    }

    [TestMethod]
    public void AntiJoin_WhenRowPresenceReferencesOutputAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Name
from #A.entities() a
anti join #B.entities() b on a.Id = b.Id
where a is missing";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "a");
        AssertMessageContains(ex, "always present");
    }

    [TestMethod]
    public void SemiJoin_WhenRowPresenceReferencesHiddenRightAlias_ShouldRejectAlias()
    {
        const string query = @"
select a.Name
from #A.entities() a
semi join #B.entities() b on a.Id = b.Id
where b is missing";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "b");
    }

    [TestMethod]
    public void AntiJoin_WhenRowPresenceReferencesHiddenRightAlias_ShouldRejectAlias()
    {
        const string query = @"
select a.Name
from #A.entities() a
anti join #B.entities() b on a.Id = b.Id
where b is missing";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "b");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A1") { Id = 1 }] },
            { "#B", [new BasicEntity("B1") { Id = 1 }] }
        };
    }
}
