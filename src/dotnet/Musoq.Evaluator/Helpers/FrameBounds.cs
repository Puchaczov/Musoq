namespace Musoq.Evaluator.Helpers;

public sealed record FrameBounds(
    FrameBound Start,
    FrameBound End)
{
    public FrameBounds(
        int precedingOffset,
        bool unboundedPreceding,
        int followingOffset,
        bool unboundedFollowing)
        : this(
            CreateStartBound(precedingOffset, unboundedPreceding),
            CreateEndBound(followingOffset, unboundedFollowing))
    {
    }

    private static FrameBound CreateStartBound(int offset, bool unbounded)
    {
        if (unbounded)
            return new FrameBound(FrameBoundKind.UnboundedPreceding);

        return offset == 0
            ? new FrameBound(FrameBoundKind.CurrentRow)
            : new FrameBound(FrameBoundKind.OffsetPreceding, offset);
    }

    private static FrameBound CreateEndBound(int offset, bool unbounded)
    {
        if (unbounded)
            return new FrameBound(FrameBoundKind.UnboundedFollowing);

        return offset == 0
            ? new FrameBound(FrameBoundKind.CurrentRow)
            : new FrameBound(FrameBoundKind.OffsetFollowing, offset);
    }
}
