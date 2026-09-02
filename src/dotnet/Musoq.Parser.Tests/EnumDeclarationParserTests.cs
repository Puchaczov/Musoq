using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class EnumDeclarationParserTests
{
    [TestMethod]
    public void EnumDeclaration_ShouldPreserveNominalShapeValuesAndSpans()
    {
        const string query = "enum JobStatus : int { Queued = 10, Running = 20i, Finished = 0x1e, };";

        var declaration = ParseSingleEnum(query);

        Assert.AreEqual("JobStatus", declaration.Name);
        Assert.AreEqual("int", declaration.UnderlyingTypeName);
        Assert.IsFalse(declaration.IsFlags);
        Assert.AreEqual(SpanOf(query, "JobStatus"), declaration.NameSpan);
        Assert.AreEqual(SpanOf(query, "int"), declaration.UnderlyingTypeSpan);
        Assert.AreEqual(new TextSpan(0, query.IndexOf('}') + 1), declaration.Span);
        CollectionAssert.AreEqual(
            new[] { "Queued", "Running", "Finished" },
            declaration.Members.Select(static member => member.Name).ToArray());
        CollectionAssert.AreEqual(
            new ulong[] { 10, 20, 30 },
            declaration.Members.Select(static member => member.RawValue).ToArray());
        CollectionAssert.AreEqual(
            new[] { "10", "20i", "0x1e" },
            declaration.Members.Select(static member => member.LiteralText).ToArray());
        Assert.AreEqual(SpanOf(query, "Queued = 10"), declaration.Members[0].Span);
        Assert.AreEqual(SpanOf(query, "10"), declaration.Members[0].ValueSpan);
        Assert.AreEqual(
            "enum JobStatus : int { Queued = 10, Running = 20i, Finished = 0x1e };",
            declaration.ToString());
    }

    [TestMethod]
    public void FlagsEnumDeclaration_ShouldAllowAliasesZeroAtomicAndNamedComposite()
    {
        const string query =
            "FlAgS EnUm FileAccess : uint { None = 0ui, Read = 1ui, View = 1ui, Write = 2ui, ReadWrite = 3ui };";

        var declaration = ParseSingleEnum(query);

        Assert.IsTrue(declaration.IsFlags);
        Assert.AreEqual("uint", declaration.UnderlyingTypeName);
        CollectionAssert.AreEqual(
            new ulong[] { 0, 1, 1, 2, 3 },
            declaration.Members.Select(static member => member.RawValue).ToArray());
    }

    [TestMethod]
    public void EnumDeclarations_ShouldSupportEveryIntegralBackingAndBoundary()
    {
        const string query = """
                             enum EByte : byte { Min = 0ub, Max = 255ub };
                             enum ESByte : sbyte { Min = -128, Max = 127b };
                             enum EShort : short { Min = -32768, Max = 32767s };
                             enum EUShort : ushort { Min = 0us, Max = 65535us };
                             enum EInt : int { Min = -2147483648, Max = 2147483647i };
                             enum EUInt : uint { Min = 0ui, Max = 4294967295ui };
                             enum ELong : long { Min = -9223372036854775808, Max = 9223372036854775807l };
                             enum EULong : ulong { Min = 0ul, Max = 18446744073709551615ul };
                             """;

        var declarations = ParseEnums(query);

        Assert.HasCount(8, declarations);
        CollectionAssert.AreEqual(
            new[] { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong" },
            declarations.Select(static declaration => declaration.UnderlyingTypeName).ToArray());
        Assert.AreEqual(128ul, declarations[1].Members[0].RawValue);
        Assert.AreEqual(32768ul, declarations[2].Members[0].RawValue);
        Assert.AreEqual(2147483648ul, declarations[4].Members[0].RawValue);
        Assert.AreEqual(9223372036854775808ul, declarations[6].Members[0].RawValue);
        Assert.AreEqual(ulong.MaxValue, declarations[7].Members[1].RawValue);
    }

    [TestMethod]
    public void EnumDeclaration_ShouldAcceptBinaryHexadecimalAndOctalIntegralLiterals()
    {
        const string query = "enum Bases : ulong { Binary = 0b1010, Octal = 0o12, Hex = 0x0A };";

        var declaration = ParseSingleEnum(query);

        CollectionAssert.AreEqual(
            new ulong[] { 10, 10, 10 },
            declaration.Members.Select(static member => member.RawValue).ToArray());
    }

    [TestMethod]
    public void LaterTableDeclaration_ShouldAcceptNullableQueryLocalEnumReference()
    {
        const string query =
            "enum State : byte { Ready = 1ub }; table Jobs { Status: State?, Previous: State };";

        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var table = (CreateTableNode)statements.Statements[1].Node;

        CollectionAssert.AreEqual(
            new[] { "State?", "State" },
            table.Columns.Select(static column => column.TypeName).ToArray());
    }

    [TestMethod]
    public void EnumAndFlags_ShouldRemainOrdinaryIdentifiersInsideExpressions()
    {
        const string query = "select enum, flags from #schema.rows() r";

        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var queryNode = (QueryNode)((SingleSetNode)statements.Statements.Single().Node).Query;

        CollectionAssert.AreEqual(
            new[] { "enum", "flags" },
            queryNode.Select.Fields
                .Select(static field => ((IdentifierNode)field.Expression).Name)
                .ToArray());
    }

    private static EnumDeclarationNode ParseSingleEnum(string query)
    {
        return ParseEnums(query).Single();
    }

    private static EnumDeclarationNode[] ParseEnums(string query)
    {
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return statements.Statements
            .Select(static statement => statement.Node)
            .OfType<EnumDeclarationNode>()
            .ToArray();
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        return new TextSpan(start, text.Length);
    }

}
