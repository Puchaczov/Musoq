using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class BinarySchemaExpressionTypes
{
    private static Type ResolveField(
        string name,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry,
        IdentifierNode? reference = null
    )
    {
        var field = fields.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));
        return field switch
        {
            FieldDefinitionNode parsed => Lift(parsed.TypeAnnotation.ClrType, parsed.IsConditional),
            ComputedFieldNode computed => Resolve(
                computed.Expression,
                fields.TakeWhile(candidate => candidate != computed).ToArray(),
                registry
            ),
            null when reference != null => registry?.ParentBindings.ResolveType(fields, reference) ?? typeof(object),
            _ => typeof(object),
        };
    }

    internal static Type ResolveSwitchSelector(
        BinarySwitchTypeNode choice,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry registry
    )
    {
        var type = Unwrap(Resolve(choice.SelectorExpression, fields, registry));
        if (
            type == typeof(object)
            && choice.SelectorExpression is not IdentifierNode
            && BinarySchemaRecordAliases.Resolve(choice.SelectorExpression, fields, registry) is null
        )
            throw new Exceptions.QuerySyntaxException(
                $"Switch selector '{choice.Selector}' does not identify a declared field.",
                choice.SelectorSpan,
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField
            );
        foreach (var arm in choice.Cases)
            if (arm.CaseValue is { } label && !BinarySchemaScalarLiteral.IsCompatibleSwitchLabel(type, label))
                throw new Exceptions.QuerySyntaxException(
                    $"Switch case label '{label}' is not compatible with selector '{choice.Selector}'.",
                    arm.CaseLabelSpan.IsEmpty ? label.Span : arm.CaseLabelSpan,
                    Musoq.Parser.Diagnostics.DiagnosticCode.MQ4013_InvalidSwitchCaseLabel
                );
        return type;
    }

    internal static Type RadixLiteralType(ConstantValueNode literal) =>
        literal.ObjValue is long value && value is < int.MinValue or > int.MaxValue ? typeof(long) : typeof(int);

    internal static Type Resolve(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry = null
    )
    {
        if (
            BinarySchemaRecordAliases.Resolve(expression, fields, registry) is { } record
            && record.TypeAnnotation is SchemaReferenceTypeNode reference
            && registry?.TryGetSchema(reference.SchemaName, out var registration) == true
            && registration?.GeneratedType is { } generatedType
        )
            return generatedType;
        switch (expression)
        {
            case NumericLiteralNode radix when radix is HexIntegerNode or BinaryIntegerNode or OctalIntegerNode:
                return RadixLiteralType(radix);
            case IdentifierNode identifier:
                return ResolveField(identifier.Name, fields, registry, identifier);
            case ArrayIndexNode indexed:
                var collection = Resolve(indexed.Array, fields, registry);
                return collection == typeof(string)
                    ? typeof(char)
                    : Lift(Unwrap(collection).GetElementType() ?? typeof(object), IsNullable(collection));
            case WordNode or StringNode:
                return typeof(string);
            case NullNode:
                return typeof(object);
            case WhenNode or ThenNode or ElseNode:
                return Resolve(((UnaryNode)expression).Expression, fields, registry);
            case CaseNode choice:
                return ResolveCase(choice, fields, registry);
            case IsNullNode predicate:
                Resolve(predicate.Expression, fields, registry);
                return typeof(bool);
            case CastNode cast:
                Resolve(cast.Expression, fields, registry);
                return BinarySchemaStrictCasts.Resolve(cast).ReturnType;
            case NotNode not:
                RequireBoolean(not.Expression, fields, registry);
                return Lift(typeof(bool), MayBeNull(not.Expression, fields, registry));
            case DotNode dot:
                return ResolvePath(dot, fields, registry);
            case StringConcatenationNode concatenation:
                RequireStringOperand(concatenation.Left, fields, registry);
                RequireStringOperand(concatenation.Right, fields, registry);
                return typeof(string);
            case CoalesceNode coalesce:
                return SchemaCoalesceExpression.Resolve(coalesce, fields, registry);
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

    private static Type ResolveIntrinsicType(AccessMethodNode method) =>
        method.Name.ToUpperInvariant() switch
        {
            "LENGTH" or "CRC32" or "ADLER32" or "INDEXOF" => typeof(int),
            "CRC16" => typeof(short),
            "MD5" or "SHA256" or "FROMHEX" => typeof(byte[]),
            "SUBSTRING" or "TOHEX" or "TOTEXT" => typeof(string),
            _ => method.ReturnType is { } type && type != typeof(void) ? type : typeof(object),
        };

    private static Type ResolveCase(CaseNode choice, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        foreach (var pair in choice.WhenThenPairs)
            RequireBoolean(pair.When, fields, registry);
        var branches = choice
            .WhenThenPairs.Select(pair => Resolve(pair.Then, fields, registry))
            .Append(Resolve(choice.Else, fields, registry))
            .ToArray();
        var common = branches.Where(type => type != typeof(object)).Select(Unwrap).Distinct().ToArray();
        var result = common.Length == 0 ? typeof(object) : common.Aggregate(CommonType);
        return Lift(result, branches.Any(type => type == typeof(object) || IsNullable(type)));
    }

    private static Type ResolveBinary(
        BinaryNode binary,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
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
        if (
            binary
            is EqualityNode
                or DiffNode
                or GreaterNode
                or GreaterOrEqualNode
                or LessNode
                or LessOrEqualNode
                or AndNode
                or OrNode
        )
            return Lift(typeof(bool), nullable);
        if (binary is AddNode && (left == typeof(string) || right == typeof(string)))
            return typeof(string);
        if (
            binary is AddNode or HyphenNode
            && (TypeConversionNodeFactory.IsDateTimeType(left) || TypeConversionNodeFactory.IsDateTimeType(right))
        )
        {
            var leftType = Unwrap(left);
            var rightType = Unwrap(right);
            var valid =
                binary is AddNode
                    ? TypeConversionNodeFactory.IsDateTimeType(leftType) && rightType == typeof(TimeSpan)
                    : BinaryOperatorTypeRules.CanApplySubtraction(leftType, rightType);
            if (!valid)
                throw new Exceptions.QuerySyntaxException(
                    $"The temporal operator cannot be applied to '{leftType.Name}' and '{rightType.Name}'.",
                    binary.SpanOrEmpty(),
                    Musoq.Parser.Diagnostics.DiagnosticCode.MQ3007_InvalidOperandTypes
                );
            return Lift(NodeHelpers.GetReturnTypeMap(leftType, rightType), nullable);
        }
        var firstOperand = Unwrap(left);
        var secondOperand = binary is LeftShiftNode or RightShiftNode ? firstOperand : Unwrap(right);
        if (
            BinaryOperatorTypeRules.IsIntegralType(firstOperand)
            && BinaryOperatorTypeRules.IsIntegralType(secondOperand)
        )
        {
            // Binary schemas also use unsigned 64-bit fields with integer masks and counts.
            var integralType =
                firstOperand == typeof(ulong) || secondOperand == typeof(ulong)
                    ? typeof(ulong)
                    : NodeHelpers.GetReturnTypeMap(firstOperand, secondOperand);
            return Lift(integralType, nullable);
        }
        var arithmeticType = BinaryOperatorTypeRules.GetWiderNumericType(firstOperand, secondOperand);
        if (
            arithmeticType == typeof(byte)
            || arithmeticType == typeof(sbyte)
            || arithmeticType == typeof(short)
            || arithmeticType == typeof(ushort)
        )
            arithmeticType = typeof(int);
        return Lift(arithmeticType, nullable);
    }

    private static Type ResolvePath(DotNode expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        if (!IsFieldPath(expression.Root))
            return ResolveExpressionMember(expression, fields, registry);
        var parts = PathParts(expression).ToArray();
        if (registry?.ParentBindings.ResolveType(fields, expression) is { } parentType)
            return parentType;
        IReadOnlyList<SchemaFieldNode>? currentFields = fields;
        TextResultShape? textShape = null;
        BinarySwitchTypeNode? currentSwitch = null;
        var nullable = false;
        Type result = typeof(object);
        foreach (var part in parts)
        {
            if (textShape?.Member(part) is { } textMember)
            {
                textShape = textMember;
                nullable |= textMember.MayBeNull;
                result = textMember.ClrType;
                continue;
            }
            textShape = null;
            TypeAnnotationNode? annotation;
            if (currentSwitch is not null)
            {
                annotation = SwitchBranch(currentSwitch, part);
                if (annotation is null && part != "Case")
                    return typeof(object);
                result = annotation?.ClrType ?? typeof(string);
                nullable |= annotation is not null;
            }
            else
            {
                var field = currentFields?.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, part, StringComparison.Ordinal)
                );
                if (field is null)
                {
                    if (NativeMemberType(Unwrap(result), part) is not { } memberType)
                        return typeof(object);
                    result = memberType;
                    currentFields = null;
                    continue;
                }
                result = ResolveField(part, currentFields!, registry);
                nullable |= field.IsConditional || IsNullable(result);
                if (field is ComputedFieldNode computed)
                {
                    var preceding = currentFields!.TakeWhile(candidate => candidate != field).ToArray();
                    nullable |= MayBeNull(computed.Expression, preceding, registry);
                    field = BinarySchemaRecordAliases.Resolve(computed.Expression, preceding, registry);
                }
                annotation = (field as FieldDefinitionNode)?.TypeAnnotation;
            }
            textShape = EmbeddedTextShape(annotation, registry);
            if (textShape is not null)
                result = textShape.ClrType;
            currentSwitch = annotation is null ? null : FindSwitch(annotation);
            currentFields = annotation is SchemaReferenceTypeNode or InlineSchemaTypeNode or SubstreamTypeNode
                ? RecordFields(annotation, registry)
                : null;
        }
        return Lift(result, nullable);
    }

    private static TextResultShape? EmbeddedTextShape(TypeAnnotationNode? annotation, SchemaRegistry? registry) =>
        annotation is StringTypeNode { AsTextSchemaName: { } name } && registry != null
            ? new TextResultShapeBinding(registry).Schema(name)
            : null;

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

    internal static bool IsFieldPath(Node expression) =>
        expression is IdentifierNode or PropertyValueNode
        || expression is DotNode dot && IsFieldPath(dot.Root) && IsFieldPath(dot.Expression);

    private static Type ResolveExpressionMember(
        DotNode expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        if (registry?.EmbeddedTextShapes?.Expression(expression, fields) is { } value)
            return Lift(value.ClrType, value.MayBeNull || MayBeNull(expression.Root, fields, registry));
        var annotation = ResolveAnnotation(expression.Root, fields, registry);
        var receiverFields = annotation is null ? null : RecordFields(annotation, registry);
        var nullable = MayBeNull(expression.Root, fields, registry);
        if (receiverFields is not null)
            return Lift(ResolveField(MemberName(expression), receiverFields, registry), nullable);
        var receiverType = Unwrap(Resolve(expression.Root, fields, registry));
        return Lift(NativeMemberType(receiverType, MemberName(expression)) ?? typeof(object), nullable);
    }

    private static Type? NativeMemberType(Type receiver, string member)
    {
        if (member == nameof(string.Length) && (receiver == typeof(string) || receiver.IsArray))
            return typeof(int);
        return SemanticColumnPropertyBindingService.TryResolveTypedProperty(receiver, member, out _)?.PropertyType;
    }

    private static string MemberName(DotNode expression) =>
        expression.Expression is IdentifierNode identifier ? identifier.Name : expression.Name;

    private static TypeAnnotationNode? ResolveAnnotation(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        if (expression is CaseNode or CoalesceNode)
            return BinarySchemaRecordAliases.Resolve(expression, fields, registry)?.TypeAnnotation;
        if (expression is ArrayIndexNode indexed)
            return ResolveAnnotation(indexed.Array, fields, registry) switch
            {
                ArrayTypeNode array => array.ElementType,
                RepeatUntilTypeNode repeat => repeat.ElementType,
                _ => null,
            };
        if (expression is DotNode dot)
        {
            if (BinarySchemaRecordAliases.Resolve(dot, fields, registry) is { } record)
                return record.TypeAnnotation;
            var receiver = ResolveAnnotation(dot.Root, fields, registry);
            if (receiver is not null && SwitchBranch(receiver, MemberName(dot)) is { } branch)
                return branch;
            var members = receiver is null ? null : RecordFields(receiver, registry);
            return members is null ? null : ResolveAnnotation(new IdentifierNode(MemberName(dot)), members, registry);
        }
        if (expression is not IdentifierNode identifier)
            return null;
        var field = fields.FirstOrDefault(candidate =>
            candidate.Name.Equals(identifier.Name, StringComparison.Ordinal)
        );
        return field switch
        {
            FieldDefinitionNode parsed => parsed.TypeAnnotation,
            ComputedFieldNode computed => ResolveAnnotation(
                computed.Expression,
                fields.TakeWhile(candidate => candidate != field).ToArray(),
                registry
            ),
            null => registry?.ParentBindings.ResolveRecord(fields, identifier)?.TypeAnnotation,
            _ => null,
        };
    }

    private static void RequireStringOperand(
        Node operand,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        if (operand is not NullNode && Resolve(operand, fields, registry) != typeof(string))
            throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
                "The || operator accepts strings or contextual NULL only.",
                operand.SpanOrEmpty(),
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ3007_InvalidOperandTypes
            );
    }

    private static void RequireCompatibleNumericOperands(Type left, Type right, Node expression)
    {
        left = Unwrap(left);
        right = Unwrap(right);
        if (
            (left == typeof(decimal) && (right == typeof(float) || right == typeof(double)))
            || (right == typeof(decimal) && (left == typeof(float) || left == typeof(double)))
        )
            throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
                "Decimal and floating point operands require an explicit conversion.",
                expression.SpanOrEmpty(),
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ3007_InvalidOperandTypes
            );
    }

    internal static bool MayBeNull(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        switch (expression)
        {
            case NullNode:
                return true;
            case AccessObjectArrayNode indexed:
                return IndexedAccessMayBeNull(indexed, fields, registry);
            case IdentifierNode identifier:
                var field = fields.FirstOrDefault(candidate =>
                    candidate.Name.Equals(identifier.Name, StringComparison.Ordinal)
                );
                if (field is ComputedFieldNode computed)
                    return MayBeNull(
                        computed.Expression,
                        fields.TakeWhile(candidate => candidate != field).ToArray(),
                        registry
                    );
                return field?.IsConditional ?? (registry?.ParentBindings.ParentMayBeNull(fields, identifier) == true);
            case ArrayIndexNode indexed:
                var indexedType = Resolve(indexed, fields, registry);
                var elementCanDefaultToNull =
                    !indexedType.IsValueType || Nullable.GetUnderlyingType(indexedType) != null;
                return elementCanDefaultToNull
                    && (
                        MayBeNull(indexed.Array, fields, registry)
                        || !TryGetConstantIntegerIndex(indexed.Index, out var index)
                        || !IsFixedIndexPresent(ResolveAnnotation(indexed.Array, fields, registry), index)
                    );
            case DotNode dot:
                if (!IsFieldPath(dot.Root) && MayBeNull(dot.Root, fields, registry))
                    return true;
                if (HasNullableIndexedAccess(dot, fields, registry))
                    return true;
                var parts = PathParts(dot).ToArray();
                if (
                    parts.LastOrDefault()?.Equals("Length", StringComparison.OrdinalIgnoreCase) == true
                    && IndexedArrayMayBeAbsent(dot.Root, fields, registry)
                )
                    return true;
                if (registry?.ParentBindings.ParentMayBeNull(fields, expression) is { } parentNullability)
                    return parentNullability;
                IReadOnlyList<SchemaFieldNode>? current = fields;
                BinarySwitchTypeNode? currentSwitch = null;
                TextResultShape? textShape = null;
                foreach (var part in parts)
                {
                    if (textShape != null)
                    {
                        textShape = textShape.Member(part);
                        if (textShape?.MayBeNull == true)
                            return true;
                        continue;
                    }
                    if (
                        currentSwitch?.Cases.Any(branch =>
                            branch.BranchAlias.Equals(part, StringComparison.OrdinalIgnoreCase)
                        ) == true
                    )
                        return true;
                    if (current is null)
                        return false;
                    var member = current.FirstOrDefault(candidate =>
                        candidate.Name.Equals(part, StringComparison.Ordinal)
                    );
                    if (member?.IsConditional == true)
                        return true;
                    if (member is ComputedFieldNode value)
                    {
                        if (
                            MayBeNull(
                                value.Expression,
                                current.TakeWhile(candidate => candidate != member).ToArray(),
                                registry
                            )
                        )
                            return true;
                        member = BinarySchemaRecordAliases.Resolve(value.Expression, current, registry);
                    }
                    var annotation = (member as FieldDefinitionNode)?.TypeAnnotation;
                    textShape = EmbeddedTextShape(annotation, registry);
                    currentSwitch = annotation is null ? null : FindSwitch(annotation);
                    current = annotation is null ? null : RecordFields(annotation, registry);
                }
                return false;
            case CoalesceNode coalesce:
                return MayBeNull(coalesce.Left, fields, registry) && MayBeNull(coalesce.Right, fields, registry);
            case BinaryNode binary:
                return MayBeNull(binary.Left, fields, registry) || MayBeNull(binary.Right, fields, registry);
            case CastNode cast:
                return IsNullable(Resolve(cast, fields, registry)) || MayBeNull(cast.Expression, fields, registry);
            case UnaryNode unary:
                return MayBeNull(unary.Expression, fields, registry);
            case CaseNode choice:
                return choice.WhenThenPairs.Any(pair => MayBeNull(pair.Then, fields, registry))
                    || MayBeNull(choice.Else, fields, registry);
            default:
                return IsNullable(Resolve(expression, fields, registry));
        }
    }

    internal static TypeAnnotationNode? SwitchBranch(TypeAnnotationNode receiver, string name) =>
        FindSwitch(receiver)?.Cases.FirstOrDefault(branch => branch.BranchAlias == name)?.BranchType;

    private static BinarySwitchTypeNode? FindSwitch(TypeAnnotationNode annotation) =>
        annotation switch
        {
            BinarySwitchTypeNode tagged => tagged,
            ArrayTypeNode array => FindSwitch(array.ElementType),
            RepeatUntilTypeNode repeat => FindSwitch(repeat.ElementType),
            SubstreamTypeNode { Target: { } target } => FindSwitch(target),
            _ => null,
        };

    private static bool IsIndexedArray(Node expression)
    {
        if (expression.ReturnType?.IsArray != true)
            return false;
        return expression is AccessObjectArrayNode or ArrayIndexNode
            || expression is DotNode dot && (IsIndexedArray(dot.Root) || IsIndexedArray(dot.Expression));
    }

    private static bool IndexedArrayMayBeAbsent(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
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

    internal static int MinimumArrayLength(TypeAnnotationNode? annotation) =>
        annotation is ArrayTypeNode { SizeExpression: { } size } && TryGetConstantIntegerIndex(size, out var length)
            ? Math.Max(0, length)
            : 0;

    internal static int MinimumArrayLength(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    ) => MinimumArrayLength(ResolveAnnotation(expression, fields, registry));

    private static bool IsFixedIndexPresent(TypeAnnotationNode? annotation, int index)
    {
        var length = MinimumArrayLength(annotation);
        return length > 0 && index < length;
    }

    private static bool IndexedAccessMayBeNull(
        AccessObjectArrayNode indexed,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    )
    {
        var array = new IdentifierNode(indexed.ObjectName);
        var annotation = ResolveAnnotation(array, fields, registry);
        if (annotation is not (ArrayTypeNode or RepeatUntilTypeNode))
            return false;

        var elementType = ResolveIndexedElementType(annotation, indexed.ReturnType);
        var elementCanDefaultToNull = !elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null;
        if (!elementCanDefaultToNull)
            return false;

        return indexed.Token.IsOutOfRange || !IsFixedIndexPresent(annotation, indexed.Token.Index);
    }

    private static Type ResolveIndexedElementType(TypeAnnotationNode? arrayAnnotation, Type fallback) =>
        arrayAnnotation switch
        {
            ArrayTypeNode array => array.ElementType.ClrType,
            RepeatUntilTypeNode repeat => repeat.ElementType.ClrType,
            _ => fallback,
        };

    private static bool HasNullableIndexedAccess(
        Node expression,
        IReadOnlyList<SchemaFieldNode> fields,
        SchemaRegistry? registry
    ) =>
        expression switch
        {
            AccessObjectArrayNode indexed => IndexedAccessMayBeNull(indexed, fields, registry),
            ArrayIndexNode indexed => MayBeNull(indexed, fields, registry),
            DotNode dot => HasNullableIndexedAccess(dot.Root, fields, registry)
                || HasNullableIndexedAccess(dot.Expression, fields, registry),
            _ => false,
        };

    internal static bool TryGetConstantIntegerIndex(Node expression, out int index)
    {
        if (
            (expression is IntegerNode or HexIntegerNode or BinaryIntegerNode or OctalIntegerNode)
            && int.TryParse(
                Convert.ToString(
                    ((ConstantValueNode)expression).ObjValue,
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out index
            )
        )
            return true;

        index = default;
        return false;
    }

    internal static IReadOnlyList<SchemaFieldNode>? RecordFields(
        TypeAnnotationNode annotation,
        SchemaRegistry? registry
    ) =>
        annotation switch
        {
            ArrayTypeNode array => RecordFields(array.ElementType, registry),
            RepeatUntilTypeNode repeat => RecordFields(repeat.ElementType, registry),
            SubstreamTypeNode { Target: { } target } => RecordFields(target, registry),
            SchemaReferenceTypeNode reference => registry?.ParentBindings.FieldsFor(reference),
            InlineSchemaTypeNode inline => inline.Fields,
            _ => null,
        };

    private static Type CommonType(Type left, Type right)
    {
        if (left == right)
            return left;
        if (BinaryOperatorTypeRules.IsNumericType(left) && BinaryOperatorTypeRules.IsNumericType(right))
            return BinaryOperatorTypeRules.GetWiderNumericType(left, right);
        throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
            "CASE branches must have compatible types.",
            Musoq.Parser.TextSpan.Empty,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint
        );
    }

    private static void RequireBoolean(Node expression, IReadOnlyList<SchemaFieldNode> fields, SchemaRegistry? registry)
    {
        if (Unwrap(Resolve(expression, fields, registry)) == typeof(bool) || expression is NullNode)
            return;
        throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
            "Logical operands and CASE conditions must be boolean.",
            expression.SpanOrEmpty(),
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint
        );
    }

    internal static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) is not null;

    internal static Type Lift(Type type, bool nullable) =>
        nullable && type.IsValueType && !IsNullable(type) ? typeof(Nullable<>).MakeGenericType(type) : type;
}
