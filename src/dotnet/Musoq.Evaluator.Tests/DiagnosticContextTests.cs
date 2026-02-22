using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DiagnosticContextTests
{
    [TestMethod]
    public void ReportException_ShouldUseConvertedDiagnosticMessageAndCode()
    {
        var context = new DiagnosticContext(new SourceText("SELECT 1"));
        var innerException = SyntaxException.InvalidExpression("invalid expr", "SELECT 1", new TextSpan(0, 6));
        var exception = new InvalidOperationException("wrapper message", innerException);

        context.ReportException(exception);

        var diagnostic = context.Diagnostics.Single();

        Assert.AreEqual(DiagnosticCode.MQ2003_InvalidExpression, diagnostic.Code);
        Assert.AreEqual(innerException.Message, diagnostic.Message);
    }
}
