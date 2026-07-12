using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static string CreateMissingMethodTargetMessage(ExecutionMethodCall methodCall)
    {
        var declaringTypeName = methodCall.Method.RequireClrMethod().DeclaringType?.FullName ?? "<unknown>";
        return $"Method call {declaringTypeName}.{methodCall.Method.MethodName} requires a reusable target assigned by MethodTargetReusePass before C# rendering.";
    }

    private static string GetUnsupportedMethodCallReason(ExecutionMethodCall methodCall)
    {
        return RequiresAssignedMethodTarget(methodCall)
            ? CreateMissingMethodTargetMessage(methodCall)
            : $"Execution IR C# backend cannot render method call {methodCall.Method.MethodName}.";
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
        var target = SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(methodCall.Method.RequireClrMethod().DeclaringType!))
            .WithArgumentList(SyntaxFactory.ArgumentList());
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    target,
                    CreateMethodNameSyntax(methodCall.Method.RequireClrMethod())))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(CreateMethodInvocationArguments(methodCall, context))));

        return CastIfNeeded(invocation, methodCall.ReturnType);
    }
}
