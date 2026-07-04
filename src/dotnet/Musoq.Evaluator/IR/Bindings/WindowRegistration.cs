using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Bindings;

public sealed record WindowRegistration(
    MethodInfo? Function,
    string FunctionName,
    IrExpression[] PartitionKeys,
    OrderField[] OrderKeys,
    IrExpression[] ValueArguments,
    IrExpression? FilterPredicate,
    int WindowIndex,
    Type ReturnType,
    WindowFrameNode? Frame = null)
{
    public WindowRegistration(
        MethodInfo? Function,
        string FunctionName,
        IrExpression[] PartitionKeys,
        OrderField[] OrderKeys,
        IrExpression[] ValueArguments,
        int WindowIndex,
        Type ReturnType,
        WindowFrameNode? Frame = null)
        : this(
            Function,
            FunctionName,
            PartitionKeys,
            OrderKeys,
            ValueArguments,
            null,
            WindowIndex,
            ReturnType,
            Frame)
    {
    }
}
