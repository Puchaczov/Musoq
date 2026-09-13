using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator;

/// <summary>
///     Holds two prepared LIKE matchers for one serial execution region or one parallel worker.
/// </summary>
/// <remarks>
///     Instances are execution-local. A worker-local instance may be shared by generated parallel helpers because it
///     partitions its bounded cache state by worker thread; a serial instance must remain confined to one region.
/// </remarks>
public sealed class LikeMatcherCacheSlot
{
    private readonly Func<string, PreparedLikeMatcher> _matcherFactory;
    private readonly SlotState _serialState;
    private readonly ConcurrentDictionary<int, SlotState>? _workerStates;

    /// <summary>Initializes an empty two-entry matcher cache.</summary>
    public LikeMatcherCacheSlot()
        : this(false)
    {
    }

    /// <summary>
    ///     Initializes an empty two-entry matcher cache, optionally isolating entries by parallel worker thread.
    /// </summary>
    /// <param name="workerLocal">
    ///     <see langword="true"/> to keep an independent two-entry state for each worker thread;
    ///     otherwise, to use one serial state.
    /// </param>
    public LikeMatcherCacheSlot(bool workerLocal)
        : this(workerLocal, PrepareMatcher)
    {
    }

    internal LikeMatcherCacheSlot(
        bool workerLocal,
        Func<string, PreparedLikeMatcher> matcherFactory)
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
    internal PreparedLikeMatcher GetOrAdd(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return GetState().GetOrAdd(pattern);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsMatch(string content, string pattern)
    {
        return GetState().IsMatch(content, pattern);
    }

    internal void Clear()
    {
        _serialState.Clear();
        if (_workerStates is not null)
            _workerStates.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SlotState GetState() => _workerStates is null
        ? _serialState
        : _workerStates.GetOrAdd(
            Environment.CurrentManagedThreadId,
            static (_, factory) => new SlotState(factory),
            _matcherFactory);

    private static PreparedLikeMatcher PrepareMatcher(string pattern) =>
        Operators.PrepareLike(pattern)
        ?? throw new InvalidOperationException("A non-null LIKE pattern did not produce a matcher.");

    private sealed class SlotState(Func<string, PreparedLikeMatcher> matcherFactory)
    {
        private CacheEntry? _first;
        private CacheEntry? _second;
        private bool _replaceFirst = true;

        public int Count => (_first is null ? 0 : 1) + (_second is null ? 0 : 1);

        public int MatcherConstructionCount { get; private set; }

        public PreparedLikeMatcher GetOrAdd(string pattern)
        {
            return GetOrAddEntry(pattern, CultureInfo.CurrentCulture).Matcher;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(string content, string pattern)
        {
            var culture = CultureInfo.CurrentCulture;
            if (_first is { } first && first.Matches(pattern, culture))
                return first.IsMatch(content);
            if (_second is { } second && second.Matches(pattern, culture))
                return second.IsMatch(content);

            return GetOrAddEntry(pattern, culture).IsMatch(content);
        }

        private CacheEntry GetOrAddEntry(string pattern, CultureInfo culture)
        {
            if (_first is { } first && first.Matches(pattern, culture))
                return first;
            if (_second is { } second && second.Matches(pattern, culture))
                return second;

            var matcher = matcherFactory(pattern);
            var entry = new CacheEntry(pattern, culture, matcher);
            MatcherConstructionCount++;

            if (_first is null)
            {
                _first = entry;
            }
            else if (_second is null)
            {
                _second = entry;
            }
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

            return entry;
        }

        public void Clear()
        {
            _first = null;
            _second = null;
            _replaceFirst = true;
            MatcherConstructionCount = 0;
        }
    }

    private sealed class CacheEntry
    {
        private readonly bool _isSingleAsciiPrefix;
        private readonly char _singleAsciiPrefix;

        public CacheEntry(string pattern, CultureInfo culture, PreparedLikeMatcher matcher)
        {
            Pattern = pattern;
            Culture = culture;
            Matcher = matcher;
            _isSingleAsciiPrefix = matcher.IsSingleAsciiPrefix;
            _singleAsciiPrefix = _isSingleAsciiPrefix ? matcher.SingleAsciiPrefix : '\0';
        }

        public string Pattern { get; }

        public CultureInfo Culture { get; }

        public PreparedLikeMatcher Matcher { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(string pattern, CultureInfo culture)
        {
            var patternMatches = ReferenceEquals(Pattern, pattern) ||
                                 Pattern.Length == pattern.Length &&
                                 (pattern.Length == 2
                                     ? Pattern[0] == pattern[0] && Pattern[1] == pattern[1]
                                     : string.Equals(Pattern, pattern, StringComparison.Ordinal));
            if (!patternMatches)
                return false;

            return ReferenceEquals(Culture, culture) ||
                   string.Equals(Culture.Name, culture.Name, StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(string content)
        {
            if (!_isSingleAsciiPrefix || content.Length == 0)
                return Matcher.IsMatch(content);

            var actual = content[0];
            if (actual > 0x7f)
                return Matcher.IsMatch(content);
            if (actual == _singleAsciiPrefix)
                return true;

            var foldedActual = (uint)(actual | 0x20);
            return foldedActual == (uint)(_singleAsciiPrefix | 0x20) &&
                   foldedActual - 'a' <= 'z' - 'a';
        }
    }
}
