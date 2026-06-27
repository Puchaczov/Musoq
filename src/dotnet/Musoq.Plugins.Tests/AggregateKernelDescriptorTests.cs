using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins.Tests;

[TestClass]
public class AggregateKernelDescriptorTests
{
    [TestMethod]
    public void AggregateFunction_NotInvoked_ShouldThrowClearError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = AggregateFunction.NotInvoked<int>();
        });

        StringAssert.Contains(exception.Message, "metadata-only");
    }

    [TestMethod]
    public void AggregateKernelDescriptor_WithTypedKernel_ShouldDescribeDeclaration()
    {
        var method = typeof(KernelLibrary).GetMethod(nameof(KernelLibrary.WeightedAvg))!;

        var descriptor = AggregateKernelDescriptor.Create(method);

        Assert.AreEqual("WeightedAvg", descriptor.FunctionName);
        Assert.AreEqual(method, descriptor.DeclarationMethod);
        Assert.AreEqual(typeof(WeightedAverageAggregate), descriptor.KernelType);
        Assert.AreEqual(typeof(WeightedAverageAggregate.State), descriptor.StateType);
        Assert.AreEqual(typeof(ValueTuple<decimal?, decimal?>), descriptor.InputShape.InputType);
        CollectionAssert.AreEqual(new[] { typeof(decimal?), typeof(decimal?) }, descriptor.InputShape.ArgumentTypes.ToArray());
        Assert.AreEqual(typeof(decimal?), descriptor.ResultType);
        Assert.AreEqual(typeof(decimal), descriptor.UnderlyingResultType);
        Assert.AreEqual(typeof(decimal?), descriptor.ResultDescriptor.PublicResultType);
        Assert.AreEqual(typeof(decimal), descriptor.ResultDescriptor.UnderlyingResultType);
        Assert.AreEqual(AggregateEmptyResultBehavior.Custom, descriptor.ResultDescriptor.EmptyResultBehavior);
        Assert.AreEqual(2, descriptor.ParentParameterIndex);
        Assert.IsTrue(descriptor.Inline);
        Assert.IsTrue(descriptor.SupportsMerge);
    }

    [TestMethod]
    public void AggregateKernelDescriptor_WithExplicitStateType_ShouldUseAttributeState()
    {
        var method = typeof(KernelLibrary).GetMethod(nameof(KernelLibrary.CountRows))!;

        var descriptor = AggregateKernelDescriptor.Create(method);

        Assert.AreEqual(typeof(ExternalCountState), descriptor.StateType);
        Assert.AreEqual(typeof(AggregateUnit), descriptor.InputShape.InputType);
        Assert.IsEmpty(descriptor.InputShape.ArgumentTypes);
        Assert.AreEqual(typeof(long), descriptor.ResultType);
    }

    [TestMethod]
    public void AggregateKernelDescriptor_WithMissingSet_ShouldFailValidation()
    {
        var method = typeof(KernelLibrary).GetMethod(nameof(KernelLibrary.InvalidMissingSet))!;

        var exception = Assert.Throws<InvalidOperationException>(() => AggregateKernelDescriptor.Create(method));

        StringAssert.Contains(exception.Message, "Set(ref State, args...)");
    }

    [TestMethod]
    public void AggregateKernelDescriptor_WithNegativeParentDefault_ShouldFailValidation()
    {
        var method = typeof(KernelLibrary).GetMethod(nameof(KernelLibrary.InvalidNegativeParent))!;

        var exception = Assert.Throws<InvalidOperationException>(() => AggregateKernelDescriptor.Create(method));

        StringAssert.Contains(exception.Message, "default depth cannot be negative");
    }

    [TestMethod]
    public void AggregateInputShape_WithMultipleArguments_ShouldUseValueTupleInput()
    {
        var shape = AggregateInputShape.Tuple([typeof(int), typeof(string)]);

        Assert.AreEqual(typeof(ValueTuple<int, string>), shape.InputType);
        CollectionAssert.AreEqual(new[] { typeof(int), typeof(string) }, shape.ArgumentTypes.ToArray());
    }

    [TestMethod]
    public void AggregateInputShape_ForUnit_ShouldUseAggregateUnitInput()
    {
        var shape = AggregateInputShape.Unit();

        Assert.AreEqual(typeof(AggregateUnit), shape.InputType);
        Assert.IsEmpty(shape.ArgumentTypes);
    }

    [TestMethod]
    public void CountAllAggregateKernel_WithTwoSets_ShouldCountEveryRow()
    {
        var state = new CountAllAggregateKernel.State();

        CountAllAggregateKernel.Set(ref state);
        CountAllAggregateKernel.Set(ref state);

        Assert.AreEqual(2L, CountAllAggregateKernel.Get(in state));
    }

    [TestMethod]
    public void SumAggregateKernel_WithMergedState_ShouldReturnCombinedNullableSum()
    {
        var first = new SumAggregateKernel<int>.State();
        var second = new SumAggregateKernel<int>.State();

        SumAggregateKernel<int>.Set(ref first, 10);
        SumAggregateKernel<int>.Set(ref second, 5);
        SumAggregateKernel<int>.Merge(ref first, in second);

        Assert.AreEqual(15, SumAggregateKernel<int>.Get(in first));
    }

    [TestMethod]
    public void SumDistinctAggregateKernel_WithMergedState_ShouldReturnDistinctCombinedSum()
    {
        var first = new SumDistinctAggregateKernel<int>.State();
        var second = new SumDistinctAggregateKernel<int>.State();

        SumDistinctAggregateKernel<int>.Set(ref first, 10);
        SumDistinctAggregateKernel<int>.Set(ref first, 10);
        SumDistinctAggregateKernel<int>.Set(ref second, 10);
        SumDistinctAggregateKernel<int>.Set(ref second, 5);
        SumDistinctAggregateKernel<int>.Merge(ref first, in second);

        Assert.AreEqual(15, SumDistinctAggregateKernel<int>.Get(in first));
    }

    [TestMethod]
    public void AvgDistinctAggregateKernel_WithNulls_ShouldExcludeNulls()
    {
        var state = new AvgDistinctAggregateKernel<int>.State();

        AvgDistinctAggregateKernel<int>.Set(ref state, 10);
        AvgDistinctAggregateKernel<int>.Set(ref state, null);
        AvgDistinctAggregateKernel<int>.Set(ref state, 10);
        AvgDistinctAggregateKernel<int>.Set(ref state, 30);

        Assert.AreEqual(20, AvgDistinctAggregateKernel<int>.Get(in state));
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private sealed class KernelLibrary
    {
        [AggregateFunction(typeof(WeightedAverageAggregate), Name = "WeightedAvg", Inline = true)]
        public decimal? WeightedAvg(decimal? value, decimal? weight, [AggregateParent] int parent = 0)
        {
            return AggregateFunction.NotInvoked<decimal?>();
        }

        [AggregateFunction(typeof(ExternalCountAggregate), StateType = typeof(ExternalCountState))]
        public long CountRows()
        {
            return AggregateFunction.NotInvoked<long>();
        }

        [AggregateFunction(typeof(MissingSetAggregate))]
        public int? InvalidMissingSet(int? value)
        {
            return AggregateFunction.NotInvoked<int?>();
        }

        [AggregateFunction(typeof(WeightedAverageAggregate))]
        public decimal? InvalidNegativeParent(decimal? value, decimal? weight, [AggregateParent] int parent = -1)
        {
            return AggregateFunction.NotInvoked<decimal?>();
        }
    }

    public static class WeightedAverageAggregate
    {
        public struct State
        {
            public decimal WeightedSum;
            public decimal WeightSum;
        }

        public static void Set(ref State state, decimal? value, decimal? weight)
        {
            if (!value.HasValue || !weight.HasValue)
                return;

            state.WeightedSum += value.Value * weight.Value;
            state.WeightSum += weight.Value;
        }

        public static decimal? Get(in State state)
        {
            return state.WeightSum == 0m
                ? null
                : state.WeightedSum / state.WeightSum;
        }

        public static void Merge(ref State target, in State source)
        {
            target.WeightedSum += source.WeightedSum;
            target.WeightSum += source.WeightSum;
        }
    }

    public struct ExternalCountState
    {
        public long Count;
    }

    public static class ExternalCountAggregate
    {
        public static void Set(ref ExternalCountState state)
        {
            state.Count += 1;
        }

        public static long Get(in ExternalCountState state)
        {
            return state.Count;
        }
    }

    public static class MissingSetAggregate
    {
        public struct State
        {
            public int Count;
        }

        public static int? Get(in State state)
        {
            return state.Count;
        }
    }
}
