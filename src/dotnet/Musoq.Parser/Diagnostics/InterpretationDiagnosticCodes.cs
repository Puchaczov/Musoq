namespace Musoq.Parser.Diagnostics;

/// <summary>Maps the public interpretation error range to its Core diagnostic range.</summary>
public static class InterpretationDiagnosticCodes
{
    public static DiagnosticCode FromCode(int interpretationCode)
    {
        if (interpretationCode is < 1 or > 15)
            interpretationCode = 15;

        return (DiagnosticCode)(7100 + interpretationCode);
    }

    internal static readonly DiagnosticCode[] All =
    [
        DiagnosticCode.MQ7101_InterpretationInsufficientData,
        DiagnosticCode.MQ7102_InterpretationValidationFailed,
        DiagnosticCode.MQ7103_InterpretationPatternMismatch,
        DiagnosticCode.MQ7104_InterpretationLiteralMismatch,
        DiagnosticCode.MQ7105_InterpretationDelimiterNotFound,
        DiagnosticCode.MQ7106_InterpretationExpectedDelimiter,
        DiagnosticCode.MQ7107_InterpretationInvalidSize,
        DiagnosticCode.MQ7108_InterpretationInvalidPosition,
        DiagnosticCode.MQ7109_InterpretationMaxIterationsExceeded,
        DiagnosticCode.MQ7110_InterpretationEncodingError,
        DiagnosticCode.MQ7111_InterpretationExpectedWhitespace,
        DiagnosticCode.MQ7112_InterpretationNoAlternativeMatched,
        DiagnosticCode.MQ7113_InterpretationInvalidSchemaReference,
        DiagnosticCode.MQ7114_InterpretationFieldReferenceError,
        DiagnosticCode.MQ7115_InterpretationGeneralError
    ];
}
