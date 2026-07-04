namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyRightAlias(
        ExecutionExpression expression,
        string rightAlias)
    {
        return OuterApplyNullSubstitutionService.SubstituteRightAlias(expression, rightAlias);
    }

}
