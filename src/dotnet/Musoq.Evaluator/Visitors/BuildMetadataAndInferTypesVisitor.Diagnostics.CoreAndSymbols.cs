using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryReportTypeMismatch(string message, Node node)
    {
        return _diagnosticReporter.TryReportTypeMismatch(message, node);
    }

    private void ThrowOrReportInvalidOperandTypes(Type leftType, Type rightType, Node errorContextNode,
        string? message = null)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3007_InvalidOperandTypes,
                message ?? $"Invalid operand types for operator: '{leftType.Name}' and '{rightType.Name}'.",
                errorContextNode);
            return;
        }

        throw new InvalidOperandTypesException(leftType, rightType);
    }


    /// <summary>
    ///     Reports an unknown column error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="columnName">The column name that was not found.</param>
    /// <param name="availableColumns">Available column names for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownColumn(string columnName, IEnumerable<string> availableColumns, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownColumn(columnName, availableColumns, node);
            return true;
        }


        var span = node.SpanOrEmpty();
        var availableList = availableColumns.ToArray();
        if (availableList.Length > 0)
        {
            var library = new TransitionLibrary();
            var candidates = availableList
                .Where(col => library.Soundex(col) == library.Soundex(columnName) ||
                              library.LevenshteinDistance(col, columnName) < 3)
                .ToArray();

            if (candidates.Length > 0)
                throw new UnknownColumnOrAliasException(
                    columnName,
                    $"Did you mean to use [{string.Join(", ", candidates)}]?",
                    span,
                    candidates);
        }

        throw new UnknownColumnOrAliasException(columnName, string.Empty, span);
    }

    /// <summary>
    ///     Reports an unknown property error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="propertyName">The property name that was not found.</param>
    /// <param name="objectType">The type of object on which the property was not found.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportUnknownProperty(
        string propertyName,
        Type? objectType,
        Node? node,
        string? accessContext = null)
    {
        if (DiagnosticContext != null)
        {
            if (node is { HasSpan: true } &&
                DiagnosticContext.HasNearbyError(DiagnosticCode.MQ3028_UnknownProperty, node.Span))
                return true;

            var availableProperties = objectType?.GetProperties().Select(static property => property.Name) ?? [];
            DiagnosticContext.ReportUnknownProperty(
                propertyName,
                availableProperties,
                node,
                objectType?.Name,
                accessContext);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new UnknownPropertyException(
            propertyName,
            objectType?.Name ?? "unknown",
            span,
            objectType?.GetProperties().Select(static property => property.Name) ?? []);
    }

    /// <summary>
    ///     Reports a type-related error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="typeName">The type name that was not found or is invalid.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportTypeNotFound(string typeName, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportTypeNotFound(typeName, node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new TypeNotFoundException(typeName, string.Empty, span);
    }

    protected bool TryReportTypeNotFound(string typeName, TextSpan span)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportTypeNotFound(typeName, span);
            return true;
        }

        throw new TypeNotFoundException(typeName, string.Empty, span);
    }

    /// <summary>
    ///     Reports an ambiguous column reference. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="columnName">The ambiguous column name.</param>
    /// <param name="alias1">First possible source alias.</param>
    /// <param name="alias2">Second possible source alias.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportAmbiguousColumn(string columnName, string alias1, string alias2, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportAmbiguousColumn(columnName, alias1, alias2, node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new AmbiguousColumnException(columnName, alias1, alias2, span);
    }

    /// <summary>
    ///     Reports a general semantic error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <typeparam name="TException">The type of exception to throw if not collecting diagnostics.</typeparam>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), never returns false (throws instead).</returns>
    protected bool TryReportSemanticError<TException>(DiagnosticCode code, string message, Node? node)
        where TException : Exception
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(code, message, node);
            return true;
        }

        throw (TException)Activator.CreateInstance(typeof(TException), message)!;
    }

    /// <summary>
    ///     Reports a semantic error using an existing exception. If diagnostics are enabled, records the error and returns
    ///     true.
    ///     Otherwise rethrows the exception.
    /// </summary>
    /// <param name="exception">The exception to report or throw.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), never returns false (throws instead).</returns>
    protected bool TryReportException(Exception exception, Node? node)
    {
        return _diagnosticReporter.TryReportException(exception, node) ? true : throw exception;
    }
}
