using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Optimization;

internal enum OptimizationAnalysisInvalidationRule
{
    Never,
    OnPlanChanged
}
