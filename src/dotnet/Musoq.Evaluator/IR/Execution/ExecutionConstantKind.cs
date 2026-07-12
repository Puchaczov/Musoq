namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionConstantKind
{
    Null = 0,
    Boolean = 1,
    Character = 2,
    SignedInteger = 3,
    UnsignedInteger = 4,
    FloatingPoint = 5,
    Decimal = 6,
    String = 7,
    DateTime = 8,
    DateTimeOffset = 9,
    Guid = 10,
    TimeSpan = 11,
    Enum = 12,
    ClrOnly = 13
}
