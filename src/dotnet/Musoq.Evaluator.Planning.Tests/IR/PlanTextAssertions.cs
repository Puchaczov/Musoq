using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.IR;

internal static class PlanTextAssertions
{
    public static string FromLines(params string[] lines)
    {
        return string.Join("\n", lines);
    }

    public static void AreEqual(string expected, string actual, bool allowUnknownNodes = false)
    {
        var normalizedExpected = Normalize(expected);
        var normalizedActual = Normalize(actual);

        if (!allowUnknownNodes)
            Assert.IsFalse(
                normalizedActual.Contains("Unknown [", StringComparison.Ordinal),
                $"Plan text contains an unhandled node:{Environment.NewLine}{normalizedActual}");

        Assert.AreEqual(
            normalizedExpected,
            normalizedActual,
            $"Expected plan:{Environment.NewLine}{normalizedExpected}{Environment.NewLine}{Environment.NewLine}Actual plan:{Environment.NewLine}{normalizedActual}");
    }

    private static string Normalize(string text)
    {
        var lines = text
            .Replace("\r\n", "\n")
            .Trim('\n')
            .Split('\n');

        var indentation = GetCommonIndentation(lines);

        if (indentation == 0)
            return string.Join("\n", lines).Trim();

        for (var index = 0; index < lines.Length; index++)
            if (lines[index].Length >= indentation)
                lines[index] = lines[index][indentation..];

        return string.Join("\n", lines).Trim();
    }

    private static int GetCommonIndentation(string[] lines)
    {
        var indentation = int.MaxValue;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var spaces = 0;
            while (spaces < line.Length && line[spaces] == ' ')
                spaces++;

            indentation = Math.Min(indentation, spaces);
        }

        return indentation == int.MaxValue ? 0 : indentation;
    }
}
