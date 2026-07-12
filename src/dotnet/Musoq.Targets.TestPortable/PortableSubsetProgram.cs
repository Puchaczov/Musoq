using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Targets.TestPortable;

internal enum PortableValueKind
{
    Null = 0,
    Boolean = 1,
    Character = 2,
    SignedInteger = 3,
    UnsignedInteger = 4,
    FloatingPoint = 5,
    Decimal = 6,
    String = 7,
    DateTime = 8,
    DateTimeOffset = 9,
    Guid = 10,
    TimeSpan = 11,
    Enum = 12
}

internal sealed record PortableValue
{
    private PortableValue(
        PortableValueKind kind,
        int bitWidth = 0,
        bool boolean = false,
        long signedInteger = 0,
        ulong unsignedInteger = 0,
        ulong floatingPointBits = 0,
        IReadOnlyList<int>? decimalBits = null,
        string? text = null,
        long ticks = 0,
        DateTimeKind dateTimeKind = DateTimeKind.Unspecified,
        int offsetMinutes = 0,
        IReadOnlyList<byte>? guidBytes = null,
        string? enumTypeStableId = null,
        PortableValue? enumUnderlyingValue = null)
    {
        Kind = kind;
        BitWidth = bitWidth;
        Boolean = boolean;
        SignedInteger = signedInteger;
        UnsignedInteger = unsignedInteger;
        FloatingPointBits = floatingPointBits;
        DecimalBits = Array.AsReadOnly(decimalBits?.ToArray() ?? []);
        Text = text ?? string.Empty;
        Ticks = ticks;
        DateTimeKind = dateTimeKind;
        OffsetMinutes = offsetMinutes;
        GuidBytes = Array.AsReadOnly(guidBytes?.ToArray() ?? []);
        EnumTypeStableId = enumTypeStableId ?? string.Empty;
        EnumUnderlyingValue = enumUnderlyingValue;
    }

    public PortableValueKind Kind { get; }

    public int BitWidth { get; }

    public bool Boolean { get; }

    public long SignedInteger { get; }

    public ulong UnsignedInteger { get; }

    public ulong FloatingPointBits { get; }

    public IReadOnlyList<int> DecimalBits { get; }

    public string Text { get; }

    public long Ticks { get; }

    public DateTimeKind DateTimeKind { get; }

    public int OffsetMinutes { get; }

    public IReadOnlyList<byte> GuidBytes { get; }

    public string EnumTypeStableId { get; }

    public PortableValue? EnumUnderlyingValue { get; }

    public static PortableValue Null { get; } = new(PortableValueKind.Null);

    public static PortableValue FromBoolean(bool value) => new(PortableValueKind.Boolean, boolean: value);

    public static PortableValue FromCharacter(char value) => new(PortableValueKind.Character, unsignedInteger: value);

    public static PortableValue FromSigned(long value, int bitWidth = 64) =>
        new(PortableValueKind.SignedInteger, bitWidth: bitWidth, signedInteger: value);

    public static PortableValue FromUnsigned(ulong value, int bitWidth = 64) =>
        new(PortableValueKind.UnsignedInteger, bitWidth: bitWidth, unsignedInteger: value);

