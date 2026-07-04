using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class PhysicalNodeIdentityMap
{
    private readonly Dictionary<PhysicalNode, PhysicalNodeId> _ids;

    private PhysicalNodeIdentityMap(Dictionary<PhysicalNode, PhysicalNodeId> ids)
    {
        _ids = ids;
    }

    public static PhysicalNodeIdentityMap Empty { get; } = new(
        new Dictionary<PhysicalNode, PhysicalNodeId>(ReferenceComparer<PhysicalNode>.Instance));

    public static PhysicalNodeIdentityMap Build(PhysicalNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var ids = new Dictionary<PhysicalNode, PhysicalNodeId>(ReferenceComparer<PhysicalNode>.Instance);
        AddTree(root, ids);

        return new PhysicalNodeIdentityMap(ids);
    }

    public static PhysicalNodeIdentityMap FromNodes(IEnumerable<PhysicalNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var ids = new Dictionary<PhysicalNode, PhysicalNodeId>(ReferenceComparer<PhysicalNode>.Instance);

        foreach (var node in nodes)
            AddNode(node, ids);

        return new PhysicalNodeIdentityMap(ids);
    }

    public bool TryGetId(PhysicalNode node, out PhysicalNodeId id)
    {
        ArgumentNullException.ThrowIfNull(node);

        return _ids.TryGetValue(node, out id);
    }

    public PhysicalNodeId GetId(PhysicalNode node)
    {
        return TryGetId(node, out var id)
            ? id
            : throw new InvalidOperationException($"Physical node {node.GetType().Name} is not registered in the identity map.");
    }

    private static void AddTree(
        PhysicalNode node,
        Dictionary<PhysicalNode, PhysicalNodeId> ids)
    {
        if (!AddNode(node, ids))
            return;

        foreach (var child in node.Children)
            AddTree(child, ids);
    }

    private static bool AddNode(
        PhysicalNode node,
        Dictionary<PhysicalNode, PhysicalNodeId> ids)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (ids.ContainsKey(node))
            return false;

        ids.Add(node, new PhysicalNodeId(ids.Count));
        return true;
    }
}
