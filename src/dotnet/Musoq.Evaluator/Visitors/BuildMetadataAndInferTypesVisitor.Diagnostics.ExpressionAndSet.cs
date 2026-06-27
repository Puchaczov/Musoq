using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    /// <summary>
    ///     Reports an invalid expression type error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="field">The field with invalid type.</param>
    /// <param name="invalidType">The invalid type.</param>
    /// <param name="context">The context where the error occurred.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportInvalidExpressionType(FieldNode field, Type? invalidType, string context, Node? node)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3027_InvalidExpressionType,
                $"Query output column '{field.FieldName}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. Only primitive types are allowed in query outputs.",
                node);
            return true;
        }

        throw new InvalidQueryExpressionTypeException(field, invalidType, context);
    }

    /// <summary>
    ///     Reports an invalid expression type error for expressions. If diagnostics are enabled, records the error and returns
    ///     true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="expressionDescription">Description of the expression.</param>
    /// <param name="invalidType">The invalid type.</param>
    /// <param name="context">The context where the error occurred.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportInvalidExpressionType(string expressionDescription, Type? invalidType, string context,
        Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3027_InvalidExpressionType,
                $"Expression '{expressionDescription}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. Only primitive types are allowed in query expressions.",
                node);
            return true;
        }

        throw new InvalidQueryExpressionTypeException(expressionDescription, invalidType, context);
    }

    /// <summary>
    ///     Reports a column must be marked as bindable property error. If diagnostics are enabled, records the error and
    ///     returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportColumnNotBindable(string columnName, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3026_ColumnNotBindable,
                $"Column '{columnName}' must be marked with BindablePropertyAsTable attribute to be used in this context.",
                node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new ColumnMustBeMarkedAsBindablePropertyAsTableException(columnName, span);
    }

    /// <summary>
    ///     Reports an unknown property error with suggestions. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="identifier">The unknown identifier.</param>
    /// <param name="properties">Available properties for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownPropertyWithSuggestions(string identifier, PropertyInfo[] properties, Node? node)
    {
        if (DiagnosticContext != null)
        {
            var library = new TransitionLibrary();
            var candidatesProperties = properties.Where(prop =>
                library.Soundex(prop.Name) == library.Soundex(identifier) ||
                library.LevenshteinDistance(prop.Name, identifier) < 3).ToArray();

            var message = candidatesProperties.Length > 0
                ? $"Unknown property '{identifier}'. Did you mean to use [{string.Join(", ", candidatesProperties.Select(p => p.Name))}]?"
                : $"Unknown property '{identifier}'.";

            DiagnosticContext.ReportError(DiagnosticCode.MQ3028_UnknownProperty, message, node);
            return true;
        }


        return false;
    }

    /// <summary>
    ///     Reports a construction not yet supported error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="description">Description of the unsupported construction.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportConstructionNotSupported(string description, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3030_ConstructionNotSupported,
                description,
                node);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Reports the legacy set-operator missing-key diagnostic. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="setOperator">The name of the set operator (UNION, EXCEPT, INTERSECT).</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportSetOperatorMissingKeys(string setOperator, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3031_SetOperatorMissingKeys,
                SetOperatorMustHaveKeyColumnsException.CreateMessage(setOperator),
                node);
            return true;
        }

        return false;
    }

}
