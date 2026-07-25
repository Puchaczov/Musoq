using System.Collections.Generic;
using System.Globalization;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{

    private static LoweringAttempt<IReadOnlyList<WindowComputationBuildResult>> CreateWindowComputations(
        IReadOnlyList<WindowRegistrationBuildResult> registrationResults,
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        string resultTableName,
        IReadOnlyDictionary<int, long> qualifyUpperBounds)
    {
        var computations = new List<WindowComputationBuildResult>(registrationResults.Count);
        var resultNameMode = registrationResults.Count > 1
            ? WindowResultNameMode.IndexedByWindow
            : WindowResultNameMode.Standard;
        var keyArrays = new WindowKeyArrayRegistry();
        var partitions = new WindowPartitionSetRegistry();
        var sortedPartitions = new WindowPartitionSetRegistry();
        var inPlaceSortableSortedPartitionSignatures =
            CreateInPlaceSortableSortedPartitionSignatures(registrationResults);
        var singleUsePartitionKeySignatures =
            CreateSingleUsePartitionKeySignatures(registrationResults);

        foreach (var registrationResult in registrationResults)
        {
            var registration = registrationResult.Registration!;
            var partitionKey = CreateWindowPartitionKey(registration.PartitionKeys, sourceLookup, aggregateSourceFields);
            if (!partitionKey.IsBuilt)
            {
                return LoweringAttempt<IReadOnlyList<WindowComputationBuildResult>>.Unsupported(
                    $"Execution IR window lowering cannot lower registration {registration.WindowIndex.ToString(CultureInfo.InvariantCulture)}. {partitionKey.UnsupportedReason}");
            }

            var orderKeys = CreateWindowOrderKeys(registration.OrderKeys, sourceLookup, aggregateSourceFields);
            if (!orderKeys.IsBuilt)
            {
                return LoweringAttempt<IReadOnlyList<WindowComputationBuildResult>>.Unsupported(
                    $"Execution IR window lowering cannot lower registration {registration.WindowIndex.ToString(CultureInfo.InvariantCulture)}. {orderKeys.UnsupportedReason}");
            }

            var computation = CreateWindowComputation(new WindowComputationContext(
                registrationResult,
                buffer,
                item,
                rowAccessMode,
                partitionKey.Value.HasValue ? partitionKey.Value.RequireValue() : null,
                orderKeys.Value,
                sourceLookup,
                aggregateSourceFields,
                resultTableName,
                resultNameMode,
                keyArrays,
                partitions,
                sortedPartitions,
                WindowRegistrationLoweringHelpers.CreateWindowPartitionSignature(registration),
                WindowRegistrationLoweringHelpers.CreateWindowOrderSignature(registration),
                WindowRegistrationLoweringHelpers.CreateWindowPartitionListSignature(registration),
                WindowRegistrationLoweringHelpers.CreateWindowSortedPartitionListSignature(registration),
                inPlaceSortableSortedPartitionSignatures,
                singleUsePartitionKeySignatures,
                qualifyUpperBounds.TryGetValue(registration.WindowIndex, out var upperBound)
                    ? upperBound
                    : null));

            if (!computation.IsBuilt)
            {
                return LoweringAttempt<IReadOnlyList<WindowComputationBuildResult>>.Unsupported(
                    $"Execution IR window lowering cannot lower registration {registration.WindowIndex.ToString(CultureInfo.InvariantCulture)}. {computation.UnsupportedReason}");
            }

            computations.Add(computation);
        }

        return LoweringAttempt<IReadOnlyList<WindowComputationBuildResult>>.Built(computations);
    }

    private static WindowComputationBuildResult CreateWindowComputation(WindowComputationContext context)
    {
        var registration = context.RegistrationResult.Registration!;
        if (context.RegistrationResult.RankingFunction != null)
        {
            var results = new ExecutionVariable(
                CreateRankingResultVariableName(
                    context.ResultTableName,
                    context.RegistrationResult.RankingFunction.Value,
                    registration.WindowIndex,
                    context.ResultNameMode),
                typeof(long[]));
            var resources = CreateWindowComputationResources(context, results);
            var node = new ExecutionComputeRankingWindow(
                context.Buffer,
                context.Item,
                context.RowAccessMode,
                context.PartitionKey,
                context.OrderKeys,
                context.RegistrationResult.RankingFunction.Value,
                results,
                resources.PartitionKeyArray,
                resources.OrderKeyArray,
                resources.Partitions,
                resources.SortedPartitions,
                context.QualifyUpperBound);

            return WindowComputationBuildResult.Success(registration, node, results);
        }

        if (context.RegistrationResult.OffsetFunction != null)
        {
            var arguments = CreateOffsetWindowArguments(registration, context.SourceLookup, context.AggregateSourceFields);
            if (!arguments.IsBuilt)
                return WindowComputationBuildResult.Unsupported(arguments.UnsupportedReason);

            var results = new ExecutionVariable(
                CreateOffsetResultVariableName(
                    context.ResultTableName,
                    context.RegistrationResult.OffsetFunction.Value,
                    registration.WindowIndex,
                    context.ResultNameMode),
                registration.ReturnType.MakeArrayType());
            var resources = CreateWindowComputationResources(context, results);
            var node = new ExecutionComputeOffsetWindow(
                context.Buffer,
                context.Item,
                context.RowAccessMode,
                context.PartitionKey,
                context.OrderKeys,
                arguments.Value,
                arguments.Offset,
                arguments.DefaultValue,
                context.RegistrationResult.OffsetFunction.Value,
                results,
                resources.PartitionKeyArray,
                resources.OrderKeyArray,
                resources.Partitions,
                resources.SortedPartitions);

            return WindowComputationBuildResult.Success(registration, node, results);
        }

        if (context.RegistrationResult.PluginFactory != null)
        {
            var resultsName = CreatePluginResultVariableName(
                context.ResultTableName,
                registration.FunctionName,
                registration.WindowIndex,
                context.ResultNameMode);
            var arguments = CreatePluginWindowArguments(registration, context.SourceLookup, context.AggregateSourceFields);
            if (!arguments.IsBuilt)
                return WindowComputationBuildResult.Unsupported(arguments.UnsupportedReason);

            var kernelComputation = CreateWindowAggregateKernel(context, arguments, resultsName);
            if (kernelComputation.IsBuilt)
                return kernelComputation.Computation;

            if (registration.FilterPredicate != null)
                return WindowComputationBuildResult.Unsupported(
                    $"Execution IR filtered window aggregate lowering requires a typed aggregate kernel for {registration.FunctionName}.");

            var pluginContractUnsupportedReason = GetTypedPluginWindowDispatchUnsupportedReason(registration, arguments);
            if (pluginContractUnsupportedReason != null)
                return WindowComputationBuildResult.Unsupported(pluginContractUnsupportedReason);

            var results = new ExecutionVariable(
                resultsName,
                WindowRegistrationLoweringHelpers.CreatePluginWindowResultArrayType(registration));
            var resources = CreateWindowComputationResources(context, results);
            var node = new ExecutionComputePluginWindow(
                context.Buffer,
                context.Item,
                context.RowAccessMode,
                context.PartitionKey,
                context.OrderKeys,
                arguments.Value,
                arguments.Arguments,
                arguments.RowScopedArguments,
                CreatePluginWindowFrame(registration),
                context.RegistrationResult.PluginFactory,
                registration.FunctionName,
                results,
                resources.PartitionKeyArray,
                resources.OrderKeyArray,
                resources.Partitions,
                resources.SortedPartitions,
                arguments.MethodTargets);

            return WindowComputationBuildResult.Success(registration, node, results);
        }

        return WindowComputationBuildResult.Unsupported(
            $"Execution IR window lowering cannot resolve supported function {registration.FunctionName}.");
    }

    private static (
        ExecutionWindowKeyArray? PartitionKeyArray,
        ExecutionWindowKeyArray? OrderKeyArray,
        ExecutionWindowPartitionSet? Partitions,
        ExecutionWindowPartitionSet? SortedPartitions) CreateWindowComputationResources(
            WindowComputationContext context,
            ExecutionVariable results,
            bool canBuildPartitionSetFromKeys = false)
    {
        var partitionKeyArray = CreateWindowPartitionKeyArray(
            context,
            results,
            canBuildPartitionSetFromKeys);
        var orderKeyArray = CreateWindowOrderKeyArray(context, results);
        var partitions = CreateWindowPartitionSet(context, results);

        return (
            partitionKeyArray,
            orderKeyArray,
            partitions,
            CreateWindowSortedPartitionSet(context, results, partitions));
    }

}
