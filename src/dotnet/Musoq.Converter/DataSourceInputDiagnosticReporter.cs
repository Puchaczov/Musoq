using System.Runtime.ExceptionServices;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Exceptions;

namespace Musoq.Converter;

internal static class DataSourceInputDiagnosticReporter
{
    public static T? ReportOrRethrow<T>(Exception providerFailure, DiagnosticContext context)
        where T : class
    {
        if (TryReport(providerFailure, context))
            return null;

        ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
        throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
    }

    public static void ReportOrRethrowProviderFailure(Exception exception, DiagnosticContext context)
    {
        if (TryReport(exception, context) ||
            EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(exception) is not { } providerFailure)
            return;

        ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
        throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
    }

    public static bool TryReport(Exception exception, DiagnosticContext context)
    {
        if (exception is DataSourceLifecycleException { Code: DiagnosticCode.MQ7014_MalformedRuntimeInput } lifecycleFailure)
        {
            context.ReportException(lifecycleFailure);
            return true;
        }

        var providerFailure = EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(exception);
        if (providerFailure is not null && DataSourceInputDiagnosticAdapter.TryAdapt(providerFailure, out var providerDiagnostic))
        {
            context.ReportException(providerDiagnostic);
            return true;
        }

        if (!DataSourceInputDiagnosticAdapter.TryAdapt(exception, out var diagnostic))
            return false;

        context.ReportException(diagnostic);
        return true;
    }
}
