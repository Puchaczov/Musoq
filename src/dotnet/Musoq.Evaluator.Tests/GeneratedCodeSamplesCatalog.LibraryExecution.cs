using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateLibraryExecutionSamples() =>
    [
        .. CreateLibraryExecutionCleanupSamples(),

        new GeneratedCodeSample
        {
            Name = "K45_TypedTextSwitchResult", FileName = "K45_TypedTextSwitchResult.cs",
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false).WithTableResultMaterialization(),
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "text Arm { Payload: rest }; text Packet { Variant: switch { _ => Arm } }; select p.Variant as Variant from #schema.first() s cross apply Parse<Packet>(s.Line) p",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionProviderQueryTests.SwitchLine, Musoq.Plugins.LibraryBase>([new(1, "value")])
        },
        new GeneratedCodeSample
        {
            Name = "K44_TypedStateRangeOrderCapture", FileName = "K44_TypedStateRangeOrderCapture.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Sum(Value) over (order by CaptureOrderKey(Position) range between 1 preceding and current row) as Total from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionWindowQueryTests.NumericRow<int>, LibraryExecutionWindowQueryTests.CleanupLibrary>([new(1, 10), new(2, 20), new(3, 30)])
        },
        new GeneratedCodeSample
        {
            Name = "K40_TypedOrderKeyContext", FileName = "K40_TypedOrderKeyContext.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, KeyWeighted(Value) over (order by Position rows between 1 preceding and current row) as Total from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionWindowQueryTests.NumericRow<int>, LibraryExecutionWindowQueryTests.OrderKeyLibrary>([new(1, 10), new(2, 20), new(3, 30)])
        },
        new GeneratedCodeSample
        {
            Name = "K41_TypedCompositeOrderKeyContext", FileName = "K41_TypedCompositeOrderKeyContext.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Partition, Position, FirstLabel(Value) over (partition by Partition order by Position, Label rows between unbounded preceding and current row) as Label from #schema.first() order by Partition, Position, Label",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionWindowQueryTests.OrderKeyRow, LibraryExecutionWindowQueryTests.OrderKeyLibrary>([new(1, 1, 10, "a"), new(1, 2, 20, "b")])
        },
        new GeneratedCodeSample
        {
            Name = "K42_InvoiceLibraryGrouped", FileName = "K42_InvoiceLibraryGrouped.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Customer, Sum(Amount) as Total, PositiveCount(Amount) as Positive, Avg(Quantity) as AverageQuantity from #schema.first() group by Customer order by Customer",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionDocumentationQueryTests.Invoice, Musoq.TypedLibraryExamples.InvoiceLibrary>(LibraryExecutionDocumentationQueryTests.Rows())
        },
        new GeneratedCodeSample
        {
            Name = "K43_InvoiceLibraryWindowed", FileName = "K43_InvoiceLibraryWindowed.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Sum(Amount) over (partition by Customer order by Position rows between unbounded preceding and current row) as RunningTotal from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionDocumentationQueryTests.Invoice, Musoq.TypedLibraryExamples.InvoiceLibrary>(LibraryExecutionDocumentationQueryTests.Rows())
        },
        new GeneratedCodeSample
        {
            Name = "K38_TypedProviderAggregate", FileName = "K38_TypedProviderAggregate.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Sum(Amount) as Total, Avg(Amount) as Average, Count(Amount) as Present from #typedrows.rows()",
            CreateSchemaProvider = static () => new LibraryExecutionQueryRowProvider()
        },
        new GeneratedCodeSample
        {
            Name = "K39_TypedProviderJoinedWindow", FileName = "K39_TypedProviderJoinedWindow.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select a.Id, Sum(a.Amount) over (order by a.Id) as Total, RowNumber() over (order by a.Id) as Number " +
                    "from #typedrows.rows() a inner join #typedrows.rows() b on a.Id = b.Id order by a.Id",
            CreateSchemaProvider = static () => new LibraryExecutionQueryRowProvider()
        },
        PermissionSample("K33_AuthorizedTypedCountMerge", "select Partition, PermittedCount(Integral) as Total from #schema.first() group by Partition",
            Musoq.Schema.Optimization.SourceExecutionPermissions.ConcurrentRowProcessing),
        PermissionSample("K34_CustomCountSerialDefault", "select Partition, SerialCount(Integral) as Total from #schema.first() group by Partition",
            Musoq.Schema.Optimization.SourceExecutionPermissions.ConcurrentRowProcessing),
        PermissionSample("K35_CheckedSumWithoutMergeEquivalence", "select Partition, Sum(Integral) as Total from #schema.first() group by Partition",
            Musoq.Schema.Optimization.SourceExecutionPermissions.ConcurrentRowProcessing),
        PermissionSample("K36_AuthorizedScalarProjection", "select ToUpper(Partition) as Partition from #schema.first()",
            Musoq.Schema.Optimization.SourceExecutionPermissions.ConcurrentRowProcessing),
        PermissionSample("K37_SerialDatasourceProjection", "select ToUpper(Partition) as Partition from #schema.first()",
            Musoq.Schema.Optimization.SourceExecutionPermissions.None),
        new GeneratedCodeSample
        {
            Name = "K31_TypedWindowCapturedVolatileInputs", FileName = "K31_TypedWindowCapturedVolatileInputs.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Sum(NextValue()) filter (where AcceptRow()) over (order by Position rows between 2 preceding and current row) as Total from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
                LibraryExecutionWindowQueryTests.VolatileWindowRow, LibraryExecutionWindowQueryTests.VolatileWindowLibrary>(
                [new(1), new(2), new(3), new(4)])
        },
        LibraryExecution("K32_TypedWindowExtremaBatch", """
            select Position, Min(Amount) over (partition by Partition order by Position
                rows between 2 preceding and current row) as Minimum,
                Max(Floating) over (partition by Partition order by Position
                    rows between 1 preceding and 1 following) as Maximum,
                Min(Integral) over (partition by Partition order by Integral
                    range between unbounded preceding and current row) as PeerMinimum
            from #schema.first() order by Position
            """),
        LibraryExecution("K28_TypedWindowValueContext", """
            select Position, FirstValue(Amount) over (partition by Partition order by Position
                    rows between 1 preceding and current row) as First,
                NthValue(Amount, Position) over (partition by Partition order by Position) as Nth,
                Lag(Amount, 1, 0) over (partition by Partition order by Position) as Previous,
                Ntile(2) over (partition by Partition order by Position) as Tile
            from #schema.first() order by Position
            """),
        LibraryExecution("K29_TypedWindowRankingContext", """
            select Position, RowNumber() over (partition by Partition order by Integral) as PositionRank,
                Rank() over (partition by Partition order by Integral) as Rank,
                DenseRank() over (partition by Partition order by Integral) as Dense,
                PercentRank() over (partition by Partition order by Integral) as Percent,
                CumeDist() over (partition by Partition order by Integral) as Distribution
            from #schema.first() order by Position
            """),
        LibraryExecution("K30_TypedWindowOutputValueCarrier", """
            select Position, Sum(Amount) over (partition by Partition order by Position
                rows between 1 preceding and current row) as Total
            from #schema.first() order by Position
            """) with { OutputType = typeof(LibraryExecutionWindowQueryTests.WindowOutput) },
        LibraryExecution("K23_TypedNumericWindows", """
            select Position, Sum(Integral) over (order by Position) as Integers,
                Avg(Integral) over (order by Position) as Average,
                Sum(Floating) over (order by Position) as Doubles,
                Count(Amount) over (partition by Partition) as Values
            from #schema.first() order by Position
            """),
        LibraryExecution("K24_CustomWindowFrameArguments", """
            select Position, ScaledTotal(Amount, Position) over (partition by Partition order by Position
                rows between 1 preceding and current row) as Total,
                SeededTotal(Amount) over (partition by Partition order by Position) as Seeded
            from #schema.first() order by Position
            """),
        LibraryExecution("K25_TypedEmptyWindowFrames", """
            select Position, Sum(Amount) over (order by Position rows between 1 following and 1 following) as Total,
                Count(*) over (order by Position rows between 1 following and 1 following) as Rows
            from #schema.first() order by Position
            """),
        LibraryExecution("K26_TypedDistinctWindowFrames", """
            select Position, SumDistinct(Integral) over (order by Position rows between 2 preceding and current row) as Total,
                AvgDistinct(Integral) over (order by Position rows between 2 preceding and current row) as Average,
                CountDistinct(Integral) over (order by Position rows between 2 preceding and current row) as Values
            from #schema.first() order by Position
            """),
        LibraryExecution("K27_TypedRangeWindowPeers", """
            select Position, Sum(Amount) over (order by Integral range between current row and current row) as Total,
                Avg(Integral) over (order by Integral range between unbounded preceding and current row) as Average
            from #schema.first() order by Position
            """),
        LibraryExecution("K22_CustomScalarSubqueryEmptyReset", """
            select (select SeededTotal(Amount) from #schema.first() where Position < 0) as Total
            from #schema.first() where Position = 1
            """),
        LibraryExecution("K21_TypedReferenceKernelState", """
            select Partition, ReferencePositiveCount(Amount) as Positive
            from #schema.first() group by Partition order by Partition
            """),
        LibraryExecution("K17_IntegralDecimalAverage", """
            select Partition, Sum(Integral) as Total, Avg(Integral) as Average,
                Avg(distinct Integral) as UniqueAverage from #schema.first()
            group by Partition order by Partition
            """),
        LibraryExecution("K18_CustomKernelReset", """
            select Partition, SeededTotal(Amount) as Total, PositiveCount(Amount) as Positive
            from #schema.first() group by Partition order by Partition
            """),
        LibraryExecution("K19_ClosedGenericCustomAggregate", """
            select GenericTotal(Integral) as Integers, GenericTotal(Amount) as Decimals from #schema.first()
            """),
        LibraryExecution("K20_FilteredDecimalAverage", """
            select Avg(Integral) filter (where Position > 1) as Average,
                Count(*) filter (where Position > 1) as Rows from #schema.first()
            """),
        LibraryExecution("K01_LibraryMixedNumericGroups", """
            select Partition, Sum(Integral) as Integers, Sum(Amount) as Decimals,
                   Sum(Floating) as Doubles, Count(Amount) as Values
            from #schema.first() group by Partition order by Partition
            """),
        LibraryExecution("K02_LibraryCustomMultiArgumentGroups", """
            select Partition, PositiveTotal(Amount) as Positive,
                   ScaledTotal(Amount, 2) as Scaled, Sum(Amount) as Total
            from #schema.first() group by Partition order by Partition
            """),
        LibraryExecution("K03_LibraryRunningWindow", """
            select Position, Sum(Amount) over (
                partition by Partition order by Position
                rows between unbounded preceding and current row) as Total,
                RowNumber() over (partition by Partition order by Position) as Number
            from #schema.first() order by Position
            """),
        LibraryExecution("K04_LibraryCompositeNullableGroups", """
            select Partition, Integral, Sum(Amount) as Total, Count(*) as Rows
            from #schema.first() group by Partition, Integral order by Partition, Integral
            """),
        LibraryExecution("K05_LibraryGroupedWindowPipeline", """
            with totals as (
                select Partition, Sum(Amount) as Total from #schema.first() group by Partition
            )
            select Partition, Total,
                   Sum(Total) over (order by Partition) as RunningTotal
            from totals order by Partition
            """),
        LibraryExecution("K06_LibraryPublicFunctionDiscovery", "desc functions #schema"),
        LibraryExecution("K07_LibraryCorrelatedScalarAggregate", """
            select a.Position,
                   (select Sum(b.Amount) from #schema.first() b
                    where b.Partition = a.Partition) as Total
            from #schema.first() a order by a.Position
            """),
        LibraryExecution("K08_TypedScalarParameterSlots", """
            param(factor: int, bonus: decimal?);
            select Position + $factor as Position, Amount + $bonus as Amount
            from #schema.first() order by Position
            """),
        Structured("K09_TypedNestedJsonParameter", "LibraryExecution", """
            param(options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int = 3)));
            select Enabled, Before, After from #inputs.configure(options: $options)
            """),
        Structured("K10_TypedNestedJsonCollections", "LibraryExecution", """
            param(values: int[][]);
            select Value, Row, Column from #inputs.matrix(values: $values) order by Row, Column
            """),
        LibraryExecution("K11_TypedReservedColumnAliases", """
            select Position as [case], Amount as [decimal], Position as T,
                   Amount as GetValue, Partition as VisitColumns from #schema.first() order by Position
            """),
        Structured("K12_TypedStructuredDeclarationDefaults", "LibraryExecution", """
            param(offset: int, options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int = 3)) =
                  (Enabled: true, Codes: array { 10 }, Window: (Before: 2)));
            select Enabled, Before + $offset as Before, After from #inputs.configure(options: $options)
            """),
        LibraryExecution("K13_TypedJoinedSourceContexts", """
            select a.Position, b.SourcePosition() as OtherPosition, b.Amount
            from #schema.first() a left outer join #schema.first() b
                on a.Position = b.Position and b.Position > 2 order by a.Position
            """),
        LibraryExecution("K15_TypedNumericScalarTypeName", """
            select Position, GetTypeName(Position) as PositionType, GetTypeName(Integral) as IntegralType
            from #schema.first() order by Position
            """),
        new GeneratedCodeSample
        {
            Name = "K16_TypedIndexedValues",
            FileName = "K16_TypedIndexedValues.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = """
                select Position, Numbers[-1] as Last, Numbers[8] as Missing,
                    Amounts[1] as NullableAmount, Costs['present'] as Cost, Costs['missing'] as MissingCost
                from #schema.first() order by Position
                """,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionRelationalQueryTests.IndexedEntity>(
                [new(1, [2, 3], [1m, null], new System.Collections.Generic.Dictionary<string, decimal> { ["present"] = 4m })])
        },



        new GeneratedCodeSample
        {
            Name = "K48_TypedNullableArithmeticFilter",
            FileName = "K48_TypedNullableArithmeticFilter.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Value from #schema.first() where Value * 2 > 100 or Value % 3 = 1 order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionProviderQueryTests.NullableArithmeticInput>(
                [new(1, null), new(2, 100), new(3, 7)])
        },
        new GeneratedCodeSample
        {
            Name = "K47_TypedIndexedMembers",
            FileName = "K47_TypedIndexedMembers.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionProviderQueryTests.IndexedMembersQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionProviderQueryTests.IndexedParent>(
                [new(1, [new(3), new(7)], new System.Collections.Generic.Dictionary<string, LibraryExecutionProviderQueryTests.IndexedChild>())])
        },
        new GeneratedCodeSample
        {
            Name = "K46_TypedPartialPayload",
            FileName = "K46_TypedPartialPayload.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionProviderQueryTests.PartialPayloadQuery,
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false).WithTableResultMaterialization(),
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionProviderQueryTests.PartialBytes>(
                [new(1, [42, 0, 0, 0, 7]), new(2, [42, 0, 0, 0])])
        },
        new GeneratedCodeSample
        {
            Name = "K14_TypedValueSourceContexts",
            FileName = "K14_TypedValueSourceContexts.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = """
                select a.Position, b.ValueSourcePosition() as OtherPosition
                from #schema.first() a left outer join #schema.first() b
                    on a.Position = b.Position and b.Position > 0 order by a.Position
                """,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionValueEntity>(
                [new(0, 0, 1m), new(1, 0, 2m)])
        }
    ];

    private static GeneratedCodeSample PermissionSample(string name, string query,
        Musoq.Schema.Optimization.SourceExecutionPermissions permissions) => new()
    {
        Name = name, FileName = $"{name}.cs", Query = query, Category = "LibraryExecution",
        Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CompilationOptions = new CompilationOptions(ParallelizationMode.Full, usePrimitiveTypeValidation: false,
            maxDegreeOfParallelismOverride: 2),
        CreateSchemaProvider = () => LibraryExecutionFixture.CreateProvider<LibraryExecutionEntity,
            LibraryExecutionConcurrencyQueryTests.ConcurrencyLibrary>(LibraryExecutionFixture.CreateRows(), permissions)
    };

    private static GeneratedCodeSample LibraryExecution(string name, string query) => new()
    {
        Name = name,
        FileName = $"{name}.cs",
        Query = query,
        Category = "LibraryExecution",
        Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CreateSchemaProvider = LibraryExecutionFixture.CreateProvider
    };
}
