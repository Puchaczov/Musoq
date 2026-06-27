using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRowTypeAccess(string TypeName, string FieldName) : FieldAccessStrategy;
