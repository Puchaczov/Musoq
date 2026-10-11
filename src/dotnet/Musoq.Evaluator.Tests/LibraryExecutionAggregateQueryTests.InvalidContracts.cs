using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Converter.Exceptions;
using System.Numerics;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionAggregateQueryTests
{
    [TestMethod]
    [DataRow("MissingReset", "Reset(ref State)")]
    [DataRow("ReadOnlySet", "Set(ref State, args...)")]
    [DataRow("MutableGet", "Get(in State)")]
    [DataRow("MutableMergeSource", "Merge(ref State, in State)")]
    [DataRow("SharedState", "shared static fields")]
    [DataRow("ErasedState", "concrete state type")]
    [DataRow("UninferredKernel", "cannot be inferred")]
    [DataRow("UninferredResult", "type parameter R cannot be inferred")]
    public void InvalidKernel_WhenCalledBySql_ShouldDiagnoseItsResponsibleContract(string function, string contract)
    {
        var query = $"select {function}(Amount) as Value from #schema.first()";

        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, InvalidKernelLibrary>(query, LibraryExecutionFixture.CreateRows()));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains(function, error.Message);
        Assert.Contains(contract, error.Message);
    }

    public sealed class InvalidKernelLibrary : LibraryBase
    {
        [AggregateFunction(typeof(MissingResetKernel))]
        public decimal? MissingReset(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(ReadOnlySetKernel))]
        public decimal? ReadOnlySet(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(MutableGetKernel))]
        public decimal? MutableGet(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(MutableMergeSourceKernel))]
        public decimal? MutableMergeSource(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(SharedStateKernel))]
        public decimal? SharedState(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(ErasedStateKernel), StateType = typeof(object))]
        public decimal? ErasedState(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(LibraryExecutionLibrary.GenericTotalKernel<>))]
        public decimal? UninferredKernel(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
        [AggregateFunction(typeof(AverageKernel<,>))]
        public R? UninferredResult<T, R>(T? value) where T : struct, INumber<T> where R : struct, INumber<R>
            => AggregateFunction.NotInvoked<R?>();
    }

    public static class MissingResetKernel
    {
        public struct State { public decimal? Value; }
        public static void Set(ref State state, decimal? value) => state.Value = value;
        public static decimal? Get(in State state) => state.Value;
    }

    public static class ReadOnlySetKernel
    {
        public struct State { public decimal? Value; }
        public static void Reset(ref State state) => state = default;
        public static void Set(in State state, decimal? value) { }
        public static decimal? Get(in State state) => state.Value;
    }

    public static class MutableGetKernel
    {
        public struct State { public decimal? Value; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, decimal? value) => state.Value = value;
        public static decimal? Get(ref State state) => state.Value;
    }

    public static class MutableMergeSourceKernel
    {
        public struct State { public decimal? Value; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, decimal? value) => state.Value = value;
        public static decimal? Get(in State state) => state.Value;
        public static void Merge(ref State target, ref State source) => target = source;
    }

    public static class SharedStateKernel
    {
        public static long Invocations = 0;
        public struct State { public decimal? Value; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, decimal? value) => state.Value = value;
        public static decimal? Get(in State state) => state.Value;
    }

    public static class ErasedStateKernel
    {
        public static void Reset(ref object state) => state = new();
        public static void Set(ref object state, decimal? value) => state = value!;
        public static decimal? Get(in object state) => (decimal?)state;
    }
}
