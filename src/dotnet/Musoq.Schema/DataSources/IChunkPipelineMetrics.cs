using System;

namespace Musoq.Schema.DataSources;

internal interface IChunkPipelineMetrics
{
    void RecordProduced(int rows, int queueDepth);

    void RecordChunkConsumed(int rows, int queueDepth);

    void RecordProducerWaitOnFull();

    void RecordProducerWaitOnFullElapsed(TimeSpan elapsed);

    IDisposable MeasureProducerWaitOnFull();

    void RecordConsumerWaitOnEmpty();

    void RecordConsumerWaitOnEmptyElapsed(TimeSpan elapsed);

    IDisposable MeasureConsumerWaitOnEmpty();

    void RecordQueueDepth(int queueDepth);

    void RecordProducerException(Exception exception);

    void RecordProducerAbandoned(TimeSpan waitElapsed);
}
