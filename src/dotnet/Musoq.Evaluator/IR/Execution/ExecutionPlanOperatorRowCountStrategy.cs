using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionPlanOperatorRowCountStrategy
{
    Unknown,
    SourceBoundary,
    TableTransform,
    RowProducer
}
