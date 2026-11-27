using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Dictionary<string, string> _interpretationSchemasByAlias = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<Node, InterpretationPresence> _interpretationPresence = new(
        ReferenceEqualityComparer.Instance
    );

    private ISchemaColumn PreserveInterpretationColumnShape(ISchemaColumn column, Node node) =>
        InterpretationColumn.Preserve(column, ExpressionPresence(node));

    private InterpretationPresence ExpressionPresence(Node node)
    {
        var presence = _interpretationPresence.GetValueOrDefault(node);
        if (presence.MayBeNull || node is NullNode or DotNode { IsNullSafePath: true })
            return presence with { MayBeNull = true };
        return node is ArrayIndexNode indexed && IndexedExpressionMayBeMissing(indexed)
            ? presence with
            {
                MayBeNull = true,
            }
            : presence;
    }

    private void RegisterCaseInterpretationPresence(
        Node result,
        IReadOnlyList<(Node When, Node Then)> branches,
        Node otherwise
    )
    {
        InterpretationPresence? combined = null;
        var interpreted = false;
        var hasNull = false;
        foreach (var branch in InterpretationPropertyBinding.CaseResults(branches, otherwise))
        {
            if (branch is NullNode)
            {
                hasNull = true;
                continue;
            }
            var presence = ExpressionPresence(branch);
            interpreted |=
                presence != default
                || InterpretationPropertyBinding.GetIntendedTypeName(branch) != null
                || branch is AccessColumnNode column && _interpretationSchemasByAlias.ContainsKey(column.Alias);
            combined = combined?.Merge(presence) ?? presence;
        }
        if (interpreted && combined is { } value && (hasNull || value != default))
            _interpretationPresence[result] = value with { MayBeNull = hasNull || value.MayBeNull };
    }

    private void RegisterCoalesceInterpretationPresence(Node result, Node left, Node right)
    {
        var presence = ExpressionPresence(left);
        if (presence.MayBeNull && right is not NullNode)
        {
            var fallback = ExpressionPresence(right);
            presence = presence.Merge(fallback) with { MayBeNull = fallback.MayBeNull };
        }
        if (presence != default)
            _interpretationPresence[result] = presence;
    }

    private bool IndexedExpressionMayBeMissing(ArrayIndexNode indexed)
    {
        var array = ExpressionPresence(indexed.Array);
        if (array.MayBeNull || array.VariableElements)
            return true;
        if (
            indexed.ReturnType?.IsValueType != false
            || string.IsNullOrEmpty(InterpretationPropertyBinding.GetIntendedTypeName(indexed))
        )
            return false;
        return !BinarySchemaExpressionTypes.TryGetConstantIntegerIndex(indexed.Index, out var index)
            || !array.ContainsIndex(index);
    }

    private void RegisterNullableInterpretationColumn(Node node, ISchemaColumn column, bool mayBeMissing)
    {
        var presence = InterpretationPresence.From(column);
        if (
            mayBeMissing
            && (
                column.IntendedTypeName is { Length: > 0 }
                || !column.ColumnType.IsValueType
                    && node is AccessColumnNode access
                    && _interpretationSchemasByAlias.ContainsKey(access.Alias)
            )
        )
            presence = presence with { MayBeNull = true };
        if (presence != default)
            _interpretationPresence[node] = presence;
    }

    private void RegisterIndexedInterpretationColumn(AccessObjectArrayNode node, ISchemaColumn column)
    {
        var array = InterpretationPresence.From(column);
        if (!array.VariableElements && node.IntendedTypeName == null)
            return;
        if (
            array.MayBeNull
            || node.Token.IsOutOfRange
            || !array.ContainsIndex(node.Token.Index)
            || node.TableAlias is { } alias
                && TryGetCurrentTableSymbol(out var current)
                && current?.CanAliasBeMissing(alias) == true
        )
            _interpretationPresence[node] = new InterpretationPresence(true);
    }

    private void MergeSetInterpretationShape(Node left, Node right)
    {
        if (left.ReturnType?.IsValueType != false)
            return;
        var combined = ExpressionPresence(left).Merge(ExpressionPresence(right));
        if (combined == default)
            _interpretationPresence.Remove(left);
        else
            _interpretationPresence[left] = combined;
    }

    private DotNode PreserveInterpretationPathNullability(DotNode node)
    {
        if (node.ReturnType is not { } type)
            return node;
        var member = InterpretationMemberNullability(node);
        var indexed = node.Expression is AccessObjectArrayNode;
        var mayBeNull =
            member.MayBeNull
            || ExpressionPresence(node.Root).MayBeNull
            || indexed && member.VariableElements
            || DirectBinaryPathMayBeNull(node, type);
        var result = mayBeNull
            ? new DotNode(
                node.Root,
                node.Expression,
                node.IsTheMostInner,
                node.Name,
                BinarySchemaExpressionTypes.Lift(type, true),
                node.IntendedTypeName
            )
            {
                IsNullSafePath = true,
            }.CopySpansFrom(node)
            : node;
        var presence = member with { MayBeNull = mayBeNull, VariableElements = !indexed && member.VariableElements };
        if (presence != default)
            _interpretationPresence[result] = presence;
        return result;
    }

    private bool DirectBinaryPathMayBeNull(DotNode node, Type type)
    {
        Node root = node;
        while (root is DotNode dot)
            root = dot.Root;
        var alias = root switch
        {
            AccessColumnNode column => column.Alias,
            AccessObjectArrayNode { IsColumnAccess: true } indexed => indexed.TableAlias,
            _ => null,
        };
        return alias != null
            && _interpretationSchemasByAlias.TryGetValue(alias, out var name)
            && SchemaRegistry?.TryGetSchema(name, out var registration) == true
            && registration?.Node is BinarySchemaNode schema
            && (
                BinarySchemaExpressionTypes.IsNullable(type)
                || BinarySchemaExpressionTypes.MayBeNull(node, GetAllBinarySchemaFields(schema), SchemaRegistry)
            );
    }

    private InterpretationPresence InterpretationMemberNullability(DotNode node)
    {
        if (
            SchemaRegistry == null
            || InterpretationPropertyBinding.GetIntendedTypeName(node.Root) is not { Length: > 0 } typeName
        )
            return default;
        var memberName = node.Expression switch
        {
            AccessObjectArrayNode indexed => indexed.ObjectName,
            PropertyValueNode property => property.Name,
            IdentifierNode identifier => identifier.Name,
            _ => null,
        };
        if (memberName == null)
            return default;

        if (TextSwitchPropertyResolver.Find(SchemaRegistry, typeName) is { } switchShape)
        {
            var member = switchShape.Member(memberName);
            return new InterpretationPresence(member?.MayBeNull == true, member?.Element?.ClrType.IsValueType == false);
        }
        var name = ParseSchemaReferenceFromTypeName(typeName).SchemaName;
        SchemaRegistry.TryGetSchema(name, out var registration);
        if (registration?.Node is TextSchemaNode text)
        {
            var member = GetAllTextSchemaFields(text)
                .Find(field => field.Name.Equals(memberName, StringComparison.Ordinal));
            if (member == null)
                return default;
            var isReferenceArray =
                node.Expression is AccessObjectArrayNode { ReturnType.IsValueType: false }
                || node.ReturnType?.IsArray == true && node.ReturnType.GetElementType()?.IsValueType == false;
            return new InterpretationPresence(InterpretationTextFieldTypeResolver.MayBeNull(member), isReferenceArray);
        }
        IReadOnlyList<SchemaFieldNode>? fields = registration?.Node is BinarySchemaNode binary
            ? GetAllBinarySchemaFields(binary)
            : SchemaRegistry.BinaryCompositions.Find<InlineSchemaTypeNode>(name)?.Fields;
        if (fields == null)
            return default;
        var memberIdentifier = new IdentifierNode(memberName);
        var mayBeNull =
            BinarySchemaExpressionTypes.MayBeNull(memberIdentifier, fields, SchemaRegistry)
            || node.Expression is AccessObjectArrayNode index
                && BinarySchemaExpressionTypes.MayBeNull(index, fields, SchemaRegistry);
        var minimum =
            node.ReturnType?.IsArray == true
                ? BinarySchemaExpressionTypes.MinimumArrayLength(memberIdentifier, fields, SchemaRegistry)
                : 0;
        return new InterpretationPresence(mayBeNull, MinimumArrayLength: minimum);
    }
}
