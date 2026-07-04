using System.Diagnostics.CodeAnalysis;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitAccessMethod(node);
    }

    public void Visit(InterpretCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(ParseCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(TryInterpretCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(TryParseCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(PartialInterpretCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(PartialParseCallNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(InterpretAtCallNode node)
    {
        Nodes.Push(node);
    }

    private void VisitAccessMethod(AccessMethodNode node)
    {
        var args = Nodes.Pop() as ArgsListNode ?? ArgsListNode.Empty;
        var filterExpression = node.FilterExpression is null ? null : Nodes.Pop();

        var functionName = node.FunctionToken.Value;
        var typeParameter = node.TypeParameter;

        if (IsInterpretFunctionCall(functionName, typeParameter, args, out var schemaName, out var dataSource))
        {
            Nodes.Push(new InterpretCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsParseFunctionCall(functionName, typeParameter, args, out schemaName, out dataSource))
        {
            Nodes.Push(new ParseCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsInterpretAtFunctionCall(functionName, typeParameter, args, out schemaName, out dataSource, out var offset))
        {
            Nodes.Push(new InterpretAtCallNode(dataSource, offset, schemaName, null));
            return;
        }

        if (IsTryInterpretFunctionCall(functionName, typeParameter, args, out schemaName, out dataSource))
        {
            Nodes.Push(new TryInterpretCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsTryParseFunctionCall(functionName, typeParameter, args, out schemaName, out dataSource))
        {
            Nodes.Push(new TryParseCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsPartialInterpretFunctionCall(functionName, typeParameter, args, out schemaName, out dataSource))
        {
            Nodes.Push(new PartialInterpretCallNode(dataSource, schemaName, null));
            return;
        }

        if (IsPartialParseFunctionCall(functionName, typeParameter, args, out schemaName, out dataSource))
        {
            Nodes.Push(new PartialParseCallNode(dataSource, schemaName, null));
            return;
        }

        Nodes.Push(new AccessMethodNode(node.FunctionToken, args, null, node.CanSkipInjectSource, node.Method,
            node.Alias, default, node.IsDistinct)
        {
            HasFilter = node.HasFilter,
            FilterExpression = filterExpression,
            FilterExpressionText = node.FilterExpressionText,
            IsPivotGenerated = node.IsPivotGenerated,
            IsScalarSubqueryValueWrapper = node.IsScalarSubqueryValueWrapper
        });
    }

    private static bool IsInterpretFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource)
    {
        schemaName = string.Empty;
        dataSource = null;

        if (!string.Equals(functionName, "Interpret", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 1 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            return true;
        }

        ThrowIfOldSyntax(functionName, args);
        return false;
    }

    private static bool IsParseFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource)
    {
        schemaName = string.Empty;
        dataSource = null;

        if (!string.Equals(functionName, "Parse", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 1 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            return true;
        }

        ThrowIfOldSyntax(functionName, args);
        return false;
    }

    private static bool IsInterpretAtFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource, [NotNullWhen(true)] out Node? offset)
    {
        schemaName = string.Empty;
        dataSource = null;
        offset = null;

        if (!string.Equals(functionName, "InterpretAt", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 2 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            offset = args.Args[1];
            return true;
        }

        ThrowIfOldInterpretAtSyntax(args);
        return false;
    }

    private static bool IsTryInterpretFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource)
    {
        schemaName = string.Empty;
        dataSource = null;

        if (!string.Equals(functionName, "TryInterpret", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 1 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            return true;
        }

        ThrowIfOldSyntax(functionName, args);
        return false;
    }

    private static bool IsTryParseFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource)
    {
        schemaName = string.Empty;
        dataSource = null;

        if (!string.Equals(functionName, "TryParse", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 1 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            return true;
        }

        ThrowIfOldSyntax(functionName, args);
        return false;
    }

    private static bool IsPartialInterpretFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource)
    {
        schemaName = string.Empty;
        dataSource = null;

        if (!string.Equals(functionName, "PartialInterpret", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 1 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            return true;
        }

        ThrowIfOldSyntax(functionName, args);
        return false;
    }

    private static bool IsPartialParseFunctionCall(string functionName, string? typeParameter, ArgsListNode? args,
        out string schemaName, [NotNullWhen(true)] out Node? dataSource)
    {
        schemaName = string.Empty;
        dataSource = null;

        if (!string.Equals(functionName, "PartialParse", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(typeParameter) && args?.Args is { Length: 1 })
        {
            schemaName = typeParameter;
            dataSource = args.Args[0];
            return true;
        }

        ThrowIfOldSyntax(functionName, args);
        return false;
    }

    private static void ThrowIfOldSyntax(string functionName, ArgsListNode? args)
    {
        if (args?.Args == null || args.Args.Length != 2)
            return;

        var lastArg = args.Args[1];
        var schemaName = lastArg switch
        {
            StringNode sn => sn.Value,
            WordNode wn => wn.Value,
            _ => null
        };

        if (schemaName == null)
            return;

        throw new InvalidOperationException(
            $"The syntax '{functionName}(data, ''{schemaName}'')' is no longer supported. Use '{functionName}<{schemaName}>(data)' instead.");
    }

    private static void ThrowIfOldInterpretAtSyntax(ArgsListNode? args)
    {
        if (args?.Args == null || args.Args.Length != 3)
            return;

        var lastArg = args.Args[2];
        var schemaName = lastArg switch
        {
            StringNode sn => sn.Value,
            WordNode wn => wn.Value,
            _ => null
        };

        if (schemaName == null)
            return;

        throw new InvalidOperationException(
            $"The syntax 'InterpretAt(data, offset, ''{schemaName}'')' is no longer supported. Use 'InterpretAt<{schemaName}>(data, offset)' instead.");
    }
}
