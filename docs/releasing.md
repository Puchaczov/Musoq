# Releasing Musoq

Musoq publication is tag-driven. The `Publish` workflow validates the tag,
qualifies the exact tagged commit, packages the selected train, publishes the
packages to NuGet, and creates the prerelease or stable GitHub Release.

## Qualify the candidate before tagging

Every release requires successful Linux and Windows `CI` push qualification for
the exact candidate commit before its tag is pushed. CI runs on pushes to `master`
and `release/**`, and on pull requests targeting `master`. Pull-request runs do not
qualify publication. Release tags start `Publish` without starting another full
test suite; Publish reuses the successful branch push run and retains its build
and package validation.

## Alpha releases

An alpha candidate may originate on any branch. If its exact commit does not
already have successful qualifying branch CI, push it to a `release/**` branch
and wait for both platform jobs. A merge to `master` is not required.

```powershell
$branch = 'release/17.0.12-alpha.1'
git push origin "HEAD:$branch"
# Wait for successful Linux and Windows CI on this exact commit.

$tag = 'v17.0.12-alpha.1'
git tag -a $tag HEAD -m "Release $tag"
git push origin "refs/tags/$tag"
```

Full-train alpha tags use `v<version>-alpha.<n>`. Package-specific alpha tags
use `<package>/v<version>-alpha.<n>`, such as
`parser/v17.0.12-alpha.1`.

If candidate CI fails, fix the candidate and requalify before tagging. If a
source fix is needed after a tag has been pushed, use the next alpha version;
never move or force-push a release tag.

## Preview, beta, and stable releases

Non-alpha releases require a `release/**` branch. Push the candidate branch and
wait for its exact-commit CI push run before pushing the tag.

```powershell
$branch = 'release/17.0.12'
git push origin "HEAD:$branch"
# Wait for successful CI on the exact branch tip.

$tag = 'v17.0.12-beta.1'
git tag -a $tag HEAD -m "Release $tag"
git push origin "refs/tags/$tag"
```

Preview releases follow the same non-alpha branch policy. The Publish gate
requires successful `CI` evidence whose `head_branch` matches `release/**`;
successful `master` or pull-request runs are insufficient.

Release candidates do not need to be merged into `master`. The policy workflow
change itself must first reach `master` so that future release branches inherit
the trigger and qualification rules. Older branches must incorporate that
workflow change before using branch-first publication without duplicate tag CI.

## Repository boundary

A Core package release changes and publishes only this repository. Do not modify
any other repositories, synchronize their documentation, rebuild their
consumers, or install packages into their checkouts as part of Core publication.
Those repositories have separate upgrade, documentation, validation, and release
waves. Record downstream migration or documentation requirements in the Core
release notes; read-only inspection does not authorize downstream changes.

Cross-repository work requires an explicit request for the named repository and
action. The release scripts and publication workflow operate within the Core
checkout and publish only the packages selected from its registry.

## Local qualification

Run the release scripts, build, tests, and package pack before publishing.
The release gate does not require benchmarks.

```powershell
pwsh -NoProfile -File scripts/release/Test-ReleaseScripts.ps1
pwsh -NoProfile -File scripts/release/Validate-Release.ps1 -Tag $tag
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

$out = "artifacts/release-$($tag.TrimStart('v').Replace('/', '-'))"
pwsh -NoProfile -File scripts/release/Pack-Release.ps1 -Tag $tag -OutputPath $out
```

`workflow_dispatch` on `Publish` is a dry run. It validates, builds, and packs
without publishing. For a real release, verify the tag-triggered
workflow, the GitHub Release assets, and every selected package/version in the
NuGet flat container after indexing completes.

## Formatter package qualification

The engine package list in `packages.json` composes the separate
`formatter-packages.json` manifest through `additionalManifests`. Every release
command validates the combined catalog, including duplicate IDs and slugs.
`Musoq.Formatter` is versioned by `MusoqFormatterVersion`.
Its only runtime package dependency is `Musoq.Parser`. Existing package versions
are unchanged by introducing it. A package-specific candidate uses
`formatter/v<version>` with the usual qualification and publication policy.

For local package checks without publication, run `Test-ReleaseScripts.ps1` and
pack with `Pack-Release.ps1 -AllPackages`. CI runs the release-tooling checks and
packs all registered packages.
