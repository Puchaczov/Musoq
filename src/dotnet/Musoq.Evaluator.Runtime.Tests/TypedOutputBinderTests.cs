using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.TypedOutput;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TypedOutputBinderTests
{
    [TestMethod]
    public void Create_WhenConstructorMatches_ShouldBindConstructor()
    {
        var plan = CreatePlan<ConstructorOutput>(
            new TypedOutputColumn("Name", 0, typeof(string)),
            new TypedOutputColumn("Age", 1, typeof(int)));

        Assert.IsNotNull(plan.Constructor);
        Assert.AreEqual(2, plan.ConstructorBindings.Count);
        Assert.AreEqual("Name", plan.ConstructorBindings[0].Column.Name);
        Assert.AreEqual(typeof(string), plan.ConstructorBindings[0].TargetType);
        Assert.AreEqual("Age", plan.ConstructorBindings[1].Column.Name);
        Assert.AreEqual(typeof(int?), plan.ConstructorBindings[1].TargetType);
        Assert.AreEqual(0, plan.MemberBindings.Count);
    }

    [TestMethod]
    public void Create_WhenPropertiesMatch_ShouldBindProperties()
    {
        var plan = CreatePlan<PropertyOutput>(
            new TypedOutputColumn("Source.Name", 0, typeof(string)),
            new TypedOutputColumn("Age", 1, typeof(int)));

        Assert.IsNull(plan.Constructor);
        Assert.AreEqual(2, plan.MemberBindings.Count);
        Assert.AreEqual("Name", plan.MemberBindings[0].MemberName);
        Assert.AreEqual(typeof(string), plan.MemberBindings[0].TargetType);
        Assert.AreEqual("Age", plan.MemberBindings[1].MemberName);
        Assert.AreEqual(typeof(int?), plan.MemberBindings[1].TargetType);
    }

    [TestMethod]
    public void Create_WhenFieldsMatch_ShouldBindFields()
    {
        var plan = CreatePlan<FieldOutput>(
            new TypedOutputColumn("Name", 0, typeof(string)),
            new TypedOutputColumn("Age", 1, typeof(int)));

        Assert.IsNull(plan.Constructor);
        Assert.AreEqual(2, plan.MemberBindings.Count);
        Assert.AreEqual("Name", plan.MemberBindings[0].MemberName);
        Assert.AreEqual("Age", plan.MemberBindings[1].MemberName);
    }

    [TestMethod]
    public void Create_WhenAliasesNormalizeToDuplicate_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePlan<PropertyOutput>(
                new TypedOutputColumn("A.Name", 0, typeof(string)),
                new TypedOutputColumn("B.Name", 1, typeof(string))));

        StringAssert.Contains(exception.Message, "duplicate output alias 'Name'");
    }

    [TestMethod]
    public void Create_WhenMemberIsMissing_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePlan<MissingMemberOutput>(new TypedOutputColumn("Name", 0, typeof(string))));

        StringAssert.Contains(exception.Message, "does not expose writable member 'Name'");
    }

    [TestMethod]
    public void Create_WhenMemberTypeIsIncompatible_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePlan<IncompatibleMemberOutput>(new TypedOutputColumn("Name", 0, typeof(string))));

        StringAssert.Contains(exception.Message, "expects 'System.Int32'");
        StringAssert.Contains(exception.Message, "has type 'System.String'");
    }

    [TestMethod]
    public void Create_WhenEnumValuedMemberTargetsIntegralCarrier_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePlan<EnumMemberOutput>(new TypedOutputColumn("Status", 0, typeof(short))));

        StringAssert.Contains(exception.Message, typeof(TypedOutputStatus).FullName!);
        StringAssert.Contains(exception.Message, typeof(short).FullName!);
    }

    [TestMethod]
    public void Create_WhenMemberNameIsAmbiguous_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePlan<AmbiguousMemberOutput>(new TypedOutputColumn("Name", 0, typeof(string))));

        StringAssert.Contains(exception.Message, "is ambiguous");
    }

    [TestMethod]
    public void Create_WhenConstructorsAreAmbiguous_ShouldReject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePlan<AmbiguousConstructorOutput>(new TypedOutputColumn("Name", 0, typeof(string))));

        StringAssert.Contains(exception.Message, "multiple public constructors");
    }

    [TestMethod]
    public void CreateOutputExpression_WhenMemberNameIsKeyword_ShouldEscapeInitializerTarget()
    {
        var binding = TypedOutputBinding.Create(
            typeof(KeywordMemberOutput),
            [new ExecutionColumnMetadataField("class", 0, typeof(string))]);

        var expression = binding.CreateOutputExpression("row").NormalizeWhitespace().ToFullString();

        Assert.Contains("new Musoq.Evaluator.Tests.TypedOutputBinderTests.KeywordMemberOutput", expression);
        Assert.Contains("@class = (string)row[0]", expression);
    }

    private static TypedOutputBindingPlan CreatePlan<TOut>(params TypedOutputColumn[] columns)
    {
        return TypedOutputBinder.Create(typeof(TOut), columns);
    }

    private sealed class ConstructorOutput(string Name, int? Age)
    {
        public string Name { get; } = Name;

        public int? Age { get; } = Age;
    }

    private sealed class PropertyOutput
    {
        public string Name { get; set; } = string.Empty;

        public int? Age { get; set; }
    }

    private sealed class FieldOutput
    {
        public string Name = string.Empty;

        // ReSharper disable once NotAccessedField.Compiler - TypedOutputBinder discovers public fields through reflection.
        public int? Age = 0;
    }

    private sealed class MissingMemberOutput
    {
        public string Other { get; set; } = string.Empty;
    }

    private sealed class IncompatibleMemberOutput
    {
        public int Name { get; set; }
    }

    private sealed class EnumMemberOutput
    {
        public TypedOutputStatus Status { get; set; }
    }

    private enum TypedOutputStatus : short
    {
        Queued = 10
    }

    private sealed class AmbiguousMemberOutput
    {
        public string Name { get; set; } = string.Empty;

        public string NAME = string.Empty;
    }

    private sealed class AmbiguousConstructorOutput
    {
        public AmbiguousConstructorOutput(string Name)
        {
            _ = Name;
        }

        public AmbiguousConstructorOutput(object Name)
        {
            _ = Name;
        }
    }

    private sealed class KeywordMemberOutput
    {
        public string @class { get; set; } = string.Empty;
    }
}
