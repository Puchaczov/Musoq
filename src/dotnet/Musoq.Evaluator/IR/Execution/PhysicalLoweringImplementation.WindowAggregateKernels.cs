using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static (bool IsBuilt, WindowComputationBuildResult Computation) CreateWindowAggregateKernel(
        WindowComputationContext context,
        PluginWindowArgumentsBuildResult arguments,
        string resultsName)
    {
        var registration = context.RegistrationResult.Registration!;
        if (!CanUseWindowAggregateKernel(registration, arguments))
            return (false, WindowComputationBuildResult.Unsupported(string.Empty));

        var frame = CreateAggregateWindowFrame(registration);
        var mode = ResolveWindowAggregateMode(frame);
        if (!mode.IsBuilt)
            return (false, WindowComputationBuildResult.Unsupported(string.Empty));

        var descriptor = ResolveWindowAggregateCapability(
            context.RegistrationResult.PluginFactory!,
            registration.FunctionName,
            arguments.Value.ReturnType.ResolveClrType(),
            registration.ReturnType,
            mode.Mode);
        if (!descriptor.IsBuilt || descriptor.Descriptor is null)
            return (false, WindowComputationBuildResult.Unsupported(string.Empty));

        var filterPredicate = registration.FilterPredicate == null
            ? null
            : ExecutionExpressionConverter.Convert(registration.FilterPredicate, context.SourceLookup);
        var results = new ExecutionVariable(resultsName, descriptor.Descriptor.ResultType.ResolveClrType().MakeArrayType());
        var resources = CreateWindowComputationResources(
            context,
            results,
            canBuildPartitionSetFromKeys: true);
        var node = new ExecutionWindowAggregateKernel(
            context.Buffer,
            context.Item,
            context.RowAccessMode,
            context.PartitionKey,
            context.OrderKeys,
            arguments.Value,
            filterPredicate,
            frame,
            descriptor.Descriptor,
            results,
            resources.PartitionKeyArray,
            resources.OrderKeyArray,
            resources.Partitions,
            resources.SortedPartitions,
            arguments.MethodTargets);

        return (true, WindowComputationBuildResult.Success(registration, node, results));
    }

    private static bool CanUseWindowAggregateKernel(
        WindowRegistration registration,
        PluginWindowArgumentsBuildResult arguments)
    {
        return registration.ValueArguments.Length == 1 &&
               arguments.Arguments.Count == 0 &&
               arguments.RowScopedArguments.Count == 0 &&
               (IsCountWindowFunction(registration.FunctionName) ||
                IsSupportedWindowAggregateKernelInput(arguments.Value.ReturnType.ResolveClrType()));
    }

    private static bool IsCountWindowFunction(string functionName)
    {
        return string.Equals(
            functionName.Replace("_", string.Empty, StringComparison.Ordinal),
            "count",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedWindowAggregateKernelInput(Type inputType)
    {
        var valueType = Nullable.GetUnderlyingType(inputType) ?? inputType;
        return valueType == typeof(decimal) ||
               valueType == typeof(int) ||
               valueType == typeof(long) ||
               valueType == typeof(double);
    }

    private static ExecutionWindowFrame? CreateAggregateWindowFrame(WindowRegistration registration)
    {
        if (registration.Frame != null)
            return CreateWindowFrame(registration.Frame);

        return registration.OrderKeys.Length == 0
            ? null
            : new ExecutionWindowFrame(
                ExecutionWindowFrameKind.Range,
                new ExecutionWindowFrameBound(ExecutionWindowFrameBoundKind.UnboundedPreceding, 0),
                new ExecutionWindowFrameBound(ExecutionWindowFrameBoundKind.CurrentRow, 0));
    }

    private static (bool IsBuilt, ExecutionWindowAggregateMode Mode) ResolveWindowAggregateMode(
        ExecutionWindowFrame? frame)
    {
        if (frame == null)
            return (true, ExecutionWindowAggregateMode.WholePartition);

        if (frame.Kind == ExecutionWindowFrameKind.Rows && IsUnboundedPrecedingToCurrentRow(frame))
            return (true, ExecutionWindowAggregateMode.Running);

        if (IsUnboundedPrecedingToUnboundedFollowing(frame))
            return (true, ExecutionWindowAggregateMode.WholePartition);

        return (true, ExecutionWindowAggregateMode.BoundedRows);
    }

    private static bool IsUnboundedPrecedingToCurrentRow(ExecutionWindowFrame frame)
    {
        return frame.Start.Kind == ExecutionWindowFrameBoundKind.UnboundedPreceding &&
               frame.End.Kind == ExecutionWindowFrameBoundKind.CurrentRow;
    }

    private static bool IsUnboundedPrecedingToUnboundedFollowing(ExecutionWindowFrame frame)
    {
        return frame.Start.Kind == ExecutionWindowFrameBoundKind.UnboundedPreceding &&
               frame.End.Kind == ExecutionWindowFrameBoundKind.UnboundedFollowing;
    }

    private static (bool IsBuilt, ExecutionWindowAggregateKernelDescriptor? Descriptor) ResolveWindowAggregateCapability(
        MethodInfo factoryMethod,
        string functionName,
        Type inputType,
        Type resultType,
        ExecutionWindowAggregateMode mode)
    {
        var capability = GetWindowAggregateCapability(factoryMethod, functionName, inputType, resultType);
        if (capability == null || !HasWindowAggregateCapability(capability, mode))
            return (false, null);

        if (!HasTypedWindowAggregateCapability(capability))
            return (false, null);

        if (mode == ExecutionWindowAggregateMode.BoundedRows &&
            !ImplementsRetractableAccumulator(capability, inputType, resultType))
        {
            return (false, null);
        }

        return (true, new ExecutionWindowAggregateKernelDescriptor(
            CreateWindowAggregateFunction(capability.Function),
            mode,
            capability.InputType,
            capability.ResultType,
            capability.AccumulatorType));
    }

    private static WindowAggregateCapability? GetWindowAggregateCapability(
        MethodInfo factoryMethod,
        string functionName,
        Type inputType,
        Type resultType)
    {
        var attribute = factoryMethod.GetCustomAttribute<WindowFunctionAttribute>();
        var providerType = attribute?.CapabilityProviderType;
        if (providerType == null ||
            !typeof(IWindowAggregateCapabilityProvider).IsAssignableFrom(providerType))
        {
            return null;
        }

        var provider = (IWindowAggregateCapabilityProvider?)Activator.CreateInstance(providerType);
        return provider?.GetCapability(new WindowAggregateCapabilityContext(functionName, inputType, resultType));
    }

    private static object CreateClrDefaultValue(Type type) =>
        Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Type {type.FullName} has no non-null CLR default value.");

    private static bool HasWindowAggregateCapability(
        WindowAggregateCapability capability,
        ExecutionWindowAggregateMode mode)
    {
        var required = mode switch
        {
            ExecutionWindowAggregateMode.WholePartition => WindowAggregateCapabilities.WholePartition,
            ExecutionWindowAggregateMode.Running => WindowAggregateCapabilities.Running,
            ExecutionWindowAggregateMode.BoundedRows => WindowAggregateCapabilities.BoundedRows,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return capability.Capabilities.HasFlag(required);
    }

    private static bool HasTypedWindowAggregateCapability(WindowAggregateCapability capability)
    {
        const WindowAggregateCapabilities required =
            WindowAggregateCapabilities.TypedInput |
            WindowAggregateCapabilities.TypedResult;

        return (capability.Capabilities & required) == required;
    }

    private static bool ImplementsRetractableAccumulator(
        WindowAggregateCapability capability,
        Type inputType,
        Type resultType)
    {
        var retractableType = typeof(IWindowRetractableAccumulator<,>)
            .MakeGenericType(inputType, resultType);
        return retractableType.IsAssignableFrom(capability.AccumulatorType);
    }

    private static ExecutionWindowAggregateFunction CreateWindowAggregateFunction(
        WindowAggregateFunction function)
    {
        return function switch
        {
            WindowAggregateFunction.Sum => ExecutionWindowAggregateFunction.Sum,
            WindowAggregateFunction.Count => ExecutionWindowAggregateFunction.Count,
            WindowAggregateFunction.Avg => ExecutionWindowAggregateFunction.Avg,
            WindowAggregateFunction.Min => ExecutionWindowAggregateFunction.Min,
            WindowAggregateFunction.Max => ExecutionWindowAggregateFunction.Max,
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };
    }
}
