using System;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static PhysicalSchemaScanNode FindFirstSchemaScan(PhysicalNode node)
    {
        if (node is PhysicalSchemaScanNode scan)
            return scan;

        foreach (var child in node.Children)
        {
            if (TryFindSchemaScan(child, out var nestedScan))
                return nestedScan;
        }

        throw new InvalidOperationException("Physical plan did not contain a schema scan.");
    }

    private static PhysicalSchemaScanNode FindSchemaScanByAlias(PhysicalNode node, string alias)
    {
        if (TryFindSchemaScanByAlias(node, alias, out var scan))
            return scan;

        throw new InvalidOperationException($"Physical plan did not contain a schema scan for alias {alias}.");
    }

    private static bool TryFindSchemaScan(PhysicalNode node, [NotNullWhen(true)] out PhysicalSchemaScanNode? scan)
    {
        if (node is PhysicalSchemaScanNode currentScan)
        {
            scan = currentScan;
            return true;
        }

        foreach (var child in node.Children)
        {
            if (TryFindSchemaScan(child, out scan))
                return true;
        }

        scan = null;
        return false;
    }

    private static bool TryFindSchemaScanByAlias(
        PhysicalNode node,
        string alias,
        [NotNullWhen(true)] out PhysicalSchemaScanNode? scan)
    {
        if (node is PhysicalSchemaScanNode currentScan &&
            string.Equals(currentScan.Alias, alias, StringComparison.OrdinalIgnoreCase))
        {
            scan = currentScan;
            return true;
        }

        foreach (var child in node.Children)
        {
            if (TryFindSchemaScanByAlias(child, alias, out scan))
                return true;
        }

        scan = null;
        return false;
    }
}
