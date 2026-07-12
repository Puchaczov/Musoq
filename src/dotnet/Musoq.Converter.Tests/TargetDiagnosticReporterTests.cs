using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetDiagnosticReporterTests
{
    [TestMethod]
    public void Report_ShouldPreserveTargetCodeSeverityRangeSourceAndSnippet()
    {
        var context = new DiagnosticContext(new SourceText("select 1", "query.musoq"));
        var targetDiagnostic = new TargetDiagnostic(
            "MT4321",
            TargetDiagnosticSeverity.Warning,
            "target warning",
            new TargetSourceRange(7, 1),
            "query.musoq",
            "select 1");

        TargetDiagnosticReporter.Report([targetDiagnostic], context);

        var diagnostic = context.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ8001_CodeGenerationFailed, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.AreEqual("[MT4321] target warning", diagnostic.Message);
        Assert.AreEqual(7, diagnostic.Location.Offset);
        Assert.AreEqual(8, diagnostic.EndLocation.Offset);
        Assert.AreEqual("query.musoq", diagnostic.Location.FilePath);
        Assert.AreEqual("select 1", diagnostic.ContextSnippet);
    }
}
