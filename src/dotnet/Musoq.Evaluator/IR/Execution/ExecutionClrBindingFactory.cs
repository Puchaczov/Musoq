using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution.Portability;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Compatibility bridge for CLR-aware construction of descriptor-backed
/// execution references. The references themselves remain descriptor-only.
/// </summary>
internal static class ExecutionClrBindingFactory
{
    internal static ExecutionTypeRef FromClr(Type clrType) =>
        new(ExecutionPortableSymbolFactory.FromType(clrType));

    internal static ExecutionTypeRef? FromOptionalClr(Type? clrType) =>
        clrType is null ? null : FromClr(clrType);

    internal static IReadOnlyList<ExecutionTypeRef> FromClrTypes(IEnumerable<Type> clrTypes) =>
        clrTypes.Select(FromClr).ToArray();

    internal static ExecutionCallableRef FromClr(MethodInfo clrMethod) =>
        new(ExecutionPortableSymbolFactory.FromMethod(clrMethod));
}
