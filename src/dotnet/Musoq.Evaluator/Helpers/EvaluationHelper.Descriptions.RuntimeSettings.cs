using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.RuntimeSettings;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static Table GetSourceRuntimeSettingsDescription(
        IReadOnlyList<SourceRuntimeSettingDescription> descriptions,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(descriptions);
        token.ThrowIfCancellationRequested();

        var table = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Required", typeof(bool), 1),
            new Column("Secret", typeof(bool), 2),
            new Column("Phases", typeof(string), 3),
            new Column("Status", typeof(string), 4),
            new Column("Description", typeof(string), 5)
        ]);

        foreach (var description in descriptions.OrderBy(static item => item.Name))
        {
            token.ThrowIfCancellationRequested();
            table.AddUnchecked(new DescriptionSourceRuntimeSettingRow(
                description.Name,
                description.Required,
                description.Secret,
                description.Phases.ToString(),
                description.Status.ToString(),
                description.Description));
        }

        return table;
    }
}
