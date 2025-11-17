using System;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins;

namespace Musoq.Evaluator.Helpers;

/// <summary>
/// Factory class responsible for creating type conversion nodes for automatic type inference.
/// Handles DateTime conversions, numeric conversions, and runtime operator wrapping.
/// Follows Single Responsibility Principle - separates node creation logic from visitor traversal.
/// </summary>
public class TypeConversionNodeFactory
{
    private readonly ILibraryMethodResolver _methodResolver;

    /// <summary>
    /// Initializes a new instance of the TypeConversionNodeFactory.
    /// </summary>
    /// <param name="methodResolver">Method resolver for looking up LibraryBase methods.</param>
    internal TypeConversionNodeFactory(ILibraryMethodResolver methodResolver)
    {
        _methodResolver = methodResolver ?? throw new ArgumentNullException(nameof(methodResolver));
    }

    /// <summary>
    /// Creates an AccessMethodNode for DateTime/DateTimeOffset/TimeSpan conversion from string.
    /// </summary>
    /// <param name="targetType">The target DateTime-related type.</param>
    /// <param name="stringValue">The string value to convert.</param>
    /// <returns>AccessMethodNode wrapping the conversion method call.</returns>
    /// <exception cref="InvalidOperationException">Thrown if target type is not supported.</exception>
    public AccessMethodNode CreateDateTimeConversionNode(Type targetType, string stringValue)
    {
        string methodName;
        
        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            methodName = nameof(LibraryBase.ToDateTime);
        }
        else if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
        {
            methodName = nameof(LibraryBase.ToDateTimeOffset);
        }
        else if (targetType == typeof(TimeSpan) || targetType == typeof(TimeSpan?))
        {
            methodName = nameof(LibraryBase.ToTimeSpan);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported datetime type: {targetType}");
        }

        var functionToken = new FunctionToken(methodName, new TextSpan(0, methodName.Length));
        var stringLiteralNode = new WordNode(stringValue);
        var args = new ArgsListNode([stringLiteralNode]);
        var method = _methodResolver.ResolveMethod(methodName, [typeof(string)]);
        
