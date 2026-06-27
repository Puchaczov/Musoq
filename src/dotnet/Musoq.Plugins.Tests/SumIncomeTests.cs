using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class SumIncomeTests
{
    [TestMethod]
    public void SumIncomeAggregateKernel_SumsOnlyNonNegativeValues()
    {
        AssertIncome(1, 4, -6, 0, 5);
        AssertIncome<long>(1, 4, -6, 0, 5);
        AssertIncome(1.5f, 4.5f, -6.5f, 0f, 6f);
        AssertIncome(1.5, 4.5, -6.5, 0, 6);
        AssertIncome(1m, 2m, -4m, 0m, 3m);
    }

    [TestMethod]
    public void SumIncomeAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new SumIncomeAggregateKernel<int>.State();

        Assert.IsNull(SumIncomeAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumIncomeAggregateKernel_AllNegativeOrNullInputsReturnNull()
    {
        var state = new SumIncomeAggregateKernel<int>.State();

        SumIncomeAggregateKernel<int>.Set(ref state, -1);
        SumIncomeAggregateKernel<int>.Set(ref state, null);
        SumIncomeAggregateKernel<int>.Set(ref state, -4);

        Assert.IsNull(SumIncomeAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumIncomeAggregateKernel_ZeroIsAQualifyingIncomeValue()
    {
        var state = new SumIncomeAggregateKernel<int>.State();

        SumIncomeAggregateKernel<int>.Set(ref state, 0);

        Assert.AreEqual(0, SumIncomeAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumIncomeAggregateKernel_MergeCombinesOnlyQualifiedPartialStates()
    {
        var target = new SumIncomeAggregateKernel<decimal>.State();
        var source = new SumIncomeAggregateKernel<decimal>.State();

        SumIncomeAggregateKernel<decimal>.Set(ref target, 2m);
        SumIncomeAggregateKernel<decimal>.Set(ref target, -10m);
        SumIncomeAggregateKernel<decimal>.Set(ref source, 3m);
        SumIncomeAggregateKernel<decimal>.Merge(ref target, in source);

        Assert.AreEqual(5m, SumIncomeAggregateKernel<decimal>.Get(in target));
    }

    private static void AssertIncome<T>(T first, T second, T third, T fourth, T expected)
        where T : struct, INumber<T>
    {
        var state = new SumIncomeAggregateKernel<T>.State();

        SumIncomeAggregateKernel<T>.Set(ref state, first);
        SumIncomeAggregateKernel<T>.Set(ref state, null);
        SumIncomeAggregateKernel<T>.Set(ref state, second);
        SumIncomeAggregateKernel<T>.Set(ref state, third);
        SumIncomeAggregateKernel<T>.Set(ref state, fourth);

        Assert.AreEqual(expected, SumIncomeAggregateKernel<T>.Get(in state));
    }
}
