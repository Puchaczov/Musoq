using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for processing fields in the RewriteQueryVisitor.
/// Handles field creation, splitting, and transformation operations.
/// </summary>
public static class FieldProcessingHelper
{
    /// <summary>
    /// Creates fields from old fields using a node stack.
    /// </summary>
    /// <param name="oldFields">The original fields.</param>
    /// <param name="nodes">The node stack to pop from.</param>
    /// <returns>The created fields.</returns>
    public static FieldNode[] CreateFields(FieldNode[] oldFields, Stack<Node> nodes)
    {
        var reorderedList = new FieldNode[oldFields.Length];
        var fields = new List<FieldNode>(reorderedList.Length);

        for (var i = reorderedList.Length - 1; i >= 0; i--) 
            reorderedList[i] = nodes.Pop() as FieldNode;

        for (int i = 0, j = reorderedList.Length, p = 0; i < j; ++i)
        {
            var field = reorderedList[i];

            if (field.Expression is AllColumnsNode)
            {
                continue;
            }

            fields.Add(new FieldNode(field.Expression, p++, field.FieldName));
        }

        return fields.ToArray();
    }

    /// <summary>
    /// Creates and concatenates fields from two table symbols.
    /// </summary>
    /// <param name="left">The left table symbol.</param>
    /// <param name="lAlias">The left alias.</param>
    /// <param name="right">The right table symbol.</param>
    /// <param name="rAlias">The right alias.</param>
    /// <param name="createLeftAndRightFieldName">Function to create field names.</param>
    /// <param name="includeKnownColumn">Function to determine if a column should be included.</param>
    /// <param name="startAt">Starting index for field order.</param>
    /// <returns>The created and concatenated fields.</returns>
    public static FieldNode[] CreateAndConcatFields(
        TableSymbol left, 
        string lAlias, 
        TableSymbol right, 
        string rAlias, 
        Func<string, string, string> createLeftAndRightFieldName, 
        Func<AccessColumnNode, bool> includeKnownColumn = null, 
        int startAt = 0)
    {
        return CreateAndConcatFields(
            left, 
            lAlias, 
            right, 
            rAlias, 
            createLeftAndRightFieldName, 
            createLeftAndRightFieldName, 
            (name, alias) => name,
            (name, alias) => name,
            includeKnownColumn, 
            startAt);
    }

    /// <summary>
    /// Creates and concatenates fields from two table symbols with detailed configuration.
    /// </summary>
    /// <param name="left">The left table symbol.</param>
    /// <param name="leftAlias">The left alias.</param>
    /// <param name="right">The right table symbol.</param>
    /// <param name="rightAlias">The right alias.</param>
    /// <param name="createLeftFieldName">Function to create left field names.</param>
    /// <param name="createRightFieldName">Function to create right field names.</param>
    /// <param name="createLeftColumnName">Function to create left column names.</param>
    /// <param name="createRightColumnName">Function to create right column names.</param>
    /// <param name="isKnownColumn">Function to determine if a column is known.</param>
    /// <param name="startAt">Starting index for field order.</param>
    /// <returns>The created and concatenated fields.</returns>
    public static FieldNode[] CreateAndConcatFields(
        TableSymbol left, 
        string leftAlias, 
        TableSymbol right, 
        string rightAlias,
        Func<string, string, string> createLeftFieldName, 
        Func<string, string, string> createRightFieldName, 
        Func<string, string, string> createLeftColumnName,
        Func<string, string, string> createRightColumnName,
        Func<AccessColumnNode, bool> isKnownColumn = null,
        int startAt = 0)
    {
        var fields = new List<FieldNode>();

        var i = startAt;

        foreach (var compoundTable in left.CompoundTables)
        {
            foreach (var column in left.GetColumns(compoundTable))
            {
                var accessColumnNode = new AccessColumnNode(
                    createLeftColumnName(column.ColumnName, compoundTable),
                    leftAlias,
                    column.ColumnType,
                    TextSpan.Empty);
                    
                if (isKnownColumn == null || !isKnownColumn(accessColumnNode))
                    continue;
                    
                fields.Add(new FieldNode(accessColumnNode, i++, createLeftFieldName(column.ColumnName, compoundTable)));
            }
        }

        foreach (var compoundTable in right.CompoundTables)
        {
            foreach (var column in right.GetColumns(compoundTable))
            {
                var accessColumnNode = new AccessColumnNode(
                    createRightColumnName(column.ColumnName, compoundTable),
                    rightAlias,
                    column.ColumnType,
                    TextSpan.Empty);
                    
                if (isKnownColumn == null || !isKnownColumn(accessColumnNode))
                    continue;
                    
                fields.Add(
                    new FieldNode(accessColumnNode, i++, createRightFieldName(column.ColumnName, compoundTable)));
            }
        }
            
        return fields.ToArray();
    }

