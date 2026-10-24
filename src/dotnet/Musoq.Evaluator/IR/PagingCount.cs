using System.Globalization;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR;

/// <summary>A fixed paging count or a runtime parameter validated before source execution.</summary>
public readonly record struct PagingCount
{
    private PagingCount(int constant, string? parameterName, ExecutionTypeRef? parameterType)
    {
        _constant = constant;
        ParameterName = parameterName;
        ParameterType = parameterType;
    }

    private readonly int _constant;
    public int? Constant => ParameterName == null ? _constant : null;
    public string? ParameterName { get; }
    public ExecutionTypeRef? ParameterType { get; }
    public bool IsConstant => Constant.HasValue;
    public int Value => Constant ?? throw new InvalidOperationException("Paging count is supplied at runtime.");

    public static PagingCount FromParameter(string name, ExecutionTypeRef type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(type);
        return new PagingCount(0, name, type);
    }

    internal static PagingCount FromSyntax(Node operand) => operand switch
    {
        IntegerNode integer => new PagingCount(checked(Convert.ToInt32(integer.ObjValue, CultureInfo.InvariantCulture)), null, null),
        ParameterReferenceNode parameter => FromParameter(parameter.Name, ExecutionClrBindingFactory.FromClr(parameter.ReturnType!)),
        _ => throw new InvalidOperationException("Paging operand must be bound before logical planning.")
    };

    public static implicit operator PagingCount(int value) => new(value, null, null);
    public override string ToString() => ToString(CultureInfo.InvariantCulture);
    public string ToString(IFormatProvider provider) => Constant?.ToString(provider) ?? $"${ParameterName}";
}
