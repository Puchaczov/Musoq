using Musoq.Parser.Helpers;

namespace Musoq.Parser.Nodes;

public abstract class BinaryNode : Node
{
    private readonly Node[] _nodes;
    private Type? _returnType;

    protected BinaryNode(Node left, Node right)
        : this(left, right, default)
    {
    }

    protected BinaryNode(Node left, Node right, TextSpan span)
    {
        _nodes = [left, right];

        // If no explicit span provided, compute from children
        if (span.IsEmpty && left?.HasSpan == true && right?.HasSpan == true)
        {
            Span = ComputeSpan(left, right);
            FullSpan = Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public Node Left => _nodes[0];

    public Node Right => _nodes[1];

    public override Type ReturnType => _returnType ??= ComputeReturnType();

    protected static string CalculateId<T>(T node)
        where T : BinaryNode
    {
        ArgumentNullException.ThrowIfNull(node);
        return $"{typeof(T).Name}{node.Left.Id}{node.Right.Id}{node.ReturnType?.Name}";
    }

    private Type ComputeReturnType()
    {
        var leftType = Left.ReturnType;
        var rightType = Right.ReturnType;
        var leftIsNullOrVoid = IsNullOrVoid(leftType);
        var rightIsNullOrVoid = IsNullOrVoid(rightType);

        if (leftIsNullOrVoid && rightIsNullOrVoid)
        {
            if (leftType is NullNode.NullType && rightType is NullNode.NullType)
                return typeof(object);

            return typeof(void);
        }

        if (leftIsNullOrVoid)
        {
            if (leftType is NullNode.NullType)
                return MakeNullable(rightType);

            return typeof(void);
        }

        if (rightIsNullOrVoid)
        {
            if (rightType is NullNode.NullType)
                return MakeNullable(leftType);

            return typeof(void);
        }


        if (leftType == null || rightType == null)
            return typeof(object);

        var returnType = NodeHelpers.GetReturnTypeMap(leftType, rightType);

        if (Left is NullNode || Right is NullNode || ShouldLiftNullableArithmetic(leftType, rightType, returnType))
            return MakeNullable(returnType);

        return returnType;
    }

    private static bool IsNullOrVoid(Type? type)
    {
        return type == null || type == typeof(void) || type is NullNode.NullType;
    }

    private static Type MakeNullable(Type? type)
    {
        if (type == null || type == typeof(void) || type is NullNode.NullType)
            return typeof(object);

        if (type.IsValueType && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            return typeof(Nullable<>).MakeGenericType(type);

        return type;
    }

    private bool ShouldLiftNullableArithmetic(Type? leftType, Type? rightType, Type? returnType)
    {
        if (leftType == null || rightType == null || returnType == null || !returnType.IsValueType || Nullable.GetUnderlyingType(returnType) != null)
            return false;

        if (Nullable.GetUnderlyingType(leftType) == null && Nullable.GetUnderlyingType(rightType) == null)
            return false;

        if (this is HyphenNode && IsTemporalNullableOperation(leftType, rightType))
            return true;

        return IsAggregateAccess(Left) || IsAggregateAccess(Right);
    }

    private static bool IsTemporalNullableOperation(Type leftType, Type rightType)
    {
        var leftUnderlying = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var rightUnderlying = Nullable.GetUnderlyingType(rightType) ?? rightType;

        return (leftUnderlying == typeof(DateTime) && rightUnderlying == typeof(DateTime)) ||
               (leftUnderlying == typeof(DateTimeOffset) && rightUnderlying == typeof(DateTimeOffset)) ||
               (leftUnderlying == typeof(DateTime) && rightUnderlying == typeof(TimeSpan)) ||
               (leftUnderlying == typeof(DateTimeOffset) && rightUnderlying == typeof(TimeSpan)) ||
               (leftUnderlying == typeof(TimeSpan) && rightUnderlying == typeof(TimeSpan));
    }

    private static bool IsAggregateAccess(Node node)
    {
        if (node is not AccessMethodNode accessMethod)
            return false;

        if (accessMethod.IsAggregate)
            return true;

        var method = accessMethod.Method;
        if (method == null)
            return false;

        return Array.Exists(
            method.GetCustomAttributes(inherit: false),
            static attribute =>
            {
                var name = attribute.GetType().Name;
                return name is "AggregateFunctionAttribute";
            });
    }
}
