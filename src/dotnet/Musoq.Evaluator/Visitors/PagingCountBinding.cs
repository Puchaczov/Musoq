namespace Musoq.Evaluator.Visitors;

internal static class PagingCountBinding
{
    internal static bool IsInRange(object? value) => value switch
    {
        sbyte number => number >= 0,
        byte => true,
        short number => number >= 0,
        ushort => true,
        int number => number >= 0,
        uint number => number <= int.MaxValue,
        long number => number is >= 0 and <= int.MaxValue,
        ulong number => number <= int.MaxValue,
        _ => false
    };
}
