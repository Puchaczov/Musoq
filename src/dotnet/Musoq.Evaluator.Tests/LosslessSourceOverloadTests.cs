using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.Reflection;
using Musoq.Schema.StructuralInputs;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LosslessSourceOverloadTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void PositionalWidening_ShouldSelectTheMoreSpecificSource(bool typed, bool reverse)
    {
        var methods = CreateMethods(typed, reverse, typeof(IntSource), typeof(LongSource));

        var result = SchemaSourceArgumentBinder.Bind(new ArgsListNode([new IntegerNode("1", "s")]), methods);

        Assert.IsNull(result.Failure, result.Failure?.Message);
        Assert.AreEqual(typeof(int), result.Invocation!.Signature.Parameters[0].ParameterType);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void NamedWidening_ShouldUseTheSamePreferenceAsPositionalBinding(bool typed, bool reverse)
    {
        var methods = CreateMethods(typed, reverse, typeof(IntSource), typeof(LongSource));
        var arguments = new ArgsListNode([new IntegerNode("1", "s")], [new ArgumentName("value", default)], default);

        var result = SchemaSourceArgumentBinder.Bind(arguments, methods);

        Assert.IsNull(result.Failure, result.Failure?.Message);
        Assert.AreEqual(typeof(int), result.Invocation!.Signature.Parameters[0].ParameterType);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void ConflictingPreferences_ShouldBeAmbiguousInEitherRegistrationOrder(bool typed, bool reverse)
    {
        var methods = CreateMethods(typed, reverse, typeof(FirstSpecificSource), typeof(SecondSpecificSource));

        var result = SchemaSourceArgumentBinder.Bind(new ArgsListNode([new IntegerNode("1", "s"), new IntegerNode("2", "s")]), methods);

        Assert.AreEqual(DiagnosticCode.MQ3089_AmbiguousCallableOverload, result.Failure?.Code);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LossyLongToDouble_ShouldNotBeApplicable(bool typed)
    {
        var result = SchemaSourceArgumentBinder.Bind(new ArgsListNode([new IntegerNode("1", "l")]),
            CreateMethods(typed, false, typeof(DoubleSource)));

        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, result.Failure?.Code);
    }

    private static SchemaMethodInfo[] CreateMethods(bool typed, bool reverse, params Type[] sources)
    {
        var methods = sources.Select(source =>
        {
            var constructor = source.GetConstructors().Single();
            var metadata = new SchemaConstructorInfo(constructor, false,
                constructor.GetParameters().Select(parameter => (parameter.Name!, parameter.ParameterType)).ToArray());
            if (typed)
                metadata.StructuralContract = StructuralInputMetadata.CreateContract("source", constructor);
            return new SchemaMethodInfo("source", metadata);
        }).ToArray();
        return reverse ? methods.Reverse().ToArray() : methods;
    }

    private sealed class IntSource { public IntSource(int value) { _ = value; } }
    private sealed class LongSource { public LongSource(long value) { _ = value; } }
    private sealed class DoubleSource { public DoubleSource(double value) { _ = value; } }
    private sealed class FirstSpecificSource { public FirstSpecificSource(int first, float second) { _ = first; _ = second; } }
    private sealed class SecondSpecificSource { public SecondSpecificSource(long first, int second) { _ = first; _ = second; } }
}
