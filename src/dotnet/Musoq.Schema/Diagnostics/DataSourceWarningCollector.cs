using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Diagnostics;

/// <summary>Thread-safe warnings owned by one query execution, including deferred row collection.</summary>
public sealed class DataSourceWarningCollector
{
    public const int MaximumWarnings = 100;
    private readonly object _gate = new();
    private Dictionary<string, Entry>? _warnings;
    private Dictionary<(SourceIdentity Identity, string Operation), BoundReporter>? _reporters;
    private BoundReporter? _lastReporter;
    private long _suppressedCount;

    public long SuppressedCount
    {
        get
        {
            lock (_gate)
                return _suppressedCount;
        }
    }

    public IReadOnlyList<Diagnostic> Snapshot()
    {
        lock (_gate)
        {
            var result =
                _warnings
                    ?.Values.Select(entry =>
                        entry.Diagnostic.WithArgument(
                            "occurrenceCount",
                            entry.Count.ToString(CultureInfo.InvariantCulture)
                        )
                    )
                    .ToList()
                ?? [];
            if (_suppressedCount > 0)
                result.Add(
                    new DataSourceDiagnostic(
                        "CORE.WARNINGS_SUPPRESSED",
                        "reporting",
                        DataSourceDiagnosticSeverity.Warning,
                        "Additional datasource warnings were suppressed."
                    )
                        .ToDiagnostic()
                        .WithArgument("suppressedCount", _suppressedCount.ToString(CultureInfo.InvariantCulture))
                );
            return result.AsReadOnly();
        }
    }

    public Action<DataSourceDiagnostic> ForSource(SourceIdentity identity, string operation = "read")
    {
        ArgumentNullException.ThrowIfNull(identity);
        var last = Volatile.Read(ref _lastReporter);
        if (last is not null && ReferenceEquals(last.Identity, identity) && last.Operation == operation)
            return last.Callback;

        return BindReporter(identity, operation);
    }

    private Action<DataSourceDiagnostic> BindReporter(SourceIdentity identity, string operation)
    {
        lock (_gate)
        {
            _reporters ??= new();
            var key = (identity, operation);
            if (!_reporters.TryGetValue(key, out var reporter))
            {
                reporter = new BoundReporter(this, identity, operation);
                if (_reporters.Count >= MaximumWarnings)
                    return reporter.Callback;
                _reporters.Add(key, reporter);
            }

            Volatile.Write(ref _lastReporter, reporter);
            return reporter.Callback;
        }
    }

    private void Report(DataSourceDiagnostic report, SourceIdentity identity, string operation)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.Severity != DataSourceDiagnosticSeverity.Warning)
            throw new ArgumentException("Report errors by throwing DataSourceException.", nameof(report));
        var diagnostic = report
            .ToDiagnostic()
            .WithArgument("schema", identity.SchemaName)
            .WithArgument("source", identity.MethodName)
            .WithArgument("alias", identity.Alias)
            .WithArgument("sourceContextId", identity.SourceContextId)
            .WithArgument("operation", operation);
        var key = MusoqErrorEnvelopeFormatter.FormatJson(MusoqErrorEnvelope.FromDiagnostic(diagnostic));
        lock (_gate)
        {
            _warnings ??= new Dictionary<string, Entry>(StringComparer.Ordinal);
            if (_warnings.TryGetValue(key, out var existing))
            {
                existing.Count++;
                return;
            }
            if (_warnings.Count == MaximumWarnings)
            {
                _suppressedCount++;
                return;
            }
            _warnings.Add(key, new Entry(diagnostic));
        }
    }

    public ISchemaProvider WrapProvider(ISchemaProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new ReportingProvider(provider is ReportingProvider prior ? prior.Inner : provider, this);
    }

    public static Action<DataSourceDiagnostic>? GetReporter(ISchemaProvider provider, SourceIdentity identity) =>
        provider is ReportingProvider bound ? bound.Collector.ForSource(identity) : null;

    private sealed class BoundReporter
    {
        private readonly DataSourceWarningCollector _collector;

        internal BoundReporter(DataSourceWarningCollector collector, SourceIdentity identity, string operation)
        {
            _collector = collector;
            Identity = identity;
            Operation = operation;
            Callback = Report;
        }

        internal SourceIdentity Identity { get; }
        internal string Operation { get; }
        internal Action<DataSourceDiagnostic> Callback { get; }

        private void Report(DataSourceDiagnostic report) => _collector.Report(report, Identity, Operation);
    }

    private sealed class Entry(Diagnostic diagnostic)
    {
        internal Diagnostic Diagnostic { get; } = diagnostic;
        internal long Count { get; set; } = 1;
    }

    private sealed class ReportingProvider(ISchemaProvider inner, DataSourceWarningCollector collector)
        : ISchemaProvider
    {
        internal ISchemaProvider Inner { get; } = inner;
        internal DataSourceWarningCollector Collector { get; } = collector;

        public ISchema GetSchema(string schema) => Inner.GetSchema(schema);
    }
}
