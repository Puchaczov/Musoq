# Performance Testing Guide

This document describes the comprehensive performance testing framework implemented for Musoq.

## Performance Test Types

### 1. Execution Performance Tests
Tests the runtime performance of executing compiled queries:
- **ExecutionBenchmark** - Standard tests comparing parallel vs sequential execution
- **ExtendedExecutionBenchmark** - Extended tests covering various query patterns (SELECT, filter, aggregation, sorting)

### 2. Compilation Performance Tests  
Tests the performance of SQL parsing and C# code generation:
- **CompilationBenchmark** - Measures time to parse SQL and compile to executable assemblies
- Includes tests for different query complexities and parallelization modes

## Running Performance Tests

### Local Development
```bash
# Standard execution benchmarks
dotnet run --configuration Release -- --track-performance

# Extended execution benchmarks  
dotnet run --configuration Release -- --track-performance --extended

# Compilation/build performance benchmarks
dotnet run --configuration Release -- --track-performance --compilation

# Generate README performance section
dotnet run --configuration Release -- --readme-gen
```

### CI/CD Integration
Performance tests run automatically on:
- **All branch pushes** - Performance data collected for every branch
- **Pull requests** - Performance comparison available for PR review
- **Manual dispatch** - Can trigger specific benchmark types via GitHub Actions

## Available Benchmark Types

| Benchmark Type | Command Flag | Description |
|---------------|--------------|-------------|
| Standard | (default) | Basic parallel vs sequential execution tests |
| Extended | `--extended` | Comprehensive query pattern testing (7 different patterns) |
| Compilation | `--compilation` | SQL parsing and C# compilation performance tests |

## Performance Data Storage
- Historical data stored in `performance-data/performance-history.json`
- Performance reports generated in `performance-reports/` directory  
- README integration via `performance-reports/performance-section.md`
- Automatic data retention (last 100 results per benchmark)

## Branch-Level Performance Tracking
The GitHub Actions workflow now runs on all branches, enabling:
- Performance comparison between branches
- Early detection of performance regressions in feature branches
- Performance impact assessment during code review

## Example Performance Data
The compilation benchmarks typically measure:
- **SQL Parsing**: ~1-5ms for simple queries, ~10-50ms for complex queries
- **Full Compilation**: ~80-100ms including C# code generation and assembly compilation
- **Parallelization Impact**: ~5-10% difference between Full and None parallelization modes