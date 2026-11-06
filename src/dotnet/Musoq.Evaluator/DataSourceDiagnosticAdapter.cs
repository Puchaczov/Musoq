using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator;

internal static class DataSourceDiagnosticAdapter
{
    public static bool TryAdapt(Exception exception, out Exception diagnostic)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var visited = new HashSet<Exception>();
        for (var current = exception; current != null && visited.Add(current); current = current.InnerException)
        {
            if (current is AggregateException) break;
            if (current is Musoq.Schema.Exceptions.DataSourceLifecycleException { Code: Musoq.Parser.Diagnostics.DiagnosticCode.MQ7020_ReportedDataSourceFailure } lifecycle)
            {
                diagnostic = lifecycle;
                return true;
            }
            if (current is DataSourceException reported)
            {
                diagnostic = reported;
                return true;
            }
            if (current is not DataSourceInputException inputFailure)
                continue;

            diagnostic = new RuntimeInputDiagnosticException(
                MapReason(inputFailure.Reason),
                DiagnosticSourceKind.DataSource,
                MapLocation(inputFailure.Location),
                inputFailure);
            return true;
        }

        diagnostic = null!;
        return false;
    }

    public static bool ContainsRecognizedFailure(Exception exception)
    {
        return TryAdapt(exception, out _);
    }

    private static RuntimeInputFailureReason MapReason(DataSourceInputFailureReason reason)
    {
        return reason switch
        {
            DataSourceInputFailureReason.MalformedInput => RuntimeInputFailureReason.MalformedInput,
            DataSourceInputFailureReason.UnterminatedQuotedField => RuntimeInputFailureReason.UnterminatedQuotedField,
            DataSourceInputFailureReason.MalformedJson => RuntimeInputFailureReason.MalformedJson,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown datasource input failure reason.")
        };
    }

    private static DiagnosticInputLocation? MapLocation(DataSourceInputLocation? location)
    {
        return location is null
            ? null
            : new DiagnosticInputLocation(
                location.Line,
                location.Column,
                location.ByteOffset,
                location.CharacterOffset,
                location.BytePositionInLine);
    }
}
