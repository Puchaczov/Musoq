using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal sealed class EmbeddedTextShapeBinding(SchemaRegistry registry)
{
    private readonly Dictionary<IReadOnlyList<SchemaFieldNode>, Dictionary<string, TextResultShape>> _members = new(
        ReferenceEqualityComparer.Instance
    );
    private readonly Dictionary<string, TextResultShape> _textSchemas = new(StringComparer.OrdinalIgnoreCase);

    internal TextResultShape? Record(SchemaReferenceTypeNode reference) =>
        registry.ParentBindings.FieldsFor(reference) is { } fields ? BinaryRecord(reference, Members(fields)) : null;

    internal TextResultShape? Expression(Node expression, IReadOnlyList<SchemaFieldNode> fields) =>
        Read(expression, Members(fields));

    private Dictionary<string, TextResultShape> Members(IReadOnlyList<SchemaFieldNode> fields)
    {
        if (_members.TryGetValue(fields, out var result))
            return result;
        result = new(StringComparer.Ordinal);
        _members.Add(fields, result);
        foreach (var field in fields)
        {
            if (field.Name == "_")
                continue;
            var value = field switch
            {
                FieldDefinitionNode parsed => Value(parsed.TypeAnnotation),
                ComputedFieldNode computed => Read(computed.Expression, result),
                _ => null,
            };
            if (value != null)
                result[field.Name] = value;
        }
        return result;
    }

    private TextResultShape? Value(TypeAnnotationNode annotation) =>
        annotation switch
        {
            StringTypeNode { AsTextSchemaName: { } name } => TextSchema(name),
            SchemaReferenceTypeNode reference => Record(reference),
            InlineSchemaTypeNode inline => BinaryRecord(inline, Members(inline.Fields)),
            BinarySwitchTypeNode choice => BinaryRecord(choice, Branches(choice)),
            SubstreamTypeNode { Target: { } target } => Value(target),
            ArrayTypeNode array => ArrayOf(Value(array.ElementType)),
            RepeatUntilTypeNode repeat => ArrayOf(Value(repeat.ElementType)),
            _ => null,
        };

    private Dictionary<string, TextResultShape> Branches(BinarySwitchTypeNode choice)
    {
        var branches = new Dictionary<string, TextResultShape>(StringComparer.Ordinal);
        foreach (var arm in choice.Cases)
            if (Value(arm.BranchType) is { } value)
                branches.Add(arm.BranchAlias, value);
        return branches;
    }

    private TextResultShape? BinaryRecord(TypeAnnotationNode type, Dictionary<string, TextResultShape> members) =>
        members.Count == 0
            ? null
            : new TextResultShape(
                typeof(object),
                InterpretationPropertyTypeNameResolver.ResolveBinaryTypeName(type, registry)
            )
            {
                Members = members,
            };

    private TextResultShape TextSchema(string name)
    {
        if (!_textSchemas.TryGetValue(name, out var schema))
            _textSchemas[name] = schema = new TextResultShapeBinding(registry).Schema(name);
        return schema;
    }

    private static TextResultShape? ArrayOf(TextResultShape? element) =>
        element is null
            ? null
            : new TextResultShape(element.ClrType.MakeArrayType(), element.TypeName is { } type ? type + "[]" : null)
            {
                Element = element,
            };

    private static TextResultShape? Read(Node expression, IReadOnlyDictionary<string, TextResultShape> members) =>
        expression switch
        {
            ArrayIndexNode index => Read(index.Array, members)?.Element,
            DotNode { Expression: IdentifierNode property } dot => Read(dot.Root, members)?.Member(property.Name),
            IdentifierNode column => members.GetValueOrDefault(column.Name),
            CaseNode choice => Choice(
                InterpretationPropertyBinding.CaseResults(choice.WhenThenPairs, choice.Else),
                members
            ),
            CoalesceNode fallback => Choice([fallback.Left, fallback.Right], members),
            WhenNode or ThenNode or ElseNode => Read(((UnaryNode)expression).Expression, members),
            _ => null,
        };

    private static TextResultShape? Choice(
        IEnumerable<Node> branches,
        IReadOnlyDictionary<string, TextResultShape> members
    )
    {
        var values = new List<TextResultShape>();
        foreach (var branch in branches)
        {
            if (branch is NullNode)
                continue;
            if (Read(branch, members) is not { } value)
                return null;
            values.Add(value);
        }
        return values.Count == 0 ? null : TextResultShape.Merge(values);
    }
}
