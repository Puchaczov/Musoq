using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class UnpivotKeywordCasingTests
{
    [TestMethod]
    [DataRow(KeywordCasing.Upper, "USING using KEEP s.Region AS keep")]
    [DataRow(KeywordCasing.Lower, "using using keep s.Region as keep")]
    [DataRow(KeywordCasing.Preserve, "UsInG using KeEp s.Region aS keep")]
    public void ContextualKeywords_ShouldHonorCasingWithoutChangingNames(KeywordCasing casing, string expected)
    {
        const string source = "unpivot values { (Region: 'North', Q1: 10d) } s on Quarter in (s.Q1 as Q1) UsInG using KeEp s.Region aS keep";
        var options = new FormattingOptions { KeywordCasing = casing };
        var result = MusoqFormatter.Format(source, options);

        Assert.IsTrue(result.IsSuccess);
        StringAssert.Contains(result.FormattedText, expected);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(result.FormattedText!).ToArray());
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(result.FormattedText!));
        Assert.AreEqual(result.FormattedText, MusoqFormatter.Format(result.FormattedText!, options).FormattedText);
    }
}
