using System;
using System.Collections;
using System.Collections.Generic;

namespace Musoq.Evaluator.Diagnostics;

public sealed class ProfiledOperatorEnumerable<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _source;
    private readonly QueryProfileRecorder _recorder;
    private readonly int _scopeDepth;

    private ProfiledOperatorEnumerable(
        IEnumerable<T> source,
        QueryProfileRecorder recorder,
        int scopeDepth)
    {
        _source = source;
        _recorder = recorder;
        _scopeDepth = scopeDepth;
    }

    public static IEnumerable<T> Create(
        IEnumerable<T> source,
        QueryProfileRecorder? recorder,
        int scopeDepth)
    {
        ArgumentNullException.ThrowIfNull(source);

        return recorder == null
            ? source
            : new ProfiledOperatorEnumerable<T>(source, recorder, scopeDepth);
    }

    public IEnumerator<T> GetEnumerator()
    {
        try
        {
            return new Enumerator(_source.GetEnumerator(), _recorder, _scopeDepth);
        }
        catch (Exception exception) when (RecordActiveOperatorException(_recorder, _scopeDepth, exception))
        {
            _recorder.DisposeActiveOperatorScopes(_scopeDepth);
            throw;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static bool RecordActiveOperatorException(
        QueryProfileRecorder recorder,
        int scopeDepth,
        Exception exception)
    {
        recorder.RecordActiveOperatorException(exception, scopeDepth);
        return true;
    }

    private sealed class Enumerator(
        IEnumerator<T> source,
        QueryProfileRecorder recorder,
        int scopeDepth) : IEnumerator<T>
    {
        public T Current => source.Current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            try
            {
                return source.MoveNext();
            }
            catch (Exception exception) when (RecordActiveOperatorException(recorder, scopeDepth, exception))
            {
                recorder.DisposeActiveOperatorScopes(scopeDepth);
                throw;
            }
        }

        public void Reset()
        {
            try
            {
                source.Reset();
            }
            catch (Exception exception) when (RecordActiveOperatorException(recorder, scopeDepth, exception))
            {
                recorder.DisposeActiveOperatorScopes(scopeDepth);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                source.Dispose();
            }
            catch (Exception exception) when (RecordActiveOperatorException(recorder, scopeDepth, exception))
            {
                recorder.DisposeActiveOperatorScopes(scopeDepth);
                throw;
            }
        }
    }
}
