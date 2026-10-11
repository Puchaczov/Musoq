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
        if (!CanUseWindowAggregateKernel(registration))
            return (false, WindowComputationBuildResult.Unsupported(string.Empty));

        var frame = CreateAggregateWindowFrame(registration);
        var mode = ResolveWindowAggregateMode(frame);
        if (!mode.IsBuilt)
            return (false, WindowComputationBuildResult.Unsupported(string.Empty));

        var descriptor = ResolveWindowAggregateCapability(
            context.RegistrationResult.SqlDeclaration!,
            registration.FunctionName,
            arguments.Value.ReturnType.ResolveClrType(),
            registration.ReturnType,
            registration.UseRunningPeers ? ExecutionWindowAggregateMode.Running : mode.Mode);
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
            descriptor.Descriptor with
            {
                CaptureArguments = registration.CaptureArguments,
                Mode = mode.Mode,
                UseRunningPeers = registration.UseRunningPeers,
                LibraryBatch = registration.UseBatchEvaluation ? descriptor.Descriptor.LibraryBatch : null
            },
            results,
            resources.PartitionKeyArray,
            resources.OrderKeyArray,
            resources.Partitions,
            resources.SortedPartitions,
            arguments.MethodTargets)
        {
            LibraryArguments = arguments.Arguments,
            UseIntegerOrderIndex = registration.UseIntegerOrderIndex
        };

        return (true, WindowComputationBuildResult.Success(registration, node, results));
    }

    private static bool CanUseWindowAggregateKernel(WindowRegistration registration)
    {
        return registration.Function?.GetCustomAttribute<WindowFunctionAttribute>() is { KernelType: not null, ContextType: null };
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
        if (factoryMethod.GetCustomAttribute<WindowFunctionAttribute>() is { KernelType: not null } declaration)
        {
            var required = mode switch
            {
                ExecutionWindowAggregateMode.WholePartition => WindowEvaluationModes.WholePartition,
                ExecutionWindowAggregateMode.Running => WindowEvaluationModes.Running,
                _ => WindowEvaluationModes.RecomputeFrames
            };
            if ((declaration.Modes & required) == 0)
                throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
                    $"Window '{functionName}' does not support evaluation mode {required}.", Musoq.Parser.TextSpan.Empty);
            var kernel = CreateValidatedWindowKernel(factoryMethod);
            return (true, new ExecutionWindowAggregateKernelDescriptor(ExecutionWindowAggregateFunction.Library,
                mode, inputType, kernel.ResultType, kernel.StateType)
            {
                LibraryKernel = new ExecutionAggregateKernelOperations(kernel),
                LibraryBatch = CreateWindowBatchLibraryOperation(factoryMethod)
            });
        }
        throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
            $"Window '{functionName}' must register a concrete typed library kernel; legacy capability providers are unsupported.", Musoq.Parser.TextSpan.Empty);
    }

    private static AggregateKernelDescriptor CreateValidatedWindowKernel(MethodInfo declaration)
    {
        try
        {
            return AggregateKernelDescriptor.CreateForWindow(declaration);
        }
        catch (InvalidOperationException exception)
        {
            throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(exception.Message,
                Musoq.Parser.TextSpan.Empty);
        }
    }

    private static object CreateClrDefaultValue(Type type) =>
        Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Type {type.FullName} has no non-null CLR default value.");
}
