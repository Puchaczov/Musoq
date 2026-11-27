using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class PublishedDiagnosticQueryTests
{
    private static BasicSchemaProvider<BasicEntity> CreateProvider() =>
        new(new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [new BasicEntity("Alice") { Id = 1 }] });

    private static void AssertAnalysisPair(
        string trigger,
        string repair,
        DiagnosticCode code,
        DiagnosticPhase phase,
        DiagnosticSeverity severity,
        DiagnosticSourceKind domain,
        int offset,
        int length,
        bool hasSuggestions,
        string argument,
        string value
    )
    {
        var analyzer = new QueryAnalyzer(CreateProvider());
        var failed = analyzer.Analyze(trigger);
        Assert.AreEqual(severity == DiagnosticSeverity.Warning, failed.IsSuccess);
        Assert.HasCount(1, failed.Diagnostics, trigger);
        var diagnostic = failed.Diagnostics.Single();
        Assert.AreEqual(code, diagnostic.Code);
        Assert.AreEqual(phase, diagnostic.Phase);
        Assert.AreEqual(severity, diagnostic.Severity);
        Assert.AreEqual(domain, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(offset, length), diagnostic.Span);
        if (hasSuggestions)
            Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        if (argument.Length != 0)
            Assert.AreEqual(value, diagnostic.Arguments[argument]);

        var control = analyzer.Analyze(repair);
        Assert.IsTrue(control.IsSuccess, repair);
        Assert.IsEmpty(control.Diagnostics, repair);
    }
}
