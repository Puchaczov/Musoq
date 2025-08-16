using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Utility methods for query rewriting operations.
/// Contains common helper methods extracted from RewriteQueryVisitor.
/// </summary>
public static class QueryRewriteUtilities
{
    /// <summary>
    /// Rewrites nullable boolean expressions to handle proper null semantics.
    /// </summary>
    /// <param name="node">The node to rewrite.</param>
    /// <returns>The rewritten node.</returns>
    public static Node RewriteNullableBoolExpressions(Node node)
    {
        var nullableBoolType = typeof(bool?);
        if (node.ReturnType != nullableBoolType || node is BinaryNode)
            return node;
            
        return new AndNode(new IsNullNode(node, true), new EqualityNode(node, new BooleanNode(true)));
    }

    /// <summary>
    /// Removes string prefix and suffix characters from field names.
    /// </summary>
    /// <param name="fieldName">The field name to rewrite.</param>
    /// <returns>The rewritten field name.</returns>
    public static string RewriteFieldNameWithoutStringPrefixAndSuffix(string fieldName)
    {
        var pattern = @"(?<!\\)'";
        var result = Regex.Replace(fieldName, pattern, string.Empty);
        result = result.Replace("\\'", "'");

        return result;
    }

    /// <summary>
    /// Checks if a method already exists in the collection.
    /// </summary>
    /// <param name="methods">The collection of methods to check.</param>
    /// <param name="node">The method node to find.</param>
    /// <returns>True if the method exists, false otherwise.</returns>
    public static bool HasMethod(IEnumerable<AccessMethodNode> methods, AccessMethodNode node)
    {
        return methods.Any(f => f.ToString() == node.ToString());
    }

    /// <summary>
    /// Creates a RefreshNode with filtered methods.
    /// </summary>
    /// <param name="refreshMethods">The refresh methods to filter.</param>
    /// <returns>A RefreshNode with filtered methods.</returns>
    public static RefreshNode CreateRefreshMethods(IReadOnlyList<AccessMethodNode> refreshMethods)
    {
        var methods = new List<AccessMethodNode>();

        foreach (var method in refreshMethods)
        {
            if (method.Method.GetCustomAttribute<AggregateSetDoNotResolveAttribute>() != null)
                continue;

            if (!HasMethod(methods, method))
                methods.Add(method);
        }

        return new RefreshNode(methods.ToArray());
    }

    /// <summary>
    /// Checks if a query has mixed aggregate and non-aggregate methods.
    /// </summary>
    /// <param name="split">The split fields array.</param>
    /// <returns>True if there are mixed aggregate and non-aggregate methods.</returns>
    public static bool IsQueryWithMixedAggregateAndNonAggregateMethods(FieldNode[][] split)
    {
        return split[0].Length > 0 && split[0].Length != split[1].Length;
    }

    /// <summary>
    /// Concatenates aggregate fields with group by fields.
    /// </summary>
    /// <param name="selectFields">The select fields.</param>
    /// <param name="groupByFields">The group by fields.</param>
    /// <returns>The concatenated fields.</returns>
    public static FieldNode[] ConcatAggregateFieldsWithGroupByFields(FieldNode[] selectFields, FieldNode[] groupByFields)
    {
        var fields = new List<FieldNode>(selectFields);
        var nextOrder = -1;

        if (selectFields.Length > 0)
            nextOrder = selectFields.Max(f => f.FieldOrder);

        foreach (var groupField in groupByFields)
        {
            var hasField =
                selectFields.Any(field => field.Expression.ToString() == groupField.Expression.ToString());

            if (!hasField) fields.Add(new FieldNode(groupField.Expression, ++nextOrder, string.Empty));
        }

        return fields.ToArray();
    }

    /// <summary>
    /// Creates a function to include known columns for both sides of a join.
    /// </summary>
    /// <param name="accessColumnNodes">The access column nodes to check.</param>
    /// <param name="joinFromNode">The join node.</param>
    /// <returns>A function that determines if a column should be included.</returns>
    public static Func<AccessColumnNode, bool> IncludeKnownColumns(AccessColumnNode[] accessColumnNodes, BinaryFromNode joinFromNode)
    {
        return accessColumnNode =>
        {
            if (accessColumnNode.Alias == joinFromNode.Source.Alias)
            {
                return accessColumnNodes.Any(f =>
                    f.Name == accessColumnNode.Name && f.Alias == joinFromNode.Source.Alias);
            }
                        
            if (accessColumnNode.Alias == joinFromNode.With.Alias)
            {
                return accessColumnNodes.Any(f =>
                    f.Name == accessColumnNode.Name && f.Alias == joinFromNode.With.Alias);
            }
                        
            return false;
        };
    }

    /// <summary>
    /// Creates a function to include known columns for the "with" side only.
    /// </summary>
    /// <param name="accessColumnNodes">The access column nodes to check.</param>
    /// <param name="binaryFromNode">The binary node.</param>
    /// <returns>A function that determines if a column should be included.</returns>
    public static Func<AccessColumnNode, bool> IncludeKnownColumnsForWithOnly(AccessColumnNode[] accessColumnNodes, BinaryFromNode binaryFromNode)
    {
        return accessColumnNode =>
        {
            if (accessColumnNode.Alias == binaryFromNode.Source.Alias)
            {
                return true;
            }

            if (accessColumnNode.Alias == binaryFromNode.With.Alias)
            {
                return accessColumnNodes.Any(f =>
                    f.Name == accessColumnNode.Name && f.Alias == binaryFromNode.With.Alias);
            }

            return false;
        };
    }
}