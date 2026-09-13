using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public sealed class CandidatePayloadRecorder
{
    private readonly ConcurrentBag<SourceExecutionPlan> _executionPlans = [];
    private int _candidateInspections;
    private int _payloadOpens;
    private int _payloadDecodes;
    private int _rowMaterializations;
    private int _enumeratorDisposals;

    public Action<int>? CandidateInspected { get; set; }

    public IReadOnlyCollection<SourceExecutionPlan> ExecutionPlans => _executionPlans.ToArray();

    public int CandidateInspections => _candidateInspections;

    public int PayloadOpens => _payloadOpens;

    public int PayloadDecodes => _payloadDecodes;

    public int RowMaterializations => _rowMaterializations;

    public int EnumeratorDisposals => _enumeratorDisposals;

    public void RecordExecutionPlan(SourceExecutionPlan plan)
    {
        _executionPlans.Add(plan);
    }

    public void RecordCandidateInspection(int id)
    {
        Interlocked.Increment(ref _candidateInspections);
        CandidateInspected?.Invoke(id);
    }

    public void RecordPayloadOpen()
    {
        Interlocked.Increment(ref _payloadOpens);
    }

    public void RecordPayloadDecode()
    {
        Interlocked.Increment(ref _payloadDecodes);
    }

    public void RecordRowMaterialization()
    {
        Interlocked.Increment(ref _rowMaterializations);
    }

    public void RecordEnumeratorDisposal()
    {
        Interlocked.Increment(ref _enumeratorDisposals);
    }
}
