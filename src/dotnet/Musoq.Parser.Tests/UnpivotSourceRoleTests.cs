using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Syntax;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class UnpivotSourceRoleTests
{
    [TestMethod]
    [DataRow("using")]
    [DataRow("keep")]
    public void ContextualControlWord_ShouldDifferFromTheSameSpelledIdentifier(string spelling)
    {
        const string source = "unpivot values { (Region: 'North', Q1: 10d) } s on Quarter in (s.Q1 as Q1) using using keep s.Region as keep";
        var syntax = Parser.ParseSyntax(source);
        var roles = syntax.Tokens.Where(token => token.Text == spelling).Select(token => token.Role).ToArray();

        Assert.IsTrue(syntax.Success);
        CollectionAssert.AreEqual(new[] { SyntaxTokenRole.Keyword, SyntaxTokenRole.Identifier }, roles);
        Assert.AreEqual(source, string.Concat(syntax.Tokens.Select(token => token.Text)));
    }
}
