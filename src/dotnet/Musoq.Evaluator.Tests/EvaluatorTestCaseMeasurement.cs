using System.Diagnostics;
using System;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

internal sealed class EvaluatorTestCaseMeasurement : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly DateTimeOffset _startedUtc = DateTimeOffset.UtcNow;
    private readonly string _parentMethod;
    private readonly string _caseId;
    private readonly string? _sampleName;
    private readonly string? _profile;
    private long _compilationMilliseconds;
    private long _executionMilliseconds;
    private long _materializationMilliseconds;
    private bool _materializationCompleted;
    private bool _disposed;

    private EvaluatorTestCaseMeasurement(string parentMethod, string caseId, string? sampleName, string? profile)
    {
        _parentMethod = parentMethod;
        _caseId = caseId;
        _sampleName = sampleName;
        _profile = profile;
    }

    public static EvaluatorTestCaseMeasurement Begin(
        string parentMethod,
        string caseId,
        string? sampleName = null,
        string? profile = null)
    {
        return new EvaluatorTestCaseMeasurement(parentMethod, caseId, sampleName, profile);
    }

    public T MeasureCompilation<T>(Func<T> compile)
    {
        var started = _stopwatch.ElapsedMilliseconds;
        var result = compile();
        _compilationMilliseconds += _stopwatch.ElapsedMilliseconds - started;
        return result;
    }

    public T MeasureExecution<T>(Func<T> execute)
    {
        var started = _stopwatch.ElapsedMilliseconds;
        var result = execute();
        _executionMilliseconds += _stopwatch.ElapsedMilliseconds - started;
        return result;
    }

    public T MeasureMaterialization<T>(Func<T> materialize)
    {
        var started = _stopwatch.ElapsedMilliseconds;
        var result = materialize();
        _materializationMilliseconds += _stopwatch.ElapsedMilliseconds - started;
        _materializationCompleted = true;
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var finishedUtc = DateTimeOffset.UtcNow;
        EvaluatorPerformanceTelemetry.WriteTestCase(
            new EvaluatorPerformanceTelemetry.TestCaseEvent(
                _parentMethod,
                _caseId,
                _sampleName,
                _profile,
                _stopwatch.Elapsed.TotalMilliseconds,
                _compilationMilliseconds,
                _executionMilliseconds,
                _materializationMilliseconds,
                _materializationCompleted,
                Environment.ProcessId,
                Environment.CurrentManagedThreadId,
                _startedUtc,
                finishedUtc));
    }
}
