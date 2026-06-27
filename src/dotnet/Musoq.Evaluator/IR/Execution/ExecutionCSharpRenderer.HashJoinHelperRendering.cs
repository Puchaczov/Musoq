using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<MethodDeclarationSyntax> CreateHashJoinHelperFunctions(HashJoinHelperSet helperSet)
    {
        yield return CreateHashBuildFunction(helperSet.Build);
        yield return CreateHashProbeFunction(helperSet.Probe);
    }

    private MethodDeclarationSyntax CreateHashBuildFunction(HashBuildHelper helper)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName, helper.RawRowsShape);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateHashBuildParameterList(helper))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private MethodDeclarationSyntax CreateHashProbeFunction(HashProbeHelper helper)
    {
        var helperLoop = ReplaceLoopSource(helper.Loop, helper.RowsParameterName);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateHashProbeParameterList(helper))
            .WithBody(StatementEmitter.CreateBlock([
                QueryEmitter.GenerateCancellationCheck(),
                ..RenderIsolatedHelperBlock(
                    new ExecutionBlock([helperLoop]),
                    profileRecorderInScope: IsInstrumentationEnabled,
                    emitChunkLoopCancellationChecks: true)
            ]));
    }

    private ExpressionStatementSyntax CreateHashBuildInvocation(HashBuildHelper helper)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateHashBuildArguments(helper));
    }

    private ExpressionStatementSyntax CreateHashProbeInvocation(HashProbeHelper helper)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateHashProbeArguments(helper));
    }

    private List<ExpressionSyntax> CreateHashBuildArguments(HashBuildHelper helper)
    {
        var arguments = new List<ExpressionSyntax>
        {
            CreateHashBuildRowsArgument(helper),
            SyntaxFactory.IdentifierName(helper.HashAdd.Hash.Name),
            SyntaxFactory.IdentifierName("token")
        };

        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private List<ExpressionSyntax> CreateHashProbeArguments(HashProbeHelper helper)
    {
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(helper.Loop.Source),
            SyntaxFactory.IdentifierName(helper.HashProbe.Hash.Name)
        };

        arguments.AddRange(helper.AppendTargets.Select(static target => SyntaxFactory.IdentifierName(target.Name)));
        arguments.Add(SyntaxFactory.IdentifierName("token"));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private ParameterListSyntax CreateHashBuildParameterList(HashBuildHelper helper)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(helper.RowsParameterName, CreateHashBuildRowsParameterType(helper)),
            CreateParameter(
                helper.HashAdd.Hash.Name,
                CreateHashTypeSyntax(
                    helper.HashAdd.KeyType,
                    helper.HashAdd.RowType,
                    helper.HashAdd.GeneratedRowTypeName)),
            CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken)))
        };

        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private ParameterListSyntax CreateHashProbeParameterList(HashProbeHelper helper)
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
                    helper.HashProbe.KeyType,
                    helper.HashProbe.RowType,
                    helper.HashProbe.GeneratedRowTypeName))
        };

        parameters.AddRange(helper.AppendTargets.Select(target => CreateParameter(
            target.Name,
            CreateAppendTargetParameterType(target))));
        parameters.Add(CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private TypeSyntax CreateAppendTargetParameterType(ExecutionVariable target)
    {
        if (TryGetFinalShapeSourceBuffer(target.Name, out var finalShapeBuffer))
            return CreateListTypeSyntax(finalShapeBuffer.ShapeTypeName);

        if (TryGetTypedRowBufferShape(target.Name, out var rowShape))
            return CreateListTypeSyntax(rowShape.TypeName);

        return CreateTypeSyntax(typeof(Table));
    }

    private TypeSyntax CreateHashBuildRowsParameterType(HashBuildHelper helper)
    {
        if (helper.RawRowsShape == null)
        {
            return CreateAggregateRowsParameterType(
                helper.Loop.Source,
                CreateVariableTypeSyntax(helper.Loop.Item));
        }

        return helper.Loop.Source is ExecutionStoredTableRows storedRows &&
               TryGetTypedStoredTableResult(storedRows.TableIndex, helper.RawRowsShape, out _)
            ? CreateReadOnlyListTypeSyntax(SyntaxFactory.ParseTypeName(helper.RawRowsShape.TypeName))
            : CreateReadOnlyListTypeSyntax(CreateTypeSyntax(typeof(Row)));
    }

    private ExpressionSyntax CreateHashBuildRowsArgument(HashBuildHelper helper)
    {
        return helper is { RawRowsShape: not null, Loop.Source: ExecutionStoredTableRows storedRows }
            ? CreateStoredTableRowsRead(storedRows)
            : RenderExpression(helper.Loop.Source);
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
