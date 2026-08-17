using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
///     Decides whether and how an aggregate kernel may be inlined into generated code.
///     This is an Execution IR level policy: it owns the eligibility decision so the C#
///     renderer only selects syntax for an already-resolved decision.
/// </summary>
internal static class AggregateInlinePolicy
{
    public static AggregateInlineKind Resolve(AggregateKernelDescriptor kernel)
    {
        ArgumentNullException.ThrowIfNull(kernel);

        if (!kernel.Inline)
            return AggregateInlineKind.None;

        var kernelType = kernel.KernelType;
        if (kernelType == typeof(CountAllAggregateKernel))
            return AggregateInlineKind.CountAll;

        if (!kernelType.IsGenericType)
            return AggregateInlineKind.None;

        var genericDefinition = kernelType.GetGenericTypeDefinition();
        if (genericDefinition == typeof(CountNullableAggregateKernel<>))
            return AggregateInlineKind.CountNullable;

        if (genericDefinition == typeof(CountReferenceAggregateKernel<>))
            return AggregateInlineKind.CountReference;

        if (genericDefinition == typeof(SumAggregateKernel<>) &&
            CanInlineDirectArithmetic(kernel.UnderlyingResultType))
        {
            return AggregateInlineKind.Sum;
        }

        if (genericDefinition == typeof(AvgAggregateKernel<>) &&
            CanInlineDirectArithmetic(kernel.UnderlyingResultType))
        {
            return AggregateInlineKind.Avg;
        }

        if (genericDefinition == typeof(MinAggregateKernel<>))
            return AggregateInlineKind.Min;

        if (genericDefinition == typeof(MaxAggregateKernel<>))
            return AggregateInlineKind.Max;

        return AggregateInlineKind.None;
    }

    private static bool CanInlineDirectArithmetic(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }
}
