using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private InvocationExpressionSyntax CreateRuntimeHelperInvocation(
        string functionName,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(functionName))
            .WithArgumentList(CreateArgumentList(
                [..CreateRuntimeHelperArguments(context), ..captures.Select(CreateCapturedLocalArgument)]));
        return CodegenHelperExtractionMetadata.AnnotateCallSite(invocation, functionName);
    }

    private IReadOnlyList<ExpressionSyntax> CreateRuntimeHelperArguments(ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName("provider"),
            SyntaxFactory.IdentifierName("sourceRuntimeSettingsBySourceContextId"),
            SyntaxFactory.IdentifierName("sourceExecutionPlans"),
            SyntaxFactory.IdentifierName("logger"),
            SyntaxFactory.IdentifierName("token"),
            SyntaxFactory.IdentifierName("__musoqProgressContext"),
            SyntaxFactory.IdentifierName("OnDataSourceProgress"),
            SyntaxFactory.IdentifierName("OnQueryProgress")
        };

        arguments.Add(SyntaxFactory.IdentifierName("OnPhaseChanged"));

        if (IsInstrumentationEnabled)
            arguments.Add(SyntaxFactory.IdentifierName(ProfileRecorderVariableName));

        if (context.Session.IncludeTableResults)
            arguments.Add(SyntaxFactory.IdentifierName("_tableResults"));

        if (context.Session.IncludeCteRowResults)
            arguments.Add(SyntaxFactory.IdentifierName("_cteRowResults"));

        if (context.Session.IncludeCteIndexResults)
            arguments.Add(SyntaxFactory.IdentifierName("_cteIndexResults"));

        return arguments;
    }

    private ParameterListSyntax CreateRuntimeHelperParameterList(
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [..CreateRuntimeHelperParameters(context), ..captures.Select(CreateCapturedLocalParameter)]));
    }

    private IReadOnlyList<ParameterSyntax> CreateRuntimeHelperParameters(ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter("provider", CreateTypeSyntax(typeof(ISchemaProvider))),
            CreateParameter(
                "sourceRuntimeSettingsBySourceContextId",
                SyntaxFactory.ParseTypeName("IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>")),
            CreateParameter(
                "sourceExecutionPlans",
                SyntaxFactory.ParseTypeName("IReadOnlyDictionary<string, SourceExecutionPlan>")),
            CreateParameter("logger", CreateTypeSyntax(typeof(ILogger))),
            CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))),
            CreateParameter("__musoqProgressContext", SyntaxFactory.ParseTypeName("QueryRunContext?")),
            CreateParameter("OnDataSourceProgress", CreateTypeSyntax(typeof(DataSourceEventHandler))),
            CreateParameter("OnQueryProgress", CreateTypeSyntax(typeof(QueryProgressEventHandler)))
        };

        parameters.Add(CreateParameter("OnPhaseChanged", SyntaxFactory.ParseTypeName("Action<string, QueryPhase>")));

        if (IsInstrumentationEnabled)
            parameters.Add(CreateParameter(ProfileRecorderVariableName, CreateTypeSyntax(typeof(QueryProfileRecorder))));

        if (context.Session.IncludeTableResults)
        {
            parameters.Add(CreateParameter("_tableResults", SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(Table)))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.OmittedArraySizeExpression()))))));
        }

        if (context.Session.IncludeCteRowResults)
            parameters.Add(CreateParameter("_cteRowResults", CreateCteRowResultsTypeSyntax()));

        if (context.Session.IncludeCteIndexResults)
        {
            parameters.Add(CreateParameter("_cteIndexResults", CreateCteIndexResultsTypeSyntax()));
        }

        return parameters;
    }

    private void AddProfileRecorderArgument(List<ExpressionSyntax> arguments)
    {
        if (IsInstrumentationEnabled)
            arguments.Add(SyntaxFactory.IdentifierName(ProfileRecorderVariableName));
    }

    private void AddProfileRecorderArgument(List<ArgumentSyntax> arguments)
    {
        if (IsInstrumentationEnabled)
            arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ProfileRecorderVariableName)));
    }

    private void AddProfileRecorderParameter(List<ParameterSyntax> parameters)
    {
        if (IsInstrumentationEnabled)
            parameters.Add(CreateParameter(ProfileRecorderVariableName, CreateTypeSyntax(typeof(QueryProfileRecorder))));
    }

    private void AddProfileRecorderExcludedName(ISet<string> excludedNames)
    {
        if (IsInstrumentationEnabled)
            excludedNames.Add(ProfileRecorderVariableName);
    }

    private IReadOnlyList<string> CreateRuntimeHelperParameterNames(ExecutionRenderContext context)
    {
        var names = new List<string>
        {
            "provider",
            "sourceRuntimeSettingsBySourceContextId",
            "sourceExecutionPlans",
            "logger",
            "token",
            "__musoqProgressContext",
            "OnDataSourceProgress",
            "OnQueryProgress",
            "OnPhaseChanged"
        };

        if (IsInstrumentationEnabled)
            names.Add(ProfileRecorderVariableName);

        if (context.Session.IncludeTableResults)
            names.Add("_tableResults");

        if (context.Session.IncludeCteRowResults)
            names.Add("_cteRowResults");

        if (context.Session.IncludeCteIndexResults)
            names.Add("_cteIndexResults");

        return names;
    }

    private static SyntaxTokenList CreatePrivateStaticModifiers()
    {
        return SyntaxFactory.TokenList(
            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
            SyntaxFactory.Token(SyntaxKind.StaticKeyword));
    }
}
