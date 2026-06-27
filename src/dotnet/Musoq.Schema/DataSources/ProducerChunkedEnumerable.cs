using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Musoq.Schema.DataSources;

internal sealed class ProducerChunkedEnumerable<T, TWriter> : IEnumerable<IReadOnlyList<T>>, IDisposable
    where TWriter : IChunkWriter<T>
{
    internal static readonly TimeSpan DefaultProducerShutdownWait = TimeSpan.FromMilliseconds(250);

    private readonly int _capacityInChunks;
    private readonly Func<BlockingCollection<IReadOnlyList<T>>, CancellationTokenSource> _createCancellation;
    private readonly Func<BlockingCollection<IReadOnlyList<T>>, CancellationToken, TWriter> _createWriter;
    private readonly Action<TWriter> _collectChunks;
    private readonly Func<bool> _isExternalCancellationRequested;
    private readonly IChunkPipelineMetrics? _metrics;
    private readonly TimeSpan _producerShutdownWait;
    private int _disposed;

    public ProducerChunkedEnumerable(
        int capacityInChunks,
        Func<BlockingCollection<IReadOnlyList<T>>, CancellationTokenSource> createCancellation,
        Func<BlockingCollection<IReadOnlyList<T>>, CancellationToken, TWriter> createWriter,
        Action<TWriter> collectChunks,
        Func<bool>? isExternalCancellationRequested = null,
        IChunkPipelineMetrics? metrics = null,
        TimeSpan? producerShutdownWait = null)
    {
        if (capacityInChunks <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacityInChunks), "Chunk capacity must be greater than zero.");

        var shutdownWait = producerShutdownWait ?? DefaultProducerShutdownWait;
        if (shutdownWait < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(producerShutdownWait), "Producer shutdown wait cannot be negative.");

        _capacityInChunks = capacityInChunks;
        _createCancellation = createCancellation ?? throw new ArgumentNullException(nameof(createCancellation));
        _createWriter = createWriter ?? throw new ArgumentNullException(nameof(createWriter));
        _collectChunks = collectChunks ?? throw new ArgumentNullException(nameof(collectChunks));
        _isExternalCancellationRequested = isExternalCancellationRequested ?? (static () => false);
        _metrics = metrics;
        _producerShutdownWait = shutdownWait;
    }

    public IEnumerator<IReadOnlyList<T>> GetEnumerator()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(GetType().FullName);

        var session = StartProducerSession();

        return new ChunkEnumerator<T>(
            session.Chunks,
            session.CancellationToken,
            session.Dispose,
            session.ThrowIfProducerFailed,
            _metrics);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
    }

    private ProducerSession StartProducerSession()
    {
        var chunks = new BlockingCollection<IReadOnlyList<T>>(_capacityInChunks);
        var cancellation = _createCancellation(chunks);
        var writer = _createWriter(chunks, cancellation.Token);
        var session = new ProducerSession(
            chunks,
            cancellation,
            _metrics,
            _producerShutdownWait);

        var producerTask = Task.Factory.StartNew(
            () => RunProducer(chunks, cancellation, writer, session),
            CancellationToken.None,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);

        session.SetProducerTask(producerTask);
        return session;
    }

    private void RunProducer(
        BlockingCollection<IReadOnlyList<T>> chunks,
        CancellationTokenSource cancellation,
        TWriter writer,
        ProducerSession? session)
    {
        try
        {
            _collectChunks(writer);
        }
        catch (OperationCanceledException exception) when (cancellation.IsCancellationRequested)
        {
            if (_isExternalCancellationRequested())
                session?.CaptureProducerException(exception);
        }
        catch (Exception exception)
        {
            session?.CaptureProducerException(exception);
        }
        finally
        {
            CompleteAdding(chunks);
            _metrics?.RecordQueueDepth(chunks.Count);
        }
    }

    private static void CompleteAdding(BlockingCollection<IReadOnlyList<T>>? chunks)
    {
        if (chunks == null)
            return;

        try
        {
            if (!chunks.IsAddingCompleted)
                chunks.CompleteAdding();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed class ProducerSession : IDisposable
    {
        private readonly CancellationTokenSource _cancellation;
        private readonly IChunkPipelineMetrics? _metrics;
        private readonly TimeSpan _producerShutdownWait;
        private Task? _producerTask;
        private ExceptionDispatchInfo? _producerException;
        private int _disposed;
        private int _cleanupCompleted;

        public ProducerSession(
            BlockingCollection<IReadOnlyList<T>> chunks,
            CancellationTokenSource cancellation,
            IChunkPipelineMetrics? metrics,
            TimeSpan producerShutdownWait)
        {
            Chunks = chunks;
            _cancellation = cancellation;
            _metrics = metrics;
            _producerShutdownWait = producerShutdownWait;
        }

        public BlockingCollection<IReadOnlyList<T>> Chunks { get; }

        public CancellationToken CancellationToken => _cancellation.Token;

        public void SetProducerTask(Task producerTask)
        {
            Volatile.Write(ref _producerTask, producerTask);
        }

        public void CaptureProducerException(Exception exception)
        {
            Volatile.Write(ref _producerException, ExceptionDispatchInfo.Capture(exception));
            _metrics?.RecordProducerException(exception);
        }

        public void ThrowIfProducerFailed()
        {
            Volatile.Read(ref _producerException)?.Throw();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            var producerTask = Volatile.Read(ref _producerTask);

            if (producerTask != null &&
                !producerTask.IsCompleted)
            {
                _cancellation.Cancel();
                CompleteAdding(Chunks);

                if (!WaitForProducer(producerTask))
                {
                    ScheduleDeferredCleanup(producerTask);
                    return;
                }
            }

            DisposeResources();
        }

        private bool WaitForProducer(Task producerTask)
        {
            try
            {
                if (producerTask.Wait(_producerShutdownWait))
                    return true;
            }
            catch (AggregateException exception) when (exception.InnerExceptions.Count == 1)
            {
                _metrics?.RecordProducerException(exception.InnerExceptions[0]);
                return true;
            }
            catch (AggregateException exception)
            {
                foreach (var innerException in exception.InnerExceptions)
                    _metrics?.RecordProducerException(innerException);

                return true;
            }

            _metrics?.RecordProducerAbandoned(_producerShutdownWait);
            return false;
        }

        private void ScheduleDeferredCleanup(Task producerTask)
        {
            _ = producerTask.ContinueWith(
                static (_, state) => ((ProducerSession)state!).DisposeResources(),
                this,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void DisposeResources()
        {
            if (Interlocked.Exchange(ref _cleanupCompleted, 1) != 0)
                return;

            _cancellation.Dispose();
            Chunks.Dispose();
        }
    }
}
