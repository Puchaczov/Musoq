using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

internal static class JoinKindSemantics
{
    public static bool ProducesLeftOnly(JoinKind kind)
    {
        return kind is JoinKind.LeftSemi or JoinKind.LeftAntiSemi;
    }

    public static OutputSchema SelectOutputSchema(JoinKind kind, OutputSchema left, OutputSchema right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return ProducesLeftOnly(kind)
            ? left
            : left.Merge(right);
    }
}
