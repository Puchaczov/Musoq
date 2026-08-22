using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator;

public class RunnableDebugDecorator(
    ITableRunnable runnable,
    AssemblyLoadContext assemblyLoadContext,
    params string[] filesToDelete)
    : ITableRunnable, IContextTableRunnable, IAsyncTableRunnable, IContextAsyncTableRunnable,
        IProfiledRunnable, IContextProfiledRunnable, IQueryProgressSource
{
    public ISchemaProvider Provider
    {
        get => runnable.Provider;
        set => runnable.Provider = value;
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId
    {
        get => runnable.SourceRuntimeSettingsBySourceContextId;
        set => runnable.SourceRuntimeSettingsBySourceContextId = value;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId
    {
        get => runnable.SourceRuntimeSettingDescriptionsBySourceContextId;
        set => runnable.SourceRuntimeSettingDescriptionsBySourceContextId = value;
    }

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans
    {
        get => runnable.SourceExecutionPlans;
        set => runnable.SourceExecutionPlans = value;
    }

    public ILogger Logger
    {
        get => runnable.Logger;
        set => runnable.Logger = value;
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => runnable.PhaseChanged += value;
        remove => runnable.PhaseChanged -= value;
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => runnable.DataSourceProgress += value;
        remove => runnable.DataSourceProgress -= value;
    }

    public event QueryProgressEventHandler QueryProgress
    {
        add
        {
            if (runnable is IQueryProgressSource progressSource)
                progressSource.QueryProgress += value;
        }
        remove
        {
            if (runnable is IQueryProgressSource progressSource)
                progressSource.QueryProgress -= value;
        }
    }

    private int _cleanedUp;

    public Table Run(CancellationToken token)
    {
        try
        {
            return runnable.Run(token);
        }
        finally
        {
            Cleanup();
        }
    }

    public Table Run(QueryRunContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            return runnable is IContextTableRunnable contextual
                ? contextual.Run(context)
                : runnable.Run(context.CancellationToken);
        }
        finally
        {
            Cleanup();
        }
    }

    public ValueTask<Table> RunAsync(CancellationToken token)
    {
        return RunAsyncCore(token);
    }

    public ValueTask<Table> RunAsync(QueryRunContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RunAsyncCore(context);
    }

    public Table RunWithProfile(CancellationToken token, Diagnostics.QueryProfileRecorder profileRecorder)
    {
        ArgumentNullException.ThrowIfNull(profileRecorder);
        try
        {
            return runnable is IProfiledRunnable profiled
                ? profiled.RunWithProfile(token, profileRecorder)
                : throw new InvalidOperationException("Query was not compiled with profiling instrumentation.");
        }
        finally
        {
            Cleanup();
        }
    }

    public Table RunWithProfile(QueryRunContext context, Diagnostics.QueryProfileRecorder profileRecorder)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(profileRecorder);
        try
        {
            return runnable is IContextProfiledRunnable contextual
                ? contextual.RunWithProfile(context, profileRecorder)
                : runnable is IProfiledRunnable profiled
                    ? profiled.RunWithProfile(context.CancellationToken, profileRecorder)
                    : throw new InvalidOperationException("Query was not compiled with profiling instrumentation.");
        }
        finally
        {
            Cleanup();
        }
    }

    private async ValueTask<Table> RunAsyncCore(CancellationToken token)
    {
        try
        {
            return runnable is IAsyncTableRunnable asyncRunnable
                ? await asyncRunnable.RunAsync(token).ConfigureAwait(false)
                : runnable.Run(token);
        }
        finally
        {
            Cleanup();
        }
    }

    private async ValueTask<Table> RunAsyncCore(QueryRunContext context)
    {
        try
        {
            return runnable is IContextAsyncTableRunnable contextual
                ? await contextual.RunAsync(context).ConfigureAwait(false)
                : runnable is IContextTableRunnable contextTable
                    ? contextTable.Run(context)
                    : runnable.Run(context.CancellationToken);
        }
        finally
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (Interlocked.Exchange(ref _cleanedUp, 1) != 0)
            return;

        assemblyLoadContext.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (var path in filesToDelete)
        {
            var file = new FileInfo(path);

            try
            {
                if (file.Exists)
                    file.Delete();
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("File is in use. Cannot delete it.");
            }
        }
    }
}
