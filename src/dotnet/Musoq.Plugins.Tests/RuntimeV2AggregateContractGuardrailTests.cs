using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class RuntimeV2AggregateContractGuardrailTests
{
    [TestMethod]
    public void AggregateFunctionAttribute_ShouldRequireTypedKernelContract()
    {
        var constructor = typeof(AggregateFunctionAttribute)
            .GetConstructors()
            .Single(static ctor => ctor.GetParameters().Length == 1);

        Assert.AreEqual(typeof(Type), constructor.GetParameters()[0].ParameterType);
        Assert.AreEqual(typeof(Type), typeof(AggregateFunctionAttribute).GetProperty(nameof(AggregateFunctionAttribute.KernelType))!.PropertyType);
        Assert.AreEqual(typeof(Type), typeof(AggregateFunctionAttribute).GetProperty(nameof(AggregateFunctionAttribute.StateType))!.PropertyType);
    }

    [TestMethod]
    public void PluginsAssembly_ShouldNotReintroduceRuntimeV1AggregateContracts()
    {
        string[] retiredTypeNames =
        [
            "AggregationSetMethodAttribute",
            "AggregationGetMethodAttribute",
            "AggregateSetDoNotResolveAttribute",
            "InjectGroupAttribute",
            "Group",
            "UserMethodsLibrary"
        ];

        var publicTypeNames = typeof(AggregateFunctionAttribute).Assembly
            .GetExportedTypes()
            .Select(static type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = retiredTypeNames
            .Where(publicTypeNames.Contains)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Runtime v2 aggregate plugins must use typed kernels, not retired runtime-v1 contracts: " +
            string.Join(", ", offenders));
    }
}