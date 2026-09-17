using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static SetOperationArmNames CreateStreamingSetOperationArmNames(
        string resultTableName,
        string resultShapeName,
        PhysicalNode leftSource,
        PhysicalNode rightSource)
    {
        return HasSourceAliasOverlap(leftSource, rightSource)
            ? CreateSetOperationArmNames(resultTableName, resultShapeName)
            : new SetOperationArmNames("left", "LeftRow0", "right", "RightRow0");
    }

    private static bool HasSourceAliasOverlap(PhysicalNode left, PhysicalNode right)
    {
        var leftAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSourceAliases(left, leftAliases);
        return HasSourceAlias(right, leftAliases);
    }

    private static void AddSourceAliases(PhysicalNode node, HashSet<string> aliases)
    {
        switch (node)
        {
            case PhysicalSchemaScanNode scan:
                aliases.Add(scan.Alias);
                break;
            case PhysicalCteRefNode cteRef:
                aliases.Add(cteRef.Alias);
                break;
            case PhysicalValuesScanNode values:
                aliases.Add(values.Alias);
                break;
        }

        foreach (var child in node.Children)
            AddSourceAliases(child, aliases);
    }

    private static bool HasSourceAlias(PhysicalNode node, HashSet<string> aliases)
    {
        if (node is PhysicalSchemaScanNode scan && aliases.Contains(scan.Alias) ||
            node is PhysicalCteRefNode cteRef && aliases.Contains(cteRef.Alias) ||
            node is PhysicalValuesScanNode values && aliases.Contains(values.Alias))
            return true;

        foreach (var child in node.Children)
            if (HasSourceAlias(child, aliases))
                return true;

        return false;
    }
}
