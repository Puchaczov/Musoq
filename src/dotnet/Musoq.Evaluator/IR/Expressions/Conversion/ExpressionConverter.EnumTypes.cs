using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static EnumTypeDescriptor? ResolveNativeEnumType(Node node)
    {
        if (node is DotNode dot)
            return ResolveNativeEnumType(dot.Expression);

        var type = node switch
        {
            AccessMethodNode { Method: { } method } => method.ReturnType,
            PropertyValueNode { PropertyInfo: { } property } => property.PropertyType,
            _ => null
        };

        if (type == null)
            return null;

        var candidate = Nullable.GetUnderlyingType(type) ?? type;
        return candidate.IsEnum ? EnumTypeDescriptor.FromClrEnum(candidate) : null;
    }

    private static EnumTypeDescriptor? ResolveCommonEnumType(IEnumerable<IrExpression?> expressions)
    {
        EnumTypeDescriptor? resolved = null;
        foreach (var expression in expressions)
        {
            var candidate = expression?.EnumType;
            if (candidate == null)
                continue;

            if (resolved != null && !resolved.Equals(candidate))
                return null;

            resolved = candidate;
        }

        return resolved;
    }
}
