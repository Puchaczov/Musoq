using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(GroupByNode node)
    {
        VisitChildrenThenNode(node);
    }

    public override void Visit(HavingNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        SetQueryPart(QueryPart.Having);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(QualifyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        SetQueryPart(QueryPart.Qualify);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public override void Visit(JoinFromNode node)
    {
        var joins = new Stack<JoinFromNode>();

        var join = node;
        while (join != null)
        {
            joins.Push(join);
            join = join.Source as JoinFromNode;
        }

        join = joins.Pop();
        join.Source.Accept(this);
        join.With.Accept(this);

        var sourceId = join.Source is ApplyFromNode ? MetaAttributes.ProcessedQueryId : join.Source.Id;
        var firstTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[sourceId]);
        var secondTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.With.Id]);
        secondTableSymbol = ApplyOrdinalityIfNeeded(join, Scope[join.With.Id], secondTableSymbol);

        EnsureSupportedJoinType(join.JoinType);
        if (MakesLeftSideNullable(join.JoinType))
        {
            firstTableSymbol = firstTableSymbol.MakeNullableIfPossible();
            Scope.ScopeSymbolTable.UpdateSymbol(Scope[sourceId], firstTableSymbol);
        }

        if (MakesRightSideNullable(join.JoinType))
        {
            secondTableSymbol = secondTableSymbol.MakeNullableIfPossible();
            Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.With.Id], secondTableSymbol);
        }

        var id = $"{Scope[sourceId]}{Scope[join.With.Id]}";

        Scope.ScopeSymbolTable.AddSymbol(id, firstTableSymbol.MergeSymbols(secondTableSymbol));
        Scope[MetaAttributes.ProcessedQueryId] = id;

        join.Expression.Accept(this);
        join.TieBreak?.Accept(this);
        Scope.ScopeSymbolTable.UpdateSymbol(id, CreateJoinOutputSymbol(join.JoinType, id, firstTableSymbol, secondTableSymbol));
        join.Accept(Visitor);

        while (joins.Count > 0)
        {
            join = joins.Pop();
            join.With.Accept(this);

            var currentTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.With.Id]);
            var previousTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(id);
            currentTableSymbol = ApplyOrdinalityIfNeeded(join, Scope[join.With.Id], currentTableSymbol);

            EnsureSupportedJoinType(join.JoinType);
            if (MakesLeftSideNullable(join.JoinType))
            {
                previousTableSymbol = previousTableSymbol.MakeNullableIfPossible();
                Scope.ScopeSymbolTable.UpdateSymbol(id, previousTableSymbol);
            }

            if (MakesRightSideNullable(join.JoinType))
            {
                currentTableSymbol = currentTableSymbol.MakeNullableIfPossible();
                Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.With.Id], currentTableSymbol);
            }

            id = $"{id}{Scope[join.With.Id]}";

            Scope.ScopeSymbolTable.AddSymbol(id, previousTableSymbol.MergeSymbols(currentTableSymbol));
            Scope[MetaAttributes.ProcessedQueryId] = id;

            join.Expression.Accept(this);
            join.TieBreak?.Accept(this);
            Scope.ScopeSymbolTable.UpdateSymbol(id, CreateJoinOutputSymbol(join.JoinType, id, previousTableSymbol, currentTableSymbol));
            join.Accept(Visitor);
        }
    }

    public override void Visit(ApplyFromNode node)
    {
        var applies = new Stack<ApplyFromNode>();

        var apply = node;
        while (apply != null)
        {
            applies.Push(apply);
            apply = apply.Source as ApplyFromNode;
        }

        apply = applies.Pop();
        apply.Source.Accept(this);
        apply.With.Accept(this);

        var sourceId = apply.Source is JoinFromNode ? MetaAttributes.ProcessedQueryId : apply.Source.Id;
        var firstTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[sourceId]);
        var secondTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[apply.With.Id]);
        secondTableSymbol = ApplyOrdinalityIfNeeded(apply, Scope[apply.With.Id], secondTableSymbol);

        switch (apply.ApplyType)
        {
            case ApplyType.Cross:
                break;
            case ApplyType.Outer:
                secondTableSymbol = secondTableSymbol.MakeNullableIfPossible();
                Scope.ScopeSymbolTable.UpdateSymbol(Scope[apply.With.Id], secondTableSymbol);
                break;
            default:
                throw new InvalidOperationException($"Unsupported apply type '{apply.ApplyType}'.");
        }

        var id = $"{Scope[sourceId]}{Scope[apply.With.Id]}";

        Scope.ScopeSymbolTable.AddSymbol(
            id,
            CreateApplyOutputSymbol(apply.ApplyType, firstTableSymbol, secondTableSymbol));
        Scope[MetaAttributes.ProcessedQueryId] = id;

        apply.Accept(Visitor);

        while (applies.Count > 0)
        {
            apply = applies.Pop();
            apply.With.Accept(this);

            var currentTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[apply.With.Id]);
            var previousTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(id);
            currentTableSymbol = ApplyOrdinalityIfNeeded(apply, Scope[apply.With.Id], currentTableSymbol);

            switch (apply.ApplyType)
            {
                case ApplyType.Cross:
                    break;
                case ApplyType.Outer:
                    currentTableSymbol = currentTableSymbol.MakeNullableIfPossible();
                    Scope.ScopeSymbolTable.UpdateSymbol(Scope[apply.With.Id], currentTableSymbol);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported apply type '{apply.ApplyType}'.");
            }

            id = $"{id}{Scope[apply.With.Id]}";

            Scope.ScopeSymbolTable.AddSymbol(
                id,
                CreateApplyOutputSymbol(apply.ApplyType, previousTableSymbol, currentTableSymbol));
            Scope[MetaAttributes.ProcessedQueryId] = id;

            apply.Accept(Visitor);
        }
    }

    private TableSymbol ApplyOrdinalityIfNeeded(
        FromNode sourceNode,
        string rightAlias,
        TableSymbol rightTableSymbol)
    {
        var withOrdinality = sourceNode switch
        {
            ApplyFromNode apply => apply.WithOrdinality,
            JoinFromNode join => join.WithOrdinality,
            _ => false
        };
        if (!withOrdinality)
            return rightTableSymbol;

        const string ordinalityColumnName = "Ordinal";
        if (rightTableSymbol.AliasContainsColumn(rightAlias, ordinalityColumnName))
        {
            throw new VisitorException(
                nameof(BuildMetadataAndInferTypesTraverseVisitor),
                nameof(ApplyOrdinalityIfNeeded),
                $"WITH ORDINALITY cannot be used because apply alias '{rightAlias}' already exposes an Ordinal column.",
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                sourceNode.SpanOrEmpty());
        }

        var ordinalColumn = new SchemaColumn(
            ordinalityColumnName,
            rightTableSymbol.GetColumns(rightAlias).Length,
            typeof(int));
        var updatedSymbol = rightTableSymbol.WithAdditionalColumn(rightAlias, ordinalColumn);

        Scope.ScopeSymbolTable.UpdateSymbol(rightAlias, updatedSymbol);
        if (Visitor is BuildMetadataAndInferTypesVisitor metadataVisitor)
            metadataVisitor.UpdateInferredColumnsByAlias(rightAlias, updatedSymbol.GetColumns(rightAlias));

        return updatedSymbol;
    }

    public override void Visit(ValuesFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ValuesSourceLiteralValidator.Validate(node);

        VisitChildrenThenNode(node);
    }
}
