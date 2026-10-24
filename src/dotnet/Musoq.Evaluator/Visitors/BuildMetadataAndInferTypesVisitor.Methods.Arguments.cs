using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private ArgsListNode GetAndValidateArgs(AccessMethodNode node)
    {
        var nodeFromStack = PopSemanticNode(nameof(GetAndValidateArgs));
        if (nodeFromStack is not ArgsListNode args)
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);
        return args;
    }

}
