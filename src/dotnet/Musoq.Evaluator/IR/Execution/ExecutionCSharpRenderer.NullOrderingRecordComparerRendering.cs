using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static void AddOrderRecordComparisonStatements(
        List<string> body,
        int index,
        ExecutionOrderField key,
        FieldBinding field)
    {
        if (RequiresExplicitNullOrdering(key))
        {
            AddExplicitNullOrderingComparisonStatements(body, index, key, field);
            return;
        }

        var comparisonExpression = CreateOrderRecordComparisonExpression(field, key.Type);
        body.Add(index == 0
            ? $"        var comparison = {comparisonExpression};"
            : $"        comparison = {comparisonExpression};");
        if (key.Descending)
            body.Add("        comparison = -comparison;");
        AddOrderRecordComparisonReturn(body);
    }

    private static void AddExplicitNullOrderingComparisonStatements(
        List<string> body,
        int index,
        ExecutionOrderField key,
        FieldBinding field)
    {
        var fieldName = GetGeneratedFieldName(field);
        var left = $"left.{fieldName}";
        var right = $"right.{fieldName}";
        var leftNull = $"leftNull{index}";
        var rightNull = $"rightNull{index}";
        var leftNullComparison = key.NullOrdering == NullOrdering.First ? "-1" : "1";
        var rightNullComparison = key.NullOrdering == NullOrdering.First ? "1" : "-1";

        body.Add(index == 0 ? "        var comparison = 0;" : "        comparison = 0;");
        body.Add($"        var {leftNull} = {CreateOrderNullCheck(left, key.Type)};");
        body.Add($"        var {rightNull} = {CreateOrderNullCheck(right, key.Type)};");
        body.Add($"        if ({leftNull} || {rightNull})");
        body.Add("        {");
        body.Add($"            if ({leftNull} && {rightNull})");
        body.Add("                comparison = 0;");
        body.Add($"            else if ({leftNull})");
        body.Add($"                comparison = {leftNullComparison};");
        body.Add("            else");
        body.Add($"                comparison = {rightNullComparison};");
        body.Add("        }");
        body.Add("        else");
        body.Add("        {");
        body.Add($"            comparison = {CreateOrderRecordComparisonExpression(field, key.Type)};");
        if (key.Descending)
            body.Add("            comparison = -comparison;");
        body.Add("        }");
        AddOrderRecordComparisonReturn(body);
    }

    private static void AddOrderRecordComparisonReturn(List<string> body)
    {
        body.Add("        if (comparison != 0)");
        body.Add("            return comparison;");
        body.Add(string.Empty);
    }

    private static bool RequiresExplicitNullOrdering(ExecutionOrderField key)
    {
        return key.NullOrdering != NullOrdering.Default &&
               (!key.Type.IsValueType || Nullable.GetUnderlyingType(key.Type) != null);
    }

    private static string CreateOrderNullCheck(string value, Type type)
    {
        return Nullable.GetUnderlyingType(type) != null
            ? $"!{value}.HasValue"
            : $"{value} == null";
    }

    private static string FormatNullOrderingSuffix(NullOrdering nullOrdering)
    {
        return nullOrdering switch
        {
            NullOrdering.First => "F",
            NullOrdering.Last => "L",
            _ => string.Empty
        };
    }
}
