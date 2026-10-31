using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal sealed class NativeTableEnumDeclarationValidator(
    ISchemaProvider provider,
    IEnumerable<Assembly> assemblies)
{
    public void Validate(
        CreateTableNode table,
        CreateTableColumnDefinition column,
        string declaredTypeName,
        Type? resolvedType)
    {
        Type? nativeEnumType;
        if (resolvedType == null)
            nativeEnumType = ResolveExactNativeEnum(declaredTypeName);
        else
            nativeEnumType = Nullable.GetUnderlyingType(resolvedType) ?? resolvedType;

        if (nativeEnumType is not { IsEnum: true })
            return;

        var backingType = EvaluationHelper.GetCastableType(Enum.GetUnderlyingType(nativeEnumType));
        var span = column.Span.IsEmpty ? table.SpanOrEmpty() : column.Span;
        throw new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "BindTableColumn",
            $"TABLE '{table.Name}' column '{column.ColumnName}' cannot declare native CLR enum type '{declaredTypeName}'. " +
            $"Declare its underlying primitive type '{backingType}', or use a query-local ENUM.",
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            span);
    }

    private Type? ResolveExactNativeEnum(string typeName)
    {
        if (!typeName.Contains('.', StringComparison.Ordinal))
            return null;

        var reachableAssemblies = new List<Assembly>();
        for (var currentProvider = provider; currentProvider != null; currentProvider = currentProvider is TransitionSchemaProvider transition
                 ? transition.InnerProvider
                 : null)
        {
            if (!reachableAssemblies.Contains(currentProvider.GetType().Assembly))
                reachableAssemblies.Add(currentProvider.GetType().Assembly);
        }

        foreach (var assembly in assemblies)
            if (!reachableAssemblies.Contains(assembly))
                reachableAssemblies.Add(assembly);

        foreach (var assembly in reachableAssemblies)
        {
            var candidate = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (candidate is not { IsEnum: true })
                continue;

            return candidate;
        }

        return null;
    }
}
