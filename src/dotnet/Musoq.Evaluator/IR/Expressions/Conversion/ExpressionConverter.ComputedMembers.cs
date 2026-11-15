using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static bool HasComputedReceiver(Node node) =>
        node is ArrayIndexNode || node is DotNode dot && HasComputedReceiver(dot.Root);

    private IrExpression ConvertComputedMember(DotNode node)
    {
        var receiver = Convert(node.Root);
        var nullSafe = !receiver.ReturnType.IsValueType || Nullable.GetUnderlyingType(receiver.ReturnType) != null;
        var member = new MemberAccess(
            receiver,
            ((IdentifierNode)node.Expression).Name,
            RequireReturnType(node),
            node.IntendedTypeName,
            nullSafe
        );
        if (node.Expression is AccessObjectArrayNode array)
        {
            member = member with
            {
                ReturnType = array.PropertyInfo!.PropertyType,
                GeneratedTypeName = ResolveIndexedArrayTypeName(node.IntendedTypeName, array.PropertyInfo.PropertyType),
            };
            return new ArrayAccess(
                member,
                new Literal(array.Token.Index, typeof(int)),
                node.ReturnType!,
                node.ReturnType!
            );
        }
        if (node.Expression is AccessObjectKeyNode key)
        {
            member = member with { ReturnType = key.PropertyInfo!.PropertyType };
            return new ArrayAccess(
                member,
                new Literal(key.Token.Key, typeof(string)),
                node.ReturnType!,
                node.ReturnType!
            );
        }
        return member;
    }
}
