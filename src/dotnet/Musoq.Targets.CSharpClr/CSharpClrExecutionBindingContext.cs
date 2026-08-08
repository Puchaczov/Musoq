using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Portability;
using Musoq.Targets.Abstractions;

namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Owns the CLR binding boundary for the C# execution target.
/// </summary>
internal sealed class CSharpClrExecutionBindingContext
{
    internal Type BindType(ExecutionTypeRef typeRef)
    {
        ArgumentNullException.ThrowIfNull(typeRef);
        return typeRef.ResolveClrType();
    }

    internal Type BindType(ExecutionPortableTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return ExecutionClrBindingResolver.ResolveType(descriptor);
    }

    internal Type BindType(
        ExecutionPortableTypeDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly> semanticAssemblies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(semanticAssemblies);
        return ExecutionClrBindingResolver.ResolveType(descriptor, semanticAssemblies);
    }

    internal Type? BindOptionalType(ExecutionTypeRef? typeRef) =>
        typeRef is null ? null : BindType(typeRef);

    internal Type[] BindTypes(IEnumerable<ExecutionTypeRef> typeRefs)
    {
        ArgumentNullException.ThrowIfNull(typeRefs);
        return typeRefs.Select(BindType).ToArray();
    }

    internal MethodInfo BindMethod(ExecutionCallableRef callableRef)
    {
        ArgumentNullException.ThrowIfNull(callableRef);
        return callableRef.ResolveClrMethod();
    }

    internal MethodInfo BindMethod(ExecutionPortableCallableDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return ExecutionClrBindingResolver.ResolveMethod(descriptor);
    }

    internal MethodInfo BindMethod(
        ExecutionPortableCallableDescriptor descriptor,
        IReadOnlyDictionary<string, Assembly> semanticAssemblies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(semanticAssemblies);
        return ExecutionClrBindingResolver.ResolveMethod(descriptor, semanticAssemblies);
    }
}
