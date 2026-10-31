using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class FormattingRobustnessTests
{
    [TestMethod]
    [DynamicData(nameof(GeneratedSources.Cases), typeof(GeneratedSources))]
    public void GeneratedSource_ShouldPreserveTokensStructureAndLabels(int seed, string source)
    {
        var result = MusoqFormatter.Format(source, new FormattingOptions { LineWidth = 60 });

        Assert.IsTrue(result.IsSuccess, "seed=" + seed);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(result.FormattedText!).ToArray(), "seed=" + seed);
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(result.FormattedText!), "seed=" + seed);
        Assert.AreEqual(result.FormattedText, MusoqFormatter.Format(result.FormattedText!, new FormattingOptions { LineWidth = 60 }).FormattedText, "seed=" + seed);
    }

    [TestMethod]
    public async Task ConcurrentCalls_ShouldReuseImmutableOptionsWithoutLeakingState()
    {
        var options = new FormattingOptions { LineWidth = 18, IndentSize = 2 };
        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
            MusoqFormatter.Format("select Alpha,Beta,Gamma from #s.rows()", options))));

        Assert.IsTrue(results.All(r => r.IsSuccess && r.FormattedText == "SELECT\n  Alpha,\n  Beta,\n  Gamma\nFROM #s.rows()\n"));
        Assert.AreEqual(18, options.LineWidth);
        Assert.AreEqual(2, options.IndentSize);
    }

    [TestMethod]
    public void TurkishCulture_ShouldNotAffectKeywordsOrIdentifiers()
    {
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var result = MusoqFormatter.Format("select İsim Label from #s.rows() where İsim is null");

            Assert.AreEqual("SELECT İsim Label\nFROM #s.rows()\nWHERE\n    İsim IS null\n", result.FormattedText);
        }
        finally
        {
            CultureInfo.CurrentCulture = prior;
        }
    }

    [TestMethod]
    public void NestedParentheses_ShouldRemainExplicitAndStable()
    {
        var source = "select " + new string('(', 32) + "a.Value+1" + new string(')', 32) + " from #s.rows() a";
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(result.FormattedText!).ToArray());
        Assert.AreEqual(result.FormattedText, MusoqFormatter.Format(result.FormattedText!).FormattedText);
    }

    [TestMethod]
    public void LargeScript_ShouldPreserveAllStatementsAndSeparators()
    {
        var source = string.Join(";", Enumerable.Range(0, 80).Select(index => "select " + index + " Value from #s.rows()"));
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(result.FormattedText!).ToArray());
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(result.FormattedText!));
    }

    [TestMethod]
    [DataRow("select r'C:\\Temp\\file' Path from #s.rows()")]
    [DataRow("select r'a''b' Value from #s.rows()")]
    [DataRow("select [Order], TrUe, null, 0xAb, 1UL from #s.rows()")]
    [DataRow("select 'a\n\nb' Value from #s.rows()")]
    [DataRow("select - -1 Value from #s.rows()") ]
    [DataRow("select - 1 Value from #s.rows()") ]
    public void ProtectedSyntax_ShouldSurviveAllPresentationOptions(string source)
    {
        var result = MusoqFormatter.Format(source, new FormattingOptions { KeywordCasing = KeywordCasing.Lower, LineEnding = LineEnding.CrLf, LineWidth = 25 });

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(result.FormattedText!).ToArray());
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(result.FormattedText!));
    }

    [TestMethod]
    public void InvalidLineEndingValue_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MusoqFormatter.Format("", new FormattingOptions { LineEnding = (LineEnding)99 }));
    }

    [TestMethod]
    public void SeparateUnarySigns_ShouldNotBecomeACommentOrChangeNumericTokenization()
    {
        var result = MusoqFormatter.Format("select - -1, - 1 from #s.rows()");

        Assert.AreEqual("SELECT - -1, - 1\nFROM #s.rows()\n", result.FormattedText);
        Assert.IsTrue(Parser.Parser.ParseSyntax(result.FormattedText!).Success);
    }
}
