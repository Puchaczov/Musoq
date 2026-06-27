using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Diagnostics;

public sealed record ExplainAnalyzeResult(
    Table Result,
    QueryProfileSnapshot Profile,
    string ExecutionPlanText,
    string ExplainAnalyzeText);
