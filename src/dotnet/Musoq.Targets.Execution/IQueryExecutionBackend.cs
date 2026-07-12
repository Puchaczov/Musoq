using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.Execution;

internal interface IQueryExecutionBackend
{
    ExecutionTargetId TargetId { get; }

    ExecutionTargetCapabilities Capabilities { get; }

    TargetRenderResult Render(TargetRenderRequest request);
}
