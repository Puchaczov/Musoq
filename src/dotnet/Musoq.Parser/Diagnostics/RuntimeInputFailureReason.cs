namespace Musoq.Parser.Diagnostics;

/// <summary>Controlled public reasons for rejecting runtime input.</summary>
public enum RuntimeInputFailureReason
{
    MalformedInput = 0,
    UnterminatedQuotedField = 1,
    MalformedJson = 2
}
