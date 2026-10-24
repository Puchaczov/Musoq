using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.ProjectionAndApply;

internal static class StringConcatenationNullSubstitution
{
    internal static OuterApplyNullSubstitutionResult Substitute(ExecutionStringConcatenation concatenation, string rightAlias)
    {
        var operands = new List<ExecutionExpression>(concatenation.Expressions.Count);
        foreach (var operand in concatenation.Expressions)
        {
            var result = OuterApplyNullSubstitutionService.SubstituteRightAlias(operand, rightAlias);
            if (!result.IsBuilt)
                return OuterApplyNullSubstitutionResult.Unsupported(result.UnsupportedReason);
            operands.Add(result.IsUnknown ? new ExecutionLiteral(null, typeof(string)) : result.Expression);
        }
        return OuterApplyNullSubstitutionResult.Known(new ExecutionStringConcatenation(operands));
    }
}
