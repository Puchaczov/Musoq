using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Bindings;

public sealed record AggregateBinding(
    string Identifier,
    string ColumnName,
    MethodInfo SetMethod,
    IrExpression[] SetArguments,
    MethodInfo GetMethod,
    IrExpression[] GetArguments,
    Type ReturnType,
    AggregateKernelDescriptor? Kernel = null,
    int ParentDepth = 0);
