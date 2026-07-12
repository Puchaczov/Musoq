namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExecutionRenderContext CreateIsolatedRenderContext()
    {
        return new ExecutionRenderContext(_renderOptions, new ExecutionRenderSession());
    }

    private ExecutionRenderContext InitializeRenderContext(ExecutionPlan plan, bool useQueryRunContext = false)
    {
        var session = new ExecutionRenderSession
        {
            UseQueryRunContext = useQueryRunContext
        };
        var context = new ExecutionRenderContext(_renderOptions, session);
        EnsureConstantInSetFields(plan, context);
        EnsureStaticMetadataFields(plan, context);
        EnsureAggregateGenerationState(plan, context);
        return context;
    }
}
