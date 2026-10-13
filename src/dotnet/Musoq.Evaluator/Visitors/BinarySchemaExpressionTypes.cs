using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class BinarySchemaExpressionTypes
{
    private static Type ResolveField(string name, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        var field = fields.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
        return field switch
        {
            FieldDefinitionNode parsed => Lift(parsed.TypeAnnotation.ClrType, parsed.IsConditional),
            ComputedFieldNode computed => Resolve(computed.Expression, fields.TakeWhile(candidate => candidate != computed).ToArray(), registry),
            _ => typeof(object)
        };
    }

    internal static Type Resolve(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry = null)
    {
        switch (expression)
        {
            case HexIntegerNode or BinaryIntegerNode or OctalIntegerNode:
                return typeof(int);
            case IdentifierNode identifier:
                return ResolveField(identifier.Name, fields, registry);
            case WordNode or StringNode:
                return typeof(string);
            case NullNode:
                return typeof(object);
            case WhenNode or ThenNode or ElseNode:
                return Resolve(((UnaryNode)expression).Expression, fields, registry);
            case CaseNode choice:
                return ResolveCase(choice, fields, registry);
            case NotNode not:
                RequireBoolean(not.Expression, fields, registry);
                return Lift(typeof(bool), IsNullable(Resolve(not.Expression, fields, registry)));
            case DotNode dot:
                return ResolvePath(dot, fields, registry);
            case BinaryNode binary:
                return ResolveBinary(binary, fields, registry);
            case AccessMethodNode method when method.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase):
                return typeof(string);
            default:
                return expression.ReturnType is { } type && type != typeof(void) ? type : typeof(object);
        }
    }

    private static Type ResolveCase(CaseNode choice, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        foreach (var pair in choice.WhenThenPairs)
            RequireBoolean(pair.When, fields, registry);
        var branches = choice.WhenThenPairs.Select(pair => Resolve(pair.Then, fields, registry))
            .Append(Resolve(choice.Else, fields, registry)).ToArray();
        var common = branches.Where(type => type != typeof(object)).Select(Unwrap).Distinct().ToArray();
        var result = common.Length == 0 ? typeof(object) : common.Aggregate(CommonType);
        return Lift(result, branches.Any(type => type == typeof(object) || IsNullable(type)));
    }

    private static Type ResolveBinary(BinaryNode binary, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        if (binary is AndNode or OrNode)
        {
            RequireBoolean(binary.Left, fields, registry);
            RequireBoolean(binary.Right, fields, registry);
        }
        var left = Resolve(binary.Left, fields, registry);
        var right = Resolve(binary.Right, fields, registry);
        var nullable = MayBeNull(binary.Left, fields, registry) || MayBeNull(binary.Right, fields, registry);
        if (binary is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode or LessNode or LessOrEqualNode or AndNode or OrNode)
            return Lift(typeof(bool), nullable);
        if (binary is AddNode && (left == typeof(string) || right == typeof(string)))
            return typeof(string);
        var arithmeticType = BinaryOperatorTypeRules.GetWiderNumericType(Unwrap(left), Unwrap(right));
        if (arithmeticType == typeof(byte) || arithmeticType == typeof(sbyte) || arithmeticType == typeof(short) || arithmeticType == typeof(ushort))
            arithmeticType = typeof(int);
        return Lift(arithmeticType, nullable);
    }

    private static Type ResolvePath(DotNode expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        var parts = PathParts(expression).ToArray();
        if (parts.Length > 1 && parts[0].Equals("parent", StringComparison.OrdinalIgnoreCase) &&
            registry?.ParentBindings.ResolveType(fields, string.Join('.', parts), expression) is { } parentType)
            return parentType;
        var currentFields = fields;
        var nullable = false;
        Type result = typeof(object);
        foreach (var part in parts)
        {
            var field = currentFields.FirstOrDefault(candidate => string.Equals(candidate.Name, part, StringComparison.OrdinalIgnoreCase));
            if (field is null)
                return typeof(object);
            result = ResolveField(part, currentFields, registry);
            nullable |= field.IsConditional || IsNullable(result);
            if (field is FieldDefinitionNode { TypeAnnotation: SchemaReferenceTypeNode reference } &&
                registry?.TryGetSchema(reference.SchemaName, out var registration) == true && registration?.Node is BinarySchemaNode schema)
                currentFields = schema.Fields;
            else if (field is FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode inline })
                currentFields = inline.Fields;
        }
        return Lift(result, nullable);
    }

    internal static IEnumerable<string> PathParts(Node node)
    {
        if (node is DotNode dot)
        {
            foreach (var part in PathParts(dot.Root))
                yield return part;
            foreach (var part in PathParts(dot.Expression))
                yield return part;
        }
        else if (node is IdentifierNode identifier)
            yield return identifier.Name;
        else if (node is PropertyValueNode property)
            yield return property.Name;
    }

    internal static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;
    internal static bool MayBeNull(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        switch (expression)
        {
            case NullNode: return true;
            case IdentifierNode identifier:
                var field = fields.FirstOrDefault(candidate => candidate.Name.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));
                if (field is ComputedFieldNode computed)
                    return MayBeNull(computed.Expression, fields.TakeWhile(candidate => candidate != field).ToArray(), registry);
                return field?.IsConditional == true;
            case DotNode dot:
                var parts = PathParts(dot).ToArray();
                if (registry?.ParentBindings.ParentMayBeNull(fields, string.Join('.', parts), expression) is { } parentNullability)
                    return parentNullability;
                var current = fields;
                foreach (var part in parts)
                {
                    var member = current.FirstOrDefault(candidate => candidate.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                    if (member?.IsConditional == true) return true;
                    if (member is FieldDefinitionNode { TypeAnnotation: SchemaReferenceTypeNode reference } &&
                        registry?.ParentBindings.Find(reference.SchemaName) is { } nested) current = nested.Fields;
                    if (member is FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode inline }) current = inline.Fields;
                    if (member is ComputedFieldNode value && MayBeNull(value.Expression, current, registry)) return true;
                }
                return false;
            case BinaryNode binary: return MayBeNull(binary.Left, fields, registry) || MayBeNull(binary.Right, fields, registry);
            case UnaryNode unary: return MayBeNull(unary.Expression, fields, registry);
            case CaseNode choice: return choice.WhenThenPairs.Any(pair => MayBeNull(pair.Then, fields, registry)) || MayBeNull(choice.Else, fields, registry);
            default: return IsNullable(Resolve(expression, fields, registry));
        }
    }
    private static Type CommonType(Type left, Type right)
    {
        if (left == right)
            return left;
        if (BinaryOperatorTypeRules.IsNumericType(left) && BinaryOperatorTypeRules.IsNumericType(right))
            return BinaryOperatorTypeRules.GetWiderNumericType(left, right);
        throw new Musoq.Evaluator.Exceptions.QuerySyntaxException("CASE branches must have compatible types.",
            Musoq.Parser.TextSpan.Empty, Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint);
    }

    private static void RequireBoolean(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        if (Unwrap(Resolve(expression, fields, registry)) == typeof(bool) || expression is NullNode)
            return;
        throw new Musoq.Evaluator.Exceptions.QuerySyntaxException("Logical operands and CASE conditions must be boolean.",
            expression.SpanOrEmpty(), Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint);
    }
    internal static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) is not null;
    internal static Type Lift(Type type, bool nullable) => nullable && type.IsValueType && !IsNullable(type)
        ? typeof(Nullable<>).MakeGenericType(type)
        : type;
}
