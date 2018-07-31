using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Plugins;

namespace Musoq.Evaluator
{
    public static class EnvironmentUtils
    {
        public static string GetOrCreateEnvironmentVariable()
        {
            var envRuntime = System.Environment.GetEnvironmentVariable(Constants.NetCoreRuntimePath,
                EnvironmentVariableTarget.Process);

            var envVersion = System.Environment.GetEnvironmentVariable(Constants.NetCoreRuntimeVersion,
                EnvironmentVariableTarget.Process);

            if (envRuntime != null && envVersion != null) return Path.Combine(envRuntime, envVersion, Constants.NetStandardDllFile);

            using (var process = new Process()
            {
                StartInfo =
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                },
                EnableRaisingEvents = true
            })
            {
                var path = new StringBuilder();
                var version = new StringBuilder();
                var error = new StringBuilder();
                var timeout = TimeSpan.FromSeconds(10);

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) => 
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            if (!e.Data.StartsWith("Microsoft.NETCore.App 2.1")) return;

                            var text = e.Data;
                            var startIndex = text.IndexOf('[') + 1;
                            var stopIndex = text.IndexOf(']') - 1;
                            var firstSpace = text.IndexOf(' ');
                            var secondSpace = text.IndexOf(' ', firstSpace + 1);
                            path.Clear();
                            path.Append(text.Substring(startIndex, stopIndex - startIndex + 1));
                            version.Clear();
                            version.Append(text.Substring(firstSpace + 1, secondSpace - (firstSpace + 1)));
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(Convert.ToInt32(timeout.TotalMilliseconds)) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        var runtimePath = path.ToString();
                        var runtimeVersion = version.ToString();
                        System.Environment.SetEnvironmentVariable(Constants.NetCoreRuntimePath, runtimePath);
                        System.Environment.SetEnvironmentVariable(Constants.NetCoreRuntimeVersion, runtimeVersion);
                        envRuntime = runtimePath;
                        envVersion = runtimeVersion;
                    }
                    else
                    {
                        throw new DotNetNotFoundException();
                    }
                }
            }

            return Path.Combine(envRuntime, envVersion, Constants.NetStandardDllFile);
        }
    }
}