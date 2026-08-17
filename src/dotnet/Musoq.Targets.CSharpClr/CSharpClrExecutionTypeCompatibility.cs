using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrExecutionTypeCompatibility
{
    private static readonly CSharpClrExecutionBindingContext DefaultBindingContext = new();

    internal static Type RequireClrType(this ExecutionTypeRef typeRef) =>
        DefaultBindingContext.BindType(typeRef);

    internal static Type? RequireOptionalClrType(this ExecutionTypeRef? typeRef) =>
        DefaultBindingContext.BindOptionalType(typeRef);

    internal static Type[] RequireClrTypes(this IEnumerable<ExecutionTypeRef> typeRefs) =>
        DefaultBindingContext.BindTypes(typeRefs);
}
