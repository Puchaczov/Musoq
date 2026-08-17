using System;

namespace Musoq.Targets.Abstractions;

internal readonly record struct TargetSourceRange
{
    public TargetSourceRange(
        int start,
        int length,
        int? startLine = null,
        int? startColumn = null,
        int? endLine = null,
        int? endColumn = null)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        Start = start;
        Length = length;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    public int Start { get; }

    public int Length { get; }

    public int? StartLine { get; }

    public int? StartColumn { get; }

    public int? EndLine { get; }

    public int? EndColumn { get; }

    public int End => checked(Start + Length);
}
