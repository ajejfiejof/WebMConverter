# Security Policy

## Reporting a vulnerability

Report vulnerabilities via
<https://github.com/ajejfiejof/WebMConverter/issues>
(mark the issue as security-sensitive) or contact the maintainers directly.
Do not open public issues with exploit details before a fix is available.

We aim to acknowledge reports within a week and will credit reporters in the
CHANGELOG unless anonymity is requested.

## Supply chain: SBOM

Generate a [CycloneDX](https://cyclonedx.org/) SBOM (`bom.json`, spec 1.7)
covering all NuGet dependencies of the solution at release time, and attach
it to the release:

```bash
dotnet tool install --global CycloneDX
dotnet-CycloneDX WebMConverter.sln --json --output sbom/
```

Generated SBOMs are build artifacts, not source: do not commit `bom.json`
(see CONTRIBUTING.md).

## Dependency hygiene

* NuGet packages are pinned to exact versions in the project files.
* New dependencies need a justification in the PR (license compatibility
  with MIT, maintenance status, platform support incl. Linux).
* The `WebMConverter.Core` library must stay free of UI and
  Windows-only calls except behind explicit `RuntimeInformation` guards.
