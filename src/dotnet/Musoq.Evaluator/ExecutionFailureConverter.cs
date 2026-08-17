using Musoq.Evaluator.Exceptions;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator;

internal static class ExecutionFailureConverter
{
    public static Exception Convert(string phase, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException or QueryExecutionException)
            return exception;

        if (exception is ScriptParameterBindingException parameterBinding)
            return QueryExecutionException.ForScriptParameterBinding(parameterBinding);

        if (exception is DataSourceLifecycleException dataSourceFailure)
            return QueryExecutionException.ForDataSourceFailure(dataSourceFailure);

        if (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(exception))
            return exception;

        return QueryExecutionException.ForExecutionFailure(phase, exception);
    }
}
