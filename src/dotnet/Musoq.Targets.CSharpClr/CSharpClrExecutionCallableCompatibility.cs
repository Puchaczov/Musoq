using System.Reflection;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrExecutionCallableCompatibility
{
    private static readonly CSharpClrExecutionBindingContext DefaultBindingContext = new();

    internal static MethodInfo RequireClrMethod(this ExecutionCallableRef callableRef) =>
        DefaultBindingContext.BindMethod(callableRef);
}
