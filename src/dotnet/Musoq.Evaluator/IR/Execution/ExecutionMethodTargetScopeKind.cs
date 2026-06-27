namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionMethodTargetScopeKind
{
    RootBlock,
    TablePipeline,
    AggregateHelper,
    WindowHelper,
    GeneratedHelper
}
