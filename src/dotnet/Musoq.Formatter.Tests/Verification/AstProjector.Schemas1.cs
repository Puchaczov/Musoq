namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.AlignmentNode value)
    {
        Member(nameof(value.AlignmentBits));
        Write(value.AlignmentBits);
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.ArrayTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.ElementType));
        Write(value.ElementType);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SizeExpression));
        Write(value.SizeExpression);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.BinarySchemaNode value)
    {
        Member(nameof(value.Comments));
        Sequence(value.Comments, static (writer, item) => writer.Write(item));
        Member(nameof(value.Extends));
        Write(value.Extends);
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsGeneric));
        Write(value.IsGeneric);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TypeParameters));
        Sequence(value.TypeParameters, static (writer, item) => writer.Write(item));
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.BinarySwitchCaseNode value)
    {
        Member(nameof(value.BranchAlias));
        Write(value.BranchAlias);
        Member(nameof(value.BranchType));
        Write(value.BranchType);
        Member(nameof(value.CaseValue));
        Write(value.CaseValue);
        Member(nameof(value.IsDefault));
        Write(value.IsDefault);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.BinarySwitchTypeNode value)
    {
        Member(nameof(value.Cases));
        Sequence(value.Cases, static (writer, item) => writer.Write(item));
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.DefaultCase));
        Write(value.DefaultCase);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Selector));
        Write(value.Selector);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.BitsTypeNode value)
    {
        Member(nameof(value.BitCount));
        Write(value.BitCount);
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.ByteArrayTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SizeExpression));
        Write(value.SizeExpression);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.ComputedFieldNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.IsComputed));
        Write(value.IsComputed);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.IsConditional));
        Write(value.IsConditional);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.FieldConstraintNode value)
    {
        Member(nameof(value.Expression));
        Write(value.Expression);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.FieldDefinitionNode value)
    {
        Member(nameof(value.AtOffset));
        Write(value.AtOffset);
        Member(nameof(value.Constraint));
        Write(value.Constraint);
        Member(nameof(value.IsComputed));
        Write(value.IsComputed);
        Member(nameof(value.IsConditional));
        Write(value.IsConditional);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TypeAnnotation));
        Write(value.TypeAnnotation);
        Member(nameof(value.ValueValidation));
        Write(value.ValueValidation);
        Member(nameof(value.WhenCondition));
        Write(value.WhenCondition);
        Member(nameof(value.Name));
        Write(value.Name);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.FieldValueValidationNode value)
    {
        Member(nameof(value.IsByteList));
        Write(value.IsByteList);
        Member(nameof(value.Kind));
        Write(value.Kind);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.Values));
        Sequence(value.Values, static (writer, item) => writer.Write(item));
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.InlineSchemaTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.PrimitiveTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.Endianness));
        Write(value.Endianness);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.TypeName));
        Write(value.TypeName);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.RepeatUntilTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.Condition));
        Write(value.Condition);
        Member(nameof(value.ElementType));
        Write(value.ElementType);
        Member(nameof(value.FieldName));
        Write(value.FieldName);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.StopKind));
        Write(value.StopKind);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.SchemaReferenceTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.FullTypeName));
        Write(value.FullTypeName);
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.IsGenericInstantiation));
        Write(value.IsGenericInstantiation);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SchemaName));
        Write(value.SchemaName);
        Member(nameof(value.TypeArguments));
        Sequence(value.TypeArguments, static (writer, item) => writer.Write(item));
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.StringTypeNode value)
    {
        Member(nameof(value.AsTextSchemaName));
        Write(value.AsTextSchemaName);
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.Encoding));
        Write(value.Encoding);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.Modifiers));
        Write(value.Modifiers);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SizeExpression));
        Write(value.SizeExpression);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.SubstreamTypeNode value)
    {
        Member(nameof(value.ClrType));
        Write(value.ClrType);
        Member(nameof(value.FixedSizeBytes));
        Optional(value.FixedSizeBytes, static (writer, item) => writer.Write(item));
        Member(nameof(value.IsFixedSize));
        Write(value.IsFixedSize);
        Member(nameof(value.Mode));
        Write(value.Mode);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SizeExpression));
        Write(value.SizeExpression);
        Member(nameof(value.Target));
        Write(value.Target);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.TextFieldDefinitionNode value)
    {
        Member(nameof(value.CaptureGroups));
        Sequence(value.CaptureGroups, static (writer, item) => writer.Write(item));
        Member(nameof(value.EscapeCharacter));
        Write(value.EscapeCharacter);
        Member(nameof(value.FieldType));
        Write(value.FieldType);
        Member(nameof(value.IsDiscard));
        Write(value.IsDiscard);
        Member(nameof(value.Modifiers));
        Write(value.Modifiers);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.PrimaryValue));
        Write(value.PrimaryValue);
        Member(nameof(value.RepeatElement));
        Write(value.RepeatElement);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
        Member(nameof(value.SecondaryValue));
        Write(value.SecondaryValue);
        Member(nameof(value.SwitchCases));
        Sequence(value.SwitchCases, static (writer, item) => writer.Write(item));
        Member(nameof(value.TypeDefinition));
        Write(value.TypeDefinition);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.TextSchemaNode value)
    {
        Member(nameof(value.Comments));
        Sequence(value.Comments, static (writer, item) => writer.Write(item));
        Member(nameof(value.Extends));
        Write(value.Extends);
        Member(nameof(value.Fields));
        Sequence(value.Fields, static (writer, item) => writer.Write(item));
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.ReturnType));
        Write(value.ReturnType);
    }

    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.TextSwitchCaseNode value)
    {
        Member(nameof(value.Body));
        Write(value.Body);
        Member(nameof(value.IsDefault));
        Write(value.IsDefault);
        Member(nameof(value.Pattern));
        Write(value.Pattern);
        Member(nameof(value.TypeName));
        Write(value.TypeName);
    }
}
