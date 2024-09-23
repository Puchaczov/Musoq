using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources;

public abstract class RowSourceBase<T> : RowSource
{
    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            var chunkedSource = new BlockingCollection<IReadOnlyList<IObjectResolver>>();
            var workFinishedSignalizer = new CancellationTokenSource();

            var thread = new Thread(() =>
            {
                try
                {
                    CollectChunks(chunkedSource);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    chunkedSource.Add(new List<EntityResolver<T>>());
                    workFinishedSignalizer.Cancel();
                }
            });

            thread.Start();

            return new ChunkedSource<T>(chunkedSource, workFinishedSignalizer.Token);
        }
    }

    protected abstract void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource);
}