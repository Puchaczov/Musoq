using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class ComputedValueBinding
{
    internal static ExecutionExpression Index(
        ArrayAccess access,
        ExecutionExpression array,
        ExecutionExpression index
    ) =>
        ExecutionIndexedAccessBinding.Bind(
            new ExecutionArrayAccess(array, index, access.ElementType, access.ReturnType)
            {
                InterpretationShape = array.InterpretationShape?.Element,
            }
        );

    internal static ExecutionExpression Member(MemberAccess member, ExecutionExpression receiver) =>
        new ExecutionMemberRead(receiver, member.MemberName, member.ReturnType, false)
        {
            IsNullSafe = member.IsNullSafe,
            InterpretationShape = receiver.InterpretationShape?.Member(member.MemberName) ?? Shape(member),
        };

    private static ExecutionInterpretationShape? Shape(MemberAccess member)
    {
        if (member.GeneratedTypeName is not { } name)
            return null;
        return new ExecutionInterpretationShape(
            new TextResultShape(member.ReturnType, InterpretationTypeNames.Executable(name))
        );
    }
}
