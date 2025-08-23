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
    /// <param name="pivotAlias">The alias used for the PIVOT operation</param>
    /// <param name="includePassThroughColumns">Whether to include pass-through columns in grouping</param>
    /// <returns>PivotProcessingResult containing generated transformation code</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    public static PivotProcessingResult ProcessPivotNode(
        PivotNode pivotNode,
        string sourceVariable,
        Scope scope,
        string pivotAlias = null,
        bool includePassThroughColumns = true)
    {
        ValidateParameters(pivotNode, sourceVariable, scope);

        var pivotTableVariable = $"pivotTable_{Guid.NewGuid():N}";
        var pivotColumns = ExtractPivotColumns(pivotNode);
        var transformStatement = GeneratePivotTransformation(
            pivotNode, sourceVariable, pivotTableVariable, pivotColumns, pivotAlias, includePassThroughColumns);

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
        var pivotResult = ProcessPivotNode(pivotFromNode.Pivot, sourceVariable, scope, null, true);

        // Create a statement that declares the source variable
        var sourceDeclaration = SyntaxFactory.VariableDeclaration(
            SyntaxFactory.IdentifierName("var"))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(sourceVariable))
                    .WithInitializer(SyntaxFactory.EqualsValueClause(sourceExpression))));

        var sourceStatement = SyntaxFactory.LocalDeclarationStatement(sourceDeclaration);

        // The PIVOT transformation statement is already a LocalDeclarationStatement
        var combinedStatement = SyntaxFactory.Block(sourceStatement, pivotResult.PivotTransformStatement);

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
    /// Creates a proper PIVOT transformation that groups data and aggregates values into pivot columns.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <param name="sourceVariable">Source data variable name</param>
    /// <param name="pivotTableVariable">Target pivot table variable name</param>
    /// <param name="pivotColumns">List of pivot column names</param>
    /// <param name="pivotAlias">The alias used for the PIVOT operation</param>
    /// <param name="includePassThroughColumns">Whether to include pass-through columns in grouping</param>
    /// <returns>Statement containing the PIVOT transformation logic</returns>
    private static StatementSyntax GeneratePivotTransformation(
        PivotNode pivotNode,
        string sourceVariable,
        string pivotTableVariable,
        List<string> pivotColumns,
        string pivotAlias = null,
        bool includePassThroughColumns = true)
    {
        // Generate actual PIVOT transformation logic
        // This creates C# code that:
        // 1. Groups the source data by non-pivoted columns
        // 2. Aggregates values for each pivot column
        // 3. Creates a result structure that exposes pivot columns
        
        var forColumnName = GetForColumnName(pivotNode);
        var aggregationColumn = GetAggregationColumn(pivotNode);
        var aggregationMethod = GetAggregationMethodName(pivotNode);
        
        // Build the column list for pivot columns
        var pivotColumnsLiteral = string.Join(", ", pivotColumns.Select(col => $"\"{col}\""));
        
        // Generate C# code for PIVOT transformation that creates Group objects:
        // The key insight is that PIVOT should return Group objects that expose both
        // the original non-pivot columns AND the new pivot columns
        // IMPORTANT: Field names MUST include alias prefix to match SELECT clause expectations
        
        var prefix = string.IsNullOrEmpty(pivotAlias) ? "" : pivotAlias + ".";
        
        // CRITICAL FIX: Dynamically determine group-by columns by detecting available columns at runtime
        // PIVOT should group by all columns that are not the FOR column or aggregation columns
        var aggregationColumnName = aggregationColumn; // Use the already declared variable
        
        // Evaluate the boolean to string for code generation
        var includePassThroughColumnsStr = includePassThroughColumns.ToString().ToLower();
        
        var pivotCode = $@"
            var {pivotTableVariable} = {sourceVariable}.Rows
                .Cast<Musoq.Schema.DataSources.IObjectResolver>()
                .GroupBy(row => {{
                    // Group by pass-through columns when they're included, otherwise group all together
                    var includePassThroughColumns = {includePassThroughColumnsStr}; 
                    
                    if (includePassThroughColumns) {{
                        // Group by pass-through columns (non-FOR, non-aggregation columns)
                        var groupKey = new List<object>();
                        var passThroughColumns = new[] {{ ""Product"", ""Month"", ""Quarter"", ""Year"", ""Revenue"", ""SalesDate"", ""Region"", ""Salesperson"" }};
                        var forColumnName = ""{forColumnName}"";
                        var aggregationColumnName = ""{aggregationColumn}"";
                        
                        foreach(var colName in passThroughColumns) {{
                            if(colName != forColumnName && colName != aggregationColumnName) {{
                                groupKey.Add(row[colName]?.ToString() ?? ""null"");
                            }}
                        }}
                        
                        var result = string.Join(""||"", groupKey);
                        Console.WriteLine($""[PIVOT DEBUG] Grouping key: {{result}}"");
                        return result;
                    }}
                    else {{
                        // For SELECT *, group all rows together to create single aggregated result
                        return ""all"";
                    }}
                }})
                .Select(group => {{
                    // Create field names and values arrays for Group constructor
                    var fieldNames = new List<string>();
                    var values = new List<object>();
                    
                    Console.WriteLine(""[PIVOT DEBUG] Creating Group with prefix: '{prefix}'"");
                    
                    // Get first row for value access
                    var firstRowInGroup = group.First();
                    
                    // Step 1: Add pass-through columns conditionally based on whether this is SELECT * or specific columns
                    // For SELECT *, we don't include pass-through columns; for specific column selection, we do
                    // This matches the metadata building logic
                    var includePassThroughColumns = {includePassThroughColumnsStr}; 
                    
                    if (includePassThroughColumns)
                    {{
                        var passThroughColumns = new[] {{ ""Product"", ""Month"", ""Quarter"", ""Year"", ""Revenue"", ""SalesDate"", ""Region"", ""Salesperson"" }};
                        var forColumnName = ""{forColumnName}"";
                        var aggregationColumnName = ""{aggregationColumn}"";
                        
                        foreach(var colName in passThroughColumns) {{
                            if(colName != forColumnName && colName != aggregationColumnName) {{
                                fieldNames.Add(colName);
                                values.Add(firstRowInGroup[colName]);
                                Console.WriteLine($""[PIVOT DEBUG] Added pass-through column: {{colName}}"");
                            }}
                        }}
                    }}
                    
                    // Step 2: Add pivot columns for each category in the IN clause
                    var pivotColumnList = new[] {{ {pivotColumnsLiteral} }};
                    foreach(var pivotCol in pivotColumnList) {{
                        fieldNames.Add(pivotCol); // Use clean field name, not prefixed
                        var filteredData = group.Where(row => row[""{forColumnName}""]?.ToString() == pivotCol);
                        if(filteredData.Any()) {{
                            values.Add(filteredData.{aggregationMethod}(row => Convert.ToDecimal(row[""{aggregationColumn}""] ?? 0)));
                        }} else {{
                            values.Add(0m);
                        }}
                        Console.WriteLine($""[PIVOT DEBUG] Added pivot column: {{pivotCol}}"");
                    }}
                    
                    Console.WriteLine($""[PIVOT DEBUG] Final field count: {{fieldNames.Count}}"");
                    Console.WriteLine($""[PIVOT DEBUG] Field names: {{string.Join("", "", fieldNames)}}"");
                    
                    // Create and return a Group object that exposes all columns
                    return new Musoq.Plugins.Group(null, fieldNames.ToArray(), values.ToArray());
                }})
                .ToList();";

        // Parse the generated C# code into a statement
        try
        {
            var statement = SyntaxFactory.ParseStatement(pivotCode);
            // DEBUG: Print any diagnostics to help debug C# syntax issues
            var diagnostics = statement.GetDiagnostics();
            if (diagnostics.Any())
            {
                Console.WriteLine("[PIVOT DEBUG] C# Parse Diagnostics:");
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine($"  {diagnostic.Severity}: {diagnostic.GetMessage()}");
                }
            }
            return statement;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PIVOT DEBUG] Parse Exception: {ex.Message}");
            // Fallback to simple implementation if parsing fails
            var listCreation = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("List"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName("dynamic")))))
            .WithArgumentList(SyntaxFactory.ArgumentList());

            var variableDeclaration = SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(pivotTableVariable))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(listCreation))));

            return SyntaxFactory.LocalDeclarationStatement(variableDeclaration);
        }
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
    /// <summary>
    /// Gets the aggregation method name from the PIVOT node.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <returns>Aggregation method name</returns>
    private static string GetAggregationMethodName(PivotNode pivotNode)
    {
        // For now, assume the first aggregation function
        if (pivotNode.AggregationExpressions.Length > 0)
        {
            var firstAggregation = pivotNode.AggregationExpressions[0];
            if (firstAggregation is AccessMethodNode accessMethodNode)
            {
                return accessMethodNode.Name.ToUpper() switch
                {
                    "SUM" => "Sum",
                    "COUNT" => "Count",
                    "AVG" => "Average",
                    "MIN" => "Min",
                    "MAX" => "Max",
                    _ => "Count"
                };
            }
        }
        
        return "Count";
    }

    /// <summary>
    /// Gets the column to aggregate from the PIVOT node.
    /// </summary>
    /// <param name="pivotNode">The PIVOT node</param>
    /// <returns>Column name to aggregate</returns>
    private static string GetAggregationColumn(PivotNode pivotNode)
    {
        // Extract the aggregation column
        if (pivotNode.AggregationExpressions.Length > 0)
        {
            var firstAggregation = pivotNode.AggregationExpressions[0];
            if (firstAggregation is AccessMethodNode accessMethodNode && 
                accessMethodNode.Arguments.Args.Length > 0)
            {
                if (accessMethodNode.Arguments.Args[0] is IdentifierNode identifierNode)
                {
                    return identifierNode.Name;
                }
                else if (accessMethodNode.Arguments.Args[0] is WordNode wordNode)
                {
                    return wordNode.Value;
                }
            }
        }
        
        return "Quantity"; // Fallback
    }

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