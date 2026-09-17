using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public sealed class CandidatePayloadRowSource(
    IReadOnlyList<CandidatePayloadSeed> rows,
    SourceExecutionContext context,
    CandidatePayloadRecorder recorder)
    : RowSource<CandidatePayloadEntity>
{
    public override IEnumerable<IReadOnlyList<CandidatePayloadEntity>> Chunks => ReadChunks();

    private IEnumerable<IReadOnlyList<CandidatePayloadEntity>> ReadChunks()
    {
        try
        {
            var candidates = SourcePlanningRowExecution.ApplyPredicateApplications(
                rows,
                context.Plan.PredicateApplications,
                SourcePredicateEvaluationPhase.CandidateMetadata,
                CreateCandidateSelector);

            foreach (var candidate in candidates)
            {
                recorder.RecordCandidateInspection(candidate.Id);
                context.EndWorkToken.ThrowIfCancellationRequested();

                recorder.RecordPayloadOpen();
                if (candidate.Fault == CandidatePayloadFault.Open)
                    throw new IOException("candidate payload open failed");

                recorder.RecordPayloadDecode();
                if (candidate.Fault == CandidatePayloadFault.Decode)
                    throw new InvalidDataException("candidate payload decode failed");

                var payload = Encoding.UTF8.GetString(Convert.FromBase64String(candidate.EncodedPayload));
                recorder.RecordRowMaterialization();
                var row = new CandidatePayloadEntity
                {
                    Id = candidate.Id,
                    Path = candidate.Path,
                    Payload = payload
                };

                var acceptedRows = SourcePlanningRowExecution.ApplyPredicateApplications(
                    [row],
                    context.Plan.PredicateApplications,
                    SourcePredicateEvaluationPhase.RowFiltering,
                    CreateRowSelector);

                foreach (var acceptedRow in acceptedRows)
                    yield return [acceptedRow];
            }
        }
        finally
        {
            recorder.RecordEnumeratorDisposal();
        }
    }

    private static Func<CandidatePayloadSeed, object?> CreateCandidateSelector(string columnName)
    {
        return columnName switch
        {
            nameof(CandidatePayloadEntity.Path) => static candidate => candidate.Path,
            _ => throw new InvalidOperationException(
                $"Column '{columnName}' is not available as candidate metadata.")
        };
    }

    private static Func<CandidatePayloadEntity, object?> CreateRowSelector(string columnName)
    {
        return columnName switch
        {
            nameof(CandidatePayloadEntity.Id) => static row => row.Id,
            nameof(CandidatePayloadEntity.Path) => static row => row.Path,
            nameof(CandidatePayloadEntity.Payload) => static row => row.Payload,
            _ => throw new InvalidOperationException($"Unknown materialized row column '{columnName}'.")
        };
    }
}
