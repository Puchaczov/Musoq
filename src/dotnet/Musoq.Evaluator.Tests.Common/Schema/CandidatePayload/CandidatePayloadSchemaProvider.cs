using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public sealed class CandidatePayloadSchemaProvider(
    IReadOnlyList<CandidatePayloadSeed> rows,
    CandidatePayloadMode mode,
    CandidatePayloadRecorder? recorder = null)
    : ISchemaProvider
{
    public CandidatePayloadRecorder Recorder { get; } = recorder ?? new CandidatePayloadRecorder();

    public ISchema GetSchema(string schema)
    {
        return new CandidatePayloadSchema(schema.TrimStart('#'), rows, mode, Recorder);
    }
}
