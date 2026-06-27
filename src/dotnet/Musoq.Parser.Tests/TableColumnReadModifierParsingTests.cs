using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class TableColumnReadModifierParsingTests
{
    [TestMethod]
    public void TableColumnReadModifiers_WhenValid_ShouldParse()
    {
        var table = ParseTable(
            "table LegacyRecord { Id: int, Name: string encoding 'windows-1250' trim, Amount: decimal culture 'pl-PL' format '#,##0.00', Payload: string source codec 'base64' };");

        Assert.AreEqual(4, table.Columns.Count);

        var nameColumn = table.Columns.Single(column => column.ColumnName == "Name");
        Assert.AreEqual("string", nameColumn.TypeName);
        Assert.AreEqual(2, nameColumn.ReadModifiers.Count);
        Assert.AreEqual("encoding", nameColumn.ReadModifiers[0].Key);
        Assert.AreEqual("windows-1250", nameColumn.ReadModifiers[0].Value);
        Assert.AreEqual("trim", nameColumn.ReadModifiers[1].Key);
        Assert.AreEqual("true", nameColumn.ReadModifiers[1].Value);

        var amountColumn = table.Columns.Single(column => column.ColumnName == "Amount");
        Assert.AreEqual("culture", amountColumn.ReadModifiers[0].Key);
        Assert.AreEqual("pl-PL", amountColumn.ReadModifiers[0].Value);
        Assert.AreEqual("format", amountColumn.ReadModifiers[1].Key);
        Assert.AreEqual("#,##0.00", amountColumn.ReadModifiers[1].Value);

        var payloadColumn = table.Columns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual("source.codec", payloadColumn.ReadModifiers[0].Key);
        Assert.AreEqual("base64", payloadColumn.ReadModifiers[0].Value);
    }

    [TestMethod]
    public void TableColumnType_WhenQualified_ShouldParse()
    {
        var table = ParseTable("table T { Payload: System.SomeCustomType };");

        var payloadColumn = table.Columns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual("System.SomeCustomType", payloadColumn.TypeName);
    }

    [TestMethod]
    public void TableColumnType_WhenQualifiedNullable_ShouldParse()
    {
        var table = ParseTable("table T { Payload: System.SomeCustomType? };");

        var payloadColumn = table.Columns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual("System.SomeCustomType?", payloadColumn.TypeName);
    }

    [TestMethod]
    public void TableColumnType_WhenQualifiedAndReadModifiersFollow_ShouldParse()
    {
        var table = ParseTable("table T { Payload: System.String encoding 'utf-8' trim };");

        var payloadColumn = table.Columns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual("System.String", payloadColumn.TypeName);
        Assert.AreEqual("encoding", payloadColumn.ReadModifiers[0].Key);
        Assert.AreEqual("utf-8", payloadColumn.ReadModifiers[0].Value);
        Assert.AreEqual("trim", payloadColumn.ReadModifiers[1].Key);
        Assert.AreEqual("true", payloadColumn.ReadModifiers[1].Value);
    }

    [TestMethod]
    public void TableColumnReadModifiers_WhenDuplicated_ShouldFailWithInvalidSchemaDefinition()
    {
        var exception = Assert.Throws<SyntaxException>(() =>
            ParseTable("table LegacyRecord { Name: string encoding 'utf-8' encoding 'windows-1250' };"));

        Assert.AreEqual(DiagnosticCode.MQ2012_InvalidSchemaDefinition, exception.Code);
    }

    [TestMethod]
    public void TableColumnReadModifiers_ToString_ShouldIncludeNormalizedModifiers()
    {
        var table = ParseTable(
            "table LegacyRecord { Name: string encoding 'windows-1250' trim, Payload: string source codec 'base64' };");

        Assert.AreEqual(
            "table LegacyRecord { Name: string encoding 'windows-1250' trim, Payload: string source codec 'base64' };",
            table.ToString());
    }

    private static CreateTableNode ParseTable(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var root = parser.ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return (CreateTableNode)statements.Statements[0].Node;
    }
}
