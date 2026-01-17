using System.Threading;
using Musoq.Plugins;

namespace Musoq.Evaluator;

public class AmendableQueryStats : QueryStats
{
    public AmendableQueryStats()
    {
        InternalRowNumber = 0;
    }

    private AmendableQueryStats(int rowNumber)
    {
        InternalRowNumber = rowNumber;
    }

    public AmendableQueryStats IncrementRowNumber()
    {
        var value = Interlocked.Increment(ref InternalRowNumber);

        return new AmendableQueryStats(value);
    }
}