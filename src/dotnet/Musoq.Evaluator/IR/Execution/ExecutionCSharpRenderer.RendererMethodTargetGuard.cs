using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static string CreateMissingMethodTargetMessage(ExecutionMethodCall methodCall)
    {
        var declaringTypeName = methodCall.Method.DeclaringType?.FullName ?? "<unknown>";
        return $"Method call {declaringTypeName}.{methodCall.Method.Name} requires a reusable target assigned by MethodTargetReusePass before C# rendering.";
    }

    private static string GetUnsupportedMethodCallReason(ExecutionMethodCall methodCall)
    {
        return RequiresAssignedMethodTarget(methodCall)
            ? CreateMissingMethodTargetMessage(methodCall)
            : $"Execution IR C# backend cannot render method call {methodCall.Method.Name}.";
    }

    private static bool RequiresAssignedMethodTarget(ExecutionMethodCall methodCall)
    {
        return !methodCall.Method.IsStatic &&
               methodCall.Target == null &&
               !ExecutionMethodTargetReuse.CanRenderWithoutTarget(methodCall);
    }

    private ExpressionSyntax RenderPerInvocationMethodCall(
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context)
    {
        var target = SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(methodCall.Method.DeclaringType!))
            .WithArgumentList(SyntaxFactory.ArgumentList());
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    target,
                    CreateMethodNameSyntax(methodCall.Method)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(CreateMethodInvocationArguments(methodCall, context))));

        return CastIfNeeded(invocation, methodCall.ReturnType);
    }
}
