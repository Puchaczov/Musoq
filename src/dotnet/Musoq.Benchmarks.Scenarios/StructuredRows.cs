using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Scenarios;

public sealed record NumberInput(int Id, decimal Amount);

public sealed class StructuredRows(IReadOnlyList<NumberInput> items, SourceExecutionContext context) : RowSource<NumberInput>
{
    public override IEnumerable<IReadOnlyList<NumberInput>> Chunks
    {
        get
        {
            context.EndWorkToken.ThrowIfCancellationRequested();
            yield return items;
        }
    }
}
