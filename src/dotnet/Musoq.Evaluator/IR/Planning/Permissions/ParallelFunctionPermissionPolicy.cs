using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Planning;

internal static class ParallelFunctionPermissionPolicy
{
    public static ParallelEligibilityCheck Check(IrExpression expression)
    {
        if (expression is MethodCall call && !SqlFunctionDeclarations.Read(call.Method).Any(static declaration =>
                declaration.Kind == SqlFunctionKind.Scalar &&
                (declaration.ExecutionPermissions & FunctionExecutionPermissions.ConcurrentInvocations) != 0))
            return ParallelEligibilityCheck.Skipped($"Function '{call.Method.Name}' does not explicitly permit concurrent invocations.");
        foreach (var child in IrExpressionTraversal.Children(expression))
        {
            var permission = Check(child);
            if (!permission.IsEligible)
                return permission;
        }
        return ParallelEligibilityCheck.Enabled;
    }
}
