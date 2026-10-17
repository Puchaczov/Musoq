using System.Collections.Generic;
using System.Linq;
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
        var alias = root switch
        {
            AccessColumnNode column => column.Alias,
            AccessObjectArrayNode { IsColumnAccess: true } indexed => indexed.TableAlias,
            _ => null
        };
        if (alias is null || !_binaryInterpretationAliases.TryGetValue(alias, out var schemaName) ||
            SchemaRegistry?.TryGetSchema(schemaName, out var registration) != true || registration?.Node is not BinarySchemaNode schema)
            return node;
        if (node.ReturnType is not { } type)
            return node;
        if (!BinarySchemaExpressionTypes.IsNullable(type) && !BinarySchemaExpressionTypes.MayBeNull(node, GetAllBinarySchemaFields(schema), SchemaRegistry))
            return node;
        type = BinarySchemaExpressionTypes.Lift(type, true);
        return new DotNode(node.Root, node.Expression, node.IsTheMostInner, node.Name, type, node.IntendedTypeName)
        { IsNullSafePath = true }.CopySpansFrom(node);
    }

    private string? ResolveBinaryArrayElementSchemaName(string sourceSchemaName, string fieldName)
    {
        if (SchemaRegistry?.TryGetSchema(sourceSchemaName, out var registration) != true ||
            registration?.Node is not BinarySchemaNode schema)
            return null;

        var field = GetAllBinarySchemaFields(schema)
            .OfType<FieldDefinitionNode>()
            .FirstOrDefault(candidate => candidate.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        var annotation = field?.TypeAnnotation;
        while (annotation is SubstreamTypeNode { Target: not null } substream)
            annotation = substream.Target;

        var element = annotation switch
        {
            ArrayTypeNode array => array.ElementType,
            RepeatUntilTypeNode repeat => repeat.ElementType,
            _ => null
        };
        if (element is SubstreamTypeNode { Target: not null } elementSubstream)
            element = elementSubstream.Target;

        return element is SchemaReferenceTypeNode reference &&
               SchemaRegistry.TryGetSchema(reference.SchemaName, out var elementRegistration) &&
               elementRegistration?.Node is BinarySchemaNode
            ? reference.SchemaName
            : null;
    }
}
