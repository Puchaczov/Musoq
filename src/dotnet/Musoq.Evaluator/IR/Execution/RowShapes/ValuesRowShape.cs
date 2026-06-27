using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ValuesRowShape(
    string Alias,
    GeneratedRowShape GeneratedShape) : RowShape(Alias, GeneratedShape.Fields);
