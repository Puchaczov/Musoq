using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void GeneratedCode_WhenCteIndexPlanIsRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(CreateCteIndexPlan(), cteIndexResultCount: 1);
    }

    [TestMethod]
    public void GeneratedCode_WhenHashAndKeySetPlansAreRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(CreateCompilableHashPayloadPlan());
        AssertGeneratedCodeCompiles(CreateKeySetPlan());
    }

    [TestMethod]
    public void GeneratedCode_WhenWindowPlanIsRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(CreateWindowRenderNodePlan());
    }

    [TestMethod]
    public void GeneratedCode_WhenFinalShapePlanIsRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(CreateProjectionPostOperationPlan());
    }

    [TestMethod]
    public void GeneratedCode_WhenStrictCastPlanIsRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(CreateStrictCastPlan());
    }

    [TestMethod]
    public void GeneratedCode_WhenSetOperationPlanIsRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(CreateSetOperationPlan());
    }

    [TestMethod]
    public void GeneratedCode_WhenProfiledPlanIsRendered_ShouldCompile()
    {
        AssertGeneratedCodeCompiles(
            CreateProjectionPostOperationPlan(),
            instrumentationMode: QueryInstrumentationMode.Full);
    }

    private static void AssertGeneratedCodeCompiles(
        ExecutionPlan plan,
        QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Disabled,
        int inMemoryTableCount = 0,
        int cteIndexResultCount = 0)
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_SemanticCompilation",
                InstrumentationMode: instrumentationMode));
        var renderer = new CSharpRenderer(context);
        const string queryIdentifier = "compiled";

        var outcome = renderer.TryRenderExecutionQueryMethod(plan, queryIdentifier);

        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        context.AddClassMember(outcome.Method!.Value.MethodDeclaration);
        GeneratedCodeCompilationAssert.Succeeds(renderer.RenderCompilationUnit(
            queryIdentifier,
            inMemoryTableCount,
            cteIndexResultCount));
    }

    private static ExecutionPlan CreateCteIndexPlan()
    {
        var resultShape = CreateSingleNameShape();
        var resultTable = new ExecutionVariable("result", typeof(object));
        var index = new ExecutionVariable("cteKeys", typeof(object));
        var loadedIndex = new ExecutionVariable("loadedCteKeys", typeof(object));

        return CreateFinalResultPlan(
            "Q_CteIndex",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateKeySet(index, typeof(int)),
                new ExecutionKeySetAdd(
                    index,
                    new ExecutionLiteral(1, typeof(int)),
                    ExecutionClrBindingFactory.FromClr(typeof(int)),
                    KeyVariableName: "cteBuildKey"),
                new ExecutionStoreCteIndex(index, 0, ExecutionCteSidecarIndexKind.KeySet, typeof(int)),
                new ExecutionLoadCteIndex(loadedIndex, 0, ExecutionCteSidecarIndexKind.KeySet, typeof(int)),
                new ExecutionKeySetProbe(
                    loadedIndex,
                    new ExecutionLiteral(1, typeof(int)),
                    ExecutionClrBindingFactory.FromClr(typeof(int)),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [new ExecutionRowValue("Name", new ExecutionLiteral("matched", typeof(string)))])
                    ]),
                    KeyVariableName: "cteProbeKey"),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateKeySetPlan()
    {
        var resultShape = CreateSingleNameShape();
        var resultTable = new ExecutionVariable("result", typeof(object));
        var keySet = new ExecutionVariable("keys", typeof(object));

        return CreateFinalResultPlan(
            "Q_KeySet",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateKeySet(keySet, typeof(int)),
                new ExecutionKeySetAdd(
                    keySet,
                    new ExecutionLiteral(10, typeof(int)),
                    ExecutionClrBindingFactory.FromClr(typeof(int)),
                    KeyVariableName: "buildKey"),
                new ExecutionKeySetProbe(
                    keySet,
                    new ExecutionLiteral(10, typeof(int)),
                    ExecutionClrBindingFactory.FromClr(typeof(int)),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [new ExecutionRowValue("Name", new ExecutionLiteral("hit", typeof(string)))])
                    ]),
                    KeyVariableName: "probeKey"),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateStrictCastPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding(
                    "Number",
                    "Number",
                    0,
                    typeof(int?),
                    FieldNullability.Unknown,
                    new GeneratedFieldAccess("Number"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var library = new ExecutionVariable("libraryBase0", typeof(LibraryBase));

        return CreateFinalResultPlan(
            "Q_StrictCast",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateObject(library),
                new ExecutionAppendRow(
                    resultTable,
                    resultShape,
                    [
                        new ExecutionRowValue(
                            "Number",
                            new ExecutionStrictCast(
                                new ExecutionLiteral("42", typeof(string)),
                                "Int32",
                                typeof(int?),
                                library))
                    ]),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateCompilableHashPayloadPlan()
    {
        var payloadShape = new HashPayloadShape(
            "DHashPayload0",
            [
                new FieldBinding("b.City", "b.City", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("b_City")),
                new FieldBinding("b.Country", "b.Country", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("b_Country"))
            ]);
        var resultShape = CreateSingleNameShape();
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("dHash", typeof(object));
        var b = new ExecutionVariable("b", typeof(BasicEntity));
        var d = new ExecutionVariable("d", typeof(object), payloadShape.TypeName);

        return CreateFinalResultPlan(
            "Q_CompilableHashPayload",
            [payloadShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(
                    hash,
                    ExecutionClrBindingFactory.FromClr(typeof(int)),
                    ExecutionClrBindingFactory.FromClr(typeof(object)),
                    GeneratedRowTypeName: payloadShape.TypeName),
                new ExecutionCreateObject(b),
                new ExecutionCreateHashPayload(
                    d,
                    payloadShape,
                    [
                        new ExecutionRowValue("b.City", new ExecutionFieldRead("b", "City", typeof(string))),
                        new ExecutionRowValue("b.Country", new ExecutionFieldRead("b", "Country", typeof(string)))
                    ]),
                new ExecutionHashAdd(
                    hash,
                    new ExecutionLiteral(1, typeof(int)),
                    d,
                    typeof(int),
                    typeof(object),
                    payloadShape.TypeName),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateSetOperationPlan()
    {
        var rowShape = CreateSingleNameShape();
        var left = new ExecutionVariable("left", typeof(object));
        var right = new ExecutionVariable("right", typeof(object));
        var result = new ExecutionVariable("result", typeof(object));

        return CreateFinalResultPlan(
            "Q_SetOperation",
            [rowShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(left, rowShape),
                new ExecutionAppendRow(
                    left,
                    rowShape,
                    [new ExecutionRowValue("Name", new ExecutionLiteral("left", typeof(string)))]),
                new ExecutionCreateTable(right, rowShape),
                new ExecutionAppendRow(
                    right,
                    rowShape,
                    [new ExecutionRowValue("Name", new ExecutionLiteral("right", typeof(string)))]),
                new ExecutionSetOperation(
                    result,
                    left,
                    right,
                    SetOpKind.UnionAll,
                    [0],
                    [typeof(string)],
                    ExecutionSetOperationStrategy.AppendLoop),
                new ExecutionReturnTable(result)
            ]),
            result,
            rowShape);
    }

    private static GeneratedRowShape CreateSingleNameShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding(
                    "Name",
                    "Name",
                    0,
                    typeof(string),
                    FieldNullability.Unknown,
                    new GeneratedFieldAccess("Name"))
            ]);
    }
}
