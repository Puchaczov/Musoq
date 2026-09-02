using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore009SelectTests
{
    [TestMethod]
    public void SelectExpressionForms_ShouldBuildExpectedExpressionNodes()
    {
        const string query =
            "select 1, Name, a.Name, 1 + 2 * 3, a.GetPopulation(), Self.Name, Self.Array[2] from #some.entities() a";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var fields = GetQuery(result.Root!).Select.Fields;
        Assert.HasCount(7, fields);
        Assert.IsInstanceOfType<IntegerNode>(fields[0].Expression);
        Assert.IsInstanceOfType<IdentifierNode>(fields[1].Expression);

        var qualifiedColumn = Assert.IsInstanceOfType<DotNode>(fields[2].Expression);
        Assert.AreEqual("a.Name", qualifiedColumn.ToString());

        var arithmetic = Assert.IsInstanceOfType<AddNode>(fields[3].Expression);
        Assert.IsInstanceOfType<StarNode>(arithmetic.Right);

        var entityMethod = Assert.IsInstanceOfType<AccessMethodNode>(fields[4].Expression);
        Assert.AreEqual("a", entityMethod.Alias);
        Assert.AreEqual("GetPopulation", entityMethod.Name);

        var propertyAccess = Assert.IsInstanceOfType<DotNode>(fields[5].Expression);
        Assert.AreEqual("Self.Name", propertyAccess.ToString());

        var indexedAccess = Assert.IsInstanceOfType<DotNode>(fields[6].Expression);
        Assert.AreEqual("Self.Array[2]", indexedAccess.ToString());
        Assert.IsInstanceOfType<AccessObjectArrayNode>(indexedAccess.Expression);
    }

    [TestMethod]
    public void SelectAliases_ShouldPreserveExplicitAndImplicitOutputNames()
    {
        const string query =
            "select Name as FullName, Name ImplicitName, Name [Full Name], 1, Name, Count(Name) from #some.entities()";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var fields = GetQuery(result.Root!).Select.Fields;
        CollectionAssert.AreEqual(
            new[] { "FullName", "ImplicitName", "Full Name", "1", "Name", "Count(Name)" },
            fields.Select(field => field.FieldName).ToArray());
        CollectionAssert.AreEqual(
            new[] { true, true, true, false, false, false },
            fields.Select(field => field.HasExplicitFieldName).ToArray());
    }

    private static QueryNode GetQuery(RootNode root)
    {
        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        var statement = Assert.IsInstanceOfType<SingleSetNode>(statements.Statements.Single().Node);
        return statement.Query;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
