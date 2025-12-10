#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
/// Manages Common Subexpression Elimination (CSE) for query optimization.
/// Tracks expressions that appear multiple times and generates cached variable declarations.
/// </summary>
public sealed class CseManager
{
    private readonly Dictionary<string, int> _slotMap = new();
    private readonly HashSet<string> _computedInCurrentRow = [];
    private readonly List<(string Name, Type Type, string ExpressionId)> _declarations = [];
    private readonly Dictionary<string, string> _expressionToVariable = new();
    private readonly HashSet<string> _nonDeterministicFunctions;
    private int _variableCounter;
    private bool _isEnabled;

    /// <summary>
    /// Creates a new CseManager with the given set of non-deterministic function names.
    /// </summary>
    /// <param name="nonDeterministicFunctions">Functions that should not be cached (e.g., random, newid).</param>
    public CseManager(HashSet<string> nonDeterministicFunctions)
    {
        _nonDeterministicFunctions = nonDeterministicFunctions ?? throw new ArgumentNullException(nameof(nonDeterministicFunctions));
    }

    /// <summary>
    /// Initializes CSE analysis for a query node.
    /// </summary>
    /// <param name="queryNode">The query node to analyze.</param>
    /// <param name="enabled">Whether CSE optimization is enabled.</param>
    public void Initialize(Node queryNode, bool enabled)
    {
        Reset();
        _isEnabled = enabled;

        if (!enabled)
            return;

        var analyzer = new CommonSubexpressionAnalysisVisitor(_nonDeterministicFunctions);
        var traverser = new CommonSubexpressionAnalysisTraverseVisitor(analyzer);
        queryNode.Accept(traverser);

        foreach (var kvp in analyzer.GetCacheSlotMap())
        {
            _slotMap[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Applies CSE optimization to an expression if applicable.
    /// Returns the original expression if CSE is not applicable.
    /// </summary>
    /// <param name="expressionId">Unique identifier for the expression.</param>
    /// <param name="expression">The expression syntax to potentially cache.</param>
    /// <param name="expressionType">The type of the expression.</param>
    /// <param name="isInsideCaseWhen">Whether we're inside a CASE WHEN expression.</param>
    /// <returns>Either the original expression, a variable reference, or an assignment expression.</returns>
    public ExpressionSyntax ApplyIfNeeded(
        string expressionId,
        ExpressionSyntax expression,
        Type expressionType,
        bool isInsideCaseWhen)
    {
        if (isInsideCaseWhen && _expressionToVariable.TryGetValue(expressionId, out var cseParamName))
        {
            return SyntaxFactory.IdentifierName(cseParamName);
        }

        if (!_isEnabled || !_slotMap.TryGetValue(expressionId, out _))
        {
            return expression;
        }

        if (!_expressionToVariable.TryGetValue(expressionId, out var variableName))
        {
            variableName = $"_cse{_variableCounter++}";
            _expressionToVariable[expressionId] = variableName;
            _declarations.Add((variableName, expressionType, expressionId));
        }

        var variableIdentifier = SyntaxFactory.IdentifierName(variableName);

        if (!_computedInCurrentRow.Add(expressionId))
        {
            return variableIdentifier;
        }

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                variableIdentifier,
                expression));
    }

    /// <summary>
    /// Generates variable declaration statements for all cached expressions.
    /// These should be placed at the beginning of the method/loop.
    /// </summary>
    public IEnumerable<StatementSyntax> GenerateDeclarations()
    {
        foreach (var (variableName, variableType, _) in _declarations)
        {
            var typeSyntax = SyntaxFactory.ParseTypeName(GetTypeName(variableType));

            yield return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(typeSyntax)
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(variableName)
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.DefaultExpression(typeSyntax))))));
        }
    }

    /// <summary>
    /// Gets the CSE variable declarations as a list.
    /// </summary>
    public List<(string VariableName, Type VariableType, string ExpressionId)> GetDeclarations()
    {
        return [.._declarations];
    }

    /// <summary>
    /// Converts a Type to a string representation suitable for code generation.
    /// </summary>
    private static string GetTypeName(Type type)
    {
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(short)) return "short";
        if (type == typeof(char)) return "char";
        if (type == typeof(object)) return "object";

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GetTypeName(underlyingType) + "?";
        }

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var genericTypeName = type.GetGenericTypeDefinition().FullName!;
        genericTypeName = genericTypeName[..genericTypeName.IndexOf('`')];
        var genericArgs = string.Join(", ", System.Linq.Enumerable.Select(type.GetGenericArguments(), GetTypeName));
        return $"{genericTypeName}<{genericArgs}>";
    }

    private void Reset()
    {
        _slotMap.Clear();
        _computedInCurrentRow.Clear();
        _declarations.Clear();
        _expressionToVariable.Clear();
        _variableCounter = 0;
        _isEnabled = false;
    }
}
