using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC100EnumDeclarationTests
{
    public static IEnumerable<object[]> DuplicateMemberCases()
    {
        yield return new object[]
        {
            "REC-100-DUP01",
            "enum AccessMode : int { Read = 1, Write = 2, Read = 3 };",
            "Read"
        };
        yield return new object[]
        {
            "REC-100-DUP02",
            "enum AccessModeCase : int { Ready = 1, ready = 2 };",
            "ready"
        };
        yield return new object[]
        {
            "REC-100-DUP03",
            "flags enum PermissionBits : uint { Read = 1ui, Write = 2ui, READ = 3ui };",
            "READ"
        };
        yield return new object[]
        {
            "REC-100-DUP04",
            "enum AliasStates : int { First = 1, Alias = 1, FIRST = 2 };",
            "FIRST"
        };
        yield return new object[]
        {
            "REC-100-DUP05",
            "enum Palette : byte { Red = 1, Blue = 2,\n Green = 3, Red = 4 };",
            "Red"
        };
        yield return new object[]
        {
            "REC-100-DUP06",
            "enum ModeSet : short { Draft = 1, Published = 2, PUBLISHED = 3, };",
            "PUBLISHED"
        };
    }

    [TestMethod]
    [DynamicData(nameof(DuplicateMemberCases))]
    public void DuplicateMemberNames_ShouldReportOnePreciseParseDiagnostic(
        string caseId,
        string query,
        string expectedMember)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2045_DuplicateEnumMember, diagnostic.Code, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);

        var expectedStart = query.LastIndexOf(expectedMember, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, expectedStart);
        Assert.AreEqual(new TextSpan(expectedStart, expectedMember.Length), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, expectedMember);
        Assert.IsTrue(diagnostic.Location.IsValid);
        Assert.IsTrue(diagnostic.EndLocation.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.AreEqual("Core Spec - Enum Declarations", diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void QueryLocalEnumTypeNames_ShouldRemainOrdinaryCaseInsensitiveReferences()
    {
        const string query =
            "enum WorkState : byte { Ready = 1ub }; table Jobs { Status: WORKSTATE?, Previous: workstate };";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = (StatementsArrayNode)result.Root!.Expression;
        var table = (CreateTableNode)statements.Statements[1].Node;
        CollectionAssert.AreEqual(
            new[] { "WORKSTATE?", "workstate" },
            table.Columns.Select(static column => column.TypeName).ToArray());
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainTwentyFourRegisteredCasesAcrossFourFamilies()
    {
        var ids = new[]
        {
            "REC-100-MEM01", "REC-100-MEM02", "REC-100-MEM03", "REC-100-MEM04", "REC-100-MEM05", "REC-100-MEM06",
            "REC-100-ID01", "REC-100-ID02", "REC-100-ID03", "REC-100-ID04", "REC-100-ID05", "REC-100-ID06",
            "REC-100-OP01", "REC-100-OP02", "REC-100-OP03", "REC-100-OP04", "REC-100-OP05", "REC-100-OP06",
            "REC-100-DUP01", "REC-100-DUP02", "REC-100-DUP03", "REC-100-DUP04", "REC-100-DUP05", "REC-100-DUP06"
        };

        Assert.HasCount(24, ids);
        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-MEM", StringComparison.Ordinal)));
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-ID", StringComparison.Ordinal)));
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-OP", StringComparison.Ordinal)));
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-DUP", StringComparison.Ordinal)));
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
