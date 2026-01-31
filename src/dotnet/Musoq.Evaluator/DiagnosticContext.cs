#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly DiagnosticBag _diagnostics;
    private readonly object _lock = new();
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

    /// <summary>
    ///     Gets the current scope path (for error context).
    /// </summary>
    public string CurrentScope
    {
        get
        {
            lock (_lock)
            {
                return _scopeStack.Count > 0 ? string.Join(".", _scopeStack.Reverse()) : "";
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
    public void ReportError(DiagnosticCode code, string message, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        ReportError(code, message, span);
    }

    /// <summary>
    ///     Reports a warning diagnostic.
    /// </summary>
    public void ReportWarning(DiagnosticCode code, string message, TextSpan span)
    {
        _diagnostics.AddWarning(code, message, span);
    }

    /// <summary>
    ///     Reports a warning diagnostic from a node.
    /// </summary>
    public void ReportWarning(DiagnosticCode code, string message, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        ReportWarning(code, message, span);
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
    ///     Reports a diagnostic from an exception.
    /// </summary>
    public void ReportException(Exception exception, TextSpan? span = null)
    {
        var diagnostic = exception.ToDiagnosticOrGeneric(SourceText);
        var actualSpan = span ?? diagnostic.Span;
        _diagnostics.AddError(diagnostic.Code, exception.Message, actualSpan);
    }

    /// <summary>
    ///     Reports an unknown column error with suggestions.
    /// </summary>
    public void ReportUnknownColumn(string columnName, IEnumerable<string> availableColumns, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        var message = $"Unknown column '{columnName}'.";

        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(columnName, availableColumns);
        if (!string.IsNullOrEmpty(suggestion)) message += $" {suggestion}";

        ReportError(DiagnosticCode.MQ3001_UnknownColumn, message, span);
    }

    /// <summary>
    ///     Reports an unknown function error with suggestions.
    /// </summary>
    public void ReportUnknownFunction(string functionName, IEnumerable<string> availableFunctions, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        var message = $"Unknown function '{functionName}'.";

        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(functionName, availableFunctions);
        if (!string.IsNullOrEmpty(suggestion)) message += $" {suggestion}";

        ReportError(DiagnosticCode.MQ3004_UnknownFunction, message, span);
    }

    /// <summary>
    ///     Reports a type mismatch error.
    /// </summary>
    public void ReportTypeMismatch(string expected, string actual, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        var message = $"Type mismatch: expected '{expected}' but got '{actual}'.";
        ReportError(DiagnosticCode.MQ3005_TypeMismatch, message, span);
    }

    /// <summary>
    ///     Reports an ambiguous column reference.
    /// </summary>
    public void ReportAmbiguousColumn(string columnName, string alias1, string alias2, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        var message = $"Ambiguous column name '{columnName}' between '{alias1}' and '{alias2}'.";
        ReportError(DiagnosticCode.MQ3002_AmbiguousColumn, message, span);
    }

    /// <summary>
    ///     Reports an invalid argument count.
    /// </summary>
    public void ReportInvalidArgumentCount(string functionName, int expected, int actual, Node node)
    {
        var span = node.HasSpan ? node.Span : TextSpan.Empty;
        var message = $"Function '{functionName}' expects {expected} argument(s) but got {actual}.";
        ReportError(DiagnosticCode.MQ3006_InvalidArgumentCount, message, span);
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

    private sealed class ScopeGuard : IDisposable
    {
        private readonly DiagnosticContext _context;
        private bool _disposed;

        public ScopeGuard(DiagnosticContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _context.ExitScope();
                _disposed = true;
            }
        }
    }
}
