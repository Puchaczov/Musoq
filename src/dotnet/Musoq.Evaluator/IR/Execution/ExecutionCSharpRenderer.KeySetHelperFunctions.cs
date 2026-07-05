using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<MethodDeclarationSyntax> CreateKeySetHelperFunctions(
        KeySetHelperSet helperSet,
        ExecutionRenderContext context)
    {
        yield return CreateKeySetBuildFunction(helperSet.Build, context);
        yield return CreateKeySetProbeFunction(helperSet.Probe, context);
    }

    private MethodDeclarationSyntax CreateKeySetBuildFunction(
        KeySetBuildHelper helper,
        ExecutionRenderContext context)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName, helper.RawRowsShape);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateKeySetBuildParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private MethodDeclarationSyntax CreateKeySetProbeFunction(
        KeySetProbeHelper helper,
        ExecutionRenderContext context)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateKeySetProbeParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private ExpressionStatementSyntax CreateKeySetBuildInvocation(
        KeySetBuildHelper helper,
        ExecutionRenderContext context)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateKeySetBuildArguments(helper, context));
    }

    private ExpressionStatementSyntax CreateKeySetProbeInvocation(
        KeySetProbeHelper helper,
        ExecutionRenderContext context)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateKeySetProbeArguments(helper, context));
    }

    private TypeSyntax CreateKeySetBuildRowsParameterType(
        KeySetBuildHelper helper,
        ExecutionRenderContext context) =>
        helper.RawRowsShape == null
            ? CreateAggregateRowsParameterType(helper.Loop.Source, CreateVariableTypeSyntax(helper.Loop.Item))
            : CreateReadOnlyListTypeSyntax(
                helper.Loop.Source is ExecutionStoredTableRows storedRows &&
                TryGetTypedStoredTableResult(storedRows.TableIndex, helper.RawRowsShape, context, out _)
                    ? SyntaxFactory.ParseTypeName(helper.RawRowsShape.TypeName)
                    : CreateTypeSyntax(typeof(Row)));

    private ExpressionSyntax CreateKeySetBuildRowsArgument(
        KeySetBuildHelper helper,
        ExecutionRenderContext context)
    {
        return helper is { RawRowsShape: not null, Loop.Source: ExecutionStoredTableRows storedRows }
            ? CreateStoredTableRowsRead(storedRows, context)
            : RenderExpression(helper.Loop.Source, context);
    }

    private static string CreateKeySetBuildFunctionBaseName(ExecutionKeySetAdd keySetAdd)
    {
        return $"Build{CreatePascalIdentifier(keySetAdd.Set.Name)}";
    }

    private static string CreateKeySetProbeFunctionBaseName(ExecutionKeySetProbe keySetProbe)
    {
        return keySetProbe.NoMatchBody is { Nodes.Count: > 0 }
            ? "AppendLeftJoinRows"
            : "AppendHashJoinRows";
    }
}
