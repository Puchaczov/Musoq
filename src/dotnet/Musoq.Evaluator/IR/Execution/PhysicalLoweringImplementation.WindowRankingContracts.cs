using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionCallableRef CreateRankingLibraryOperation(WindowRegistration registration)
    {
        var declaration = registration.Function;
        if (declaration?.GetCustomAttribute<WindowFunctionAttribute>() is not { KernelType: not null } attribute ||
            attribute.RankingKind == WindowRankingKind.None)
            throw new SourcePlanContractException("Ranking execution requires a library declaration with a concrete kernel operation.", TextSpan.Empty);

        try
        {
            return ExecutionClrBindingFactory.FromClr(AggregateKernelDescriptor.CreateRankingForWindow(declaration));
        }
        catch (InvalidOperationException exception)
        {
            throw new SourcePlanContractException(exception.Message, TextSpan.Empty);
        }
    }
    private static WindowComputationBuildResult CreateRankingComputation(WindowComputationContext context)
    {
        var registration = context.RegistrationResult.Registration!;
        var function = context.RegistrationResult.RankingFunction ??
                       throw new InvalidOperationException("Ranking lowering requires a registered context kind.");
        var results = new ExecutionVariable(
            CreateRankingResultVariableName(
                context.ResultTableName,
                function,
                registration.WindowIndex,
                context.ResultNameMode),
            registration.ReturnType.MakeArrayType());
        var resources = CreateWindowComputationResources(context, results);
        var node = new ExecutionComputeRankingWindow(
            context.Buffer,
            context.Item,
            context.RowAccessMode,
            context.PartitionKey,
            context.OrderKeys,
            function,
            results,
            resources.PartitionKeyArray,
            resources.OrderKeyArray,
            resources.Partitions,
            resources.SortedPartitions,
            context.QualifyUpperBound)
        {
            LibraryGet = CreateRankingLibraryOperation(registration),
            RequiresPeerMetadata = RankingWindowMetadata.RequiresPeerMetadata(registration),
            UseIntegerOrderIndex = registration.UseIntegerOrderIndex
        };

        return WindowComputationBuildResult.Success(registration, node, results);
    }
}
