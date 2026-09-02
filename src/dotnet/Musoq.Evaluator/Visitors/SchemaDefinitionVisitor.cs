using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Visitor that extracts schema definitions from the AST and registers them in a SchemaRegistry.
///     This visitor processes the AST before query execution to collect all schema definitions.
/// </summary>
public class SchemaDefinitionVisitor : NoOpExpressionVisitor
{
    /// <summary>
    ///     Creates a new schema definition visitor.
    /// </summary>
    /// <param name="registry">The registry to populate with schema definitions.</param>
    public SchemaDefinitionVisitor(SchemaRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    ///     Gets the schema registry populated by this visitor.
    /// </summary>
    public SchemaRegistry Registry { get; }

    /// <summary>
    ///     Visits a binary schema node and registers it.
    /// </summary>
    public override void Visit(BinarySchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Registry.Register(node.Name, node);


        ValidateGenericParameters(node);
        var typeParameters = new HashSet<string>(node.TypeParameters, StringComparer.OrdinalIgnoreCase);
        var declaredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaredFieldTypes = new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);
        AddInheritedFields(
            node.Extends,
            node.Name,
            declaredFields,
            declaredFieldTypes,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { node.Name });

        var localFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in node.Fields)
        {
            ValidateDuplicateField(field, node.Name, localFields);

            if (field is FieldDefinitionNode parsedField)
            {
                ValidateTypeReferences(parsedField.TypeAnnotation, node.Name, typeParameters);
                ValidateFieldReferences(parsedField, node.Name, declaredFields, declaredFieldTypes);

                declaredFields.Add(parsedField.Name);
                declaredFieldTypes[parsedField.Name] = parsedField.TypeAnnotation.ReturnType;
            }
            else
            {
                if (field is ComputedFieldNode computedField)
                    ValidateExpressionReferences(field.Name, computedField.Expression, node.Name, declaredFields);

                declaredFields.Add(field.Name);
                declaredFieldTypes[field.Name] = ResolveExpressionType(
                    (field as ComputedFieldNode)?.Expression,
                    declaredFieldTypes);
            }
        }
    }

    /// <summary>
    ///     Visits a text schema node and registers it.
    /// </summary>
    public override void Visit(TextSchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Registry.Register(node.Name, node);

        if (!string.IsNullOrWhiteSpace(node.Extends))
        {
            ValidateTextFieldSchemaReference(
                node.Extends,
                node.Name,
                node.ExtendsSpan.IsEmpty ? node.SpanOrEmpty() : node.ExtendsSpan,
                "base schema");
        }

        foreach (var field in node.Fields)
        {
            if (field.FieldType == TextFieldType.SchemaReference)
            {
                if (string.IsNullOrWhiteSpace(field.PrimaryValue))
                    throw InvalidTextSchema(
                        $"Text schema reference field '{field.Name}' must specify a referenced text schema.",
                        field.SpanOrEmpty());

                ValidateTextFieldSchemaReference(
                    field.PrimaryValue!,
                    node.Name,
                    field.SpanOrEmpty(),
                    $"schema reference field '{field.Name}'");
                continue;
            }

            if (field.FieldType == TextFieldType.Repeat)
            {
                if (string.IsNullOrWhiteSpace(field.PrimaryValue))
                    throw InvalidTextSchema(
                        $"Text repeat field '{field.Name}' must specify a referenced text schema.",
                        field.SpanOrEmpty());

                ValidateTextFieldSchemaReference(
                    field.PrimaryValue!,
                    node.Name,
                    field.SpanOrEmpty(),
                    $"repeat field '{field.Name}'");
                continue;
            }

            if (field.FieldType != TextFieldType.Switch)
                continue;

            if (field.SwitchCases.Length == 0)
                throw InvalidTextSchema(
                    $"Text switch field '{field.Name}' must contain at least one case.",
                    field.SpanOrEmpty());

            var seenDefault = false;
            foreach (var switchCase in field.SwitchCases)
            {
                if (switchCase.IsDefault)
                {
                    if (seenDefault)
                        throw InvalidTextSchema(
                            $"Text switch field '{field.Name}' may contain only one default case.",
                            field.SpanOrEmpty());

                    seenDefault = true;
                }
                else if (seenDefault)
                {
                    throw InvalidTextSchema(
                        $"Text switch field '{field.Name}' must place the default case after all pattern cases.",
                        field.SpanOrEmpty());
                }

                ValidateTextFieldSchemaReference(
                    switchCase.TypeName,
                    node.Name,
                    field.SpanOrEmpty(),
                    $"switch field '{field.Name}'");
            }
        }
    }

    private void ValidateTextFieldSchemaReference(
        string referencedName,
        string referencingName,
        TextSpan span,
        string context)
    {
        if (string.Equals(referencedName, referencingName, StringComparison.OrdinalIgnoreCase))
            throw new QuerySyntaxException(
                $"Text {context} cannot reference schema '{referencedName}' from schema '{referencingName}' because recursive schema definitions are not supported.",
                span,
                DiagnosticCode.MQ4004_CircularSchemaReference);

        if (!Registry.TryGetSchema(referencedName, out var registration) || registration == null)
            throw new QuerySyntaxException(
                $"Text {context} references undefined schema '{referencedName}'.",
                span,
                DiagnosticCode.MQ4003_UndefinedSchemaReference);

        var referencedIndex = Registry.Schemas.ToList().FindIndex(registrationItem =>
            ReferenceEquals(registrationItem, registration));
        var referencingIndex = Registry.Schemas.ToList().FindIndex(registrationItem =>
            string.Equals(registrationItem.Name, referencingName, StringComparison.OrdinalIgnoreCase));
        if (referencedIndex >= referencingIndex)
            throw new QuerySyntaxException(
                $"Text {context} references '{referencedName}', but referenced schemas must be defined before '{referencingName}'.",
                span,
                DiagnosticCode.MQ4003_UndefinedSchemaReference);

        if (registration.Node is not TextSchemaNode)
            throw new QuerySyntaxException(
                $"Text {context} must reference a text schema, but '{referencedName}' is not a text schema.",
                span,
                DiagnosticCode.MQ4007_InvalidSchemaFieldType);
    }

    private static QuerySyntaxException InvalidTextSchema(string message, TextSpan span)
    {
        return new QuerySyntaxException(message, span, DiagnosticCode.MQ4002_InvalidTextSchemaField);
    }

    private void ValidateTypeReferences(TypeAnnotationNode typeNode, string currentSchemaName,
        HashSet<string> typeParameters)
    {
        switch (typeNode)
        {
            case SchemaReferenceTypeNode refNode:
                ValidateSchemaReferenceType(refNode, currentSchemaName, typeParameters);
                break;

            case StringTypeNode { AsTextSchemaName: not null } stringType:
                ValidateTextSchemaReference(
                    stringType.AsTextSchemaName!,
                    currentSchemaName,
                    stringType.SpanOrEmpty());
                break;

            case ArrayTypeNode arrayNode:
                ValidateTypeReferences(arrayNode.ElementType, currentSchemaName, typeParameters);
                break;

            case RepeatUntilTypeNode repeatUntilNode:
                ValidateTypeReferences(repeatUntilNode.ElementType, currentSchemaName, typeParameters);
                break;

            case BinarySwitchTypeNode switchType:
                foreach (var switchCase in switchType.Cases)
                    ValidateTypeReferences(switchCase.BranchType, currentSchemaName, typeParameters);
                break;

            case InlineSchemaTypeNode inlineSchema:
                foreach (var field in inlineSchema.Fields)
                    if (field is FieldDefinitionNode inlineField)
                        ValidateTypeReferences(inlineField.TypeAnnotation, currentSchemaName, typeParameters);
                break;

            case SubstreamTypeNode { Target: not null } substream:
                ValidateTypeReferences(substream.Target, currentSchemaName, typeParameters);
                break;
        }
    }

    private void ValidateGenericParameters(BinarySchemaNode node)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var typeParameter in node.TypeParameters)
        {
            if (string.IsNullOrWhiteSpace(typeParameter) ||
                string.Equals(typeParameter, "_", StringComparison.Ordinal) ||
                IsPrimitiveSchemaTypeName(typeParameter))
                throw new QuerySyntaxException(
                    $"Generic type parameter '{typeParameter}' must be a schema identifier, not a primitive type or discard name.",
                    node.SpanOrEmpty(),
                    DiagnosticCode.MQ4007_InvalidSchemaFieldType);

            if (!names.Add(typeParameter))
                throw new QuerySyntaxException(
                    $"Generic type parameter '{typeParameter}' is declared more than once in schema '{node.Name}'.",
                    node.SpanOrEmpty(),
                    DiagnosticCode.MQ4007_InvalidSchemaFieldType);
        }
    }

    private static void ValidateDuplicateField(
        SchemaFieldNode field,
        string schemaName,
        ISet<string> localFields)
    {
        if (string.Equals(field.Name, "_", StringComparison.Ordinal) || localFields.Add(field.Name))
            return;

        throw new QuerySyntaxException(
            $"Binary schema '{schemaName}' declares field '{field.Name}' more than once.",
            field.SpanOrEmpty(),
            DiagnosticCode.MQ4008_DuplicateSchemaField);
    }

    private void ValidateSchemaReferenceType(
        SchemaReferenceTypeNode reference,
        string currentSchemaName,
        ISet<string> typeParameters)
    {
        var span = reference.SpanOrEmpty();
        if (typeParameters.Contains(reference.SchemaName))
        {
            if (reference.IsGenericInstantiation)
                throw InvalidGenericSchemaReference(
                    $"Generic type parameter '{reference.SchemaName}' cannot receive type arguments.",
                    span);

            return;
        }

        ValidateSchemaReference(reference.SchemaName, currentSchemaName, span);
        if (!Registry.TryGetSchema(reference.SchemaName, out var registration) || registration == null)
            return;

        if (!reference.IsGenericInstantiation)
        {
            if (registration.Node is BinarySchemaNode { IsGeneric: true } genericSchema)
                throw InvalidGenericSchemaReference(
                    $"Generic schema '{genericSchema.Name}' must be instantiated with {genericSchema.TypeParameters.Length} " +
                    $"schema type argument{(genericSchema.TypeParameters.Length == 1 ? string.Empty : "s")}.",
                    span);

            return;
        }

        if (registration.Node is not BinarySchemaNode binarySchema)
            throw InvalidGenericSchemaReference(
                $"Schema reference '{reference.FullTypeName}' supplies type arguments, but '{reference.SchemaName}' is not a binary schema.",
                span);

        if (!binarySchema.IsGeneric)
            throw InvalidGenericSchemaReference(
                $"Schema reference '{reference.FullTypeName}' supplies type arguments, but schema '{binarySchema.Name}' is not generic.",
                span);

        if (binarySchema.TypeParameters.Length != reference.TypeArguments.Length)
            throw InvalidGenericSchemaReference(
                $"Schema '{binarySchema.Name}' declares {binarySchema.TypeParameters.Length} type parameter(s), but " +
                $"reference '{reference.FullTypeName}' supplies {reference.TypeArguments.Length}.",
                span);

        foreach (var typeArgument in reference.TypeArguments)
            ValidateGenericTypeArgument(typeArgument, currentSchemaName, typeParameters, span);
    }

    private void ValidateGenericTypeArgument(
        string typeArgument,
        string currentSchemaName,
        ISet<string> typeParameters,
        TextSpan span)
    {
        var trimmedArgument = typeArgument.Trim();
        if (string.IsNullOrEmpty(trimmedArgument))
            throw InvalidGenericSchemaReference("Generic schema type arguments cannot be empty.", span);

        if (IsPrimitiveSchemaTypeName(trimmedArgument))
            throw InvalidGenericSchemaReference(
                $"Generic type argument '{trimmedArgument}' must name a binary schema; primitive types cannot be used as schema arguments.",
                span);

        var argumentReference = ParseGenericTypeArgument(trimmedArgument, span);
        if (typeParameters.Contains(argumentReference.SchemaName))
        {
            if (argumentReference.IsGenericInstantiation)
                throw InvalidGenericSchemaReference(
                    $"Generic type parameter '{argumentReference.SchemaName}' cannot receive type arguments.",
                    span);

            return;
        }

        ValidateSchemaReference(argumentReference.SchemaName, currentSchemaName, span);
        if (!Registry.TryGetSchema(argumentReference.SchemaName, out var registration) || registration == null)
            return;

        if (registration.Node is not BinarySchemaNode binarySchema)
            throw InvalidGenericSchemaReference(
                $"Generic type argument '{trimmedArgument}' must name a binary schema.",
                span);

        if (!argumentReference.IsGenericInstantiation)
        {
            if (binarySchema.IsGeneric)
                throw InvalidGenericSchemaReference(
                    $"Generic schema argument '{binarySchema.Name}' must be instantiated with {binarySchema.TypeParameters.Length} type parameter(s).",
                    span);

            return;
        }

        if (!binarySchema.IsGeneric)
            throw InvalidGenericSchemaReference(
                $"Schema argument '{argumentReference.FullTypeName}' supplies type arguments to non-generic schema '{binarySchema.Name}'.",
                span);

        if (binarySchema.TypeParameters.Length != argumentReference.TypeArguments.Length)
            throw InvalidGenericSchemaReference(
                $"Schema '{binarySchema.Name}' declares {binarySchema.TypeParameters.Length} type parameter(s), but " +
                $"argument '{argumentReference.FullTypeName}' supplies {argumentReference.TypeArguments.Length}.",
                span);

        foreach (var nestedArgument in argumentReference.TypeArguments)
            ValidateGenericTypeArgument(nestedArgument, currentSchemaName, typeParameters, span);
    }

    private static SchemaReferenceTypeNode ParseGenericTypeArgument(string typeArgument, TextSpan span)
    {
        var openIndex = typeArgument.IndexOf('<', StringComparison.Ordinal);
        if (openIndex < 0)
            return (SchemaReferenceTypeNode)new SchemaReferenceTypeNode(typeArgument).WithSpan(span);

        if (openIndex == 0 || !typeArgument.EndsWith('>'))
            throw InvalidGenericSchemaReference(
                $"Generic type argument '{typeArgument}' is not a valid schema reference.",
                span);

        var depth = 0;
        for (var index = openIndex; index < typeArgument.Length; index++)
        {
            if (typeArgument[index] == '<')
                depth++;
            else if (typeArgument[index] == '>')
                depth--;

            if (depth < 0)
                throw InvalidGenericSchemaReference(
                    $"Generic type argument '{typeArgument}' is not balanced.",
                    span);
        }

        if (depth != 0)
            throw InvalidGenericSchemaReference(
                $"Generic type argument '{typeArgument}' is not balanced.",
                span);

        var schemaName = typeArgument[..openIndex].Trim();
        var argumentText = typeArgument[(openIndex + 1)..^1];
        var typeArguments = SplitGenericTypeArguments(argumentText);
        if (typeArguments.Any(string.IsNullOrWhiteSpace))
            throw InvalidGenericSchemaReference(
                $"Generic type argument '{typeArgument}' contains an empty nested argument.",
                span);

        return (SchemaReferenceTypeNode)new SchemaReferenceTypeNode(schemaName, typeArguments).WithSpan(span);
    }

    private static string[] SplitGenericTypeArguments(string argumentText)
    {
        var arguments = new List<string>();
        var depth = 0;
        var start = 0;

        for (var index = 0; index < argumentText.Length; index++)
        {
            switch (argumentText[index])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth == 0:
                    arguments.Add(argumentText[start..index].Trim());
                    start = index + 1;
                    break;
            }

            if (depth < 0)
                break;
        }

        arguments.Add(argumentText[start..].Trim());
        return arguments.ToArray();
    }

    private void ValidateTextSchemaReference(string schemaName, string currentSchemaName, TextSpan span)
    {
        ValidateSchemaReference(schemaName, currentSchemaName, span);
        if (Registry.TryGetSchema(schemaName, out var registration) &&
            registration?.Node is not TextSchemaNode)
            throw InvalidGenericSchemaReference(
                $"Binary string field text schema '{schemaName}' must reference a text schema.",
                span);
    }

    private static QuerySyntaxException InvalidGenericSchemaReference(string message, TextSpan span)
    {
        return new QuerySyntaxException(message, span, DiagnosticCode.MQ4007_InvalidSchemaFieldType);
    }

    private static bool IsPrimitiveSchemaTypeName(string typeName)
    {
        return typeName.Trim().ToUpperInvariant() is
            "BYTE" or "SBYTE" or "SHORT" or "USHORT" or "INT" or "UINT" or
            "LONG" or "ULONG" or "FLOAT" or "DOUBLE" or "STRING" or "BITS" or "ALIGN";
    }

    private void ValidateSchemaReference(string referencedName, string referencingName, TextSpan span)
    {
        try
        {
            Registry.ValidateReference(referencedName, referencingName);
        }
        catch (InvalidOperationException exception)
        {
            throw new QuerySyntaxException(exception.Message, span, exception);
        }
    }

    private void ValidateFieldReferences(
        FieldDefinitionNode field,
        string schemaName,
        HashSet<string> declaredFields,
        IReadOnlyDictionary<string, Type?> declaredFieldTypes)
    {
        ValidateTypeExpressionReferences(field.TypeAnnotation, field.Name, schemaName, declaredFields, declaredFieldTypes);

        if (field.AtOffset != null)
            ValidateExpressionReferences(field.Name, field.AtOffset, schemaName, declaredFields);

        if (field.WhenCondition != null)
            ValidateExpressionReferences(field.Name, field.WhenCondition, schemaName, declaredFields);

        if (field.Constraint != null)
        {
            var constraintFields = new HashSet<string>(declaredFields, StringComparer.OrdinalIgnoreCase)
            {
                field.Name
            };
            var constraintTypes = new Dictionary<string, Type?>(declaredFieldTypes, StringComparer.OrdinalIgnoreCase)
            {
                [field.Name] = field.TypeAnnotation.ReturnType
            };

            ValidateExpressionReferences(field.Name, field.Constraint.Expression, schemaName, constraintFields);
            ValidateBooleanConstraint(field, constraintTypes);
        }
    }

    private void ValidateTypeExpressionReferences(
        TypeAnnotationNode typeNode,
        string fieldName,
        string schemaName,
        HashSet<string> declaredFields,
        IReadOnlyDictionary<string, Type?> declaredFieldTypes)
    {
        switch (typeNode)
        {
            case ByteArrayTypeNode byteArray:
                ValidateExpressionReferences(fieldName, byteArray.SizeExpression, schemaName, declaredFields);
                break;

            case StringTypeNode stringType:
                ValidateExpressionReferences(fieldName, stringType.SizeExpression, schemaName, declaredFields);
                break;

            case SubstreamTypeNode substream:
                ValidateExpressionReferences(fieldName, substream.SizeExpression, schemaName, declaredFields);
                if (substream.Target is not null)
                    ValidateTypeExpressionReferences(
                        substream.Target,
                        fieldName,
                        schemaName,
                        declaredFields,
                        declaredFieldTypes);
                break;

            case ArrayTypeNode array:
                ValidateExpressionReferences(fieldName, array.SizeExpression, schemaName, declaredFields);
                ValidateTypeExpressionReferences(array.ElementType, fieldName, schemaName, declaredFields, declaredFieldTypes);
                break;

            case InlineSchemaTypeNode inline:
                ValidateInlineFieldReferences(inline, fieldName, schemaName, declaredFields, declaredFieldTypes);
                break;

            case RepeatUntilTypeNode repeatUntil:
                ValidateTypeExpressionReferences(repeatUntil.ElementType, fieldName, schemaName, declaredFields, declaredFieldTypes);

                if (repeatUntil.Condition != null)
                {
                    var repeatFields = new HashSet<string>(declaredFields, StringComparer.OrdinalIgnoreCase)
                    {
                        repeatUntil.FieldName
                    };
                    var repeatFieldTypes = new Dictionary<string, Type?>(declaredFieldTypes, StringComparer.OrdinalIgnoreCase)
                    {
                        [repeatUntil.FieldName] = repeatUntil.ClrType
                    };

                    ValidateExpressionReferences(fieldName, repeatUntil.Condition, schemaName, repeatFields);
                    ValidateBooleanRepeatCondition(fieldName, repeatUntil.Condition, repeatFieldTypes);
                }
                break;

            case BinarySwitchTypeNode switchType:
                ValidateBinarySwitchReferences(
                    switchType,
                    fieldName,
                    schemaName,
                    declaredFields,
                    declaredFieldTypes);

                foreach (var switchCase in switchType.Cases)
                    ValidateTypeExpressionReferences(
                        switchCase.BranchType,
                        fieldName,
                        schemaName,
                        declaredFields,
                        declaredFieldTypes);
                break;
        }
    }

    private static void ValidateBinarySwitchReferences(
        BinarySwitchTypeNode switchType,
        string fieldName,
        string schemaName,
        HashSet<string> declaredFields,
        IReadOnlyDictionary<string, Type?> declaredFieldTypes)
    {
        if (!declaredFields.Contains(switchType.Selector) ||
            !declaredFieldTypes.TryGetValue(switchType.Selector, out var selectorType))
            throw new QuerySyntaxException(
                $"Switch selector '{switchType.Selector}' must reference a field declared before the switch field.",
                GetSwitchSelectorSpan(switchType),
                DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField);

        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < switchType.Cases.Length; index++)
        {
            var switchCase = switchType.Cases[index];
            if (!aliases.Add(switchCase.BranchAlias) ||
                string.Equals(switchCase.BranchAlias, "Case", StringComparison.OrdinalIgnoreCase))
                throw new QuerySyntaxException(
                    $"Duplicate switch branch alias '{switchCase.BranchAlias}'.",
                    GetSwitchBranchAliasSpan(switchCase),
                    DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias);

            if (switchCase.IsDefault && index != switchType.Cases.Length - 1)
                throw new QuerySyntaxException(
                    "Switch default case '_' must be the last case.",
                    GetSwitchCaseLabelSpan(switchCase),
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel);

            if (switchCase.BranchType is not (PrimitiveTypeNode or ByteArrayTypeNode or SchemaReferenceTypeNode))
                throw new QuerySyntaxException(
                    $"Switch branch '{switchCase.BranchAlias}' uses unsupported binary type '{switchCase.BranchType}'.",
                    GetSwitchBranchTypeSpan(switchCase),
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel);

            if (!switchCase.IsDefault &&
                !IsSwitchCaseLabelCompatible(selectorType, switchCase.CaseValue!))
                throw new QuerySyntaxException(
                    $"Switch case label '{switchCase.CaseValue}' is not compatible with selector '{switchType.Selector}'.",
                    GetSwitchCaseLabelSpan(switchCase),
                    DiagnosticCode.MQ4013_InvalidSwitchCaseLabel);
        }
    }

    private static bool IsSwitchCaseLabelCompatible(Type? selectorType, Node label)
    {
        var targetType = selectorType is null ? null : Nullable.GetUnderlyingType(selectorType) ?? selectorType;
        if (targetType == typeof(bool))
            return label is BooleanNode;

        if (targetType == typeof(string))
            return label is StringNode or WordNode;

        if (!IsNumericSwitchCaseLabel(label) || !TryGetNumericSwitchCaseValue(label, out var numeric))
            return false;

        if (targetType == typeof(float) || targetType == typeof(double) || targetType == typeof(decimal))
            return true;

        if (targetType == typeof(byte))
            return IsIntegralSwitchValue(numeric, byte.MinValue, byte.MaxValue);
        if (targetType == typeof(sbyte))
            return IsIntegralSwitchValue(numeric, sbyte.MinValue, sbyte.MaxValue);
        if (targetType == typeof(short))
            return IsIntegralSwitchValue(numeric, short.MinValue, short.MaxValue);
        if (targetType == typeof(ushort))
            return IsIntegralSwitchValue(numeric, ushort.MinValue, ushort.MaxValue);
        if (targetType == typeof(int))
            return IsIntegralSwitchValue(numeric, int.MinValue, int.MaxValue);
        if (targetType == typeof(uint))
            return IsIntegralSwitchValue(numeric, uint.MinValue, uint.MaxValue);
        if (targetType == typeof(long))
            return IsIntegralSwitchValue(numeric, long.MinValue, long.MaxValue);
        if (targetType == typeof(ulong))
            return IsIntegralSwitchValue(numeric, ulong.MinValue, ulong.MaxValue);

        return false;
    }

    private static bool IsNumericSwitchCaseLabel(Node node)
    {
        if (node is HyphenNode hyphen)
            return IsNumericSwitchCaseLabel(hyphen.Left) && IsNumericSwitchCaseLabel(hyphen.Right);

        return node is ConstantValueNode constant && constant.ObjValue is byte or sbyte or short or ushort or int or
            uint or long or ulong or float or double or decimal;
    }

    private static bool TryGetNumericSwitchCaseValue(Node node, out decimal value)
    {
        if (node is HyphenNode hyphen &&
            TryGetNumericSwitchCaseValue(hyphen.Left, out var left) &&
            TryGetNumericSwitchCaseValue(hyphen.Right, out var right))
        {
            value = left - right;
            return true;
        }

        if (node is ConstantValueNode { ObjValue: byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal } constant)
        {
            try
            {
                value = Convert.ToDecimal(constant.ObjValue, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception) when (constant.ObjValue is float or double)
            {
                // NaN and infinity cannot be valid switch labels.
            }
        }

        value = default;
        return false;
    }

    private static bool IsIntegralSwitchValue(decimal value, decimal minimum, decimal maximum)
    {
        return decimal.Truncate(value) == value && value >= minimum && value <= maximum;
    }

    private static TextSpan GetSwitchSelectorSpan(BinarySwitchTypeNode switchType)
    {
        return switchType.SelectorSpan.IsEmpty ? switchType.Span : switchType.SelectorSpan;
    }

    private static TextSpan GetSwitchCaseLabelSpan(BinarySwitchCaseNode switchCase)
    {
        return switchCase.CaseLabelSpan.IsEmpty
            ? switchCase.CaseValue?.Span ?? switchCase.BranchTypeSpan
            : switchCase.CaseLabelSpan;
    }

    private static TextSpan GetSwitchBranchAliasSpan(BinarySwitchCaseNode switchCase)
    {
        return switchCase.BranchAliasSpan.IsEmpty
            ? switchCase.BranchTypeSpan
            : switchCase.BranchAliasSpan;
    }

    private static TextSpan GetSwitchBranchTypeSpan(BinarySwitchCaseNode switchCase)
    {
        return switchCase.BranchTypeSpan.IsEmpty ? switchCase.BranchType.Span : switchCase.BranchTypeSpan;
    }

    private void ValidateInlineFieldReferences(
        InlineSchemaTypeNode inlineSchema,
        string fieldName,
        string schemaName,
        HashSet<string> outerFields,
        IReadOnlyDictionary<string, Type?> outerFieldTypes)
    {
        var declaredFields = new HashSet<string>(outerFields, StringComparer.OrdinalIgnoreCase);
        var declaredFieldTypes = new Dictionary<string, Type?>(outerFieldTypes, StringComparer.OrdinalIgnoreCase);
        var localFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in inlineSchema.Fields)
        {
            ValidateDuplicateField(field, $"{schemaName}.{fieldName}", localFields);

            if (field is FieldDefinitionNode parsedField)
                ValidateFieldReferences(parsedField, schemaName, declaredFields, declaredFieldTypes);
            else if (field is ComputedFieldNode computedField)
                ValidateExpressionReferences(fieldName, computedField.Expression, schemaName, declaredFields);

            declaredFields.Add(field.Name);
            declaredFieldTypes[field.Name] = field.ReturnType;
        }
    }

    private static void ValidateBooleanConstraint(
        FieldDefinitionNode field,
        IReadOnlyDictionary<string, Type?> fieldTypes)
    {
        if (IsBooleanExpression(field.Constraint!.Expression, fieldTypes))
            return;

        throw new QuerySyntaxException(
            $"Binary schema field '{field.Name}' check expression must evaluate to boolean.",
            field.Constraint.Expression.SpanOrEmpty(),
            DiagnosticCode.MQ4006_InvalidFieldConstraint);
    }

    private static void ValidateBooleanRepeatCondition(
        string fieldName,
        Node condition,
        IReadOnlyDictionary<string, Type?> fieldTypes)
    {
        if (IsBooleanExpression(condition, fieldTypes))
            return;

        throw new QuerySyntaxException(
            $"Binary schema field '{fieldName}' repeat-until condition must evaluate to boolean.",
            condition.SpanOrEmpty(),
            DiagnosticCode.MQ4006_InvalidFieldConstraint);
    }

    private static bool IsBooleanExpression(Node expression, IReadOnlyDictionary<string, Type?> fieldTypes)
    {
        switch (expression)
        {
            case EqualityNode:
            case DiffNode:
            case GreaterNode:
            case GreaterOrEqualNode:
            case LessNode:
            case LessOrEqualNode:
            case BooleanNode:
                return true;

            case AndNode and:
                return IsBooleanExpression(and.Left, fieldTypes) &&
                       IsBooleanExpression(and.Right, fieldTypes);

            case OrNode or:
                return IsBooleanExpression(or.Left, fieldTypes) &&
                       IsBooleanExpression(or.Right, fieldTypes);

            case NotNode not:
                return IsBooleanExpression(not.Expression, fieldTypes);

            case IdentifierNode identifier:
                return fieldTypes.TryGetValue(identifier.Name, out var identifierType) &&
                       identifierType == typeof(bool);

            case AccessMethodNode method:
                return method.Method == null || method.ReturnType == typeof(bool);

            default:
                return expression.ReturnType == typeof(bool);
        }
    }

    private static Type? ResolveExpressionType(
        Node? expression,
        IReadOnlyDictionary<string, Type?> fieldTypes)
    {
        if (expression == null)
            return typeof(object);

        if (expression is IdentifierNode identifier && fieldTypes.TryGetValue(identifier.Name, out var identifierType))
            return identifierType;

        return expression.ReturnType;
    }

    private void ValidateExpressionReferences(
        string fieldName,
        Node? expression,
        string schemaName,
        HashSet<string> declaredFields)
    {
        if (expression == null)
            return;

        var identifiers = new IdentifierCollector();
        new IdentifierCollectorTraverse(identifiers).Traverse(expression);

        foreach (var reference in identifiers.References)
            if (!declaredFields.Contains(reference.Name))
                throw new QuerySyntaxException(
                    $"Binary schema '{schemaName}' field '{fieldName}' references field '{reference.Name}' before it is declared.",
                    reference.Node.SpanOrEmpty());
    }

    private void AddInheritedFields(
        string? parentName,
        string childSchemaName,
        HashSet<string> fields,
        Dictionary<string, Type?> fieldTypes,
        ISet<string> inheritancePath)
    {
        if (string.IsNullOrWhiteSpace(parentName))
            return;

        if (!Registry.TryGetSchema(parentName, out var registration) || registration?.Node is not BinarySchemaNode parent)
            throw new QuerySyntaxException(
                $"Binary schema '{childSchemaName}' extends undefined or non-binary schema '{parentName}'.",
                GetInheritanceSpan(childSchemaName, parentName),
                DiagnosticCode.MQ2030_UnsupportedSyntax);

        ValidateSchemaReference(parentName, childSchemaName, GetInheritanceSpan(childSchemaName, parentName));

        if (parent.IsGeneric)
            throw InvalidGenericSchemaReference(
                $"Binary schema '{childSchemaName}' cannot extend generic schema '{parent.Name}' without type arguments.",
                GetInheritanceSpan(childSchemaName, parentName));

        if (!inheritancePath.Add(parent.Name))
            throw new QuerySyntaxException(
                $"Binary schema inheritance contains a cycle involving '{parent.Name}'.",
                GetInheritanceSpan(childSchemaName, parentName),
                DiagnosticCode.MQ2030_UnsupportedSyntax);

        try
        {
            AddInheritedFields(parent.Extends, parent.Name, fields, fieldTypes, inheritancePath);
        }
        finally
        {
            inheritancePath.Remove(parent.Name);
        }

        foreach (var field in parent.Fields)
        {
            fields.Add(field.Name);
            fieldTypes[field.Name] = ResolveExpressionType(field is ComputedFieldNode computed ? computed.Expression : field, fieldTypes);
        }
    }

    private TextSpan GetInheritanceSpan(string childSchemaName, string parentName)
    {
        if (Registry.TryGetSchema(childSchemaName, out var registration) &&
            registration?.Node is BinarySchemaNode binary &&
            !binary.ExtendsSpan.IsEmpty)
            return binary.ExtendsSpan;

        if (Registry.TryGetSchema(parentName, out var parentRegistration) && parentRegistration?.Node is Node parentNode)
            return parentNode.SpanOrEmpty();

        return TextSpan.Empty;
    }

    private sealed class IdentifierCollector : NoOpExpressionVisitor
    {
        public HashSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<(string Name, Node Node)> References { get; } = [];

        public override void Visit(IdentifierNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Names.Add(node.Name);
            References.Add((node.Name, node));
        }

        public override void Visit(AccessColumnNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Names.Add(node.Name);
            References.Add((node.Name, node));
        }
    }

    private sealed class IdentifierCollectorTraverse(IdentifierCollector visitor)
        : RawTraverseVisitor<IdentifierCollector>(visitor)
    {
        public override void Visit(DotNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            node.Root.Accept(this);
        }

        public void Traverse(Node node)
        {
            ArgumentNullException.ThrowIfNull(node);
            node.Accept(this);
        }
    }
}
