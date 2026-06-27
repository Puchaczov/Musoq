using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupOwnerField(
    int PrefixLength,
    string FieldName,
    AggregateGroupShape Shape);
