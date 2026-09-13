using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-134 matrix. Each case injects a unique synthetic sentinel and
/// checks every public-safe diagnostic surface exercised by that family.
/// Trusted verbose output is checked separately and is never treated as a
/// redacted sink.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DiagnosticRec134SecretSafeDiagnosticsTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow("PR-01")]
    [DataRow("PR-02")]
    [DataRow("PR-03")]
    [DataRow("PR-04")]
    [DataRow("PR-05")]
    [DataRow("PR-06")]
    [DataRow("PR-07")]
    [DataRow("PR-08")]
    [DataRow("PR-09")]
    [DataRow("PR-10")]
    [DataRow("PR-11")]
    [DataRow("PR-12")]
    [DataRow("SET-01")]
    [DataRow("SET-02")]
    [DataRow("SET-03")]
    [DataRow("SET-04")]
    [DataRow("SET-05")]
    [DataRow("SET-06")]
    [DataRow("SET-07")]
    [DataRow("SET-08")]
    [DataRow("SET-09")]
    [DataRow("SET-10")]
    [DataRow("SET-11")]
    [DataRow("SET-12")]
    [DataRow("SN-01")]
    [DataRow("SN-02")]
    [DataRow("SN-03")]
    [DataRow("SN-04")]
    [DataRow("SN-05")]
    [DataRow("SN-06")]
    [DataRow("SN-07")]
    [DataRow("SN-08")]
    [DataRow("SN-09")]
    [DataRow("SN-10")]
    [DataRow("SN-11")]
    [DataRow("SN-12")]
    [DataRow("LG-01")]
    [DataRow("LG-02")]
    [DataRow("LG-03")]
    [DataRow("LG-04")]
    [DataRow("LG-05")]
    [DataRow("LG-06")]
    [DataRow("LG-07")]
    [DataRow("LG-08")]
    [DataRow("LG-09")]
    [DataRow("LG-10")]
    [DataRow("LG-11")]
    [DataRow("LG-12")]
    public void SecretSafeDiagnosticsMatrix_ShouldProtectPublicSinks(string caseId)
    {
        var sentinel = CreateSentinel(caseId);

        if (caseId.StartsWith("PR-", StringComparison.Ordinal))
        {
            AssertProviderParameterAndSubstrateBoundaries(caseId, sentinel);
            return;
        }

        if (caseId.StartsWith("SET-", StringComparison.Ordinal))
        {
            AssertRuntimeSettingsDoNotAppearInDescriptions(caseId, sentinel);
            return;
        }

        if (caseId.StartsWith("SN-", StringComparison.Ordinal))
        {
            AssertSpanSelectedLiteralDoesNotReachDiagnosticSinks(caseId, sentinel);
            return;
        }

        if (caseId.StartsWith("LG-", StringComparison.Ordinal))
        {
            AssertEngineLogsDoNotCaptureSubstrateValues(caseId, sentinel);
            return;
        }

        Assert.Fail($"Unknown REC-134 case '{caseId}'.");
    }

    private static void AssertProviderParameterAndSubstrateBoundaries(string caseId, string sentinel)
    {
        AssertStructuredPayloadBoundary(caseId, sentinel);

        var suffix = int.Parse(caseId[3..], System.Globalization.CultureInfo.InvariantCulture);
        QueryExecutionException exception;
        switch (suffix)
        {
            case <= 4:
            {
                var binding = ScriptParameterBindingException.TypeMismatch(
                    "apiToken",
                    typeof(int),
                    sentinel,
                    new InvalidOperationException($"parameter substrate {sentinel}"));
                exception = QueryExecutionException.ForScriptParameterBinding(binding);
                break;
            }
            case <= 8:
            {
                var provider = new SecretFailureProvider(sentinel);
                using var compiled = InstanceCreator.CompileForExecution(
                    $"select r.Value from #rec134.items() r /* {caseId} */",
                    $"REC134_{caseId}_{Guid.NewGuid():N}",
                    provider,
                    new TestsLoggerResolver(),
                    TestCompilationOptions);

                exception = Assert.ThrowsExactly<QueryExecutionException>(
                    () => _ = compiled.Run().Count,
                    caseId);
                break;
            }
            default:
                exception = QueryExecutionException.ForExecutionFailure(
                    "row enumeration",
                    new InvalidOperationException($"substrate row value {sentinel}"));
                break;
        }

        AssertSafeExceptionSinks(exception, sentinel, caseId);
        AssertTrustedVerbosePolicy(exception, sentinel, caseId);
    }

    private static void AssertStructuredPayloadBoundary(string caseId, string sentinel)
    {
        var query = $"select '{sentinel}' from #rec134.items() r";
        var source = new SourceText(query);
        var literalStart = query.IndexOf(sentinel, StringComparison.Ordinal) - 1;
        var literalSpan = new TextSpan(literalStart, sentinel.Length + 2);
        var locations = source.GetLocations(literalSpan);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ9002_InternalExecutionError,
            DiagnosticSeverity.Error,
            $"Untrusted diagnostic value: {sentinel}",
            locations.Start,
            locations.End,
            phase: DiagnosticPhase.Internal,
            sourceKind: DiagnosticSourceKind.Query,
            arguments:
            [
                new KeyValuePair<string, string>("password", sentinel),
                new KeyValuePair<string, string>("substrate", sentinel),
                new KeyValuePair<string, string>("stableFact", "kept")
            ],
            relatedLocations:
            [
                new DiagnosticRelatedLocation(
                    locations.Start,
                    locations.End,
                    $"related value: {sentinel}")
            ],
            correlationId: $"REC134-{caseId}")
            .WithSourceContext(source, literalSpan)
            .WithSuggestedFix(DiagnosticAction.QuickFix(
                $"Replace {sentinel}",
                literalSpan,
                sentinel));

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        AssertNoSecret(
            sentinel,
            caseId,
            ("envelope", DescribeEnvelope(envelope)),
            ("envelope-text", MusoqErrorEnvelopeFormatter.FormatText(envelope)),
            ("envelope-json", MusoqErrorEnvelopeFormatter.FormatJson(envelope)),
            ("diagnostic", diagnostic.ToString()),
            ("diagnostic-detailed", diagnostic.ToDetailedString()),
            ("diagnostic-text", new DiagnosticFormatter { UseColor = false }.Format(diagnostic)),
            ("diagnostic-json", new DiagnosticFormatter().FormatAsJson(diagnostic)));
    }

    private static void AssertSafeExceptionSinks(
        QueryExecutionException exception,
        string sentinel,
        string caseId)
    {
        Assert.IsNotNull(exception.Envelope, caseId);
        AssertNoSecret(
            sentinel,
            caseId,
            ("exception-message", exception.Message),
            ("exception-format-text", exception.FormatText()),
            ("exception-format-json", exception.FormatJson()),
            ("envelope", DescribeEnvelope(exception.Envelope!)),
            ("envelope-text", MusoqErrorEnvelopeFormatter.FormatText(exception.Envelope!)),
            ("envelope-json", MusoqErrorEnvelopeFormatter.FormatJson(exception.Envelope!)));
    }

    private static void AssertTrustedVerbosePolicy(
        QueryExecutionException exception,
        string sentinel,
        string caseId)
    {
        var verbose = exception.FormatVerboseText();
        StringAssert.Contains(verbose, sentinel, caseId);
        StringAssert.Contains(verbose, "Trusted verbose", caseId);

        var directVerbose = MusoqErrorEnvelope.FromExceptionVerbose(
            new InvalidOperationException($"verbose diagnostic detail {sentinel}"));
        Assert.IsTrue(directVerbose.Details?.Contains(sentinel, StringComparison.Ordinal) == true, caseId);
        StringAssert.Contains(
            MusoqErrorEnvelopeFormatter.FormatText(directVerbose),
            "Trusted verbose",
            caseId);
    }

    private static void AssertRuntimeSettingsDoNotAppearInDescriptions(string caseId, string sentinel)
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new SecretSettingsResolver(sentinel);
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        using (var selected = InstanceCreator.CompileForExecution(
                   $"select Token from #settings.items() /* {caseId}-selected */",
                   $"REC134_{caseId}_selected_{Guid.NewGuid():N}",
                   provider,
                   new TestsLoggerResolver(),
                   options))
        {
            var selectedValue = selected.Run()[0][0];
            Assert.AreEqual(sentinel, selectedValue, caseId);
        }

        using var described = InstanceCreator.CompileForExecution(
            $"desc settings #settings.items() /* {caseId}-description */",
            $"REC134_{caseId}_description_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver(),
            options);
        var table = described.Run();
        var renderedValues = table.Rows
            .SelectMany(static row => row.Values)
            .Select(static value => value?.ToString() ?? string.Empty)
            .ToArray();

        Assert.IsFalse(renderedValues.Any(value => value.Contains(sentinel, StringComparison.Ordinal)), caseId);
        Assert.IsTrue(renderedValues.Contains("TOKEN", StringComparer.Ordinal), caseId);
        Assert.IsTrue(renderedValues.Contains("Provided", StringComparer.Ordinal), caseId);
        Assert.IsTrue(renderedValues.Contains("True", StringComparer.Ordinal), caseId);
    }

    private static void AssertSpanSelectedLiteralDoesNotReachDiagnosticSinks(string caseId, string sentinel)
    {
        var query = $"select '{sentinel}\nselect 1 from #rec134.items() r /* {caseId} */";
        var lexer = new Lexer(query, true, recoverOnError: true);
        var parseResult = new Musoq.Parser.Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
        var diagnostic = parseResult.Diagnostics.Single(item =>
            item.Code == DiagnosticCode.MQ1002_UnterminatedString);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        var formatter = new DiagnosticFormatter { UseColor = false };

        AssertNoSecret(
            sentinel,
            caseId,
            ("parse-format", parseResult.FormatDiagnostics()),
            ("diagnostic", diagnostic.ToString()),
            ("diagnostic-detailed", diagnostic.ToDetailedString()),
            ("diagnostic-text", formatter.Format(diagnostic)),
            ("diagnostic-json", formatter.FormatAsJson(diagnostic)),
            ("envelope", DescribeEnvelope(envelope)),
            ("envelope-text", MusoqErrorEnvelopeFormatter.FormatText(envelope)),
            ("envelope-json", MusoqErrorEnvelopeFormatter.FormatJson(envelope)));
    }

    private void AssertEngineLogsDoNotCaptureSubstrateValues(string caseId, string sentinel)
    {
        var resolver = new CapturingLoggerResolver();
        var provider = new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
        {
            ["#rec134"] =
            [
                new BinaryEntity
                {
                    Name = sentinel,
                    Content = [0x2A]
                }
            ]
        });
        var query = $"binary Rec134Header {{ Value: byte }}; select h.Value from #rec134.bytes() b cross apply Interpret<Rec134Header>(b.Content) h /* {caseId} */";

        using var compiled = CompileGeneratedQuery(
            query,
            $"REC134_{caseId}_{Guid.NewGuid():N}",
            provider,
            resolver,
            TestCompilationOptions);
        var table = compiled.Run();

        Assert.AreEqual(1, table.Count, caseId);
        Assert.IsNotEmpty(resolver.Entries, caseId);
        Assert.IsFalse(
            resolver.Entries.Any(entry => entry.Contains(sentinel, StringComparison.Ordinal)),
            caseId);
    }

    private static string DescribeEnvelope(MusoqErrorEnvelope envelope)
    {
        var parts = new List<string>
        {
            envelope.Message,
            envelope.Snippet ?? string.Empty,
            envelope.Explanation ?? string.Empty,
            envelope.Details ?? string.Empty,
            envelope.CorrelationId ?? string.Empty
        };
        parts.AddRange(envelope.SuggestedFixes);
        parts.AddRange(envelope.Arguments.SelectMany(static pair => new[] { pair.Key, pair.Value }));
        parts.AddRange(envelope.RelatedLocations.Select(static location => location.Message ?? string.Empty));
        parts.AddRange(envelope.Actions.SelectMany(static action => new[]
        {
            action.Title,
            action.TextEdit?.NewText ?? string.Empty
        }));
        return string.Join(" | ", parts);
    }

    private static void AssertNoSecret(
        string sentinel,
        string caseId,
        params (string Surface, string Value)[] surfaces)
    {
        foreach (var surface in surfaces)
            Assert.IsFalse(
                surface.Value.Contains(sentinel, StringComparison.Ordinal),
                $"{caseId}: sentinel reached {surface.Surface}.");
    }

    private static string CreateSentinel(string caseId) =>
        $"REC134_SYNTHETIC_SECRET_{caseId.Replace('-', '_')}_A7F9C2";

    private sealed class SecretSettingsResolver(string sentinel) : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request) =>
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TOKEN"] = sentinel
            };
    }

    private sealed class SecretFailureProvider(string sentinel) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            Assert.AreEqual("#rec134", schema);
            return new SecretFailureSchema(sentinel);
        }
    }

    private sealed class SecretFailureSchema(string sentinel) : SchemaBase("rec134", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => new SecretFailureTable();

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) =>
            throw new InvalidOperationException($"provider substrate failure {sentinel}");

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class SecretFailureTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn("Value", 0, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(SecretRow));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName == name);

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName == name).ToArray();
    }

    public sealed class SecretRow
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class CapturingLoggerResolver : ILoggerResolver
    {
        public List<string> Entries { get; } = [];

        public ILogger ResolveLogger() => new CapturingLogger(Entries);

        public ILogger<T> ResolveLogger<T>() => new CapturingLogger<T>(Entries);
    }

    private class CapturingLogger(ICollection<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add(formatter(state, exception));
        }
    }

    private sealed class CapturingLogger<T>(ICollection<string> entries)
        : CapturingLogger(entries), ILogger<T>;

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
