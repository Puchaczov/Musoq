using System.Reflection;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrExecutionCallableCompatibility
{
    private static readonly CSharpClrExecutionBindingContext DefaultBindingContext = new();

    internal static ConstructorInfo RequireClrConstructor(this ExecutionCallableRef callableRef) =>
        callableRef.ResolveClrConstructor();

    internal static MethodInfo RequireClrMethod(this ExecutionCallableRef callableRef) =>
        DefaultBindingContext.BindMethod(callableRef);
}
