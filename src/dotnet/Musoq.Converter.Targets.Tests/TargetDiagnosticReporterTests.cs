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
            new TargetSourceRange(7, 1, 3, 4, 3, 5),
            "generated-query.g.cs",
            "select 1");

        TargetDiagnosticReporter.Report([targetDiagnostic], context);

        var diagnostic = context.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ8001_CodeGenerationFailed, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.CodeGeneration, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, diagnostic.SourceKind);
        Assert.AreEqual("[MT4321] target warning", diagnostic.Message);
        Assert.AreEqual("MT4321", diagnostic.Arguments["targetCode"]);
        Assert.AreEqual(7, diagnostic.Location.Offset);
        Assert.AreEqual(8, diagnostic.EndLocation.Offset);
        Assert.AreEqual(3, diagnostic.Location.Line);
        Assert.AreEqual(4, diagnostic.Location.Column);
        Assert.AreEqual("generated-query.g.cs", diagnostic.Location.FilePath);
        Assert.AreEqual("select 1", diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void Report_WhenTargetHasNoRange_ShouldKeepTheLocationUnknownInsteadOfUsingSqlOffsetZero()
    {
        var context = new DiagnosticContext(new SourceText("select 1", "query.musoq"));
        var targetDiagnostic = new TargetDiagnostic(
            "MT9999",
            TargetDiagnosticSeverity.Error,
            "target failure");

        TargetDiagnosticReporter.Report([targetDiagnostic], context);

        var diagnostic = context.Diagnostics.Single();
        Assert.AreEqual(SourceLocation.None, diagnostic.Location);
        Assert.AreEqual(SourceLocation.None, diagnostic.EndLocation);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, diagnostic.SourceKind);
        Assert.AreEqual(DiagnosticPhase.CodeGeneration, diagnostic.Phase);
    }
}
