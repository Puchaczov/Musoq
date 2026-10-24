using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>One native type and its statically bound member in a closed result union.</summary>
public sealed record ExecutionUnionMemberCase(string TypeName, string MemberName);

/// <summary>Evaluates its receiver once and reads a member of the selected concrete result, or null.</summary>
public sealed record ExecutionUnionMemberRead : ExecutionExpression
{
    public ExecutionUnionMemberRead(ExecutionExpression receiver, string memberName,
        ExecutionTypeRef returnType, IReadOnlyList<ExecutionUnionMemberCase> cases) : base(returnType)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        ArgumentNullException.ThrowIfNull(cases);
        if (cases.Count == 0)
            throw new ArgumentException("A closed union member requires at least one declaring arm.", nameof(cases));
        Receiver = receiver;
        MemberName = memberName;
        Cases = ExecutionIrCollections.Freeze(cases);
    }

    public ExecutionExpression Receiver { get; init; }
    public string MemberName { get; }
    public IReadOnlyList<ExecutionUnionMemberCase> Cases { get; }
}