    public static PortableValue FromDouble(double value) => new(
        PortableValueKind.FloatingPoint,
        bitWidth: 64,
        floatingPointBits: unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

    public static PortableValue FromFloatingPointBits(int bitWidth, ulong bits) => new(
        PortableValueKind.FloatingPoint,
        bitWidth: bitWidth,
        floatingPointBits: bits);

    public static PortableValue FromDecimal(decimal value) => new(
        PortableValueKind.Decimal,
        decimalBits: decimal.GetBits(value));

    public static PortableValue FromString(string value) => new(
        PortableValueKind.String,
        text: value ?? throw new ArgumentNullException(nameof(value)));

    public static PortableValue FromDateTime(long ticks, DateTimeKind kind) =>
        new(PortableValueKind.DateTime, ticks: ticks, dateTimeKind: kind);

    public static PortableValue FromDateTimeOffset(long ticks, int offsetMinutes) =>
        new(PortableValueKind.DateTimeOffset, ticks: ticks, offsetMinutes: offsetMinutes);

    public static PortableValue FromGuid(IEnumerable<byte> bytes) =>
        new(PortableValueKind.Guid, guidBytes: bytes?.ToArray() ?? throw new ArgumentNullException(nameof(bytes)));

    public static PortableValue FromTimeSpan(long ticks) => new(PortableValueKind.TimeSpan, ticks: ticks);

    public static PortableValue FromEnum(string typeStableId, PortableValue underlyingValue) =>
        new(PortableValueKind.Enum, enumTypeStableId: typeStableId, enumUnderlyingValue: underlyingValue);

    public double AsDouble() => BitWidth switch
    {
        32 => BitConverter.Int32BitsToSingle(unchecked((int)FloatingPointBits)),
        64 => BitConverter.Int64BitsToDouble(unchecked((long)FloatingPointBits)),
        _ => throw new InvalidOperationException($"Unsupported floating-point width '{BitWidth}'.")
    };

    public decimal AsDecimal() => DecimalBits.Count == 4
        ? new decimal(DecimalBits.ToArray())
        : throw new InvalidOperationException("Portable decimal must contain four words.");

    public bool IsNull => Kind == PortableValueKind.Null;

    public string ToManifestValue() => Kind switch
    {
        PortableValueKind.Null => "null",
        PortableValueKind.Boolean => Boolean ? "true" : "false",
        PortableValueKind.Character => $"char:{UnsignedInteger:X4}",
        PortableValueKind.SignedInteger => $"i{BitWidth}:{SignedInteger.ToString(CultureInfo.InvariantCulture)}",
        PortableValueKind.UnsignedInteger => $"u{BitWidth}:{UnsignedInteger.ToString(CultureInfo.InvariantCulture)}",
        PortableValueKind.FloatingPoint => $"f{BitWidth}:{FloatingPointBits:X16}",
        PortableValueKind.Decimal => $"decimal:{string.Join(",", DecimalBits)}",
        PortableValueKind.String => $"string:{Convert.ToHexString(Encoding.Unicode.GetBytes(Text))}",
        PortableValueKind.DateTime => $"datetime:{Ticks}:{DateTimeKind}",
        PortableValueKind.DateTimeOffset => $"datetimeoffset:{Ticks}:{OffsetMinutes}",
        PortableValueKind.Guid => $"guid:{Convert.ToHexString(GuidBytes.ToArray())}",
        PortableValueKind.TimeSpan => $"timespan:{Ticks}",
        PortableValueKind.Enum => $"enum:{EnumTypeStableId}:{EnumUnderlyingValue?.ToManifestValue()}",
        _ => throw new ArgumentOutOfRangeException()
    };
}

internal sealed record PortableRow
{
    private readonly IReadOnlyDictionary<string, PortableValue> _values;

    public PortableRow(IEnumerable<KeyValuePair<string, PortableValue>> values)
    {
        var pairs = (values ?? throw new ArgumentNullException(nameof(values))).ToArray();
        if (pairs.Select(static pair => pair.Key).Distinct(StringComparer.Ordinal).Count() != pairs.Length)
            throw new ArgumentException("Portable row field names must be unique.", nameof(values));

        Fields = Array.AsReadOnly(pairs);
        _values = pairs.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
    }

    public IReadOnlyList<KeyValuePair<string, PortableValue>> Fields { get; }

    public PortableValue this[string fieldName] => _values.TryGetValue(fieldName, out var value)
        ? value
        : throw new KeyNotFoundException($"Portable row does not contain field '{fieldName}'.");

    public static PortableRow Create(params (string Name, PortableValue Value)[] values) =>
        new(values.Select(static value => new KeyValuePair<string, PortableValue>(value.Name, value.Value)));
}

internal abstract record PortableExpression;

internal sealed record PortableLiteralExpression(PortableValue Value) : PortableExpression;

internal sealed record PortableFieldExpression(string? Alias, string FieldName) : PortableExpression;

internal sealed record PortableParameterExpression(string Name) : PortableExpression;

internal sealed record PortableScriptVariableExpression(string Name) : PortableExpression;

internal sealed record PortableVariableExpression(string Name) : PortableExpression;

internal enum PortableBinaryOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    And,
    Or,
    Equal,
    NotEqual,
    IsDistinctFrom,
    IsNotDistinctFrom,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    StringConcatenate
}

internal enum PortableUnaryOperation
{
    Not,
    Negate
}

internal sealed record PortableBinaryExpression(
    PortableBinaryOperation Operation,
    PortableExpression Left,
    PortableExpression Right) : PortableExpression;

internal sealed record PortableUnaryExpression(PortableUnaryOperation Operation, PortableExpression Operand) : PortableExpression;

internal sealed record PortableNullCheckExpression(PortableExpression Expression, bool IsNegated) : PortableExpression;

