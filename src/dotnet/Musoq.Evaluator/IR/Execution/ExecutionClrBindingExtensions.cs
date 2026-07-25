using System;
using System.Reflection;
using Musoq.Evaluator.IR.Execution.Portability;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// CLR binding is an explicit capability layered over portable descriptors;
/// it is not part of the portable reference contract.
/// </summary>
internal static class ExecutionClrBindingExtensions
{
    internal static Type ResolveClrType(this ExecutionTypeRef typeRef)
    {
        ArgumentNullException.ThrowIfNull(typeRef);
        return ExecutionClrBindingResolver.ResolveType(typeRef.Descriptor);
    }

    internal static string ClrDisplayName(this ExecutionTypeRef typeRef)
    {
        var type = typeRef.ResolveClrType();
        return type.FullName ?? type.Name;
    }

    internal static MethodInfo ResolveClrMethod(this ExecutionCallableRef callableRef)
    {
        ArgumentNullException.ThrowIfNull(callableRef);
        return ExecutionClrBindingResolver.ResolveMethod(callableRef.Descriptor);
    }
}
