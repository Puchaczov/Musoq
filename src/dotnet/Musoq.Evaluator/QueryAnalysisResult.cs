#nullable enable annotations

using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

/// <summary>
///     Result of query analysis containing parsed AST and collected diagnostics.
/// </summary>
public sealed class QueryAnalysisResult
{
    /// <summary>
    ///     Gets the root node of the parsed query, or null if parsing failed completely.
    /// </summary>
    public RootNode? Root { get; init; }

    /// <summary>
    ///     Gets whether the query was successfully parsed (may still have semantic errors).
    /// </summary>
    public bool IsParsed => Root != null;

    /// <summary>
    ///     Gets all collected diagnostics (errors, warnings, and info).
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    ///     Gets only the error-level diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Errors => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets only the warning-level diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Warnings => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Gets whether there are any errors.
    /// </summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets whether the analysis completed successfully (no errors).
    /// </summary>
    public bool IsSuccess => IsParsed && !HasErrors;
}
