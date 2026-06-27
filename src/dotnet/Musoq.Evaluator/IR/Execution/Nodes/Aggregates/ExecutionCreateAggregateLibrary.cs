using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateAggregateLibrary(
    ExecutionVariable Library,
    Type LibraryType) : ExecutionNode;
