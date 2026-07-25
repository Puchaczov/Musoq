using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator;

/// <summary>
///     Exception thrown when semantic analysis encounters unrecoverable errors.
/// </summary>
public sealed class SemanticAnalysisException : Exception, IDiagnosticException
{
    public SemanticAnalysisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SemanticAnalysisException(string message)
        : base(message)
    {
    }

    public SemanticAnalysisException()
    {
    }
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
    public Diagnostic PrimaryDiagnostic { get; } = Diagnostic.Error(
        DiagnosticCode.MQ3001_UnknownColumn,
        string.Empty,
        TextSpan.Empty);

    /// <summary>
    ///     Gets the diagnostic code.
    /// </summary>
    public DiagnosticCode Code => PrimaryDiagnostic.Code;

    public TextSpan? Span => PrimaryDiagnostic.Span;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return PrimaryDiagnostic;
    }

    /// <summary>
    ///     Gets the source location of the error.
    /// </summary>
    public SourceLocation Location => PrimaryDiagnostic.Location;
}
