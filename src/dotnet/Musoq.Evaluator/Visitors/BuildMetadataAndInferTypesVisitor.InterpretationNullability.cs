using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Dictionary<string, string> _binaryInterpretationAliases = new(StringComparer.OrdinalIgnoreCase);

    private DotNode PreserveInterpretationPathNullability(DotNode node)
    {
        Node root = node;
        while (root is DotNode dot)
            root = dot.Root;
        if (root is not AccessColumnNode column || !_binaryInterpretationAliases.TryGetValue(column.Alias, out var schemaName) ||
            SchemaRegistry?.TryGetSchema(schemaName, out var registration) != true || registration?.Node is not BinarySchemaNode schema)
            return node;
        var type = BinarySchemaExpressionTypes.Resolve(node, GetAllBinarySchemaFields(schema), SchemaRegistry);
        if (!BinarySchemaExpressionTypes.IsNullable(type) && !BinarySchemaExpressionTypes.MayBeNull(node, GetAllBinarySchemaFields(schema), SchemaRegistry))
            return node;
        return new DotNode(node.Root, node.Expression, node.IsTheMostInner, node.Name, type, node.IntendedTypeName)
        { IsNullSafePath = true }.CopySpansFrom(node);
    }
}
