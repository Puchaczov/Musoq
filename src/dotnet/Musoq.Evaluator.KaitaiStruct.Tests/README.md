# Kaitai binary compatibility tests

This MSTest project compiles Musoq SQL and executes it in-process against
Kaitai-derived binary fixtures. It uses the Core projects in this checkout and
does not require an installed Musoq executable or external reference tools.

Run the suite from the repository root:

```powershell
dotnet test src/dotnet/Musoq.Evaluator.KaitaiStruct.Tests -c Release --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

The suite contains 200 cases and 204 SQL queries using 47 binary fixtures.
Each case directory contains its SQL and the expected values used by the
compatibility test. Fixtures are under `Upstream/src/`; the MIT notice and
attribution are in this project directory.
