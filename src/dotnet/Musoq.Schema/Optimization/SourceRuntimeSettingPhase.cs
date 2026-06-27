using System;

namespace Musoq.Schema.Optimization;

[Flags]
public enum SourceRuntimeSettingPhase
{
    None = 0,
    Metadata = 1,
    Planning = 2,
    Execution = 4,
    All = Metadata | Planning | Execution
}
