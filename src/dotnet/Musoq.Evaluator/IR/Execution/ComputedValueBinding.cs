using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class ComputedValueBinding
{
    internal static ExecutionLiteral Literal(Literal literal)
    {
        if (literal.Value == null && literal.GeneratedTypeName is { } name)
            return new ExecutionLiteral(null, literal.ReturnType)
            {
                InterpretationShape = new ExecutionInterpretationShape(
                    new TextResultShape(literal.ReturnType, InterpretationTypeNames.Executable(name))
                ),
            };
        var type = literal.Value == null && literal.ReturnType == typeof(object) ? typeof(DBNull) : literal.ReturnType;
        return new ExecutionLiteral(literal.Value, type);
    }

    internal static ExecutionCaseWhen Case(
        IReadOnlyList<ExecutionCaseWhenBranch> branches,
        ExecutionExpression? otherwise,
        CaseWhen source
    )
    {
        ExecutionExpression BindNullResult(ExecutionExpression value) =>
            value.ReturnType.ResolveClrType() == typeof(DBNull) && source.ReturnType != typeof(DBNull)
                ? new ExecutionStrictCast(value, source.ReturnType.Name, source.ReturnType)
                : value;
        var result = new ExecutionCaseWhen(
            branches.Select(branch => branch with { Result = BindNullResult(branch.Result) }).ToArray(),
            otherwise is null ? null : BindNullResult(otherwise),
            source.ReturnType
        );
        if (source.InterpretationTypeName is not { } name)
            return result;
        return result with
        {
            InterpretationShape = CommonShape(
                name,
                source.ReturnType,
                branches.Select(branch => branch.Result).Append(otherwise)
            ),
        };
    }

    internal static ExecutionCoalesce Coalesce(IReadOnlyList<ExecutionExpression> values, Coalesce source)
    {
        var result = new ExecutionCoalesce(values, source.ReturnType);
        return source.InterpretationTypeName is { } name
            ? result with
            {
                InterpretationShape = CommonShape(name, source.ReturnType, values),
            }
            : result;
    }

    private static ExecutionInterpretationShape CommonShape(
        string name,
        Type returnType,
        IEnumerable<ExecutionExpression?> values
    ) =>
        values
            .FirstOrDefault(value =>
                value is not null and not ExecutionLiteral { Value: null } && value.InterpretationShape != null
            )
            ?.InterpretationShape
        ?? new ExecutionInterpretationShape(new TextResultShape(returnType, InterpretationTypeNames.Executable(name)));

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

    internal static ExecutionExpression Member(MemberAccess member, ExecutionExpression receiver)
    {
        if (receiver.InterpretationShape is { } shape && shape.Member(member.MemberName) is { } declared)
            return InterpretationMemberBinding.ReadMember(
                receiver,
                member.MemberName,
                shape,
                declared,
                member.IsNullSafe
            );

        return new ExecutionMemberRead(receiver, member.MemberName, member.ReturnType, false)
        {
            IsNullSafe = member.IsNullSafe,
            InterpretationShape = Shape(member),
        };
    }

    private static ExecutionInterpretationShape? Shape(MemberAccess member)
    {
        if (member.GeneratedTypeName is not { } name)
            return null;
        return new ExecutionInterpretationShape(
            new TextResultShape(member.ReturnType, InterpretationTypeNames.Executable(name))
        );
    }
}
