namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    internal const string Header = """
        binary Header {
            Kind: byte,
            Value: int le,
            Count: byte,
            Tail: byte[Count]
        };

        """;
    internal const string Entry = "text Entry { Level: until '|', Number: until '|', Label: rest }; ";
    internal const string NestedPacket = """
        binary Body { Value: int le };
        binary Packet {
            Kind: byte,
            Body: substream[4] as Body exact,
            Count: byte,
            Tail: byte[Count]
        };

        """;

    private static IEnumerable<Scenario> Interpretation() =>
    [
        new("B23_Parameters", "Contracts", "Typed script parameters in a source filter.",
            """
            param(minimum: int = 1); select r.Id as Id
            from bench.rows() r
            where r.Id >= $minimum
            order by r.Id
            """, "core/4.3"),
        new("B23_StructuredInput", "Contracts", "Closed record arrays and named source arguments.",
            """
            with inputs as (select r.Id as Id, r.Amount as Amount
            from bench.rows() r
            where r.Id % 2 = 1) select s.Id as Id, s.Amount as Amount
            from bench.structured(items: inputs) s
            order by s.Id
            """, "core/4.5"),
        new("B25_Couple", "Contracts", "Reordered subset contract on a typed source.",
            """
            table Selected { Name: string, Id: int }; couple bench.rows with table Selected as SelectedRows; select r.Id as Id, r.Name as Name
            from SelectedRows() r
            order by r.Id
            """, "table-couple/3,4"),
        new("B26_AcceptedPredicate", "Contracts", "Provider applies the exact accepted prefix predicate.",
            "select r.Id as Id from bench.planned() r where r.Name like 'item-1%' order by r.Id", "core/6,7"),
        new("B26_ResidualPredicate", "Contracts", "Same predicate remains in the engine for an ordinary source.",
            "select r.Id as Id from bench.rows() r where r.Name like 'item-1%' order by r.Id", "core/6,7"),
        new("B27_BinaryHeader", "Interpretation", "Fixed little-endian header and sized payload.",
            Header + """
            select r.Id as Id, h.Value as Value, h.Kind as Kind
            from bench.rows() r
            cross apply Interpret<Header>(r.Payload) h
            order by r.Id
            """, "binary-text/4.2"),
        new("B28_BinarySubstream", "Interpretation", "Nested exact bounded substream decoding.",
            NestedPacket + """
            select r.Id as Id, p.Body.Value as Value
            from bench.rows() r
            cross apply Interpret<Packet>(r.Payload) p
            order by r.Id
            """, "binary-text/4.13"),
        new("B28_BinarySwitch", "Interpretation", "Tagged binary branch with a raw fallback.",
            """
            binary Body { Value: int le }; binary Tagged { Kind: byte, Body: switch Kind { 0 => Known: Body, _ => Raw: byte[4] } }; select r.Id as Id, p.Body.Case as Kind
            from bench.rows() r
            cross apply Interpret<Tagged>(r.Payload) p
            order by r.Id
            """, "binary-text/4.12"),
        new("B29_BinaryRepeat", "Interpretation", "EOF repetition after the fixed header.",
            """
            binary Repeated { Kind: byte, Value: int le, Count: byte, Tail: byte repeat until eof }; select r.Id as Id, Length(p.Tail) as Count
            from bench.rows() r
            cross apply Interpret<Repeated>(r.Payload) p
            order by r.Id
            """, "binary-text/4.10"),
        new("B29_InterpretAt", "Interpretation", "Decode a scalar at a fixed supplied offset.",
            """
            binary Scalar { Value: int le }; select r.Id as Id, p.Value as Value
            from bench.rows() r
            cross apply InterpretAt<Scalar>(r.Payload, 1) p
            order by r.Id
            """, "binary-text/7.6"),
        new("B30_TextDelimited", "Interpretation", "Delimiter capture and rest-of-input.",
            Entry + """
            select r.Id as Id, p.Level as Level, p.Label as Label
            from bench.rows() r
            cross apply Parse<Entry>(r.Text) p
            order by r.Id
            """, "binary-text/5"),
        new("B31_TextCaptures", "Interpretation", "Typed named regex captures with explicit capture order.",
            """
            text Captured {
                Fields: pattern r'(?<Level>[A-Z]+)\|(?<Number>[0-9]+)\|(?<Label>.*)' capture (Level, Number, Label)
            };
            select r.Id as Id, p.Fields.Label as Label
            from bench.rows() r
            cross apply Parse<Captured>(r.Text) p
            order by r.Id
            """, "binary-text/5.2"),
        new("B32_TryParse", "Interpretation", "Tolerant parsing preserves alternating malformed rows.",
            """
            text Valid { Prefix: literal 'INFO|', Message: rest }; select r.Id as Id, p.Message as Message
            from bench.rows() r
            outer apply TryParse<Valid>(case when r.Id % 2 = 0 then r.Text else 'bad' end) p
            order by r.Id
            """, "binary-text/7.5"),
        new("B32_Partial", "Interpretation", "Truncated integer retains completed byte and terminal cursor.",
            Header + """
            select r.Id as Id, p.ParsedFields.Kind as Kind, p.BytesConsumed as Consumed, p.ErrorField as Error
            from bench.rows() r
            cross apply PartialInterpret<Header>(r.Truncated) p
            order by r.Id
            """, "binary-text/7.8")
    ];
}
