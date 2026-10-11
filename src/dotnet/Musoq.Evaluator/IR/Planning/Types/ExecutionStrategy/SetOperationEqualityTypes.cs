using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static class SetOperationEqualityTypes
{
    internal static Type? FindNanSensitiveType(IEnumerable<Type> types) => types.FirstOrDefault(static type =>
        (Nullable.GetUnderlyingType(type) ?? type) == typeof(float) ||
        (Nullable.GetUnderlyingType(type) ?? type) == typeof(double));
}
