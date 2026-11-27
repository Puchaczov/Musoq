using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed record TableRowShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];
    private IReadOnlyList<FieldBinding> _contexts = [];

    public TableRowShape(
        string alias,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<FieldBinding> contexts,
        string? generatedRowTypeName = null
    )
        : base(alias, fields)
    {
        Alias = alias;
        Fields = fields;
        Contexts = contexts;
        GeneratedRowTypeName = generatedRowTypeName;
    }

    internal static TableRowShape FromSchema(string alias, OutputSchema schema) =>
        new TableRowShape(
            alias,
            schema
                .Columns.Select(column =>
                    new FieldBinding(
                        column.Name,
                        $"{alias}.{column.Name}",
                        column.Index,
                        column.Type,
                        FieldNullability.Unknown,
                        new PositionalAccess(column.Index, ExecutionClrBindingFactory.FromClr(column.Type)),
                        sourceReadType: column.SourceReadType,
                        enumType: column.EnumType
                    ) with
                    {
                        Stability = column.Stability,
                        GeneratedTypeName = string.IsNullOrEmpty(column.IntendedTypeName)
                            ? null
                            : column.IntendedTypeName,
                    }
                )
                .ToArray()
        );

    public string Alias { get; init; }

    public override IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }

    public IReadOnlyList<FieldBinding> Contexts
    {
        get => _contexts;
        init => _contexts = ExecutionIrCollections.Freeze(value);
    }

    public string? GeneratedRowTypeName { get; init; }
    internal bool StoreOwnRowPresence { get; init; }

    public TableRowShape(string alias, IReadOnlyList<FieldBinding> fields)
        : this(alias, fields, []) { }
}
