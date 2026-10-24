using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void VisitAccessMethod(AccessMethodNode node,
        Func<FunctionToken, ArgsListNode, ArgsListNode?, MethodInfo, string, bool, AccessMethodNode> func)
    {
        var args = GetAndValidateArgs(node);
        node.FilterExpression = node.FilterExpression is null
            ? null
            : PopSemanticNode(nameof(VisitAccessMethod));

        if (TryElideCorrelatedScalarSubqueryResultAccessor(node, args))
            return;

        if (TryResolveDeferredScalarSubqueryValue(node, args))
            return;

        var methodContext = ResolveMethodContext(node, args);
        if (!TryResolveMethodWithRecovery(node, args, methodContext, out var method, out var canSkipInjectSource))
            return;

        if (methodContext.IsUnopenedSource)
            SourceArgumentMethodValidation.Validate(node, method);

        method = ProcessGenericMethodIfNeeded(method, args, methodContext.EntityType);
        SuspiciousOrdinaryStringEscapeDiagnostics.ReportMethodArgumentRisks(DiagnosticContext, args, node.Arguments,
            method, canSkipInjectSource);

        var accessMethod = CreateAccessMethod(node, args, method, methodContext, canSkipInjectSource, func);

        node.ChangeMethod(method);
        FinalizeMethodVisit(method, accessMethod);
    }

}
