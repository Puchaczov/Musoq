using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;

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
}
