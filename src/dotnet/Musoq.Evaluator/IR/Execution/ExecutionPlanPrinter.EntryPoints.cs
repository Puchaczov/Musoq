using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static readonly AsyncLocal<IReadOnlyDictionary<int, string>?> TypedStoredTableSlots = new();
    private static readonly AsyncLocal<FinalShapePrintContext?> FinalShapeContext = new();
    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>?> TypedRowBuffers = new();

    public static string Print(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var previousTypedStoredTableSlots = TypedStoredTableSlots.Value;
        var previousFinalShapeContext = FinalShapeContext.Value;
        var previousTypedRowBuffers = TypedRowBuffers.Value;
        TypedStoredTableSlots.Value = TypedStoredTableResultResolver.Resolve(plan)
            .ToDictionary(
                static pair => pair.Key,
                static pair => $"List<{pair.Value.RowShape.TypeName}>");
        FinalShapeContext.Value = CreateFinalShapePrintContext(plan);
        TypedRowBuffers.Value = CreateTypedRowBuffers(plan);

        try
        {
            var builder = new StringBuilder();
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"ExecutionPlan [{plan.Identifier}]");
            AppendShapes(builder, plan.Shapes);
            builder.AppendLine();
            builder.AppendLine("  Body");
            AppendBlock(builder, plan.Body, 4);
            return NormalizeLineEndings(builder.ToString().TrimEnd());
        }
        finally
        {
            TypedStoredTableSlots.Value = previousTypedStoredTableSlots;
            FinalShapeContext.Value = previousFinalShapeContext;
            TypedRowBuffers.Value = previousTypedRowBuffers;
        }
    }

    public static string PrintUnsupported(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return $"ExecutionPlanUnsupported [{reason}]";
    }

    private static void AppendBlock(StringBuilder builder, ExecutionBlock block, int indentation)
    {
        foreach (var node in block.Nodes)
            AppendNode(builder, node, indentation);
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
    }

    private static bool TryGetTypedStoredTableSlot(int tableIndex, out string generatedRowTypeName)
    {
        if (TypedStoredTableSlots.Value != null &&
            TypedStoredTableSlots.Value.TryGetValue(tableIndex, out generatedRowTypeName!))
        {
            return true;
        }

        generatedRowTypeName = string.Empty;
        return false;
    }

    private static string FormatCteRowResultSlot(int tableIndex)
    {
        return $"_cteRowResults.Slot{tableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private sealed record FinalShapePrintContext(
        string FinalTableName,
        string RowTypeName,
        string ShapeTypeName,
        IReadOnlyDictionary<string, string> SourceBuffers);
}
