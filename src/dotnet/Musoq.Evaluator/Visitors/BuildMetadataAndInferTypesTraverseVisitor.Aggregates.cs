using System;
using System.Threading;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class AggregateWildcardArgumentScope
{
    private static readonly AsyncLocal<int> Depth = new();

    public static bool IsActive => Depth.Value > 0;

    public static IDisposable Enter()
    {
        Depth.Value += 1;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            Depth.Value -= 1;
        }
    }
}

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (Visitor is not BuildMetadataAndInferTypesVisitor ||
            !Array.Exists(node.Arguments.Args, static argument => argument is AllColumnsNode))
        {
            base.Visit(node);
            return;
        }

        using (AggregateWildcardArgumentScope.Enter())
            base.Visit(node);
    }
}
