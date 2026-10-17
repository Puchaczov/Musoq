using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class BinarySchemaRecordAliases
{
    internal static FieldDefinitionNode? Resolve(Node expression, IReadOnlyList<SchemaFieldNode> fields)
    {
        if (expression is WhenNode or ThenNode or ElseNode)
            return Resolve(((UnaryNode)expression).Expression, fields);
        if (expression is IdentifierNode identifier)
            return ResolveIdentifier(identifier.Name, fields);
        return expression is CaseNode choice ? ResolveChoice(choice, fields) : null;
    }

    private static FieldDefinitionNode? ResolveIdentifier(string name, IReadOnlyList<SchemaFieldNode> fields)
    {
        var field = fields.FirstOrDefault(candidate => candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (field is ComputedFieldNode computed)
            return Resolve(computed.Expression, fields.TakeWhile(candidate => candidate != computed).ToArray());
        return field is FieldDefinitionNode parsed && IsRecord(parsed.TypeAnnotation) ? parsed : null;
    }

    private static FieldDefinitionNode? ResolveChoice(CaseNode choice, IReadOnlyList<SchemaFieldNode> fields)
    {
        var branches = choice.WhenThenPairs.Select(pair => UnwrapBranch(pair.Then)).Append(UnwrapBranch(choice.Else))
            .Where(branch => branch is not NullNode).ToArray();
        var targets = branches.Select(branch => Resolve(branch, fields)).ToArray();
        if (targets.Length == 0 || targets.Any(target => target is null))
            return null;
        return targets.Select(target => Identity(target!.TypeAnnotation, target.Name)).Distinct(StringComparer.Ordinal).Count() == 1 ? targets[0] : null;
    }

    private static string Identity(TypeAnnotationNode annotation, string fieldName) => annotation switch
    {
        SchemaReferenceTypeNode reference => reference.FullTypeName,
        SubstreamTypeNode { Target: { } target } => Identity(target, fieldName),
        ArrayTypeNode array => Identity(array.ElementType, fieldName) + "[]",
        RepeatUntilTypeNode repeat => Identity(repeat.ElementType, fieldName) + "[]",
        _ => annotation.GetType().Name + ":" + fieldName
    };

    private static Node UnwrapBranch(Node expression) => expression is WhenNode or ThenNode or ElseNode
        ? ((UnaryNode)expression).Expression : expression;

    private static bool IsRecord(TypeAnnotationNode annotation) => annotation switch
    {
        SchemaReferenceTypeNode or InlineSchemaTypeNode or BinarySwitchTypeNode => true,
        SubstreamTypeNode { Target: { } target } => IsRecord(target),
        ArrayTypeNode array => IsRecord(array.ElementType),
        RepeatUntilTypeNode repeat => IsRecord(repeat.ElementType),
        _ => false
    };
}
