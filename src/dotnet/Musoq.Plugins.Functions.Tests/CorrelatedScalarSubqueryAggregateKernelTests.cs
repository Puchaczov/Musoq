using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class CorrelatedScalarSubqueryAggregateKernelTests
{
    [TestMethod]
    public void NullableValueKernels_ShouldKeepEmptyAndNullRowsDistinctDuringMerge()
    {
        var scalar = new NullableScalarSubqueryAggregateKernel<ulong>.State();
        var other = new NullableScalarSubqueryAggregateKernel<ulong>.State();
        Assert.IsNull(NullableScalarSubqueryAggregateKernel<ulong>.Get(in scalar));
        NullableScalarSubqueryAggregateKernel<ulong>.Set(ref other, null);
        NullableScalarSubqueryAggregateKernel<ulong>.Merge(ref scalar, in other);
        NullableScalarSubqueryAggregateKernel<ulong>.Set(ref scalar, 7UL);
        Assert.Throws<ScalarSubqueryCardinalityException>(() =>
            NullableScalarSubqueryAggregateKernel<ulong>.Get(in scalar)
        );
        NullableScalarSubqueryAggregateKernel<ulong>.Reset(ref scalar);
        Assert.IsNull(NullableScalarSubqueryAggregateKernel<ulong>.Get(in scalar));

        var correlated = new NullableCorrelatedScalarSubqueryAggregateKernel<float>.State();
        var first = new NullableCorrelatedScalarSubqueryAggregateKernel<float>.State();
        NullableCorrelatedScalarSubqueryAggregateKernel<float>.Set(ref first, null);
        NullableCorrelatedScalarSubqueryAggregateKernel<float>.Merge(ref correlated, in first);
        var result = NullableCorrelatedScalarSubqueryAggregateKernel<float>.Get(in correlated);
        Assert.AreEqual(1, result.Cardinality);
        Assert.IsNull(result.Value);
        NullableCorrelatedScalarSubqueryAggregateKernel<float>.Reset(ref correlated);
        Assert.AreEqual(0, NullableCorrelatedScalarSubqueryAggregateKernel<float>.Get(in correlated).Cardinality);
    }

    [TestMethod]
    public void Get_WhenStateIsEmpty_ShouldReturnEmptyCardinality()
    {
        var state = new CorrelatedScalarSubqueryAggregateKernel<int>.State();

        var result = CorrelatedScalarSubqueryAggregateKernel<int>.Get(in state);

        Assert.AreEqual(0, result.Cardinality);
        Assert.AreEqual(0, result.Value);
    }

    [TestMethod]
    public void Set_WhenMoreThanOneValueArrives_ShouldRetainFirstAndSaturateCardinality()
    {
        var state = new CorrelatedScalarSubqueryAggregateKernel<int>.State();

        CorrelatedScalarSubqueryAggregateKernel<int>.Set(ref state, 17);
        CorrelatedScalarSubqueryAggregateKernel<int>.Set(ref state, 23);
        CorrelatedScalarSubqueryAggregateKernel<int>.Set(ref state, 42);
        var result = CorrelatedScalarSubqueryAggregateKernel<int>.Get(in state);

        Assert.AreEqual(2, result.Cardinality);
        Assert.AreEqual(17, result.Value);
    }

    [TestMethod]
    public void Merge_WhenBothStatesHaveRows_ShouldRetainFirstAndSaturateCardinality()
    {
        var target = new CorrelatedScalarSubqueryAggregateKernel<int>.State();
        var source = new CorrelatedScalarSubqueryAggregateKernel<int>.State();
        CorrelatedScalarSubqueryAggregateKernel<int>.Set(ref target, 17);
        CorrelatedScalarSubqueryAggregateKernel<int>.Set(ref source, 23);

        CorrelatedScalarSubqueryAggregateKernel<int>.Merge(ref target, in source);
        CorrelatedScalarSubqueryAggregateKernel<int>.Merge(ref target, in source);
        var result = CorrelatedScalarSubqueryAggregateKernel<int>.Get(in target);

        Assert.AreEqual(2, result.Cardinality);
        Assert.AreEqual(17, result.Value);
    }

    [TestMethod]
    public void Merge_WhenTargetIsEmpty_ShouldCopySourceState()
    {
        var target = new CorrelatedScalarSubqueryAggregateKernel<int>.State();
        var source = new CorrelatedScalarSubqueryAggregateKernel<int>.State();
        CorrelatedScalarSubqueryAggregateKernel<int>.Set(ref source, 23);

        CorrelatedScalarSubqueryAggregateKernel<int>.Merge(ref target, in source);
        var result = CorrelatedScalarSubqueryAggregateKernel<int>.Get(in target);

        Assert.AreEqual(1, result.Cardinality);
        Assert.AreEqual(23, result.Value);
    }

    [TestMethod]
    public void Extractor_WhenCardinalityExceedsOne_ShouldThrowExpectedDiagnostic()
    {
        var result = new CorrelatedScalarSubqueryResult<int>(17, 2);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CorrelatedScalarSubqueryResultExtractor.GetValue<int>(result)
        );

        Assert.AreEqual("Scalar subquery returned more than one row.", exception.Message);
    }
}
