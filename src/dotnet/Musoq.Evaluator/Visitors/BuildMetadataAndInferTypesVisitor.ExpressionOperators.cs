using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void VisitBinaryOperatorWithSafePop<T>(Func<Node, Node, T> nodeFactory, string operationName) where T : Node
    {
        var nodes = SafePopMultiple(Nodes, 2, operationName);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(nodeFactory(left, right));
    }

    private void VisitBinaryOperatorWithTypeConversion<T>(Func<Node, Node, T> nodeFactory, Node errorContextNode,
        BinaryOperatorKind operatorKind, BinaryOperationContext operationContext = BinaryOperationContext.Standard) where T : Node
    {
        var right = SafePop(Nodes, "VisitBinaryOperatorWithTypeConversion (right)");
        var left = SafePop(Nodes, "VisitBinaryOperatorWithTypeConversion (left)");


        if (operationContext == BinaryOperationContext.ArithmeticOperation)
        {
            var leftIsNull = left.ReturnType is NullNode.NullType;
            var rightIsNull = right.ReturnType is NullNode.NullType;

            if (leftIsNull && rightIsNull)
            {
                Nodes.Push(new NullNode(typeof(object)));
                return;
            }

            if (leftIsNull || rightIsNull)
            {
                var nonNullType = leftIsNull ? right.ReturnType : left.ReturnType;
                var baseType = NormalizeOperandType(nonNullType);

                var newLeft = leftIsNull ? new NullNode(baseType, left.Span) : left;
                var newRight = rightIsNull ? new NullNode(baseType, right.Span) : right;

                var nullBranchResult = nodeFactory(newLeft, newRight);
                if (errorContextNode.HasSpan)
                    nullBranchResult.WithSpan(errorContextNode.Span);
                Nodes.Push(nullBranchResult);
                return;
            }
        }

        var leftIsObject = TypeConversionNodeFactory.IsObjectType(left.ReturnType);
        var rightIsObject = TypeConversionNodeFactory.IsObjectType(right.ReturnType);

        if (leftIsObject || rightIsObject)
        {
            var operatorMethodName = _nodeFactory.GetRuntimeOperatorMethodName(nodeFactory);
            if (operatorMethodName != null)
            {
                var wrappedNode = _nodeFactory.CreateRuntimeOperatorCall(operatorMethodName, left, right);
                Nodes.Push(wrappedNode);
                return;
            }
        }

        var transformedLeft = TransformStringToDateTimeIfNeeded(left, right);
        var transformedRight = TransformStringToDateTimeIfNeeded(right, left);

        transformedLeft = TransformToNumericTypeIfNeeded(transformedLeft, transformedRight, operationContext);
        transformedRight = TransformToNumericTypeIfNeeded(transformedRight, transformedLeft, operationContext);

        ValidateBinaryOperatorOperands(transformedLeft, transformedRight, operatorKind, errorContextNode);

        var result = nodeFactory(transformedLeft, transformedRight);
        if (errorContextNode.HasSpan)
            result.WithSpan(errorContextNode.Span);
        Nodes.Push(result);
    }

    private Node TransformStringToDateTimeIfNeeded(Node candidateNode, Node otherNode)
    {
        var otherNodeType = otherNode.ReturnType;
        if (candidateNode is not WordNode stringNode ||
            otherNodeType == null ||
            !TypeConversionNodeFactory.IsDateTimeType(otherNodeType))
            return candidateNode;

        return _nodeFactory.CreateDateTimeConversionNode(otherNodeType, stringNode.Value);
    }

    private Node TransformToNumericTypeIfNeeded(Node candidateNode, Node otherNode,
        BinaryOperationContext operationContext)
    {
        var shouldTransform = operationContext switch
        {
            BinaryOperationContext.ArithmeticOperation => TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType),
            BinaryOperationContext.RelationalComparison => TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType),
            _ => TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType)
        };

        if (!shouldTransform || !TypeConversionNodeFactory.IsNumericLiteralNode(otherNode, out var targetType))
            return candidateNode;

        if (ContainsScriptParameterReference(candidateNode))
            return candidateNode;

        return _nodeFactory.CreateNumericConversionNode(candidateNode, targetType,
            TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType), operationContext);
    }

    private void ValidateBooleanOperand(Node operand, string operatorName, Node errorContextNode)
    {
        var operandType = NormalizeOperandType(operand.ReturnType);
        if (CanSkipStaticTypeValidation(operandType) || operandType == typeof(bool))
            return;

        ThrowOrReportInvalidOperandTypes(typeof(bool), operandType, errorContextNode,
            $"Operator {operatorName} requires boolean operands, but got '{operandType.Name}'.");
    }

    private void ValidatePatternOperand(Node operand, string operatorName, Node errorContextNode)
    {
        var operandType = NormalizeOperandType(operand.ReturnType);
        if (CanSkipStaticTypeValidation(operandType) || operandType == typeof(string))
            return;

        var message =
            $"Operator {operatorName} requires string operands, but got '{operandType.Name}'.";

        if (TryReportTypeMismatch(message, errorContextNode))
            return;

        throw new TypeMismatchException(typeof(string), operandType,
            errorContextNode.SpanOrEmpty());
    }

    private void ValidateCollectionPredicateItems(Node left, ArgsListNode args, Node errorContextNode)
    {
        foreach (var item in args.Args)
            ValidateBinaryOperatorOperands(left, item, BinaryOperatorKind.Equality, errorContextNode);
    }

    private void ValidateBinaryOperatorOperands(Node left, Node right, BinaryOperatorKind operatorKind, Node errorContextNode)
    {
        var leftType = NormalizeOperandType(left.ReturnType);
        var rightType = NormalizeOperandType(right.ReturnType);

        if (CanSkipStaticTypeValidation(leftType) || CanSkipStaticTypeValidation(rightType))
            return;

        var isValid = operatorKind switch
        {
            BinaryOperatorKind.Add => CanApplyAddition(leftType, rightType),
            BinaryOperatorKind.Subtract => CanApplySubtraction(leftType, rightType),
            BinaryOperatorKind.Multiply => CanApplyNumericOperator(leftType, rightType),
            BinaryOperatorKind.Divide => CanApplyNumericOperator(leftType, rightType),
            BinaryOperatorKind.Modulo => CanApplyNumericOperator(leftType, rightType),
            BinaryOperatorKind.BitwiseAnd => CanApplyBitwiseOperator(leftType, rightType),
            BinaryOperatorKind.BitwiseOr => CanApplyBitwiseOperator(leftType, rightType),
            BinaryOperatorKind.BitwiseXor => CanApplyBitwiseOperator(leftType, rightType),
            BinaryOperatorKind.LeftShift => CanApplyShiftOperator(leftType, rightType),
            BinaryOperatorKind.RightShift => CanApplyShiftOperator(leftType, rightType),
            BinaryOperatorKind.Equality => CanApplyEqualityOperator(leftType, rightType),
            BinaryOperatorKind.Inequality => CanApplyEqualityOperator(leftType, rightType),
            BinaryOperatorKind.Relational => CanApplyRelationalOperator(leftType, rightType),
            _ => true
        };

        if (isValid)
            return;

        if (operatorKind is BinaryOperatorKind.Equality or BinaryOperatorKind.Inequality or BinaryOperatorKind.Relational)
        {
            var message = CreateComparisonTypeMismatchMessage(left, right, leftType, rightType);
            if (TryReportTypeMismatch(message, errorContextNode))
                return;

            throw new TypeMismatchException(leftType, rightType,
                errorContextNode.SpanOrEmpty());
        }

        ThrowOrReportInvalidOperandTypes(leftType, rightType, errorContextNode);
    }

    private static string CreateComparisonTypeMismatchMessage(Node left, Node right, Type leftType, Type rightType)
    {
        var leftHasParameter = TryFindFirstScriptParameterReference(left, out var leftParameter);
        var rightHasParameter = TryFindFirstScriptParameterReference(right, out var rightParameter);

        if (!leftHasParameter && !rightHasParameter)
        {
            return $"Type mismatch: cannot compare '{leftType.Name}' with '{rightType.Name}'.";
        }

        return $"Type mismatch: cannot compare {DescribeComparedExpression(leftParameter, leftType)} with " +
               $"{DescribeComparedExpression(rightParameter, rightType)}. Script parameters use their declared types; " +
               "use an explicit conversion if needed.";
    }

    private static string DescribeComparedExpression(ParameterReferenceNode? parameter, Type expressionType)
    {
        return parameter != null
            ? $"script parameter '${parameter.Name}' of type '{FormatTypeName(parameter.ReturnType ?? expressionType)}'"
            : $"expression of type '{FormatTypeName(expressionType)}'";
    }

    private static string FormatTypeName(Type? type)
    {
        if (type == null)
            return "unknown";

        if (type is NullNode.NullType)
            return "null";

        var nullableType = Nullable.GetUnderlyingType(type);
        return nullableType == null
            ? type.Name
            : $"{nullableType.Name}?";
    }

    private static bool ContainsScriptParameterReference(Node node)
    {
        return TryFindFirstScriptParameterReference(node, out _);
    }

    private static bool TryFindFirstScriptParameterReference(
        Node? node,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ParameterReferenceNode? parameter)
    {
        switch (node)
        {
            case null:
                parameter = null;
                return false;
            case ParameterReferenceNode parameterReference:
                parameter = parameterReference;
                return true;
            case BinaryNode binaryNode:
                return TryFindFirstScriptParameterReference(binaryNode.Left, out parameter) ||
                       TryFindFirstScriptParameterReference(binaryNode.Right, out parameter);
            case UnaryNode unaryNode:
                return TryFindFirstScriptParameterReference(unaryNode.Expression, out parameter);
            case ArgsListNode argsListNode:
                foreach (var arg in argsListNode.Args)
                {
                    if (TryFindFirstScriptParameterReference(arg, out parameter))
                        return true;
                }

                parameter = null;
                return false;
            case AccessMethodNode accessMethodNode:
                if (TryFindFirstScriptParameterReference(accessMethodNode.Arguments, out parameter))
                    return true;
                return TryFindFirstScriptParameterReference(accessMethodNode.ExtraAggregateArguments, out parameter);
            case BetweenNode betweenNode:
                return TryFindFirstScriptParameterReference(betweenNode.Expression, out parameter) ||
                       TryFindFirstScriptParameterReference(betweenNode.Min, out parameter) ||
                       TryFindFirstScriptParameterReference(betweenNode.Max, out parameter);
            case CaseNode caseNode:
                foreach (var (when, then) in caseNode.WhenThenPairs)
                {
                    if (TryFindFirstScriptParameterReference(when, out parameter) ||
                        TryFindFirstScriptParameterReference(then, out parameter))
                    {
                        return true;
                    }
                }

                return TryFindFirstScriptParameterReference(caseNode.Else, out parameter);
            case FieldNode fieldNode:
                return TryFindFirstScriptParameterReference(fieldNode.Expression, out parameter);
            case IsNullNode isNullNode:
                return TryFindFirstScriptParameterReference(isNullNode.Expression, out parameter);
            default:
                parameter = null;
                return false;
        }
    }
}
