using Musoq.Evaluator.IR.Execution.Lowering.Ctes;
namespace Musoq.Evaluator.IR.Execution.Lowering;

internal enum LoweringAttemptKind
{
    NoMatch,
    Built,
    Unsupported
}

internal readonly record struct LoweringAttempt<T>
{
    private readonly T? value;

    private LoweringAttempt(LoweringAttemptKind kind, T? value, string? unsupportedReason)
    {
        Kind = kind;
        this.value = value;
        UnsupportedReason = unsupportedReason ?? string.Empty;
    }

    public LoweringAttemptKind Kind { get; }

    public T Value => Kind == LoweringAttemptKind.Built
        ? value!
        : throw new InvalidOperationException(
            $"A lowering attempt with kind '{Kind}' does not contain a built result.");

    public string UnsupportedReason { get; }

    public bool IsTerminal => Kind is LoweringAttemptKind.Built or LoweringAttemptKind.Unsupported;

    public bool IsBuilt => Kind == LoweringAttemptKind.Built;

    public bool IsUnsupported => Kind == LoweringAttemptKind.Unsupported;

    public static LoweringAttempt<T> NoMatch() => new(LoweringAttemptKind.NoMatch, default, null);

    public static LoweringAttempt<T> Built(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new LoweringAttempt<T>(LoweringAttemptKind.Built, value, null);
    }

    public static LoweringAttempt<T> Unsupported(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new LoweringAttempt<T>(LoweringAttemptKind.Unsupported, default, reason);
    }

    public T RequireValue()
    {
        return Value;
    }

    public string RequireUnsupportedReason()
    {
        return Kind == LoweringAttemptKind.Unsupported && !string.IsNullOrWhiteSpace(UnsupportedReason)
            ? UnsupportedReason
            : throw new InvalidOperationException(
                $"A lowering attempt with kind '{Kind}' does not contain an unsupported reason.");
    }
}

internal static class LoweringAttemptConversions
{
    public static LoweringAttempt<ExecutionPlan> From(ExecutionPlanBuildResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsBuilt)
        {
            return LoweringAttempt<ExecutionPlan>.Unsupported(
                result.UnsupportedReason ?? "The physical plan lowerer reported an unsupported shape.");
        }

        return result.ExecutionPlan is { } executionPlan
            ? LoweringAttempt<ExecutionPlan>.Built(executionPlan)
            : throw new InvalidOperationException(
                "A supported execution-plan result must expose an execution plan.");
    }

    public static LoweringAttempt<LoweredTable> From(TableBuildResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsBuilt
            ? LoweringAttempt<LoweredTable>.Built(LoweredTable.FromBuilt(result))
            : LoweringAttempt<LoweredTable>.Unsupported(result.UnsupportedReason);
    }
}

internal readonly record struct OptionalValue<T>
{
    private OptionalValue(bool hasValue, T? value)
    {
        HasValue = hasValue;
        Value = value;
    }

    public bool HasValue { get; }

    public T? Value { get; }

    public static OptionalValue<T> None() => new(false, default);

    public static OptionalValue<T> Some(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new OptionalValue<T>(true, value);
    }

    public T RequireValue() => HasValue
        ? Value!
        : throw new InvalidOperationException("The optional lowering value is empty.");
}
