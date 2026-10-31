using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class FormattingFoundationTests
{
    [TestMethod]
    [DataRow("select a.Name from #items.all() a", "SELECT a.Name\nFROM #items.all() a\n")]
    [DataRow("select a,b,c from #s.rows();", "SELECT a, b, c\nFROM #s.rows();\n")]
    [DataRow("select -1,0xFF,true,NULL from #s.rows()", "SELECT -1, 0xFF, true, NULL\nFROM #s.rows()\n")]
    [DataRow("select (a.Price+1) Label from #items.all() a", "SELECT (a.Price + 1) Label\nFROM #items.all() a\n")]
    [DataRow("", "")]
    [DataRow("  \r\n\t", "")]
    [DataRow("-- keep  ", "-- keep  \n")]
    [DataRow("/* keep\r\n  exact */", "/* keep\r\n  exact */\n")]
    public void DefaultOptions_ShouldProduceExactOutput(string source, string expected)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
    }

    [TestMethod]
    public void LongProjection_ShouldWrapOneItemPerLine()
    {
        var result = MusoqFormatter.Format("select Alpha,Beta,Gamma from #s.rows()", new FormattingOptions { LineWidth = 20 });

        Assert.AreEqual("SELECT\n    Alpha,\n    Beta,\n    Gamma\nFROM #s.rows()\n", result.FormattedText);
    }

    [TestMethod]
    public void ExactWidthProjection_ShouldStayInline()
    {
        var result = MusoqFormatter.Format("select Alpha,Beta from #s.rows()", new FormattingOptions { LineWidth = 18 });

        Assert.AreEqual("SELECT Alpha, Beta\nFROM #s.rows()\n", result.FormattedText);
    }

    [TestMethod]
    public void KeywordOptions_ShouldPreserveIdentifierAndLiteralSpelling()
    {
        var result = MusoqFormatter.Format("SELECT 'SELECT' AS Mixed FROM #Some.Source() Alias", new FormattingOptions { KeywordCasing = KeywordCasing.Lower });

        Assert.AreEqual("select 'SELECT' as Mixed\nfrom #Some.Source() Alias\n", result.FormattedText);
    }

    [TestMethod]
    public void PreserveKeywordOption_ShouldKeepEachKeywordOccurrence()
    {
        var result = MusoqFormatter.Format("sElEcT One FrOm #s.rows()", new FormattingOptions { KeywordCasing = KeywordCasing.Preserve });

        Assert.AreEqual("sElEcT One\nFrOm #s.rows()\n", result.FormattedText);
    }

    [TestMethod]
    public void ExistingCrLf_ShouldControlGeneratedLineBreaks()
    {
        var result = MusoqFormatter.Format("select One\r\nfrom #s.rows()");

        Assert.AreEqual("SELECT One\r\nFROM #s.rows()\r\n", result.FormattedText);
    }

    [TestMethod]
    public void ExplicitLf_ShouldPreserveEmbeddedLiteralLineBreaks()
    {
        var result = MusoqFormatter.Format("select 'a\r\nb' from #s.rows()", new FormattingOptions { LineEnding = LineEnding.Lf });

        Assert.IsTrue(result.IsSuccess);
        StringAssert.Contains(result.FormattedText, "'a\r\nb'");
        Assert.IsTrue(result.FormattedText!.EndsWith("FROM #s.rows()\n", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("select @ from #s.rows()")]
    [DataRow("select 'missing")]
    [DataRow("select One from #s.rows(); select @ from #s.rows()")]
    public void InvalidScript_ShouldReturnDiagnosticsWithoutPartialOutput(string source)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.FormattedText);
        Assert.IsTrue(result.Diagnostics.Count > 0);
    }

    [TestMethod]
    public void WhereQuery_ShouldUseExpandedPredicateLayout()
    {
        var result = MusoqFormatter.Format("select One from #s.rows() where One > 0");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SELECT One\nFROM #s.rows()\nWHERE\n    One > 0\n", result.FormattedText);
    }

    [TestMethod]
    public void NullSource_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => MusoqFormatter.Format(null!));
    }

    [TestMethod]
    public void InvalidWidth_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MusoqFormatter.Format("", new FormattingOptions { LineWidth = 0 }));
    }

    [TestMethod]
    public void InvalidIndentation_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MusoqFormatter.Format("", new FormattingOptions { IndentSize = -1 }));
    }

    [TestMethod]
    public void InvalidCasing_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MusoqFormatter.Format("", new FormattingOptions { KeywordCasing = (KeywordCasing)99 }));
    }
}
