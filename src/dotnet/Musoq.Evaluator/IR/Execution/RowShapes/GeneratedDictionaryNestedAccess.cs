using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedDictionaryNestedAccess(
    string FieldName,
    string PropertyPath,
    int? FieldIndex = null) : FieldAccessStrategy;
