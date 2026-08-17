using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed class SidecarJoinRuntimePlanner(
    Func<SidecarJoinRuntimeStep, ExecutionBlock, ExecutionBlock> createStepBlock,
    Func<SidecarJoinRuntimeGuard, ExecutionBlock, ExecutionBlock> createGuardBlock)
{
    public ExecutionBlock CreateRuntimeBody(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        RowShape baseShape,
        ExecutionBlock continuation)
    {
        var scheduled = TryScheduleRuntimeOperations(operations, baseShape) ?? operations;
        var body = continuation;

        for (var index = scheduled.Count - 1; index >= 0; index--)
            body = CreateOperationBlock(scheduled[index], body);

        return body;
    }

    public IReadOnlyList<SidecarJoinRuntimeOperation>? TryScheduleRuntimeOperations(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        RowShape baseShape)
    {
        if (operations.Count < 2)
            return operations;

        var activeAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSourceAlias(activeAliases, baseShape);
        var remaining = operations.ToList();
        var scheduled = new List<SidecarJoinRuntimeOperation>(operations.Count);

        while (remaining.Count > 0)
        {
            var candidateIndex = FindNextOperationIndex(remaining, activeAliases);
            if (candidateIndex < 0)
                return null;

            var operation = remaining[candidateIndex];
            remaining.RemoveAt(candidateIndex);
            scheduled.Add(operation);

            if (operation is not SidecarJoinRuntimeStep step)
                continue;

            foreach (var alias in step.IntroducedAliases)
                activeAliases.Add(alias);
        }

        return scheduled;
    }

    public ExecutionBlock CreateOperationBlock(
        SidecarJoinRuntimeOperation operation,
        ExecutionBlock continuation)
    {
        return operation switch
        {
            SidecarJoinRuntimeStep step => createStepBlock(step, continuation),
            SidecarJoinRuntimeGuard guard => createGuardBlock(guard, continuation),
            _ => throw new InvalidOperationException($"Sidecar join runtime operation '{operation.GetType().Name}' is not supported.")
        };
    }

    private static int FindNextOperationIndex(
        IReadOnlyList<SidecarJoinRuntimeOperation> operations,
        IReadOnlySet<string> activeAliases)
    {
        var firstReadyIndex = -1;

        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            if (!operation.RequiredAliases.All(activeAliases.Contains))
                continue;

            firstReadyIndex = firstReadyIndex < 0 ? index : firstReadyIndex;
            if (CanHoistOperation(operation))
                return index;
        }

        return firstReadyIndex;
    }

    private static bool CanHoistOperation(SidecarJoinRuntimeOperation operation)
    {
        return operation switch
        {
            SidecarJoinRuntimeGuard => true,
            SidecarJoinRuntimeStep { Sidecar.Kind: CteSidecarIndexKind.KeySet, Residual: null, Filter: null } => true,
            _ => false
        };
    }

    private static void AddSourceAlias(ISet<string> aliases, RowShape shape)
    {
        if (RowShapeLookup.TryResolveSourceAlias(shape, out var alias))
            aliases.Add(alias);
    }
}
