using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class OptimizationAnalysisFactSet
{
    private readonly Dictionary<string, OptimizationAnalysisFact> _facts = new(StringComparer.Ordinal);

    private int _nextPassRunId;
    private int _currentPassRunId;
    private OptimizationStage _currentStage;
    private string? _currentPassName;
    private int _currentIteration;
    private readonly HashSet<string> _currentPassConsumedFacts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _currentPassRecomputedFacts = new(StringComparer.Ordinal);

    public IReadOnlyList<OptimizationAnalysisFact> Snapshot()
    {
        return _facts.Values
            .OrderBy(static fact => fact.Key, StringComparer.Ordinal)
            .ToArray();
    }

    public void Set<T>(
        string key,
        T value,
        OptimizationAnalysisInvalidationRule invalidationRule = OptimizationAnalysisInvalidationRule.OnPlanChanged)
    {
        ValidateKey(key);

        _facts[key] = new OptimizationAnalysisFact(
            key,
            value,
            typeof(T),
            invalidationRule,
            _currentStage,
            _currentPassName,
            _currentIteration,
            [])
        {
            ProducedInPassRun = _currentPassRunId
        };
        _currentPassRecomputedFacts.Add(key);
    }

    public bool TryGet<T>(string key, out T? value)
    {
        ValidateKey(key);

        if (!_facts.TryGetValue(key, out var fact) ||
            !TryCastFactValue(fact, out value))
        {
            value = default;
            return false;
        }

        return true;
    }

    public bool TryConsume<T>(string key, string consumer, out T? value)
    {
        ValidateKey(key);
        if (string.IsNullOrWhiteSpace(consumer))
            throw new ArgumentException("Analysis fact consumer cannot be null or whitespace.", nameof(consumer));

        if (!_facts.TryGetValue(key, out var fact) ||
            !TryCastFactValue(fact, out value))
        {
            value = default;
            return false;
        }

        _facts[key] = fact with
        {
            Consumers = [.. fact.Consumers, consumer]
        };
        _currentPassConsumedFacts.Add(key);
        return true;
    }

    internal void BeginPass(OptimizationStage stage, string passName, int iteration)
    {
        if (string.IsNullOrWhiteSpace(passName))
            throw new ArgumentException("Optimization pass name cannot be null or whitespace.", nameof(passName));

        _currentStage = stage;
        _currentPassName = passName;
        _currentIteration = iteration;
        _currentPassRunId = ++_nextPassRunId;
        _currentPassConsumedFacts.Clear();
        _currentPassRecomputedFacts.Clear();
    }

    internal int InvalidateForPlanChange()
    {
        var invalidatedKeys = _facts
            .Where(pair =>
                pair.Value.InvalidationRule == OptimizationAnalysisInvalidationRule.OnPlanChanged &&
                pair.Value.ProducedInPassRun != _currentPassRunId)
            .Select(static pair => pair.Key)
            .ToArray();

        foreach (var key in invalidatedKeys)
            _facts.Remove(key);

        return invalidatedKeys.Length;
    }

    internal string? CreateCurrentPassFactDiagnostic(int invalidatedFactCount)
    {
        if (_currentPassConsumedFacts.Count == 0 &&
            _currentPassRecomputedFacts.Count == 0 &&
            invalidatedFactCount == 0)
        {
            return null;
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"analysis facts: consumed {_currentPassConsumedFacts.Count}, recomputed {_currentPassRecomputedFacts.Count}, invalidated {invalidatedFactCount}.");
    }

    private static bool TryCastFactValue<T>(OptimizationAnalysisFact fact, out T? value)
    {
        if (fact.Value is null)
        {
            if (default(T) is null)
            {
                value = default;
                return true;
            }

            value = default;
            return false;
        }

        if (fact.Value is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Analysis fact key cannot be null or whitespace.", nameof(key));
    }
}
