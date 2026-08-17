using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionNodeRegistry
{
    private static readonly IReadOnlyList<ExecutionNodeDescriptor> DescriptorList =
        ExecutionNodeDefinitionCatalog.Definitions
            .Select(static definition => new ExecutionNodeDescriptor(
                definition.NodeType,
                definition.RendererFamily,
                definition.ChildBlockShape,
                definition.GetChildBlocks,
                definition.Behavior))
            .ToArray();

    private static readonly IReadOnlyDictionary<Type, ExecutionNodeDescriptor> DescriptorsByType =
        DescriptorList.ToDictionary(static descriptor => descriptor.NodeType);

    public static IReadOnlyCollection<ExecutionNodeDescriptor> Descriptors => DescriptorList;

    public static bool TryGetDescriptor(ExecutionNode node, out ExecutionNodeDescriptor descriptor)
    {
        return DescriptorsByType.TryGetValue(node.GetType(), out descriptor!);
    }

    public static ExecutionRendererNodeFamily GetRendererFamily(ExecutionNode node)
    {
        return TryGetDescriptor(node, out var descriptor)
            ? descriptor.RendererFamily
            : ExecutionRendererNodeFamily.Unsupported;
    }

    public static IReadOnlyList<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return TryGetDescriptor(node, out var descriptor)
            ? descriptor.GetChildBlocks(node)
            : [];
    }

}

internal sealed record ExecutionNodeDescriptor(
    Type NodeType,
    ExecutionRendererNodeFamily RendererFamily,
    ExecutionNodeChildBlockShape ChildBlockShape,
    Func<ExecutionNode, IReadOnlyList<ExecutionBlock>> GetChildBlocks,
    ExecutionNodeBehaviorDefinition Behavior);
