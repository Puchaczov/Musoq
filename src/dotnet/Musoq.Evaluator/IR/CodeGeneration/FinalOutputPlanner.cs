using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static partial class FinalOutputPlanner
{
    public static FinalOutputPlan Plan(ExecutionPlan plan, TableViaRowsResultInfo result)
    {
        var serial = CanStreamSerialFinalAppendRows(plan, result);
        var supported = plan.Body.Nodes.OfType<ExecutionReturnTable>().LastOrDefault()?.Table.Name == result.TableName &&
                        (serial || HasSupportedFinalShapeWriter(plan.Body, result.TableName, result.Columns.Count));
        var generatedRows = FinalGeneratedRowSinkPolicy.CanUse(plan, result.TableName);
        IReadOnlyDictionary<string, string> sourceBuffers;
        IReadOnlyDictionary<string, string> sortedSources;
        if (generatedRows)
        {
            sourceBuffers = new Dictionary<string, string>(StringComparer.Ordinal);
            sortedSources = plan.Body.Nodes.OfType<ExecutionSortTable>().ToDictionary(
                static sort => sort.Target.Name, static sort => sort.Source.Name, StringComparer.Ordinal);
        }
        else
        {
            sourceBuffers = CreateFinalShapeSourceBuffers(plan.Body, result.TableName, result.ShapeTypeName, result.ShapeFields);
            sortedSources = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var output = new FinalOutputPlan(
            result,
            supported,
            generatedRows ? FinalOutputRowRepresentation.GeneratedRow : FinalOutputRowRepresentation.SelectShape,
            !generatedRows && !serial,
            !generatedRows,
            generatedRows,
            sourceBuffers.ToFrozenDictionary(StringComparer.Ordinal),
            sortedSources.ToFrozenDictionary(StringComparer.Ordinal));
        return SelectOwnedTerminalBuffer(plan, output);
    }
}
