using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRowNestedAccess(string TypeName, string FieldName, string PropertyPath) : FieldAccessStrategy;
