using System.IO;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Diagnostics;

/// <summary>
/// Describes rejected datasource input without exposing provider-controlled text.
/// </summary>
public sealed class DataSourceInputException : IOException
{
    public DataSourceInputException(
        DataSourceInputFailureReason reason,
        DataSourceInputLocation? location = null,
        Exception? innerException = null)
        : base(GetSafeMessage(reason), innerException)
    {
        _ = GetParserReason(reason);
        Reason = reason;
        Location = location;
    }

    public DataSourceInputFailureReason Reason { get; }

    public DataSourceInputLocation? Location { get; }

    internal RuntimeInputDiagnosticException ToParserDiagnosticException()
    {
        return new RuntimeInputDiagnosticException(
            GetParserReason(Reason),
            DiagnosticSourceKind.DataSource,
            Location is null
                ? null
                : new DiagnosticInputLocation(
                    Location.Line,
                    Location.Column,
                    Location.ByteOffset,
                    Location.CharacterOffset,
                    Location.BytePositionInLine),
            InnerException);
    }

    private static string GetSafeMessage(DataSourceInputFailureReason reason)
    {
        return reason switch
        {
            DataSourceInputFailureReason.MalformedInput => ErrorCatalog.GetMessage(DiagnosticCode.MQ7014_MalformedRuntimeInput),
            DataSourceInputFailureReason.UnterminatedQuotedField => "The final quoted field is not terminated.",
            DataSourceInputFailureReason.MalformedJson => "Runtime JSON input is malformed.",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown datasource input failure reason.")
        };
    }

    private static RuntimeInputFailureReason GetParserReason(DataSourceInputFailureReason reason)
    {
        return reason switch
        {
            DataSourceInputFailureReason.MalformedInput => RuntimeInputFailureReason.MalformedInput,
            DataSourceInputFailureReason.UnterminatedQuotedField => RuntimeInputFailureReason.UnterminatedQuotedField,
            DataSourceInputFailureReason.MalformedJson => RuntimeInputFailureReason.MalformedJson,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown datasource input failure reason.")
        };
    }
}
