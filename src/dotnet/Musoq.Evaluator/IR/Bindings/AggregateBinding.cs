using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Bindings;

public sealed record AggregateBinding(
    string Identifier,
    string ColumnName,
    MethodInfo SetMethod,
    IrExpression[] SetArguments,
    IrExpression? FilterPredicate,
    MethodInfo GetMethod,
    IrExpression[] GetArguments,
    Type ReturnType,
    AggregateKernelDescriptor? Kernel = null,
    int ParentDepth = 0,
    string? DisplayName = null)
{
    public AggregateBinding(
        string identifier,
        string columnName,
        MethodInfo setMethod,
        IrExpression[] setArguments,
        MethodInfo getMethod,
        IrExpression[] getArguments,
        Type returnType,
        AggregateKernelDescriptor? kernel = null,
        int parentDepth = 0,
        string? displayName = null)
        : this(
            identifier,
            columnName,
            setMethod,
            setArguments,
            null,
            getMethod,
            getArguments,
            returnType,
            kernel,
            parentDepth,
            displayName)
    {
    }
}
