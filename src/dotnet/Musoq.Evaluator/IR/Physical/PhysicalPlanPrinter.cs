using System.Text;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Physical;
public static partial class PhysicalPlanPrinter
{
    public static string Print(PhysicalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sb = new StringBuilder();
        PrintNode(node, sb, indent: 0);
        return sb.ToString().TrimEnd();
    }

    private static void PrintNode(PhysicalNode node, StringBuilder sb, int indent)
    {
        var prefix = PlanPrinterHelpers.Indent(indent);
        switch (node)
        {
            case PhysicalProjectNode project:
                PrintProject(project, sb, prefix, indent);
                break;
            case PhysicalAggregateCandidateNode aggregateCandidate:
                PrintAggregateCandidate(aggregateCandidate, sb, prefix, indent);
                break;
            case PhysicalSingleKeyAggregateNode singleAgg:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalSingleKeyAggregate [key: {singleAgg.GroupKeyName} ({singleAgg.GroupKeyType.Name})] [aggs: ");
                PlanPrinterHelpers.AppendAggregateBindings(sb, singleAgg.Bindings);
                sb.AppendLine("]");
                PrintNode(singleAgg.Input, sb, indent + 2);
                break;

            case PhysicalValueTupleAggregateNode tupleAgg:
                sb.Append(prefix).Append("PhysicalValueTupleAggregate [keys: ");
                PlanPrinterHelpers.AppendNames(sb, tupleAgg.GroupKeyNames);
                sb.Append("] [aggs: ");
                PlanPrinterHelpers.AppendAggregateBindings(sb, tupleAgg.Bindings);
                sb.AppendLine("]");
                PrintNode(tupleAgg.Input, sb, indent + 2);
                break;

            case PhysicalAggregateOnlyNode aggOnly:
                sb.Append(prefix).Append("PhysicalAggregateOnly [aggs: ");
                PlanPrinterHelpers.AppendAggregateBindings(sb, aggOnly.Bindings);
                sb.AppendLine("]");
                PrintNode(aggOnly.Input, sb, indent + 2);
                break;

            case PhysicalFilterNode filter:
                sb.Append(prefix).Append("PhysicalFilter [");
                sb.Append(IrExpressionPrinter.Print(filter.Predicate));
                sb.AppendLine("]");
                PrintNode(filter.Input, sb, indent + 2);
                break;

            case PhysicalHavingFilterNode having:
                sb.Append(prefix).Append("PhysicalHaving [");
                sb.Append(IrExpressionPrinter.Print(having.Predicate));
                sb.AppendLine("]");
                PrintNode(having.Input, sb, indent + 2);
                break;

            case PhysicalQualifyFilterNode qualify:
                sb.Append(prefix).Append("PhysicalQualify [");
                sb.Append(IrExpressionPrinter.Print(qualify.Predicate));
                sb.AppendLine("]");
                PrintNode(qualify.Input, sb, indent + 2);
                break;

            case PhysicalSortNode sort:
                sb.Append(prefix).Append("PhysicalSort [");
                PlanPrinterHelpers.AppendOrderFields(sb, sort.Keys);
                sb.AppendLine("]");
                PrintNode(sort.Input, sb, indent + 2);
                break;

            case PhysicalSkipNode skip:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalSkip [{skip.Count}]");
                PrintNode(skip.Input, sb, indent + 2);
                break;

            case PhysicalTakeNode take:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalTake [{take.Count}]");
                PrintNode(take.Input, sb, indent + 2);
                break;

            case PhysicalTopNNode topN:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalTopN [{topN.N}] [");
                PlanPrinterHelpers.AppendOrderFields(sb, topN.Keys);
                sb.AppendLine("]");
                PrintNode(topN.Input, sb, indent + 2);
                break;

            case PhysicalTopOffsetNode topOffset:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalTopOffset [skip {topOffset.Skip}, take {topOffset.Take}] [");
                PlanPrinterHelpers.AppendOrderFields(sb, topOffset.Keys);
                sb.AppendLine("]");
                PrintNode(topOffset.Input, sb, indent + 2);
                break;

            case PhysicalMaterializeNode materialize:
                sb.Append(prefix).AppendLine("PhysicalMaterialize");
                PrintNode(materialize.Input, sb, indent + 2);
                break;

            case PhysicalWindowNode window:
                sb.Append(prefix).Append("PhysicalWindow [");
                PlanPrinterHelpers.AppendWindowRegistrations(sb, window.Registrations);
                sb.AppendLine("]");
                PrintNode(window.Input, sb, indent + 2);
                break;

            case PhysicalSchemaScanNode scan:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalSchemaScan [{PlanPrinterHelpers.FormatSchemaName(scan.SchemaName)}.{scan.MethodName}(");
                PlanPrinterHelpers.AppendExpressions(sb, scan.Arguments);
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $") as {scan.Alias}]");
                AppendPushedPredicates(sb, scan.PushedPredicates);
                sb.AppendLine();
                break;

            case PhysicalValuesScanNode values:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalValuesScan [{values.Rows.Count} rows as {values.Alias}]");
                break;

            case PhysicalUnpivotNode unpivot:
                PrintUnpivot(unpivot, sb, prefix, indent);
                break;
            case PhysicalInterpretSourceNode interpret:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalInterpretSource [{PlanPrinterHelpers.FormatSchemaName(interpret.SchemaName)}(");
                PlanPrinterHelpers.AppendExpressions(sb, interpret.Arguments);
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $") as {interpret.Alias}]");
                break;
            case PhysicalPropertySourceNode property:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalPropertySource [{property.SourceAlias}.");
                PlanPrinterHelpers.AppendProperties(sb, property.PropertiesChain);
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" as {property.Alias}] [apply: {property.ApplyKind}] [type: ");
                sb.Append(property.ResultType.Name);
                sb.AppendLine("]");
                break;

            case PhysicalAccessMethodSourceNode accessMethod:
                sb.Append(prefix).Append("PhysicalAccessMethodSource [");
                sb.Append(IrExpressionPrinter.Print(accessMethod.MethodCallExpression));
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" as {accessMethod.Alias}] [apply: {accessMethod.ApplyKind}] [type: ");
                sb.Append(accessMethod.ResultType.Name);
                sb.AppendLine("]");
                break;
            case PhysicalCteRefNode cteRef:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalCteRef [{cteRef.CteName} as {cteRef.Alias}]");
                break;
            case PhysicalJoinCandidateNode joinCandidate:
                PrintJoinCandidate(joinCandidate, sb, prefix, indent);
                break;
            case PhysicalHashJoinNode hashJoin:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalHashJoin [{hashJoin.Kind}] [build: ");
                PlanPrinterHelpers.AppendExpressions(sb, hashJoin.BuildKeys);
                sb.Append("] [probe: ");
                PlanPrinterHelpers.AppendExpressions(sb, hashJoin.ProbeKeys);
                sb.Append(']');
                if (hashJoin.Residual is not null)
                {
                    sb.Append(" [residual: ");
                    sb.Append(IrExpressionPrinter.Print(hashJoin.Residual));
                    sb.Append(']');
                }
                sb.AppendLine();
                PrintNode(hashJoin.Left, sb, indent + 2);
                PrintNode(hashJoin.Right, sb, indent + 2);
                break;

            case PhysicalNestedLoopJoinNode nlJoin:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalNestedLoopJoin [{nlJoin.Kind}] [");
                sb.Append(IrExpressionPrinter.Print(nlJoin.OnPredicate));
                sb.Append(']');
                AppendTieBreak(sb, nlJoin.TieBreak);
                sb.AppendLine();
                PrintNode(nlJoin.Left, sb, indent + 2);
                PrintNode(nlJoin.Right, sb, indent + 2);
                break;

            case PhysicalSortMergeJoinNode sortMergeJoin:
                sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalSortMergeJoin [{sortMergeJoin.Kind}] [left: ");
                sb.Append(IrExpressionPrinter.Print(sortMergeJoin.LeftKey));
                sb.Append("] [right: ");
                sb.Append(IrExpressionPrinter.Print(sortMergeJoin.RightKey));
                sb.Append("] [op: ").Append(FormatBinaryOperator(sortMergeJoin.ComparisonKind));
                sb.Append(']');
                AppendSortMergePartitions(sb, sortMergeJoin);
                if (sortMergeJoin.Residual is not null)
                {
                    sb.Append(" [residual: ");
                    sb.Append(IrExpressionPrinter.Print(sortMergeJoin.Residual));
                    sb.Append(']');
                }
                sb.AppendLine();
                PrintNode(sortMergeJoin.Left, sb, indent + 2);
                PrintNode(sortMergeJoin.Right, sb, indent + 2);
                break;

            case PhysicalNestedLoopApplyNode nlApply:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalNestedLoopApply [{nlApply.Kind}{(nlApply.WithOrdinality ? ", with ordinality" : string.Empty)}]");
                PrintNode(nlApply.Left, sb, indent + 2);
                PrintNode(nlApply.Right, sb, indent + 2);
                break;

            case PhysicalSetOperationNode setOp:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalSetOp [{setOp.Kind}]");
                PrintNode(setOp.Left, sb, indent + 2);
                PrintNode(setOp.Right, sb, indent + 2);
                break;

            case PhysicalRecursiveCteNode recursiveCte: PrintRecursiveCte(recursiveCte, sb, prefix, indent); break;
            case PhysicalCteNode cte: PrintCte(cte, sb, prefix, indent); break;

            case PhysicalDescNode desc:
                PrintDesc(desc, sb, prefix);
                break;

            case PhysicalMultiStatementNode multi:
                sb.Append(prefix).AppendLine("PhysicalMultiStatement");
                foreach (var stmt in multi.Statements)
                    PrintNode(stmt, sb, indent + 2);
                break;
            default:
                sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Unknown [{node.GetType().Name}]");
                break;
        }
    }

}
