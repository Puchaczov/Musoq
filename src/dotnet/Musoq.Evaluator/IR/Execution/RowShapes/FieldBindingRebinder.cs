using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class FieldBindingRebinder
{
    public static FieldBinding Rebind(
        FieldBinding field,
        FieldAccessStrategy accessStrategy,
        string? qualifiedName = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(accessStrategy);

        return field with
        {
            QualifiedName = qualifiedName ?? field.QualifiedName,
            AccessStrategy = accessStrategy
        };
    }

    public static FieldBinding[] Rebind(
        IEnumerable<FieldBinding> fields,
        Func<FieldBinding, FieldAccessStrategy> createAccess)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(createAccess);

        return fields
            .Select(field => Rebind(field, createAccess(field)))
            .ToArray();
    }
}
