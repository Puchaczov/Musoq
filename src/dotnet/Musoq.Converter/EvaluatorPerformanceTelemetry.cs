using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

internal static class EvaluatorPerformanceTelemetry
{
    private const string TimingDirectoryVariable = "MUSOQ_EVALUATOR_TIMING_DIRECTORY";
    private const string EnabledVariable = "MUSOQ_EVALUATOR_PERF_TELEMETRY";
    private static readonly object Sync = new();
    private static readonly AsyncLocal<CompilationScope?> CurrentScope = new();
    private static readonly AsyncLocal<ConsumerScope?> CurrentConsumerScope = new();
    private static readonly AsyncLocal<PhaseFrame?> CurrentPhase = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly bool Enabled = string.Equals(
        Environment.GetEnvironmentVariable(EnabledVariable),
        "1",
        StringComparison.Ordinal);
    private static readonly string? TimingDirectory =
        Enabled ? Environment.GetEnvironmentVariable(TimingDirectoryVariable) : null;
    private static readonly CompilationScope DisabledCompilationScope = new();

    public static bool IsEnabled => Enabled && !string.IsNullOrWhiteSpace(TimingDirectory);

    internal static IDisposable BeginConsumerScope(string family, string? testName = null)
    {
        if (!IsEnabled)
            return NoopDisposable.Instance;

        var previous = CurrentConsumerScope.Value;
        CurrentConsumerScope.Value = new ConsumerScope(family, testName, previous);
        return new ConsumerScopeLease(previous);
    }

    public static CompilationScope BeginCompilation(
        string script,
        string assemblyName,
        ISchemaProvider provider,
        CompilationOptions options)
    {
        return IsEnabled
            ? new CompilationScope(script, assemblyName, provider, options)
            : DisabledCompilationScope;
    }

    internal static PhaseScope BeginPhase(string name)
    {
        if (!IsEnabled || CurrentScope.Value is not { } scope)
            return default;

        return new PhaseScope(scope, name, Stopwatch.GetTimestamp());
    }

    public static void WriteTestCase(TestCaseEvent testCase)
    {
        if (!IsEnabled)
            return;

        Append("test-case-events.jsonl", testCase);
    }

    internal static BatchScope BeginBatchFinalization(
        string batchId,
        int itemCount,
        string compatibilityFingerprint,
        string? origin = null,
        int? compatibilityGroupCount = null,
        double? queueDelayMilliseconds = null)
    {
        return IsEnabled
            ? new BatchScope(
                batchId,
                itemCount,
                Fingerprint(compatibilityFingerprint),
                origin,
                compatibilityGroupCount,
                queueDelayMilliseconds,
                CurrentConsumerScope.Value)
            : DisabledBatchScope.Instance;
    }

    internal sealed class CompilationScope : IDisposable
    {
        private readonly long _started = Stopwatch.GetTimestamp();
        private readonly string _queryFingerprint = string.Empty;
        private readonly string _assemblyName = string.Empty;
        private readonly string _providerType = string.Empty;
        private readonly string? _consumerFamily;
        private readonly string? _consumerTestName;
        private string _providerSignature = string.Empty;
        private string? _providerContractBucket;
        private string? _batchId;
        private int? _batchSize;
        private string _compilationMode = "single";
        private string _reusePath = "compile";
        private string? _artifactIdentity;
        private string? _bindingIdentity;
        private bool _realEmission;
        private bool _realLoad;
        private readonly string _optionsFingerprint = string.Empty;
        private readonly Dictionary<string, PhaseAggregate> _phases = new(StringComparer.Ordinal);
        private bool _disposed;
        private CompilationScope? _parent;

        internal CompilationScope()
        {
        }

        internal CompilationScope(string script, string assemblyName, ISchemaProvider provider, CompilationOptions options)
        {
            if (!IsEnabled)
                return;

            _queryFingerprint = Fingerprint(script);
            _assemblyName = assemblyName;
            _providerType = provider.GetType().AssemblyQualifiedName ?? provider.GetType().FullName ?? provider.GetType().Name;
            _consumerFamily = CurrentConsumerScope.Value?.Family;
            _consumerTestName = CurrentConsumerScope.Value?.TestName;
            _providerSignature = "<not-recorded>";
            _optionsFingerprint = CompilationOptionsFingerprint.Compute(options);
            _parent = CurrentScope.Value;
            CurrentScope.Value = this;
        }

        internal void SetProviderSignature(string? signature)
        {
            if (IsEnabled && !string.IsNullOrWhiteSpace(signature))
                _providerSignature = Fingerprint(signature);
        }

        public bool CacheEligible { get; set; }

        public string CacheOutcome { get; set; } = "not-eligible";

        public string? SemanticContractFingerprint { get; private set; }

        public void SetCompilationMode(string mode, string? batchId = null, int? batchSize = null)
        {
            if (!IsEnabled)
                return;

            _compilationMode = string.IsNullOrWhiteSpace(mode) ? "single" : mode;
            _batchId = batchId;
            _batchSize = batchSize;
        }