internal sealed record PortableCoalesceExpression(IReadOnlyList<PortableExpression> Expressions) : PortableExpression;

internal sealed record PortableCaseBranch(PortableExpression Condition, PortableExpression Result);

internal sealed record PortableCaseExpression(
    IReadOnlyList<PortableCaseBranch> Branches,
    PortableExpression? ElseExpression) : PortableExpression;

internal sealed record PortableInExpression(
    PortableExpression Expression,
    IReadOnlyList<PortableExpression> Values) : PortableExpression;

internal sealed record PortableStrictCastExpression(
    PortableExpression Expression,
    string TargetTypeName) : PortableExpression;

internal sealed record PortableRowValue(string FieldName, PortableExpression Value);

internal sealed record PortableOrderField(string FieldName, bool Descending);

internal abstract record PortableInstruction;

internal sealed record PortableLoadSourceInstruction(
    string RowsVariable,
    string SourceContextId) : PortableInstruction;

internal sealed record PortableCreateTableInstruction(string TableVariable) : PortableInstruction;

internal sealed record PortableCreateValuesInstruction(
    string RowsVariable,
    IReadOnlyList<IReadOnlyList<PortableRowValue>> Rows) : PortableInstruction;

internal sealed record PortableForEachInstruction(
    string ItemVariable,
    string RowsVariable,
    PortableBlock Body) : PortableInstruction;

internal sealed record PortableLetInstruction(string Variable, PortableExpression Value) : PortableInstruction;

internal sealed record PortableIfInstruction(PortableExpression Condition, PortableBlock Body) : PortableInstruction;

internal sealed record PortableContinueInstruction : PortableInstruction;

internal sealed record PortableContinueIfInstruction(PortableExpression Condition) : PortableInstruction;

internal sealed record PortableAppendRowInstruction(
    string TableVariable,
    IReadOnlyList<PortableRowValue> Values) : PortableInstruction;

internal sealed record PortableCreateRowInstruction(
    string RowVariable,
    IReadOnlyList<PortableRowValue> Values) : PortableInstruction;

internal sealed record PortableOrderSliceInstruction(
    string SourceVariable,
    string TargetVariable,
    IReadOnlyList<PortableOrderField> Order,
    int Skip,
    int? Take) : PortableInstruction;

internal sealed record PortableReturnInstruction(string TableVariable) : PortableInstruction;

internal sealed record PortableBlock
{
    public PortableBlock(IEnumerable<PortableInstruction>? instructions)
    {
        Instructions = Array.AsReadOnly((instructions ?? []).ToArray());
    }

    public IReadOnlyList<PortableInstruction> Instructions { get; }
}

internal sealed record PortableSubsetProgram
{
    public PortableSubsetProgram(
        string planIdentifier,
        ExecutionSemanticsContract semanticsContract,
        PortableBlock body)
    {
        PlanIdentifier = string.IsNullOrWhiteSpace(planIdentifier)
            ? throw new ArgumentException("Plan identifier cannot be empty.", nameof(planIdentifier))
            : planIdentifier;
        SemanticsContract = semanticsContract ?? throw new ArgumentNullException(nameof(semanticsContract));
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }

    public string PlanIdentifier { get; }

    public ExecutionSemanticsContract SemanticsContract { get; }

    public int SemanticsVersion => SemanticsContract.Version;

    public string SemanticsFingerprint => SemanticsContract.Fingerprint;

    public PortableBlock Body { get; }

    public string CreateManifest()
    {
        var builder = new StringBuilder();
        builder.AppendLine("portable-subset-program:v1");
        builder.Append("plan=").AppendLine(PlanIdentifier);
        builder.Append("semantics=").AppendLine(SemanticsVersion.ToString(CultureInfo.InvariantCulture));
        builder.Append("semantics-fingerprint=").AppendLine(SemanticsFingerprint);
        AppendBlock(builder, Body, 0);
        return builder.ToString();
    }

    private static void AppendBlock(StringBuilder builder, PortableBlock block, int depth)
    {
        foreach (var instruction in block.Instructions)
        {
            builder.Append(' ', depth * 2).AppendLine(FormatInstruction(instruction));
            switch (instruction)
            {
                case PortableForEachInstruction loop:
                    AppendBlock(builder, loop.Body, depth + 1);
                    break;
                case PortableIfInstruction branch:
                    AppendBlock(builder, branch.Body, depth + 1);
                    break;
            }
        }
    }

