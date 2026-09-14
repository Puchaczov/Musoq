using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator;

/// <summary>Holds two prepared RLIKE matchers for one serial region or parallel worker.</summary>
public sealed class RLikeMatcherCacheSlot
{
    private readonly Func<string, PreparedRLikeMatcher> _matcherFactory;
    private readonly SlotState _serialState;
    private readonly ConcurrentDictionary<int, SlotState>? _workerStates;

    /// <summary>Initializes a serial two-entry matcher cache.</summary>
    public RLikeMatcherCacheSlot()
        : this(false)
    {
    }

    /// <summary>Initializes a two-entry matcher cache, optionally partitioned by worker thread.</summary>
    public RLikeMatcherCacheSlot(bool workerLocal)
        : this(workerLocal, PrepareMatcher)
    {
    }

    internal RLikeMatcherCacheSlot(
        bool workerLocal,
        Func<string, PreparedRLikeMatcher> matcherFactory)
    {
        _matcherFactory = matcherFactory ?? throw new ArgumentNullException(nameof(matcherFactory));
        _serialState = new SlotState(_matcherFactory);
        _workerStates = workerLocal ? new ConcurrentDictionary<int, SlotState>() : null;
    }

    internal int Count => _workerStates is null
        ? _serialState.Count
        : _workerStates.Values.Sum(static state => state.Count);

    internal int MatcherConstructionCount => _workerStates is null
        ? _serialState.MatcherConstructionCount
        : _workerStates.Values.Sum(static state => state.MatcherConstructionCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsMatch(string input, string pattern) => GetState().IsMatch(input, pattern);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SlotState GetState() => _workerStates is null
        ? _serialState
        : _workerStates.GetOrAdd(
            Environment.CurrentManagedThreadId,
            static (_, factory) => new SlotState(factory),
            _matcherFactory);

    private static PreparedRLikeMatcher PrepareMatcher(string pattern) =>
        Operators.PrepareRLike(pattern)
        ?? throw new InvalidOperationException("A non-null RLIKE pattern did not produce a matcher.");

    private sealed class SlotState(Func<string, PreparedRLikeMatcher> matcherFactory)
    {
        private CacheEntry? _first;
        private CacheEntry? _second;
        private bool _replaceFirst = true;

        public int Count => (_first is null ? 0 : 1) + (_second is null ? 0 : 1);

        public int MatcherConstructionCount { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(string input, string pattern)
        {
            if (_first is { } first && first.Matches(pattern))
                return first.Matcher.IsMatch(input);
            if (_second is { } second && second.Matches(pattern))
                return second.Matcher.IsMatch(input);

            return Add(pattern).IsMatch(input);
        }

        public void Clear()
        {
            _first = null;
            _second = null;
            _replaceFirst = true;
            MatcherConstructionCount = 0;
        }

        private PreparedRLikeMatcher Add(string pattern)
        {
            var matcher = matcherFactory(pattern);
            var entry = new CacheEntry(pattern, matcher);
            MatcherConstructionCount++;

            if (_first is null)
                _first = entry;
            else if (_second is null)
                _second = entry;
            else if (_replaceFirst)
            {
                _first = entry;
                _replaceFirst = false;
            }
            else
            {
                _second = entry;
                _replaceFirst = true;
            }

            return matcher;
        }
    }

    private sealed record CacheEntry(string Pattern, PreparedRLikeMatcher Matcher)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(string pattern) =>
            ReferenceEquals(Pattern, pattern) || string.Equals(Pattern, pattern, StringComparison.Ordinal);
    }
}
