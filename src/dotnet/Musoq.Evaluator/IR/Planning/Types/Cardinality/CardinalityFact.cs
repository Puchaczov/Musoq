using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning.Cardinality;

internal sealed record CardinalityFact(
    string TargetId,
    string TargetKind,
    CardinalityKind Kind,
    long? ExactRows,
    long? LowerBound,
    long? UpperBound,
    double Confidence,
    string Reason)
{
    public PhysicalNode? Node { get; init; }
}
