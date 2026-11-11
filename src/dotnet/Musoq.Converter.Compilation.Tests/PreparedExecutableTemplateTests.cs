using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Architecture;

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
            "contract"
        );

        Assert.AreSame(artifact, template.ExecutableArtifact);
        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, template.TargetId);
        Assert.AreEqual("TestOnly.Runnable", template.RunnableTypeName);
        Assert.AreEqual("contract", template.SemanticContractFingerprint);
        Assert.IsFalse(
            CompilerContractSymbols
                .For(typeof(PreparedExecutableTemplate))
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(static property =>
                    SymbolEqualityComparer.Default.Equals(
                        property.PropertyType,
                        CompilerContractSymbols.For(typeof(QueryRuntimeBinding))
                    )
                )
        );
        Assert.IsFalse(
            CompilerContractSymbols
                .For(typeof(PreparedExecutableTemplate))
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(static field =>
                    SymbolEqualityComparer.Default.Equals(
                        field.FieldType,
                        CompilerContractSymbols.For(typeof(QueryRuntimeBinding))
                    )
                )
        );
    }

    [TestMethod]
    public void Constructor_WhenTargetDoesNotMatchArtifact_ShouldReject()
    {
        var artifact = new TestOnlyExecutableQueryArtifact("template");

        Assert.Throws<InvalidOperationException>(() =>
            new PreparedExecutableTemplate(artifact, ExecutionTargetIds.CSharpClr, "TestOnly.Runnable", "contract")
        );
    }

    private sealed record TestOnlyExecutableQueryArtifact(string Payload)
        : ExecutableQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);
}
