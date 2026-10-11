using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ConstantMembershipEligibility
{
    public static bool CanUse(Type elementType, IReadOnlyList<object?> values, bool sqlSemantics)
    {
        if (elementType.IsByRef || elementType.IsPointer || elementType == typeof(void))
            return false;
        if (values.Any(value => value != null &&
                value.GetType() != (Nullable.GetUnderlyingType(elementType) ?? elementType)))
            return false;
        if (sqlSemantics && values.Any(value => value is double.NaN or float.NaN))
            return false;
        return !elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null ||
               values.All(static value => value != null);
    }
}
