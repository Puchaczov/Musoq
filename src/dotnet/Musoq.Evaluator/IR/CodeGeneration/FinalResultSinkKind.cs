namespace Musoq.Evaluator.IR.CodeGeneration;

public enum FinalResultSinkKind
{
    TableDirect,
    TableRowsMaterialized,
    TypedSerialEnumerable,
    TypedParallelShards,
    GeneratedRowParallelShards
}
