using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private static CapturedLocal CreateCapturedLocal(ExecutionVariable variable)
    {
        return new CapturedLocal(variable.Name, variable.Type.RequireClrType(), variable.GeneratedRowTypeName);
    }

    private CapturedLocal CreateCapturedLocal(ExecutionScriptParameterRead parameterRead)
    {
        return new CapturedLocal(
            GetScriptParameterLocalName(parameterRead.Name),
            parameterRead.ReturnType.RequireClrType());
    }

    private CapturedLocal CreateCapturedLocal(ExecutionScriptVariableRead variableRead)
    {
        return new CapturedLocal(
            GetScriptVariableLocalName(variableRead.Name),
            variableRead.ReturnType.RequireClrType());
    }

    private static ParameterSyntax CreateCapturedLocalParameter(CapturedLocal capture)
    {
        return CreateParameter(capture.Name, CreateCapturedLocalTypeSyntax(capture));
    }

    private static ExpressionSyntax CreateCapturedLocalArgument(CapturedLocal capture)
    {
        return SyntaxFactory.IdentifierName(capture.Name);
    }

    private static TypeSyntax CreateCapturedLocalTypeSyntax(CapturedLocal capture)
    {
        if (!string.IsNullOrWhiteSpace(capture.GeneratedRowTypeName) && capture.Type.RequireClrType() == typeof(IReadOnlyList<Musoq.Evaluator.Tables.Row>))
            return CreateReadOnlyListTypeSyntax(SyntaxFactory.ParseTypeName(capture.GeneratedRowTypeName));

        return string.IsNullOrWhiteSpace(capture.GeneratedRowTypeName)
            ? CreateTypeSyntax(capture.Type)
            : SyntaxFactory.ParseTypeName(capture.GeneratedRowTypeName);
    }

    private static void AddHelperCapture(
        CapturedLocal capture,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        if (string.IsNullOrWhiteSpace(capture.Name) || excludedNames.Contains(capture.Name))
            return;

        captures.TryAdd(capture.Name, capture);
    }

    private static void AddHelperCapture(
        ExecutionVariable variable,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        AddHelperCapture(CreateCapturedLocal(variable), excludedNames, captures);
    }

    private void AddHelperCapture(
        ExecutionScriptParameterRead parameterRead,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        AddHelperCapture(CreateCapturedLocal(parameterRead), excludedNames, captures);
    }

    private void AddHelperCapture(
        ExecutionScriptVariableRead variableRead,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        AddHelperCapture(CreateCapturedLocal(variableRead), excludedNames, captures);
    }

    private void AddHelperCaptures(
        ExecutionBlock block,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        foreach (var node in block.Nodes)
            AddHelperCaptures(node, excludedNames, captures);
    }

    private void AddHelperCaptures(
        ExecutionNode node,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        if (node is ExecutionHashProbe hashProbe)
        {
            AddHelperCapture(
                hashProbe.Hash with
                {
                    Type = ExecutionClrBindingFactory.FromClr(typeof(object)),
                    GeneratedRowTypeName = CreateHashTypeName(
                        hashProbe.KeyType.RequireClrType(),
                        hashProbe.RowType.RequireClrType(),
                        hashProbe.GeneratedRowTypeName)
                },
                excludedNames,
                captures);
        }

        if (node is ExecutionKeySetProbe keySetProbe)
        {
            AddHelperCapture(
                keySetProbe.Set with
                {
                    Type = ExecutionClrBindingFactory.FromClr(typeof(object)),
                    GeneratedRowTypeName = CreateKeySetTypeName(keySetProbe.KeyType)
                },
                excludedNames,
                captures);
        }

        foreach (var expression in ExecutionIrAnalysis.GetNodeExpressions(node))
            AddHelperCaptures(expression, excludedNames, captures);

        foreach (var childBlock in GetChildBlocks(node))
            AddHelperCaptures(childBlock, excludedNames, captures);
    }

    private void AddHelperCaptures(
        IEnumerable<ExecutionExpression> expressions,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        foreach (var expression in expressions)
            AddHelperCaptures(expression, excludedNames, captures);
    }

    private void AddHelperCaptures(
        ExecutionExpression? expression,
        HashSet<string> excludedNames,
        Dictionary<string, CapturedLocal> captures)
    {
        if (expression == null)
            return;

        foreach (var current in ExecutionIrAnalysis.FlattenExpressions(expression))
        {
            switch (current)
            {
                case ExecutionVariableRead variableRead:
                    AddHelperCapture(variableRead.Variable, excludedNames, captures);
                    break;
                case ExecutionScriptParameterRead parameterRead:
                    AddHelperCapture(parameterRead, excludedNames, captures);
                    break;
                case ExecutionScriptVariableRead variableRead:
                    AddHelperCapture(variableRead, excludedNames, captures);
                    break;
                case ExecutionMethodCall methodCall:
                    if (methodCall.Target != null) AddHelperCapture(methodCall.Target, excludedNames, captures);
                    if (methodCall.Cache != null) AddHelperCapture(methodCall.Cache, excludedNames, captures);
                    break;
                case ExecutionStrictCast { Target: not null } strictCast:
                    AddHelperCapture(strictCast.Target, excludedNames, captures); break;
                case ExecutionRowStream rows:
                    AddHelperCapture(rows.Variable, excludedNames, captures);
                    break;
                case ExecutionScalarRowStream rows:
                    AddHelperCapture(rows.Variable, excludedNames, captures);
                    break;
                case ExecutionRowContextsRead rowContexts:
                    AddHelperCapture(rowContexts.Row, excludedNames, captures);
                    break;
                case ExecutionRowPresence rowPresence:
                    AddHelperCaptures(rowPresence.PresenceSource, excludedNames, captures);
                    break;
                case ExecutionWindowValueRead windowValue:
                    AddHelperCapture(windowValue.Results, excludedNames, captures);
                    AddHelperCapture(windowValue.Index, excludedNames, captures);
                    break;
                case ExecutionAggregateCall aggregateCall:
                    AddHelperCapture(aggregateCall.Group, excludedNames, captures);
                    break;
                case ExecutionGroupKeyRead groupKey:
                    AddHelperCapture(groupKey.Group, excludedNames, captures);
                    break;
                case ExecutionAggregateCapturedValueRead capturedValue:
                    AddHelperCapture(capturedValue.Group, excludedNames, captures);
                    break;
            }
        }
    }
}
