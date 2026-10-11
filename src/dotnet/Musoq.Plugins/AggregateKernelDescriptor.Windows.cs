using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public sealed partial record AggregateKernelDescriptor
{
    /// <summary>Validates the concrete context operation registered by a ranking declaration.</summary>
    public static MethodInfo CreateRankingForWindow(MethodInfo declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var window = declaration.GetCustomAttribute<WindowFunctionAttribute>();
        if (window?.KernelType is null || window.RankingKind == WindowRankingKind.None)
            throw CreateInvalidKernelException(declaration, "Ranking declaration must register its KernelType and RankingKind.");
        if (!window.RequiresPeerMetadata && window.RankingKind != WindowRankingKind.RowNumber)
            throw CreateInvalidKernelException(declaration, "Only an ordinal ranking contract can omit peer metadata.");

        if (window.SupportsOrdinalUpperBound && window.RankingKind is not
            (WindowRankingKind.RowNumber or WindowRankingKind.Rank or WindowRankingKind.DenseRank))
            throw CreateInvalidKernelException(declaration, "Ordinal upper bounds require an ordinal ranking contract.");
        var kernel = ResolveKernelType(window.KernelType, declaration);
        ValidateKernelState(kernel, typeof(WindowRankingContext), declaration);
        return ResolveGetMethod(kernel, typeof(WindowRankingContext), declaration);
    }

    /// <summary>Validates the typed accumulation operations registered by a window declaration.</summary>
    public static AggregateKernelDescriptor CreateForWindow(MethodInfo declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var window = declaration.GetCustomAttribute<WindowFunctionAttribute>();
        if (window?.KernelType is null)
            throw CreateInvalidKernelException(declaration, "Window declaration must register a concrete KernelType.");

        return Create(declaration, new AggregateFunctionAttribute(window.KernelType)
        {
            Name = window.Name,
            StateType = window.StateType,
            EmptyResultBehavior = window.EmptyResultBehavior,
            ExecutionPermissions = FunctionExecutionPermissionResolver.Resolve(declaration, SqlFunctionKind.Window, window)
        });
    }
}
