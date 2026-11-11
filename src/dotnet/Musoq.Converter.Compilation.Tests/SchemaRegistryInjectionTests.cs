using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;

namespace Musoq.Converter.Tests;

[TestClass]
public class SchemaRegistryInjectionTests
{
    [TestMethod]
    public void TransformTree_WhenUsingCustomVisitorFactory_ShouldInjectSchemaRegistryWithoutReflection()
    {
        BuildMetadataAndInferTypesVisitor? capturedVisitor = null;
        var expectedRegistry = new SchemaRegistry();
        var loggerResolver = new TestsLoggerResolver();

        var compiled = InstanceCreator.CompileForExecution(
            "select 1 from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            loggerResolver,
            () => new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null),
                    loggerResolver)),
            items =>
            {
                items.SchemaRegistry = expectedRegistry;
                items.CreateBuildMetadataAndInferTypesVisitor =
                    (provider, columns, compilationOptions, schemaRegistry, logger) =>
                    {
                        capturedVisitor = new BuildMetadataAndInferTypesVisitor(
                            provider,
                            columns,
                            logger,
                            compilationOptions,
                            schemaRegistry);

                        return capturedVisitor;
                    };
            });

        var result = compiled.Run();

        Assert.IsNotNull(result);
        capturedVisitor = capturedVisitor ?? throw new AssertFailedException("Expected visitor to be captured.");
        Assert.AreSame(expectedRegistry, capturedVisitor.SchemaRegistry);
    }
}
