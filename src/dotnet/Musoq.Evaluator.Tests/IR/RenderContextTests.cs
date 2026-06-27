using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RenderContextTests : IDisposable
{
    private AdhocWorkspace? _workspace;
    private SyntaxGenerator _generator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _workspace = new AdhocWorkspace();
        _generator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);
    }

    [TestCleanup]
    public void Cleanup()
    {
        DisposeWorkspace();
    }

    public void Dispose()
    {
        DisposeWorkspace();
        GC.SuppressFinalize(this);
    }

    private void DisposeWorkspace()
    {
        _workspace?.Dispose();
        _workspace = null;
    }

    [TestMethod]
    public void WhenConstructedWithoutOptions_ShouldInitializeRuntimeV2Infrastructure()
    {
        var context = new RenderContext(_generator);

        Assert.AreSame(_generator, context.Generator);
        Assert.IsEmpty(context.ClassMembers);
        Assert.AreEqual(string.Empty, context.AssemblyName);
        Assert.IsNull(context.Scope);
        Assert.AreEqual(QueryResultMode.Table, context.ResultMode);
        Assert.AreEqual(FinalResultSinkKind.TableDirect, context.FinalResultSinkKind);
        Assert.IsNull(context.OutputType);
    }

    [TestMethod]
    public void WhenConstructedWithOptions_ShouldExposeRuntimeV2Metadata()
    {
        var scope = new Scope(null, 0, "query") { [MetaAttributes.MethodName] = "ComputeTable_scope_1" };

        var context = new RenderContext(
            _generator,
            new RenderContextOptions(
                Scope: scope,
                AssemblyName: "Query.Compiled_Test",
                ResultMode: QueryResultMode.TypedEnumerable,
                OutputType: typeof(string),
                FinalResultSinkKind: FinalResultSinkKind.TypedSerialEnumerable));

        Assert.AreSame(scope, context.Scope);
        Assert.AreEqual("Query.Compiled_Test", context.AssemblyName);
        Assert.AreEqual(QueryResultMode.TypedEnumerable, context.ResultMode);
        Assert.AreEqual(typeof(string), context.OutputType);
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, context.FinalResultSinkKind);
    }
}
