namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExecutionRenderContext InitializeRenderContext(ExecutionPlan plan)
    {
        var useQueryRunContext = RenderSessionSlot.Value?.UseQueryRunContext ?? false;
        var session = new ExecutionRenderSession
        {
            UseQueryRunContext = useQueryRunContext
        };
        RenderSessionSlot.Value = session;
        var context = new ExecutionRenderContext(_renderOptions, session);
        EnsureConstantInSetFields(plan);
        EnsureStaticMetadataFields(plan);
        EnsureAggregateGenerationState(plan);
        return context;
    }

    private ExecutionRenderSession InitializeRenderSession(ExecutionPlan plan) => InitializeRenderContext(plan).Session;
}
