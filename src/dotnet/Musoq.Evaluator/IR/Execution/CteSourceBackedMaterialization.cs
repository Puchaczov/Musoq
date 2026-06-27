namespace Musoq.Evaluator.IR.Execution;

internal sealed record CteSourceBackedMaterialization(
    int CreateTableIndex,
    int StoreTableIndex,
    ExecutionCreateTable CreateTable,
    ExecutionSourceLoop Loop,
    ExecutionAppendRow AppendRow);
