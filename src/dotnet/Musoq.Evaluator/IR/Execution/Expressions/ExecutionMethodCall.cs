using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodCall : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _arguments = [];
    private IReadOnlyDictionary<int, string> _generatedGenericArguments =
        System.Collections.Immutable.ImmutableDictionary<int, string>.Empty;

    public ExecutionMethodCall(
        ExecutionCallableRef method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        ExecutionTypeRef returnType,
        ExecutionExpression? injectedSource,
        ExecutionVariable? target = null,
        ExecutionVariable? cache = null)
        : base(returnType)
    {
        Method = method;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        Alias = alias;
        ReturnType = returnType;
        InjectedSource = injectedSource;
        Target = target;
        Cache = cache;
    }

    public ExecutionCallableRef Method { get; init; }

    public IReadOnlyList<ExecutionExpression> Arguments
    {
        get => _arguments;
        init => _arguments = ExecutionIrCollections.Freeze(value);
    }

    /// <summary>Concrete generated type arguments selected before target rendering.</summary>
    public IReadOnlyDictionary<int, string> GeneratedGenericArguments
    {
        get => _generatedGenericArguments;
        init => _generatedGenericArguments = ExecutionIrCollections.Freeze(value);
    }

    /// <summary>Concrete generated result selected from the closed generic declaration.</summary>
    public string? GeneratedResultTypeName { get; init; }

    public string? Alias { get; init; }

    public override ExecutionTypeRef ReturnType { get; init; }

    public ExecutionExpression? InjectedSource { get; init; }

    public ExecutionVariable? Target { get; init; }

    public ExecutionVariable? Cache { get; init; }

    internal EnumIntrinsicKind? EnumIntrinsic { get; init; }

    internal EnumTypeDescriptor? OperandEnumType { get; init; }

    internal EnumScalarValue? EnumMask { get; init; }

    internal ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType,
        ExecutionExpression? injectedSource,
        ExecutionVariable? target = null,
        ExecutionVariable? cache = null)
        : this(ExecutionClrBindingFactory.FromClr(method), arguments, alias, ExecutionClrBindingFactory.FromClr(returnType), injectedSource, target, cache)
    {
    }

    internal ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType)
        : this(ExecutionClrBindingFactory.FromClr(method), arguments, alias, ExecutionClrBindingFactory.FromClr(returnType), null)
    {
    }
}
