using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Runtime;

namespace Musoq.Converter.Tests;

[TestClass]
public class IrCutoverDefaultsTests
{
    [TestMethod]
    public void Build_WhenUsingDefaultCompilationOptions_ShouldUseExecutionIrPath()
    {
        var items = CreateBuildItems();

        Build(items);

        Assert.IsNotNull(items.TransformedQueryTree);
        Assert.IsNotNull(items.Compilation);
        Assert.IsTrue(items.AccessToClassPath.EndsWith(".CompiledQuery", StringComparison.Ordinal));
    }

    private static BuildItems CreateBuildItems()
    {
        return new BuildItems
        {
            SchemaProvider = new SystemSchemaProvider(),
            RawQuery = "select 1 from #system.dual()",
            AssemblyName = Guid.NewGuid().ToString(),
            CreateBuildMetadataAndInferTypesVisitor = null,
            DiagnosticContext = new DiagnosticContext()
        };
    }

    private static void Build(BuildItems items)
    {
        RuntimeLibraries.CreateReferences();

        var loggerResolver = new TestsLoggerResolver();
        var chain = new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null),
                    loggerResolver)));

        chain.Build(items);
    }
}
