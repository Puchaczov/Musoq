using System.Reflection;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrExecutionCallableCompatibility
{
    internal static MethodInfo RequireClrMethod(this ExecutionCallableRef callableRef) =>
        callableRef?.ClrMethod ?? throw new ArgumentNullException(nameof(callableRef));
}
