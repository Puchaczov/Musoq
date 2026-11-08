using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.RecursiveCte;

public sealed class RecursiveGraphSchemaProvider(
    RecursiveGraphData data,
    RecursiveGraphSourceRecorder? recorder = null) : ISchemaProvider
{
    public RecursiveGraphSourceRecorder Recorder { get; } = recorder ?? new RecursiveGraphSourceRecorder();

    public ISchema GetSchema(string schema) => new RecursiveGraphSchema(data, Recorder);
}
