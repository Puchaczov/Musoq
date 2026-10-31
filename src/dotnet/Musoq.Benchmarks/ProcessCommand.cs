using System.Diagnostics;

namespace Musoq.Benchmarks;

public sealed record CommandResult(int ExitCode, string Output, string Error);

public static class ProcessCommand
{
    public static CommandResult Run(string directory, string executable, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["LC_ALL"] = "C";
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return new CommandResult(process.ExitCode, output.GetAwaiter().GetResult().Trim(), error.GetAwaiter().GetResult().Trim());
    }

    public static string Require(string directory, string executable, params string[] arguments)
    {
        var result = Run(directory, executable, arguments);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"{executable} failed ({result.ExitCode}): {result.Error}\n{result.Output}");
        return result.Output;
    }
}
