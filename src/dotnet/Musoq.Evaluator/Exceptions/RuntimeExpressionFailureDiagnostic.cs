using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal static class RuntimeExpressionFailureDiagnostic
{
    public static Diagnostic? Create(Exception exception)
    {
        if (exception is TargetInvocationException { InnerException: { } inner })
            return Create(inner);

        if (exception is AggregateException aggregate)
        {
            var diagnostics = aggregate.Flatten().InnerExceptions.Select(Create).ToArray();
            if (diagnostics.Length == 0 || diagnostics.Any(static diagnostic => diagnostic == null))
                return null;
            var first = diagnostics[0]!;
            return diagnostics.All(diagnostic => diagnostic!.Code == first.Code) ? first : null;
        }

        if (exception is RegexExecutionTimeoutException timeout)
            return CreateRegexTimeout(timeout.PatternOperator, timeout.Engine, timeout.MatchTimeout);

        if (exception is RegexMatchTimeoutException rawTimeout)
            return CreateRegexTimeout("Unknown", "Unknown", rawTimeout.MatchTimeout);

        var code = exception switch
        {
            InvalidRuntimeRegexException => DiagnosticCode.MQ7015_InvalidRuntimeRegex,
            Musoq.Plugins.ScalarSubqueryCardinalityException => DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation,
            _ => (DiagnosticCode?)null
        };
        if (!code.HasValue)
            return null;

        return new Diagnostic(
            code.Value,
            DiagnosticSeverity.Error,
            ErrorCatalog.GetMessage(code.Value),
            SourceLocation.None,
            SourceLocation.None);
    }

    private static Diagnostic CreateRegexTimeout(string patternOperator, string engine, TimeSpan matchTimeout) => new(
        DiagnosticCode.MQ7017_RegexMatchTimeout,
        DiagnosticSeverity.Error,
        ErrorCatalog.GetMessage(DiagnosticCode.MQ7017_RegexMatchTimeout),
        SourceLocation.None,
        arguments: new Dictionary<string, string>
        {
            ["operator"] = patternOperator,
            ["engine"] = engine,
            ["timeoutMilliseconds"] = matchTimeout.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
        });
}
