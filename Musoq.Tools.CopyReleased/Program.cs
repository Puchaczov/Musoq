using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using Newtonsoft.Json;

namespace Musoq.Tools.CopyReleased
{
    static class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ApplicationArguments>(args)
                .MapResult(
                    ProcessArguments,
                    errors =>
                    {
                        foreach(var error in errors)
                            Console.WriteLine(error.Tag);

                        return 99;
                    });
        }

        private static int ProcessArguments(ApplicationArguments parsedArgs)
        {
            if (!Directory.Exists(parsedArgs.SourceDir))
                return 0;

            var file = Path.Combine(parsedArgs.ConfigurationDir, $"_conf.{parsedArgs.BuildType.ToLowerInvariant()}.mrf");

            if (!File.Exists(file))
                return 0;

            var dirs = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(file));

            foreach (var dir in dirs)
            {
                var destinationDir = Path.Combine(dir, parsedArgs.DestinationDirName);
                using (var proc = new Process()
                {
                    StartInfo =
                    {
                        UseShellExecute = true,
                        FileName = Path.Combine(Environment.SystemDirectory, "xcopy.exe"),
                        Arguments = $@"""{parsedArgs.SourceDir}"" ""{destinationDir}"" /E /I /Y",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                })
                {
                    proc.Start();
                    proc.WaitForExit();
                    
                    if(proc.ExitCode > 0)
                        return proc.ExitCode;
                }
            }

            return 0;
        }
    }
}
