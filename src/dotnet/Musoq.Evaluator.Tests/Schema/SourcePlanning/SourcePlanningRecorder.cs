using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class SourcePlanningRecorder
{
    private readonly ConcurrentBag<SourcePlanRequest> _requests = [];
    private readonly ConcurrentBag<SourceExecutionPlan> _executionPlans = [];
    private int _expensivePayloadComputations;
    private int _sourceRowsProduced;

    public IReadOnlyCollection<SourcePlanRequest> Requests => _requests.ToArray();

    public IReadOnlyCollection<SourceExecutionPlan> ExecutionPlans => _executionPlans.ToArray();

    public int DescribeCount { get; private set; }

    public int ExpensivePayloadComputations => _expensivePayloadComputations;

    public int SourceRowsProduced => _sourceRowsProduced;

    public void RecordDescribe()
    {
        DescribeCount++;
    }

    public void RecordRequest(SourcePlanRequest request)
    {
        _requests.Add(request);
    }

    public void RecordExecutionPlan(SourceExecutionPlan plan)
    {
        _executionPlans.Add(plan);
    }

    public void RecordExpensivePayloadComputed()
    {
        Interlocked.Increment(ref _expensivePayloadComputations);
    }

    public void RecordRowsProduced(int rows)
    {
        Interlocked.Add(ref _sourceRowsProduced, rows);
    }
}
