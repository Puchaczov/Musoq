using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Musoq.Schema.DataSources
{
    public abstract class RowSourceBase<T> : RowSource
    {
        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var chunkedSource = new BlockingCollection<IReadOnlyList<EntityResolver<T>>>();
                var tokenSource = new CancellationTokenSource();

                var thread = new Thread(() =>
                {
                    try
                    {
                        CollectChunks(chunkedSource);
                    }
                    catch (Exception exc)
                    {
                        if (Debugger.IsAttached)
                            Debug.WriteLine(exc);
                    }
                    finally
                    {
                        chunkedSource.Add(new List<EntityResolver<T>>());
                        tokenSource.Cancel();
                    }
                });

                thread.Start();

                return new ChunkedSource<T>(chunkedSource, tokenSource.Token);
            }
        }

        protected abstract void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<T>>> chunkedSource);
    }
}
