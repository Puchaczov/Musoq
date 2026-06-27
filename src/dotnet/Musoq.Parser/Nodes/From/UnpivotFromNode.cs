using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes.From;

public class UnpivotFromNode : FromNode
{
    public const string DefaultAlias = "__unpivot";

    public UnpivotFromNode(
        FromNode source,
        string nameColumn,
        string valueColumn,
        IReadOnlyList<UnpivotEntryNode> entries,
        IReadOnlyList<FieldNode> keepFields)
        : base(DefaultAlias)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        NameColumn = nameColumn ?? throw new ArgumentNullException(nameof(nameColumn));
        ValueColumn = valueColumn ?? throw new ArgumentNullException(nameof(valueColumn));
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        KeepFields = keepFields ?? throw new ArgumentNullException(nameof(keepFields));
    }

    public UnpivotFromNode(
        FromNode source,
        string nameColumn,
        string valueColumn,
        IReadOnlyList<UnpivotEntryNode> entries,
        IReadOnlyList<FieldNode> keepFields,
        Type returnType)
        : base(DefaultAlias, returnType)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        NameColumn = nameColumn ?? throw new ArgumentNullException(nameof(nameColumn));
        ValueColumn = valueColumn ?? throw new ArgumentNullException(nameof(valueColumn));
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        KeepFields = keepFields ?? throw new ArgumentNullException(nameof(keepFields));
    }

    public FromNode Source { get; }

    public string NameColumn { get; }

    public string ValueColumn { get; }

    public IReadOnlyList<UnpivotEntryNode> Entries { get; }

    public IReadOnlyList<FieldNode> KeepFields { get; }

    public override string Id => $"{nameof(UnpivotFromNode)}{Source.Id}{NameColumn}{ValueColumn}{string.Join(string.Empty, Entries.Select(entry => entry.Id))}{string.Join(string.Empty, KeepFields.Select(keepField => keepField.Id))}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var keep = KeepFields.Count == 0 ? string.Empty : $" keep {string.Join(", ", KeepFields.Select(field => field.ToString()))}";
        return $"{Source} unpivot on {NameColumn} in ({string.Join(", ", Entries.Select(entry => entry.ToString()))}) using {ValueColumn}{keep}";
    }
}
