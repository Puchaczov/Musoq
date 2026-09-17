using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.StructuredSamples;

public readonly struct StructuredWideRow
{
    public StructuredWideRow(int value) => Value = value;

    public int Value { get; }
}

public sealed class StructuredWideInput
{
    public int F01 { get; }
    public int F02 { get; }
    public int F03 { get; }
    public int F04 { get; }
    public int F05 { get; }
    public int F06 { get; }
    public int F07 { get; }
    public int F08 { get; }
    public int F09 { get; }
    public int F10 { get; }
    public int F11 { get; }
    public int F12 { get; }
    public int F13 { get; }
    public int F14 { get; }
    public int F15 { get; }
    public int F16 { get; }
    public int F17 { get; }
    public int F18 { get; }
    public int F19 { get; }
    public int F20 { get; }
    public int F21 { get; }
    public int F22 { get; }
    public int F23 { get; }
    public int F24 { get; }
    public int F25 { get; }
    public int F26 { get; }
    public int F27 { get; }
    public int F28 { get; }
    public int F29 { get; }
    public int F30 { get; }
    public int F31 { get; }
    public int F32 { get; }
    public int F33 { get; }
    public int F34 { get; }
    public int F35 { get; }
    public int F36 { get; }
    public int F37 { get; }
    public int F38 { get; }
    public int F39 { get; }
    public int F40 { get; }
    public int F41 { get; }
    public int F42 { get; }
    public int F43 { get; }
    public int F44 { get; }
    public int F45 { get; }
    public int F46 { get; }
    public int F47 { get; }
    public int F48 { get; }
    public int F49 { get; }
    public int F50 { get; }
    public int F51 { get; }
    public int F52 { get; }
    public int F53 { get; }
    public int F54 { get; }
    public int F55 { get; }
    public int F56 { get; }
    public int F57 { get; }
    public int F58 { get; }
    public int F59 { get; }
    public int F60 { get; }
    public int F61 { get; }
    public int F62 { get; }
    public int F63 { get; }
    public int F64 { get; }
    public int F65 { get; }

    public StructuredWideInput(
        int F01 = 0,
        int F02 = 0,
        int F03 = 0,
        int F04 = 0,
        int F05 = 0,
        int F06 = 0,
        int F07 = 0,
        int F08 = 0,
        int F09 = 0,
        int F10 = 0,
        int F11 = 0,
        int F12 = 0,
        int F13 = 0,
        int F14 = 0,
        int F15 = 0,
        int F16 = 0,
        int F17 = 0,
        int F18 = 0,
        int F19 = 0,
        int F20 = 0,
        int F21 = 0,
        int F22 = 0,
        int F23 = 0,
        int F24 = 0,
        int F25 = 0,
        int F26 = 0,
        int F27 = 0,
        int F28 = 0,
        int F29 = 0,
        int F30 = 0,
        int F31 = 0,
        int F32 = 0,
        int F33 = 0,
        int F34 = 0,
        int F35 = 0,
        int F36 = 0,
        int F37 = 0,
        int F38 = 0,
        int F39 = 0,
        int F40 = 0,
        int F41 = 0,
        int F42 = 0,
        int F43 = 0,
        int F44 = 0,
        int F45 = 0,
        int F46 = 0,
        int F47 = 0,
        int F48 = 0,
        int F49 = 0,
        int F50 = 0,
        int F51 = 0,
        int F52 = 0,
        int F53 = 0,
        int F54 = 0,
        int F55 = 0,
        int F56 = 0,
        int F57 = 0,
        int F58 = 0,
        int F59 = 0,
        int F60 = 0,
        int F61 = 0,
        int F62 = 0,
        int F63 = 0,
        int F64 = 0,
        int F65 = 0)
    {
        this.F01 = F01;
        this.F02 = F02;
        this.F03 = F03;
        this.F04 = F04;
        this.F05 = F05;
        this.F06 = F06;
        this.F07 = F07;
        this.F08 = F08;
        this.F09 = F09;
        this.F10 = F10;
        this.F11 = F11;
        this.F12 = F12;
        this.F13 = F13;
        this.F14 = F14;
        this.F15 = F15;
        this.F16 = F16;
        this.F17 = F17;
        this.F18 = F18;
        this.F19 = F19;
        this.F20 = F20;
        this.F21 = F21;
        this.F22 = F22;
        this.F23 = F23;
        this.F24 = F24;
        this.F25 = F25;
        this.F26 = F26;
        this.F27 = F27;
        this.F28 = F28;
        this.F29 = F29;
        this.F30 = F30;
        this.F31 = F31;
        this.F32 = F32;
        this.F33 = F33;
        this.F34 = F34;
        this.F35 = F35;
        this.F36 = F36;
        this.F37 = F37;
        this.F38 = F38;
        this.F39 = F39;
        this.F40 = F40;
        this.F41 = F41;
        this.F42 = F42;
        this.F43 = F43;
        this.F44 = F44;
        this.F45 = F45;
        this.F46 = F46;
        this.F47 = F47;
        this.F48 = F48;
        this.F49 = F49;
        this.F50 = F50;
        this.F51 = F51;
        this.F52 = F52;
        this.F53 = F53;
        this.F54 = F54;
        this.F55 = F55;
        this.F56 = F56;
        this.F57 = F57;
        this.F58 = F58;
        this.F59 = F59;
        this.F60 = F60;
        this.F61 = F61;
        this.F62 = F62;
        this.F63 = F63;
        this.F64 = F64;
        this.F65 = F65;
    }
}
public sealed class StructuredWideSource : RowSource<StructuredWideRow>
{
    private readonly IReadOnlyList<StructuredWideInput> _items;
    private readonly SourceExecutionContext _context;

    public StructuredWideSource(IReadOnlyList<StructuredWideInput> items, SourceExecutionContext context)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override IEnumerable<IReadOnlyList<StructuredWideRow>> Chunks
    {
        get
        {
            _context.EndWorkToken.ThrowIfCancellationRequested();
            yield return [new StructuredWideRow(_items.Count == 0 ? 0 : _items[0].F65)];
        }
    }
}