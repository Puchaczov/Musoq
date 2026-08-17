namespace Musoq.Evaluator.IR.Execution;

internal static class FinalSelectShapeNaming
{
    public static string CreateTypeName(FinalShapeResult finalResult)
    {
        ArgumentNullException.ThrowIfNull(finalResult);
        return CreateTypeName(finalResult.Shape);
    }

    public static string CreateTypeName(GeneratedRowShape finalRowShape)
    {
        ArgumentNullException.ThrowIfNull(finalRowShape);

        const string rowMarker = "Row";
        const string shapeMarker = "Shape";

        var typeName = finalRowShape.TypeName;
        var rowIndex = typeName.IndexOf(rowMarker, StringComparison.Ordinal);
        return rowIndex < 0
            ? $"{typeName}{shapeMarker}"
            : string.Concat(
                typeName.AsSpan(0, rowIndex),
                shapeMarker,
                typeName.AsSpan(rowIndex + rowMarker.Length));
    }
}
