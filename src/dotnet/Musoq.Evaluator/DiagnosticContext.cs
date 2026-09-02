using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

/// <summary>
///     Context for collecting diagnostics during visitor-based analysis.
///     Thread-safe and supports hierarchical scoping for nested analysis.
/// </summary>
public sealed class DiagnosticContext
{
    private const int CandidateLimit = 5;
    private static readonly string[] KnownTypeNames =
    [
        "Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
        "Single", "Double", "Decimal", "Char", "String", "DateTime", "DateTimeOffset", "TimeSpan", "Guid",
        "Object", "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float",
        "double", "decimal", "char", "string", "datetime", "datetimeoffset", "timespan", "guid", "object"
    ];
    private readonly DiagnosticBag _diagnostics;
    private readonly Lock _lock = new();
    private readonly Stack<string> _scopeStack;

    /// <summary>
    ///     Creates a new DiagnosticContext.
    /// </summary>
    public DiagnosticContext(SourceText? sourceText = null, int maxErrors = 100)
    {
        SourceText = sourceText;
        _diagnostics = new DiagnosticBag { MaxErrors = maxErrors, SourceText = sourceText };
        _scopeStack = new Stack<string>();
    }

    /// <summary>
    ///     Gets the source text being analyzed.
    /// </summary>
    public SourceText? SourceText { get; }

    /// <summary>
    ///     Gets all collected diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => _diagnostics.ToSortedList();

    /// <summary>
    ///     Gets only error diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Errors => _diagnostics.GetErrors();

    /// <summary>
    ///     Gets only warning diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Warnings => _diagnostics.GetWarnings();

    /// <summary>
    ///     Returns true if there are any errors.
    /// </summary>
    public bool HasErrors => _diagnostics.HasErrors;

    /// <summary>
    ///     Returns true if the max error limit has been reached.
    /// </summary>
    public bool HasReachedMaxErrors => _diagnostics.HasTooManyErrors;

    internal bool HasNearbyError(DiagnosticCode code, TextSpan span, int distance = 1)
    {
        if (span.IsEmpty)
            return false;

        return Errors.Any(diagnostic =>
            diagnostic.Code == code &&
            diagnostic.Location.IsValid &&
            (diagnostic.Span == span ||
             (diagnostic.Span.End <= span.Start &&
              span.Start - diagnostic.Span.End <= distance)));
    }

    /// <summary>
    ///     Gets the current scope path (for error context).
    /// </summary>
    public string CurrentScope
    {
        get
        {
            lock (_lock)
            {
                return _scopeStack.Count > 0 ? string.Join(".", _scopeStack.Reverse()) : string.Empty;
            }
        }
    }

    /// <summary>
    ///     Enters a named scope for better error context.
    /// </summary>
    public IDisposable EnterScope(string name)
    {
        lock (_lock)
        {
            _scopeStack.Push(name);
        }

        return new ScopeGuard(this);
    }

    private void ExitScope()
    {
        lock (_lock)
        {
            if (_scopeStack.Count > 0)
                _scopeStack.Pop();
        }
    }

    /// <summary>
    ///     Reports an error diagnostic.
    /// </summary>
    public void ReportError(DiagnosticCode code, string message, TextSpan span)
    {
        _diagnostics.AddError(code, message, span);
    }

    /// <summary>
    ///     Reports an error diagnostic from a node.
    /// </summary>
    public void ReportError(DiagnosticCode code, string message, Node? node)
    {
        if (node is null || !node.HasSpan)
        {
            _diagnostics.Add(new Diagnostic(
                code,
                DiagnosticSeverity.Error,
                message,
                SourceLocation.None,
                SourceLocation.None));
            return;
        }

        ReportError(code, message, node.Span);
    }

    /// <summary>
    ///     Reports a warning diagnostic.
    /// </summary>
    public void ReportWarning(DiagnosticCode code, string message, TextSpan span)
    {
        _diagnostics.Add(CreateWarningDiagnostic(code, message, span));
    }

