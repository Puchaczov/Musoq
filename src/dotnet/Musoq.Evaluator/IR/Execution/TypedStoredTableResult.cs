using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record TypedStoredTableResult(
    int TableIndex,
    GeneratedRowShape RowShape);
