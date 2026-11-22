using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class BinaryFieldOffsetDiagnosticsTests
{
    [TestMethod]
    public void OffsetBeforeType_ShouldPointToTheAcceptedFieldOrder()
    {
        const string sql = "binary Header { HeaderBytes: at 0 byte[1] };";

        var result = Parser.ParseSyntax(sql);

        var error = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCode.MQ2001_UnexpectedToken);
        StringAssert.Contains(error.Message, "HeaderBytes: byte[1] at 0");
    }

    [TestMethod]
    public void OffsetAfterType_ShouldParse()
    {
        var result = Parser.ParseSyntax("binary Header { HeaderBytes: byte[1] at 0 };");

        Assert.IsFalse(result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
