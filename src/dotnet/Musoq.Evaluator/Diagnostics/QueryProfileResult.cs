using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Diagnostics;

public sealed record QueryProfileResult(
    Table Result,
    QueryProfileSnapshot Profile,
    string ProfileText);
