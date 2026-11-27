using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator;

internal readonly record struct InterpretationPresence(
    bool MayBeNull,
    bool VariableElements = false,
    int MinimumArrayLength = 0
)
{
    internal static InterpretationPresence From(ISchemaColumn column) =>
        column is InterpretationColumn interpreted ? interpreted.Presence : default;

    internal bool ContainsIndex(int index) => MinimumArrayLength > 0 && index < MinimumArrayLength;

    internal InterpretationPresence Merge(InterpretationPresence other) =>
        new(
            MayBeNull || other.MayBeNull,
            VariableElements || other.VariableElements,
            Math.Min(MinimumArrayLength, other.MinimumArrayLength)
        );
}

internal sealed class InterpretationColumn : SchemaColumn
{
    private InterpretationColumn(ISchemaColumn column, InterpretationPresence presence)
        : base(
            column.ColumnName,
            column.ColumnIndex,
            column.ColumnType,
            column.SourceReadType,
            column.EnumType,
            column.IntendedTypeName,
            column.ReadModifiers,
            column.Stability
        )
    {
        Presence = presence;
    }

    internal InterpretationPresence Presence { get; }

    internal static ISchemaColumn Copy(ISchemaColumn column, ISchemaColumn source) =>
        Preserve(column, InterpretationPresence.From(source));

    internal static ISchemaColumn Preserve(
        ISchemaColumn column,
        bool mayBeNull,
        bool variableElements = false,
        int minimumArrayLength = 0
    ) => Preserve(column, new InterpretationPresence(mayBeNull, variableElements, minimumArrayLength));

    internal static ISchemaColumn Preserve(ISchemaColumn column, InterpretationPresence presence)
    {
        if (column.IntendedTypeName == null && column.ColumnType.IsValueType)
            presence = presence with { MayBeNull = false };
        return presence == default ? column : new InterpretationColumn(column, presence);
    }
}
