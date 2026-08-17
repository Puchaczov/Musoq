using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SyntaxDiagnosticFactoryTests
{
    [TestMethod]
    public void CreateDiagnostic_PreservesStructuredSyntaxMetadata()
    {
        var source = new SourceText("selct");

        var diagnostic = SyntaxDiagnosticFactory.CreateDiagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Unexpected token.",
            new TextSpan(0, 5),
            currentToken: null,
            source);

        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(0, diagnostic.Location.Offset);
        Assert.AreEqual(5, diagnostic.EndLocation.Offset);
        Assert.IsNotNull(diagnostic.ContextSnippet);
        Assert.IsNotNull(diagnostic.Explanation);
        Assert.IsNotNull(diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void EnhanceLexerDiagnostic_PreservesRelatedInformationAndLocation()
    {
        var source = new SourceText("@");

        var diagnostic = SyntaxDiagnosticFactory.EnhanceLexerDiagnostic(
            DiagnosticCode.MQ1001_UnknownToken,
            "Unknown token.",
            new TextSpan(0, 1),
            source,
            new[] { "near SELECT" });

        Assert.AreEqual(DiagnosticCode.MQ1001_UnknownToken, diagnostic.Code);
        Assert.AreEqual(1, diagnostic.EndLocation.Offset);
        CollectionAssert.AreEqual(new[] { "near SELECT" }, diagnostic.RelatedInfo.ToArray());
        Assert.IsNotNull(diagnostic.ContextSnippet);
        Assert.IsNotNull(diagnostic.Explanation);
        Assert.IsNotNull(diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }
}
