using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DiagnosticRework083StarModifierContractTests : BasicEntityTestBase
{
    [TestMethod]
    public void ModifierChain_ShouldApplyLikeExcludeReplaceAndRenameInOrder()
    {
        const string query =
            "select * like '%O%' exclude (COUNTRY) replace (Population * 3 as pOpUlAtIoN) " +
            "rename (POPULATION as Population3x) from #A.Entities()";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(new BasicEntity
            {
                Country = "Norway",
                Population = 10m,
                Money = 2m,
                Month = "october"
            })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Population3x", typeof(decimal)),
            ("Money", typeof(decimal)),
            ("Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [30m, 2m, "october"]);
    }

    [TestMethod]
    public void QualifiedModifier_ShouldOnlyExpandAndTransformRequestedSource()
    {
        const string query =
            "select a.* exclude (city) from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("left") { Id = 7, City = "Warsaw", Country = "PL", Population = 10m }],
            ["#B"] = [new BasicEntity("right") { Id = 7, City = "Krakow", Country = "PL", Population = 20m }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[]
            {
                "a.Name", "a.Country", "a.Population", "a.Money", "a.Month", "a.Time", "a.Id",
                "a.NullableValue"
            },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", "PL", 10m, 0m, string.Empty, default(DateTime), 7, null]);
    }

    [TestMethod]
    public void UnqualifiedModifier_ShouldExpandEveryJoinedSourceInSourceOrder()
    {
        const string query =
            "select * like 'Na%' from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("left") { Id = 7 }],
            ["#B"] = [new BasicEntity("right") { Id = 7 }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[] { "a.Name", "b.Name" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["left", "right"]);
    }

    [TestMethod]
    [DataRow(
        "select * exclude (Missing) from #A.Entities()",
        DiagnosticCode.MQ3041_StarExcludeColumnNotFound,
        DiagnosticPhase.Bind,
        "EXCLUDE references non-existent column 'Missing'.",
        "*")]
    [DataRow(
        "select * replace (1 as Missing) from #A.Entities()",
        DiagnosticCode.MQ3042_StarReplaceColumnNotFound,
        DiagnosticPhase.Bind,
        "REPLACE targets column 'Missing' which does not exist in the table.",
        "*")]
    [DataRow(
        "select * exclude (Name, City, Country, Population, Money, Month, Time, Id, NullableValue) from #A.Entities()",
        DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns,
        DiagnosticPhase.Bind,
        "EXCLUDE would remove all columns from the star expansion.",
        "*")]
    [DataRow(
        "select * exclude (City) replace (1 as city) from #A.Entities()",
        DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace,
        DiagnosticPhase.Bind,
        "Column 'city' appears in both EXCLUDE and REPLACE.",
        "*")]
    [DataRow(
        "select * like 'Z%' from #A.Entities()",
        DiagnosticCode.MQ3045_StarLikeMatchedNoColumns,
        DiagnosticPhase.Bind,
        "Star modifier LIKE 'Z%' matched no columns.",
        "*")]
    [DataRow(
        "select * exclude (Name, name) from #A.Entities()",
        DiagnosticCode.MQ3046_StarExcludeDuplicateColumn,
        DiagnosticPhase.Bind,
        "Duplicate column 'name' in EXCLUDE list.",
        "*")]
    [DataRow(
        "select * replace (1 as Name, 2 as name) from #A.Entities()",
        DiagnosticCode.MQ3047_StarReplaceDuplicateColumn,
        DiagnosticPhase.Bind,
        "Duplicate column 'name' in REPLACE list.",
        "*")]
    [DataRow(
        "select * like 'N%' replace (1 as City) from #A.Entities()",
        DiagnosticCode.MQ3048_StarReplaceTargetsRemovedColumn,
        DiagnosticPhase.Bind,
        "REPLACE targets column 'City' which was removed by LIKE filter or EXCLUDE.",
        "*")]
    [DataRow(
        "select * rename (Name as Label, name as OtherName) from #A.Entities()",
        DiagnosticCode.MQ3068_StarRenameDuplicateSource,
        DiagnosticPhase.Bind,
        "Duplicate source column 'name' in RENAME list.",
        "*")]
    [DataRow(
        "select * rename (Name as Label, City as Label) from #A.Entities()",
        DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
        DiagnosticPhase.Bind,
        "RENAME would produce duplicate output column 'Label'.",
        "*")]
    [DataRow(
        "select * rename (Name as City) from #A.Entities()",
        DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
        DiagnosticPhase.Bind,
        "RENAME would produce duplicate output column 'City'.",
        "*")]
    [DataRow(
        "select * rename (Missing as Label) from #A.Entities()",
        DiagnosticCode.MQ3070_StarRenameColumnNotFound,
        DiagnosticPhase.Bind,
        "RENAME references non-existent output column 'Missing'.",
        "*")]
    public void InvalidModifierForms_ShouldExposeExactBindEnvelopes(
        string query,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase,
        string expectedMessage,
        string expectedSpanText)
    {
        var result = CompileWithDiagnostics(query);
        AssertExactEnvelope(result, query, expectedCode, expectedPhase, expectedMessage, expectedSpanText);
    }

    [TestMethod]
    public void OutOfOrderModifier_ShouldExposeExactParseEnvelope()
    {
        const string query = "select * replace (1 as Name) like 'N%' from #A.Entities()";
        var result = CompileWithDiagnostics(query);

        AssertExactEnvelope(
            result,
            query,
            DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            DiagnosticPhase.Parse,
            "Duplicate or out-of-order star modifier. Expected order: LIKE/NOT LIKE, EXCLUDE, REPLACE, RENAME.",
            "like");
    }

    private BuildResult CompileWithDiagnostics(string query)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(CreateSingleSource()),
            LoggerResolver);
    }

    private static void AssertExactEnvelope(
        BuildResult result,
        string query,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase,
        string expectedMessage,
        string expectedSpanText)
    {
        var envelopes = result.ToEnvelopes().ToArray();
        Assert.HasCount(1, envelopes, string.Join(Environment.NewLine, envelopes.Select(static envelope => envelope.Message)));

        var envelope = envelopes.Single();
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(expectedPhase, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedMessage, envelope.Message);

        var offset = query.IndexOf(expectedSpanText, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, offset);
        Assert.AreEqual(offset, envelope.Offset);
        Assert.AreEqual(expectedSpanText.Length, envelope.Length);
        Assert.AreEqual(offset + expectedSpanText.Length, envelope.EndOffset);
        Assert.AreEqual(1, envelope.Line);
        Assert.AreEqual(offset + 1, envelope.Column);
        Assert.AreEqual(1, envelope.EndLine);
        Assert.AreEqual(offset + expectedSpanText.Length + 1, envelope.EndColumn);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
        Assert.AreEqual(envelope.SuggestedFixes.Count, envelope.Actions.Count);
        Assert.AreEqual("Core Spec - Star Modifiers", envelope.DocsReference);
    }
}
