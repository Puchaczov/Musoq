# Releasing Musoq

Musoq releases are tag driven. Ordinary `master` CI validates code and packages, but it never publishes to NuGet.

## Release Types

Use a full-train release for majors, cross-package API or behavior changes, dependency minimum changes, or changes that users should receive by upgrading only `Musoq.Converter`.

```powershell
# Full train: all five package versions must match the tag.
pwsh scripts/release/Validate-Release.ps1 -Tag v17.0.0-alpha.1
git tag -a v17.0.0-alpha.1 -m "Release v17.0.0-alpha.1"
git push origin v17.0.0-alpha.1
```

Use a package-specific release only for backward-compatible minor or patch changes where downstream packages do not need source changes or dependency minimum updates.

```powershell
# Example parser-only preview patch.
# First update MusoqParserVersion in scripts/Versions.props.
pwsh scripts/release/Validate-Release.ps1 -Tag parser/v17.0.1-preview.1
git tag -a parser/v17.0.1-preview.1 -m "Release parser v17.0.1-preview.1"
git push origin parser/v17.0.1-preview.1
```

Package-specific major tags such as `parser/v18.0.0` are intentionally rejected. Use a full-train tag such as `v18.0.0`.

## Version Updates

Package versions live in [scripts/Versions.props](scripts/Versions.props).

```xml
<MusoqParserVersion>10.0.1</MusoqParserVersion>
<MusoqPluginsVersion>13.0.0</MusoqPluginsVersion>
<MusoqSchemaVersion>15.0.0</MusoqSchemaVersion>
<MusoqEvaluatorVersion>16.0.0</MusoqEvaluatorVersion>
<MusoqConverterVersion>10.0.3</MusoqConverterVersion>
```

Do not hardcode package versions in individual package projects. The release scripts validate the evaluated MSBuild `Version` against the pushed release tag.

## Local Release Validation

Run these before tagging:

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

pwsh scripts/release/Validate-Release.ps1 -Tag parser/v17.0.1-preview.1
pwsh scripts/release/Pack-Release.ps1 -Tag parser/v17.0.1-preview.1 -OutputPath artifacts/nupkgs
pwsh scripts/release/Test-PackageSmoke.ps1 -Tag parser/v17.0.1-preview.1 -PackageDirectory artifacts/nupkgs
```

For CI-style pack validation of all release packages without a tag:

```powershell
pwsh scripts/release/Pack-Release.ps1 -AllPackages -OutputPath artifacts/ci-nupkgs
```

## Maintenance Branch Hotfixes

When the current major is newer than the line you need to patch, branch from the old release tag:

```powershell
git switch -c release/17.x v17.0.0
git cherry-pick <fix-commit>
# Update only the relevant version property in scripts/Versions.props.
git commit -am "chore: release parser v17.0.1"
git tag -a parser/v17.0.1 -m "Release parser v17.0.1"
git push origin release/17.x
git push origin parser/v17.0.1
```

Forward-port the fix back to `master` unless it is intentionally obsolete:

```powershell
git switch master
git pull --ff-only
git cherry-pick <fix-commit>
```

## GitHub And NuGet Security Setup

The repository enforces safe tag parsing, selected-package packing, least-privilege workflow permissions, CODEOWNER coverage for release-critical files, and a separate `nuget-production` environment for publishing.

Maintainers must configure these external settings:

- Protect release tags matching `v*` and `*/v*`.
- Restrict who can push release tags.
- Require reviewers on the `nuget-production` environment.
- Disable workflows creating or approving pull requests.
- Enable Dependabot alerts and code scanning for workflows where available.
- Configure NuGet Trusted Publishing for `.github/workflows/publish.yml` and each package owner/package combination.
- Add `NUGET_USER` as the NuGet account/organization used by Trusted Publishing.
- Keep `NUGET_MUSOQ_KEY` only as a temporary fallback until Trusted Publishing is verified.

Trusted Publishing is preferred because `NuGet/login@v1` exchanges GitHub OIDC identity for a short-lived NuGet API key. The fallback secret is intentionally only available in the publish job after release validation, packing, smoke testing, and `nuget-production` environment approval.

## GitHub Release Behavior

- `v18.0.0` publishes all five packages and creates a stable GitHub Release.
- `v17.0.0-alpha.1` publishes all five packages and creates a prerelease GitHub Release.
- `v18.0.0-preview.1` publishes all five packages and creates a prerelease GitHub Release.
- `parser/v17.0.1-preview.1` publishes only `Musoq.Parser` and creates a prerelease GitHub Release.
- Manual `workflow_dispatch` runs are dry runs: they validate, build, test, pack, and smoke-test but do not publish.

## Release Notes

`CHANGELOG.md` is the permanent release history. Curated GitHub Release bodies live under `release-notes/` and are named after the exact release tag:

```text
release-notes/v17.0.0-alpha.1.md
release-notes/parser/v17.0.1-preview.1.md
```

When a matching release-notes file exists, `.github/workflows/publish.yml` uses it as the GitHub Release body. If no matching file exists, the workflow falls back to GitHub-generated release notes.

Update the changelog and release-notes file before pushing the release tag.
