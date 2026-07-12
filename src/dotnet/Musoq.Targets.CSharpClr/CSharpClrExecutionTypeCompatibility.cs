using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrExecutionTypeCompatibility
{
    internal static Type RequireClrType(this ExecutionTypeRef typeRef) =>
        typeRef?.ClrType ?? throw new ArgumentNullException(nameof(typeRef));

    internal static Type? RequireOptionalClrType(this ExecutionTypeRef? typeRef) =>
        typeRef?.ClrType;

    internal static Type[] RequireClrTypes(this IEnumerable<ExecutionTypeRef> typeRefs) =>
        typeRefs.Select(static typeRef => typeRef.RequireClrType()).ToArray();
}
