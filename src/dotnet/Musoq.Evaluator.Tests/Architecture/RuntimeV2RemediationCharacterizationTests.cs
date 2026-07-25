using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>
/// Durable behavior and ownership contracts for the completed evaluator remediation.
/// </summary>
[TestClass]
public sealed class RuntimeV2RemediationCharacterizationTests : BasicEntityTestBase
{
    [TestMethod]
    public void PhysicalLowering_ShouldHaveOneFacadeOnly()
    {
        var assembly = typeof(PhysicalToExecutionPlanBuilder).Assembly;
        var builderTypes = assembly
            .GetTypes()
            .Where(static type => type.Name == nameof(PhysicalToExecutionPlanBuilder))
            .ToArray();

        Assert.HasCount(1, builderTypes);

        var builderMethods = builderTypes[0]
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(static method => !method.IsSpecialName)
            .ToArray();

        CollectionAssert.AreEqual(new[] { nameof(PhysicalToExecutionPlanBuilder.Build) }, builderMethods.Select(static method => method.Name).ToArray());

        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var builderFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "*.cs")
            .Where(file => File.ReadAllText(file).Contains("class PhysicalToExecutionPlanBuilder", StringComparison.Ordinal))
            .ToArray();

        Assert.HasCount(1, builderFiles);
        Assert.IsLessThanOrEqualTo(250, File.ReadAllLines(builderFiles[0]).Length);
    }

    [TestMethod]
    public void LoweringHandlers_ShouldUseTheDiscriminatedAttemptContract()
    {
        var handlerConstructor = typeof(PhysicalLoweringHandlers)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(static constructor => constructor.GetParameters().Length == 12);
        var handlerDelegates = handlerConstructor.GetParameters().Select(static parameter => parameter.ParameterType).ToArray();

        Assert.IsTrue(handlerDelegates.All(static type =>
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Func<,>) &&
            type.GetGenericArguments()[1].IsGenericType &&
            type.GetGenericArguments()[1].GetGenericTypeDefinition() == typeof(LoweringAttempt<>)));

        var attemptType = typeof(LoweringAttempt<>);
        Assert.IsNull(attemptType.GetProperty("Supported", BindingFlags.Instance | BindingFlags.Public));
        Assert.AreEqual(attemptType.GetGenericArguments()[0], attemptType.GetProperty(nameof(LoweringAttempt<object>.Value))!.PropertyType);
        Assert.IsFalse(typeof(PhysicalToExecutionPlanBuilder).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Any());
    }

    [TestMethod]
    public void QueryAnalyzer_AndCompiler_ShouldBothAcceptRepresentativeQuery()
    {
        const string query = "SELECT Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] };
        var analyzer = new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(sources));

        var analysis = analyzer.Analyze(query);
        var compiled = CreateAndRunVirtualMachine(query, sources);

        Assert.IsTrue(analysis.IsParsed);
        Assert.IsFalse(analysis.HasErrors, string.Join(Environment.NewLine, analysis.Errors));
        Assert.IsNotNull(compiled);
    }
}
