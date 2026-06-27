## Troubleshooting

### Common Issues
- **Build failures**: Usually missing .NET 10.0.300+ SDK or corrupted package cache
- **Test failures**: Often related to environment-specific paths or test data
- **Memory issues during development**: Expected due to runtime code generation
- **Package conflicts**: Use `dotnet clean` then rebuild if dependency issues occur

### Development Environment Issues
- **"Permission denied" during benchmarks**: This is normal - benchmarks will run but without high priority
- **Temp file conflicts**: Delete `/tmp/Musoq` folder if compilation conflicts occur
- **Assembly loading errors**: Restart development session if assembly conflicts persist

### Debugging Failed Tests
```bash
# Run a specific failing test with concise but useful failure output
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity quiet --logger "console;verbosity=normal"

# Escalate to detailed output only after the narrowed run is insufficient
dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity minimal --logger "console;verbosity=detailed"

# Run tests in isolation to identify environment conflicts
dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --collect:"XPlat Code Coverage"
```
