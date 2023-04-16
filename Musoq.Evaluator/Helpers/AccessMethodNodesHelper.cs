using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Helpers;

public static class AccessMethodNodesHelper
{
    public static bool IsAggregateMethod(this AccessMethodNode node)
    {
        return node.Method != null && node.Method.GetCustomAttribute<AggregationMethodAttribute>() != null;
    }
}