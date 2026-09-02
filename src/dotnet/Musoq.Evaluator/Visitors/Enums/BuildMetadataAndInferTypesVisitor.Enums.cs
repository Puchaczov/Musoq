using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public IReadOnlyDictionary<string, EnumTypeDescriptor> QueryLocalEnumTypes =>
        new Dictionary<string, EnumTypeDescriptor>(_enumBinding.QueryLocalTypes, StringComparer.OrdinalIgnoreCase);

    public override void Visit(EnumMemberNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new EnumMemberNode(
                node.Name,
                node.RawValue,
                node.LiteralText,
                node.NameSpan,
                node.ValueSpan,
                node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(EnumDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var members = new EnumMemberNode[node.Members.Count];
        for (var index = members.Length - 1; index >= 0; index--)
            members[index] = (EnumMemberNode)PopSemanticNode("Visit(EnumDeclarationNode).Member");

        var declaration = (EnumDeclarationNode)new EnumDeclarationNode(
                node.Name,
                node.UnderlyingTypeName,
                node.IsFlags,
                members,
                node.NameSpan,
                node.UnderlyingTypeSpan,
                node.Span)
            .WithFullSpan(node.FullSpan);

        if (_enumBinding.QueryLocalTypes.ContainsKey(node.Name))
        {
            var span = node.NameSpan.IsEmpty ? node.SpanOrEmpty() : node.NameSpan;
            var exception = new VisitorException(
                VisitorName,
                "BindEnumDeclaration",
                $"Enum type '{node.Name}' is already declared in this query batch. Enum type names are case-insensitive.",
                DiagnosticCode.MQ3106_DuplicateEnumType,
                span);

            if (DiagnosticContext != null)
                DiagnosticContext.ReportException(exception, span);
            else
                throw exception;

            PushSemanticNode(declaration);
            return;
        }

        var kind = ResolveEnumUnderlyingKind(node.UnderlyingTypeName);
        var descriptors = new EnumMemberDescriptor[members.Length];
        for (var index = 0; index < members.Length; index++)
            descriptors[index] = new EnumMemberDescriptor(
                members[index].Name,
                EnumScalarValue.FromRaw(kind, members[index].RawValue));

        _enumBinding.QueryLocalTypes.Add(
            node.Name,
            new EnumTypeDescriptor(node.Name, EnumTypeOrigin.QueryLocal, kind, node.IsFlags, descriptors));
        PushSemanticNode(declaration);
    }

    private static EnumUnderlyingKind ResolveEnumUnderlyingKind(string underlyingTypeName)
    {
        return underlyingTypeName.ToLowerInvariant() switch
        {
            "byte" => EnumUnderlyingKind.Byte,
            "sbyte" => EnumUnderlyingKind.SByte,
            "short" => EnumUnderlyingKind.Int16,
            "ushort" => EnumUnderlyingKind.UInt16,
            "int" => EnumUnderlyingKind.Int32,
            "uint" => EnumUnderlyingKind.UInt32,
            "long" => EnumUnderlyingKind.Int64,
            "ulong" => EnumUnderlyingKind.UInt64,
            _ => throw new InvalidOperationException(
                $"Parser accepted unsupported enum backing type '{underlyingTypeName}'.")
        };
    }

    private void MarkEnumExpression(Node node, EnumTypeDescriptor descriptor)
    {
        _enumBinding.ExpressionTypes[node] = descriptor;
    }

    private bool TryGetEnumExpressionType(Node node, out EnumTypeDescriptor descriptor)
    {
        if (_enumBinding.ExpressionTypes.TryGetValue(node, out descriptor!))
            return true;

        switch (node)
        {
            case FieldNode field:
                return TryGetEnumExpressionType(field.Expression, out descriptor);
            case ThenNode then:
                return TryGetEnumExpressionType(then.Expression, out descriptor);
            case ElseNode @else:
                return TryGetEnumExpressionType(@else.Expression, out descriptor);
            case AccessMethodNode { Method: { } method }:
                return TryGetNativeEnumDescriptor(method.ReturnType, out descriptor);
            case PropertyValueNode { PropertyInfo: { } property }:
                return TryGetNativeEnumDescriptor(property.PropertyType, out descriptor);
            case DotNode dot when TryGetEnumExpressionType(dot.Expression, out descriptor):
                return true;
        }

        descriptor = null!;
        return false;
    }

    private bool TryGetNativeEnumDescriptor(Type type, out EnumTypeDescriptor descriptor)
    {
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (!enumType.IsEnum)
        {
            descriptor = null!;
            return false;
        }

        if (!_enumBinding.NativeTypes.TryGetValue(enumType, out descriptor!))
        {
            descriptor = EnumTypeDescriptor.FromClrEnum(enumType);
            _enumBinding.NativeTypes.Add(enumType, descriptor);
        }

        AddAssembly(enumType.Assembly);
        return true;
    }
}
