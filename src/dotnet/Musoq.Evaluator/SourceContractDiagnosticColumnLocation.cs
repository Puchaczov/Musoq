using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator;

internal sealed record SourceContractDiagnosticColumnLocation(
    TextSpan ColumnSpan,
    IReadOnlyDictionary<string, TextSpan> ModifierSpans);
