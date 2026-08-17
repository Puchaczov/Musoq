using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter;

public sealed class CompiledExplainAnalyzeQuery : IDisposable
{
    private readonly CompiledQuery _compiledQuery;
    private readonly ExecutionPlanOperatorCatalog _operatorCatalog;
    private bool _disposed;

    internal CompiledExplainAnalyzeQuery(
        CompiledQuery compiledQuery,
        ExecutionPlanOperatorCatalog operatorCatalog)
    {
        _compiledQuery = compiledQuery ?? throw new ArgumentNullException(nameof(compiledQuery));
        _operatorCatalog = operatorCatalog ?? throw new ArgumentNullException(nameof(operatorCatalog));
    }

    public IDictionary<string, object?> Parameters => _compiledQuery.Parameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _compiledQuery.ParameterDefinitions;

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters => _compiledQuery.RequiredParameters;

    public IReadOnlyList<ScriptParameterContract> ParameterContracts => _compiledQuery.ParameterContracts;

    public ExplainAnalyzeResult Run(CancellationToken token = default)
    {
        EnsureNotDisposed();

        var profileResult = _compiledQuery.RunWithProfile(token, emitTelemetry: false);
        var executionPlanText = _operatorCatalog.AnnotatedExecutionPlanText;
        var operators = ExecutionPlanOperatorIdAnnotator.CreateOperatorSnapshots(
            _operatorCatalog,
            profileResult.Profile);
        var profile = profileResult.Profile with { Operators = operators };
        QueryProfileTelemetry.Emit(profile);

        return new ExplainAnalyzeResult(
            profileResult.Result,
            profile,
            executionPlanText,
            ExplainAnalyzeTextPrinter.Print(executionPlanText, profile));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _compiledQuery.Dispose();
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CompiledExplainAnalyzeQuery));
    }
}
