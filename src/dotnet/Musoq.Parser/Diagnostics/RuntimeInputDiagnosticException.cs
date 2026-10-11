using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
/// Describes rejected runtime input without exposing provider-owned text.
/// </summary>
public sealed class RuntimeInputDiagnosticException : Exception, IDiagnosticException
{
    public RuntimeInputDiagnosticException(
        RuntimeInputFailureReason reason,
        DiagnosticSourceKind sourceKind = DiagnosticSourceKind.Runtime,
        DiagnosticInputLocation? inputLocation = null,
        Exception? innerException = null)
        : base(GetSafeMessage(reason), innerException)
    {
        if (sourceKind is not DiagnosticSourceKind.Runtime and not DiagnosticSourceKind.DataSource)
            throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Runtime input diagnostics must originate from runtime or data-source input.");

        Reason = reason;
        SourceKind = sourceKind;
        InputLocation = inputLocation;
    }

    public RuntimeInputFailureReason Reason { get; }

    public DiagnosticSourceKind SourceKind { get; }

    public DiagnosticInputLocation? InputLocation { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ7014_MalformedRuntimeInput;

    public TextSpan? Span => null;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            GetSafeMessage(Reason),
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.Runtime,
            sourceKind: SourceKind,
            arguments:
            [
                new KeyValuePair<string, string>("inputReason", GetReasonCode(Reason))
            ]).WithInputLocation(InputLocation);
    }

    /// <summary>Returns the fixed public summary associated with a controlled input failure reason.</summary>
    public static string GetSafeMessage(RuntimeInputFailureReason reason)
    {
        return reason switch
        {
            RuntimeInputFailureReason.MalformedInput => ErrorCatalog.GetMessage(DiagnosticCode.MQ7014_MalformedRuntimeInput),
            RuntimeInputFailureReason.UnterminatedQuotedField => "The final quoted field is not terminated.",
            RuntimeInputFailureReason.MalformedJson => "Runtime JSON input is malformed.",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown runtime input failure reason.")
        };
    }

    /// <summary>Returns the stable argument value associated with a controlled input failure reason.</summary>
    public static string GetReasonCode(RuntimeInputFailureReason reason)
    {
        return reason switch
        {
            RuntimeInputFailureReason.MalformedInput => "malformed-input",
            RuntimeInputFailureReason.UnterminatedQuotedField => "unterminated-quoted-field",
            RuntimeInputFailureReason.MalformedJson => "malformed-json",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown runtime input failure reason.")
        };
    }
}
