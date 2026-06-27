using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using ExecutionCSharpRenderer = Musoq.Evaluator.IR.Execution.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static GeneratedRowShape CreateSingleStringResultShape(string fieldName)
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding(
                    fieldName,
                    fieldName,
                    0,
                    typeof(string),
                    FieldNullability.Unknown,
                    new GeneratedFieldAccess(fieldName))
            ]);
    }

    private static string RenderClassMembersCode(ExecutionCSharpRenderer renderer, ExecutionPlan plan)
    {
        return string.Join(
            Environment.NewLine,
            renderer.RenderClassMembers(plan).Select(member => member.NormalizeWhitespace().ToFullString()));
    }

    private static void AssertNoParameterAccessInHelpers(string helperCode)
    {
        Assert.IsFalse(helperCode.Contains(nameof(ScriptParameterBinder), StringComparison.Ordinal));
        Assert.IsFalse(helperCode.Contains("Parameters", StringComparison.Ordinal));
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Trim();
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    public sealed class Person
    {
        public string Name { get; init; } = string.Empty;

        public int Age { get; init; }

        public DateTime? Start { get; init; }

        public DateTime? End { get; init; }
    }

    private sealed class KernelAggregateLibrary
    {
        [AggregateFunction(typeof(KernelSumAggregate))]
        public int? Sum(int? value)
        {
            _ = value;

            return AggregateFunction.NotInvoked<int?>();
        }
    }

    public static class KernelSumAggregate
    {
        public struct State
        {
            public bool HasValue;
            public int Value;
        }

        public static void Set(ref State state, int? value)
        {
            if (!value.HasValue)
                return;

            state.Value = state.HasValue
                ? checked(state.Value + value.Value)
                : value.Value;
            state.HasValue = true;
        }

        public static int? Get(in State state)
        {
            return state.HasValue ? state.Value : null;
        }

        public static void Merge(ref State target, in State source)
        {
            if (!source.HasValue)
                return;

            target.Value = target.HasValue
                ? checked(target.Value + source.Value)
                : source.Value;
            target.HasValue = true;
        }
    }
}
