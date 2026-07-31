using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Targets.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetRenderTelemetryTests
{
    [TestMethod]
    public void Push_RestoresNestedRenderPhaseCallbacks()
    {
        var events = new List<string>();
        using (TargetRenderTelemetry.Push(name => new RecordingScope(events, "outer", name)))
        {
            using (TargetRenderTelemetry.BeginPhase("class-assembly"))
            {
                using (TargetRenderTelemetry.Push(name => new RecordingScope(events, "inner", name)))
                using (TargetRenderTelemetry.BeginPhase("syntax-tree"))
                {
                }
            }

            using (TargetRenderTelemetry.BeginPhase("references"))
            {
            }
        }

        CollectionAssert.AreEqual(
            new[]
            {
                "begin:outer:class-assembly",
                "begin:inner:syntax-tree",
                "end:inner:syntax-tree",
                "end:outer:class-assembly",
                "begin:outer:references",
                "end:outer:references"
            },
            events);
    }

    [TestMethod]
    public void BeginPhase_WithoutRecorder_IsSafe()
    {
        using var phase = TargetRenderTelemetry.BeginPhase("disabled");
    }

    private sealed class RecordingScope : IDisposable
    {
        private readonly IList<string> _events;
        private readonly string _owner;
        private readonly string _phase;
        private bool _disposed;

        public RecordingScope(IList<string> events, string owner, string phase)
        {
            _events = events;
            _owner = owner;
            _phase = phase;
            _events.Add($"begin:{owner}:{phase}");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _events.Add($"end:{_owner}:{_phase}");
        }
    }
}
