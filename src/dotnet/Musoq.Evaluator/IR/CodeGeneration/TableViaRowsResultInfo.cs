using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed record TableViaRowsResultInfo(
    string TableName,
    string RowTypeName,
    string ShapeTypeName,
    IReadOnlyList<FieldBinding> ShapeFields,
    IReadOnlyList<ExecutionColumnMetadataField> Columns,
    string? ColumnsFieldName = null);
