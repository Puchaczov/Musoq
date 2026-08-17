namespace Musoq.Parser.Diagnostics;

/// <summary>
///     A typed secondary location associated with a diagnostic.
/// </summary>
public sealed class DiagnosticRelatedLocation
{
    /// <summary>
    ///     Creates a related location.
    /// </summary>
    public DiagnosticRelatedLocation(
        SourceLocation location,
        SourceLocation? endLocation = null,
        string? message = null,
        DiagnosticSourceKind sourceKind = DiagnosticSourceKind.Query)
    {
        Location = location;
        EndLocation = endLocation ?? location;
        Message = message;
        SourceKind = sourceKind;
    }

    /// <summary>Gets the start of the related location.</summary>
    public SourceLocation Location { get; }

    /// <summary>Gets the end of the related location.</summary>
    public SourceLocation EndLocation { get; }

    /// <summary>Gets the optional explanation for the related location.</summary>
    public string? Message { get; }

    /// <summary>Gets the source domain containing the related location.</summary>
    public DiagnosticSourceKind SourceKind { get; }
}
