
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

}
