using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<MethodDeclarationSyntax> CreateKeySetHelperFunctions(KeySetHelperSet helperSet)
    {
        yield return CreateKeySetBuildFunction(helperSet.Build);
        yield return CreateKeySetProbeFunction(helperSet.Probe);
    }

    private MethodDeclarationSyntax CreateKeySetBuildFunction(KeySetBuildHelper helper)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName, helper.RawRowsShape);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateKeySetBuildParameterList(helper))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private MethodDeclarationSyntax CreateKeySetProbeFunction(KeySetProbeHelper helper)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateKeySetProbeParameterList(helper))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private ExpressionStatementSyntax CreateKeySetBuildInvocation(KeySetBuildHelper helper)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateKeySetBuildArguments(helper));
    }

    private ExpressionStatementSyntax CreateKeySetProbeInvocation(KeySetProbeHelper helper)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateKeySetProbeArguments(helper));
    }

    private TypeSyntax CreateKeySetBuildRowsParameterType(KeySetBuildHelper helper) =>
        helper.RawRowsShape == null
            ? CreateAggregateRowsParameterType(helper.Loop.Source, CreateVariableTypeSyntax(helper.Loop.Item))
            : CreateReadOnlyListTypeSyntax(
                helper.Loop.Source is ExecutionStoredTableRows storedRows &&
                TryGetTypedStoredTableResult(storedRows.TableIndex, helper.RawRowsShape, out _)
                    ? SyntaxFactory.ParseTypeName(helper.RawRowsShape.TypeName)
                    : CreateTypeSyntax(typeof(Row)));

    private ExpressionSyntax CreateKeySetBuildRowsArgument(KeySetBuildHelper helper)
    {
        return helper is { RawRowsShape: not null, Loop.Source: ExecutionStoredTableRows storedRows }
            ? CreateStoredTableRowsRead(storedRows)
            : RenderExpression(helper.Loop.Source);
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
