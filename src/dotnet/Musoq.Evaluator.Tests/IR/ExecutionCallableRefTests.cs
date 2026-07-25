using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Abstractions;
using Musoq.Targets.CSharpClr;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionCallableRefTests
{
    [TestMethod]
    public void FromClr_WhenCreatedRepeatedly_ShouldExposeStablePortableSignature()
    {
        var method = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
        Assert.IsNotNull(method);

        var first = ExecutionClrBindingFactory.FromClr(method);
        var second = ExecutionClrBindingFactory.FromClr(method);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.StableId, second.StableId);
        Assert.AreEqual(nameof(string.Contains), first.MethodName);
        Assert.AreEqual(0, first.Descriptor.GenericArity);
        Assert.AreEqual(ExecutionCallableInvocationMode.Instance, first.Descriptor.InvocationMode);
        Assert.AreEqual(ExecutionPortableCallableKind.ClrMethod, first.Descriptor.Kind);
        Assert.AreEqual("primitive:string", first.Descriptor.ParameterTypes[0].StableName);
        Assert.AreEqual("primitive:bool", first.Descriptor.ReturnType!.StableName);
        Assert.DoesNotContain(", Version=", first.StableId);
    }

    [TestMethod]
    public void CSharpCompatibility_WhenCallableRefIsProvided_ShouldBindFromDescriptor()
    {
        var method = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
        Assert.IsNotNull(method);

        Assert.AreSame(method, ExecutionClrBindingFactory.FromClr(method).RequireClrMethod());
    }
}
