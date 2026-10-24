using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal IEnumerable<MemberDeclarationSyntax> RenderSubqueryMembers(ExecutionPlan owner)
    {
        var subqueries = ExecutionSubqueryPlans.SelfAndDescendants(owner)
            .SelectMany(static plan => ExecutionIrAnalysis.FlattenExpressions(plan.Body))
            .OfType<ExecutionSubquery>().DistinctBy(static query => query.Identifier);
        foreach (var query in subqueries)
        {
            var final = query.Plan.FinalResult!;
            var nativeRows = HasNativeSubqueryRows(query.Plan);
            var shapeName = FinalSelectShapeNaming.CreateTypeName(final.Shape);
            IReadOnlyList<MemberDeclarationSyntax> members;
            if (nativeRows)
                members = RenderClassMembers(query.Plan, final.TableName, shapeName, final.Shape.Fields);
            else
                members = RenderClassMembers(query.Plan);
            var names = new SubqueryMemberNames(query.Identifier, members);
            foreach (var member in members)
                yield return (MemberDeclarationSyntax)names.Visit(member)!;
            var context = InitializeRenderContext(query.Plan);
            context.Session.IsSubqueryHelper = true;
            MethodDeclarationSyntax method;
            if (nativeRows)
                method = RenderFinalShapeRowsMethod(query.Plan, SubqueryMethodName(query.Identifier), query.Identifier,
                    final.TableName, shapeName, final.Shape.Fields, isSubqueryHelper: true);
            else
                method = RenderMethod(query.Plan, SubqueryMethodName(query.Identifier), query.Identifier, context);
            method = method.WithModifiers(CreatePrivateStaticModifiers())
                .WithParameterList(CreateRuntimeHelperParameterList(SubqueryScriptCaptures(query.Plan), SubqueryRuntimeContext()));
            var parameters = query.Bindings.Select((binding, index) =>
                CreateParameter($"__outer{index}", CreateTypeSyntax(binding.ReturnType))).ToArray();
            yield return (MethodDeclarationSyntax)names.Visit(method.AddParameterListParameters(parameters)
                .AddParameterListParameters(query.CteBindings.Select(binding => CreateParameter(binding.ParameterName,
                    SyntaxFactory.ParseTypeName($"IReadOnlyList<{binding.Shape.TypeName}>"))).ToArray()))!;
        }
    }

    private ExpressionSyntax RenderSubquery(ExecutionSubquery query, ExecutionRenderContext context)
    {
        var final = query.Plan.FinalResult ?? throw new InvalidOperationException("Subquery has no final result shape.");
        if (!TableViaRowsResultInfoResolver.TryResolveRenderPlan(query.Plan, out var output))
            throw new NotSupportedException("Subquery result does not expose a native row result.");
        var nativeRows = HasNativeSubqueryRows(query.Plan);
        var rowType = "global::Musoq.Evaluator.Tables.Row";
        if (nativeRows)
        {
            if (output.FinalSinkPlans.TableOutput.UsesGeneratedRow)
                rowType = output.FinalSinkPlans.TableOutput.Result.RowTypeName;
            else
                rowType = FinalSelectShapeNaming.CreateTypeName(final.Shape);
        }
        var arguments = new List<ExpressionSyntax>(CreateRuntimeHelperArguments(SubqueryRuntimeContext()));
        arguments.AddRange(SubqueryScriptCaptures(query.Plan).Select(CreateCapturedLocalArgument));
        arguments.AddRange(query.Bindings.Select(binding => RenderExpression(binding, context)));
        arguments.AddRange(query.CapturedCteValues.Select(value => RenderExpression(value, context)));
        var table = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SubqueryMethodName(query.Identifier)))
            .WithArgumentList(CreateArgumentList(arguments));
        ExpressionSyntax rows = table;
        if (!nativeRows)
            rows = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression(
                    "global::Musoq.Evaluator.Runtime.SubqueryExecution.Rows"))
                .WithArgumentList(CreateArgumentList(table));
        var typeArguments = new List<string> { rowType };
        var callArguments = new List<ExpressionSyntax>();
        if (query.Kind is SubqueryResultKind.In or SubqueryResultKind.Any or SubqueryResultKind.All)
        {
            var operand = query.Operand ?? throw new InvalidOperationException("Comparison subquery has no operand.");
            typeArguments.Add(CreateTypeSyntax(operand.ReturnType).ToString());
            callArguments.Add(RenderExpression(operand, context));
        }
        callArguments.Add(rows);
        if (query.Kind != SubqueryResultKind.Exists)
        {
            var valueType = query.Kind == SubqueryResultKind.Scalar ? query.ReturnType : final.Shape.Fields[0].Type;
            typeArguments.Add(CreateTypeSyntax(valueType).ToString());
            var field = final.Shape.Fields[0];
            var read = $"row.{(field.AccessStrategy as GeneratedFieldAccess)?.FieldName ?? field.Name}";
            if (!nativeRows)
                read = $"row.GetValue<{CreateTypeSyntax(field.Type)}>(0)";
            callArguments.Add(SyntaxFactory.ParseExpression($"static row => ({CreateTypeSyntax(valueType)}){read}"));
            if (query.Kind is SubqueryResultKind.In or SubqueryResultKind.Any or SubqueryResultKind.All)
                callArguments.Add(RenderSubqueryComparison(query, valueType, context));
        }
        callArguments.Add(SyntaxFactory.IdentifierName("token"));
        return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression(
                $"global::Musoq.Evaluator.Runtime.SubqueryExecution.{query.Kind}<{string.Join(", ", typeArguments)}>"))
            .WithArgumentList(CreateArgumentList(callArguments));
    }

    private ExpressionSyntax RenderSubqueryComparison(ExecutionSubquery query, ExecutionTypeRef rightType, ExecutionRenderContext context)
    {
        var comparison = new ExecutionBinary(query.Comparison,
            new ExecutionVariableRead(new ExecutionVariable("left", query.Operand!.ReturnType)),
            new ExecutionVariableRead(new ExecutionVariable("right", rightType)), typeof(bool?)) { UsesSqlNullSemantics = true };
        var body = RenderBinary(comparison, context);
        return SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("left")),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("right"))
            })), body).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
    }

    private static string SubqueryMethodName(string identifier) => $"__Execute{identifier}";

    private static bool HasNativeSubqueryRows(ExecutionPlan plan) =>
        TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var output) && output.FinalSinkPlans.TableOutput.IsSupported;

    private ExecutionRenderContext SubqueryRuntimeContext()
    {
        var context = CreateIsolatedRenderContext();
        context.Session.IncludeTableResults = false;
        context.Session.IncludeCteRowResults = false;
        context.Session.IncludeCteIndexResults = false;
        return context;
    }

    private CapturedLocal[] SubqueryScriptCaptures(ExecutionPlan plan) => ExecutionSubqueryPlans.SelfAndDescendants(plan)
        .SelectMany(static scope => ExecutionIrAnalysis.FlattenExpressions(scope.Body))
        .Select(expression => expression switch
        {
            ExecutionScriptParameterRead parameter => CreateCapturedLocal(parameter),
            ExecutionScriptVariableRead variable => CreateCapturedLocal(variable),
            _ => null
        }).OfType<CapturedLocal>().DistinctBy(static capture => capture.Name).ToArray();
}
