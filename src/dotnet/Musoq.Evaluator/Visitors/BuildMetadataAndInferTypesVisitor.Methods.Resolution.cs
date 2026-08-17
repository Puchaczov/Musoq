using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static (MethodInfo Method, bool CanSkipInjectSource) ResolveMethod(AccessMethodNode node, ArgsListNode args,
        MethodResolutionContext context)
    {
        var argTypes = GetArgumentTypes(args);

        var methodName = node.Name;
        if (node.IsDistinct)
        {
            var distinctMethodName = $"{node.Name}Distinct";
            if (TryResolveAggregateDeclarationMethod(distinctMethodName, argTypes, args, context, out var distinctDeclaration))
                return (distinctDeclaration, false);

            throw CallableResolutionDiagnostics.CreateException(
                context.SchemaTablePair.Schema,
                distinctMethodName,
                argTypes,
                context.EntityType,
                node.SpanOrEmpty(),
                args.Args);
        }

        if (TryResolveAggregateDeclarationMethod(methodName, argTypes, args, context, out var declaration))
            return (declaration, false);

        if (context.SchemaTablePair.Schema.TryResolveMethod(methodName, argTypes, context.EntityType, out var method))
        {
            if (method.GetCustomAttribute<AggregationMethodAttribute>() is not null)
                throw new CannotResolveMethodException(
                    $"Aggregate method {methodName} must declare a typed AggregateFunctionAttribute and a valid non-negative literal parent depth.",
                    node.SpanOrEmpty());

            return (method, false);
        }

        if (context.SchemaTablePair.Schema.TryResolveRawMethod(methodName, argTypes, out method)) return (method, true);

        if (IsInterpretOrParseFunction(methodName))
            throw new CannotResolveMethodException(
                $"'{methodName}' can only be used inside CROSS APPLY or OUTER APPLY, not in SELECT or WHERE. " +
                $"Example: ... CROSS APPLY {methodName}<SchemaName>(source) alias",
                DiagnosticCode.MQ3033_InterpretFunctionOutsideApply,
                node.Span);

        throw CallableResolutionDiagnostics.CreateException(
            context.SchemaTablePair.Schema,
            methodName,
            argTypes,
            context.EntityType,
            node.SpanOrEmpty(),
            args.Args);
    }
}
