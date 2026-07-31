using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class PreparedExecutableTemplateTests
{
    [TestMethod]
    public void Constructor_ShouldExposeOnlyImmutableExecutableIdentity()
    {
        var artifact = new TestOnlyExecutableQueryArtifact("template");
        var template = new PreparedExecutableTemplate(
            artifact,
            TestExecutionTargetIds.TestOnlyNonClr,
            "TestOnly.Runnable",
            "contract");

        Assert.AreSame(artifact, template.ExecutableArtifact);
        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, template.TargetId);
        Assert.AreEqual("TestOnly.Runnable", template.RunnableTypeName);
        Assert.AreEqual("contract", template.SemanticContractFingerprint);
        Assert.IsFalse(typeof(PreparedExecutableTemplate).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(static property => property.PropertyType == typeof(QueryRuntimeBinding)));
        Assert.IsFalse(typeof(PreparedExecutableTemplate).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(static field => field.FieldType == typeof(QueryRuntimeBinding)));
    }

    [TestMethod]
    public void Constructor_WhenTargetDoesNotMatchArtifact_ShouldReject()
    {
        var artifact = new TestOnlyExecutableQueryArtifact("template");

        Assert.Throws<InvalidOperationException>(() => new PreparedExecutableTemplate(
            artifact,
            ExecutionTargetIds.CSharpClr,
            "TestOnly.Runnable",
            "contract"));
    }

    private sealed record TestOnlyExecutableQueryArtifact(string Payload)
        : ExecutableQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);
}
