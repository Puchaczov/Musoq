using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExpandoAdapterShape(
    string Alias,
    string TypeName,
    Type RuntimeType,
    IReadOnlyList<FieldBinding> Fields) : RowShape(TypeName, Fields);
