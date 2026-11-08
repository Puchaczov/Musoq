using Musoq.Converter.Build;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests.IR;

internal sealed class PlanTextBuildHarness : GenericEntityTestBase
{
    public BuildItems BuildForThreeSources<TFirst, TSecond, TThird>(
        string query,
        TFirst[] first,
        TSecond[] second,
        TThird[] third)
    {
        return CreateBuildItems(query, first, second, third);
    }
}
