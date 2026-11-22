using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

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
            UseQueryRunContext = useQueryRunContext,
            UsesTolerantInterpretation = ExecutionSubqueryPlans
                .SelfAndDescendants(plan)
                .Any(static candidate =>
                    ExecutionIrAnalysis
                        .CollectNodes<ExecutionInterpretSource>(candidate.Body)
                        .Any(static node =>
                            node.Kind is InterpretSourceKind.TryInterpret or InterpretSourceKind.TryParse
                        )
                ),
            ValueCaptureScope = plan.Body,
            RegexMatchTimeout = plan.RegexMatchTimeout,
        };
        var context = new ExecutionRenderContext(_renderOptions, session);
        EnsureConstantInSetFields(plan, context);
        EnsureStaticMetadataFields(plan, context);
        EnsureAggregateGenerationState(plan, context);
        return context;
    }
}
