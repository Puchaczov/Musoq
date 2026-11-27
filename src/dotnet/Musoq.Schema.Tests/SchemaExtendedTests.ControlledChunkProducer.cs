using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    private sealed class RecordingProducerScheduler : TaskScheduler
    {
        private readonly ConcurrentQueue<Task> _queued = new();

        public int QueuedCount => _queued.Count;

        public void RunProducer()
        {
            Assert.IsTrue(_queued.TryDequeue(out var task));
            Assert.IsTrue(TryExecuteTask(task));
        }

        protected override void QueueTask(Task task) => _queued.Enqueue(task);

        protected override IEnumerable<Task> GetScheduledTasks() => _queued.ToArray();

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    }

    private sealed class ControlledChunkProducer(bool cooperates)
    {
        private readonly TaskCompletionSource _started = TestWatchdog.Signal();
        private readonly TaskCompletionSource _cancelled = TestWatchdog.Signal();
        private readonly TaskCompletionSource _release = TestWatchdog.Signal();
        private readonly TaskCompletionSource _exited = TestWatchdog.Signal();

        public void PublishAndWait(IChunkWriter<int> writer)
        {
            _started.SetResult();
            var token = cooperates ? writer.CancellationToken : CancellationToken.None;
            try
            {
                writer.Write([1]);
                _release.Task.WaitAsync(token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _cancelled.SetResult();
                throw;
            }
            finally
            {
                _exited.SetResult();
            }
        }

        public async Task VerifyConsumerDisposalAsync(
            IEnumerable<IReadOnlyList<int>> chunks,
            Action? verifyAbandonment = null
        )
        {
            using var watchdog = new TestWatchdog();
            var read = TestWatchdog.StartWorker(() =>
            {
                using var enumerator = chunks.GetEnumerator();
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(1, enumerator.Current[0]);
            });

            await TestWatchdog.WithCleanupAsync(
                async () =>
                {
                    await watchdog.WaitAsync(read, "the consumer's early disposal");
                    if (cooperates)
                    {
                        await watchdog.WaitAsync(_cancelled.Task, "producer cancellation");
                        await watchdog.WaitAsync(_exited.Task, "producer exit");
                        return;
                    }

                    Assert.IsFalse(
                        _exited.Task.IsCompleted,
                        "Disposal should return while the producer is still held."
                    );
                    verifyAbandonment?.Invoke();
                },
                async () =>
                {
                    _release.TrySetResult();
                    try
                    {
                        await read;
                    }
                    finally
                    {
                        if (_started.Task.IsCompleted)
                            await _exited.Task;
                    }
                }
            );
        }
    }
}
