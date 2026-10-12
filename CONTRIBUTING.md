# Contributing to WebMConverter

Please read the [Code of Conduct](CODE_OF_CONDUCT.md) first. All interaction
here follows it.

## Commit convention: Conventional Commits

Every commit message follows the
[Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
spec so history stays greppable and releases can be automated:

```
<type>(<scope>): <short imperative summary>

[optional body: what changed and why]

[optional footer: BREAKING CHANGE: ..., Refs: ...]
```

Types: `feat` (new feature), `fix` (bug fix), `docs`, `style`, `refactor`,
`test`, `chore`, `ci`, `build`, `revert`. Scopes are the area touched:
`core`, `app`, `caption`, `fonts`, `vapoursynth`, `packaging`, `ci`, etc.

Examples:

```
feat(core): add VapourSynth script generation
fix(core): apply crop filter exactly once in ffmpeg chain
test(core): add xUnit suite for Core (27 tests)
docs: rewrite README for the Avalonia cutover
```

Rules:

* One logical change per commit; commit incrementally.
* `BREAKING CHANGE:` footer when behavior changes incompatibly (e.g. the
  crop fix changed output pixels vs the legacy app).
* Bug-compatible ports of legacy quirks get a comment in code **and** a
  CHANGELOG note — never silently "fix" legacy output.

## Development

Requirements: .NET 8 SDK, plus `ffmpeg`/`ffprobe`/`ffplay` on PATH for the
end-to-end tests (`yt-dlp` optional).

```bash
dotnet build WebMConverter.sln -c Release
dotnet test WebMConverter.Core.Tests/WebMConverter.Core.Tests.csproj -c Release
dotnet run --project WebMConverter.Avalonia -c Release
```

* UI-free logic belongs in `WebMConverter.Core` (net8.0, no UI types). The
  Avalonia head must build and run on Linux with no Windows targeting.
* New Core behavior needs xUnit coverage in `WebMConverter.Core.Tests`.
  E2E tests must degrade to a logged no-op when ffmpeg is absent.
* Verify GUI changes by launching under Xvfb and screenshotting, as CI has
  no display.

## Pull requests

* Small, conventionally-committed, with tests for Core changes.
* Update CHANGELOG.md (unreleased section) for user-visible changes.
* Packaging changes must stay valid: `desktop-file-validate`, JSON/XML
  parsing, `bash -n` for scripts.

## Security

See [SECURITY.md](SECURITY.md). Every CI run publishes a CycloneDX SBOM
artifact (`bom.json`); do not commit generated SBOMs to the repo.
