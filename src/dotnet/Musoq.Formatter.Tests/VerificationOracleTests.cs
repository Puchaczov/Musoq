using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class VerificationOracleTests
{
    [TestMethod]
    [DataRow("select Name from #s.rows()", "select Other from #s.rows()")]
    [DataRow("select 'value' from #s.rows()", "select 'other' from #s.rows()")]
    [DataRow("select 'a b' from #s.rows()", "select 'ab' from #s.rows()")]
    [DataRow("select 0x10 from #s.rows()", "select 16 from #s.rows()")]
    [DataRow("select a.X + 1 from #s.rows() a", "select a.X - 1 from #s.rows() a")]
    [DataRow("select (Name) from #s.rows()", "select Name from #s.rows()")]
    [DataRow("select Name from #s.rows();", "select Name from #s.rows()")]
    [DataRow("select Name, Other from #s.rows()", "select Name Other from #s.rows()")]
    [DataRow("select Name /* keep */ from #s.rows()", "select Name /* changed */ from #s.rows()")]
    [DataRow("select Name as Label from #s.rows()", "select Name as Other from #s.rows()")]
    [DataRow("select Name as Label from #s.rows()", "select Name Label from #s.rows()")]
    [DataRow("couple #s.[two words] with table s as a", "couple #s.[twowords] with table s as a")]
    public void SourceOracle_ShouldRejectProtectedSyntaxMutations(string original, string mutation)
    {
        Assert.IsTrue(Parser.Parser.ParseSyntax(original).Success);
        Assert.IsTrue(Parser.Parser.ParseSyntax(mutation).Success);
        Assert.IsFalse(SourceSignature.Of(original).SequenceEqual(SourceSignature.Of(mutation)));
    }

    [TestMethod]
    [DataRow("select Name from #s.rows()", "select Other from #s.rows()")]
    [DataRow("select 1 from #s.rows()", "select 2 from #s.rows()")]
    [DataRow("select a.X + 1 from #s.rows() a", "select a.X - 1 from #s.rows() a")]
    [DataRow("select Name as Label from #s.rows()", "select Name as Other from #s.rows()")]
    [DataRow("select a.X + 1 from #s.rows() a", "select 1 + a.X from #s.rows() a")]
    [DataRow("select (a.X + 1) * 2 from #s.rows() a", "select a.X + 1 * 2 from #s.rows() a")]
    [DataRow("select Name, Other from #s.rows()", "select Name Other from #s.rows()")]
    [DataRow("profile select Name from #s.rows()", "explain analyze select Name from #s.rows()")]
    public void StructureOracle_ShouldRejectSemanticAndFieldLabelMutations(string original, string mutation)
    {
        Assert.AreNotEqual(AstSignature.Parse(original), AstSignature.Parse(mutation));
    }

    [TestMethod]
    public void Oracles_ShouldAllowStructuralKeywordCasingAndWhitespaceChanges()
    {
        const string source = "select Name from #s.rows() a where a.X > 1";
        const string formatted = "SELECT Name\nFROM #s.rows() a\nWHERE\n    a.X > 1\n";

        CollectionAssert.AreEqual(SourceSignature.Of(source).ToArray(), SourceSignature.Of(formatted).ToArray());
        Assert.AreEqual(AstSignature.Parse(source), AstSignature.Parse(formatted));
    }
}