    private static string FormatInstruction(PortableInstruction instruction) => instruction switch
    {
        PortableLoadSourceInstruction source => $"source {source.SourceContextId} -> {source.RowsVariable}",
        PortableCreateTableInstruction table => $"table {table.TableVariable}",
        PortableCreateValuesInstruction values => $"values {values.RowsVariable} x {values.Rows.Count}",
        PortableForEachInstruction loop => $"foreach {loop.ItemVariable} in {loop.RowsVariable}",
        PortableLetInstruction let => $"let {let.Variable} = {FormatExpression(let.Value)}",
        PortableIfInstruction branch => $"if {FormatExpression(branch.Condition)}",
        PortableContinueInstruction => "continue",
        PortableContinueIfInstruction condition => $"continue-if {FormatExpression(condition.Condition)}",
        PortableAppendRowInstruction append => $"append {append.TableVariable} ({FormatValues(append.Values)})",
        PortableCreateRowInstruction row => $"row {row.RowVariable} ({FormatValues(row.Values)})",
        PortableOrderSliceInstruction order => $"order-slice {order.SourceVariable} -> {order.TargetVariable};skip={order.Skip};take={order.Take?.ToString(CultureInfo.InvariantCulture) ?? "all"}",
        PortableReturnInstruction result => $"return {result.TableVariable}",
        _ => throw new InvalidOperationException($"Unknown portable instruction '{instruction.GetType().Name}'.")
    };

    private static string FormatValues(IEnumerable<PortableRowValue> values) =>
        string.Join(",", values.Select(static value => $"{value.FieldName}={FormatExpression(value.Value)}"));

    private static string FormatExpression(PortableExpression expression) => expression switch
    {
        PortableLiteralExpression literal => literal.Value.ToManifestValue(),
        PortableFieldExpression field => $"field:{field.Alias}.{field.FieldName}",
        PortableParameterExpression parameter => $"parameter:{parameter.Name}",
        PortableScriptVariableExpression variable => $"script-variable:{variable.Name}",
        PortableVariableExpression variable => $"variable:{variable.Name}",
        PortableBinaryExpression binary => $"({FormatExpression(binary.Left)} {FormatBinaryOperation(binary.Operation)} {FormatExpression(binary.Right)})",
        PortableUnaryExpression unary => $"{FormatUnaryOperation(unary.Operation)}({FormatExpression(unary.Operand)})",
        PortableNullCheckExpression nullCheck => $"null-check:{nullCheck.IsNegated}({FormatExpression(nullCheck.Expression)})",
        PortableCoalesceExpression coalesce => $"coalesce({string.Join(",", coalesce.Expressions.Select(FormatExpression))})",
        PortableCaseExpression @case => $"case:{@case.Branches.Count}",
        PortableInExpression @in => $"in({FormatExpression(@in.Expression)};{@in.Values.Count})",
        PortableStrictCastExpression cast => $"cast:{cast.TargetTypeName}({FormatExpression(cast.Expression)})",
        _ => throw new InvalidOperationException($"Unknown portable expression '{expression.GetType().Name}'.")
    };

    private static string FormatBinaryOperation(PortableBinaryOperation operation) => operation switch
    {
        PortableBinaryOperation.Add => "add",
        PortableBinaryOperation.Subtract => "subtract",
        PortableBinaryOperation.Multiply => "multiply",
        PortableBinaryOperation.Divide => "divide",
        PortableBinaryOperation.Modulo => "modulo",
        PortableBinaryOperation.And => "and",
        PortableBinaryOperation.Or => "or",
        PortableBinaryOperation.Equal => "equal",
        PortableBinaryOperation.NotEqual => "not-equal",
        PortableBinaryOperation.IsDistinctFrom => "is-distinct-from",
        PortableBinaryOperation.IsNotDistinctFrom => "is-not-distinct-from",
        PortableBinaryOperation.GreaterThan => "greater-than",
        PortableBinaryOperation.LessThan => "less-than",
        PortableBinaryOperation.GreaterOrEqual => "greater-or-equal",
        PortableBinaryOperation.LessOrEqual => "less-or-equal",
        PortableBinaryOperation.StringConcatenate => "string-concatenate",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };

    private static string FormatUnaryOperation(PortableUnaryOperation operation) => operation switch
    {
        PortableUnaryOperation.Not => "not",
        PortableUnaryOperation.Negate => "negate",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };
}

internal sealed record PortableSubsetRenderedArtifact(
    PortableSubsetProgram Program,
    TargetHostAbiInventory HostAbiInventory)
    : RenderedQueryArtifact(PortableSubsetTarget.TargetId);
