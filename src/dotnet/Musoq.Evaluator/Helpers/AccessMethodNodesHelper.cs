using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Helpers;

public static class AccessMethodNodesHelper
{
    public static bool IsAggregateMethod(this AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Method != null && node.Method.GetCustomAttribute<AggregateFunctionAttribute>() != null;
    }
}
