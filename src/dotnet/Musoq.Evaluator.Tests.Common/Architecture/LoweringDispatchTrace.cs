using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

internal sealed class LoweringDispatchTrace
{
    internal static readonly string[] ExpectedPlanOrder =
    [
        "multi-statement",
        "cte",
        "desc",
        "set-operation",
        "aggregate",
        "window",
        "pipeline",
    ];
    internal static readonly string[] ExpectedTableOrder =
    [
        "multi-statement-table",
        "set-operation-table",
        "window-table",
        "pipeline-table",
        "aggregate-table",
    ];
    internal List<string> PlanCalls { get; } = [];
    internal List<string> TableCalls { get; } = [];

    internal void Exercise()
    {
        var facade = new PhysicalLoweringDispatchFacade(
            new PhysicalLoweringHandlers(
                _ => Plan("multi-statement"),
                _ => Plan("cte"),
                _ => Plan("desc"),
                _ => Plan("set-operation"),
                _ => Plan("aggregate"),
                _ => Plan("window"),
                _ => Plan("pipeline"),
                _ => Table("multi-statement-table"),
                _ => Table("set-operation-table"),
                _ => Table("window-table"),
                _ => Table("pipeline-table"),
                _ => Table("aggregate-table")
            )
        );
        Assert.AreEqual(LoweringAttemptKind.NoMatch, facade.TryBuildPlan(null!).Kind);
        Assert.AreEqual(LoweringAttemptKind.NoMatch, facade.TryBuildTable(null!).Kind);
    }

    private LoweringAttempt<ExecutionPlan> Plan(string name)
    {
        PlanCalls.Add(name);
        return LoweringAttempt<ExecutionPlan>.NoMatch();
    }

    private LoweringAttempt<LoweredTable> Table(string name)
    {
        TableCalls.Add(name);
        return LoweringAttempt<LoweredTable>.NoMatch();
    }
}
