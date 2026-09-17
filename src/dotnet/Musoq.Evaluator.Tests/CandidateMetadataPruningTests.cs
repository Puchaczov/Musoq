using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.CandidatePayload;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CandidateMetadataPruningTests : BasicEntityTestBase
{
    [TestMethod]
    public void CandidateMetadata_ShouldPreserveResultsAndOpenExactlyMatchingPayloads()
    {
        const string query = "select c.Id, c.Path, c.Payload from #candidate.items() c " +
                             "where c.Path like '/root/alpha%' order by c.Id";

        var baseline = Run(query, CandidatePayloadMode.RejectAll, out var baselineRecorder);
        var optimized = Run(query, CandidatePayloadMode.CandidateMetadata, out var optimizedRecorder);

        AssertEquivalent(baseline, optimized);
        CollectionAssert.AreEqual(new[] { 0, 5 }, optimized.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(DefaultRows.Count, baselineRecorder.PayloadOpens);
        Assert.AreEqual(2, optimizedRecorder.PayloadOpens);
        Assert.AreEqual(2, optimizedRecorder.PayloadDecodes);
        Assert.AreEqual(2, optimizedRecorder.RowMaterializations);
        Assert.AreEqual(1, optimizedRecorder.EnumeratorDisposals);
    }

    [TestMethod]
    public void CandidateMetadata_WhenNoRowsMatch_ShouldOpenNoPayloads()
    {
        var table = Run(
            "select c.Id from #candidate.items() c where c.Path like '/missing/%'",
            CandidatePayloadMode.CandidateMetadata,
            out var recorder);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(0, recorder.PayloadOpens);
        Assert.AreEqual(0, recorder.PayloadDecodes);
        Assert.AreEqual(0, recorder.RowMaterializations);
        Assert.AreEqual(1, recorder.EnumeratorDisposals);
    }

    [TestMethod]
    public void CandidateMetadata_ShouldUseLegacyUnicodeInputSemantics()
    {
        const string query = "select c.Id, c.Path from #candidate.items() c " +
                             "where c.Path like '/root/k%' order by c.Id";

        var baseline = Run(query, CandidatePayloadMode.RejectAll, out _);
        var optimized = Run(query, CandidatePayloadMode.CandidateMetadata, out var recorder);

        AssertEquivalent(baseline, optimized);
        CollectionAssert.AreEqual(new[] { 2 }, optimized.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(1, recorder.PayloadOpens);
    }

    [TestMethod]
    [DataRow("where c.Path like 'K'", 0)]
    [DataRow("where c.Path like 'I'", 1)]
    [DataRow("where c.Path like 'K%'", 0)]
    [DataRow("where c.Path like '%K'", 0)]
    [DataRow("where c.Path like '%K%'", 0)]
    public void CandidateMetadata_AllSpecializedKindsShouldPreserveLegacyUnicodeInputSemantics(
        string predicate,
        int expectedId)
    {
        var rows = new[]
        {
            CandidatePayloadSeed.Create(0, "K", "kelvin"),
            CandidatePayloadSeed.Create(1, "İ", "dotted-i"),
            CandidatePayloadSeed.Create(2, "ASCII", "ascii"),
            CandidatePayloadSeed.Create(3, null, "null")
        };
        var query = $"select c.Id from #candidate.items() c {predicate} order by c.Id";

        var baseline = Run(query, rows, CandidatePayloadMode.RejectAll, out _);
        var optimized = Run(query, rows, CandidatePayloadMode.CandidateMetadata, out var recorder);

        AssertEquivalent(baseline, optimized);
        CollectionAssert.AreEqual(new[] { expectedId }, optimized.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(1, recorder.PayloadOpens);
    }

    [TestMethod]
    [DoNotParallelize]
    public void CandidateMetadata_UnderTurkishCulture_ShouldPreserveAsciiIRegexSemantics()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var rows = new[]
        {
            CandidatePayloadSeed.Create(0, "i", "dotted-lower"),
            CandidatePayloadSeed.Create(1, "ı", "dotless-lower"),
            CandidatePayloadSeed.Create(2, "I", "ascii-upper")
        };

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            const string query = "select c.Id from #candidate.items() c where c.Path like 'I' order by c.Id";
            var baseline = Run(query, rows, CandidatePayloadMode.RejectAll, out _);
            var optimized = Run(query, rows, CandidatePayloadMode.CandidateMetadata, out var recorder);

            AssertEquivalent(baseline, optimized);
            CollectionAssert.AreEqual(new[] { 1, 2 }, optimized.Select(row => (int)row[0]).ToArray());
            Assert.AreEqual(2, recorder.PayloadOpens);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void CandidateMetadata_NegationShouldPreserveNullSemantics()
    {
        const string query = "select c.Id from #candidate.items() c " +
                             "where c.Path not like '/root/alpha%' order by c.Id";

        var baseline = Run(query, CandidatePayloadMode.RejectAll, out _);
        var optimized = Run(query, CandidatePayloadMode.CandidateMetadata, out var recorder);

        AssertEquivalent(baseline, optimized);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 6 }, optimized.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(5, recorder.PayloadOpens);
    }

    [TestMethod]
    public void RowFiltering_ShouldRunOnlyAfterEveryPayloadIsMaterialized()
    {
        var table = Run(
            "select c.Id from #candidate.items() c where c.Path like '/root/alpha%' order by c.Id",
            CandidatePayloadMode.RowFiltering,
            out var recorder);

        CollectionAssert.AreEqual(new[] { 0, 5 }, table.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(DefaultRows.Count, recorder.PayloadOpens);
        Assert.AreEqual(DefaultRows.Count, recorder.PayloadDecodes);
        Assert.AreEqual(DefaultRows.Count, recorder.RowMaterializations);
    }

    [TestMethod]
    public void PartialAnd_ShouldPruneByMetadataAndRetainPayloadResidual()
    {
        const string query = "select c.Id from #candidate.items() c " +
                             "where c.Path like '/root/%' and c.Payload like '%keep%' order by c.Id";

        var baseline = Run(query, CandidatePayloadMode.RejectAll, out _);
        var optimized = Run(query, CandidatePayloadMode.CandidateMetadata, out var recorder);

        AssertEquivalent(baseline, optimized);
        CollectionAssert.AreEqual(new[] { 0, 2, 5, 6 }, optimized.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(5, recorder.PayloadOpens);
    }

    [TestMethod]
    public void SupportedLookingOr_ShouldRemainResidualAndOpenEveryPayload()
    {
        const string query = "select c.Id from #candidate.items() c " +
                             "where c.Path like '/root/alpha%' or c.Payload like '%other%' order by c.Id";

        var baseline = Run(query, CandidatePayloadMode.RejectAll, out _);
        var optimized = Run(query, CandidatePayloadMode.CandidateMetadata, out var recorder);

        AssertEquivalent(baseline, optimized);
        CollectionAssert.AreEqual(new[] { 0, 4, 5 }, optimized.Select(row => (int)row[0]).ToArray());
        Assert.AreEqual(DefaultRows.Count, recorder.PayloadOpens);
        Assert.IsEmpty(recorder.ExecutionPlans.Single().PredicateApplications);
    }

    [TestMethod]
    [DataRow(CandidatePayloadMode.UnknownVersion, "'/root/alpha%'")]
    [DataRow(CandidatePayloadMode.PayloadOnlyCapability, "'/root/alpha%'")]
    [DataRow(CandidatePayloadMode.CandidateMetadata, "'/root/_lpha%'")]
    public void UnsupportedCapabilityShapeOrVersion_ShouldKeepRuntimeResidual(
        CandidatePayloadMode mode,
        string pattern)
    {
        var query = $"select c.Id from #candidate.items() c where c.Path like {pattern} order by c.Id";
        var baseline = Run(query, CandidatePayloadMode.RejectAll, out _);
        var actual = Run(query, mode, out var recorder);

        AssertEquivalent(baseline, actual);
        Assert.AreEqual(DefaultRows.Count, recorder.PayloadOpens);
        Assert.IsEmpty(recorder.ExecutionPlans.Single().PredicateApplications);
    }

    [TestMethod]
    [DataRow(CandidatePayloadMode.MalformedMissingApplication)]
    [DataRow(CandidatePayloadMode.MalformedDuplicateApplication)]
    [DataRow(CandidatePayloadMode.MalformedAlteredApplication)]
    [DataRow(CandidatePayloadMode.MalformedUnadvertisedPhase)]
    [DataRow(CandidatePayloadMode.MalformedUnknownVersionApplication)]
    public void InvalidApplications_ShouldFailBeforeAnyPayloadOperation(CandidatePayloadMode mode)
    {
        var provider = new CandidatePayloadSchemaProvider(DefaultRows, mode);

        _ = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select c.Id from #candidate.items() c where c.Path like '/root/%'",
                schemaProvider: provider));

        Assert.AreEqual(0, provider.Recorder.PayloadOpens);
        Assert.AreEqual(0, provider.Recorder.PayloadDecodes);
        Assert.AreEqual(0, provider.Recorder.RowMaterializations);
        Assert.AreEqual(0, provider.Recorder.EnumeratorDisposals);
    }

    [TestMethod]
    [DataRow(CandidatePayloadFault.Open, 1, 0, 0)]
    [DataRow(CandidatePayloadFault.Decode, 1, 1, 0)]
    public void MatchingPayloadFailure_ShouldPreserveContextAndDispose(
        CandidatePayloadFault fault,
        int expectedOpens,
        int expectedDecodes,
        int expectedMaterializations)
    {
        var rows = new[]
        {
            CandidatePayloadSeed.Create(10, "/ignored/value", "ignored"),
            CandidatePayloadSeed.Create(11, "/fault/value", "secret-payload", fault)
        };
        var provider = new CandidatePayloadSchemaProvider(rows, CandidatePayloadMode.CandidateMetadata);
        using var compiled = CreateAndRunVirtualMachine(
            "select c.Id from #candidate.items() c where c.Path like '/fault/%'",
            schemaProvider: provider);

        var exception = Assert.Throws<QueryExecutionException>(() =>
            TableMaterializationTestHelper.Materialize(compiled.Run()));

        Assert.IsNotNull(exception.Envelope);
        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, exception.Envelope.Code);
        Assert.AreEqual("#candidate", exception.Envelope.Arguments["schema"]);
        Assert.AreEqual("items", exception.Envelope.Arguments["source"]);
        Assert.AreEqual("c", exception.Envelope.Arguments["alias"]);
        Assert.AreEqual(expectedOpens, provider.Recorder.PayloadOpens);
        Assert.AreEqual(expectedDecodes, provider.Recorder.PayloadDecodes);
        Assert.AreEqual(expectedMaterializations, provider.Recorder.RowMaterializations);
        Assert.AreEqual(1, provider.Recorder.EnumeratorDisposals);
        Assert.DoesNotContain("secret-payload", exception.Message);
    }

    [TestMethod]
    public void CancellationDuringCandidateEnumeration_ShouldOpenNoLaterPayloadAndDispose()
    {
        using var cancellation = new CancellationTokenSource();
        var recorder = new CandidatePayloadRecorder
        {
            CandidateInspected = id =>
            {
                if (id == 1)
                    cancellation.Cancel();
            }
        };
        var provider = new CandidatePayloadSchemaProvider(
            DefaultRows,
            CandidatePayloadMode.CandidateMetadata,
            recorder);
        using var compiled = CreateAndRunVirtualMachine(
            "select c.Id from #candidate.items() c where c.Path like '/root/%'",
            schemaProvider: provider);

        _ = Assert.Throws<OperationCanceledException>(() =>
            TableMaterializationTestHelper.Materialize(compiled.Run(cancellation.Token)));

        Assert.AreEqual(1, recorder.PayloadOpens);
        Assert.AreEqual(1, recorder.PayloadDecodes);
        Assert.AreEqual(1, recorder.RowMaterializations);
        Assert.AreEqual(1, recorder.EnumeratorDisposals);
    }

    [TestMethod]
    public void EarlyConsumerTermination_ShouldDisposeCandidateEnumerator()
    {
        var provider = new CandidatePayloadSchemaProvider(DefaultRows, CandidatePayloadMode.CandidateMetadata);
        using var compiled = CreateAndRunVirtualMachine(
            "select c.Id from #candidate.items() c where c.Path like '/root/%' take 1",
            schemaProvider: provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, provider.Recorder.EnumeratorDisposals);
    }

    private static readonly IReadOnlyList<CandidatePayloadSeed> DefaultRows =
    [
        CandidatePayloadSeed.Create(0, "/root/alpha.txt", "keep-alpha"),
        CandidatePayloadSeed.Create(1, "/root/beta.txt", "drop-beta"),
        CandidatePayloadSeed.Create(2, "/root/Kappa.txt", "keep-kelvin"),
        CandidatePayloadSeed.Create(3, null, "keep-null"),
        CandidatePayloadSeed.Create(4, "/other/alpha.txt", "keep-other"),
        CandidatePayloadSeed.Create(5, "/root/ALPHA.log", "keep-upper"),
        CandidatePayloadSeed.Create(6, "/root/gamma.txt", "keep-gamma")
    ];

    private Table Run(
        string query,
        CandidatePayloadMode mode,
        out CandidatePayloadRecorder recorder)
    {
        return Run(query, DefaultRows, mode, out recorder);
    }

    private Table Run(
        string query,
        IReadOnlyList<CandidatePayloadSeed> rows,
        CandidatePayloadMode mode,
        out CandidatePayloadRecorder recorder)
    {
        var provider = new CandidatePayloadSchemaProvider(rows, mode);
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        var table = TableMaterializationTestHelper.Materialize(compiled.Run());
        recorder = provider.Recorder;
        return table;
    }

    private static void AssertEquivalent(Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        CollectionAssert.AreEqual(
            expected.Select(row => row.Values).SelectMany(static values => values).ToArray(),
            actual.Select(row => row.Values).SelectMany(static values => values).ToArray());
        Assert.AreEqual(ComputeHash(expected), ComputeHash(actual));
    }

    private static string ComputeHash(Table table)
    {
        var text = new StringBuilder();
        foreach (var row in table)
        {
            foreach (var value in row.Values)
            {
                var formatted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
                text.Append(formatted.Length).Append(':').Append(formatted).Append('|');
            }

            text.AppendLine();
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
