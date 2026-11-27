using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "with c (select 1 as X from #A.Entities()) select X from c",
        "with c as (select 1 as X from #A.Entities()) select X from c",
        DiagnosticCode.MQ2001_UnexpectedToken,
        7,
        1
    )]
    [DataRow(
        "select case else 0 end from #A.Entities()",
        "select case when Id > 0 then 1 else 0 end from #A.Entities()",
        DiagnosticCode.MQ2001_UnexpectedToken,
        12,
        4
    )]
    [DataRow(
        "select case when true 1 end from #A.Entities()",
        "select case when Id > 0 then 1 else 0 end from #A.Entities()",
        DiagnosticCode.MQ2018_MissingOperator,
        22,
        0
    )]
    [DataRow(
        "select case when true then 1 from #A.Entities()",
        "select case when Id > 0 then 1 else 0 end from #A.Entities()",
        DiagnosticCode.MQ2001_UnexpectedToken,
        29,
        4
    )]
    [DataRow(
        "binary 1 { Data: byte }; select 1 from #A.Entities()",
        "binary Packet { Data: byte }; select 1 from #A.Entities()",
        DiagnosticCode.MQ2001_UnexpectedToken,
        7,
        1
    )]
    [DataRow(
        "binary Packet { Data: }; select 1 from #A.Entities()",
        "binary Packet { Data: byte }; select 1 from #A.Entities()",
        DiagnosticCode.MQ2001_UnexpectedToken,
        22,
        1
    )]
    public void CatalogExample_ShouldReportTheObservedParserCodeAndAcceptTheRepair(
        string trigger,
        string repair,
        DiagnosticCode expected,
        int offset,
        int length
    )
    {
        var analyzer = new QueryAnalyzer(CreateProvider());
        var result = analyzer.Analyze(trigger);
        Assert.IsFalse(result.IsSuccess);
        Assert.HasCount(1, result.Diagnostics);
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expected, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(offset, length), diagnostic.Span);
        var repaired = analyzer.Analyze(repair);
        Assert.IsTrue(repaired.IsSuccess);
        Assert.IsEmpty(repaired.Diagnostics);
    }
}
