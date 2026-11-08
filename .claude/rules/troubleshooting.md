## Troubleshooting

### Common Issues
- **Build failures**: Usually missing .NET 10.0.401+ SDK or corrupted package cache
- **Test failures**: Often related to environment-specific paths or test data
- **Memory issues during development**: Expected due to runtime code generation
- **Package conflicts**: Use `dotnet clean` then rebuild if dependency issues occur

### Development Environment Issues
- **"Permission denied" during benchmarks**: This is normal - benchmarks will run but without high priority
- **Temp file conflicts**: Identify the exact files and owning process before cleanup. Temporary locations vary by OS; compiler diagnostics use `Path.Combine(Path.GetTempPath(), "Musoq")`. Remove only files owned by the completed task, preserve other runs, and keep task-generated diagnostics under repository-root `.artifacts/` when the output location is configurable.
- **Assembly loading errors**: Restart development session if assembly conflicts persist

### Debugging Failed Tests
```bash
# Run a specific failing test with concise but useful failure output
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity quiet --logger "console;verbosity=normal"

# Escalate to detailed output only after the narrowed run is insufficient
dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity minimal --logger "console;verbosity=detailed"

# Run tests in isolation to identify environment conflicts
dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --collect:"XPlat Code Coverage"
```

## Critical Timing Expectations

### Build Commands - NEVER CANCEL These Operations
- **dotnet restore**: 15-30 seconds depending on cache state (measured: ~17s)
- **dotnet build**: 20-30 seconds for Release configuration (measured: ~24s)
- **dotnet test**: full solution testing can take a few minutes depending on cache state and current test count
- **Individual module tests**: varies by module
  - Parser, Schema, Plugins, and Converter are usually quick
  - Evaluator is the largest suite because it covers query planning, rendering, compilation, and runtime execution

### Memory and Performance
- **The system generates and compiles C# code at runtime**
- **Memory usage increases during compilation phases**
- **Performance tests validate query execution times**
- **Benchmarks project provides performance measurement tools**
