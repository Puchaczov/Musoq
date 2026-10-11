using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

internal static class FinalShapeBoundaryProjection
{
    public static ExecutionBlock Apply(ExecutionBlock block, string tableName, string? bufferName)
    {
        var nodes = new List<ExecutionNode>();
        foreach (var node in block.Nodes)
        {
            if (bufferName is not null)
            {
                if (node is ExecutionCreateTable { CapacityHint: not null } create && create.Table.Name == tableName)
                {
                    nodes.Add(new ExecutionEnsureTableCapacity(create.Table with { Name = bufferName }, create.CapacityHint));
                    continue;
                }
                if (node is ExecutionEnsureTableCapacity ensure && ensure.Table.Name == tableName)
                {
                    nodes.Add(ensure with { Table = ensure.Table with { Name = bufferName } });
                    continue;
                }
            }
            if (!IsBoundary(node, tableName))
                nodes.Add(node);
        }
        return new ExecutionBlock(nodes);
    }

    private static bool IsBoundary(ExecutionNode node, string tableName)
        => node switch
        {
            ExecutionCreateTable create => create.Table.Name == tableName,
            ExecutionEnsureTableCapacity ensure => ensure.Table.Name == tableName,
            ExecutionReturnTable returned => returned.Table.Name == tableName,
            _ => false
        };
}
