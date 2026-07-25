namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyRightAlias(
        ExecutionExpression expression,
        string rightAlias)
    {
        return OuterApplyNullSubstitutionService.SubstituteRightAlias(expression, rightAlias);
    }

}