    /// <summary>
    ///     Reports a warning diagnostic from a node.
    /// </summary>
    public void ReportWarning(DiagnosticCode code, string message, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.HasSpan)
        {
            _diagnostics.Add(CreateWarningDiagnostic(code, message));
            return;
        }

        ReportWarning(code, message, node.Span);
    }

    private Diagnostic CreateWarningDiagnostic(DiagnosticCode code, string message, TextSpan? span = null)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var suggestedFixes = metadata?.SuggestedFixes
            .Select(DiagnosticAction.Suggestion)
            .ToArray();

        if (!span.HasValue)
        {
            return new Diagnostic(
                code,
                DiagnosticSeverity.Warning,
                message,
                SourceLocation.None,
                SourceLocation.None,
                suggestedFixes: suggestedFixes,
                explanation: metadata?.Explanation,
                docsReference: metadata?.DocsReference,
                phase: metadata?.Phase);
        }

        var warningSpan = span.Value;
        var locations = SourceText != null
            ? SourceText.GetLocations(warningSpan)
            : (Start: new SourceLocation(warningSpan.Start, 1, warningSpan.Start + 1),
                End: new SourceLocation(warningSpan.End, 1, warningSpan.End + 1));
        var contextSnippet = SourceText != null && locations.Start.IsValid
            ? SourceText.GetContextSnippet(warningSpan)
            : null;

        return new Diagnostic(
            code,
            DiagnosticSeverity.Warning,
            message,
            locations.Start,
            locations.End,
            contextSnippet,
            suggestedFixes: suggestedFixes,
            explanation: metadata?.Explanation,
            docsReference: metadata?.DocsReference,
            phase: metadata?.Phase);
    }

    /// <summary>
    ///     Reports an info diagnostic.
    /// </summary>
    public void ReportInfo(DiagnosticCode code, string message, TextSpan span)
    {
        _diagnostics.AddInfo(code, message, span);
    }

    /// <summary>
    ///     Reports a hint diagnostic.
    /// </summary>
    public void ReportHint(DiagnosticCode code, string message, TextSpan span)
    {
        _diagnostics.AddHint(code, message, span);
    }

    /// <summary>
    ///     Imports diagnostics collected elsewhere into this context.
    /// </summary>
    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        _diagnostics.AddRange(diagnostics);
    }

    /// <summary>
    ///     Reports a diagnostic from an exception.
    /// </summary>
    public void ReportException(Exception exception, TextSpan? span = null)
    {
        var diagnostic = exception.ToDiagnosticOrGeneric(SourceText);
        var effectiveSpan = span ?? diagnostic.Span;

        if (effectiveSpan is { IsEmpty: false } &&
            HasNearbyError(diagnostic.Code, effectiveSpan))
            return;

        if (!span.HasValue)
        {
            if (!diagnostic.Location.IsValid && !diagnostic.EndLocation.IsValid)
                diagnostic = diagnostic.WithLocations(SourceLocation.None, SourceLocation.None);

            _diagnostics.Add(diagnostic);
            return;
        }

        var actualSpan = span.Value;
        _diagnostics.Add(diagnostic.WithSourceContext(SourceText, actualSpan));
    }

    /// <summary>
    ///     Reports an unknown alias error with suggestions.
    /// </summary>
    public void ReportUnknownAlias(string alias, IEnumerable<string> availableAliases, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(availableAliases);

        var candidates = availableAliases
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static candidate => candidate, StringComparer.Ordinal)
            .ToArray();
        var span = GetAliasSpan(alias, node);
        var message = $"Unknown alias '{alias}'.";

        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(alias, candidates);
        var suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
        message = AppendCandidateGuidance(message, closeCandidates, suggestion);

        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["alias"] = alias,
            ["availableAliases"] = string.Join(", ", candidates)
        };
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null;
        if (!string.IsNullOrEmpty(suggestion))
        {
            facts["suggestion"] = suggestion;

            // A text replacement is safe only when there is one visible
            // candidate. The catalog intentionally keeps its suggestion
            // deterministic, but a tie between aliases must not become an
            // arbitrary automatic edit.
            if (candidates.Length == 1)
                suggestedFixes =
                [
                    DiagnosticAction.QuickFix(
                        $"Replace '{alias}' with '{suggestion}'",
                        span,
                        suggestion)
                ];
        }

        _diagnostics.Add(SemanticDiagnosticFactory.Create(
            DiagnosticCode.MQ3015_UnknownAlias,
            message,
            span,
            SourceText,
            facts,
            suggestedFixes));
    }

    /// <summary>
    ///     Reports an unknown column error with suggestions.
    /// </summary>
    public void ReportUnknownColumn(string columnName, IEnumerable<string> availableColumns, Node? node)
    {
        ReportUnknownColumn(columnName, availableColumns, node.SpanOrEmpty());
    }

    /// <summary>
    ///     Reports an unknown column error at an explicit source span.
    /// </summary>
    public void ReportUnknownColumn(string columnName, IEnumerable<string> availableColumns, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(availableColumns);

        var candidates = NormalizeCandidates(availableColumns);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(columnName, candidates);
        var suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
        var message = AppendCandidateGuidance($"Unknown column '{columnName}'.", closeCandidates, suggestion);
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["column"] = columnName,
            ["availableColumns"] = string.Join(", ", GetDisplayCandidates(candidates, closeCandidates))
        };
        if (closeCandidates.Count > 0)
            facts["candidateColumns"] = string.Join(", ", closeCandidates);
        if (suggestion != null)
            facts["suggestion"] = suggestion;

        var replacementSpan = GetIdentifierSpan(columnName, span);
        var suggestedFixes = CreateSafeReplacement(columnName, suggestion, replacementSpan);
        _diagnostics.Add(SemanticDiagnosticFactory.Create(
            DiagnosticCode.MQ3001_UnknownColumn,
            message,
            span,
            SourceText,
            facts,
            suggestedFixes));
    }

    /// <summary>
    ///     Reports an unknown property error with suggestions.
    /// </summary>
    public void ReportUnknownProperty(
        string propertyName,
        IEnumerable<string> availableProperties,
        Node? node,
        string? objectTypeName = null,
        string? accessContext = null)
    {
        ArgumentNullException.ThrowIfNull(availableProperties);
        var span = node.SpanOrEmpty();
        var candidates = NormalizeCandidates(availableProperties);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(propertyName, candidates);
        var suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
        var message = $"Unknown property '{propertyName}'" +
                      (string.IsNullOrWhiteSpace(objectTypeName) ? "." : $" on '{objectTypeName}'.");
        if (!string.IsNullOrWhiteSpace(accessContext))
            message += $" Accessed through '{accessContext}'.";
        message = AppendCandidateGuidance(message, closeCandidates, suggestion);

        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["property"] = propertyName,
            ["availableProperties"] = string.Join(", ", GetDisplayCandidates(candidates, closeCandidates))
        };
        if (!string.IsNullOrWhiteSpace(objectTypeName))
            facts["objectType"] = objectTypeName;
        if (!string.IsNullOrWhiteSpace(accessContext))
            facts["accessContext"] = accessContext;
        if (closeCandidates.Count > 0)
            facts["candidateProperties"] = string.Join(", ", closeCandidates);
        if (suggestion != null)
            facts["suggestion"] = suggestion;

        var replacementSpan = GetIdentifierSpan(propertyName, span);
        var suggestedFixes = CreateSafeReplacement(propertyName, suggestion, replacementSpan);
        _diagnostics.Add(SemanticDiagnosticFactory.Create(
            DiagnosticCode.MQ3028_UnknownProperty,
            message,
            span,
            SourceText,
            facts,
            suggestedFixes));
    }

    /// <summary>
    ///     Reports an unknown function error with suggestions.
    /// </summary>
    public void ReportUnknownFunction(string functionName, IEnumerable<string> availableFunctions, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(availableFunctions);
        var span = node.SpanOrEmpty();
        var candidates = NormalizeCandidates(availableFunctions);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(functionName, candidates);
        var suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
        var message = AppendCandidateGuidance($"Unknown function '{functionName}'.", closeCandidates, suggestion);
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["callable"] = functionName,
            ["availableCallables"] = string.Join(", ", GetDisplayCandidates(candidates, closeCandidates))
        };
        if (closeCandidates.Count > 0)
            facts["candidateCallables"] = string.Join(", ", closeCandidates);
        if (suggestion != null)
            facts["suggestion"] = suggestion;

        var replacementSpan = GetIdentifierSpan(functionName, span);
        var suggestedFixes = CreateSafeReplacement(functionName, suggestion, replacementSpan);
        _diagnostics.Add(SemanticDiagnosticFactory.Create(
            DiagnosticCode.MQ3086_UnknownCallable,
            message,
            span,
            SourceText,
            facts,
            suggestedFixes));
    }

    /// <summary>
    ///     Reports a type mismatch error.
    /// </summary>
    public void ReportTypeMismatch(string expected, string actual, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var span = node.SpanOrEmpty();
        var message = $"Type mismatch: expected '{expected}' but got '{actual}'.";
        ReportError(DiagnosticCode.MQ3005_TypeMismatch, message, span);
    }

    /// <summary>
    ///     Reports a type name that could not be resolved with canonical type
    ///     candidates and a safe replacement when the spelling is unambiguous.
    /// </summary>
    public void ReportTypeNotFound(string typeName, Node? node)
    {
        ReportTypeNotFound(typeName, node.SpanOrEmpty());
    }

    /// <summary>
    ///     Reports a type name that could not be resolved at an explicit span.
    /// </summary>
    public void ReportTypeNotFound(string typeName, TextSpan span)
    {
        var candidates = NormalizeCandidates(KnownTypeNames);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(typeName, candidates);
        var suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
        var message = AppendCandidateGuidance(
            $"Type '{typeName}' could not be found or resolved.",
            closeCandidates,
            suggestion);
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["type"] = typeName,
            ["candidateTypes"] = string.Join(", ", closeCandidates)
        };
        if (suggestion != null)
            facts["suggestion"] = suggestion;

        var replacementSpan = GetIdentifierSpan(typeName, span);
        var suggestedFixes = CreateSafeReplacement(typeName, suggestion, replacementSpan);
        _diagnostics.Add(SemanticDiagnosticFactory.Create(
            DiagnosticCode.MQ3005_TypeMismatch,
            message,
            span,
            SourceText,
            facts,
            suggestedFixes));
    }

    /// <summary>
    ///     Reports an ambiguous column reference.
    /// </summary>
    public void ReportAmbiguousColumn(string columnName, string alias1, string alias2, Node? node)
    {
        var span = node.SpanOrEmpty();
        var message = $"Ambiguous column name '{columnName}' between '{alias1}' and '{alias2}'.";
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["column"] = columnName,
            ["aliases"] = $"{alias1}, {alias2}"
        };
        _diagnostics.Add(SemanticDiagnosticFactory.Create(
            DiagnosticCode.MQ3002_AmbiguousColumn,
            message,
            span,
            SourceText,
            facts));
    }

    /// <summary>
    ///     Reports an ambiguous aggregate owner error.
    /// </summary>
    public void ReportAmbiguousAggregateOwner(string methodCall, IEnumerable<string> candidateAliases, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var span = node.SpanOrEmpty();
        var aliases = string.Join(", ", candidateAliases.Select(alias => $"'{alias}'"));
        var message = $"Aggregate call '{methodCall}' is ambiguous because multiple source aliases expose different implementations: {aliases}.";
        ReportError(DiagnosticCode.MQ3034_AmbiguousAggregateOwner, message, span);
    }

    /// <summary>
    ///     Reports an ambiguous method owner error.
    /// </summary>
    public void ReportAmbiguousMethodOwner(string methodCall, IEnumerable<string> candidateAliases, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var span = node.SpanOrEmpty();
        var aliases = string.Join(", ", candidateAliases.Select(alias => $"'{alias}'"));
        var message = $"Method call '{methodCall}' is ambiguous because multiple source aliases expose different implementations: {aliases}.";
        ReportError(DiagnosticCode.MQ3035_AmbiguousMethodOwner, message, span);
    }

    /// <summary>
    ///     Reports an invalid argument count.
    /// </summary>
    public void ReportInvalidArgumentCount(string functionName, int expected, int actual, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var span = node.SpanOrEmpty();
        var message = $"Function '{functionName}' expects {expected} argument(s) but got {actual}.";
        ReportError(DiagnosticCode.MQ3087_InvalidCallableArity, message, span);
    }

    /// <summary>
    ///     Clears all diagnostics.
    /// </summary>
    public void Clear()
    {
        _diagnostics.Clear();
        lock (_lock)
        {
            _scopeStack.Clear();
        }
    }

    /// <summary>
    ///     Creates a SemanticAnalysisResult from the current state.
    /// </summary>
    public SemanticAnalysisResult ToResult(Node rootNode)
    {
        return new SemanticAnalysisResult(rootNode, _diagnostics.ToSortedList());
    }

    private static string AppendCandidateGuidance(
        string message,
        IReadOnlyList<string> closeCandidates,
        string? suggestion)
    {
        if (!string.IsNullOrEmpty(suggestion))
            return $"{message} Did you mean '{suggestion}'?";

        return closeCandidates.Count > 1
            ? $"{message} Possible matches: {FormatCandidates(closeCandidates)}."
            : message;
    }

    private static string[] NormalizeCandidates(IEnumerable<string> candidates)
    {
        var canonicalCandidates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (!canonicalCandidates.TryGetValue(candidate, out var existing) ||
                string.CompareOrdinal(candidate, existing) < 0)
                canonicalCandidates[candidate] = candidate;
        }

        return canonicalCandidates.Values
            .OrderBy(static candidate => candidate, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GetDisplayCandidates(
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> closeCandidates)
    {
        var displayCandidates = new List<string>(CandidateLimit);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in closeCandidates.Concat(candidates))
        {
            if (!seen.Add(candidate))
                continue;

            displayCandidates.Add(candidate);
            if (displayCandidates.Count == CandidateLimit)
                break;
        }

        return displayCandidates.ToArray();
    }

    private static string FormatCandidates(IEnumerable<string> candidates)
    {
        return string.Join(", ", candidates.Select(static candidate => $"'{candidate}'"));
    }

    private static IReadOnlyList<DiagnosticAction>? CreateSafeReplacement(
        string original,
        string? suggestion,
        TextSpan span)
    {
        if (string.IsNullOrWhiteSpace(suggestion) || span.IsEmpty || span.Length != original.Length)
            return null;

        return
        [
            DiagnosticAction.QuickFix(
                $"Replace '{original}' with '{suggestion}'",
                span,
                suggestion)
        ];
    }

    private TextSpan GetAliasSpan(string alias, Node node)
    {
        var nodeSpan = node.SpanOrEmpty();
        if (SourceText == null || string.IsNullOrWhiteSpace(alias) || nodeSpan.IsEmpty)
            return nodeSpan;

        var start = Math.Max(0, nodeSpan.Start);
        var end = Math.Min(SourceText.Length, nodeSpan.End);
        if (start >= end)
            return nodeSpan;

        var index = SourceText.Text.IndexOf(
            alias,
            start,
            end - start,
            StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? new TextSpan(index, alias.Length) : nodeSpan;
    }

    private TextSpan GetIdentifierSpan(string identifier, TextSpan nodeSpan)
    {
        if (SourceText == null || string.IsNullOrWhiteSpace(identifier) || nodeSpan.IsEmpty)
            return nodeSpan;

        var start = Math.Max(0, nodeSpan.Start);
        var end = Math.Min(SourceText.Length, nodeSpan.End);
        if (start >= end)
            return nodeSpan;

        var index = SourceText.Text.IndexOf(
            identifier,
            start,
            end - start,
            StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? new TextSpan(index, identifier.Length) : nodeSpan;
    }

    private sealed class ScopeGuard(DiagnosticContext context) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                context.ExitScope();
                _disposed = true;
            }
        }
    }
}
