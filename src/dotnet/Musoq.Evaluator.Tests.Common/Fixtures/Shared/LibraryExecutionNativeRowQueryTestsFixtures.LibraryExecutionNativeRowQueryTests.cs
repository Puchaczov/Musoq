using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNativeRowQueryTestsFixtures
{

    public const string CompositeJoinQuery = "select a.Position as LeftPosition, b.Position as RightPosition " +
        "from #schema.first() a inner join #schema.first() b on a.Number = b.Number and a.Text = b.Text " +
        "order by LeftPosition, RightPosition";

    public const string NativeTypedGroupingQuery = "select Number, Count(Position) as Total from #schema.first() group by Number";

    public readonly record struct NativeSubtotal(int? Number, long Total);

    internal struct NumericVisitor : IColumnValueVisitor
    {
        public long Total;
        public void Visit<T>(int columnIndex, in T value)
        {
            if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected concrete int column.");
            Total += System.Runtime.CompilerServices.Unsafe.As<T, int>(ref System.Runtime.CompilerServices.Unsafe.AsRef(in value));
        }
    }

    public const string NativeSignalQuery = "select p.Position as Position, p.Signals.Temperature as Temperature " +
        "from #schema.first() p order by Position";
    public static NativeSignalInput[] NativeSignalRows() =>
    [
        new(1, new System.Collections.Generic.Dictionary<string, double?> { ["Temperature"] = 90d }),
        new(2, new System.Collections.Generic.Dictionary<string, double?>()),
        new(3, null)
    ];

    public sealed record NativeSignalInput(int Position, System.Collections.Generic.IReadOnlyDictionary<string, double?>? Signals);

    public sealed class OpaqueLibrary : Musoq.Plugins.LibraryBase
    {
        [Musoq.Plugins.Attributes.BindableMethod]
        public string PayloadText(OpaquePayload payload) => payload.Value;
        [Musoq.Plugins.Attributes.BindableMethod]
        public System.Collections.Generic.IEnumerable<OpaquePayload> Payloads() => [new("typed apply", 42)];
    }

    public sealed record OpaqueInput(OpaquePayload Payload);
    public sealed record OpaquePayload(string Value, object Unresolved);

    public const string NativePayloadQuery = "select p.Position as Position, p.Payload.Value as Value " +
        "from #positional.all() p order by Position";

    public sealed record NativePayload(decimal? Value);
    public static Musoq.Tests.Common.Schema.PositionalRowsSchemaProvider PayloadProvider() => new(
        [new Musoq.Schema.DataSources.SchemaColumn("Payload", 0, typeof(NativePayload)),
         new Musoq.Schema.DataSources.SchemaColumn("Position", 1, typeof(int))],
        [[new NativePayload(7m), 1], [null!, 2]]);

    public sealed record NativeKeyInput(int Position, int? Number, string? Text, int? Other);
    public static NativeKeyInput[] Rows() =>
    [
        new(1, 1, "A", 3), new(2, 1, "A", 3), new(3, 2, "A", 3), new(4, null, "A", 2),
        new(5, 1, "B", 4), new(6, 1, "a", 3), new(7, null, null, null)
    ];

}
