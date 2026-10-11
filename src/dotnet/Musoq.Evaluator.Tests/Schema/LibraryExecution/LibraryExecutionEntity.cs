namespace Musoq.Evaluator.Tests.Schema.LibraryExecution;

public sealed record LibraryExecutionEntity(
    int Position,
    string Partition,
    int? Integral,
    decimal? Amount,
    double? Floating);
