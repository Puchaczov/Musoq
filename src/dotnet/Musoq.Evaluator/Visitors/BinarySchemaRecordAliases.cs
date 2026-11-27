using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class BinarySchemaRecordAliases
{
    internal static FieldDefinitionNode? Resolve(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        if (expression is WhenNode or ThenNode or ElseNode)
            return Resolve(((UnaryNode)expression).Expression, fields, registry);
        if (expression is IdentifierNode identifier)
            return ResolveIdentifier(identifier, fields, registry);
        if (expression is ArrayIndexNode indexed && Resolve(indexed.Array, fields, registry) is { } collection)
        {
            var element = collection.TypeAnnotation switch
            {
                ArrayTypeNode array => array.ElementType,
                RepeatUntilTypeNode repeat => repeat.ElementType,
                _ => null,
            };
            return element is not null && IsRecord(element) ? new FieldDefinitionNode(collection.Name, element) : null;
        }
        if (expression is DotNode dot)
        {
            if (registry?.ParentBindings.ResolveRecord(fields, expression) is { } parentRecord)
                return parentRecord;
            var receiver = Resolve(dot.Root, fields, registry);
            if (
                receiver is not null
                && dot.Expression is IdentifierNode member
                && BinarySchemaExpressionTypes.SwitchBranch(receiver.TypeAnnotation, member.Name) is { } branch
            )
                return IsRecord(branch) ? new FieldDefinitionNode(member.Name, branch) : null;
            return
                receiver is not null
                && BinarySchemaExpressionTypes.RecordFields(receiver.TypeAnnotation, registry) is { } members
                ? Resolve(dot.Expression, members, registry)
                : null;
        }
        return expression switch
        {
            CaseNode choice => ResolveChoice(
                InterpretationPropertyBinding.CaseResults(choice.WhenThenPairs, choice.Else),
                fields,
                registry
            ),
            CoalesceNode fallback => ResolveChoice([fallback.Left, fallback.Right], fields, registry),
            _ => null,
        };
    }

    private static FieldDefinitionNode? ResolveIdentifier(
        IdentifierNode identifier,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        var field = fields.FirstOrDefault(candidate =>
            candidate.Name.Equals(identifier.Name, StringComparison.Ordinal)
        );
        if (field is null)
            return registry?.ParentBindings.ResolveRecord(fields, identifier);
        if (field is ComputedFieldNode computed)
            return Resolve(
                computed.Expression,
                fields.TakeWhile(candidate => candidate != computed).ToArray(),
                registry
            );
        return field is FieldDefinitionNode parsed && IsRecord(parsed.TypeAnnotation) ? parsed : null;
    }

    private static FieldDefinitionNode? ResolveChoice(
        IEnumerable<Node> branches,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        var targets = branches
            .Where(branch => branch is not NullNode)
            .Select(branch => Resolve(branch, fields, registry))
            .ToArray();
        if (targets.Length == 0 || targets.Any(target => target is null))
            return null;
        var first = targets[0]!;
        var identity = Identity(first, registry);
        if (targets.Skip(1).Any(target => Identity(target!, registry) != identity))
            return null;
        // The shared element type does not imply equal counts across branches.
        return first.TypeAnnotation is ArrayTypeNode or RepeatUntilTypeNode
            ? targets.MinBy(target => BinarySchemaExpressionTypes.MinimumArrayLength(target!.TypeAnnotation))
            : first;
    }

    internal static string Identity(FieldDefinitionNode field, SchemaRegistry? registry) =>
        Identity(field.TypeAnnotation, field.Name, registry);

    private static string Identity(TypeAnnotationNode annotation, string fieldName, SchemaRegistry? registry) =>
        annotation switch
        {
            SchemaReferenceTypeNode reference => reference.FullTypeName,
            InlineSchemaTypeNode or BinarySwitchTypeNode when registry is not null => registry.BinaryCompositions.Name(
                annotation
            ),
            StringTypeNode { AsTextSchemaName: { } name } => name,
            SubstreamTypeNode { Target: { } target } => Identity(target, fieldName, registry),
            ArrayTypeNode array => Identity(array.ElementType, fieldName, registry) + "[]",
            RepeatUntilTypeNode repeat => Identity(repeat.ElementType, fieldName, registry) + "[]",
            _ => annotation.GetType().Name + ":" + fieldName,
        };

    private static bool IsRecord(TypeAnnotationNode annotation) =>
        annotation switch
        {
            SchemaReferenceTypeNode or InlineSchemaTypeNode or BinarySwitchTypeNode => true,
            StringTypeNode { AsTextSchemaName: not null } => true,
            SubstreamTypeNode { Target: { } target } => IsRecord(target),
            ArrayTypeNode array => IsRecord(array.ElementType),
            RepeatUntilTypeNode repeat => IsRecord(repeat.ElementType),
            _ => false,
        };
}
