using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private ArrayAccess ConvertArrayAccess(ArrayIndexNode node)
    {
        var arrayExpr = Convert(node.Array);
        var indexExpr = Convert(node.Index);

        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }

    private ArrayAccess ConvertAccessObjectArray(AccessObjectArrayNode node)
    {
        var indexableType = node.ColumnType ?? node.PropertyInfo?.PropertyType;
        if (indexableType == null)
            throw new InvalidOperationException($"AccessObjectArrayNode '{node}' is missing its indexable type; cannot lower to IR.");
        var intendedArrayType = ResolveIndexedArrayTypeName(node.IntendedTypeName, indexableType);
        var columnAccessNode = new AccessColumnNode(node.ObjectName, node.TableAlias ?? string.Empty,
            indexableType, TextSpan.Empty, intendedArrayType);
        var arrayExpr = ConvertColumnAccess(columnAccessNode);
        var indexExpr = new Literal(node.Token.Index, typeof(int));

        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }

    private static string? ResolveIndexedArrayTypeName(string? elementTypeName, Type collectionType)
    {
        if (elementTypeName == null || !collectionType.IsArray)
            return null;

        var elementType = collectionType.GetElementType()!;
        while (elementType.IsArray)
            elementType = elementType.GetElementType()!;

        // A dot chain can carry the enclosing record's intended name. Concrete
        // primitive arrays already have their complete CLR identity.
        return elementType == typeof(object) ? elementTypeName + "[]" : null;
    }

    private ArrayAccess ConvertAccessObjectKey(AccessObjectKeyNode node)
    {
        var indexableType = node.ColumnType ?? node.PropertyInfo?.PropertyType;
        if (indexableType == null)
            throw new InvalidOperationException($"AccessObjectKeyNode '{node}' is missing its indexable type; cannot lower to IR.");

        var alias = node.TableAlias ?? string.Empty;
        var arrayExpr = new ColumnRef(alias, node.ObjectName, indexableType)
        {
            Stability = ResolveColumnStability(alias, node.ObjectName)
        };
        var indexExpr = new Literal(node.Token.Key, typeof(string));
        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }
}
