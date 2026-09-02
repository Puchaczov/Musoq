using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticUx064SourceContextTests
{
    [TestMethod]
    public void LegacyDiagnosticExceptionConversion_ShouldResolveMultilineLocationAndSnippet()
    {
        const string query = "select 1\r\nfrom #test.people()";
        var sourceText = new SourceText(query);
        var start = query.IndexOf("from", StringComparison.Ordinal);
        var span = new TextSpan(start, "from".Length);
        var exception = new ObjectIsNotAnArrayException("not an array", span);

        var diagnostic = exception.ToDiagnosticOrGeneric(sourceText);

        Assert.AreEqual(span, diagnostic.Span);
        Assert.AreEqual(2, diagnostic.Location.Line);
        Assert.AreEqual(1, diagnostic.Location.Column);
        Assert.AreEqual(2, diagnostic.EndLocation.Line);
        Assert.AreEqual(5, diagnostic.EndLocation.Column);
        StringAssert.Contains(diagnostic.ContextSnippet!, "from #test.people()");
    }

    [TestMethod]
    public void LegacyDiagnosticExceptionWithoutSpan_ShouldRemainUnknown()
    {
        var sourceText = new SourceText("select 1\nfrom #test.people()");
        var exception = new ObjectIsNotAnArrayException("not an array");

        var envelope = MusoqErrorEnvelope.FromException(exception, sourceText.Text);

        Assert.IsNull(envelope.Line);
        Assert.IsNull(envelope.Column);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.EndOffset);
        Assert.IsNull(envelope.Snippet);
    }

    [TestMethod]
    public void ReportException_WithExplicitZeroLengthSpan_ShouldKeepInsertionLocation()
    {
        const string query = "select\nfrom #test.people()";
        var sourceText = new SourceText(query);
        var start = query.IndexOf("from", StringComparison.Ordinal);
        var insertion = new TextSpan(start, 0);
        var context = new DiagnosticContext(sourceText);

        context.ReportException(new InvalidOperationException("internal failure"), insertion);

        var diagnostic = context.Errors.Single();
        Assert.AreEqual(insertion, diagnostic.Span);
        Assert.AreEqual(2, diagnostic.Location.Line);
        Assert.AreEqual(1, diagnostic.Location.Column);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
    }
}
