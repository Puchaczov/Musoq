using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore006StatementParameterTests
{
    [TestMethod]
    [DataRow(
        "select 1 from #test.rows(); select 2 from #test.rows(); select 3 from #test.rows()",
        DisplayName = "without final terminator")]
    [DataRow(
        "select 1 from #test.rows(); select 2 from #test.rows(); select 3 from #test.rows();",
        DisplayName = "with final terminator")]
    public void SemicolonDelimitedBatch_ShouldPreserveStatementOrder(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        Assert.HasCount(3, statements.Statements);

        for (var index = 0; index < statements.Statements.Length; index++)
        {
            var statement = Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[index].Node);
            var expression = statement.Query.Select.Fields.Single().Expression;
            var value = Assert.IsInstanceOfType<IntegerNode>(expression);
            Assert.AreEqual(index + 1, value.ObjValue);
        }
    }

    [TestMethod]
    public void LeadingParameterBlock_ShouldAllowDeclarationToQueryBoundaryWithoutTerminator()
    {
        const string query =
            "params(limit: int = 7) select $limit from #test.rows(); select 2 from #test.rows()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        Assert.HasCount(3, statements.Statements);
        var parameterBlock = Assert.IsInstanceOfType<ParameterBlockNode>(statements.Statements[0].Node);
        Assert.HasCount(1, parameterBlock.Parameters);
        Assert.AreEqual("limit", parameterBlock.Parameters[0].Name);
        Assert.AreEqual("int", parameterBlock.Parameters[0].DeclaredTypeName);
        Assert.IsInstanceOfType<IntegerNode>(parameterBlock.Parameters[0].DefaultValue);
        Assert.IsInstanceOfType<ParameterReferenceNode>(
            Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[1].Node)
                .Query.Select.Fields.Single().Expression);
        Assert.AreEqual(
            2,
            Assert.IsInstanceOfType<IntegerNode>(
                    Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[2].Node)
                        .Query.Select.Fields.Single().Expression)
                .ObjValue);
    }

    [TestMethod]
    public void ExecutableStatementsWithoutSeparator_ShouldReportExactParseDiagnostic()
    {
        const string query = "select 1 from #test.rows() select 2 from #test.rows()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var secondSelect = query.LastIndexOf("select", StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(secondSelect, "select".Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void ParameterDeclarations_ShouldAcceptSupportedScalarNullableAndCollectionForms()
    {
        const string query = """
            param(
                boolValue: bool,
                byteValue: byte,
                sbyteValue: sbyte,
                shortValue: short,
                ushortValue: ushort,
                intValue: int,
                uintValue: uint,
                longValue: long,
                ulongValue: ulong,
                floatValue: float,
                doubleValue: double,
                decimalValue: decimal,
                charValue: char,
                stringValue: string,
                dateTimeValue: datetime,
                offsetValue: datetimeoffset,
                durationValue: timespan,
                guidValue: guid,
                ids: string[],
                nullableCount: int?
            )
            select $boolValue from #test.rows()
            """;

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        var parameterBlock = Assert.IsInstanceOfType<ParameterBlockNode>(statements.Statements[0].Node);
        CollectionAssert.AreEqual(
            new[]
            {
                "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
                "float", "double", "decimal", "char", "string", "datetime", "datetimeoffset",
                "timespan", "guid", "string[]", "int?"
            },
            parameterBlock.Parameters.Select(parameter => parameter.DeclaredTypeName).ToArray());
    }

    [TestMethod]
    public void ParameterDeclarations_ShouldAcceptPrimitiveAndNullDefaults()
    {
        const string query = """
            param(
                enabled: bool = true,
                marker: char = 'x',
                limit: int = 0x10,
                maybeLimit: int? = null,
                created: datetime = '2024-01-02T03:04:05Z'
            )
            select $enabled from #test.rows()
            """;

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var parameterBlock = Assert.IsInstanceOfType<ParameterBlockNode>(
            Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression).Statements[0].Node);
        Assert.IsTrue(parameterBlock.Parameters.All(parameter => parameter.HasDefaultValue));
        Assert.IsInstanceOfType<BooleanNode>(parameterBlock.Parameters[0].DefaultValue);
        Assert.IsInstanceOfType<NullNode>(parameterBlock.Parameters[3].DefaultValue);
    }

    [TestMethod]
    [DataRow(
        "param(string author) select 1 from #test.rows()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        "string author",
        DisplayName = "C# declaration order")]
    [DataRow(
        "param(author string) select 1 from #test.rows()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        "author string",
        DisplayName = "missing colon")]
    [DataRow(
        "param([string]$author) select 1 from #test.rows()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        "[string]$author",
        DisplayName = "PowerShell declaration")]
    [DataRow(
        "def query(author: string = 'x') select 1 from #test.rows()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        "def",
        DisplayName = "Python declaration")]
    [DataRow(
        "declare author string; select 1 from #test.rows()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        "declare",
        DisplayName = "SQL declaration")]
    [DataRow(
        "param(, author: string) select 1 from #test.rows()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        ",",
        DisplayName = "leading comma")]
    public void MalformedParameterDeclarations_ShouldReportExactStructuredParseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string offendingText)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var start = query.IndexOf(offendingText, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        Assert.AreEqual(new TextSpan(start, offendingText.Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void RepeatedCommaInParameterList_ShouldReportOneMalformedDeclaration()
    {
        const string query = "param(author: string,, limit: int) select 1 from #test.rows()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration, result.Diagnostics[0].Code);
        var secondComma = query.IndexOf(",,", StringComparison.Ordinal) + 1;
        Assert.AreEqual(new TextSpan(secondComma, 1), result.Diagnostics[0].Span);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
