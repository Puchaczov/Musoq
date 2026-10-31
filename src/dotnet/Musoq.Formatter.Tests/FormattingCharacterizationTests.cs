using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class FormattingCharacterizationTests
{
    [TestMethod]
    [DataRow("couple #some.table with table Test as Bound;", "COUPLE #some.table WITH TABLE Test AS Bound;\n")]
    [DataRow("couple #some.[table name] with settings profile as Bound;", "COUPLE #some.[table name] WITH SETTINGS profile AS Bound;\n")]
    [DataRow("select ((a.X)) Named from #s.rows() a;", "SELECT ((a.X)) Named\nFROM #s.rows() a;\n")]
    [DataRow("from #s.rows() a select a.Name", "FROM #s.rows() a\nSELECT a.Name\n")]
    [DataRow("select 1 Value from #s.rows();select 2 Value from #s.rows()", "SELECT 1 Value\nFROM #s.rows();\n\nSELECT 2 Value\nFROM #s.rows()\n")]
    public void ConformingSyntax_ShouldKeepReviewedOutputAndParserLabels(string source, string expected)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(expected));
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
    }
}
