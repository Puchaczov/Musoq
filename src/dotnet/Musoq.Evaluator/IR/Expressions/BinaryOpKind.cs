namespace Musoq.Evaluator.IR.Expressions;

public enum BinaryOpKind
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    And,
    Or,
    Equal,
    NotEqual, IsDistinctFrom, IsNotDistinctFrom,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,
    LeftShift,
    RightShift,
    StringConcatenate
}
