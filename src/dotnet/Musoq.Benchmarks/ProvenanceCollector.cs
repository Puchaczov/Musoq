using System.Runtime;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Running;
using Musoq.Converter;

namespace Musoq.Benchmarks;

public static class ProvenanceCollector
{
    public static string RepositoryRoot() => ProcessCommand.Require(Environment.CurrentDirectory, "git", "rev-parse", "--show-toplevel");

    public static RunProvenance Collect(string root, CampaignOptions options)
    {
        var source = Source(root);
        var hardware = CollectHardware(options.MachineProfile ?? System.Environment.MachineName);
        var environment = new EnvironmentStamp(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription,
            ProcessCommand.Require(root, "dotnet", "--version"), typeof(BenchmarkRunner).Assembly.GetName().Version!.ToString(),
            options.InProcess ? GCSettings.IsServerGC : true, true, System.Environment.ProcessorCount,
            options.Mode, options.Official ? "Official" : options.Job, options.InProcess, RuntimeVariables());
        var engineVersion = typeof(InstanceCreator).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? typeof(InstanceCreator).Assembly.GetName().Version!.ToString();
        var suiteVersion = typeof(MeasuredProcess).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var build = new BuildStamp(engineVersion, suiteVersion.Split('+').Last(),
            typeof(InstanceCreator).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            suiteVersion);
        return new RunProvenance(source, hardware, environment, build);
    }

    public static SourceStamp Source(string root)
    {
        var branch = ProcessCommand.Run(root, "git", "symbolic-ref", "--quiet", "--short", "HEAD");
        var dirty = Git(root, "status", "--porcelain", "--untracked-files=normal", "--", ".", ":(exclude)benchmarks/history");
        return new(Git(root, "rev-parse", "HEAD"), branch.ExitCode == 0 ? branch.Output : null,
            Git(root, "tag", "--points-at", "HEAD").Split('\n', StringSplitOptions.RemoveEmptyEntries),
            dirty.Length != 0, CiBranch());
    }

    public static HardwareStamp CollectHardware(string profile)
    {
        if (OperatingSystem.IsWindows())
            return WindowsHardware(profile);
        if (OperatingSystem.IsLinux())
            return LinuxHardware(profile);
        throw new PlatformNotSupportedException("Benchmark provenance supports Windows and Linux.");
    }

    public static HardwareStamp ParseLinux(string profile, string cpuInfo, string memoryInfo)
    {
        var blocks = cpuInfo.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var fields = cpuInfo.Split('\n').Where(line => line.Contains(':')).Select(line => line.Split(':', 2)).ToArray();
        var model = fields.FirstOrDefault(field => field[0].Trim() is "model name" or "Hardware")?[1].Trim() ?? "unknown";
        var logical = fields.Count(field => field[0].Trim() == "processor");
        var cores = blocks.Select(block => block.Split('\n').Where(line => line.Contains(':'))
            .Select(line => line.Split(':', 2)).ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.Ordinal))
            .Where(block => block.ContainsKey("physical id") && block.ContainsKey("core id"))
            .Select(block => block["physical id"] + ":" + block["core id"]).Distinct(StringComparer.Ordinal).Count();
        var memory = memoryInfo.Split('\n').First(line => line.StartsWith("MemTotal:", StringComparison.Ordinal))
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[1];
        return new HardwareStamp(profile, model, cores == 0 ? null : cores, logical,
            System.Environment.ProcessorCount, long.Parse(memory, System.Globalization.CultureInfo.InvariantCulture) * 1024,
            RuntimeInformation.ProcessArchitecture.ToString(), cores == 0 ? ["Physical core topology unavailable in /proc/cpuinfo."] : []);
    }

    private static HardwareStamp LinuxHardware(string profile) => ParseLinux(profile,
        File.ReadAllText("/proc/cpuinfo"), File.ReadAllText("/proc/meminfo"));

    private static HardwareStamp WindowsHardware(string profile)
    {
        const string command = "$p=Get-CimInstance Win32_Processor; $s=Get-CimInstance Win32_ComputerSystem; " +
            "[pscustomobject]@{cpu=($p.Name -join '; ');physical=($p.NumberOfCores|Measure-Object -Sum).Sum;" +
            "logical=($p.NumberOfLogicalProcessors|Measure-Object -Sum).Sum;memory=$s.TotalPhysicalMemory}|ConvertTo-Json -Compress";
        var result = ProcessCommand.Require(System.Environment.CurrentDirectory, "powershell.exe", "-NoProfile", "-NonInteractive", "-Command", command);
        using var json = JsonDocument.Parse(result);
        var item = json.RootElement;
        return new HardwareStamp(profile, item.GetProperty("cpu").GetString()!, item.GetProperty("physical").GetInt32(),
            item.GetProperty("logical").GetInt32(), System.Environment.ProcessorCount, item.GetProperty("memory").GetInt64(),
            RuntimeInformation.ProcessArchitecture.ToString(), []);
    }

    private static string Git(string root, params string[] args) => ProcessCommand.Require(root, "git", args);

    private static string? CiBranch()
    {
        var head = System.Environment.GetEnvironmentVariable("GITHUB_HEAD_REF");
        if (!string.IsNullOrWhiteSpace(head))
            return head;
        return System.Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
    }

    private static SortedDictionary<string, string> RuntimeVariables()
    {
        var variables = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry pair in System.Environment.GetEnvironmentVariables())
        {
            var name = pair.Key.ToString()!;
            if (name.StartsWith("COMPlus_", StringComparison.OrdinalIgnoreCase) ||
                new[] { "DOTNET_GC", "DOTNET_Tiered", "DOTNET_TC_", "DOTNET_Jit", "DOTNET_ReadyToRun", "DOTNET_EnableHWIntrinsic", "DOTNET_PROCESSOR_COUNT" }
                    .Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                variables.Add(name, pair.Value?.ToString() ?? "");
        }
        return variables;
    }
}
