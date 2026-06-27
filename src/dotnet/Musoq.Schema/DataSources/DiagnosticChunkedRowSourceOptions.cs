namespace Musoq.Schema.DataSources;

public sealed record DiagnosticChunkedRowSourceOptions(int CapacityInChunks = 4);
