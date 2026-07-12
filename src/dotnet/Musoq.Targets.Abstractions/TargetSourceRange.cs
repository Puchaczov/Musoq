using System;

namespace Musoq.Targets.Abstractions;

internal readonly record struct TargetSourceRange
{
    public TargetSourceRange(int start, int length)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => checked(Start + Length);
}
