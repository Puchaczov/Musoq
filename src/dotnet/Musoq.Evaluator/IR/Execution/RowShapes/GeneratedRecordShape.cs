using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRecordShape(
    string TypeName,
    IReadOnlyList<FieldBinding> Fields,
    bool EmitAsValueType = false) : RowShape(TypeName, Fields);
