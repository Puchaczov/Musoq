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
using Musoq.Parser.Nodes.From;
using Musoq.Plugins;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Helper class for processing PIVOT operations and generating corresponding C# syntax.
/// Handles PIVOT transformation logic, aggregation functions, and column generation.
/// </summary>
public static class PivotNodeProcessor
{
    /// <summary>
    /// Represents the result of processing a PIVOT operation.
    /// </summary>
    public class PivotProcessingResult
    {
        public StatementSyntax PivotTransformStatement { get; init; }
        public List<string> PivotColumns { get; init; }
        public string PivotTableVariable { get; init; }
    }

    /// <summary>
    /// Processes a PIVOT operation and generates the necessary C# code for row-to-column transformation.
    /// </summary>
    /// <param name="pivotNode">The PivotNode containing aggregation and FOR clause</param>
    /// <param name="sourceVariable">Variable name of the source data</param>
    /// <param name="scope">Current scope for symbol resolution</param>
    /// <returns>PivotProcessingResult containing generated transformation code</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    public static PivotProcessingResult ProcessPivotNode(
        PivotNode pivotNode,
        string sourceVariable,
        Scope scope)
    {
        ValidateParameters(pivotNode, sourceVariable, scope);

        var pivotTableVariable = $"pivotTable_{Guid.NewGuid():N}";
        var pivotColumns = ExtractPivotColumns(pivotNode);
        var transformStatement = GeneratePivotTransformation(
            pivotNode, sourceVariable, pivotTableVariable, pivotColumns);

        return new PivotProcessingResult
        {
            PivotTransformStatement = transformStatement,
            PivotColumns = pivotColumns,
            PivotTableVariable = pivotTableVariable
        };
    }

    /// <summary>
    /// Processes a PIVOT FROM node by visiting the source and applying PIVOT transformation.
    /// </summary>
    /// <param name="pivotFromNode">The PivotFromNode to process</param>
    /// <param name="nodes">Stack containing visited nodes</param>
    /// <param name="scope">Current scope for symbol resolution</param>
    /// <returns>PivotProcessingResult containing transformation logic</returns>
    public static PivotProcessingResult ProcessPivotFromNode(
        PivotFromNode pivotFromNode,
        Stack<SyntaxNode> nodes,
        Scope scope)
    {
        ValidateParameters(pivotFromNode, nodes, scope);

        // The source should be the last node on the stack
        var sourceExpression = nodes.Pop() as ExpressionSyntax 
            ?? throw new InvalidOperationException("Expected source expression for PIVOT operation");

        var sourceVariable = $"source_{Guid.NewGuid():N}";
        var pivotResult = ProcessPivotNode(pivotFromNode.Pivot, sourceVariable, scope);

        // Create a statement that declares the source variable and applies the PIVOT
        var sourceDeclaration = SyntaxFactory.VariableDeclaration(
            SyntaxFactory.IdentifierName("var"))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(sourceVariable))
                    .WithInitializer(SyntaxFactory.EqualsValueClause(sourceExpression))));

        var sourceStatement = SyntaxFactory.LocalDeclarationStatement(sourceDeclaration);

        // Combine source declaration with PIVOT transformation
        var blockStatements = new List<StatementSyntax> { sourceStatement, pivotResult.PivotTransformStatement };
        
        var combinedStatement = SyntaxFactory.Block(blockStatements);

        return new PivotProcessingResult
        {
            PivotTransformStatement = combinedStatement,
            PivotColumns = pivotResult.PivotColumns,
            PivotTableVariable = pivotResult.PivotTableVariable
        };
    }

    /// <summary>
    /// Extracts the pivot column names from the IN clause.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <returns>List of column names for pivoting</returns>
    private static List<string> ExtractPivotColumns(PivotNode pivotNode)
    {
        var columns = new List<string>();
        
        foreach (var value in pivotNode.InValues)
        {
            // For now, assume string literals - this can be enhanced later for dynamic values
            if (value is StringNode stringNode)
            {
                columns.Add(stringNode.Value);
            }
            else if (value is WordNode wordNode)
            {
                columns.Add(wordNode.Value);
            }
            else
            {
                // Fallback to a generic column name
                columns.Add($"Col_{columns.Count}");
            }
        }

        return columns;
    }

    /// <summary>
    /// Generates the C# code for the PIVOT transformation.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <param name="sourceVariable">Source data variable name</param>
    /// <param name="pivotTableVariable">Target pivot table variable name</param>
    /// <param name="pivotColumns">List of pivot column names</param>
    /// <returns>Statement containing the PIVOT transformation logic</returns>
    private static StatementSyntax GeneratePivotTransformation(
        PivotNode pivotNode,
        string sourceVariable,
        string pivotTableVariable,
        List<string> pivotColumns)
    {
        // For now, implement a basic PIVOT transformation using GroupBy and aggregation
        // This is a simplified implementation that can be enhanced later

        var csharpCode = $@"
var {pivotTableVariable} = {sourceVariable}
    .GroupBy(row => new {{ 
        // Group by all non-pivot, non-aggregation columns
        Key = ""placeholder""
    }})
    .Select(group => new {{
        {string.Join(",\n        ", pivotColumns.Select(col => 
            $"{SanitizeColumnName(col)} = group.Where(row => row.{GetForColumnName(pivotNode)} == \"{col}\").{GetAggregationMethod(pivotNode)}()"))}
    }})
    .ToList();";

        // Parse the generated C# code into syntax tree
        var statement = SyntaxFactory.ParseStatement(csharpCode);
        return statement;
    }

    /// <summary>
    /// Gets the column name specified in the FOR clause.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <returns>Column name to pivot on</returns>
    private static string GetForColumnName(PivotNode pivotNode)
    {
        // Extract the FOR column name
        if (pivotNode.ForColumn is WordNode wordNode)
        {
            return wordNode.Value;
        }
        else if (pivotNode.ForColumn is IdentifierNode identifierNode)
        {
            return identifierNode.Name;
        }
        
        return "Category"; // Fallback
    }

    /// <summary>
    /// Gets the aggregation method from the PIVOT node.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <returns>Aggregation method name</returns>
    private static string GetAggregationMethod(PivotNode pivotNode)
    {
        // For now, assume the first aggregation function
        if (pivotNode.AggregationExpressions.Length > 0)
        {
            var firstAggregation = pivotNode.AggregationExpressions[0];
            if (firstAggregation is AccessMethodNode accessMethodNode)
            {
                return accessMethodNode.Name.ToUpper() switch
                {
                    "SUM" => "Sum(row => row.Amount)",
                    "COUNT" => "Count()",
                    "AVG" => "Average(row => row.Amount)",
                    "MIN" => "Min(row => row.Amount)",
                    "MAX" => "Max(row => row.Amount)",
                    _ => "Count()"
                };
            }
        }
        
        return "Count()";
    }

    /// <summary>
    /// Sanitizes column names to be valid C# identifiers.
    /// </summary>
    /// <param name="columnName">Original column name</param>
    /// <returns>Sanitized column name</returns>
    private static string SanitizeColumnName(string columnName)
    {
        // Remove quotes and make valid C# identifier
        var sanitized = columnName.Trim('"', '\'')
            .Replace(" ", "_")
            .Replace("-", "_");
            
        // Ensure it starts with a letter or underscore
        if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_')
        {
            sanitized = "_" + sanitized;
        }
        
        return sanitized;
    }

    /// <summary>
    /// Validates the input parameters for null values.
    /// </summary>
    private static void ValidateParameters(object node, object additionalParam, Scope scope)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (additionalParam == null)
            throw new ArgumentNullException(nameof(additionalParam));
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
    }
}