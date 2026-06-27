using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator;

/// <summary>
///     Shared runtime contract for generated query classes.
/// </summary>
public interface IQueryRunnable
{
    ISchemaProvider Provider { get; set; }

    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

    IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }

    IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }

    ILogger Logger { get; set; }

    event QueryPhaseEventHandler PhaseChanged;

    event DataSourceEventHandler DataSourceProgress;
}
