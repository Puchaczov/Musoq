using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Plugins;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class LosslessOverloadResolutionTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Widening_ShouldPreferTheMoreSpecificTargetInEitherRegistrationOrder(bool reverse)
    {
        var metadata = new OrderedMetadata(reverse);

        var method = metadata.GetMethod("Choose", [typeof(short)], null);

        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ConflictingArgumentPreferences_ShouldRemainAmbiguous(bool reverse)
    {
        var metadata = new OrderedMetadata(reverse);

        var exception = Assert.Throws<MethodResolutionException>(() =>
            metadata.GetMethod("Conflicting", [typeof(short), typeof(short)], null));

        Assert.AreEqual("ambiguous-overload", exception.ResolutionReason);
    }

    [TestMethod]
    public void Int32_ShouldSelectDoubleInsteadOfLossySingle()
    {
        var method = new OrderedMetadata(false).GetMethod("Floating", [typeof(int)], null);

        Assert.AreEqual(typeof(double), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void Int64_ShouldRejectBothFloatingOverloads()
    {
        Assert.IsFalse(new OrderedMetadata(false).TryGetMethod("Floating", [typeof(long)], null, out _));
    }

    [TestMethod]
    public void NullableInput_ShouldNotBindToNonNullableValueParameters()
    {
        Assert.IsFalse(new OrderedMetadata(false).TryGetMethod("Choose", [typeof(short?)], null, out _));
    }

    [TestMethod]
    public void OtherwiseEqualCandidates_ShouldPreferFixedArity()
    {
        var method = new OrderedMetadata(false).GetMethod("Fixed", [typeof(short)], null);

        Assert.IsFalse(method.GetParameters()[0].IsDefined(typeof(ParamArrayAttribute)));
    }

    [TestMethod]
    public void OtherwiseEqualCandidates_ShouldPreferNonGenericDeclaration()
    {
        var method = new OrderedMetadata(false).GetMethod("Generic", [typeof(int)], null);

        Assert.IsFalse(method.IsGenericMethod);
    }

    [TestMethod]
    public void GenericIdentity_ShouldOutrankConcreteWidening()
    {
        var method = new OrderedMetadata(false).GetMethod("GenericWidening", [typeof(int)], null);

        Assert.IsTrue(method.IsGenericMethod);
    }

    [TestMethod]
    public void OtherwiseEqualCandidates_ShouldPreferFewerDefaults()
    {
        var method = new OrderedMetadata(false).GetMethod("Defaults", [typeof(int)], null);

        Assert.HasCount(1, method.GetParameters());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BuiltInAdapters_ShouldSelectTheirNativeContractsInEitherOrder(bool reverse)
    {
        var metadata = new OrderedMetadata(reverse, typeof(LibraryBase));

        var split = metadata.GetMethod("Split", [typeof(string), typeof(string)], null);
        var rowNumber = metadata.GetMethod("RowNumber", [], null);

        Assert.AreEqual(typeof(ReadOnlySpan<string>), split.GetParameters()[1].ParameterType);
        Assert.AreEqual(typeof(IQueryStats), rowNumber.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void VariadicArguments_ShouldUseTheSameLosslessConversions()
    {
        var metadata = new OrderedMetadata(false);

        Assert.IsTrue(metadata.TryGetMethod("NumericTail", [typeof(short), typeof(int)], null, out _));
        Assert.IsFalse(metadata.TryGetMethod("NumericTail", [typeof(short), typeof(long)], null, out _));
        Assert.IsFalse(metadata.TryGetMethod("NumericTail", [typeof(long)], null, out _));
        Assert.IsFalse(metadata.TryGetMethod("NumericTail", [], null, out _));
    }

    [TestMethod]
    public void GenericNullableLifting_ShouldOutrankNumericWidening()
    {
        var method = new OrderedMetadata(false).GetMethod("NullableGeneric", [typeof(short)], null);

        Assert.IsTrue(method.IsGenericMethod);
    }

    private sealed class OrderedMetadata : MethodsMetadata
    {
        internal OrderedMetadata(bool reverse, Type? library = null)
        {
            var methods = (library ?? typeof(Overloads)).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(static method => method.IsDefined(typeof(BindableMethodAttribute)) &&
                    !method.IsDefined(typeof(WindowFunctionAttribute)) && !method.IsDefined(typeof(AggregateFunctionAttribute)))
                .OrderBy(static method => method.ToString(), StringComparer.Ordinal).ToArray();
            foreach (var method in reverse ? methods.Reverse() : methods)
                RegisterMethod(method);
        }
    }

    public sealed class Overloads
    {
        [BindableMethod] public void Choose(int value) { }
        [BindableMethod] public void Choose(long value) { }
        [BindableMethod] public void Conflicting(int first, float second) { }
        [BindableMethod] public void Conflicting(long first, int second) { }
        [BindableMethod] public void Floating(float value) { }
        [BindableMethod] public void Floating(double value) { }
        [BindableMethod] public void Fixed(int value) { }
        [BindableMethod] public void Fixed(params int[] values) { }
        [BindableMethod] public void Generic(int value) { }
        [BindableMethod] public void Generic<T>(T value) { }
        [BindableMethod] public void GenericWidening(long value) { }
        [BindableMethod] public void GenericWidening<T>(T value) { }
        [BindableMethod] public void Defaults(int value) { }
        [BindableMethod] public void Defaults(int value, int extra = 0) { }
        [BindableMethod] public void NumericTail(int first, params double[] values) { }
        [BindableMethod] public void NullableGeneric<T>(T? value) where T : struct { }
        [BindableMethod] public void NullableGeneric(long value) { }
    }
}
