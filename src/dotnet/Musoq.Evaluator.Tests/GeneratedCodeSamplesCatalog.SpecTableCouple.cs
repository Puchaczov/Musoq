using System;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationTableCoupleSamples()
    {
        return
        [
            TableCoupleSample(
                "Q319_SpecTableTypeMatrix",
                @"
                    table TypeMatrix {
                        ByteCol: byte,
                        SByteCol: sbyte,
                        ShortCol: short,
                        IntCol: Int,
                        LongCol: long,
                        UShortCol: ushort,
                        UIntCol: uint,
                        ULongCol: ulong,
                        FloatCol: float,
                        DoubleCol: double,
                        DecimalCol: decimal,
                        MoneyCol: money,
                        BoolCol: boolean,
                        BitCol: bit,
                        CharCol: char,
                        StringCol: STRING,
                        DateTimeCol: datetime,
                        DateTimeOffsetCol: datetimeoffset?,
                        TimeSpanCol: timespan,
                        GuidCol: guid,
                        ObjectCol: object,
                        FullyQualified: System.Int32,
                        NullableInt: int?,
                    };
                    couple #unknown.rows with table TypeMatrix as Typed;
                    select ByteCol, SByteCol, ShortCol, IntCol, LongCol, UShortCol, UIntCol,
                        ULongCol, FloatCol, DoubleCol, DecimalCol, MoneyCol, BoolCol, BitCol,
                        CharCol, StringCol, DateTimeCol, DateTimeOffsetCol, TimeSpanCol, GuidCol,
                        ObjectCol,
                        FullyQualified, NullableInt
                    from Typed()",
                CreateTypedTypeMatrixSampleProvider) with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
            },
            TableCoupleSample(
                "Q320_SpecTableReadModifiers",
                @"
                    table LegacyInvoiceRow {
                        InvoiceNo: string encoding 'windows-1250' trim,
                        CustomerName: string encoding 'windows-1250' trim,
                        Total: decimal culture 'pl-PL' format '#,##0.00',
                        Attachment: string source codec 'base64',
                    };
                    couple #readmods.records with table LegacyInvoiceRow as Invoices;
                    select InvoiceNo, CustomerName, Total, Attachment
                    from Invoices()",
                CreateTypedReadModifiersSampleProvider),
            TableCoupleSample(
                "Q321_SpecTableCoupleArguments",
                @"
                    param (label: string = 'parameter');
                    table NamedArgs {
                        Value: int,
                        First: string,
                        Second: int
                    };
                    table InputShape { Text: string };
                    couple named.any with table NamedArgs as Data;
                    couple #unknown.others with table InputShape as Forward;
                    with Input as (
                        select 'cte' as Text from #unknown.anything()
                    )
                    select d.First, d.Second, p.First, p.Second, c.Text
                    from Data('positional') d
                    cross join Data(second: 4, first: $label) p
                    cross join Forward(Input) c",
                CreateTableCoupleArgumentsSampleProvider),
            TableCoupleSampleWithOptions(
                "Q322_SpecTableSettingsProfiles",
                @"
                    table SettingsRow { Token: string };
                    couple #settings.items with settings blue as SettingsOnly;
                    couple #settings.items with table SettingsRow and settings red as TableFirst;
                    couple #settings.items with settings green and table SettingsRow as SettingsFirst;
                    select a.Token, b.Token, c.Token
                    from SettingsOnly() a
                    cross join TableFirst() b
                    cross join SettingsFirst() c",
                CreateSettingsProfileCompilationOptions()),
            TableCoupleSample(
                "Q323_SpecTableCoupleComposition",
                @"
                    table Row {
                        Id: int,
                        Name: string,
                        Population: decimal
                    };
                    couple #A.entities with table Row as LeftRows;
                    couple #B.entities with table Row as RightRows;
                    with Expanded as (
                        select l.Id, l.Name, l.Population
                        from LeftRows() l
                        cross apply RightRows() r
                        where l.Id = r.Id
                    ), Joined as (
                        select e.Name, e.Population
                        from Expanded e
                        inner join RightRows() r on e.Name = r.Name
                    )
                    select Name, Sum(Population) as Total
                    from Joined
                    group by Name
                    union (Name)
                    select Name, Sum(Population) as Total
                    from LeftRows()
                    group by Name")
        ];
    }

    private static GeneratedCodeSample TableCoupleSample(string name, string query)
    {
        return TableCoupleSample(name, query, CreateTableCoupleSampleProvider);
    }

    private static GeneratedCodeSample TableCoupleSample(
        string name,
        string query,
        Func<Musoq.Schema.ISchemaProvider> createSchemaProvider)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "Scan",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = createSchemaProvider
        };
    }

    private static GeneratedCodeSample TableCoupleSampleWithOptions(
        string name,
        string query,
        CompilationOptions compilationOptions)
    {
        return TableCoupleSample(name, query, CreateSettingsProfileSampleProvider) with
        {
            CompilationOptions = compilationOptions
        };
    }
}
