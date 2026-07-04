using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionPlanInventory
{
    public static int CountTableSlots(ExecutionPlan? executionPlan)
    {
        if (executionPlan == null)
            return 0;

        return FindMaxTableIndex(executionPlan.Body) + 1;
    }

    public static int CountCteIndexSlots(ExecutionPlan? executionPlan)
    {
        if (executionPlan == null)
            return 0;

        return FindMaxCteIndexSlot(executionPlan.Body) + 1;
    }

    public static int FindMaxTableIndex(ExecutionBlock block)
    {
        var storedTableIndexes = ExecutionIrAnalysis
            .CollectNodes<ExecutionStoreTable>(block)
            .Select(static store => store.TableIndex);
        var storedTableReads = ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTable>(block)
            .Select(static table => table.TableIndex);
        var storedTableRowReads = ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTableRows>(block)
            .Select(static rows => rows.TableIndex);

        return storedTableIndexes
            .Concat(storedTableReads)
            .Concat(storedTableRowReads)
            .DefaultIfEmpty(-1)
            .Max();
    }

    public static int FindMaxCteIndexSlot(ExecutionBlock block)
    {
        var storeSlots = ExecutionIrAnalysis
            .CollectNodes<ExecutionStoreCteIndex>(block)
            .Select(static store => store.IndexSlot);
        var loadSlots = ExecutionIrAnalysis
            .CollectNodes<ExecutionLoadCteIndex>(block)
            .Select(static load => load.IndexSlot);

        return storeSlots
            .Concat(loadSlots)
            .DefaultIfEmpty(-1)
            .Max();
    }
}
