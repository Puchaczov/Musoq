using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Schema;

namespace Musoq.Evaluator;

[DebuggerStepThrough]
public class CompiledTypedQuery<TOut>
{
    private readonly ITypedRunnable<TOut> _runnable;
    private readonly TypedRunState _runState;

    public CompiledTypedQuery(ITypedRunnable<TOut> runnable)
    {
        _runnable = runnable ?? throw QueryExecutionException.ForNullRunnable();
        var parameterizedRunnable = runnable as IParameterizedRunnable;
        _runState = new TypedRunState(
            parameterizedRunnable?.ParameterDefinitions,
            parameterizedRunnable?.Parameters);
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => _runState.AddPhaseChanged(value);
        remove => _runState.RemovePhaseChanged(value);
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => _runState.AddDataSourceProgress(value);
        remove => _runState.RemoveDataSourceProgress(value);
    }

    public IDictionary<string, object?> Parameters => _runState.Parameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _runState.ParameterDefinitions;

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters => _runState.RequiredParameters;

    public IEnumerable<TOut> Run()
    {
        return Run(CancellationToken.None);
    }

    public IEnumerable<TOut> Run(CancellationToken token)
    {
        return Run(_runState.CreateOptions(token));
    }

    public IEnumerable<TOut> Run(TypedQueryRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var token = options.CancellationToken;
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        return new ConfiguredTypedRunEnumerable(_runnable, options);
    }

    private sealed class ConfiguredTypedRunEnumerable(
        ITypedRunnable<TOut> runnable,
        TypedQueryRunOptions options) : IEnumerable<TOut>
    {
        private int _started;

        public IEnumerator<TOut> GetEnumerator()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                throw new InvalidOperationException("Query result enumerable can be enumerated only once.");

            var rows = RunConfiguredQuery();
            var enumerator = GetConfiguredEnumerator(rows);

            return new ConfiguredTypedRunEnumerator(enumerator);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<TOut> RunConfiguredQuery()
        {
            try
            {
                var rows = runnable.Run(options);
                if (rows == null)
                    throw new InvalidOperationException("Query execution returned null result.");

                return rows;
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
        }

        private static IEnumerator<TOut> GetConfiguredEnumerator(IEnumerable<TOut> rows)
        {
            try
            {
                return rows.GetEnumerator();
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
        }
    }

    private sealed class ConfiguredTypedRunEnumerator(
        IEnumerator<TOut> inner) : IEnumerator<TOut>
    {
        private int _disposed;

        public TOut Current => inner.Current;

        object? System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            try
            {
                return inner.MoveNext();
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
        }

        public void Reset()
        {
            inner.Reset();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            inner.Dispose();
        }
    }
}