        return new AccessMethodNode(
            functionToken, 
            args, 
            ArgsListNode.Empty, 
            false,
            method);
    }

    /// <summary>
    /// Creates an AccessMethodNode that wraps a binary operation in a runtime operator method call.
    /// This enables dynamic type handling for object columns by delegating type conversion to runtime.
    /// </summary>
    /// <param name="methodName">Name of the runtime operator method in LibraryBase (e.g., "InternalApplyMultiplyOperator").</param>
    /// <param name="left">Left operand node.</param>
    /// <param name="right">Right operand node.</param>
    /// <returns>AccessMethodNode representing the runtime operator method call.</returns>
    public Node CreateRuntimeOperatorCall(string methodName, Node left, Node right)
    {
        var functionToken = new FunctionToken(methodName, new TextSpan(0, methodName.Length));
        var args = new ArgsListNode([left, right]);
        var method = _methodResolver.ResolveMethod(methodName, [typeof(object), typeof(object)]);
        
        return new AccessMethodNode(
            functionToken, 
            args, 
            ArgsListNode.Empty, 
            false,
            method);
    }

    /// <summary>
    /// Creates an AccessMethodNode for numeric type conversion.
    /// Selects appropriate conversion method based on target type and conversion mode.
    /// </summary>
    /// <param name="sourceNode">The node to convert.</param>
    /// <param name="targetType">The target numeric type (int, long, or decimal).</param>
    /// <param name="isObjectType">True if source is System.Object type.</param>
    /// <param name="isRelationalComparison">True for comparison operators (>, <, >=, <=).</param>
    /// <param name="isArithmeticOperation">True for arithmetic operators (+, -, *, /, %).</param>
    /// <returns>AccessMethodNode wrapping the conversion method call.</returns>
    public AccessMethodNode CreateNumericConversionNode(Node sourceNode, Type targetType, bool isObjectType, bool isRelationalComparison, bool isArithmeticOperation)
    {
        string methodName;

        var useNumericOnlyMode = isObjectType && isArithmeticOperation;
        var useComparisonMode = isObjectType && isRelationalComparison;

        if (useNumericOnlyMode)
        {
            methodName = nameof(LibraryBase.TryConvertNumericOnly);
        }
        else if (targetType == typeof(decimal))
        {
            methodName = useComparisonMode ? nameof(LibraryBase.TryConvertToDecimalComparison) : nameof(LibraryBase.TryConvertToDecimalStrict);
        }
        else if (targetType == typeof(long) || targetType == typeof(ulong))
        {
            methodName = useComparisonMode ? nameof(LibraryBase.TryConvertToInt64Comparison) : nameof(LibraryBase.TryConvertToInt64Strict);
        }
        else
        {
            methodName = useComparisonMode ? nameof(LibraryBase.TryConvertToInt32Comparison) : nameof(LibraryBase.TryConvertToInt32Strict);
        }

        Type[] parameterTypes = [sourceNode.ReturnType];

        var functionToken = new FunctionToken(methodName, new TextSpan(0, methodName.Length));
        var args = new ArgsListNode([sourceNode]);
        var method = _methodResolver.ResolveMethod(methodName, parameterTypes);
        
        return new AccessMethodNode(
            functionToken, 
            args, 
            ArgsListNode.Empty, 
            false,
            method);
    }

    /// <summary>
    /// Determines which runtime operator method to use based on the binary operator node type.
    /// Uses a factory pattern with dummy nodes to identify the operator type.
    /// </summary>
    /// <typeparam name="T">Type of binary operator node.</typeparam>
    /// <param name="nodeFactory">Factory function that creates the operator node.</param>
    /// <returns>Name of the runtime operator method (e.g., "InternalApplyMultiplyOperator"), or null if not supported.</returns>
    public string GetRuntimeOperatorMethodName<T>(Func<Node, Node, T> nodeFactory) where T : Node
    {
        var dummyLeft = new IntegerNode("0", "s");
        var dummyRight = new IntegerNode("0", "s");
        var resultNode = nodeFactory(dummyLeft, dummyRight);
        
        return resultNode switch
        {
            StarNode => nameof(LibraryBase.InternalApplyMultiplyOperator),
            FSlashNode => nameof(LibraryBase.InternalApplyDivideOperator),
            ModuloNode => nameof(LibraryBase.InternalApplyModuloOperator),
            AddNode => nameof(LibraryBase.InternalApplyAddOperator),
            HyphenNode => nameof(LibraryBase.InternalApplySubtractOperator),
            GreaterNode => nameof(LibraryBase.InternalGreaterThanOperator),
            GreaterOrEqualNode => nameof(LibraryBase.InternalGreaterThanOrEqualOperator),
            LessNode => nameof(LibraryBase.InternalLessThanOperator),
            LessOrEqualNode => nameof(LibraryBase.InternalLessThanOrEqualOperator),
            EqualityNode => nameof(LibraryBase.InternalEqualOperator),
            DiffNode => nameof(LibraryBase.InternalNotEqualOperator),
            _ => null
        };
    }

    /// <summary>
    /// Checks if the given type is a DateTime-related type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if type is DateTime, DateTimeOffset, TimeSpan, or their nullable versions.</returns>
    public static bool IsDateTimeType(Type type)
    {
        return type == typeof(DateTime) || type == typeof(DateTime?) ||
               type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?) ||
               type == typeof(TimeSpan) || type == typeof(TimeSpan?);
    }

    /// <summary>
    /// Checks if the given type is System.Object, indicating a dynamically-typed column.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type is System.Object, false otherwise.</returns>
    public static bool IsObjectType(Type type)
    {
        return type == typeof(object);
    }

    /// <summary>
    /// Checks if the given type is string or System.Object.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if type is string or object.</returns>
    public static bool IsStringOrObjectType(Type type)
    {
        return type == typeof(string) || type == typeof(object);
    }

    /// <summary>
    /// Checks if the node represents a numeric literal.
    /// </summary>
    /// <param name="node">Node to check.</param>
    /// <param name="numericType">Output parameter for the numeric type if successful.</param>
    /// <returns>True if node is a numeric literal, false otherwise.</returns>
    public static bool IsNumericLiteralNode(Node node, out Type numericType)
    {
        switch (node)
        {
            case IntegerNode intNode:
                numericType = intNode.ReturnType;
                return true;
            case DecimalNode:
                numericType = typeof(decimal);
                return true;
            case HexIntegerNode hexNode:
                numericType = hexNode.ReturnType;
                return true;
            case BinaryIntegerNode binNode:
                numericType = binNode.ReturnType;
                return true;
            case OctalIntegerNode octNode:
                numericType = octNode.ReturnType;
                return true;
            default:
                numericType = null;
                return false;
        }
    }
}
