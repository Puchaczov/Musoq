using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SourceNameCharacterizationTests
{
    public static IEnumerable<object[]> Names
    {
        get
        {
            yield return ["table", "table"];
            yield return ["something4", "something4"];
            yield return ["[table name]", "table name"];
            yield return ["*", "*"];
            yield return ["Δείγμα", "Δείγμα"];
            yield return ["𐐀Name", "𐐀Name"];
        }
    }

    [TestMethod]
    [DynamicData(nameof(Names))]
    public void SourceMethodName_ShouldRetainItsParsedValue(string spelling, string expected)
    {
        var source = $"couple #some.{spelling} with table Test as Bound;";
        var root = new Parser(new Lexer(source, true)).ComposeAll();
        var statement = ((StatementsArrayNode)root.Expression).Statements.Single();

        Assert.AreEqual(expected, ((CoupleNode)statement.Node).SchemaMethodNode.Method);
    }

    [TestMethod]
    [DynamicData(nameof(Names))]
    public void SourceMethodName_ShouldReconstructExactSource(string spelling, string expected)
    {
        var source = $"couple #some.{spelling} with table Test as Bound;";
        var result = Parser.ParseSyntax(source);

        Assert.IsTrue(result.Success, expected);
        Assert.AreEqual(source, string.Concat(result.Tokens.Select(token => token.Text)));
        Assert.AreEqual(source, result.Root!.ToFullString());
    }
}
