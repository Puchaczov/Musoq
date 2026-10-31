using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class ScriptFormattingTests
{
    [TestMethod]
    [DataRow("let count: int=1;select $count from #s.rows()", "LET count: int = 1;\n\nSELECT $count\nFROM #s.rows()\n")]
    [DataRow("params(name: string) select $name from #s.rows()", "PARAMS(name: string)\n\nSELECT $name\nFROM #s.rows()\n")]
    [DataRow("select One from #s.rows(); select Two from #s.rows()", "SELECT One\nFROM #s.rows();\n\nSELECT Two\nFROM #s.rows()\n")]
    [DataRow("select One from #s.rows(); -- trailing\nselect Two from #s.rows()", "SELECT One\nFROM #s.rows(); -- trailing\n\nSELECT Two\nFROM #s.rows()\n")]
    [DataRow("binary Header { Magic: int le, Version: short le };", "BINARY Header {\n    Magic: int le,\n    Version: short le\n};\n")]
    [DataRow("text LogEntry { Level: until ':', Message: rest };", "TEXT LogEntry {\n    Level: UNTIL ':',\n    Message: REST\n};\n")]
    [DataRow("binary Rec { Items: substream[1] as { Value: byte } exact[2] }", "BINARY Rec {\n    Items: SUBSTREAM[1] AS {\n        Value: byte\n    } exact[2]\n}\n")]
    [DataRow("binary Pair<T,U> { First:T,Second:U }; binary Holder { Value:Pair<byte,Pair<int,short>> }", "BINARY Pair<T, U> {\n    First: T,\n    Second: U\n};\n\nBINARY Holder {\n    Value: Pair<byte, Pair<int, short>>\n}\n")]
    [DataRow("flags enum Access: byte { None=0, Read=1 }; select Access.Read from #s.rows()", "FLAGS ENUM Access: byte {\n    None = 0,\n    Read = 1\n};\n\nSELECT Access.Read\nFROM #s.rows()\n")]
    [DataRow("table Invoice { ProductName: string, Price: decimal };", "TABLE Invoice {\n    ProductName: string,\n    Price: decimal\n};\n")]
    [DataRow("desc #s.rows()", "DESC #s.rows()\n")]
    [DataRow("profile select a.Name from #s.rows() a where a.Flag=1", "PROFILE\n    SELECT a.Name\n    FROM #s.rows() a\n    WHERE\n        a.Flag = 1\n")]
    [DataRow("explain analyze select Name from #s.rows()", "EXPLAIN ANALYZE\n    SELECT Name\n    FROM #s.rows()\n")]
    public void CompleteScript_ShouldMatchReviewedLayout(string source, string expected)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expected, result.FormattedText);
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
        Assert.IsTrue(Parser.Parser.ParseSyntax(expected).Success);
    }

    [TestMethod]
    [DataRow("profile select Name")]
    [DataRow("explain analyze select Name")]
    public void MalformedEmbeddedQuery_ShouldRejectTheEntireScript(string source)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.FormattedText);
        Assert.IsTrue(result.Diagnostics.Count > 0);
    }
}
