using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

internal static class TestSynchronization
{
    internal static readonly TimeSpan DeadlockTimeout = TimeSpan.FromSeconds(30);

    internal static async Task WaitForSignalAsync(Task signal, string description)
    {
        try
        {
            await signal.WaitAsync(DeadlockTimeout);
        }
        catch (TimeoutException)
        {
            throw new AssertFailedException($"{description} did not complete within {DeadlockTimeout}.");
        }
    }
}
