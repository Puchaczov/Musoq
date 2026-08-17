namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Reads one member from an already typed receiver. Dynamic reads are lowered by
/// the C# target to a single contained DLR get-member operation; static reads stay
/// ordinary CLR member access.
/// </summary>
public sealed record ExecutionMemberRead : ExecutionExpression
{
    public ExecutionMemberRead(
        ExecutionExpression receiver,
        string memberName,
        ExecutionTypeRef returnType,
        bool isDynamic)
        : base(returnType)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("A member name is required.", nameof(memberName));

        Receiver = receiver;
        MemberName = memberName;
        IsDynamic = isDynamic;
    }

    internal ExecutionMemberRead(
        ExecutionExpression receiver,
        string memberName,
        Type returnType,
        bool isDynamic)
        : this(receiver, memberName, ExecutionClrBindingFactory.FromClr(returnType), isDynamic)
    {
    }

    public ExecutionExpression Receiver { get; init; }

    public string MemberName { get; }

    public bool IsDynamic { get; }
}
