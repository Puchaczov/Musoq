using System.Diagnostics;
using System.Threading;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

[DebuggerStepThrough]
public class CompiledQuery(IRunnable runnable)
{
    public Table Run()
    {
        using var exitSourcesLoaderTokenSource = new CancellationTokenSource();
            
        var table = Run(exitSourcesLoaderTokenSource.Token);
        exitSourcesLoaderTokenSource.Cancel();
            
        return table;
    }

    public Table Run(CancellationToken token)
    {
        return runnable.Run(token);
    }
}