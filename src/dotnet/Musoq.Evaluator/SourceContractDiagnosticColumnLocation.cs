using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

internal sealed record SourceContractDiagnosticColumnLocation(
    TextSpan ColumnSpan,
    IReadOnlyDictionary<string, TextSpan> ModifierSpans);
