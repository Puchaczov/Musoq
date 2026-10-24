using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Dictionary<string, string> _binaryInterpretationAliases = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<Node> _nullableInterpretationRecords = new(ReferenceEqualityComparer.Instance);

    private void RegisterNullableInterpretationColumn(Node node, Musoq.Schema.ISchemaColumn column)
    {
        if (column is NullableInterpretationColumn)
            _nullableInterpretationRecords.Add(node);
    }

    private bool IsNullableInterpretationExpression(Node node) =>
        node is DotNode { IsNullSafePath: true } || _nullableInterpretationRecords.Contains(node);

    private DotNode PreserveInterpretationPathNullability(DotNode node)
    {
        Node root = node;
        while (root is DotNode dot)
            root = dot.Root;
        var alias = root switch
        {
            AccessColumnNode column => column.Alias,
            AccessObjectArrayNode { IsColumnAccess: true } indexed => indexed.TableAlias,
            _ => null
        };
        if (node.ReturnType is not { } type)
            return node;
        if (!IsNullableInterpretationExpression(node.Root))
        {
            if (alias is null || !_binaryInterpretationAliases.TryGetValue(alias, out var schemaName) ||
                SchemaRegistry?.TryGetSchema(schemaName, out var registration) != true || registration?.Node is not BinarySchemaNode schema)
                return node;
            if (!BinarySchemaExpressionTypes.IsNullable(type) &&
                !BinarySchemaExpressionTypes.MayBeNull(node, GetAllBinarySchemaFields(schema), SchemaRegistry))
                return node;
        }
        type = BinarySchemaExpressionTypes.Lift(type, true);
        return new DotNode(node.Root, node.Expression, node.IsTheMostInner, node.Name, type, node.IntendedTypeName)
        { IsNullSafePath = true }.CopySpansFrom(node);
    }

}
