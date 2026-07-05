using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<ExpressionSyntax> CreateKeySetBuildArguments(
        KeySetBuildHelper helper,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            CreateKeySetBuildRowsArgument(helper, context),
            SyntaxFactory.IdentifierName(helper.KeySetAdd.Set.Name),
            SyntaxFactory.IdentifierName("token")
        };

        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private List<ExpressionSyntax> CreateKeySetProbeArguments(
        KeySetProbeHelper helper,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            RenderExpression(helper.Loop.Source, context),
            SyntaxFactory.IdentifierName(helper.KeySetProbe.Set.Name)
        };

        arguments.AddRange(helper.AppendTargets.Select(static target => SyntaxFactory.IdentifierName(target.Name)));
        arguments.Add(SyntaxFactory.IdentifierName("token"));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private ParameterListSyntax CreateKeySetBuildParameterList(
        KeySetBuildHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(helper.RowsParameterName, CreateKeySetBuildRowsParameterType(helper, context)),
            CreateParameter(helper.KeySetAdd.Set.Name, CreateKeySetTypeSyntax(helper.KeySetAdd.KeyType)),
            CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken)))
        };

        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private ParameterListSyntax CreateKeySetProbeParameterList(
        KeySetProbeHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(
                helper.RowsParameterName,
                CreateAggregateRowsParameterType(
                    helper.Loop.Source,
                    CreateVariableTypeSyntax(helper.Loop.Item))),
            CreateParameter(helper.KeySetProbe.Set.Name, CreateKeySetTypeSyntax(helper.KeySetProbe.KeyType))
        };

        parameters.AddRange(helper.AppendTargets.Select(target => CreateParameter(
            target.Name,
            CreateAppendTargetParameterType(target, context))));
        parameters.Add(CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }
}
