using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class WindowFunctionArgumentValidation
{
    public static void Validate(
        AccessMethodNode functionCall,
        Node[] arguments,
        Action<DiagnosticCode, string, Node> report)
    {
        ArgumentNullException.ThrowIfNull(functionCall);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(report);

        var normalizedName = NormalizeWindowFunctionName(functionCall.Name);

        if (normalizedName is "ROWNUMBER" or "RANK" or "DENSERANK" or "PERCENTRANK" or "CUMEDIST")
        {
            ValidateWindowArgumentCount(functionCall, arguments.Length, "0", report);
            return;
        }

        switch (normalizedName)
        {
            case "NTILE":
                if (!ValidateWindowArgumentCount(functionCall, arguments.Length, "1", report))
                    return;

                if (!IsIntWindowArgument(arguments[0]))
                    report(
                        DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                        CreateWindowTypeMismatchMessage(functionCall.Name, arguments),
                        arguments[0]);
                else if (TryGetConstantInteger(arguments[0], out var bucketCount) && bucketCount <= 0)
                    report(
                        DiagnosticCode.MQ3103_InvalidWindowFunctionArgument,
                        $"Window function '{functionCall.Name}' has an invalid argument: bucket count must be a positive integer.",
                        arguments[0]);
                return;

            case "LAG":
            case "LEAD":
                if (!ValidateWindowArgumentCount(functionCall, arguments.Length, "1..3", report))
                    return;

                if (arguments.Length >= 2 && !IsIntWindowArgument(arguments[1]))
                    report(
                        DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                        CreateWindowTypeMismatchMessage(functionCall.Name, arguments),
                        arguments[1]);
                return;

            case "SUM":
            case "COUNT":
            case "AVG":
            case "MIN":
            case "MAX":
            case "FIRSTVALUE":
            case "LASTVALUE":
                ValidateWindowArgumentCount(functionCall, arguments.Length, "1", report);
                return;

            case "NTHVALUE":
                if (!ValidateWindowArgumentCount(functionCall, arguments.Length, "2", report))
                    return;

                if (!IsIntWindowArgument(arguments[1]))
                    report(
                        DiagnosticCode.MQ3088_NoMatchingCallableOverload,
                        CreateWindowTypeMismatchMessage(functionCall.Name, arguments),
                        arguments[1]);
                else if (TryGetConstantInteger(arguments[1], out var position) && position <= 0)
                    report(
                        DiagnosticCode.MQ3103_InvalidWindowFunctionArgument,
                        $"Window function '{functionCall.Name}' has an invalid argument: position must be a positive 1-based integer.",
                        arguments[1]);
                return;
        }
    }

    private static bool ValidateWindowArgumentCount(
        AccessMethodNode functionCall,
        int actualCount,
        string expectedCounts,
        Action<DiagnosticCode, string, Node> report)
    {
        if (expectedCounts == "1..3"
                ? actualCount is >= 1 and <= 3
                : actualCount.ToString(CultureInfo.InvariantCulture) == expectedCounts)
            return true;

        report(
            DiagnosticCode.MQ3087_InvalidCallableArity,
            $"Callable '{functionCall.Name}' does not accept {actualCount.ToString(CultureInfo.InvariantCulture)} argument(s); expected {expectedCounts}.",
            functionCall);
        return false;
    }

    private static string CreateWindowTypeMismatchMessage(string functionName, IReadOnlyList<Node> arguments)
    {
        return $"No overload of callable '{functionName}' accepts argument types ({string.Join(", ", arguments.Select(FormatWindowArgumentType))}).";
    }

    private static string FormatWindowArgumentType(Node argument)
    {
        var type = argument.ReturnType;
        if (type == null || type == typeof(void))
            return "unknown";

        if (type == typeof(NullNode.NullType))
            return "null";

        var underlying = Nullable.GetUnderlyingType(type);
        return underlying == null ? type.Name : $"{underlying.Name}?";
    }

    private static bool IsIntWindowArgument(Node argument)
    {
        return argument.ReturnType == typeof(int);
    }

    private static bool TryGetConstantInteger(Node argument, out long value)
    {
        if (argument is IntegerNode integer)
        {
            value = Convert.ToInt64(integer.ObjValue, CultureInfo.InvariantCulture);
            return true;
        }

        value = 0;
        return false;
    }

    private static string NormalizeWindowFunctionName(string functionName)
    {
        return functionName.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }
}
