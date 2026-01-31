#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

/// <summary>
///     Represents the result of semantic analysis on a parsed query.
///     Contains the analyzed AST along with any semantic errors or warnings.
/// </summary>
public sealed class SemanticAnalysisResult
{
    private readonly List<Diagnostic> _diagnostics;

    /// <summary>
    ///     Creates a new SemanticAnalysisResult.
    /// </summary>
    public SemanticAnalysisResult(
        Node rootNode,
        IEnumerable<Diagnostic>? diagnostics = null)
    {
        RootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
        _diagnostics = diagnostics?.ToList() ?? new List<Diagnostic>();
    }

    /// <summary>
    ///     Gets the root AST node after semantic analysis.
    /// </summary>
    public Node RootNode { get; }

    /// <summary>
    ///     Gets all diagnostics from semantic analysis.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>
    ///     Gets only error diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Errors => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets only warning diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Warnings => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Returns true if analysis completed successfully with no errors.
    /// </summary>
    public bool Success => !HasErrors;

    /// <summary>
    ///     Returns true if there are any errors.
    /// </summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Returns true if there are any warnings.
    /// </summary>
    public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Gets the total count of errors.
    /// </summary>
    public int ErrorCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets the total count of warnings.
    /// </summary>
    public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Adds a diagnostic to this result.
    /// </summary>
    internal void AddDiagnostic(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    /// <summary>
    ///     Adds multiple diagnostics to this result.
    /// </summary>
    internal void AddDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        _diagnostics.AddRange(diagnostics);
    }

    /// <summary>
    ///     Throws an exception if there are any errors.
    /// </summary>
    public void ThrowIfErrors()
    {
        if (HasErrors)
        {
            var firstError = Errors.First();
            throw new SemanticAnalysisException(
                $"Semantic analysis failed with {ErrorCount} error(s): {firstError.Message}",
                firstError);
        }
    }

    /// <summary>
    ///     Gets diagnostics at a specific location.
    /// </summary>
    public IEnumerable<Diagnostic> GetDiagnosticsAt(int offset)
    {
        return _diagnostics.Where(d => d.Span.Contains(offset));
    }

    /// <summary>
    ///     Gets diagnostics overlapping a span.
    /// </summary>
    public IEnumerable<Diagnostic> GetDiagnosticsIn(TextSpan span)
    {
        return _diagnostics.Where(d => d.Span.Overlaps(span));
    }

    /// <summary>
    ///     Creates a failed result with the given diagnostics.
    /// </summary>
    public static SemanticAnalysisResult Failed(Node rootNode, params Diagnostic[] diagnostics)
    {
        return new SemanticAnalysisResult(rootNode, diagnostics);
    }

    /// <summary>
    ///     Creates a failed result from an exception.
    /// </summary>
    public static SemanticAnalysisResult FromException(Node rootNode, Exception exception)
    {
        var diagnostic = exception.ToDiagnosticOrGeneric();
        return new SemanticAnalysisResult(rootNode, new[] { diagnostic });
    }
}

/// <summary>
///     Exception thrown when semantic analysis encounters unrecoverable errors.
/// </summary>
public sealed class SemanticAnalysisException : Exception
{
    /// <summary>
    ///     Creates a new SemanticAnalysisException.
    /// </summary>
    public SemanticAnalysisException(string message, Diagnostic primaryDiagnostic)
        : base(message)
    {
        PrimaryDiagnostic = primaryDiagnostic;
    }

    /// <summary>
    ///     Creates a new SemanticAnalysisException with an inner exception.
    /// </summary>
    public SemanticAnalysisException(string message, Diagnostic primaryDiagnostic, Exception innerException)
        : base(message, innerException)
    {
        PrimaryDiagnostic = primaryDiagnostic;
    }

    /// <summary>
    ///     Gets the primary diagnostic that caused this exception.
    /// </summary>
    public Diagnostic PrimaryDiagnostic { get; }

    /// <summary>
    ///     Gets the diagnostic code.
    /// </summary>
    public DiagnosticCode Code => PrimaryDiagnostic.Code;

    /// <summary>
    ///     Gets the source location of the error.
    /// </summary>
    public SourceLocation Location => PrimaryDiagnostic.Location;
}
