using System;
using System.Collections.Generic;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests.IR;

internal static class PlanOnlyBuildItems
{
    public static BuildItems Create(string script)
    {
        var items = new BuildItems
        {
            RawQuery = script,
            AssemblyName = Guid.NewGuid().ToString(),
            SchemaProvider = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [] },
                { "#B", [] }
            }),
            DiagnosticContext = new DiagnosticContext(new SourceText(script)),
            CreateBuildMetadataAndInferTypesVisitor = null
        };
        items.StopAfterPlanning = true;

        var chain = new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TerminalBuildChain(),
                    new TestsLoggerResolver())));
        chain.Build(items);

        return items;
    }
}
