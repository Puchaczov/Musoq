using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal static class StoredTableRepresentationPlanPrinter
{
    public static string GetSlotTypeName(ExecutionStoredTableRepresentationPlan representation) =>
        representation.Kind switch
        {
            ExecutionStoredTableRepresentationKind.GeneratedRowList => $"List<{representation.RowShape.TypeName}>",
            ExecutionStoredTableRepresentationKind.GeneratedColumnarBuffer =>
                $"GeneratedColumnarBuffer<{representation.RowShape.TypeName}>",
            _ => representation.Kind.ToString()
        };

    public static bool ShouldPrint(
        ExecutionStoredTableRepresentationPlan representation,
        IReadOnlySet<int> structuralTableIndexes) =>
        representation.Kind != ExecutionStoredTableRepresentationKind.GeneratedRowList ||
        structuralTableIndexes.Contains(representation.TableIndex) ||
        representation.Ownership != ExecutionStructuralOwnershipMode.ConstructFresh ||
        representation.Lifetime != ExecutionStructuralPreparationLifetime.Execution;
}
