using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    /// <summary>
    ///     Reports an object-not-array error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportObjectNotArray(string message, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(DiagnosticCode.MQ3017_ObjectNotArray, message, node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new ObjectIsNotAnArrayException(message, span);
    }

    /// <summary>
    ///     Reports an no-indexer error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportNoIndexer(string message, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(DiagnosticCode.MQ3018_NoIndexer, message, node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new ObjectDoesNotImplementIndexerException(message, span);
    }

    /// <summary>
    ///     Reports a set operator column count error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportSetOperatorColumnCount(Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3019_SetOperatorColumnCount,
                "Set operator must have the same quantity of columns in both queries",
                node);
            return true;
        }

        throw new SetOperatorMustHaveSameQuantityOfColumnsException();
    }

    /// <summary>
    ///     Reports a set operator column type error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="left">The left field node.</param>
    /// <param name="right">The right field node.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportSetOperatorColumnTypes(FieldNode left, FieldNode right, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3020_SetOperatorColumnTypes,
                $"Set operator must have the same types of columns in both queries. Left column expression is {left} and right column expression is {right}",
                node);
            return true;
        }

        throw new SetOperatorMustHaveSameTypesOfColumnsException(left, right);
    }

    /// <summary>
    ///     Reports a duplicate alias error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="aliasNode">The node that declared the duplicate alias.</param>
    /// <param name="alias">The duplicate alias.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportDuplicateAlias(Node? aliasNode, string alias, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3021_DuplicateAlias,
                $"Alias '{alias}' is already used in query. Please use a different alias.",
                node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new AliasAlreadyUsedException(alias, span);
    }

    /// <summary>
    ///     Reports a missing alias error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="methodNode">The access method node.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportMissingAlias(AccessMethodNode methodNode)
    {
        ArgumentNullException.ThrowIfNull(methodNode);
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3022_MissingAlias,
                AliasMissingException.CreateMethodCallMessage(methodNode.ToString()),
                methodNode);
            return true;
        }

        var span = methodNode.SpanOrEmpty();
        throw new AliasMissingException(AliasMissingException.CreateMethodCallMessage(methodNode.ToString()), span);
    }

    /// <summary>
    ///     Reports a table not defined error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="tableName">The undefined table name.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportTableNotDefined(string tableName, Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3023_TableNotDefined,
                $"Table '{tableName}' is not defined in query",
                node);
            return true;
        }

        var span = node.SpanOrEmpty();
        throw new TableIsNotDefinedException(tableName, span);
    }

    /// <summary>
    ///     Reports a column must be array error. If diagnostics are enabled, records the error and returns true.
    ///     Otherwise throws the exception.
    /// </summary>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (diagnostics mode), false if exception was thrown.</returns>
    protected bool TryReportColumnMustBeArray(Node? node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3025_ColumnMustBeArray,
                "Column must be an array or implement IEnumerable<T> interface",
                node);
            return true;
        }

        throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
    }
}
