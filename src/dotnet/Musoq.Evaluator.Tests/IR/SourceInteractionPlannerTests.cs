using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using ParserSchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using PlanningContext = Musoq.Evaluator.IR.Planning.PlanningContext;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class SourceInteractionPlannerTests
{
    [TestMethod]
    public void Plan_WhenSourceShapeCannotBeResolved_ShouldClassifyShapeAsUnknown()
    {
        var sourceContextId = "t:0";
        var scan = CreateScan("t", sourceContextId);
        var sourceNode = CreateSourceNode("t", 0);
        var context = CreatePlanningContext(scan, sourceNode);
        var sources = CreateSourceProperties(scan);

        var result = SourceInteractionPlanner.Plan(context, [scan], sources, new Dictionary<string, SourcePredicatePlan>());

        var plan = result.PlansBySourceId[sourceContextId];
        Assert.AreEqual(SourceShapeKind.Unknown, plan.ShapeKind);
        Assert.Contains("unavailable", plan.ShapeReason);
        Assert.IsTrue(result.Decisions.Any(static decision => decision.Category == PlanningDecisionCategory.SourceInteraction));
    }

    [TestMethod]
    public void Plan_WhenSourceArgumentReferencesOuterAlias_ShouldClassifyArgumentAsCorrelated()
    {
        var sourceContextId = "r:1";
        var argument = new ColumnRef("i", "Name", typeof(string));
        var scan = CreateScan("r", sourceContextId, argument);
        var sourceNode = CreateSourceNode("r", 1);
        var context = CreatePlanningContext(scan, sourceNode);
        var sources = CreateSourceProperties(scan);

        var result = SourceInteractionPlanner.Plan(context, [scan], sources, new Dictionary<string, SourcePredicatePlan>());

        var plan = result.PlansBySourceId[sourceContextId];
        Assert.AreEqual(SourceArgumentMode.CorrelatedArguments, plan.ArgumentMode);
        Assert.Contains("outer alias", plan.ArgumentReason);
    }

    private static PlanningContext CreatePlanningContext(SchemaScanNode scan, ParserSchemaFromNode sourceNode)
    {
        ISchemaColumn[] usedColumns = [new SchemaColumn("Name", 0, typeof(string))];

        return new PlanningContext(
            new LogicalPlanningArtifacts(scan, scan, new OptimizationTrace()),
            new CompilationOptions(),
            new ThrowingSchemaProvider(),
            new Dictionary<ParserSchemaFromNode, ISchemaColumn[]>
            {
                [sourceNode] = usedColumns
            },
            new Dictionary<ParserSchemaFromNode, WhereNode>(),
            new Dictionary<ParserSchemaFromNode, SourcePlanRequest>
            {
                [sourceNode] = SourcePlanRequest.Empty(SourceIdentityFactory.Create(sourceNode))
            },
            new Dictionary<string, ISchemaColumn[]>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
            null,
            null,
            null);
    }

    private static Dictionary<string, SourcePlanProperties> CreateSourceProperties(SchemaScanNode scan)
    {
        return new Dictionary<string, SourcePlanProperties>(StringComparer.Ordinal)
        {
            [scan.SourceContextId!] = new(
                scan.SourceContextId!,
                scan.Alias,
                scan.SchemaName,
                scan.MethodName,
                ["Name"],
                [],
                [],
                [],
                PlanningConfidence.Low,
                "Source entity type could not be resolved from metadata.")
        };
    }

    private static Parser.SchemaFromNode CreateSourceNode(string alias, int sourcePosition)
    {
        return new Parser.SchemaFromNode(
            "test",
            "data",
            ArgsListNode.Empty,
            alias,
            sourcePosition,
            false);
    }

    private static SchemaScanNode CreateScan(string alias, string sourceContextId, params IrExpression[] arguments)
    {
        return new SchemaScanNode(
            "test",
            "data",
            arguments,
            alias,
            CreateSchema(("Name", typeof(string))),
            sourceContextId);
    }

    private static OutputSchema CreateSchema(params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var index = 0; index < columns.Length; index++)
            schemaColumns[index] = new ColumnSchema(columns[index].Name, columns[index].Type, index);

        return new OutputSchema(schemaColumns);
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new NotSupportedException("Schema access is not expected in this planner unit test.");
        }
    }
}
