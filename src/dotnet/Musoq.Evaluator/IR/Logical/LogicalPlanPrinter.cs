using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Printing;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Logical;

public static class LogicalPlanPrinter
{
    public static string Print(LogicalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sb = new StringBuilder();
        PrintNode(node, sb, indent: 0);
        return sb.ToString().TrimEnd();
    }

    private static void PrintNode(LogicalNode node, StringBuilder sb, int indent)
    {
        var prefix = PlanPrinterHelpers.Indent(indent);

        switch (node)
        {
            case ProjectNode project:
                sb.Append(prefix).Append("Project [");
                PlanPrinterHelpers.AppendProjectedFields(sb, project.Fields);
                sb.AppendLine("]");
                PrintNode(project.Input, sb, indent + 2);
                break;

            case AggregateNode aggregate:
                sb.Append(prefix).Append("Aggregate [keys: ");
                PlanPrinterHelpers.AppendNames(sb, aggregate.GroupKeyNames);
                sb.Append("] [aggs: ");
                PlanPrinterHelpers.AppendAggregateBindings(sb, aggregate.Bindings);
                sb.AppendLine("]");
                PrintNode(aggregate.Input, sb, indent + 2);
                break;

            case FilterNode filter:
                sb.Append(prefix).Append("Filter [");
                sb.Append(IrExpressionPrinter.Print(filter.Predicate));
                sb.AppendLine("]");
                PrintNode(filter.Input, sb, indent + 2);
                break;

            case HavingFilterNode having:
                sb.Append(prefix).Append("Having [");
                sb.Append(IrExpressionPrinter.Print(having.Predicate));
                sb.AppendLine("]");
                PrintNode(having.Input, sb, indent + 2);
                break;

            case QualifyFilterNode qualify:
                sb.Append(prefix).Append("Qualify [");
                sb.Append(IrExpressionPrinter.Print(qualify.Predicate));
                sb.AppendLine("]");
                PrintNode(qualify.Input, sb, indent + 2);
                break;

            case SortNode sort:
                sb.Append(prefix).Append("Sort [");
                PlanPrinterHelpers.AppendOrderFields(sb, sort.Keys);
                sb.AppendLine("]");
                PrintNode(sort.Input, sb, indent + 2);
                break;

            case SkipNode skip:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Skip [{skip.Count}]");
                PrintNode(skip.Input, sb, indent + 2);
                break;

            case TakeNode take:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Take [{take.Count}]");
                PrintNode(take.Input, sb, indent + 2);
                break;

            case WindowNode window:
                sb.Append(prefix).Append("Window [");
                PlanPrinterHelpers.AppendWindowRegistrations(sb, window.Registrations);
                sb.AppendLine("]");
                PrintNode(window.Input, sb, indent + 2);
                break;

            case SchemaScanNode scan:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"SchemaScan [{PlanPrinterHelpers.FormatSchemaName(scan.SchemaName)}.{scan.MethodName}(");
                PlanPrinterHelpers.AppendExpressions(sb, scan.Arguments);
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $") as {scan.Alias}]");
                break;

