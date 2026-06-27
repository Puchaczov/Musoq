using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserStarRenameTests
{
    [TestMethod]
    public void StarRename_ShouldParseRenameItems()
    {
        var star = ParseStar("select * rename (Name as EntityName, City as Place) from #some.a()");

        Assert.IsNotNull(star.RenameItems);
        Assert.HasCount(2, star.RenameItems);
        Assert.AreEqual("Name", star.RenameItems[0].SourceName);
        Assert.AreEqual("EntityName", star.RenameItems[0].TargetName);
        Assert.AreEqual("City", star.RenameItems[1].SourceName);
        Assert.AreEqual("Place", star.RenameItems[1].TargetName);
    }

    [TestMethod]
    public void StarRename_WithQualifiedSource_ShouldParseSourceName()
    {
        var star = ParseStar("select a.* rename (a.Name as EntityName) from #some.a() a");

        Assert.IsNotNull(star.RenameItems);
        Assert.HasCount(1, star.RenameItems);
        Assert.AreEqual("a.Name", star.RenameItems[0].SourceName);
        Assert.AreEqual("EntityName", star.RenameItems[0].TargetName);
    }

    [TestMethod]
    public void StarRename_WithEarlierModifiers_ShouldPreserveModifierOrder()
    {
        var star = ParseStar(
            "select * like 'C%' exclude (Country) replace (City + '!' as City) rename (City as Location) from #some.a()");

        Assert.AreEqual("C%", star.LikePattern);
        Assert.IsNotNull(star.ExcludeColumns);
        CollectionAssert.AreEqual(new[] { "Country" }, star.ExcludeColumns);
        Assert.IsNotNull(star.ReplaceItems);
        Assert.HasCount(1, star.ReplaceItems);
        Assert.IsNotNull(star.RenameItems);
        Assert.HasCount(1, star.RenameItems);
        Assert.AreEqual("Location", star.RenameItems[0].TargetName);
    }

    [TestMethod]
    public void StarRename_BeforeReplace_ShouldReportOutOfOrderModifier()
    {
        var ex = Assert.Throws<SyntaxException>(() =>
            ParseStar("select * rename (Name as EntityName) replace (1 as City) from #some.a()"));

        StringAssert.Contains(ex.Message, "Expected order: LIKE/NOT LIKE, EXCLUDE, REPLACE, RENAME");
    }

    private static AllColumnsNode ParseStar(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var root = parser.ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;

        return (AllColumnsNode)singleSet.Query.Select.Fields[0].Expression;
    }
}
