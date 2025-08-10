using System;
using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Base class for visitors that provides defensive programming utilities and safe operations.
/// </summary>
public abstract class DefensiveVisitorBase
{
    /// <summary>
    /// Gets the name of this visitor for error reporting.
    /// </summary>
    protected abstract string VisitorName { get; }

    /// <summary>
    /// Safely pops a node from the stack with validation.
    /// </summary>
    /// <param name="nodes">The stack to pop from.</param>
    /// <param name="operation">The operation being performed (for error context).</param>
    /// <returns>The popped node.</returns>
    /// <exception cref="VisitorException">Thrown when the stack is empty.</exception>
    protected Node SafePop(Stack<Node> nodes, string operation)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (nodes.Count == 0)
            throw VisitorException.CreateForStackUnderflow(VisitorName, operation, 1, 0);

        return nodes.Pop();
    }

    /// <summary>
    /// Safely pops multiple nodes from the stack with validation.
    /// </summary>
    /// <param name="nodes">The stack to pop from.</param>
    /// <param name="count">The number of nodes to pop.</param>
    /// <param name="operation">The operation being performed (for error context).</param>
    /// <returns>An array of popped nodes in reverse order (first popped is last in array).</returns>
    /// <exception cref="VisitorException">Thrown when the stack doesn't have enough items.</exception>
    protected Node[] SafePopMultiple(Stack<Node> nodes, int count, string operation)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        if (nodes.Count < count)
            throw VisitorException.CreateForStackUnderflow(VisitorName, operation, count, nodes.Count);

        var result = new Node[count];
        for (int i = count - 1; i >= 0; i--)
        {
            result[i] = nodes.Pop();
        }
        return result;
    }

    /// <summary>
    /// Safely peeks at the top node without removing it.
    /// </summary>
    /// <param name="nodes">The stack to peek into.</param>
    /// <param name="operation">The operation being performed (for error context).</param>
    /// <returns>The top node.</returns>
    /// <exception cref="VisitorException">Thrown when the stack is empty.</exception>
    protected Node SafePeek(Stack<Node> nodes, string operation)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));

        if (nodes.Count == 0)
            throw VisitorException.CreateForStackUnderflow(VisitorName, operation, 1, 0);

        return nodes.Peek();
    }

    /// <summary>
    /// Safely casts a node to the expected type with validation.
    /// </summary>
    /// <typeparam name="T">The expected node type.</typeparam>
    /// <param name="node">The node to cast.</param>
    /// <param name="operation">The operation being performed (for error context).</param>
    /// <returns>The node cast to the expected type.</returns>
    /// <exception cref="VisitorException">Thrown when the node is null or not of the expected type.</exception>
    protected T SafeCast<T>(Node node, string operation) where T : Node
    {
        if (node == null)
            throw VisitorException.CreateForNullNode(VisitorName, operation, typeof(T).Name);

        if (node is not T castNode)
        {
            throw VisitorException.CreateForInvalidNodeType(
                VisitorName, 
                operation, 
                typeof(T).Name, 
                node.GetType().Name
            );
        }

        return castNode;
    }

    /// <summary>
    /// Validates constructor parameters for visitors.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterValue">The value to validate.</param>
    /// <param name="operation">The operation context (usually "constructor").</param>
    /// <exception cref="VisitorException">Thrown when the parameter is null.</exception>
    protected void ValidateConstructorParameter(string parameterName, object parameterValue, string operation = "constructor")
    {
        if (parameterValue == null)
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                operation,
                $"Parameter '{parameterName}' cannot be null.",
                "Ensure all required dependencies are properly initialized before creating the visitor."
            );
        }
    }

    /// <summary>
    /// Validates that a string parameter is not null or empty.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterValue">The value to validate.</param>
    /// <param name="operation">The operation context.</param>
    /// <exception cref="VisitorException">Thrown when the parameter is null or empty.</exception>
    protected void ValidateStringParameter(string parameterName, string parameterValue, string operation)
    {
        if (string.IsNullOrEmpty(parameterValue))
        {
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                operation,
                $"Parameter '{parameterName}' cannot be null or empty.",
                "Provide a valid non-empty string value."
            );
        }
    }

    /// <summary>
    /// Safely handles exceptions that occur during visitor operations.
    /// </summary>
    /// <param name="action">The action to execute safely.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <exception cref="VisitorException">Thrown when the action fails.</exception>
    protected void SafeExecute(Action action, string operation)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        try
        {
            action();
        }
        catch (VisitorException)
        {
            // Re-throw visitor exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new VisitorException(
                VisitorName,
                operation,
                "An unexpected error occurred during processing. See inner exception for details.",
                ex
            );
        }
    }

    /// <summary>
    /// Safely executes a function with exception handling.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute safely.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <returns>The result of the function.</returns>
    /// <exception cref="VisitorException">Thrown when the function fails.</exception>
    protected T SafeExecute<T>(Func<T> func, string operation)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        try
        {
            return func();
        }
        catch (VisitorException)
        {
            // Re-throw visitor exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new VisitorException(
                VisitorName,
                operation,
                "An unexpected error occurred during processing. See inner exception for details.",
                ex
            );
        }
    }
}