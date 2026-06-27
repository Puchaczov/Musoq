using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal enum JoinExpressionSide
{
    Constant,
    Left,
    Right,
    Mixed
}
