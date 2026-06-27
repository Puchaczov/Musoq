using System;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Converter.Tests.Schema;

public class EmptyLibrary : LibraryBase
{
    [BindableMethod]
    public int ExpensiveMethod(int value)
    {
        return value * 2;
    }

    [AggregateFunction(
        typeof(CustomLengthTotalAggregate),
        Name = nameof(CustomLengthTotal),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public int CustomLengthTotal(int? value, [AggregateParent] int parent = 0)
    {
        return AggregateFunction.NotInvoked<int>();
    }

    [WindowFunction(Name = "RunningProduct")]
    public IWindowFunction<int, decimal> WindowRunningProduct()
    {
        return new RunningProductWindowFunction();
    }

    [WindowFunction(Name = "ScaledRunningProduct")]
    public ScaledRunningProductWindowFunction WindowScaledRunningProduct()
    {
        return new ScaledRunningProductWindowFunction();
    }

    [WindowFunction(Name = "ObjectRunningProduct")]
    public IWindowFunction<object, decimal> WindowObjectRunningProduct()
    {
        return new ObjectRunningProductWindowFunction();
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

    private sealed class RunningProductWindowFunction : IWindowFunction<int, decimal>
    {
        private decimal _product;

        public void PartitionStart()
        {
            _product = 1;
        }

        public void Accumulate(int value)
        {
            _product *= value;
        }

        public decimal GetValue()
        {
            return _product;
        }
    }

    public sealed class ScaledRunningProductWindowFunction :
        IWindowFunction<int, decimal>,
        IWindowFunctionArguments<int>
    {
        private decimal _product;
        private int _scale = 1;

        public void SetArguments(int scale)
        {
            _scale = scale;
        }

        public void PartitionStart()
        {
            _product = 1;
        }

        public void Accumulate(int value)
        {
            _product *= value * _scale;
        }

        public decimal GetValue()
        {
            return _product;
        }
    }

    private sealed class ObjectRunningProductWindowFunction : IWindowFunction<object, decimal>
    {
        private decimal _product;

        public void PartitionStart()
        {
            _product = 1;
        }

        public void Accumulate(object value)
        {
            if (value is not null)
                _product *= Convert.ToDecimal(value);
        }

        public decimal GetValue()
        {
            return _product;
        }
    }
}
