using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SqlConcatenationGeneratedCodeTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(8)]
    [DataRow(16)]
    public void RepeatedChains_ShouldUseOneTypedFinalAllocationAndReuseTheResult(int arity)
    {
        var expression = string.Join(" || ", Enumerable.Repeat("e.Name", arity));
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [new BasicEntity("x")] };
        var inspection = InstanceCreator.CompileForInspection(
            $"select {expression} as A, {expression} as B from #A.Entities() e", Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources), LoggerResolver,
            new CompilationOptions(ParallelizationMode.None));
        var syntax = CSharpSyntaxTree.ParseText(inspection.GeneratedCSharpCode).GetRoot();
        var compute = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText.StartsWith("ComputeShapeRows_", StringComparison.Ordinal));
        var concatenations = compute.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() is "global::System.String.Concat" or "global::System.String.Create")
            .ToArray();

        var rowConstructors = compute.DescendantNodes().OfType<YieldStatementSyntax>()
            .Select(statement => statement.Expression).OfType<ObjectCreationExpressionSyntax>().ToArray();
        Assert.IsNotEmpty(rowConstructors);
        Assert.HasCount(rowConstructors.Length, concatenations, compute.ToString());
        Assert.HasCount(concatenations.Length, concatenations.Select(call => call.Ancestors().OfType<BlockSyntax>().First()).Distinct().ToArray());
        Assert.IsTrue(rowConstructors.All(row => row.ArgumentList!.Arguments.Count == 2 &&
            SyntaxFactory.AreEquivalent(row.ArgumentList.Arguments[0].Expression, row.ArgumentList.Arguments[1].Expression)),
            "Repeated stable chains must share their computed result in every native traversal path.");
        Assert.IsEmpty(compute.DescendantNodes().OfType<ArrayCreationExpressionSyntax>().ToArray());
        Assert.IsEmpty(compute.DescendantNodes().OfType<LambdaExpressionSyntax>().ToArray());
        Assert.IsEmpty(compute.DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().ToArray());
        if (arity <= 4)
        {
            Assert.AreEqual("global::System.String.Concat", concatenations[0].Expression.ToString());
            Assert.HasCount(arity, concatenations[0].ArgumentList.Arguments);
            return;
        }
        Assert.AreEqual("global::System.String.Create", concatenations[0].Expression.ToString());
        Assert.IsInstanceOfType<TupleExpressionSyntax>(concatenations[0].ArgumentList.Arguments[1].Expression);
        StringAssert.Contains(inspection.GeneratedCSharpCode, "ModuleInitializer");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "static __MusoqQueryConcat()");
    }
}
