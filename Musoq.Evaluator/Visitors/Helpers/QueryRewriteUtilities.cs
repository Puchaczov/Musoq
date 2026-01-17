using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public static Node RewriteNullableBoolExpressions(Node node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        var nullableBoolType = typeof(bool?);
        if (node.ReturnType != nullableBoolType)
            return node;
            
        return new AndNode(new IsNullNode(node, true), new EqualityNode(node, new BooleanNode(true)));
    }

    /// <summary>
    /// Removes string prefix and suffix characters from field names.
    /// </summary>
    /// <param name="fieldName">The field name to rewrite.</param>
    /// <returns>The rewritten field name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static string RewriteFieldNameWithoutStringPrefixAndSuffix(string fieldName)
    {
        if (fieldName == null)
            throw new ArgumentNullException(nameof(fieldName));

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
    /// <exception cref="ArgumentNullException">Thrown when methods or node is null.</exception>
    public static bool HasMethod(IEnumerable<AccessMethodNode> methods, AccessMethodNode node)
    {
        if (methods == null)
            throw new ArgumentNullException(nameof(methods));
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return methods.Any(f => f.ToString() == node.ToString());
    }

    /// <summary>
    /// Creates a RefreshNode with filtered methods.
    /// </summary>
    /// <param name="refreshMethods">The refresh methods to filter.</param>
    /// <returns>A RefreshNode with filtered methods.</returns>
    /// <exception cref="ArgumentNullException">Thrown when refreshMethods is null.</exception>
    public static RefreshNode CreateRefreshMethods(IReadOnlyList<AccessMethodNode> refreshMethods)
    {
        if (refreshMethods == null)
            throw new ArgumentNullException(nameof(refreshMethods));

        var methods = new List<AccessMethodNode>();

        foreach (var method in refreshMethods)
        {
            if (method == null)
                continue; 

            if (method.Method?.GetCustomAttribute<AggregateSetDoNotResolveAttribute>() != null)
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
    /// <exception cref="ArgumentNullException">Thrown when split is null.</exception>
    /// <exception cref="ArgumentException">Thrown when split array is malformed.</exception>
    public static bool IsQueryWithMixedAggregateAndNonAggregateMethods(FieldNode[][] split)
    {
        if (split == null)
            throw new ArgumentNullException(nameof(split));
        if (split.Length < 2)
            throw new ArgumentException("Split array must contain at least 2 elements", nameof(split));
        if (split[0] == null || split[1] == null)
            throw new ArgumentException("Split array elements cannot be null", nameof(split));

        return split[0].Length > 0 && split[0].Length != split[1].Length;
    }

    /// <summary>
    /// Concatenates aggregate fields with group by fields.
    /// </summary>
    /// <param name="selectFields">The select fields.</param>
    /// <param name="groupByFields">The group by fields.</param>
    /// <returns>The concatenated fields.</returns>
    /// <exception cref="ArgumentNullException">Thrown when selectFields or groupByFields is null.</exception>
    public static FieldNode[] ConcatAggregateFieldsWithGroupByFields(FieldNode[] selectFields, FieldNode[] groupByFields)
    {
        if (selectFields == null)
            throw new ArgumentNullException(nameof(selectFields));
        if (groupByFields == null)
            throw new ArgumentNullException(nameof(groupByFields));

        var fields = new List<FieldNode>(selectFields);
        var nextOrder = -1;

        if (selectFields.Length > 0)
        if (selectFields.Any(f => f == null))
        {
            if (selectFields.Any(f => f == null))
                throw new ArgumentException("selectFields contains null elements.", nameof(selectFields));
        }

        if (selectFields.Length > 0)
            nextOrder = selectFields.Max(f => f.FieldOrder);

        foreach (var groupField in groupByFields)
        {
            if (groupField?.Expression == null)
                continue; 

            var hasField =
                selectFields.Any(field => field?.Expression?.ToString() == groupField.Expression.ToString());

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
    /// <exception cref="ArgumentNullException">Thrown when accessColumnNodes or joinFromNode is null.</exception>
    public static Func<AccessColumnNode, bool> IncludeKnownColumns(AccessColumnNode[] accessColumnNodes, BinaryFromNode joinFromNode)
    {
        if (accessColumnNodes == null)
            throw new ArgumentNullException(nameof(accessColumnNodes));
        if (joinFromNode == null)
            throw new ArgumentNullException(nameof(joinFromNode));

        return accessColumnNode =>
        {
            if (accessColumnNode == null || joinFromNode.Source == null || joinFromNode.With == null)
                return false;

            if (accessColumnNode.Alias == joinFromNode.Source.Alias)
            {
                return accessColumnNodes.Any(f =>
                    f != null && f.Name == accessColumnNode.Name && f.Alias == joinFromNode.Source.Alias);
            }
                        
            if (accessColumnNode.Alias == joinFromNode.With.Alias)
            {
                return accessColumnNodes.Any(f =>
                    f != null && f.Name == accessColumnNode.Name && f.Alias == joinFromNode.With.Alias);
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
    /// <exception cref="ArgumentNullException">Thrown when accessColumnNodes or binaryFromNode is null.</exception>
    public static Func<AccessColumnNode, bool> IncludeKnownColumnsForWithOnly(AccessColumnNode[] accessColumnNodes, BinaryFromNode binaryFromNode)
    {
        if (accessColumnNodes == null)
            throw new ArgumentNullException(nameof(accessColumnNodes));
        if (binaryFromNode == null)
            throw new ArgumentNullException(nameof(binaryFromNode));

        return accessColumnNode =>
        {
            if (accessColumnNode == null || binaryFromNode.Source == null || binaryFromNode.With == null)
                return false;

            if (accessColumnNode.Alias == binaryFromNode.Source.Alias)
            {
                return true;
            }

            if (accessColumnNode.Alias == binaryFromNode.With.Alias)
            {
                return accessColumnNodes.Any(f =>
                    f != null && f.Name == accessColumnNode.Name && f.Alias == binaryFromNode.With.Alias);
            }

            return false;
        };
    }
}