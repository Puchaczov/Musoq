using System;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator;

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
