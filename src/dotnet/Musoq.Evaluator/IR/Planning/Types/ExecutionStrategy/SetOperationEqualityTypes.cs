using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static class SetOperationEqualityTypes
{
    internal static Type? FindNanSensitiveType(IEnumerable<Type> types) => types.FirstOrDefault(IsNanSensitive);

    internal static bool IsNanSensitive(Type type) =>
        Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type) is TypeCode.Single or TypeCode.Double;
}
