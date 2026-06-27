using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal enum CodegenHelperExtractionCandidateKind
{
    ExistingHelper,
    InlineBlock
}
