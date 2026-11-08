using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{

    internal sealed class ErasedPlanningTypeSchema(PlanningTypeRow[] rows) : GenericSchema<LibraryBase>(
        new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["first"] = (new GenericEntityTable<PlanningTypeRow>(), new GenericChunkSource<PlanningTypeRow>(rows,
                GenericEntityTable<PlanningTypeRow>.NameToIndexMap, GenericEntityTable<PlanningTypeRow>.IndexToObjectAccessMap))
        })
    {
        public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
            => base.DescribeSource(name, context, parameters) with
            {
                RowType = typeof(IReadOnlyDictionary<string, object>),
                TransferCapabilities = SourceTransferCapabilities.None
            };
    }

    public sealed record CaseDistinctPlanningRow(int Position, string Name, int? name);

    public sealed record PlanningTypeRow(int Position, decimal? Amount);
    internal sealed class PlanningTypeSchema(PlanningTypeRow[] rows) : GenericSchema<LibraryBase>(
        new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["first"] = (new GenericEntityTable<PlanningTypeRow>(), new GenericChunkSource<PlanningTypeRow>(rows,
                GenericEntityTable<PlanningTypeRow>.NameToIndexMap, GenericEntityTable<PlanningTypeRow>.IndexToObjectAccessMap))
        })
    {
        internal List<SourcePlanRequest> Requests { get; } = [];
        public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
        {
            Requests.Add(request);
            return SourcePlanResult.RejectAll(request);
        }
    }

}
