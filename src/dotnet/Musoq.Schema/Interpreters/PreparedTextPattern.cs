using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Musoq.Schema.Interpreters;

/// <summary>A schema's constant pattern and capture mapping, compiled on first use.</summary>
public sealed class PreparedTextPattern
{
    private readonly string _anchored;
    private readonly string? _validationPattern;
    private readonly string[] _captureNames;
    private readonly Lock _compileLock = new();
    private volatile CompiledPattern? _compiled;

    /// <summary>Prepares the immutable configuration of a pattern field.</summary>
    public PreparedTextPattern(string pattern, TextPatternModifiers modifiers, string[]? captureNames = null)
        : this(pattern, modifiers, captureNames, isLookahead: false)
    {
    }

    private PreparedTextPattern(string pattern, TextPatternModifiers modifiers, string[]? captureNames, bool isLookahead)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Pattern = pattern;
        Modifiers = modifiers;
        IsLookahead = isLookahead;
        _captureNames = captureNames is null ? [] : (string[])captureNames.Clone();
        var normalized = TextPatternCompiler.Normalize(TextPatternCompiler.ApplyGreediness(pattern,
            modifiers.HasFlag(TextPatternModifiers.Greedy), modifiers.HasFlag(TextPatternModifiers.Lazy)));
        _validationPattern = isLookahead ? null : normalized;
        _anchored = isLookahead && normalized.StartsWith('^') ? normalized : "^" + normalized;
    }

    /// <summary>Prepares a non-consuming switch lookahead.</summary>
    public static PreparedTextPattern CreateLookahead(string pattern) =>
        new(pattern, TextPatternModifiers.None, null, isLookahead: true);

    internal string Pattern { get; }
    internal TextPatternModifiers Modifiers { get; }
    internal bool IsLookahead { get; }
    internal bool HasConflictingQuantifiers =>
        Modifiers.HasFlag(TextPatternModifiers.Greedy | TextPatternModifiers.Lazy);

    internal Regex Regex
    {
        get
        {
            if (_compiled is { } compiled)
                return compiled.Regex;
            lock (_compileLock)
            {
                if (_compiled is { } existing)
                    return existing.Regex;
                var regex = TextPatternCompiler.Compile(_anchored, _validationPattern);
                _compiled = new CompiledPattern(regex, _captureNames.Select(regex.GroupNumberFromName).ToArray());
                return regex;
            }
        }
    }

    internal string Capture(Match match, int slot)
    {
        var group = _compiled!.GroupNumbers[slot];
        return group < 0 ? string.Empty : match.Groups[group].Value;
    }

    private sealed record CompiledPattern(Regex Regex, int[] GroupNumbers);
}
