using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPlanOperatorDescriptor(
    string Id,
    string DisplayName,
    string NodeKind,
    ExecutionPlanOperatorRowCountStrategy RowCountStrategy)
{
    /// <summary>
    /// Gets the stable execution operation identifier when this descriptor was
    /// created from an execution plan. Text-only compatibility catalogs do not
    /// have a node identity and therefore leave this value unset.
    /// </summary>
    public string? OperationId { get; init; }
}
