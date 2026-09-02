namespace Musoq.Evaluator.IR.Analysis;

internal enum OptimizationEvaluationClassification
{
    Unknown,
    EvaluationPreserving,
    StabilityChecked,
    RegionChecked,
    NotApplicable
}
