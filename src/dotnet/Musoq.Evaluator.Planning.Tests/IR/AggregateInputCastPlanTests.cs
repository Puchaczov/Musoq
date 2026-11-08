using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AggregateInputCastPlanTests : BasicEntityTestBase
{
    [TestMethod]
    public void CoalescedAggregateInput_ShouldCastTheWholeOperandToNullable()
    {
        const string query = "select Sum(v.Score ?? 0) as Total from values { (Score: null), (Score: 2) } v";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        var casts = CSharpSyntaxTree.ParseText(inspection.GeneratedCSharpCode).GetRoot()
            .DescendantNodes().OfType<CastExpressionSyntax>();

        Assert.IsTrue(casts.Any(cast => cast.Type is NullableTypeSyntax &&
            cast.Expression is ParenthesizedExpressionSyntax { Expression: BinaryExpressionSyntax operand } &&
            operand.IsKind(SyntaxKind.CoalesceExpression)));
    }
}
