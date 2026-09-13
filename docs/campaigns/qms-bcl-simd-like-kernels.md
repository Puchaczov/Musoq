# BCL SIMD qualification for LIKE ASCII guards

## Decision

Use `System.Text.Ascii.IsValid` for LIKE comparison spans longer than eight UTF-16 code units. Retain the scalar, allocation-free probe for spans of zero through eight code units and name it `IsAsciiUpToEightCodeUnits`, with `ScalarAsciiProbeLimit = 8` documenting the boundary.

No handwritten `Vector128`, `Vector256`, or `Vector512` kernel was added. Both long-span candidates are .NET BCL operations; the selected implementation delegates hardware dispatch and scalar fallback to the runtime.

## Measurement identity

- Source parent: `a06440129` (`perf(evaluator): streamline high-cardinality LIKE preparation`).
- Candidate tree: the QMS-R18 worktree containing only the benchmark/probe changes described here; the final QMS-R19 receipt records the committed R18 SHA.
- BenchmarkDotNet: 0.15.8, `ShortRun` (one launch, three warmups, three measurements).
- SDK: 10.0.303.
- Runtime: .NET 10.0.11, x64 RyuJIT.
- OS: Windows 11 25H2, build 10.0.26200.9445.
- CPU: Intel Core Ultra 9 285K, 24 physical/logical cores.
- Enabled hardware: AVX2, AVX, BMI1/2, SSE through SSE4.2, AES, PCLMUL, FMA, and related reported intrinsics; vector size 256 bits.
- Fixture: lengths 1, 4, 8, 9, 16, 64, 256, and 4,096, with all-ASCII input or one non-ASCII code unit at the first, middle, or last position.
- Both operations allocated zero bytes in every measured case.

The three isolated normal-intrinsics cohorts used the same command, changing only the artifact suffix from 1 through 3:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj `
  -c Release --no-build -- `
  --filter "Musoq.Benchmarks.AsciiLikeSpanBenchmark.*" `
  --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-r18-ascii-1"
```

The complete reports are:

- `BenchmarkDotNet.Artifacts/qms-r18-ascii-1/results/Musoq.Benchmarks.AsciiLikeSpanBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/qms-r18-ascii-2/results/Musoq.Benchmarks.AsciiLikeSpanBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/qms-r18-ascii-3/results/Musoq.Benchmarks.AsciiLikeSpanBenchmark-report-full-compressed.json`

Raw BenchmarkDotNet artifacts remain ignored. Each report contains all 64 benchmarks and no missing statistics.

## Selection gates

The candidate is selected only when its median-of-three long-input geometric mean is no greater than `0.95x` the current primitive and no individual median-of-three case is greater than `1.03x`.

| Cohort | Long-input geometric mean | Worst case | Result |
| --- | ---: | ---: | --- |
| 1 | 0.3534x | 0.5049x | Pass |
| 2 | 0.3538x | 0.5106x | Pass |
| 3 | 0.3564x | 0.5217x | Pass |
| Median of three per case | **0.3532x** | **0.5053x** | **Select `Ascii.IsValid`** |

“Long input” means lengths 64, 256, and 4,096 across all four character-position cases. The ratio matrix below is `Ascii.IsValid / ContainsAnyExceptInRange`, using each operation's median across the three isolated reports.

| Length | ASCII | Non-ASCII first | Non-ASCII middle | Non-ASCII last |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 0.4983x | 0.4973x | 0.4932x | 0.4861x |
| 4 | 0.2895x | 0.4940x | 0.3168x | 0.2284x |
| 8 | 0.4956x | 0.4936x | 0.4942x | 0.4979x |
| 9 | 0.4963x | 0.5053x | 0.4984x | 0.4930x |
| 16 | 0.4982x | 0.4918x | 0.4940x | 0.4921x |
| 64 | 0.3690x | 0.4937x | 0.3360x | 0.3688x |
| 256 | 0.3909x | 0.4976x | 0.3716x | 0.3905x |
| 4,096 | 0.2512x | 0.4887x | 0.1967x | 0.2450x |

The aggregate long-input result is 64.68% less time than the current general range-search primitive. The worst measured case is still 49.47% less time, so the 3% per-case regression safeguard passes with substantial margin.

## Intrinsic diagnostics

The fixed 4,096-code-unit diagnostic used:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj `
  -c Release --no-build -- `
  --filter "Musoq.Benchmarks.AsciiLikeSpanIntrinsicDiagnosticBenchmark.*" `
  --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-r18-intrinsics-enabled"

$env:DOTNET_EnableHWIntrinsic = '0'
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj `
  -c Release --no-build -- `
  --filter "Musoq.Benchmarks.AsciiLikeSpanIntrinsicDiagnosticBenchmark.*" `
  --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-r18-intrinsics-disabled"
```

| Runtime mode | `ContainsAnyExceptInRange` | `Ascii.IsValid` | Ratio | Code size, current/candidate |
| --- | ---: | ---: | ---: | ---: |
| AVX2 enabled | 102.79 ns | 28.88 ns | 0.2810x | 550 B / 371 B |
| Hardware intrinsics disabled | 1,737.17 ns | 99.12 ns | 0.0571x | 77 B / 159 B |

With AVX2 enabled, the range-search disassembly uses `vpsubw`, `vpminuw`, `vpcmpeqw`, and `vptest`; the specialized ASCII validator uses `vmovups`, `vpor`, and `vptest` over YMM registers. With `DOTNET_EnableHWIntrinsic=0`, BenchmarkDotNet reports an empty hardware-intrinsics set and the emitted assembly contains no XMM or YMM instructions. Both paths still return the same values, and `Ascii.IsValid` retains a substantially faster scalar fallback.

The short scalar construction deliberately ORs up to eight UTF-16 code units and checks the combined high bits once. It avoids the call/setup cost of a general span primitive for tiny values while making the maximum supported width explicit; inputs longer than `ScalarAsciiProbeLimit` never enter that helper.
