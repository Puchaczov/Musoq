using System.Globalization;
using System.Reflection;
using System.Text;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Bindings;

internal static class AggregateCallIdentity
{
    public static string Create(AccessMethodNode method, bool isDistinct)
    {
        var writer = new IdentityWriter();
        writer.Add("agg:v2");
        writer.Add(GetAggregateName(method.Method, method.Name));
        writer.Add(method.Alias);
        writer.Add(method.TypeParameter ?? string.Empty);
        writer.Add(isDistinct ? "distinct" : "all");
        writer.Add(method.Arguments.Args.Length.ToString(CultureInfo.InvariantCulture));

        foreach (var argument in method.Arguments.Args)
            WriteNode(argument, writer);

        writer.Add(method.FilterExpression is null ? "no-filter" : "filter");
        if (method.FilterExpression is not null)
            WriteNode(method.FilterExpression, writer);

        return writer.ToString();
    }

    public static string Create(MethodCall method)
    {
        var writer = new IdentityWriter();
        writer.Add("agg:v2");
        writer.Add(GetAggregateName(method.Method, method.Method.Name));
        writer.Add(method.Alias ?? string.Empty);
        writer.Add(string.Empty);
        writer.Add("all");
        writer.Add(method.Arguments.Count.ToString(CultureInfo.InvariantCulture));

        foreach (var argument in method.Arguments)
            WriteExpression(argument, writer);

        writer.Add("no-filter");
        return writer.ToString();
    }

    private static string GetAggregateName(MethodInfo? method, string fallbackName)
    {
        var attributeName = method?.GetCustomAttribute<AggregateFunctionAttribute>()?.Name;
        var name = string.IsNullOrWhiteSpace(attributeName) ? fallbackName : attributeName;
        return name.ToUpperInvariant();
    }

    private static void WriteNode(Node? node, IdentityWriter writer)
    {
        if (node is null)
        {
            writer.Add("<null>");
            return;
        }

        writer.Add(node.GetType().FullName ?? node.GetType().Name);
        writer.Add(TypeName(node.ReturnType));

        switch (node)
        {
            case AccessColumnNode column:
                writer.Add(column.Alias);
                writer.Add(column.Name);
                writer.Add(column.IntendedTypeName ?? string.Empty);
                break;
            case AllColumnsNode allColumns:
                writer.Add(allColumns.Alias ?? string.Empty);
                break;
            case AccessMethodNode method:
                writer.Add(GetAggregateName(method.Method, method.Name));
                writer.Add(method.Alias ?? string.Empty);
                writer.Add(method.TypeParameter ?? string.Empty);
                writer.Add(method.IsDistinct ? "distinct" : "all");
                foreach (var argument in method.Arguments.Args)
                    WriteNode(argument, writer);
                break;
            case ConstantValueNode constant:
                writer.Add(LiteralValue(constant.ObjValue));
                break;
            case IdentifierNode identifier:
                writer.Add(identifier.Name);
                break;
            case BinaryNode binary:
                WriteNode(binary.Left, writer);
                WriteNode(binary.Right, writer);
                break;
            case CaseNode caseNode:
                foreach (var (when, then) in caseNode.WhenThenPairs)
                {
                    WriteNode(when, writer);
                    WriteNode(then, writer);
                }
                WriteNode(caseNode.Else, writer);
                break;
            case DotNode dot:
                WriteNode(dot.Root, writer);
                WriteNode(dot.Expression, writer);
                writer.Add(dot.Name);
                break;
            case UnaryNode unary:
                WriteNode(unary.Expression, writer);
                break;
            default:
                writer.Add(node.Id);
                break;
        }
    }

    private static void WriteExpression(IrExpression expression, IdentityWriter writer)
    {
        writer.Add(expression.GetType().FullName ?? expression.GetType().Name);
        writer.Add(TypeName(expression.ReturnType));

        switch (expression)
        {
            case ColumnRef column:
                writer.Add(column.Alias);
                writer.Add(column.ColumnName);
                break;
            case WildcardLiteral:
                writer.Add("*");
                break;
            case Literal literal:
                writer.Add(LiteralValue(literal.Value));
                break;
            case MethodCall method:
                writer.Add(GetAggregateName(method.Method, method.Method.Name));
                writer.Add(method.Alias ?? string.Empty);
                foreach (var argument in method.Arguments)
                    WriteExpression(argument, writer);
                break;
            case BinaryOp binary:
                writer.Add(binary.Kind.ToString());
                WriteExpression(binary.Left, writer);
                WriteExpression(binary.Right, writer);
                break;
            case UnaryOp unary:
                writer.Add(unary.Kind.ToString());
                WriteExpression(unary.Operand, writer);
                break;
            case AggregateRef aggregateRef:
                writer.Add(aggregateRef.Identifier);
                break;
            default:
                writer.Add(IrExpressionPrinter.Print(expression));
                break;
        }
    }

    private static string TypeName(Type? type)
    {
        return type?.FullName ?? "<null>";
    }

    private static string LiteralValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private sealed class IdentityWriter
    {
        private readonly StringBuilder _builder = new();

        public void Add(string value)
        {
            _builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            _builder.Append(':');
            _builder.Append(value);
            _builder.Append(';');
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
