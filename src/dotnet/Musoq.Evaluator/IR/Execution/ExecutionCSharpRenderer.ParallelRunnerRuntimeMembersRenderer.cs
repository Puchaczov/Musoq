using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IReadOnlyList<ExpressionSyntax> CreateParallelRunnerConstructorArguments(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName("provider"),
            SyntaxFactory.IdentifierName("sourceRuntimeSettingsBySourceContextId"),
            SyntaxFactory.IdentifierName("sourceExecutionPlans"),
            SyntaxFactory.IdentifierName("logger"),
            SyntaxFactory.IdentifierName("token"),
            SyntaxFactory.IdentifierName("OnDataSourceProgress")
        };

        if (IsInstrumentationEnabled)
            arguments.Add(SyntaxFactory.IdentifierName(ProfileRecorderVariableName));

        if (NeedsParallelTaskPhaseChanged(parallel))
            arguments.Add(SyntaxFactory.IdentifierName("OnPhaseChanged"));

        if (RenderSession.IncludeTableResults)
            arguments.Add(SyntaxFactory.IdentifierName("_tableResults"));

        if (RenderSession.IncludeCteRowResults)
            arguments.Add(SyntaxFactory.IdentifierName("_cteRowResults"));

        if (RenderSession.IncludeCteIndexResults)
            arguments.Add(SyntaxFactory.IdentifierName("_cteIndexResults"));

        arguments.AddRange(captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private IReadOnlyList<ParameterSyntax> CreateParallelRunnerConstructorParameters(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures)
    {
        return [
            ..CreateParallelRunnerRuntimeMembers(parallel)
                .Select(static member => CreateParameter(member.ParameterName, member.Type)),
            ..captures.Select(CreateCapturedLocalParameter)
        ];
    }

    private ParameterListSyntax CreateParallelTaskParameterList(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
        [
            ..CreateParallelRunnerRuntimeMembers(parallel)
                .Select(static member => CreateParameter(member.ParameterName, member.Type)),
            ..captures.Select(CreateCapturedLocalParameter)
        ]));
    }

    private IReadOnlyList<ExpressionSyntax> CreateParallelRunnerStaticTaskArguments(
        ExecutionParallelBlock parallel,
        IReadOnlyList<CapturedLocal> captures)
    {
        return [
            ..CreateParallelRunnerRuntimeMembers(parallel)
                .Select(static member => (ExpressionSyntax)SyntaxFactory.IdentifierName(member.FieldName)),
            ..captures.Select(capture => (ExpressionSyntax)SyntaxFactory.IdentifierName(
                CreateParallelRunnerCapturedFieldName(capture)))
        ];
    }

    private static string CreateParallelRunnerCapturedFieldName(CapturedLocal capture)
    {
        return $"_{capture.Name}";
    }

    private IReadOnlyList<ParallelRunnerRuntimeMember> CreateParallelRunnerRuntimeMembers(
        ExecutionParallelBlock parallel)
    {
        var members = new List<ParallelRunnerRuntimeMember>
        {
            new("provider", "_provider", CreateTypeSyntax(typeof(ISchemaProvider))),
            new(
                "sourceRuntimeSettingsBySourceContextId",
                "_sourceRuntimeSettingsBySourceContextId",
                SyntaxFactory.ParseTypeName("IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>")),
            new(
                "sourceExecutionPlans",
                "_sourceExecutionPlans",
                SyntaxFactory.ParseTypeName("IReadOnlyDictionary<string, SourceExecutionPlan>")),
            new("logger", "_logger", CreateTypeSyntax(typeof(ILogger))),
            new("token", "_token", CreateTypeSyntax(typeof(CancellationToken))),
            new("OnDataSourceProgress", "_onDataSourceProgress", CreateTypeSyntax(typeof(DataSourceEventHandler)))
        };

        if (IsInstrumentationEnabled)
        {
            members.Add(new ParallelRunnerRuntimeMember(
                ProfileRecorderVariableName,
                $"_{ProfileRecorderVariableName}",
                CreateTypeSyntax(typeof(QueryProfileRecorder))));
        }

        if (NeedsParallelTaskPhaseChanged(parallel))
            members.Add(new ParallelRunnerRuntimeMember(
                "OnPhaseChanged",
                "_onPhaseChanged",
                SyntaxFactory.ParseTypeName("Action<string, QueryPhase>")));

        if (RenderSession.IncludeTableResults)
        {
            members.Add(new ParallelRunnerRuntimeMember("_tableResults", "_tableResults", SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(Table)))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.OmittedArraySizeExpression()))))));
        }

        if (RenderSession.IncludeCteRowResults)
            members.Add(new ParallelRunnerRuntimeMember("_cteRowResults", "_cteRowResults", CreateCteRowResultsTypeSyntax()));

        if (RenderSession.IncludeCteIndexResults)
            members.Add(new ParallelRunnerRuntimeMember("_cteIndexResults", "_cteIndexResults", CreateCteIndexResultsTypeSyntax()));

        return members;
    }

    private static bool NeedsParallelTaskPhaseChanged(ExecutionParallelBlock parallel)
    {
        return parallel.Tasks.Any(static task => task.RelatedQueryIdentifier != null);
    }
}
