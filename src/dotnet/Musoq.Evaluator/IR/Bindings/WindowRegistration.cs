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
    /// <summary>Gets whether physical planning selected integer-key partition ordering.</summary>
    public bool UseIntegerOrderIndex { get; init; }
    /// <summary>Gets whether planning permits ordinal QUALIFY bounds under the declaration's result guarantee.</summary>
    public bool UseOrdinalUpperBounds { get; init; }
    /// <summary>Gets whether bounded evaluation captures SQL inputs once per source row.</summary>
    public bool CaptureArguments { get; init; }
    /// <summary>Gets whether planning selected the registered typed batch operation.</summary>
    public bool UseBatchEvaluation { get; init; }
    /// <summary>Gets whether a running RANGE prefix accumulates each peer group once.</summary>
    public bool UseRunningPeers { get; init; }

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
