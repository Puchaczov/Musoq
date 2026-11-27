using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;
using static Musoq.Evaluator.Visitors.SemanticExpressionDiagnosticFacts;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void VisitBinaryOperatorWithSafePop<T>(Func<Node, Node, T> nodeFactory, string operationName)
        where T : Node
    {
        var nodes = PopSemanticNodes(2, operationName);
        var right = nodes[1];
        var left = nodes[0];
        PushSemanticNode(nodeFactory(left, right));
    }

    private void VisitBinaryOperatorWithTypeConversion<T>(
        Func<Node, Node, T> nodeFactory,
        Node errorContextNode,
        BinaryOperatorKind operatorKind,
        BinaryOperationContext operationContext = BinaryOperationContext.Standard
    )
        where T : Node
    {
        var right = PopSemanticNode("VisitBinaryOperatorWithTypeConversion (right)");
        var left = PopSemanticNode("VisitBinaryOperatorWithTypeConversion (left)");
        PushSemanticNode(
            BindBinaryOperator((left, right), nodeFactory, errorContextNode, operatorKind, operationContext)
        );
    }

    private Node BindBinaryOperator<T>(
        (Node Left, Node Right) operands,
        Func<Node, Node, T> nodeFactory,
        Node errorContextNode,
        BinaryOperatorKind operatorKind,
        BinaryOperationContext operationContext
    )
        where T : Node
    {
        var (left, right) = operands;
        if (
            TryBindEnumBinaryOperation(left, right, operatorKind, errorContextNode, out var enumLeft, out var enumRight)
        )
        {
            var enumResult = nodeFactory(enumLeft, enumRight);
            if (errorContextNode.HasSpan)
                enumResult.WithSpan(errorContextNode.Span);
            return enumResult;
        }

        if (
            operationContext == BinaryOperationContext.ArithmeticOperation
            && NullArithmeticBinding.Bind(left, right, nodeFactory, errorContextNode) is { } nullArithmetic
        )
            return nullArithmetic;

        var leftIsObject = TypeConversionNodeFactory.IsObjectType(left.ReturnType);
        var rightIsObject = TypeConversionNodeFactory.IsObjectType(right.ReturnType);

        if (leftIsObject || rightIsObject)
        {
            var operatorMethodName = _nodeFactory.GetRuntimeOperatorMethodName(nodeFactory);
            if (operatorMethodName != null)
                return _nodeFactory.CreateRuntimeOperatorCall(operatorMethodName, left, right);
        }

        var transformedLeft = TransformStringToDateTimeIfNeeded(left, right);
        var transformedRight = TransformStringToDateTimeIfNeeded(right, left);

        transformedLeft = TransformToNumericTypeIfNeeded(transformedLeft, transformedRight, operationContext);
        transformedRight = TransformToNumericTypeIfNeeded(transformedRight, transformedLeft, operationContext);

        var operandsPassedValidation = ValidateBinaryOperatorOperands(
            transformedLeft,
            transformedRight,
            operatorKind,
            errorContextNode
        );

        T result;
        try
        {
            result = nodeFactory(transformedLeft, transformedRight);
        }
        catch (InvalidOperandTypesException exception) when (operandsPassedValidation)
        {
            throw new InvalidOperandTypesException(
                exception.LeftType,
                exception.RightType,
                operatorKind.ToString(),
                errorContextNode.SpanOrEmpty()
            );
        }

        if (errorContextNode.HasSpan)
            result.WithSpan(errorContextNode.Span);
        return result;
    }

    private Node BindRangeBound<T>((Node Left, Node Right) operands, Func<Node, Node, T> create, BetweenNode source)
        where T : Node =>
        BindBinaryOperator(
            operands,
            create,
            source,
            BinaryOperatorKind.Relational,
            BinaryOperationContext.RelationalComparison
        );

    private Node TransformStringToDateTimeIfNeeded(Node candidateNode, Node otherNode)
    {
        var otherNodeType = otherNode.ReturnType;
        if (
            candidateNode is not WordNode stringNode
            || otherNodeType == null
            || !TypeConversionNodeFactory.IsDateTimeType(otherNodeType)
        )
            return candidateNode;

        var conversion = _nodeFactory.CreateDateTimeConversionNode(otherNodeType, stringNode.Value);
        conversion.Arguments.Args[0].WithSpan(stringNode.Span);
        conversion.WithSpan(stringNode.Span);
        return conversion;
    }

    private Node TransformToNumericTypeIfNeeded(
        Node candidateNode,
        Node otherNode,
        BinaryOperationContext operationContext
    )
    {
        var shouldTransform = operationContext switch
        {
            BinaryOperationContext.ArithmeticOperation => TypeConversionNodeFactory.IsObjectType(
                candidateNode.ReturnType
            ),
            BinaryOperationContext.RelationalComparison => TypeConversionNodeFactory.IsStringOrObjectType(
                candidateNode.ReturnType
            ),
            _ => TypeConversionNodeFactory.IsStringOrObjectType(candidateNode.ReturnType),
        };

        if (!shouldTransform || !TypeConversionNodeFactory.IsNumericLiteralNode(otherNode, out var targetType))
            return candidateNode;

        if (ContainsScriptParameterReference(candidateNode))
            return candidateNode;

        return _nodeFactory.CreateNumericConversionNode(
            candidateNode,
            targetType,
            TypeConversionNodeFactory.IsObjectType(candidateNode.ReturnType),
            operationContext
        );
    }

    private Node BindBooleanOperand(Node operand, string operatorName, Node errorContextNode)
    {
        if (CommonColumnTypeResolver.IsExplicitNullType(operand.ReturnType))
            return NullArithmeticBinding.AsNullable(operand, typeof(bool));
        var operandType = NormalizeOperandType(operand.ReturnType);
        if (CanSkipStaticTypeValidation(operandType) || operandType == typeof(bool))
            return operand;

        ThrowOrReportInvalidOperandTypes(
            typeof(bool),
            operandType,
            errorContextNode,
            $"Operator {operatorName} requires boolean operands, but got '{operandType.Name}'."
        );
        return operand;
    }

    private void ValidatePatternOperand(Node operand, string operatorName, Node errorContextNode)
    {
        var operandType = NormalizeOperandType(operand.ReturnType);
        if (CanSkipStaticTypeValidation(operandType) || operandType == typeof(string))
            return;

        var message = $"Operator {operatorName} requires string operands, but got '{operandType.Name}'.";

        if (TryReportTypeMismatch(message, errorContextNode))
            return;

        throw new TypeMismatchException(typeof(string), operandType, errorContextNode.SpanOrEmpty());
    }

    private void ValidateCollectionPredicateItems(Node left, ArgsListNode args, Node errorContextNode)
    {
        foreach (var item in args.Args)
            ValidateBinaryOperatorOperands(left, item, BinaryOperatorKind.Equality, errorContextNode);
    }

    private bool ValidateBinaryOperatorOperands(
        Node left,
        Node right,
        BinaryOperatorKind operatorKind,
        Node errorContextNode
    )
    {
        var leftType = NormalizeOperandType(left.ReturnType);
        var rightType = NormalizeOperandType(right.ReturnType);

        if (CanSkipStaticTypeValidation(leftType) || CanSkipStaticTypeValidation(rightType))
            return true;

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
            _ => true,
        };

        if (isValid)
            return true;

        if (
            operatorKind
            is BinaryOperatorKind.Equality
                or BinaryOperatorKind.Inequality
                or BinaryOperatorKind.Relational
        )
        {
            var message = CreateComparisonTypeMismatchMessage(left, right, leftType, rightType);
            if (TryReportTypeMismatch(message, errorContextNode))
                return false;

            throw new TypeMismatchException(leftType, rightType, errorContextNode.SpanOrEmpty());
        }

        ThrowOrReportInvalidOperandTypes(leftType, rightType, errorContextNode);
        return false;
    }
}
