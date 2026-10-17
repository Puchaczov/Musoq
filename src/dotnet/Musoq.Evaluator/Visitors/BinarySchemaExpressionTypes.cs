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
        if (BinarySchemaRecordAliases.Resolve(expression, fields) is { } record &&
            record.TypeAnnotation is SchemaReferenceTypeNode reference &&
            registry?.TryGetSchema(reference.SchemaName, out var registration) == true && registration?.GeneratedType is { } generatedType)
            return generatedType;
        switch (expression)
        {
            case HexIntegerNode or BinaryIntegerNode or OctalIntegerNode:
                return typeof(int);
            case IdentifierNode identifier:
                return ResolveField(identifier.Name, fields, registry);
            case ArrayIndexNode indexed:
                var collection = Resolve(indexed.Array, fields, registry);
                return Lift(Unwrap(collection).GetElementType() ?? typeof(object), IsNullable(collection));
            case WordNode or StringNode:
                return typeof(string);
            case NullNode:
                return typeof(object);
            case WhenNode or ThenNode or ElseNode:
                return Resolve(((UnaryNode)expression).Expression, fields, registry);
            case CaseNode choice:
                return ResolveCase(choice, fields, registry);
            case CastNode cast:
                Resolve(cast.Expression, fields, registry);
                return BinarySchemaStrictCasts.Resolve(cast).ReturnType;
            case NotNode not:
                RequireBoolean(not.Expression, fields, registry);
                return Lift(typeof(bool), IsNullable(Resolve(not.Expression, fields, registry)));
            case DotNode dot:
                return ResolvePath(dot, fields, registry);
            case BinaryNode binary:
                return ResolveBinary(binary, fields, registry);
            case AccessMethodNode method when method.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase):
                return typeof(string);
            case AccessMethodNode method:
                return ResolveIntrinsicType(method);
            default:
                return expression.ReturnType is { } type && type != typeof(void) ? type : typeof(object);
        }
    }

    private static Type ResolveIntrinsicType(AccessMethodNode method) => method.Name.ToUpperInvariant() switch
    {
        "LENGTH" or "CRC32" or "ADLER32" or "INDEXOF" => typeof(int),
        "CRC16" => typeof(short),
        "MD5" or "SHA256" or "FROMHEX" => typeof(byte[]),
        "SUBSTRING" or "TOHEX" or "TOTEXT" => typeof(string),
        _ => method.ReturnType is { } type && type != typeof(void) ? type : typeof(object)
    };

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
        if (binary is AddNode or HyphenNode or StarNode or FSlashNode or ModuloNode)
            RequireCompatibleNumericOperands(left, right, binary);
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
        if (HasIndexedReceiver(expression.Root))
            return ResolveIndexedMember(expression, fields, registry);
        var parts = PathParts(expression).ToArray();
        if (parts.Length > 1 && parts[0].Equals("parent", StringComparison.OrdinalIgnoreCase) &&
            registry?.ParentBindings.ResolveType(fields, string.Join('.', parts), expression) is { } parentType)
            return parentType;
        IReadOnlyList<SchemaFieldNode>? currentFields = fields;
        var nullable = false;
        Type result = typeof(object);
        foreach (var part in parts)
        {
            var field = currentFields?.FirstOrDefault(candidate => string.Equals(candidate.Name, part, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                var property = Unwrap(result).GetProperty(part);
                if (property is null)
                    return typeof(object);
                result = property.PropertyType;
                currentFields = null;
                continue;
            }
            result = ResolveField(part, currentFields!, registry);
            nullable |= field.IsConditional || IsNullable(result);
            if (field is ComputedFieldNode computed)
                nullable |= MayBeNull(computed.Expression, currentFields!.TakeWhile(candidate => candidate != field).ToArray(), registry);
            var reference = field switch
            {
                FieldDefinitionNode { TypeAnnotation: SchemaReferenceTypeNode direct } => direct,
                FieldDefinitionNode { TypeAnnotation: SubstreamTypeNode { Target: SchemaReferenceTypeNode bounded } } => bounded,
                _ => null
            };
            if (reference is not null && registry?.TryGetSchema(reference.SchemaName, out var registration) == true && registration?.Node is BinarySchemaNode schema)
                currentFields = schema.Fields;
            else if (field is FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode inline })
                currentFields = inline.Fields;
            else
                currentFields = null;
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

    private static bool HasIndexedReceiver(Node expression) => expression is ArrayIndexNode ||
        expression is DotNode dot && HasIndexedReceiver(dot.Root);

    private static Type ResolveIndexedMember(DotNode expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        var annotation = ResolveAnnotation(expression.Root, fields, registry);
        var receiverFields = annotation is null ? null : RecordFields(annotation, registry);
        var nullable = MayBeNull(expression.Root, fields, registry);
        if (receiverFields is not null)
            return Lift(ResolveField(expression.Name, receiverFields, registry), nullable);
        var receiverType = Unwrap(Resolve(expression.Root, fields, registry));
        return Lift(receiverType.GetProperty(expression.Name)?.PropertyType ?? typeof(object), nullable);
    }

    private static TypeAnnotationNode? ResolveAnnotation(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        if (expression is ArrayIndexNode indexed)
            return ResolveAnnotation(indexed.Array, fields, registry) switch
            {
                ArrayTypeNode array => array.ElementType,
                RepeatUntilTypeNode repeat => repeat.ElementType,
                _ => null
            };
        if (expression is DotNode dot)
        {
            var receiver = ResolveAnnotation(dot.Root, fields, registry);
            var members = receiver is null ? null : RecordFields(receiver, registry);
            return members is null ? null : ResolveAnnotation(new IdentifierNode(dot.Name), members, registry);
        }
        if (expression is not IdentifierNode identifier)
            return null;
        var field = fields.FirstOrDefault(candidate => candidate.Name.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));
        return field switch
        {
            FieldDefinitionNode parsed => parsed.TypeAnnotation,
            ComputedFieldNode computed => ResolveAnnotation(computed.Expression, fields.TakeWhile(candidate => candidate != field).ToArray(), registry),
            _ => null
        };
    }

    private static void RequireCompatibleNumericOperands(Type left, Type right, Node expression)
    {
        left = Unwrap(left);
        right = Unwrap(right);
        if ((left == typeof(decimal) && (right == typeof(float) || right == typeof(double))) ||
            (right == typeof(decimal) && (left == typeof(float) || left == typeof(double))))
            throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
                "Decimal and floating point operands require an explicit conversion.", expression.SpanOrEmpty(),
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ3007_InvalidOperandTypes);
    }
    internal static bool MayBeNull(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        switch (expression)
        {
            case NullNode: return true;
            case AccessObjectArrayNode indexed:
                return IndexedAccessMayBeNull(indexed, fields, registry);
            case IdentifierNode identifier:
                var field = fields.FirstOrDefault(candidate => candidate.Name.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));
                if (field is ComputedFieldNode computed)
                    return MayBeNull(computed.Expression, fields.TakeWhile(candidate => candidate != field).ToArray(), registry);
                return field?.IsConditional == true;
            case ArrayIndexNode indexed:
                var indexedType = Resolve(indexed, fields, registry);
                var elementCanDefaultToNull = !indexedType.IsValueType ||
                                              Nullable.GetUnderlyingType(indexedType) != null;
                return elementCanDefaultToNull &&
                       (MayBeNull(indexed.Array, fields, registry) ||
                        !TryGetConstantIntegerIndex(indexed.Index, out var index) ||
                        !IsFixedIndexPresent(ResolveAnnotation(indexed.Array, fields, registry), index));
            case DotNode dot:
                if (HasNullableIndexedAccess(dot, fields, registry))
                    return true;
                var parts = PathParts(dot).ToArray();
                if (parts.LastOrDefault()?.Equals("Length", StringComparison.OrdinalIgnoreCase) == true &&
                    IndexedArrayMayBeAbsent(dot.Root, fields, registry))
                    return true;
                if (registry?.ParentBindings.ParentMayBeNull(fields, string.Join('.', parts), expression) is { } parentNullability)
                    return parentNullability;
                IReadOnlyList<SchemaFieldNode>? current = fields;
                BinarySwitchTypeNode? currentSwitch = null;
                foreach (var part in parts)
                {
                    if (currentSwitch?.Cases.Any(branch => branch.BranchAlias.Equals(part, StringComparison.OrdinalIgnoreCase)) == true)
                        return true;
                    if (current is null)
                        return false;
                    var member = current.FirstOrDefault(candidate => candidate.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                    if (member?.IsConditional == true) return true;
                    if (member is ComputedFieldNode value)
                    {
                        if (MayBeNull(value.Expression, current.TakeWhile(candidate => candidate != member).ToArray(), registry))
                            return true;
                        member = BinarySchemaRecordAliases.Resolve(value.Expression, current);
                    }
                    var annotation = (member as FieldDefinitionNode)?.TypeAnnotation;
                    currentSwitch = annotation is null ? null : FindSwitch(annotation);
                    current = annotation is null ? null : RecordFields(annotation, registry);
                }
                return false;
            case BinaryNode binary: return MayBeNull(binary.Left, fields, registry) || MayBeNull(binary.Right, fields, registry);
            case CastNode cast: return IsNullable(Resolve(cast, fields, registry)) || MayBeNull(cast.Expression, fields, registry);
            case UnaryNode unary: return MayBeNull(unary.Expression, fields, registry);
            case CaseNode choice: return choice.WhenThenPairs.Any(pair => MayBeNull(pair.Then, fields, registry)) || MayBeNull(choice.Else, fields, registry);
            default: return IsNullable(Resolve(expression, fields, registry));
        }
    }

    private static BinarySwitchTypeNode? FindSwitch(TypeAnnotationNode annotation) => annotation switch
    {
        BinarySwitchTypeNode tagged => tagged,
        ArrayTypeNode array => FindSwitch(array.ElementType),
        RepeatUntilTypeNode repeat => FindSwitch(repeat.ElementType),
        SubstreamTypeNode { Target: { } target } => FindSwitch(target),
        _ => null
    };

    private static bool IsIndexedArray(Node expression)
    {
        if (expression.ReturnType?.IsArray != true)
            return false;
        return expression is AccessObjectArrayNode or ArrayIndexNode ||
            expression is DotNode dot && (IsIndexedArray(dot.Root) || IsIndexedArray(dot.Expression));
    }

    private static bool IndexedArrayMayBeAbsent(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        if (!IsIndexedArray(expression))
            return false;
        if (expression is AccessObjectArrayNode indexed)
        {
            var annotation = ResolveAnnotation(new IdentifierNode(indexed.ObjectName), fields, registry);
            return indexed.Token.IsOutOfRange || !IsFixedIndexPresent(annotation, indexed.Token.Index);
        }
        if (expression is DotNode { Expression: AccessObjectArrayNode member } dot)
        {
            var receiver = ResolveAnnotation(dot.Root, fields, registry);
            var members = receiver is null ? null : RecordFields(receiver, registry);
            return members is null || IndexedArrayMayBeAbsent(member, members, registry);
        }
        return true;
    }

    private static bool IsFixedIndexPresent(TypeAnnotationNode? annotation, int index)
    {
        if (annotation is not ArrayTypeNode { SizeExpression: IntegerNode count } ||
            !int.TryParse(Convert.ToString(count.ObjValue, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var length))
            return false;
        return length > 0 && (index < 0 || index < length);
    }

    private static bool IndexedAccessMayBeNull(
        AccessObjectArrayNode indexed,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry)
    {
        var array = new IdentifierNode(indexed.ObjectName);
        var annotation = ResolveAnnotation(array, fields, registry);
        if (annotation is not (ArrayTypeNode or RepeatUntilTypeNode))
            return false;

        var elementType = ResolveIndexedElementType(annotation, indexed.ReturnType);
        var elementCanDefaultToNull = !elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null;
        if (!elementCanDefaultToNull)
            return false;

        return indexed.Token.IsOutOfRange ||
               !IsFixedIndexPresent(annotation, indexed.Token.Index);
    }

    private static Type ResolveIndexedElementType(TypeAnnotationNode? arrayAnnotation, Type fallback) => arrayAnnotation switch
    {
        ArrayTypeNode array => array.ElementType.ClrType,
        RepeatUntilTypeNode repeat => repeat.ElementType.ClrType,
        _ => fallback
    };

    private static bool HasNullableIndexedAccess(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry) => expression switch
    {
        AccessObjectArrayNode indexed => IndexedAccessMayBeNull(indexed, fields, registry),
        ArrayIndexNode indexed => MayBeNull(indexed, fields, registry),
        DotNode dot => HasNullableIndexedAccess(dot.Root, fields, registry) ||
                       HasNullableIndexedAccess(dot.Expression, fields, registry),
        _ => false
    };

    private static bool TryGetConstantIntegerIndex(Node expression, out int index)
    {
        if ((expression is IntegerNode or HexIntegerNode or BinaryIntegerNode or OctalIntegerNode) &&
            int.TryParse(
                Convert.ToString(((ConstantValueNode)expression).ObjValue, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out index))
            return true;

        index = default;
        return false;
    }

    private static IReadOnlyList<SchemaFieldNode>? RecordFields(TypeAnnotationNode annotation, SchemaRegistry? registry) => annotation switch
    {
        ArrayTypeNode array => RecordFields(array.ElementType, registry),
        RepeatUntilTypeNode repeat => RecordFields(repeat.ElementType, registry),
        SubstreamTypeNode { Target: { } target } => RecordFields(target, registry),
        SchemaReferenceTypeNode reference => registry?.ParentBindings.Find(reference.SchemaName)?.Fields,
        InlineSchemaTypeNode inline => inline.Fields,
        _ => null
    };

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
