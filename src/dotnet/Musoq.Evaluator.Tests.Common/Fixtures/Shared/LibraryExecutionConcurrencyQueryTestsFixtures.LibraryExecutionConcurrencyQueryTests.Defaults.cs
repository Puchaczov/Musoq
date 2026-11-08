using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionConcurrencyQueryTestsFixtures
{

    [LibraryExecutionDefaults(AggregatePermissions = FunctionExecutionPermissions.ConcurrentInvocations |
        FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge)]
    public class PermittedDefaultsLibrary : LibraryBase
    {
        [AggregateFunction(typeof(CountNullableAggregateKernel<int>))]
        public long DefaultCount(int? value) => AggregateFunction.NotInvoked<long>();

        [AggregateFunction(typeof(CountNullableAggregateKernel<int>), ExecutionPermissions = FunctionExecutionPermissions.None)]
        public long ExplicitSerialCount(int? value) => AggregateFunction.NotInvoked<long>();
    }

    public sealed class DescendantDefaultsLibrary : PermittedDefaultsLibrary
    {
        [AggregateFunction(typeof(CountNullableAggregateKernel<int>))]
        public long DescendantCount(int? value) => AggregateFunction.NotInvoked<long>();
    }

}
