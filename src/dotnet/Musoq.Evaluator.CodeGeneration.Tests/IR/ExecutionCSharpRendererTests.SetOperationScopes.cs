using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void GeneratedCode_WhenUnionResultEscapesPhaseScope_ShouldCompile()
    {
        var plan = CreateSetOperationPlan();
        var body = new ExecutionBlock([
            new ExecutionPhaseBoundary(QueryPhase.Begin, ":cte0"),
            .. plan.Body.Nodes.SkipLast(1),
            new ExecutionPhaseBoundary(QueryPhase.End, ":cte0"),
            plan.Body.Nodes[^1],
        ]);

        AssertGeneratedCodeCompiles(plan with { Body = body });
    }
}
