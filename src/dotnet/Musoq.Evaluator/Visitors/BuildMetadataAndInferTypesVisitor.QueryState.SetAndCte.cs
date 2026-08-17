using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Dictionary<string, ISchemaColumn[]> _provisionalRecursiveCteColumns =
        new(StringComparer.Ordinal);

    public override void Visit(TranslatedSetTreeNode node)
    {
    }

    public override void Visit(TranslatedSetOperatorNode node)
    {
    }

    public override void Visit(UnionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "Union");
    }

    public override void Visit(UnionAllNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "UnionAll");
    }

    public override void Visit(ExceptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "Except");
    }

    public override void Visit(IntersectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "Intersect");
    }

    public override void Visit(PutTrueNode node)
    {
        PushSemanticNode(new PutTrueNode());
    }

    public override void Visit(MultiStatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = PopSemanticNode();

        PushSemanticNode(new MultiStatementNode(items, node.ReturnType));
    }

    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        var set = PopSemanticNode();

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)PopSemanticNode();

        PushSemanticNode(new CteExpressionNode(sets, set, node.IsRecursive));
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var set = PopSemanticNode();

        var collector = new GetSelectFieldsVisitor();
        var traverser = new GetSelectFieldsTraverseVisitor(collector);

        set.Accept(traverser);

        var projectedColumns = collector.CollectedFieldNames;
        var hasProvisionalColumns = _provisionalRecursiveCteColumns.Remove(node.Name, out var provisionalColumns);
        var exportedColumns = hasProvisionalColumns
            ? provisionalColumns!
            : ResolveCteOutputColumns(node, projectedColumns);
        var exportedSet = hasProvisionalColumns || exportedColumns == projectedColumns
            ? set
            : RenameCteOutputColumns(set, exportedColumns);
        var table = new VariableTable(exportedColumns);
        var parentScope = _sourceBinding.CurrentScope.Parent ??
                          throw new VisitorException(
                              VisitorName,
                              "VisitCteInnerExpressionNode",
                              "CTE binding requires a parent scope.");

        if (!hasProvisionalColumns &&
            parentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(node.Name))
            throw new AliasAlreadyUsedException(node.Name, node.SpanOrEmpty());

        var tableSymbol = new TableSymbol(node.Name, new TransitionSchema(node.Name, table), table, false);
        if (hasProvisionalColumns)
            parentScope.ScopeSymbolTable.UpdateSymbol(node.Name, tableSymbol);
        else
            parentScope.ScopeSymbolTable.AddSymbol(node.Name, tableSymbol);

        if (_compilationOptions.UsePrimitiveTypeValidation)
            foreach (var fieldInfo in exportedColumns)
                if (!BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(fieldInfo.ColumnType))
                {
                    var fieldNode = new FieldNode(new IntegerNode("0", "s"), fieldInfo.ColumnIndex,
                        fieldInfo.ColumnName);
                    if (TryReportInvalidExpressionType(fieldNode, fieldInfo.ColumnType, $"CTE '{node.Name}'",
                            fieldNode))
                        continue;
                    throw new InvalidQueryExpressionTypeException(
                        fieldNode,
                        fieldInfo.ColumnType,
                        $"CTE '{node.Name}'");
                }

        PushSemanticNode(new CteInnerExpressionNode(
            exportedSet,
            node.Name,
            node.Columns,
            hasProvisionalColumns));
    }

    internal void PrepareRecursiveCteAnchor(CteInnerExpressionNode definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var anchor = PopSemanticNode();
        var collector = new GetSelectFieldsVisitor();
        anchor.Accept(new GetSelectFieldsTraverseVisitor(collector));

        var projectedColumns = collector.CollectedFieldNames;
        var exportedColumns = ResolveCteOutputColumns(definition, projectedColumns);
        var exportedAnchor = exportedColumns == projectedColumns
            ? anchor
            : RenameCteOutputColumns(anchor, exportedColumns);
        PushSemanticNode(exportedAnchor);

        var cteScope = _sourceBinding.CurrentScope;
        while (cteScope != null && cteScope.Name != "CTE")
            cteScope = cteScope.Parent;

        if (cteScope == null)
            throw new VisitorException(
                VisitorName,
                nameof(PrepareRecursiveCteAnchor),
                "Recursive CTE anchor binding requires an enclosing CTE scope.");

        var table = new VariableTable(exportedColumns);
        cteScope.ScopeSymbolTable.AddSymbol(
            definition.Name,
            new TableSymbol(
                definition.Name,
                new TransitionSchema(definition.Name, table),
                table,
                false));
        _provisionalRecursiveCteColumns.Add(definition.Name, exportedColumns);
    }

    private ISchemaColumn[] ResolveCteOutputColumns(
        CteInnerExpressionNode node,
        ISchemaColumn[] projectedColumns)
    {
        if (node.Columns.Length == 0)
            return projectedColumns;

        if (node.Columns.Length != projectedColumns.Length)
        {
            var message = ErrorCatalog.GetMessage(
                DiagnosticCode.MQ3077_CteColumnListCountMismatch,
                node.Name,
                node.Columns.Length,
                projectedColumns.Length);
            ReportOrThrowCteColumnListError(
                DiagnosticCode.MQ3077_CteColumnListCountMismatch,
                message,
                node.Columns.Length > 0 ? node.Columns[0].Span : node.SpanOrEmpty());
            return projectedColumns;
        }

        if (CteColumnListValidator.TryFindDuplicate(node, out var failure))
        {
            ReportOrThrowCteColumnListError(
                DiagnosticCode.MQ3078_DuplicateCteColumnName,
                failure.Message,
                failure.Span);
            return projectedColumns;
        }

        var exportedColumns = new ISchemaColumn[projectedColumns.Length];
        for (var index = 0; index < projectedColumns.Length; index++)
        {
            var projected = projectedColumns[index];
            exportedColumns[index] = new SchemaColumn(
                node.Columns[index].Name,
                projected.ColumnIndex,
                projected.ColumnType);
        }

        return exportedColumns;
    }

    private void ReportOrThrowCteColumnListError(
        DiagnosticCode code,
        string message,
        TextSpan span)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(code, message, span);
            return;
        }

        throw new CteColumnListValidationException(code, message, span);
    }

    private static Node RenameCteOutputColumns(Node node, ISchemaColumn[] columns)
    {
        return node switch
        {
            QueryNode query => RenameQueryOutputColumns(query, columns),
            UnionNode union => new UnionNode(
                union.ResultTableName,
                union.Keys,
                RenameCteOutputColumns(union.Left, columns),
                union.Right,
                union.IsNested,
                union.IsTheLastOne),
            UnionAllNode unionAll => new UnionAllNode(
                unionAll.ResultTableName,
                unionAll.Keys,
                RenameCteOutputColumns(unionAll.Left, columns),
                unionAll.Right,
                unionAll.IsNested,
                unionAll.IsTheLastOne),
            ExceptNode except => new ExceptNode(
                except.ResultTableName,
                except.Keys,
                RenameCteOutputColumns(except.Left, columns),
                except.Right,
                except.IsNested,
                except.IsTheLastOne),
            IntersectNode intersect => new IntersectNode(
                intersect.ResultTableName,
                intersect.Keys,
                RenameCteOutputColumns(intersect.Left, columns),
                intersect.Right,
                intersect.IsNested,
                intersect.IsTheLastOne),
            _ => node
        };
    }

    private static QueryNode RenameQueryOutputColumns(QueryNode query, ISchemaColumn[] columns)
    {
        var fields = new FieldNode[query.Select.Fields.Length];
        for (var index = 0; index < fields.Length; index++)
        {
            var field = query.Select.Fields[index];
            fields[index] = new FieldNode(
                field.Expression,
                field.FieldOrder,
                columns[index].ColumnName,
                true,
                field.Span);
        }

        var select = new SelectNode(fields, query.Select.IsDistinct, query.Select.Span);
        return new QueryNode(
            select,
            query.From,
            query.Where,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            query.Span);
    }
}
