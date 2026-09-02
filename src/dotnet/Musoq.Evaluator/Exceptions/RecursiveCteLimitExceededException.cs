using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public sealed class RecursiveCteLimitExceededException : InvalidOperationException, IDiagnosticException
{
    public RecursiveCteLimitExceededException(
        string cteName,
        DiagnosticCode code,
        int configuredLimit)
        : base($"{ErrorCatalog.GetMessage(code, configuredLimit)} CTE: '{cteName}'.")
    {
        if (code is not (DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded or
            DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded or
            DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded))
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, "Expected a recursive CTE runtime limit diagnostic.");
        }

        Code = code;
        ConfiguredLimit = configuredLimit;
        CteName = cteName;
    }

    public DiagnosticCode Code { get; }

    public int ConfiguredLimit { get; }

    public string CteName { get; }

    public TextSpan? Span => null;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.Runtime,
            sourceKind: DiagnosticSourceKind.Runtime,
            arguments:
            [
                new KeyValuePair<string, string>("cteName", CteName),
                new KeyValuePair<string, string>(
                    "configuredLimit",
                    ConfiguredLimit.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ]);
    }
}
