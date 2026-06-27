using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void VisitAccessMethod(AccessMethodNode node,
        Func<FunctionToken, ArgsListNode, ArgsListNode?, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var args = GetAndValidateArgs(node);
        var methodContext = ResolveMethodContext(node, args);
        var (method, canSkipInjectSource) = ResolveMethod(node, args, methodContext);

        method = ProcessGenericMethodIfNeeded(method, args, methodContext.EntityType);

        var accessMethod = CreateAccessMethod(node, args, method, methodContext, canSkipInjectSource, func);

        node.ChangeMethod(method);
        FinalizeMethodVisit(method, accessMethod);
    }

    private ArgsListNode GetAndValidateArgs(AccessMethodNode node)
    {
        var nodeFromStack = SafePop(Nodes, nameof(GetAndValidateArgs));
        if (nodeFromStack is not ArgsListNode args)
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);
        return args;
    }

}