        public void SetReusePath(string path)
        {
            if (IsEnabled && !string.IsNullOrWhiteSpace(path))
                _reusePath = path;
        }

        public void SetArtifactIdentity(string? identity, bool emitted, bool loaded)
        {
            if (!IsEnabled)
                return;

            _artifactIdentity = string.IsNullOrWhiteSpace(identity) ? null : Fingerprint(identity);
            _realEmission |= emitted;
            _realLoad |= loaded;
        }

        public void SetBindingIdentity(string? identity)
        {
            if (IsEnabled && !string.IsNullOrWhiteSpace(identity))
                _bindingIdentity = Fingerprint(identity);
        }

        public void SetSemanticContractFingerprint(string fingerprint)
        {
            if (IsEnabled)
                SemanticContractFingerprint = fingerprint;
        }

        public void SetProviderContractBucket(string? bucket)
        {
            if (IsEnabled)
                _providerContractBucket = string.IsNullOrWhiteSpace(bucket) ? null : Fingerprint(bucket);
        }

        public void AddPhase(string name, long startedTimestamp)
        {
            if (IsEnabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds;
                AddPhase(name, elapsed, elapsed);
            }
        }

        public void AddPhase(string name, double inclusiveMilliseconds, double exclusiveMilliseconds)
        {
            if (!IsEnabled)
                return;

            if (!_phases.TryGetValue(name, out var aggregate))
                aggregate = new PhaseAggregate();

            aggregate.Count++;
            aggregate.InclusiveMilliseconds += inclusiveMilliseconds;
            aggregate.ExclusiveMilliseconds += Math.Max(0, exclusiveMilliseconds);
            aggregate.MaxInclusiveMilliseconds = Math.Max(
                aggregate.MaxInclusiveMilliseconds,
                inclusiveMilliseconds);
            _phases[name] = aggregate;
        }

        public void SetCacheOutcome(string outcome)
        {
            if (IsEnabled)
                CacheOutcome = outcome;
        }

