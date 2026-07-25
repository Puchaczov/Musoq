using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanRenderRecursiveCte(ExecutionRecursiveCte recursiveCte)
    {
        return recursiveCte.MaxIterations > 0 &&
               recursiveCte.MaxRows > 0 &&
               recursiveCte.MaxSnapshotRows > 0 &&
               CanRenderRecursiveIdentity(recursiveCte) &&
               CanRenderBlock(recursiveCte.Anchor) &&
               CanRenderBlock(recursiveCte.InvariantSetup) &&
               CanRenderBlock(recursiveCte.RecursiveMember);
    }

    private static bool CanRenderRecursiveCteAppend(ExecutionRecursiveCteAppend append)
    {
        return CanRenderRecursiveIdentity(append) && CanRenderNode(append.AppendRow);
    }

    private static bool CanRenderRecursiveIdentity(ExecutionRecursiveCte recursiveCte)
    {
        return recursiveCte.IdentityMode == ExecutionRecursiveCteIdentityMode.None
            ? recursiveCte.Seen == null && recursiveCte.IdentityFieldIndexes.Length == 0
            : recursiveCte.Seen != null &&
              CanRenderRecursiveIdentityFields(recursiveCte.RowShape, recursiveCte.IdentityFieldIndexes);
    }

    private static bool CanRenderRecursiveIdentity(ExecutionRecursiveCteAppend append)
    {
        return append.Seen == null
            ? append.IdentityFieldIndexes.Length == 0
            : CanRenderRecursiveIdentityFields(append.AppendRow.RowShape, append.IdentityFieldIndexes);
    }

    private static bool CanRenderRecursiveIdentityFields(GeneratedRowShape rowShape, int[] fieldIndexes)
    {
        return fieldIndexes.Length > 0 &&
               fieldIndexes.Distinct().Count() == fieldIndexes.Length &&
               fieldIndexes.All(index => index >= 0 &&
                                         index < rowShape.Fields.Count &&
                                         CanReferenceType(rowShape.Fields[index].Type));
    }

    private static void ValidateSourceScan(ExecutionSourceScan sourceScan)
    {
        if (!CanRenderIdentifier(sourceScan.Rows.Name))
            throw new InvalidOperationException(
                $"Execution source rows variable '{sourceScan.Rows.Name}' is not a supported C# identifier.");
    }
}
