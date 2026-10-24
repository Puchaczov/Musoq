using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Targets.Abstractions;

public sealed record ExecutionSemanticsContract
{
    private readonly IReadOnlyDictionary<ExecutionSemanticsRuleId, ExecutionSemanticsRule> _rulesById;
    private readonly IReadOnlyDictionary<string, string> _rules;

    public ExecutionSemanticsContract(int version, IEnumerable<ExecutionSemanticsRule> rules)
    {
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version));

        var ruleArray = (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();
        if (ruleArray.Length == 0 || ruleArray.Any(static rule => string.IsNullOrWhiteSpace(rule.Behavior)))
        {
            throw new ArgumentException(
                "Execution semantics rules require at least one non-empty behavior.",
                nameof(rules));
        }

        if (ruleArray.Select(static rule => rule.Id).Distinct().Count() != ruleArray.Length)
            throw new ArgumentException("Execution semantics rule ids must be unique.", nameof(rules));

        Version = version;
        RuleDefinitions = Array.AsReadOnly(
            ruleArray
                .OrderBy(static rule => rule.StableId, StringComparer.Ordinal)
                .ToArray());
        _rulesById = new ReadOnlyDictionary<ExecutionSemanticsRuleId, ExecutionSemanticsRule>(
            RuleDefinitions.ToDictionary(static rule => rule.Id));
        _rules = new ReadOnlyDictionary<string, string>(
            RuleDefinitions.ToDictionary(static rule => rule.StableId, static rule => rule.Behavior, StringComparer.Ordinal));
        Fingerprint = CreateFingerprint(version, RuleDefinitions);
    }

    public ExecutionSemanticsContract(int version, IReadOnlyDictionary<string, string> rules)
        : this(version, CreateRules(rules))
    {
    }

    public int Version { get; }

    public IReadOnlyList<ExecutionSemanticsRule> RuleDefinitions { get; }

    public IReadOnlyDictionary<string, string> Rules => _rules;

    public string Fingerprint { get; }

    public string RequireRule(string operation) =>
        _rules.TryGetValue(operation, out var behavior)
            ? behavior
            : throw new KeyNotFoundException($"Execution semantics version {Version} has no rule for '{operation}'.");

    public string RequireRule(ExecutionSemanticsRuleId id) =>
        _rulesById.TryGetValue(id, out var rule)
            ? rule.Behavior
            : throw new KeyNotFoundException($"Execution semantics version {Version} has no rule for '{id}'.");

    public bool IsEquivalentTo(ExecutionSemanticsContract? other) =>
        other is not null &&
        Version == other.Version &&
        string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal);

    public static ExecutionSemanticsContract Version1 { get; } = new(
        1,
        [
            new(ExecutionSemanticsRuleId.NullLogic, "sql-three-valued"),
            new(ExecutionSemanticsRuleId.NullOrdering, "musoq-clr-v1"),
            new(ExecutionSemanticsRuleId.IntegerRuntimeAddSubtractMultiply, "unchecked-width-wrap"),
            new(ExecutionSemanticsRuleId.IntegerConstantFoldingAddSubtractMultiply, "checked-diagnostic"),
            new(ExecutionSemanticsRuleId.IntegerAggregateAddSubtractMultiply, "checked-overflow"),
            new(ExecutionSemanticsRuleId.IntegerDivide, "truncate-toward-zero;divide-by-zero-error"),
            new(ExecutionSemanticsRuleId.IntegerModulo, "dividend-sign;divide-by-zero-error"),
            new(ExecutionSemanticsRuleId.FloatingPoint, "ieee-754-clr"),
            new(ExecutionSemanticsRuleId.Decimal, "clr-128-bit-decimal-checked"),
            new(ExecutionSemanticsRuleId.StringEqualityOrderingHashing, "ordinal"),
            new(ExecutionSemanticsRuleId.SqlStringConcatenation, "string-only;null-propagating;left-to-right;eager-operands"),
            new(ExecutionSemanticsRuleId.TemporalValueSemantics, "clr-value-semantics"),
            new(ExecutionSemanticsRuleId.StrictCast, "invariant-culture"),
            new(ExecutionSemanticsRuleId.GroupingDistinctJoinSetEquality, "musoq-clr-v1")
        ]);

    public static ExecutionSemanticsContract Version2 { get; } = new(
        2,
        Version1.RuleDefinitions.Select(static rule => rule.Id switch
        {
            ExecutionSemanticsRuleId.NullLogic => rule with { Behavior = "sql-three-valued;left-to-right;short-circuit-and-or" },
            ExecutionSemanticsRuleId.GroupingDistinctJoinSetEquality => rule with
            {
                Behavior = "musoq-clr-v1;union-left-first-representative;except-intersect-left-multiplicity"
            },
            _ => rule
        }).Concat([
                new(ExecutionSemanticsRuleId.ImplicitConversion, "lossless-domain-widening;pointwise-specificity;fixed-arity;non-generic;fewer-defaults;ambiguous-incomparable"),
                new(ExecutionSemanticsRuleId.Coalesce, "bind-all-operands;unique-most-specific-common-type;lazy-fallback"),
                new(ExecutionSemanticsRuleId.SubqueryExecution, "typed-lexical-captures;demand-scoped-apply;post-shaping-scalar-cardinality;three-valued-membership;post-shaping-any-some-all-three-valued-reduction"),
                new(ExecutionSemanticsRuleId.InterpretationExpressions, "lossless-coalesce;lazy-three-valued-logic;checked-int32-cursor-domain;typed-parse-recovery;precise-partial-cursor;recursive-binary-values;direct-text-switch-values;payload-json-converters")
            ]));

    public static ExecutionSemanticsContract Current => Version2;

    public static IReadOnlyList<ExecutionSemanticsContract> KnownContracts { get; } = [Version1, Version2];

    private static IEnumerable<ExecutionSemanticsRule> CreateRules(IReadOnlyDictionary<string, string> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
        {
            if (!ExecutionSemanticsRuleIds.TryParse(rule.Key, out var id))
            {
                throw new ArgumentException(
                    $"Execution semantics rule '{rule.Key}' has no stable typed rule id.",
                    nameof(rules));
            }

            yield return new ExecutionSemanticsRule(id, rule.Value);
        }
    }

    private static string CreateFingerprint(
        int version,
        IEnumerable<ExecutionSemanticsRule> rules)
    {
        var payload = string.Concat(
            $"version={version}\n",
            string.Join(
                string.Empty,
                rules.Select(static rule => $"{rule.StableId}={rule.Behavior}\n")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
