using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class CorpusPreservationTests
{
    public static IEnumerable<object[]> SpecificationCases => CorpusFixtures.Load()
        .Select(fixture => new object[] { fixture.Id, fixture.Source });

    [TestMethod]
    [DynamicData(nameof(SpecificationCases))]
    public void SpecificationQuery_ShouldPreserveSourceStructureAndLabels(string id, string source)
    {
        var result = MusoqFormatter.Format(source);

        Assert.IsTrue(result.IsSuccess, id + ": " + string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.IsTrue(Parser.Parser.ParseSyntax(result.FormattedText!).Success, id);
        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(result.FormattedText!).ToArray(), id);
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(result.FormattedText!), id);
        Assert.AreEqual(result.FormattedText, MusoqFormatter.Format(result.FormattedText!).FormattedText, id);
    }
}
