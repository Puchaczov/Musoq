namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedRowCarrierClassifier
{
    public static GeneratedRowShape Apply(
        GeneratedRowShape shape,
        GeneratedRowCarrierBoundary boundary,
        GeneratedRowContextCarrierKind contextKind,
        bool requiresRowBaseAccess)
    {
        return shape with
        {
            RequiresRowBase = Classify(shape, boundary, contextKind, requiresRowBaseAccess) == GeneratedRowCarrierKind.PublicRow
        };
    }

    private static GeneratedRowCarrierKind Classify(
        GeneratedRowShape shape,
        GeneratedRowCarrierBoundary boundary,
        GeneratedRowContextCarrierKind contextKind,
        bool requiresRowBaseAccess)
    {
        if (boundary == GeneratedRowCarrierBoundary.Public ||
            !shape.SupportsGeneratedFieldAccess ||
            requiresRowBaseAccess ||
            contextKind == GeneratedRowContextCarrierKind.RequiresRowContexts)
        {
            return GeneratedRowCarrierKind.PublicRow;
        }

        return GeneratedRowCarrierKind.LeanInternal;
    }

    private enum GeneratedRowCarrierKind
    {
        PublicRow,
        LeanInternal
    }
}
