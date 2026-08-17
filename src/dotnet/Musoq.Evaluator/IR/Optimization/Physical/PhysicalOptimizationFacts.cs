using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning.Cardinality;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed record PhysicalOptimizationFacts(
    SourceRewriteFacts SourceRewrite,
    IReadOnlyList<CardinalityFact> CardinalityFacts)
{
    public static PhysicalOptimizationFacts From(PlanProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return new PhysicalOptimizationFacts(
            SourceRewriteFacts.From(properties),
            properties.Cardinality.Facts);
    }

    public PlanProperties ApplyTo(PlanProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return SourceRewrite.ApplyTo(properties) with
        {
            CardinalityFacts = CardinalityFacts
        };
    }
}

