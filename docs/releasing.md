# Releasing Musoq

Musoq publication is tag-driven. The `Publish` workflow validates the tag,
qualifies the exact tagged commit, packages the selected train, smoke-tests the
packages, publishes them to NuGet, and creates the prerelease or stable GitHub
Release.

## Alpha releases from any branch

Alpha tags may be created from any branch once that branch contains the current
release workflow policy. Push the annotated tag directly; a branch push or merge
to `master` is not required.

```powershell
$tag = 'v17.0.12-alpha.1'
git tag -a $tag HEAD -m "Release $tag"
git push origin "refs/tags/$tag"
```

Full-train alpha tags use `v<version>-alpha.<n>`. Package-specific alpha tags
use `<package>/v<version>-alpha.<n>`, such as
`parser/v17.0.12-alpha.1`.

The alpha tag starts an exact-commit `CI` push run and the tag-triggered
`Publish` workflow. Publish waits for that CI run to complete successfully
before any package or GitHub Release is made. Pull-request runs do not qualify
the tag.

If alpha CI fails, the tag remains immutable and nothing is published. Fix the
candidate and use the next alpha version; never move or force-push the failed
tag.

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
workflow change before using the any-branch alpha path.

## Local qualification

Run the release scripts, build, tests, package pack, and package smoke checks
before publishing. The release gate does not require benchmarks.

```powershell
pwsh -NoProfile -File scripts/release/Test-ReleaseScripts.ps1
pwsh -NoProfile -File scripts/release/Validate-Release.ps1 -Tag $tag
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

$out = "artifacts/release-$($tag.TrimStart('v').Replace('/', '-'))"
pwsh -NoProfile -File scripts/release/Pack-Release.ps1 -Tag $tag -OutputPath $out
pwsh -NoProfile -File scripts/release/Test-PackageSmoke.ps1 -Tag $tag -PackageDirectory $out
```

`workflow_dispatch` on `Publish` is a dry run. It validates, builds, packs, and
smoke-tests without publishing. For a real release, verify the tag-triggered
workflow, the GitHub Release assets, and every selected package/version in the
NuGet flat container after indexing completes.
