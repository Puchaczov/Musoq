using System;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Diagnostics;

public sealed record SourceOperationProfileSnapshot(
    string Name,
    SourceDiagnosticOperation Operation,
    long Count,
    TimeSpan ElapsedTime);