        public void Dispose()
        {
            if (_disposed || !IsEnabled)
                return;

            _disposed = true;
            var phaseDetails = _phases.ToDictionary(
                static pair => pair.Key,
                static pair => new
                {
                    inclusiveMilliseconds = pair.Value.InclusiveMilliseconds,
                    exclusiveMilliseconds = pair.Value.ExclusiveMilliseconds,
                    count = pair.Value.Count,
                    maxInclusiveMilliseconds = pair.Value.MaxInclusiveMilliseconds
                },
                StringComparer.Ordinal);

            Append("compilation-stages.jsonl", new
            {
                kind = "compilation",
                utc = DateTimeOffset.UtcNow,
                processId = Environment.ProcessId,
                threadId = Environment.CurrentManagedThreadId,
                queryFingerprint = _queryFingerprint,
                assemblyName = _assemblyName,
                consumerFamily = _consumerFamily,
                consumerTestName = _consumerTestName,
                providerType = _providerType,
                providerSignature = _providerSignature,
                providerContractBucket = _providerContractBucket,
                optionsFingerprint = _optionsFingerprint,
                cacheEligible = CacheEligible,
                cacheOutcome = CacheOutcome,
                semanticContractFingerprint = SemanticContractFingerprint,
                compilationMode = _compilationMode,
                batchId = _batchId,
                batchSize = _batchSize,
                reusePath = _reusePath,
                artifactIdentity = _artifactIdentity,
                bindingIdentity = _bindingIdentity,
                realEmission = _realEmission,
                realLoad = _realLoad,
                totalMilliseconds = Stopwatch.GetElapsedTime(_started).TotalMilliseconds,
                phases = _phases.ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value.ExclusiveMilliseconds,
                    StringComparer.Ordinal),
                phaseDetails
            });
            CurrentScope.Value = _parent;
        }
    }

    private sealed class PhaseFrame(PhaseFrame? parent)
    {
        public PhaseFrame? Parent { get; } = parent;

        public long ChildElapsedTicks { get; set; }
    }

    internal class BatchScope : IDisposable
    {
        private readonly long _started = Stopwatch.GetTimestamp();
        private readonly string _batchId;
        private readonly int _itemCount;
        private readonly string _compatibilityFingerprint;
        private readonly string? _origin;
        private readonly int? _compatibilityGroupCount;
        private readonly double? _queueDelayMilliseconds;
        private readonly string? _consumerFamily;
        private readonly string? _consumerTestName;
        private bool _succeeded;
        private bool _emitted;
        private bool _loaded;
        private string? _fallbackReason;
        private string? _artifactIdentity;
        private bool _disposed;

        internal BatchScope(
            string batchId,
            int itemCount,
            string compatibilityFingerprint,
            string? origin,
            int? compatibilityGroupCount,
            double? queueDelayMilliseconds,
            ConsumerScope? consumer)
        {
            _batchId = batchId;
            _itemCount = itemCount;
            _compatibilityFingerprint = compatibilityFingerprint;
            _origin = origin;
            _compatibilityGroupCount = compatibilityGroupCount;
            _queueDelayMilliseconds = queueDelayMilliseconds;
            _consumerFamily = consumer?.Family;
            _consumerTestName = consumer?.TestName;
        }

        public void SetResult(string? artifactIdentity, bool succeeded, bool emitted, bool loaded)
        {
            if (!IsEnabled)
                return;

            _artifactIdentity = string.IsNullOrWhiteSpace(artifactIdentity)
                ? null
                : Fingerprint(artifactIdentity);
            _succeeded = succeeded;
            _emitted = emitted;
            _loaded = loaded;
        }

        public void SetFallbackReason(string? reason)
        {
            if (IsEnabled && !string.IsNullOrWhiteSpace(reason))
                _fallbackReason = reason;
        }

        public void Dispose()
        {
            if (_disposed || !IsEnabled)
                return;

            _disposed = true;
            Append("execution-batches.jsonl", new
            {
                kind = "execution-batch",
                utc = DateTimeOffset.UtcNow,
                processId = Environment.ProcessId,
                threadId = Environment.CurrentManagedThreadId,
                batchId = _batchId,
                itemCount = _itemCount,
                compatibilityFingerprint = _compatibilityFingerprint,
                origin = _origin,
                compatibilityGroupCount = _compatibilityGroupCount,
                queueDelayMilliseconds = _queueDelayMilliseconds,
                consumerFamily = _consumerFamily,
                consumerTestName = _consumerTestName,
                succeeded = _succeeded,
                realEmission = _emitted,
                realLoad = _loaded,
                artifactIdentity = _artifactIdentity,
                fallbackReason = _fallbackReason,
                totalMilliseconds = Stopwatch.GetElapsedTime(_started).TotalMilliseconds
            });
        }
    }

    private sealed class DisabledBatchScope : BatchScope
    {
        internal static DisabledBatchScope Instance { get; } = new();

        private DisabledBatchScope()
            : base(string.Empty, 0, string.Empty, null, null, null, null)
        {
        }

        public new void SetResult(string? artifactIdentity, bool succeeded, bool emitted, bool loaded)
        {
        }

        public new void SetFallbackReason(string? reason)
        {
        }

        public new void Dispose()
        {
        }
    }

    internal sealed class ConsumerScope(string family, string? testName, ConsumerScope? parent)
    {
        public string Family { get; } = family;
        public string? TestName { get; } = testName;
        public ConsumerScope? Parent { get; } = parent;
    }

    private sealed class ConsumerScopeLease(ConsumerScope? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            CurrentConsumerScope.Value = previous;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        internal static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    internal struct PhaseScope : IDisposable
    {
        private readonly CompilationScope? _scope;
        private readonly string? _name;
        private readonly long _startedTimestamp;
        private readonly PhaseFrame? _frame;
        private readonly PhaseFrame? _parent;
        private bool _disposed;

        internal PhaseScope(CompilationScope scope, string name, long startedTimestamp)
        {
            _scope = scope;
            _name = name;
            _startedTimestamp = startedTimestamp;
            _parent = CurrentPhase.Value;
            _frame = new PhaseFrame(_parent);
            CurrentPhase.Value = _frame;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            var elapsedTicks = Stopwatch.GetTimestamp() - _startedTimestamp;
            var elapsedMilliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;
            var childMilliseconds = (_frame?.ChildElapsedTicks ?? 0) * 1000.0 / Stopwatch.Frequency;

            if (_scope is not null && _name is not null)
                _scope.AddPhase(_name, elapsedMilliseconds, elapsedMilliseconds - childMilliseconds);

            if (_parent is not null)
                _parent.ChildElapsedTicks += elapsedTicks;

            CurrentPhase.Value = _parent;
        }
    }

    private sealed class PhaseAggregate
    {
        public int Count { get; set; }

        public double InclusiveMilliseconds { get; set; }

        public double ExclusiveMilliseconds { get; set; }

        public double MaxInclusiveMilliseconds { get; set; }
    }

    internal sealed record TestCaseEvent(
        string ParentMethod,
        string CaseId,
        string? SampleName,
        string? Profile,
        double ElapsedMilliseconds,
        double CompilationMilliseconds,
        double ExecutionMilliseconds,
        double MaterializationMilliseconds,
        bool MaterializationCompleted,
        int ProcessId,
        int ThreadId,
        DateTimeOffset StartedUtc,
        DateTimeOffset FinishedUtc);

    private static void Append(string fileName, object value)
    {
        var directory = TimingDirectory;
        if (!IsEnabled || string.IsNullOrWhiteSpace(directory))
            return;

        try
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, fileName);
            var line = JsonSerializer.Serialize(value, JsonOptions);
            lock (Sync)
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Measurement must never change test behavior.
        }
    }

    private static string Fingerprint(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
