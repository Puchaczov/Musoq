using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.IR;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SemanticVisitorRefactorContractTests : BasicEntityTestBase
{
    [TestMethod]
    public void SourceBinding_WhenUnknownProjectionColumn_ShouldKeepBindDiagnostic()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileBasic("select MissingColumn from #A.Entities()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "MissingColumn");
    }

    [TestMethod]
    public void SetOperator_WhenKeyTargetsAliasedField_ShouldRecordFieldPositionAndType()
    {
        var visitor = AnalyzeVisitor(
            "select Name as Label, City as CityKey from #A.Entities() " +
            "union (CityKey) " +
            "select Name as Label, City as CityKey from #A.Entities()");

        CollectionAssert.AreEqual(new[] { 1 }, visitor.SetOperatorFieldPositions.Values.Single());
        CollectionAssert.AreEqual(new[] { typeof(string) }, visitor.SetOperatorFieldTypes.Values.Single());
    }

    [TestMethod]
    public void SetOperator_WhenKeysAreOmitted_ShouldRecordAllFieldPositionsAndTypes()
    {
        var visitor = AnalyzeVisitor(
            "select Name as Label, City as CityKey from #A.Entities() " +
            "union " +
            "select Name as Label, City as CityKey from #A.Entities()");

        CollectionAssert.AreEqual(new[] { 0, 1 }, visitor.SetOperatorFieldPositions.Values.Single());
        CollectionAssert.AreEqual(new[] { typeof(string), typeof(string) }, visitor.SetOperatorFieldTypes.Values.Single());
    }

    [TestMethod]
    public void WindowMetadata_WhenRankingAndOffsetFunctionsAreBound_ShouldPreserveReturnTypesAndSpecification()
    {
        var visitor = AnalyzeVisitor(
            "select RowNumber() over (partition by City order by Name) as RowNum, " +
            "Lag(Population) over (order by Name) as PrevPopulation " +
            "from #A.Entities()");

        var query = GetQuery(visitor);
        var rowNumber = (WindowFunctionNode)query.Select.Fields[0].Expression;
        var lag = (WindowFunctionNode)query.Select.Fields[1].Expression;

        Assert.AreEqual(typeof(long), rowNumber.ReturnType);
        Assert.IsNotNull(rowNumber.WindowSpecification);
        Assert.HasCount(1, rowNumber.WindowSpecification.PartitionFields);
        Assert.HasCount(1, rowNumber.WindowSpecification.OrderByFields);

        Assert.AreEqual(typeof(decimal?), lag.ReturnType);
        Assert.IsNotNull(lag.WindowSpecification);
        Assert.HasCount(0, lag.WindowSpecification.PartitionFields);
        Assert.HasCount(1, lag.WindowSpecification.OrderByFields);
    }

    [TestMethod]
    public void AliasResolution_WhenCteColumnAliasIsQualifiedOutside_ShouldPreserveResultShape()
    {
        var vm = CompileBasic(
            "with p as (select Name as PersonName from #A.Entities()) select p.PersonName from p",
            new BasicEntity("Alice"));

        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "p.PersonName", typeof(string));
        Assert.AreEqual("Alice", table[0][0]);
    }

    [TestMethod]
    public void InterpretationSchemaReference_WhenNestedBinarySchemaFieldIsProjected_ShouldPreserveColumnPath()
    {
        const string query = @"
            binary Inner {
                Value: short le
            };
            binary Packet {
                Header: Inner,
                Tail: byte
            };
            select
                p.Header.Value,
                p.Tail
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "packet.bin", Content = [0x01, 0x00, 0xFF] }]
                }),
            LoggerResolver);

        var logicalProject = (ProjectNode)PipelinePlanAssertions.UnwrapMultiStatement(buildItems.RequireLogicalPlan());
        Assert.AreEqual("p.Header.Value", IrExpressionPrinter.Print(logicalProject.Fields[0].Expression));
        Assert.AreEqual("p.Tail", IrExpressionPrinter.Print(logicalProject.Fields[1].Expression));

        var physicalProject = (PhysicalProjectNode)PipelinePlanAssertions.UnwrapPhysicalMultiStatement(buildItems.RequirePhysicalPlan());
        Assert.AreEqual("p.Header.Value", IrExpressionPrinter.Print(physicalProject.Fields[0].Expression));
        Assert.AreEqual("p.Tail", IrExpressionPrinter.Print(physicalProject.Fields[1].Expression));
    }

    private CompiledQuery CompileBasic(string query, params BasicEntity[] entities)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            CreateBasicSchemaProvider(entities),
            LoggerResolver,
            TestCompilationOptions);
    }

    private static BuildMetadataAndInferTypesVisitor AnalyzeVisitor(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var tree = parser.ComposeAll();
        var logger = new Mock<ILogger<BuildMetadataAndInferTypesVisitor>>();

        var visitor = new BuildMetadataAndInferTypesVisitor(
            CreateBasicSchemaProvider(),
            new Dictionary<string, string[]>(),
            logger.Object);

        var traverser = new BuildMetadataAndInferTypesTraverseVisitor(visitor);
        tree.Accept(traverser);

        return visitor;
    }

    private static QueryNode GetQuery(BuildMetadataAndInferTypesVisitor visitor)
    {
        return visitor.Root.Expression switch
        {
            QueryNode query => query,
            StatementsArrayNode { Statements: [{ Node: QueryNode query }] } => query,
            var node => throw new InvalidOperationException($"Expected query root, got {node.GetType().Name}.")
        };
    }

    private static BasicSchemaProvider<BasicEntity> CreateBasicSchemaProvider(params BasicEntity[] entities)
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = entities
            });
    }
}
