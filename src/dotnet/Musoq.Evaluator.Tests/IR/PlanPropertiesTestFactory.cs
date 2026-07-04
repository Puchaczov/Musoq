using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.IR;

internal static class PlanPropertiesTestFactory
{
    public static PlanProperties CreateEmpty()
    {
        return new PlanProperties(
            new Dictionary<string, SourcePlanProperties>(StringComparer.Ordinal),
            new Dictionary<string, IrExpression[]>(StringComparer.Ordinal),
            new Dictionary<string, string[]>(StringComparer.Ordinal),
            new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal),
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, RequiredColumnUsage[]>(StringComparer.Ordinal),
            [],
            [],
            new Dictionary<string, SourcePredicatePlan>(StringComparer.Ordinal),
            new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal),
            new Dictionary<string, SourcePlanRequest>(StringComparer.Ordinal),
            new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal),
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }
}
