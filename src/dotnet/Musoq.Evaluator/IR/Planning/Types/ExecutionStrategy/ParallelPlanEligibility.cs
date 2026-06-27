using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record ParallelPlanEligibility(bool IsEligible, string Outcome, string Reason)
{
    public static ParallelPlanEligibility Enabled(string reason)
    {
        return new ParallelPlanEligibility(true, "Enabled", reason);
    }

    public static ParallelPlanEligibility Disabled(string reason)
    {
        return new ParallelPlanEligibility(false, "Disabled", reason);
    }

    public static ParallelPlanEligibility Skipped(string reason)
    {
        return new ParallelPlanEligibility(false, "Skipped", reason);
    }
}
