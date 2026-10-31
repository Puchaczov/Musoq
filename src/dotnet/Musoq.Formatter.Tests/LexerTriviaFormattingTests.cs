using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class LexerTriviaFormattingTests
{
    [TestMethod]
    [DataRow("select 2 /* keep */ -1 from #s.rows()", "SELECT 2 /* keep */ - 1\nFROM #s.rows()\n")]
    [DataRow("select 2 -- keep\n-1 from #s.rows()", "SELECT\n    2 -- keep\n    - 1\nFROM #s.rows()\n")]
    [DataRow("let items: int[] = array {1}; select $items /* keep */ [0] from #s.rows()",
        "LET items: int[] = ARRAY { 1 };\n\nSELECT $items /* keep */[0]\nFROM #s.rows()\n")]
    public void CommentSeparatedExpression_ShouldPreserveSyntaxAndLayout(string source, string expected)
    {
        var result = MusoqFormatter.Format(source);

        Assert.AreEqual(expected, result.FormattedText);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(expected).ToArray());
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(expected));
        Assert.AreEqual(expected, MusoqFormatter.Format(expected).FormattedText);
    }
}