    /// <summary>
    /// Splits fields between aggregate and non-aggregate methods.
    /// </summary>
    /// <param name="fieldsToSplit">The fields to split.</param>
    /// <param name="groupByFields">The group by fields.</param>
    /// <param name="useOuterFields">Whether to create outer fields.</param>
    /// <returns>Array of split fields: [aggregate, outer, raw aggregate].</returns>
    public static FieldNode[][] SplitBetweenAggregateAndNonAggregate(FieldNode[] fieldsToSplit, FieldNode[] groupByFields, bool useOuterFields)
    {
        var nestedFields = new List<FieldNode>();
        var outerFields = new List<FieldNode>();
        var rawNestedFields = new List<FieldNode>();

        var fieldOrder = 0;

        foreach (var root in fieldsToSplit)
        {
            var subNodes = new Stack<Node>();

            subNodes.Push(root.Expression);

            while (subNodes.Count > 0)
            {
                var subNode = subNodes.Pop();

                switch (subNode)
                {
                    case AccessMethodNode aggregateMethod when aggregateMethod.IsAggregateMethod():
                    {
                        var subNodeStr = subNode.ToString();
                        if (nestedFields.Select(f => f.Expression.ToString()).Contains(subNodeStr))
                            continue;

                        var nameArg = (WordNode) aggregateMethod.Arguments.Args[0];
                        nestedFields.Add(new FieldNode(subNode, fieldOrder, nameArg.Value));
                        rawNestedFields.Add(new FieldNode(subNode, fieldOrder, string.Empty));
                        fieldOrder += 1;
                        break;
                    }
                    case AccessMethodNode method:
                    {
                        foreach (var arg in method.Arguments.Args)
                            subNodes.Push(arg);
                        break;
                    }
                    case BinaryNode binary:
                        subNodes.Push(binary.Left);
                        subNodes.Push(binary.Right);
                        break;
                }
            }

            if (!useOuterFields)
                continue;

            var rewriter = new RewriteFieldWithGroupMethodCall(groupByFields);
            var traverser = new CloneTraverseVisitor(rewriter);

            root.Accept(traverser);

            outerFields.Add(rewriter.Expression);
        }

        var retFields = new FieldNode[3][];

        retFields[0] = nestedFields.ToArray();
        retFields[1] = outerFields.ToArray();
        retFields[2] = rawNestedFields.ToArray();

        return retFields;
    }

    /// <summary>
    /// Creates ORDER BY access fields after GROUP BY processing.
    /// </summary>
    /// <param name="fieldsToSplit">The fields to split.</param>
    /// <param name="groupByFields">The group by fields.</param>
    /// <returns>The processed ordered fields.</returns>
    public static FieldOrderedNode[] CreateAfterGroupByOrderByAccessFields(FieldOrderedNode[] fieldsToSplit, FieldNode[] groupByFields)
    {
        var outerFields = new List<FieldOrderedNode>();

        foreach (var root in fieldsToSplit)
        {
            var rewriter = new RewriteFieldOrderedWithGroupMethodCall(groupByFields);
            var traverser = new CloneTraverseVisitor(rewriter);

            root.Accept(traverser);

            outerFields.Add(rewriter.Expression);
        }

        return outerFields.ToArray();
    }
}