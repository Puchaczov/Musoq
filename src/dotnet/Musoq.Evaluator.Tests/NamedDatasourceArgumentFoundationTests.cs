using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Schema;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NamedDatasourceArgumentFoundationTests
{
    [TestMethod]
    public void SchemaSourceSignature_UsesReflectedOptionalDefault()
    {
        var constructor = typeof(OptionalSource).GetConstructor([typeof(string), typeof(int)])!;
        var metadata = new SchemaConstructorInfo(
            constructor,
            false,
            ("required", typeof(string)),
            ("optional", typeof(int)));

        var signature = SchemaSourceSignature.Create(new SchemaMethodInfo("items", metadata));

        Assert.HasCount(2, signature.Parameters);
        Assert.IsTrue(signature.Parameters[0].IsRequired);
        Assert.IsFalse(signature.Parameters[1].IsRequired);
        Assert.AreEqual(7, signature.Parameters[1].DefaultValue);
    }

    [TestMethod]
    public void SchemaSourceSignature_ExcludesInjectedExecutionContextBeforeMatchingDefaults()
    {
        var constructor = typeof(ContextSource).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var metadata = new SchemaConstructorInfo(
            constructor,
            true,
            ("value", typeof(string)));

        var signature = SchemaSourceSignature.Create(new SchemaMethodInfo("items", metadata));

        Assert.HasCount(1, signature.Parameters);
        Assert.AreEqual("value", signature.Parameters[0].Name);
        Assert.IsFalse(signature.Parameters[0].IsRequired);
        Assert.AreEqual("fallback", signature.Parameters[0].DefaultValue);
    }

    [TestMethod]
    public void SchemaSourceSignature_WithoutOriginConstructor_TreatsArgumentsAsRequired()
    {
        var signature = SchemaSourceSignature.Create(
            new SchemaMethodInfo(
                "items",
                new SchemaConstructorInfo(null, false, ("value", typeof(string)))));

        Assert.HasCount(1, signature.Parameters);
        Assert.IsTrue(signature.Parameters[0].IsRequired);
        Assert.IsNull(signature.Parameters[0].DefaultValue);
    }

    private sealed class OptionalSource
    {
        public OptionalSource(string required, int optional = 7)
        {
        }
    }

    private sealed class ContextSource
    {
        public ContextSource(SourceExecutionContext context, string value = "fallback")
        {
        }
    }
}
