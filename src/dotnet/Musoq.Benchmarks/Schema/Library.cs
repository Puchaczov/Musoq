using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks.Schema;

public class Library : LibraryBase
{
    [AggregateFunction(
        typeof(CustomLengthTotalAggregate),
        Name = nameof(CustomLengthTotal),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public int CustomLengthTotal(int? value, [AggregateParent] int parent = 0)
    {
        return AggregateFunction.NotInvoked<int>();
    }

    public static class CustomLengthTotalAggregate
    {
        public struct State
        {
            public int Sum;
        }

        public static void Set(ref State state, int? value)
        {
            if (value.HasValue)
                state.Sum = checked(state.Sum + value.GetValueOrDefault());
        }

        public static int Get(in State state)
        {
            return state.Sum;
        }

        public static void Merge(ref State target, in State source)
        {
            target.Sum = checked(target.Sum + source.Sum);
        }
    }
}
