using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class ComputedSourceArgumentTests
{
    [TestMethod]
    [DataRow("$root + '/something.exe'", "C:/temp/something.exe")]
    [DataRow("$root || '/something.exe'", "C:/temp/something.exe")]
    [DataRow("$root::string", "C:/temp")]
    [DataRow("ToUpper($root)", "C:/TEMP")]
    [DataRow("ToUpper(ToLower($root))", "C:/TEMP")]
    [DataRow("case when $root is null then 'missing' else $root + '/file' end", "C:/temp/file")]
    public void ComputedFromArgument_ShouldReachTheProvider(string expression, string expected)
    {
        var provider = new CapturingProvider();
        using var query = Compile($"param(root: string = 'C:/temp') select e.Path from #args.echo({expression}) e", provider);
        Assert.IsEmpty(provider.Opened);
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
        Assert.AreEqual(expected, provider.Opened.Single().Path);
    }

    [TestMethod]
    [DataRow("param")]
    [DataRow("params")]
    public void DefaultAndOverrides_ShouldBeEvaluatedForEveryExecution(string declaration)
    {
        var provider = new CapturingProvider();
        using var query = Compile($"{declaration}(root: string = 'C:/temp') select e.Path from #args.echo($root + '/file') e", provider);
        using (var first = query.Run()) TableMaterializationTestHelper.AssertRowsInOrder(first, ["C:/temp/file"]);
        Set(query, "root", "D:/other");
        using (var second = query.Run()) TableMaterializationTestHelper.AssertRowsInOrder(second, ["D:/other/file"]);
        Set(query, "root", "E:/last");
        using (var third = query.Run()) TableMaterializationTestHelper.AssertRowsInOrder(third, ["E:/last/file"]);
        CollectionAssert.AreEqual(new[] { "C:/temp/file", "D:/other/file", "E:/last/file" }, provider.Opened.Select(r => r.Path).ToArray());
    }

    [TestMethod]
    public void RequiredComputedArgument_ShouldFailBeforeOpeningAnySource()
    {
        var provider = new CapturingProvider();
        using var query = Compile("param(root: string) select e.Path from #args.seed() s cross apply #args.echo($root + s.Path) e", provider);
        var failure = Assert.Throws<QueryExecutionException>(() => { using var result = query.Run(); _ = result.Count; });
        Assert.IsNotNull(failure.Envelope);
        Assert.AreEqual(DiagnosticCode.MQ7003_RequiredScriptParameterMissing, failure.Envelope.Code);
        Assert.IsEmpty(provider.Opened);
        Assert.AreEqual(0, provider.OpenCount);
        Set(query, "root", "root/");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["root/a"], ["root/b"]);
        CollectionAssert.AreEqual(new[] { "root/a", "root/b" }, provider.Opened.Select(r => r.Path).ToArray());
    }

    [TestMethod]
    public void NamedComputedArguments_ShouldPreserveCanonicalSlotsAndNumericTypes()
    {
        var provider = new CapturingProvider();
        using var query = Compile("param(root: string = 'root', n: int = 20, suffix: string = '.bin') " +
            "select e.Path, e.Suffix, e.Number from #args.echo(number: $n * 2 + 2, suffix: $suffix + '.x', path: $root + '/file') e", provider);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["root/file", ".bin.x", 42]);
        var opened = provider.Opened.Single();
        Assert.AreEqual("root/file", opened.Path);
        Assert.AreEqual(".bin.x", opened.Suffix);
        Assert.AreEqual(42, opened.Number);
    }

    [TestMethod]
    public void NullableComputedArgument_ShouldPreserveTypedNullAndOverrides()
    {
        var provider = new CapturingProvider();
        using var query = Compile("param(n: int? = null) select e.Number from #args.echo('path', '', $n + 1) e", provider);
        using (var first = query.Run()) TableMaterializationTestHelper.AssertRowsInOrder(first, new object?[] { null });
        Assert.IsNull(provider.Opened.Single().Number);
        Set(query, "n", 41);
        using var second = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(second, [42]);
        Assert.AreEqual(42, provider.Opened.Last().Number);
    }

    [TestMethod]
    public void VolatileArguments_ShouldRunOnceInOrderPerCorrelatedOpening()
    {
        var id = Guid.NewGuid().ToString("N");
        ArgumentLibrary.Start(id);
        try
        {
            var provider = new CapturingProvider { TraceId = id };
            using var query = Compile($"param(root: string = 'root/') select e.Path from #args.seed() s cross apply " +
                $"#args.echo(Touch('{id}', 'left', $root) + s.Path, Touch('{id}', 'right', '/suffix')) e", provider);
            Assert.IsEmpty(provider.Opened);
            using var table = query.Run();
            TableMaterializationTestHelper.AssertRowsInOrder(table, ["root/a"], ["root/b"]);
            CollectionAssert.AreEqual(new[] { "left", "right", "open", "left", "right", "open" }, ArgumentLibrary.End(id));
        }
        finally { ArgumentLibrary.End(id); }
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("with c as (select s.Path from #args.seed() s) ")]
    public void InitialFromScalarCall_ShouldBindItsLibraryBeforeOpeningTheSource(string prefix)
    {
        var provider = new CapturingProvider();
        using var query = Compile("param(root: string = 'root') " + prefix +
            "select e.Path from #args.echo(ToUpper($root)) e", provider);
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["ROOT"]);
        Assert.AreEqual("ROOT", provider.Opened.Single().Path);
    }

    [TestMethod]
    public void InitialFromVolatileCalls_ShouldRemainRuntimeOnlyAndCaseGuarded()
    {
        var id = Guid.NewGuid().ToString("N");
        ArgumentLibrary.Start(id);
        try
        {
            var provider = new CapturingProvider { TraceId = id };
            using var query = Compile($"param(root: string = 'root') select e.Path from #args.echo(" +
                $"case when $root = 'root' then Touch('{id}', 'selected', $root) else Touch('{id}', 'unselected', $root) end) e", provider);
            Assert.AreEqual(0, provider.OpenCount);
            using var result = query.Run();
            TableMaterializationTestHelper.AssertRowsInOrder(result, ["root"]);
            CollectionAssert.AreEqual(new[] { "selected", "open" }, ArgumentLibrary.End(id));
        }
        finally { ArgumentLibrary.End(id); }
    }

    [TestMethod]
    public void UnaliasedFromScalarCall_ShouldUseTheActualGeneratedSourceAlias()
    {
        var provider = new CapturingProvider();
        using var query = Compile("param(root: string = 'root') select Path from #args.echo(ToUpper($root))", provider);
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["ROOT"]);
        Assert.AreEqual("ROOT", provider.Opened.Single().Path);
    }

    [TestMethod]
    public void NamedVolatileArguments_ShouldPreserveExistingCanonicalEvaluationOrder()
    {
        var id = Guid.NewGuid().ToString("N");
        ArgumentLibrary.Start(id);
        try
        {
            var provider = new CapturingProvider { TraceId = id };
            using var query = Compile($"param(root: string = 'root') select e.Path, e.Suffix from #args.echo(" +
                $"suffix: Touch('{id}', 'right', '/suffix'), path: Touch('{id}', 'left', $root) + '/file') e", provider);
            using var result = query.Run();
            TableMaterializationTestHelper.AssertRowsInOrder(result, ["root/file", "/suffix"]);
            CollectionAssert.AreEqual(new[] { "left", "right", "open" }, ArgumentLibrary.End(id));
        }
        finally { ArgumentLibrary.End(id); }
    }
}
