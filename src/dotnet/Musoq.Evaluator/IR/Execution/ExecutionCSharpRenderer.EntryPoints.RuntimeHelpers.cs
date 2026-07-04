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

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private InvocationExpressionSyntax CreateRuntimeHelperInvocation(
        string functionName,
        IReadOnlyList<CapturedLocal> captures)
    {
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(functionName))
            .WithArgumentList(CreateArgumentList(
                [..CreateRuntimeHelperArguments(), ..captures.Select(CreateCapturedLocalArgument)]));
        return CodegenHelperExtractionMetadata.AnnotateCallSite(invocation, functionName);
    }

    private IReadOnlyList<ExpressionSyntax> CreateRuntimeHelperArguments()
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

        if (RenderSession.IncludeTableResults)
            arguments.Add(SyntaxFactory.IdentifierName("_tableResults"));

        if (RenderSession.IncludeCteRowResults)
            arguments.Add(SyntaxFactory.IdentifierName("_cteRowResults"));

        if (RenderSession.IncludeCteIndexResults)
            arguments.Add(SyntaxFactory.IdentifierName("_cteIndexResults"));

        return arguments;
    }

    private ParameterListSyntax CreateRuntimeHelperParameterList(IReadOnlyList<CapturedLocal> captures)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [..CreateRuntimeHelperParameters(), ..captures.Select(CreateCapturedLocalParameter)]));
    }

    private IReadOnlyList<ParameterSyntax> CreateRuntimeHelperParameters()
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
            CreateParameter("OnDataSourceProgress", CreateTypeSyntax(typeof(DataSourceEventHandler)))
        };

        if (IsInstrumentationEnabled)
            parameters.Add(CreateParameter(ProfileRecorderVariableName, CreateTypeSyntax(typeof(QueryProfileRecorder))));

        if (RenderSession.IncludeTableResults)
        {
            parameters.Add(CreateParameter("_tableResults", SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(Table)))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.OmittedArraySizeExpression()))))));
        }

        if (RenderSession.IncludeCteRowResults)
            parameters.Add(CreateParameter("_cteRowResults", CreateCteRowResultsTypeSyntax()));

        if (RenderSession.IncludeCteIndexResults)
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

    private IReadOnlyList<string> CreateRuntimeHelperParameterNames()
    {
        var names = new List<string>
        {
            "provider",
            "sourceRuntimeSettingsBySourceContextId",
            "sourceExecutionPlans",
            "logger",
            "token",
            "OnDataSourceProgress"
        };

        if (IsInstrumentationEnabled)
            names.Add(ProfileRecorderVariableName);

        if (RenderSession.IncludeTableResults)
            names.Add("_tableResults");

        if (RenderSession.IncludeCteRowResults)
            names.Add("_cteRowResults");

        if (RenderSession.IncludeCteIndexResults)
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
