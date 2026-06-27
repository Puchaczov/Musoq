using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    public static ScriptConstantEvaluationResult Evaluate(
        Node expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(variables);
        return EvaluateCore(expression, variables, parameters, ownerName);
    }

    public static ScriptConstantEvaluationResult EvaluateStaticExpression(
        Node expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(variables);
        return EvaluateCore(expression, variables, null, string.Empty);
    }

    private static ScriptConstantEvaluationResult EvaluateCore(
        Node expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        return expression switch
        {
            ConstantValueNode constant => ScriptConstantEvaluationResult.Evaluated(constant.ObjValue, constant.ReturnType ?? constant.ObjValue.GetType()),
            NullNode => ScriptConstantEvaluationResult.Evaluated(null, typeof(NullNode.NullType)),
            ParameterReferenceNode parameter => EvaluateParameterReference(parameter, variables, parameters, ownerName),
            ScriptVariableReferenceNode variable => EvaluateVariableReference(variable, variables, ownerName),
            AddNode add => EvaluateBinary(add.Left, add.Right, variables, parameters, ownerName, EvaluateAdd),
            HyphenNode subtract => EvaluateBinary(subtract.Left, subtract.Right, variables, parameters, ownerName, EvaluateSubtract),
            StarNode multiply => EvaluateBinary(multiply.Left, multiply.Right, variables, parameters, ownerName, EvaluateMultiply),
            FSlashNode divide => EvaluateBinary(divide.Left, divide.Right, variables, parameters, ownerName, EvaluateDivide),
            ModuloNode modulo => EvaluateBinary(modulo.Left, modulo.Right, variables, parameters, ownerName, EvaluateModulo),
            BitwiseAndNode bitwiseAnd => EvaluateBinary(bitwiseAnd.Left, bitwiseAnd.Right, variables, parameters, ownerName, EvaluateBitwiseAnd),
            BitwiseOrNode bitwiseOr => EvaluateBinary(bitwiseOr.Left, bitwiseOr.Right, variables, parameters, ownerName, EvaluateBitwiseOr),
            BitwiseXorNode bitwiseXor => EvaluateBinary(bitwiseXor.Left, bitwiseXor.Right, variables, parameters, ownerName, EvaluateBitwiseXor),
            LeftShiftNode leftShift => EvaluateBinary(leftShift.Left, leftShift.Right, variables, parameters, ownerName, EvaluateLeftShift),
            RightShiftNode rightShift => EvaluateBinary(rightShift.Left, rightShift.Right, variables, parameters, ownerName, EvaluateRightShift),
            AndNode and => EvaluateBinary(and.Left, and.Right, variables, parameters, ownerName, EvaluateAnd),
            OrNode orNode => EvaluateBinary(orNode.Left, orNode.Right, variables, parameters, ownerName, EvaluateOr),
            EqualityNode equality => EvaluateBinary(equality.Left, equality.Right, variables, parameters, ownerName, EvaluateEquality),
            IsDistinctFromNode distinct => EvaluateBinary(
                distinct.Left,
                distinct.Right,
                variables,
                parameters,
                ownerName,
                distinct.IsNegated ? EvaluateIsNotDistinctFrom : EvaluateIsDistinctFrom),
            DiffNode diff => EvaluateBinary(diff.Left, diff.Right, variables, parameters, ownerName, EvaluateDifference),
            GreaterNode greater => EvaluateBinary(greater.Left, greater.Right, variables, parameters, ownerName, EvaluateGreater),
            GreaterOrEqualNode greaterOrEqual => EvaluateBinary(greaterOrEqual.Left, greaterOrEqual.Right, variables, parameters, ownerName, EvaluateGreaterOrEqual),
            LessNode less => EvaluateBinary(less.Left, less.Right, variables, parameters, ownerName, EvaluateLess),
            LessOrEqualNode lessOrEqual => EvaluateBinary(lessOrEqual.Left, lessOrEqual.Right, variables, parameters, ownerName, EvaluateLessOrEqual),
            NotNode not => EvaluateNot(not.Expression, variables, parameters, ownerName),
            IsNullNode isNull => EvaluateIsNull(isNull, variables, parameters, ownerName),
            BetweenNode between => EvaluateBetween(between, variables, parameters, ownerName),
            _ => ScriptConstantEvaluationResult.Failed(
                DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
                CreateUnsupportedExpressionMessage(expression, ownerName))
        };
    }

    private static ScriptConstantEvaluationResult EvaluateBinary(
        Node leftExpression,
        Node rightExpression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName,
        Func<object?, object?, ScriptConstantEvaluationResult> operation)
    {
        var left = EvaluateCore(leftExpression, variables, parameters, ownerName);
        if (!left.Success)
            return left;

        var right = EvaluateCore(rightExpression, variables, parameters, ownerName);
        if (!right.Success)
            return right;

        return operation(left.Value, right.Value);
    }
}
