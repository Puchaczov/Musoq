using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class ReportedDataSourceDiagnosticTests
{
    [TestMethod]
    public void Report_FreezesCollectionsAndRedactsSensitiveDetails()
    {
        var fixes = new List<string> { "Supply valid credentials." };
        var details = new Dictionary<string, string> { ["token"] = "secret", ["status"] = "403" };
        var report = new DataSourceDiagnostic(
            "REMOTE.ACCESS_DENIED",
            "authentication",
            "Access was denied.",
            new DataSourceDiagnosticOptions
            {
                Severity = DataSourceDiagnosticSeverity.Error,
                SuggestedFixes = fixes,
                FailureKind = DataSourceFailureKind.AccessDenied,
                Details = details,
            }
        );
        fixes.Clear();
        details["status"] = "200";
        Assert.AreEqual("Supply valid credentials.", report.SuggestedFixes.Single());
        Assert.AreEqual("403", report.Details["status"]);
        Assert.AreNotEqual("secret", report.Details["token"]);
    }

    [TestMethod]
    public void Report_ErrorEnvelopeRetainsProviderFieldsAndExcludesPrivateException()
    {
        var report = new DataSourceDiagnostic(
            "REMOTE.RATE_LIMIT",
            "remote-limit",
            "The service rate limit was reached.",
            new DataSourceDiagnosticOptions
            {
                Severity = DataSourceDiagnosticSeverity.Error,
                SuggestedFixes = ["Wait before retrying."],
                FailureKind = DataSourceFailureKind.RateLimited,
                InputLocation = new DataSourceInputLocation(characterOffset: 7),
                ArgumentName = "root",
                ArgumentIndex = 0,
                CanRetry = true,
                RetryAfter = TimeSpan.FromSeconds(30),
            }
        );
        var exception = new DataSourceException(report, new IOException("PRIVATE SDK BODY"));
        var envelope = MusoqErrorEnvelope.FromDiagnostic(exception.ToDiagnostic());
        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);
        StringAssert.Contains(json, "REMOTE.RATE_LIMIT");
        StringAssert.Contains(json, "Wait before retrying.");
        Assert.IsFalse(json.Contains("PRIVATE SDK BODY", StringComparison.Ordinal));
        Assert.AreEqual(DiagnosticCode.MQ7020_ReportedDataSourceFailure, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.DataSource, envelope.Phase);
        Assert.AreEqual(7L, report.ToDiagnostic().InputLocation!.CharacterOffset);
        Assert.AreEqual("30000", report.ToDiagnostic().Arguments["retryAfterMilliseconds"]);
    }

    [TestMethod]
    public void Report_WarningsCannotBeThrownAndUseWarningEnvelope()
    {
        var report = new DataSourceDiagnostic(
            "CSV.SKIPPED",
            "format",
            DataSourceDiagnosticSeverity.Warning,
            "One record was skipped."
        );
        Assert.ThrowsExactly<ArgumentException>(() => new DataSourceException(report));
        Assert.AreEqual(DiagnosticCode.MQ5027_ReportedDataSourceWarning, report.ToDiagnostic().Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, report.ToDiagnostic().Severity);
    }

    [TestMethod]
    public void Report_BoundsAndEscapesPublicText()
    {
        var report = new DataSourceDiagnostic(
            "P.ERROR",
            "category",
            "prefix\t\n" + new string('x', 1000),
            new DataSourceDiagnosticOptions
            {
                Severity = DataSourceDiagnosticSeverity.Error,
                SuggestedFixes = Enumerable.Repeat("fix", 100).ToArray(),
            }
        );
        Assert.IsFalse(report.Message.Any(char.IsControl));
        Assert.IsTrue(report.Message.Length <= DiagnosticSafety.MaxDisplayLength);
        Assert.AreEqual(8, report.SuggestedFixes.Count);
        Assert.ThrowsExactly<ArgumentException>(() =>
            new DataSourceDiagnostic("bad\ncode", "category", DataSourceDiagnosticSeverity.Error, "message")
        );
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new DataSourceDiagnostic(
                "P.ERROR",
                "category",
                "message",
                new DataSourceDiagnosticOptions { Severity = DataSourceDiagnosticSeverity.Error, ArgumentIndex = -1 }
            )
        );
    }

    [TestMethod]
    [DataRow("describe")]
    [DataRow("plan")]
    [DataRow("open")]
    [DataRow("read")]
    [DataRow("cleanup")]
    public void Lifecycle_PreservesReportedErrorAndOperation(string operation)
    {
        var failure = new DataSourceException(
            new DataSourceDiagnostic(
                "API.UNAVAILABLE",
                "network",
                "The remote service is unavailable.",
                new DataSourceDiagnosticOptions
                {
                    Severity = DataSourceDiagnosticSeverity.Error,
                    SuggestedFixes = ["Retry later."],
                    FailureKind = DataSourceFailureKind.Unavailable,
                }
            )
        );
        var wrapped = WrapFailure(operation, failure);
        var diagnostic = wrapped.ToDiagnostic();
        Assert.AreEqual(DiagnosticCode.MQ7020_ReportedDataSourceFailure, diagnostic.Code);
        Assert.AreEqual("API.UNAVAILABLE", diagnostic.Arguments["providerCode"]);
        Assert.AreEqual("#api", diagnostic.Arguments["schema"]);
        Assert.AreEqual("r", diagnostic.Arguments["alias"]);
        Assert.AreEqual(operation, diagnostic.Arguments["operation"]);
        Assert.AreSame(failure, wrapped.InnerException);
    }

    private static Musoq.Schema.Exceptions.DataSourceLifecycleException WrapFailure(
        string operation,
        Exception failure
    ) =>
        operation switch
        {
            "open" => Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#api", "rows", "r", "ctx", failure),
            "read" => Musoq.Schema.Exceptions.DataSourceLifecycleException.ForRead("#api", "rows", "r", "ctx", failure),
            "cleanup" => Musoq.Schema.Exceptions.DataSourceLifecycleException.ForCleanup(
                "#api",
                "rows",
                "r",
                "ctx",
                failure
            ),
            _ => Musoq.Schema.Exceptions.DataSourceLifecycleException.ForProviderOperation(
                "#api",
                "rows",
                "r",
                "ctx",
                operation,
                failure
            ),
        };

    [TestMethod]
    public void MixedAggregate_RemainsIndependentOfProviderReport()
    {
        var failure = CollectionFailure();
        var mixed = Musoq.Schema.Exceptions.DataSourceLifecycleException.ForRead(
            "#api",
            "rows",
            "r",
            "ctx",
            new AggregateException(failure, new InvalidOperationException("unrelated defect"))
        );
        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, mixed.Code);
    }

    [TestMethod]
    public void Warnings_AreBoundedDeduplicatedAndSafeAcrossParallelReporters()
    {
        var collector = new DataSourceWarningCollector();
        var reporter = collector.ForSource(new Musoq.Schema.Optimization.SourceIdentity("#csv", "file", "ctx", "c"));
        var warning = new DataSourceDiagnostic(
            "CSV.SKIPPED",
            "format",
            DataSourceDiagnosticSeverity.Warning,
            "A record was skipped."
        );
        System.Threading.Tasks.Parallel.For(0, 1000, _ => reporter(warning));
        Assert.AreEqual("1000", collector.Snapshot().Single().Arguments["occurrenceCount"]);
        System.Threading.Tasks.Parallel.For(
            0,
            200,
            index =>
                reporter(
                    new DataSourceDiagnostic(
                        "CSV.WARNING" + index,
                        "format",
                        DataSourceDiagnosticSeverity.Warning,
                        "A different issue occurred."
                    )
                )
        );
        Assert.AreEqual(101, collector.Snapshot().Count);
        Assert.AreEqual(101L, collector.SuppressedCount);
        Assert.AreEqual(0, new DataSourceWarningCollector().Snapshot().Count);
        Assert.ThrowsExactly<ArgumentException>(() =>
            reporter(new DataSourceDiagnostic("CSV.ERROR", "format", DataSourceDiagnosticSeverity.Error, "Fatal."))
        );
    }

    [TestMethod]
    public void DisabledWarningReporting_AllocatesNoPerRowState()
    {
        var context = SourceContext();
        var warning = new DataSourceDiagnostic(
            "P.WARNING",
            "format",
            DataSourceDiagnosticSeverity.Warning,
            "A record was skipped."
        );
        ExerciseDisabledWarningReporter(context, warning);
        var before = GC.GetAllocatedBytesForCurrentThread();
        ExerciseDisabledWarningReporter(context, warning);
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void ExerciseDisabledWarningReporter(
        Musoq.Schema.Optimization.SourceExecutionContext context,
        DataSourceDiagnostic warning
    )
    {
        for (var index = 0; index < 10000; index++)
            context.ReportWarning(warning);
    }

    [TestMethod]
    public void WarningCollector_RecognizesDifferentInputPositionsAndSourceOccurrences()
    {
        var collector = new DataSourceWarningCollector();
        var first = collector.ForSource(new Musoq.Schema.Optimization.SourceIdentity("#csv", "file", "ctx0", "c0"));
        var second = collector.ForSource(new Musoq.Schema.Optimization.SourceIdentity("#csv", "file", "ctx1", "c1"));
        var start = new DataSourceDiagnostic(
            "CSV.SKIPPED",
            "format",
            "Skipped a record.",
            new DataSourceDiagnosticOptions
            {
                Severity = DataSourceDiagnosticSeverity.Warning,
                InputLocation = new DataSourceInputLocation(characterOffset: 0),
            }
        );
        var next = new DataSourceDiagnostic(
            "CSV.SKIPPED",
            "format",
            "Skipped a record.",
            new DataSourceDiagnosticOptions
            {
                Severity = DataSourceDiagnosticSeverity.Warning,
                InputLocation = new DataSourceInputLocation(characterOffset: 1),
            }
        );
        first(start);
        first(next);
        second(start);
        second(next);
        Assert.AreEqual(4, collector.Snapshot().Count);
    }

    [TestMethod]
    [DataRow(DataSourceFailureKind.InvalidInput)]
    [DataRow(DataSourceFailureKind.InvalidConfiguration)]
    [DataRow(DataSourceFailureKind.AccessDenied)]
    [DataRow(DataSourceFailureKind.ResourceNotFound)]
    [DataRow(DataSourceFailureKind.RateLimited)]
    [DataRow(DataSourceFailureKind.Unavailable)]
    [DataRow(DataSourceFailureKind.Timeout)]
    [DataRow(DataSourceFailureKind.ProviderFailure)]
    public void FailureKinds_PreserveProviderCodesAndGuidance(DataSourceFailureKind kind)
    {
        var report = new DataSourceDiagnostic(
            "CUSTOM.SPECIFIC",
            "provider-owned-category",
            "A recognized provider problem occurred.",
            new DataSourceDiagnosticOptions
            {
                Severity = DataSourceDiagnosticSeverity.Error,
                SuggestedFixes = ["Correct the datasource configuration."],
                FailureKind = kind,
            }
        );
        var envelope = MusoqErrorEnvelope.FromDiagnostic(report.ToDiagnostic());
        Assert.AreEqual(kind.ToString(), envelope.Arguments["failureKind"]);
        Assert.AreEqual("CUSTOM.SPECIFIC", envelope.Arguments["providerCode"]);
        Assert.AreEqual("provider-owned-category", envelope.Arguments["providerCategory"]);
    }

    [TestMethod]
    public void QueryScopedConstruction_PreservesReportedFailure()
    {
        var context = SourceContext();
        var request = new Musoq.Schema.QueryScopedRowSourceRequest(context, new Musoq.Schema.QueryRowShape([]));
        var exception = Assert.ThrowsExactly<Musoq.Schema.Exceptions.DataSourceLifecycleException>(() =>
            Musoq.Schema.DataSources.DataSourceLifecycle.OpenQueryScopedRowSource<
                int,
                UnusedMaterializer,
                Musoq.Schema.EmptySourceArguments
            >(new FailingQueryScopedSchema(), "rows", request, default, "#custom", "r", "ctx")
        );
        Assert.AreEqual(DiagnosticCode.MQ7020_ReportedDataSourceFailure, exception.Code);
        Assert.AreEqual("CUSTOM.READ", exception.ToDiagnostic().Arguments["providerCode"]);
    }

    [TestMethod]
    public void BackgroundProducer_ReportsWarningAndPreservesCollectionFailure()
    {
        var warnings = new DataSourceWarningCollector();
        var context = SourceContext(
            warnings.ForSource(new Musoq.Schema.Optimization.SourceIdentity("#custom", "rows", "ctx", "r"))
        );
        var exception = Assert.ThrowsExactly<Musoq.Schema.Exceptions.DataSourceLifecycleException>(() =>
            Musoq
                .Schema.DataSources.DataSourceLifecycle.Read(
                    new FailingProducer(context),
                    "#custom",
                    "rows",
                    "r",
                    "ctx"
                )
                .ToArray()
        );
        Assert.AreEqual(DiagnosticCode.MQ7020_ReportedDataSourceFailure, exception.Code);
        Assert.AreEqual(1, warnings.Snapshot().Count);
    }

    private static Musoq.Schema.Optimization.SourceExecutionContext SourceContext(
        Action<DataSourceDiagnostic>? reporter = null
    ) =>
        new(
            "query",
            Musoq.Schema.Optimization.SourceExecutionPlan.Empty(Musoq.Schema.Optimization.SourceIdentity.Empty),
            System.Threading.CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance
        )
        {
            WarningReporter = reporter,
        };

    private static DataSourceException CollectionFailure() =>
        new(
            new DataSourceDiagnostic(
                "CUSTOM.READ",
                "remote",
                "The service stopped producing rows.",
                new DataSourceDiagnosticOptions
                {
                    Severity = DataSourceDiagnosticSeverity.Error,
                    SuggestedFixes = ["Retry later."],
                    FailureKind = DataSourceFailureKind.Unavailable,
                }
            )
        );

    private sealed class FailingQueryScopedSchema : Musoq.Schema.IQueryScopedRowSourceSchema
    {
        public Musoq.Schema.DataSources.RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
            string name,
            Musoq.Schema.QueryScopedRowSourceRequest request,
            TArguments arguments
        )
            where TMaterializer : struct, Musoq.Schema.IQueryRowMaterializer<TRow>
            where TArguments : struct, Musoq.Schema.ISourceArguments => throw CollectionFailure();
    }

    private readonly struct UnusedMaterializer : Musoq.Schema.IQueryRowMaterializer<int>
    {
        public static int Materialize<TReader>(scoped ref TReader reader)
            where TReader : Musoq.Schema.IQuerySourceFieldReader, allows ref struct =>
            throw new AssertFailedException("Source construction should fail first.");
    }

    private sealed class FailingProducer : Musoq.Schema.DataSources.DiagnosticChunkedRowSource<int>
    {
        private readonly Musoq.Schema.Optimization.SourceExecutionContext _context;

        internal FailingProducer(Musoq.Schema.Optimization.SourceExecutionContext context)
            : base(context, "rows") => _context = context;

        protected override void CollectChunks(Musoq.Schema.DataSources.DiagnosticChunkWriter<int> writer)
        {
            _context.ReportWarning(
                new DataSourceDiagnostic(
                    "CUSTOM.WARNING",
                    "remote",
                    DataSourceDiagnosticSeverity.Warning,
                    "The service is unstable."
                )
            );
            writer.Write([1]);
            throw CollectionFailure();
        }
    }

    [TestMethod]
    public void PublicReport_RedactsKnownSecretsAndPathsFromMessagesFixesAndDetails()
    {
        var report = new DataSourceDiagnostic(
            "P.ERROR",
            "access",
            "Could not open PRIVATE-PATH with PRIVATE-TOKEN.",
            new DataSourceDiagnosticOptions
            {
                Details = new Dictionary<string, string>
                {
                    ["filePath"] = "PRIVATE-PATH",
                    ["apiToken"] = "PRIVATE-TOKEN",
                },
                SuggestedFixes = ["Correct PRIVATE-PATH and PRIVATE-TOKEN."],
            }
        );
        var json = MusoqErrorEnvelopeFormatter.FormatJson(MusoqErrorEnvelope.FromDiagnostic(report.ToDiagnostic()));
        Assert.DoesNotContain("PRIVATE-PATH", json);
        Assert.DoesNotContain("PRIVATE-TOKEN", json);
        Assert.DoesNotContain("PRIVATE-PATH", report.Message);
        Assert.DoesNotContain("PRIVATE-TOKEN", report.SuggestedFixes.Single());
    }

    [TestMethod]
    public void SourceReopening_ReusesBoundReporterWithoutAllocating()
    {
        var collector = new DataSourceWarningCollector();
        var identity = new Musoq.Schema.Optimization.SourceIdentity("#csv", "file", "ctx", "c");
        ExerciseReporterBinding(collector, identity);
        var before = GC.GetAllocatedBytesForCurrentThread();
        ExerciseReporterBinding(collector, identity);
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void ExerciseReporterBinding(
        DataSourceWarningCollector collector,
        Musoq.Schema.Optimization.SourceIdentity identity
    )
    {
        for (var index = 0; index < 10000; index++)
            GC.KeepAlive(collector.ForSource(identity));
    }

    [TestMethod]
    public void MetadataAndPlanCollections_SnapshotProviderOwnedLists()
    {
        var reports = new List<DataSourceDiagnostic>
        {
            new("P.WARNING", "format", DataSourceDiagnosticSeverity.Warning, "A record was skipped."),
        };
        var descriptor = new Musoq.Schema.Optimization.SourceDescriptor
        {
            Identity = Musoq.Schema.Optimization.SourceIdentity.Empty,
            ReportedDiagnostics = reports,
        };
        var plan = Musoq.Schema.Optimization.SourcePlanResult.RejectAll(
            new Musoq.Schema.Optimization.SourcePlanRequest
            {
                Identity = Musoq.Schema.Optimization.SourceIdentity.Empty,
            }
        ) with
        {
            ReportedDiagnostics = reports,
        };
        reports.Clear();
        Assert.AreEqual("P.WARNING", descriptor.ReportedDiagnostics.Single().Code);
        Assert.AreEqual("P.WARNING", plan.ReportedDiagnostics.Single().Code);
    }
}
