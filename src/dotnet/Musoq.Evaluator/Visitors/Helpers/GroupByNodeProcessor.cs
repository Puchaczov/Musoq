using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for processing GroupByNode operations and generating corresponding syntax.
///     Handles group key generation, group value creation, and field names processing.
/// </summary>
public static class GroupByNodeProcessor
{
    /// <summary>
    ///     Processes a GroupByNode and generates the necessary syntax structures.
    /// </summary>
    /// <param name="node">The GroupByNode to process</param>
    /// <param name="nodes">Stack containing field expressions</param>
    /// <param name="scope">Current scope for symbol resolution</param>
    /// <returns>GroupByProcessingResult containing all generated syntax</returns>
    /// <exception cref="ArgumentNullException">Thrown when node, nodes, or scope is null</exception>
    public static GroupByProcessingResult ProcessGroupByNode(
        GroupByNode node,
        Stack<SyntaxNode> nodes,
        Scope scope)
    {
        ValidateParameters(node, nodes, scope);

        var args = new SyntaxNode[node.Fields.Length];

        SyntaxNode having = null;
        if (node.Having != null)
            having = nodes.Pop();

        var syntaxList = new ExpressionSyntax[node.Fields.Length];


        for (int i = 0, j = node.Fields.Length - 1; i < node.Fields.Length; i++, j--)
            args[j] = nodes.Pop();

        var keysElements = GenerateGroupKeys(args, syntaxList);

        var groupValues = CreateGroupValues(syntaxList);
        var groupKeys = CreateGroupKeys(keysElements);
        var groupFieldsStatement = CreateGroupFieldsStatement(scope);

        return new GroupByProcessingResult
        {
            GroupKeys = groupKeys,
            GroupValues = groupValues,
            GroupHaving = having,
            GroupFieldsStatement = groupFieldsStatement
        };
    }

    /// <summary>
    ///     Generates group keys from the provided arguments.
    /// </summary>
    /// <param name="args">Array of field expressions</param>
    /// <param name="syntaxList">Output array for syntax expressions</param>
    /// <returns>List of ObjectCreationExpressionSyntax for group keys</returns>
    private static List<ObjectCreationExpressionSyntax> GenerateGroupKeys(
        SyntaxNode[] args,
        ExpressionSyntax[] syntaxList)
    {
        var keysElements = new List<ObjectCreationExpressionSyntax>();

        for (var i = 0; i < args.Length; i++)
        {
            syntaxList[i] = SyntaxHelper.CreateArrayOfObjects(
                args.Take(i + 1).Cast<ExpressionSyntax>().ToArray());

            var currentKey = new ArgumentSyntax[i + 1];
            for (var j = i; j >= 0; j--)
                currentKey[j] = SyntaxFactory.Argument((ExpressionSyntax)args[j]);

            keysElements.Add(
                SyntaxHelper.CreateObjectOf(
                    nameof(GroupKey),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(currentKey))));
        }

        return keysElements;
    }

    /// <summary>
    ///     Creates the group values variable declaration.
    /// </summary>
    /// <param name="syntaxList">Array of expressions for values</param>
    /// <returns>VariableDeclarationSyntax for group values</returns>
    private static VariableDeclarationSyntax CreateGroupValues(ExpressionSyntax[] syntaxList)
    {
        return SyntaxHelper.CreateAssignment(
            "values",
            SyntaxHelper.CreateArrayOf(nameof(Object), syntaxList, 2));
    }

    /// <summary>
    ///     Creates the group keys variable declaration.
    /// </summary>
    /// <param name="keysElements">List of ObjectCreationExpressionSyntax for keys</param>
    /// <returns>VariableDeclarationSyntax for group keys</returns>
    private static VariableDeclarationSyntax CreateGroupKeys(
        List<ObjectCreationExpressionSyntax> keysElements)
    {
        return SyntaxHelper.CreateAssignment(
            "keys",
            SyntaxHelper.CreateArrayOfObjects(
                nameof(GroupKey),
                keysElements.Cast<ExpressionSyntax>().ToArray()));
    }

    /// <summary>
    ///     Creates the group fields statement for field names processing.
    /// </summary>
    /// <param name="scope">Current scope for symbol resolution</param>
    /// <returns>StatementSyntax for group fields declaration</returns>
    private static StatementSyntax CreateGroupFieldsStatement(Scope scope)
    {
        var groupFields = scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>("groupFields");

        var fieldNames = new StringBuilder();
        fieldNames.Append("var groupFieldsNames = new string[][]{");


        for (var i = 0; i < groupFields.Names.Length - 1; i++)
        {
            var fieldName =
                $"new string[]{{{string.Join(",", groupFields.Names.Where((f, idx) => idx <= i).Select(f => $"@\"{f}\""))}}}";
            fieldNames.Append(fieldName);
            fieldNames.Append(',');
        }


        var lastFieldName =
            $"new string[]{{{string.Join(",", groupFields.Names.Select(f => $"@\"{f}\""))}}}";
        fieldNames.Append(lastFieldName);
        fieldNames.Append("};");

        return SyntaxFactory.ParseStatement(fieldNames.ToString());
    }

    /// <summary>
    ///     Validates the input parameters for ProcessGroupByNode.
    /// </summary>
    /// <param name="node">The GroupByNode to validate</param>
    /// <param name="nodes">The nodes stack to validate</param>
    /// <param name="scope">The scope to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    private static void ValidateParameters(GroupByNode node, Stack<SyntaxNode> nodes, Scope scope)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
    }

    /// <summary>
    ///     Represents the result of processing a GroupByNode.
    /// </summary>
    public class GroupByProcessingResult
    {
        public VariableDeclarationSyntax GroupKeys { get; init; }
        public VariableDeclarationSyntax GroupValues { get; init; }
        public SyntaxNode GroupHaving { get; init; }
        public StatementSyntax GroupFieldsStatement { get; init; }
    }
}
