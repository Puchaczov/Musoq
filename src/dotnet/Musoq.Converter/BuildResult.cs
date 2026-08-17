using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter;

/// <summary>
///     Represents the outcome of query compilation with collected diagnostics.
///     This is the preferred result type for the diagnostic-aware compilation path.
/// </summary>
public sealed class BuildResult
{
    private readonly string? _queryText;

    private BuildResult(
        CompiledQuery? compiledQuery,
        IReadOnlyList<Diagnostic> diagnostics,
        string? queryText,
        Exception? caughtException,
        BuildItems? buildItems)
    {
        CompiledQuery = compiledQuery;
        Diagnostics = diagnostics;
        _queryText = queryText;
        CaughtException = caughtException;
        BuildItems = buildItems;
        Errors = diagnostics.Where(d => d.IsError).ToList();
        Warnings = diagnostics.Where(d => d.IsWarning).ToList();
    }

    /// <summary>
    ///     Gets the compiled query, or null if compilation failed.
    /// </summary>
    public CompiledQuery? CompiledQuery { get; }

    /// <summary>
    ///     Gets all diagnostics collected during compilation (errors, warnings, info).
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>
    ///     Returns true if any error diagnostics were collected.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    ///     Returns true if compilation succeeded and a runnable query is available.
    /// </summary>
    [MemberNotNullWhen(true, nameof(CompiledQuery))]
    public bool Succeeded => CompiledQuery != null && !HasErrors;

    /// <summary>
    ///     Gets only error-level diagnostics.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors { get; }

    /// <summary>
    ///     Gets only warning-level diagnostics.
    /// </summary>
    public IReadOnlyList<Diagnostic> Warnings { get; }

    /// <summary>
    ///     Gets the exception caught during compilation, if any.
    ///     Preserved for debugging — used as InnerException in MusoqQueryException.
    /// </summary>
    public Exception? CaughtException { get; }

    internal BuildItems? BuildItems { get; }

    /// <summary>
    ///     Converts error diagnostics into spec-compliant error envelopes.
    /// </summary>
    public IReadOnlyList<MusoqErrorEnvelope> ToEnvelopes()
    {
        return Errors
            .Select(d => MusoqErrorEnvelope.FromDiagnostic(d, _queryText))
            .ToList();
    }

    /// <summary>
    ///     Converts every collected diagnostic, including warnings and informational
    ///     diagnostics, into a spec-compliant envelope. <see cref="ToEnvelopes" />
    ///     remains the error-only operation used by existing exception surfaces.
    /// </summary>
    public IReadOnlyList<MusoqErrorEnvelope> ToAllEnvelopes()
    {
        return Diagnostics
            .Select(diagnostic => MusoqErrorEnvelope.FromDiagnostic(diagnostic, _queryText))
            .ToList();
    }

    /// <summary>
    ///     Creates a successful result with optional warnings/info diagnostics.
    /// </summary>
    internal static BuildResult Success(CompiledQuery query, IReadOnlyList<Diagnostic> diagnostics,
        string? queryText, BuildItems? buildItems = null)
    {
        return new BuildResult(query, diagnostics, queryText, caughtException: null, buildItems);
    }

    /// <summary>
    ///     Creates a failed result with collected error diagnostics.
    /// </summary>
    internal static BuildResult Failure(IReadOnlyList<Diagnostic> diagnostics, string? queryText,
        Exception? caughtException = null,
        BuildItems? buildItems = null)
    {
        return new BuildResult(null, diagnostics, queryText, caughtException, buildItems);
    }
}
