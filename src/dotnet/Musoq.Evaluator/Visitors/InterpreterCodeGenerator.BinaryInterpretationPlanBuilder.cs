using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    /// <summary>
    ///     Builds an immutable bound plan for a binary schema, resolving inheritance,
    ///     field ordering, and per-field property-shape decisions ahead of C# rendering.
    /// </summary>
    /// <param name="schema">The binary schema to bind.</param>
    /// <returns>The resolved bound interpretation plan.</returns>
    public BoundBinaryInterpretationPlan BuildBinaryPlan(BinarySchemaNode schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _currentSchemaName = schema.Name;
        _inlineSchemas.Clear();
        _switchSchemas.Clear();
        _discardCounter = 0;
        _currentTypeParameters = schema.TypeParameters ?? Array.Empty<string>();

        var allFields = GetAllFieldsIncludingInherited(schema);
        SetCurrentNullableFieldNames(allFields);
        var boundFields = new List<BoundBinaryField>(allFields.Count);
        foreach (var field in allFields)
            boundFields.Add(BindBinaryField(field, allFields));

        return new BoundBinaryInterpretationPlan
        {
            SchemaName = schema.Name,
            IsGeneric = schema.IsGeneric,
            TypeParameters = _currentTypeParameters,
            Extends = string.IsNullOrEmpty(schema.Extends) ? null : schema.Extends,
            Fields = boundFields
        };
    }

    private BoundBinaryField BindBinaryField(SchemaFieldNode field, List<SchemaFieldNode> allFields)
    {
        var kind = field is ComputedFieldNode ? BoundBinaryFieldKind.Computed : BoundBinaryFieldKind.Parsed;
        var isDiscard = field.Name == "_";
        var isAlignment = field is FieldDefinitionNode { TypeAnnotation: AlignmentNode };
        var localVariableName = GetLocalVarName(field.Name);
        var isConditional = field.IsConditional ||
                            (field is ComputedFieldNode computed &&
                             ReferencesConditionalField(computed.Expression, allFields));

        if (isDiscard || isAlignment)
            return new BoundBinaryField
            {
                Source = field,
                Name = field.Name,
                Kind = kind,
                LocalVariableName = localVariableName,
                IsDiscard = isDiscard,
                IsAlignment = isAlignment,
                IsConditional = isConditional
            };

        var clrTypeName = GetClrTypeNameForFieldWithContext(field, allFields);
        var isNullableProperty = isConditional &&
                                 !IsReferenceType(clrTypeName) &&
                                 !IsTypeParameter(clrTypeName);

        return new BoundBinaryField
        {
            Source = field,
            Name = field.Name,
            Kind = kind,
            LocalVariableName = localVariableName,
            IsDiscard = false,
            IsAlignment = false,
            IsConditional = isConditional,
            PropertyName = EscapeCSharpIdentifier(field.Name),
            PropertyClrType = isNullableProperty ? $"{clrTypeName}?" : clrTypeName,
            IsNullableProperty = isNullableProperty,
            Switch = BindBinarySwitch(field)
        };
    }

    private static BoundBinarySwitch? BindBinarySwitch(SchemaFieldNode field)
    {
        if (field is not FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode switchType })
            return null;

        var branches = new List<BoundBinarySwitchBranch>(switchType.Cases.Length);
        foreach (var switchCase in switchType.Cases)
            branches.Add(new BoundBinarySwitchBranch
            {
                CaseValue = switchCase.CaseValue,
                BranchAlias = switchCase.BranchAlias,
                BranchType = switchCase.BranchType
            });

        var defaultCase = switchType.DefaultCase;

        return new BoundBinarySwitch
        {
            Selector = switchType.Selector,
            Branches = branches,
            DefaultBranch = defaultCase is null
                ? null
                : new BoundBinarySwitchBranch
                {
                    CaseValue = defaultCase.CaseValue,
                    BranchAlias = defaultCase.BranchAlias,
                    BranchType = defaultCase.BranchType
                }
        };
    }
}
