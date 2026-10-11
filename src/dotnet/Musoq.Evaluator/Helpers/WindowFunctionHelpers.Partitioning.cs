namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static WindowPartitionSet ResolvePartitionSet(int rowCount)
        => WindowPartitionSet.Sequential(rowCount);

    public static WindowPartitionSet ResolvePartitionSet<T>(int rowCount, T[]? partitionKeys)
    {
        if (partitionKeys is null)
            return WindowPartitionSet.Sequential(rowCount);
        if (rowCount == 0)
            return WindowPartitionSet.Empty(rowCount);
        var builder = new WindowPartitionBuilder<T>(rowCount);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            builder.Add(partitionKeys[rowIndex], rowIndex);
        return builder.ToPartitionSet();
    }
}
