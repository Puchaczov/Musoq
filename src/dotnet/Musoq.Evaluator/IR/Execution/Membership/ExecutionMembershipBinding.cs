using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionMembershipEqualityKind
{
    TypedEquality,
    NumericEquality,
    NeverMatch
}

public sealed record ExecutionMembershipComparison(
    ExecutionMembershipEqualityKind Kind, ExecutionTypeRef ComparisonType);

public enum ExecutionMembershipEvaluationMode
{
    Eager,
    ShortCircuit
}

public sealed record ExecutionMembershipBinding
{
    public ExecutionMembershipBinding(ExecutionTypeRef operandType,
        IReadOnlyList<ExecutionTypeRef> memberTypes, IReadOnlyList<ExecutionMembershipComparison> comparisons,
        ExecutionMembershipEvaluationMode evaluationMode)
    {
        OperandType = operandType;
        EvaluationMode = evaluationMode;
        MemberTypes = ExecutionIrCollections.Freeze(memberTypes);
        Comparisons = ExecutionIrCollections.Freeze(comparisons);
    }
    public ExecutionTypeRef OperandType { get; }
    public ExecutionMembershipEvaluationMode EvaluationMode { get; }
    public IReadOnlyList<ExecutionTypeRef> MemberTypes { get; }
    public IReadOnlyList<ExecutionMembershipComparison> Comparisons { get; }
}
