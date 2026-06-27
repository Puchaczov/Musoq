using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using ApplyFromNode = Musoq.Parser.Nodes.From.ApplyFromNode;
using ApplyNode = Musoq.Parser.Nodes.From.ApplyNode;
using ExpressionConverter = Musoq.Evaluator.IR.Expressions.ExpressionConverter;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using PropertyFromNode = Musoq.Parser.Nodes.From.PropertyFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    private bool ShouldPreserveDirectApplyChain(DirectApplyChainCandidate candidate)
    {
        if (candidate.From.Expression is not ApplyNode)
            return false;

        if (_joinedTables.Count < 2)
            return false;

        if (!UsesSupportedDirectApplyOrderKeys(candidate.Select, candidate.OrderBy))
            return false;

        if (candidate.Select.IsDistinct)
            return false;

        if (HasProjectionStar(candidate.Select))
            return false;

        if (HasDuplicateSelectedFieldNames(candidate.Select))
            return false;

        if (!CanReferenceDirectApplySourceEntity())
            return false;

        return IsCrossPropertyApplyChainFromBaseAlias();
    }

    private bool CanReferenceDirectApplySourceEntity()
    {
        var baseAlias = ResolveLeftMostSourceAlias(_joinedTables[0].Source);
        if (!Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(baseAlias))
            return false;

        var tableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(baseAlias);
        var sourceEntityType = tableSymbol.GetTableByAlias(baseAlias).Table.Metadata?.TableEntityType;

        return sourceEntityType != null && CanReferenceType(sourceEntityType);
    }

    private bool IsCrossPropertyApplyChainFromBaseAlias()
    {
        var baseAlias = ResolveLeftMostSourceAlias(_joinedTables[0].Source);
        if (string.IsNullOrWhiteSpace(baseAlias))
            return false;

        foreach (var joinedTable in _joinedTables)
        {
            if (joinedTable is not ApplyFromNode
                {
                    ApplyType: ApplyType.Cross,
                    With: PropertyFromNode property
                })
            {
                return false;
            }

            if (!string.Equals(property.SourceAlias, baseAlias, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static string ResolveLeftMostSourceAlias(FromNode source)
    {
        while (source is BinaryFromNode binary)
            source = binary.Source;

        return source.Alias;
    }

    private static bool HasProjectionStar(SelectNode select)
    {
        return select.Fields.Any(field => field.Expression is AllColumnsNode);
    }

    private static bool HasDuplicateSelectedFieldNames(SelectNode select)
    {
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in select.Fields)
        {
            if (!fieldNames.Add(field.FieldName))
                return true;
        }

        return false;
    }

    private static bool UsesSupportedDirectApplyOrderKeys(SelectNode select, OrderByNode? orderBy)
    {
        if (orderBy == null)
            return true;

        var projectedFields = TryCreateDirectApplyProjectedFields(select);
        if (projectedFields == null)
            return false;

        foreach (var field in orderBy.Fields)
        {
            if (IsProjectedDirectApplyOrderKey(projectedFields, field))
                continue;

            if (IsQualifiedColumnDirectApplyOrderKey(field))
                continue;

            return false;
        }

        return true;
    }

    private static List<ProjectedField>? TryCreateDirectApplyProjectedFields(SelectNode select)
    {
        var converter = new ExpressionConverter();
        var projectedFields = new List<ProjectedField>(select.Fields.Length);

        foreach (var field in select.Fields)
        {
            var expression = TryConvertDirectApplyOrderExpression(converter, field.Expression);
            if (expression == null)
                return null;

            projectedFields.Add(new ProjectedField(field.FieldName, expression, field.FieldOrder));
        }

        return projectedFields;
    }

    private static bool IsProjectedDirectApplyOrderKey(
        IReadOnlyList<ProjectedField> projectedFields,
        FieldNode field)
    {
        var converter = new ExpressionConverter();
        var expression = TryConvertDirectApplyOrderExpression(converter, field.Expression);
        if (expression == null)
            return false;

        return SortKeyProjectionResolver.TryResolveOutputName(expression, projectedFields) != null ||
               IsProjectedDirectApplyOrderAlias(expression, projectedFields);
    }

    private static bool IsProjectedDirectApplyOrderAlias(
        IrExpression expression,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        if (expression is not ColumnRef { Alias: "" } columnRef)
            return false;

        return projectedFields.Any(field =>
            string.Equals(field.OutputName, columnRef.ColumnName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsQualifiedColumnDirectApplyOrderKey(FieldNode field)
    {
        var expression = TryConvertDirectApplyOrderExpression(new ExpressionConverter(), field.Expression);

        return expression is ColumnRef columnRef &&
               !string.IsNullOrWhiteSpace(columnRef.Alias);
    }

    private static IrExpression? TryConvertDirectApplyOrderExpression(
        ExpressionConverter converter,
        Node expression)
    {
        try
        {
            return converter.Convert(expression);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool CanReferenceType(Type type)
    {
        if (type.IsByRef || type.IsPointer)
            return false;

        if (type.IsArray)
            return type.GetElementType() is { } elementType && CanReferenceType(elementType);

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
            return CanReferenceType(nullableType);

        if (type.IsGenericType)
            return CanReferencePublicType(type.GetGenericTypeDefinition()) &&
                   type.GetGenericArguments().All(CanReferenceType);

        return CanReferencePublicType(type);
    }

    private static bool CanReferencePublicType(Type type)
    {
        if (!type.IsNested)
            return type.IsPublic;

        return type is { IsNestedPublic: true, DeclaringType: not null } &&
               CanReferencePublicType(type.DeclaringType);
    }

    private sealed record DirectApplyChainCandidate(
        SelectNode Select,
        ExpressionFromNode From,
        OrderByNode? OrderBy,
        GroupByNode? GroupBy,
        WindowNode? Window,
        QualifyNode? Qualify);
}
