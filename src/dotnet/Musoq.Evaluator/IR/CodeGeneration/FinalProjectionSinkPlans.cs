using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal sealed record FinalProjectionSinkPlans(
    FinalProjectionSinkPlan TableDirectProjection,
    FinalProjectionSinkPlan TypedDirectProjection,
    FinalProjectionSinkPlan TypedPostOperations);
