using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Evaluator.Utils;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using System.Threading;

namespace Musoq.Evaluator.Visitors;

internal static class MetadataSnapshotContractsFreezer
{
    public static IReadOnlyDictionary<TKey, IReadOnlyList<ISchemaColumn>> FreezeSchemaColumns<TKey>(
        IEnumerable<KeyValuePair<TKey, IEnumerable<ISchemaColumn>>> values)
        where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, IReadOnlyList<ISchemaColumn>>(
            values.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<ISchemaColumn>)Array.AsReadOnly(
                    pair.Value.Select(BoundSchemaColumn.Capture).Cast<ISchemaColumn>().ToArray())));
    }

    public static IReadOnlyList<BoundSourceContract> FreezeSourceContracts(
        IEnumerable<BoundSourceContract> values)
    {
        return Array.AsReadOnly(values
            .Select(static contract => contract with
            {
                Columns = Array.AsReadOnly(contract.Columns.ToArray()),
                RequiredMemberSignatures = Array.AsReadOnly(contract.RequiredMemberSignatures.ToArray())
            })
            .ToArray());
    }

    public static IReadOnlyDictionary<TKey, SourcePlanRequest> FreezeSourcePlanRequests<TKey>(
        IReadOnlyDictionary<TKey, SourcePlanRequest> values)
        where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, SourcePlanRequest>(
            values.ToDictionary(
                pair => pair.Key,
                pair => pair.Value with { CancellationToken = CancellationToken.None }));
    }
}
