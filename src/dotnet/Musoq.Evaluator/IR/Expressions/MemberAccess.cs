namespace Musoq.Evaluator.IR.Expressions;

/// <summary>Reads a bound member from a computed receiver without flattening its evaluation into a column path.</summary>
public sealed record MemberAccess(
    IrExpression Receiver,
    string MemberName,
    Type ReturnType,
    string? GeneratedTypeName = null,
    bool IsNullSafe = false
) : IrExpression(ReturnType);
