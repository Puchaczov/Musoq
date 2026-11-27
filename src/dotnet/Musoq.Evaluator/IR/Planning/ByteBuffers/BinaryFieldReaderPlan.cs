using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Planning;

internal enum BinaryFieldDemand
{
    Value,
    Length,
    Discard,
}

internal sealed record BinaryFieldReaderPlan(
    string SchemaName,
    string Container,
    ImmutableDictionary<string, BinaryFieldDemand> Fields
)
{
    internal string TypeName =>
        $"{InterpretationCSharpNames.Qualified(Container)}.{InterpretationCSharpNames.Identifier(SchemaName)}";

    internal BinaryFieldDemand Demand(string field) => Fields.GetValueOrDefault(field, BinaryFieldDemand.Value);

    internal string Describe() =>
        $" [byte fields: {string.Join(", ", Fields.OrderBy(field => field.Key, StringComparer.Ordinal).Select(field => $"{field.Key}={field.Value}"))}]";

    internal static IEnumerable<BinaryFieldReaderPlan> Collect(PhysicalNode node)
    {
        if (node is PhysicalInterpretSourceNode { FieldReader: { } reader })
            yield return reader;
        foreach (var child in node.Children)
        foreach (var plan in Collect(child))
            yield return plan;
    }

    internal static bool HasByteFields(PhysicalInterpretSourceNode source, SchemaRegistry? registry) =>
        source.Kind
            is InterpretSourceKind.Interpret
                or InterpretSourceKind.InterpretAt
                or InterpretSourceKind.TryInterpret
        && registry is not null
        && registry.TryGetSchema(source.SchemaName, out var registration)
        && registration?.Node is BinarySchemaNode { IsGeneric: false, Extends: null } schema
        && schema.Fields.Any(field =>
            field is FieldDefinitionNode { Name: not "_", TypeAnnotation: ByteArrayTypeNode }
        );

    internal static HashSet<string> EligibleFields(PhysicalInterpretSourceNode source, SchemaRegistry? registry)
    {
        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (
            source.Kind
                is not (
                    InterpretSourceKind.Interpret
                    or InterpretSourceKind.InterpretAt
                    or InterpretSourceKind.TryInterpret
                )
            || registry is null
            || !registry.TryGetSchema(source.SchemaName, out var registration)
            || registration?.Node is not BinarySchemaNode { IsGeneric: false, Extends: null } schema
        )
            return eligible;

        // A nested interpreter can capture its enclosing schema type. Keep that nominal contract intact.
        if (
            schema
                .Fields.OfType<FieldDefinitionNode>()
                .Any(field =>
                    field.TypeAnnotation
                        is not (
                            PrimitiveTypeNode
                            or BitsTypeNode
                            or AlignmentNode
                            or ByteArrayTypeNode
                            or StringTypeNode { AsTextSchemaName: null }
                        )
                )
        )
            return eligible;

        // Walk backwards. A later unknown decoder can consume earlier fields through a parent context.
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in schema.Fields.Reverse())
        {
            if (node is ComputedFieldNode computed)
            {
                if (!CollectReferences(computed.Expression, required))
                    break;
                continue;
            }
            if (node is not FieldDefinitionNode field)
                break;
            if (
                field.TypeAnnotation is ByteArrayTypeNode
                && field.Name != "_"
                && field.Constraint is null
                && field.ValueValidation is null
                && !required.Contains(field.Name)
            )
                eligible.Add(field.Name);

            if (
                !CollectReferences(field.WhenCondition, required)
                || !CollectReferences(field.AtOffset, required)
                || !CollectReferences(field.Constraint?.Expression, required)
                || (
                    field.ValueValidation is { } validation
                    && !validation.Values.All(value => CollectReferences(value, required))
                )
            )
                break;
            var known = field.TypeAnnotation switch
            {
                PrimitiveTypeNode or BitsTypeNode or AlignmentNode => true,
                ByteArrayTypeNode bytes => CollectReferences(bytes.SizeExpression, required),
                StringTypeNode { AsTextSchemaName: null } text => CollectReferences(text.SizeExpression, required),
                _ => false,
            };
            if (!known)
                break;
        }
        return eligible;
    }

    private static bool CollectReferences(Node? node, HashSet<string> required)
    {
        switch (node)
        {
            case null or NumericLiteralNode or BooleanNode or StringNode or NullNode:
                return true;
            case AccessObjectArrayNode or AccessObjectKeyNode:
                return false;
            case AccessColumnNode column:
                required.Add(column.Name);
                required.Add(column.Alias);
                return true;
            case IdentifierNode identifier:
                required.Add(identifier.Name);
                return true;
            case BinaryNode binary:
                return CollectReferences(binary.Left, required) && CollectReferences(binary.Right, required);
            case NotNode unary:
                return CollectReferences(unary.Expression, required);
            default:
                return false;
        }
    }
}
