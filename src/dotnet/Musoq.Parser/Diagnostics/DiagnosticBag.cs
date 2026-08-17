using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     A thread-safe collection for accumulating diagnostics during compilation.
/// </summary>
public sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly ConcurrentBag<Diagnostic> _diagnostics = new();
    private readonly ConcurrentDictionary<DiagnosticIdentity, byte> _diagnosticKeys = new();
    private readonly ConcurrentDictionary<DiagnosticIdentity, long> _diagnosticOrder = new();
    private long _nextSequence;
    private int _errorCount;
    private int _warningCount;

    /// <summary>
    ///     Gets the source text for context generation.
    /// </summary>
    public SourceText? SourceText { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of errors to collect before stopping.
    ///     Default is 100.
    /// </summary>
    public int MaxErrors { get; set; } = 100;

    /// <summary>
    ///     Gets the number of errors collected.
    /// </summary>
    public int ErrorCount => _errorCount;

    /// <summary>
    ///     Gets the number of warnings collected.
    /// </summary>
    public int WarningCount => _warningCount;

    /// <summary>
    ///     Gets the total number of diagnostics collected.
    /// </summary>
    public int Count => _diagnostics.Count;

    /// <summary>
    ///     Returns true if any errors have been collected.
    /// </summary>
    public bool HasErrors => _errorCount > 0;

    /// <summary>
    ///     Returns true if the maximum error count has been reached.
    /// </summary>
    public bool HasTooManyErrors => _errorCount >= MaxErrors;

    public IEnumerator<Diagnostic> GetEnumerator()
    {
        return _diagnostics.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Adds a diagnostic to the bag.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    /// <returns>True if the diagnostic was added, false if max errors reached.</returns>
    public bool Add(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        if (diagnostic.IsError && HasTooManyErrors)
            return false;

        var key = DiagnosticIdentity.Create(diagnostic);
        if (!_diagnosticKeys.TryAdd(key, 0))
            return false;

        _diagnosticOrder.TryAdd(key, Interlocked.Increment(ref _nextSequence));
        _diagnostics.Add(diagnostic);

        if (diagnostic.IsError)
            Interlocked.Increment(ref _errorCount);
        else if (diagnostic.IsWarning)
            Interlocked.Increment(ref _warningCount);

        return true;
    }

    /// <summary>
    ///     Creates and adds a diagnostic.
    /// </summary>
    public bool Add(DiagnosticCode code, DiagnosticSeverity severity, string message,
        SourceLocation location, SourceLocation? endLocation = null)
    {
        string? contextSnippet = null;
        if (SourceText != null && location.IsValid)
        {
            var span = endLocation.HasValue
                ? new TextSpan(location.Offset, endLocation.Value.Offset - location.Offset)
                : new TextSpan(location.Offset, 1);
            contextSnippet = SourceText.GetContextSnippet(span);
        }

        var diagnostic = new Diagnostic(code, severity, message, location, endLocation, contextSnippet);
        return Add(diagnostic);
    }

    /// <summary>
    ///     Adds an error diagnostic.
    /// </summary>
    public bool AddError(DiagnosticCode code, string message, TextSpan span)
    {
        var (start, end) = GetLocations(span);
        return Add(code, DiagnosticSeverity.Error, message, start, end);
    }

    /// <summary>
    ///     Adds an error diagnostic with a formatted message.
    /// </summary>
    public bool AddError(DiagnosticCode code, TextSpan span, params object[] args)
    {
        var message = ErrorCatalog.GetMessage(code, args);
        return AddError(code, message, span);
    }

    /// <summary>
    ///     Adds a warning diagnostic.
    /// </summary>
    public bool AddWarning(DiagnosticCode code, string message, TextSpan span)
    {
        var (start, end) = GetLocations(span);
        return Add(code, DiagnosticSeverity.Warning, message, start, end);
    }

    /// <summary>
    ///     Adds a warning diagnostic with a formatted message.
    /// </summary>
    public bool AddWarning(DiagnosticCode code, TextSpan span, params object[] args)
    {
        var message = ErrorCatalog.GetMessage(code, args);
        return AddWarning(code, message, span);
    }

    /// <summary>
    ///     Adds an info diagnostic.
    /// </summary>
    public bool AddInfo(DiagnosticCode code, string message, TextSpan span)
    {
        var (start, end) = GetLocations(span);
        return Add(code, DiagnosticSeverity.Info, message, start, end);
    }

    /// <summary>
    ///     Adds a hint diagnostic.
    /// </summary>
    public bool AddHint(DiagnosticCode code, string message, TextSpan span)
    {
        var (start, end) = GetLocations(span);
        return Add(code, DiagnosticSeverity.Hint, message, start, end);
    }

    /// <summary>
    ///     Adds all diagnostics from another bag.
    /// </summary>
    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        foreach (var diagnostic in diagnostics)
            if (!Add(diagnostic))
                break;
    }

    /// <summary>
    ///     Adds all diagnostics from another bag.
    /// </summary>
    public void AddRange(DiagnosticBag other)
    {
        ArgumentNullException.ThrowIfNull(other);
        AddRange(other._diagnostics);
    }

    /// <summary>
    ///     Gets all diagnostics sorted by location.
    /// </summary>
    public IReadOnlyList<Diagnostic> ToSortedList()
    {
        var diagnostics = _diagnostics.ToArray();
        var knownContent = diagnostics
            .Where(static diagnostic => diagnostic.Location.IsValid)
            .Select(DiagnosticContentIdentity.Create)
            .ToHashSet();

        var ordered = diagnostics
            .Where(diagnostic => diagnostic.Location.IsValid ||
                                !knownContent.Contains(DiagnosticContentIdentity.Create(diagnostic)))
            .OrderBy(static d => d.SourceKind)
            .ThenBy(static d => d.Location.FilePath, StringComparer.Ordinal)
            .ThenBy(static d => d.Location.Offset < 0 ? int.MaxValue : d.Location.Offset)
            .ThenBy(static d => d.EndLocation.Offset < 0 ? int.MaxValue : d.EndLocation.Offset)
            .ThenByDescending(d => _diagnosticOrder.GetValueOrDefault(DiagnosticIdentity.Create(d), long.MaxValue))
            .ThenBy(static d => d.Phase)
            .ThenBy(d => d.Severity)
            .ThenBy(d => (int)d.Code)
            .ThenBy(d => d.Message, StringComparer.Ordinal)
            .ToList();

        return ordered;
    }

    /// <summary>
    ///     Gets only error diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> GetErrors()
    {
        return _diagnostics.Where(d => d.IsError);
    }

    /// <summary>
    ///     Gets only warning diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> GetWarnings()
    {
        return _diagnostics.Where(d => d.IsWarning);
    }

    /// <summary>
    ///     Clears all diagnostics.
    /// </summary>
    public void Clear()
    {
        while (_diagnostics.TryTake(out _))
        {
        }

        _diagnosticKeys.Clear();
        _diagnosticOrder.Clear();
        Interlocked.Exchange(ref _nextSequence, 0);
        Interlocked.Exchange(ref _errorCount, 0);
        Interlocked.Exchange(ref _warningCount, 0);
    }

    private (SourceLocation Start, SourceLocation End) GetLocations(TextSpan span)
    {
        if (SourceText == null)
            return (new SourceLocation(span.Start, 1, span.Start + 1),
                new SourceLocation(span.End, 1, span.End + 1));

        return SourceText.GetLocations(span);
    }

    private readonly record struct DiagnosticIdentity(
        DiagnosticCode Code,
        DiagnosticSeverity Severity,
        DiagnosticPhase Phase,
        DiagnosticSourceKind SourceKind,
        int Offset,
        int EndOffset,
        int Line,
        int Column,
        string? FilePath,
        string Message)
    {
        public static DiagnosticIdentity Create(Diagnostic diagnostic)
        {
            return new DiagnosticIdentity(
                diagnostic.Code,
                diagnostic.Severity,
                diagnostic.Phase,
                diagnostic.SourceKind,
                diagnostic.Location.Offset,
                diagnostic.EndLocation.Offset,
                diagnostic.Location.Line,
                diagnostic.Location.Column,
                diagnostic.Location.FilePath,
                diagnostic.Message);
        }
    }

    private readonly record struct DiagnosticContentIdentity(
        DiagnosticCode Code,
        DiagnosticSeverity Severity,
        DiagnosticPhase Phase,
        DiagnosticSourceKind SourceKind,
        string Message)
    {
        public static DiagnosticContentIdentity Create(Diagnostic diagnostic)
        {
            return new DiagnosticContentIdentity(
                diagnostic.Code,
                diagnostic.Severity,
                diagnostic.Phase,
                diagnostic.SourceKind,
                diagnostic.Message);
        }
    }
}
