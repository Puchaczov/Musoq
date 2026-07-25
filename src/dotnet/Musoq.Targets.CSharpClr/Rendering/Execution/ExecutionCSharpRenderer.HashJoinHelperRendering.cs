using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<MethodDeclarationSyntax> CreateHashJoinHelperFunctions(
        HashJoinHelperSet helperSet,
        ExecutionRenderContext context)
    {
        yield return CreateHashBuildFunction(helperSet.Build, context);
        yield return CreateHashProbeFunction(helperSet.Probe, context);
    }

    private MethodDeclarationSyntax CreateHashBuildFunction(
        HashBuildHelper helper,
        ExecutionRenderContext context)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName, helper.RawRowsShape);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateHashBuildParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private MethodDeclarationSyntax CreateHashProbeFunction(
        HashProbeHelper helper,
        ExecutionRenderContext context)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateHashProbeParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    context,
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private ExpressionStatementSyntax CreateHashBuildInvocation(
        HashBuildHelper helper,
        ExecutionRenderContext context)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateHashBuildArguments(helper, context));
    }

    private ExpressionStatementSyntax CreateHashProbeInvocation(
        HashProbeHelper helper,
        ExecutionRenderContext context)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateHashProbeArguments(helper, context));
    }

    private List<ExpressionSyntax> CreateHashBuildArguments(
        HashBuildHelper helper,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            CreateHashBuildRowsArgument(helper, context),
            SyntaxFactory.IdentifierName(helper.HashAdd.Hash.Name),
            SyntaxFactory.IdentifierName("token")
        };

        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private List<ExpressionSyntax> CreateHashProbeArguments(
        HashProbeHelper helper,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(helper.Loop.Source, context),
            SyntaxFactory.IdentifierName(helper.HashProbe.Hash.Name)
        };

        arguments.AddRange(helper.AppendTargets.Select(static target => SyntaxFactory.IdentifierName(target.Name)));
        arguments.Add(SyntaxFactory.IdentifierName("token"));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private ParameterListSyntax CreateHashBuildParameterList(
        HashBuildHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(helper.RowsParameterName, CreateHashBuildRowsParameterType(helper, context)),
            CreateParameter(
                helper.HashAdd.Hash.Name,
                CreateHashTypeSyntax(
                    helper.HashAdd.KeyType.RequireClrType(),
                    helper.HashAdd.RowType.RequireClrType(),
                    helper.HashAdd.GeneratedRowTypeName)),
            CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken)))
        };

        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private ParameterListSyntax CreateHashProbeParameterList(
        HashProbeHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(
                helper.RowsParameterName,
                CreateAggregateRowsParameterType(
                    helper.Loop.Source,
                    CreateVariableTypeSyntax(helper.Loop.Item))),
            CreateParameter(
                helper.HashProbe.Hash.Name,
                CreateHashTypeSyntax(
                    helper.HashProbe.KeyType.RequireClrType(),
                    helper.HashProbe.RowType.RequireClrType(),
                    helper.HashProbe.GeneratedRowTypeName))
        };

        parameters.AddRange(helper.AppendTargets.Select(target => CreateParameter(
            target.Name,
            CreateAppendTargetParameterType(target, context))));
        parameters.Add(CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private TypeSyntax CreateAppendTargetParameterType(
        ExecutionVariable target,
        ExecutionRenderContext context)
    {
        if (TryGetFinalShapeSourceBuffer(target.Name, context, out var finalShapeBuffer))
            return CreateListTypeSyntax(finalShapeBuffer.ShapeTypeName);

        if (TryGetTypedRowBufferShape(target.Name, context, out var rowShape))
            return CreateListTypeSyntax(rowShape.TypeName);

        return CreateTypeSyntax(typeof(Table));
    }

    private TypeSyntax CreateHashBuildRowsParameterType(
        HashBuildHelper helper,
        ExecutionRenderContext context)
    {
        if (helper.RawRowsShape == null)
        {
            return CreateAggregateRowsParameterType(
                helper.Loop.Source,
                CreateVariableTypeSyntax(helper.Loop.Item));
        }

        return helper.Loop.Source is ExecutionStoredTableRows storedRows &&
               TryGetTypedStoredTableResult(storedRows.TableIndex, helper.RawRowsShape, context, out _)
            ? CreateReadOnlyListTypeSyntax(SyntaxFactory.ParseTypeName(helper.RawRowsShape.TypeName))
            : CreateReadOnlyListTypeSyntax(CreateTypeSyntax(typeof(Row)));
    }

    private ExpressionSyntax CreateHashBuildRowsArgument(
        HashBuildHelper helper,
        ExecutionRenderContext context)
    {
        return helper is { RawRowsShape: not null, Loop.Source: ExecutionStoredTableRows storedRows }
            ? CreateStoredTableRowsRead(storedRows, context)
            : RenderExpression(helper.Loop.Source, context);
    }

    private static ExecutionSourceLoop ReplaceLoopSource(
        ExecutionSourceLoop loop,
        string rowsParameterName,
        GeneratedRowShape? rawRowsShape = null)
    {
        var rowsVariable = rawRowsShape == null
            ? new ExecutionVariable(rowsParameterName, typeof(object))
            : new ExecutionVariable(rowsParameterName, typeof(IReadOnlyList<Row>), rawRowsShape.TypeName);

        return loop switch
        {
            ExecutionForEach forEach => forEach with
            {
                Source = ExecutionRowStreams.RebindLike(forEach.Source, rowsVariable)
            },
            _ => throw new InvalidOperationException(
                $"Unsupported source loop node '{loop.GetType().Name}' for helper source replacement.")
        };
    }
}