            case ValuesScanNode values:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"ValuesScan [{values.Rows.Count} rows as {values.Alias}]");
                break;

            case UnpivotNode unpivot:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"Unpivot [name: {unpivot.NameColumn}; value: {unpivot.ValueColumn}; entries: ");
                AppendUnpivotEntries(sb, unpivot.Entries);
                sb.Append("; keep: ");
                PlanPrinterHelpers.AppendProjectedFields(sb, unpivot.KeepFields.ToArray());
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"] as {unpivot.Alias}");
                PrintNode(unpivot.Source, sb, indent + 2);
                break;

            case InterpretSourceNode interpret:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"InterpretSource [{PlanPrinterHelpers.FormatSchemaName(interpret.SchemaName)}(");
                PlanPrinterHelpers.AppendExpressions(sb, interpret.Arguments);
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $") as {interpret.Alias}]");
                break;

            case PropertySourceNode property:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PropertySource [{property.SourceAlias}.");
                PlanPrinterHelpers.AppendProperties(sb, property.PropertiesChain);
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" as {property.Alias}] [apply: {property.ApplyKind}] [type: ");
                sb.Append(property.ResultType.Name);
                sb.AppendLine("]");
                break;

            case AccessMethodSourceNode accessMethod:
                sb.Append(prefix).Append("AccessMethodSource [");
                sb.Append(IrExpressionPrinter.Print(accessMethod.MethodCallExpression));
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" as {accessMethod.Alias}] [apply: {accessMethod.ApplyKind}] [type: ");
                sb.Append(accessMethod.ResultType.Name);
                sb.AppendLine("]");
                break;

            case CteRefNode cteRef:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"CteRef [{cteRef.CteName} as {cteRef.Alias}]");
                break;

            case JoinNode join:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"Join [{join.Kind}] [");
                sb.Append(IrExpressionPrinter.Print(join.OnPredicate));
                sb.Append(']');
                AppendTieBreak(sb, join.TieBreak);
                sb.AppendLine();
                PrintNode(join.Left, sb, indent + 2);
                PrintNode(join.Right, sb, indent + 2);
                break;

            case ApplyNode apply:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Apply [{apply.Kind}{(apply.WithOrdinality ? ", with ordinality" : string.Empty)}]");
                PrintNode(apply.Left, sb, indent + 2);
                PrintNode(apply.Right, sb, indent + 2);
                break;

            case SetOperationNode setOp:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"SetOp [{setOp.Kind}]");
                PrintNode(setOp.Left, sb, indent + 2);
                PrintNode(setOp.Right, sb, indent + 2);
                break;

            case RecursiveCteNode recursiveCte:
                sb.Append(prefix).Append("RecursiveCte [");
                sb.Append(recursiveCte.Name);
                sb.Append("] [");
                sb.Append(recursiveCte.UnionKind);
                if (recursiveCte.Keys.Length > 0)
                {
                    sb.Append(": ");
                    PlanPrinterHelpers.AppendNames(sb, recursiveCte.Keys);
                }
                sb.AppendLine("]");
                sb.Append(prefix).AppendLine("  Anchor");
                PrintNode(recursiveCte.Anchor, sb, indent + 4);
                sb.Append(prefix).AppendLine("  RecursiveMember");
                PrintNode(recursiveCte.RecursiveMember, sb, indent + 4);
                break;

            case CteNode cte:
                sb.Append(prefix).AppendLine("Cte");
                foreach (var def in cte.Definitions)
                {
                    sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  Definition [{def.Name}]");
                    PrintNode(def.Plan, sb, indent + 4);
                }
                sb.Append(prefix).AppendLine("  Query");
                PrintNode(cte.Query, sb, indent + 4);
                break;

            case DescNode desc:
                if (desc.Type == DescType.Query)
                    sb.Append(prefix).AppendLine("DescQuery");
                else
                    sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Desc [{PlanPrinterHelpers.FormatSchemaName(desc.SchemaName)}.{desc.MethodName}()] [{desc.Type}] [{desc.Column}]");
                break;

            case MultiStatementNode multi:
                sb.Append(prefix).AppendLine("MultiStatement");
                foreach (var stmt in multi.Statements)
                    PrintNode(stmt, sb, indent + 2);
                break;

            default:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Unknown [{node.GetType().Name}]");
                break;
        }
    }

    private static void AppendUnpivotEntries(StringBuilder sb, IReadOnlyList<UnpivotEntry> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(IrExpressionPrinter.Print(entries[i].Value));
            sb.Append(" as ");
            sb.Append(entries[i].NameValue);
        }
    }

    private static void AppendTieBreak(StringBuilder sb, OrderField? tieBreak)
    {
        if (tieBreak == null)
            return;

        sb.Append(" [tie: ");
        PlanPrinterHelpers.AppendOrderFields(sb, [tieBreak]);
        sb.Append(']');
    }

}
