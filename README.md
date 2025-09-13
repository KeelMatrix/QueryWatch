# KeelMatrix.QueryWatch

[![Build](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/KeelMatrix.QueryWatch.svg)](https://www.nuget.org/packages/KeelMatrix.QueryWatch/)

**For developers building tools and libraries, `KeelMatrix.QueryWatch` delivers a working NuGet package in minutes without the usual boilerplate.**

This repository is a template that bundles together a library project, an xUnit test project, a sample console application, common build configuration, GitHub workflows, licensing and telemetry stubs, and a full suite of documentation. It allows you to start publishing high‑quality NuGet packages with minimal effort.

## Getting started

1. Clone or download this template.
2. Rename the solution and default project names when prompted to match your package name.
3. Open the solution in Visual Studio 2022 or run `dotnet build` from the command line.

### Install from NuGet

```bash
dotnet add package KeelMatrix.QueryWatch
```

### Quickstart

Here's how to use the sample API exposed by the template:

```csharp
using KeelMatrix.QueryWatch;

var hello = new Hello();
Console.WriteLine(hello.Greet("World"));
```

You can also explore the sample console application by running:

```bash
cd samples/KeelMatrix.QueryWatch.Sample
dotnet run
```

## Target frameworks

This package targets the following frameworks:

* `net8.0` – for modern .NET applications.
* `netstandard2.0` – for broad compatibility with .NET Framework and .NET Core.

## Versioning and releases

This project follows [Semantic Versioning](https://semver.org/). Breaking changes or removal of a target framework require a new major version. New features that do not break existing behavior increment the minor version. Patch versions are used for bug fixes and small improvements. Pre‑release packages use suffixes such as `-alpha`, `-beta`, or `-rc`.

Release notes are maintained in [`CHANGELOG.md`](CHANGELOG.md). To create a new release:

1. Update the version in the library’s `.csproj` file and add an entry in the changelog.
2. Commit your changes and tag the commit (for example `git tag v1.0.0`).
3. Push the tag. GitHub Actions will build, sign, and publish the package to nuget.org (assuming you have configured `NUGET_API_KEY` in your repository secrets).

## Documentation

* [`LICENSE`](LICENSE) – The license this project is released under (MIT by default).
* [`SECURITY.md`](SECURITY.md) – How to report security issues.
* [`CONTRIBUTING.md`](CONTRIBUTING.md) – Guidelines for contributors.
* [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) – The code of conduct for this project.
* [`PRIVACY.md`](PRIVACY.md) – Information about telemetry and how to disable it.

## FAQ

### How do I build and test the package locally?

Run the following commands from the repository root:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet pack --configuration Release --no-build --output ./artifacts/packages
```

The resulting `.nupkg` and `.snupkg` files can be found in `./artifacts/packages`.

### How do I consume the package without publishing it to NuGet?

Update your `NuGet.config` to add a local feed pointing at the `artifacts/packages` folder. For example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local" value="./artifacts/packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Then run `dotnet restore` in your consuming project.

### How do I add paid features or telemetry?

The template includes stub interfaces `ILicenseValidator` and `ITelemetryClient` with no‑op implementations. Replace these with your own implementations and wire them into your API as needed. See comments in the source code for guidance.

---

_This README is intentionally generic. Replace the sample code and descriptive text with information relevant to your package._

## Why this template (promise line)

For developers shipping .NET libraries quickly: this template gets you from **idea → publishable NuGet** in minutes, with tests, CI, SourceLink, symbols, docs, and repo hygiene baked in.

## Supported TFMs

- `net8.0`
- `netstandard2.0`

## Release & versioning policy

- **SemVer**: PATCH=fixed bugs, MINOR=new features (no breaking changes), MAJOR=breaking changes or dropping a TFM.
- Use pre-releases: `-alpha`, `-beta`, `-rc` as needed.
- Tag releases as `vX.Y.Z`; CI picks up tags to publish.
- Maintain `CHANGELOG.md` for notable changes.

## NuGet ID & branding

- Choose a consistent **package ID prefix** (e.g., `KeelMatrix.*`).
- When ready, **reserve your prefix** on nuget.org (TODO: add link).
- Set **Authors**, **RepositoryUrl**, **PackageProjectUrl**, **icon** in the `.csproj`.

## CI artifacts

CI uploads built packages to `./artifacts/packages` as workflow artifacts you can download.

## Licensing & monetization stubs

This template includes `ILicenseValidator` + `NoopLicenseValidator` and doc notes to wire a MoR (Paddle/Lemon Squeezy). Mark paid features in code and validate via your chosen MoR before enabling. Include an **offline grace** policy (e.g., 7–30 days).

## Telemetry (optional, off by default)

Implements `ITelemetryClient` with a no‑op default. If you later enable telemetry, publish a clear privacy note and allow opt‑out via `TOOLNAME_NO_TELEMETRY=1`.

## Release checklist

- [ ] Tests pass on Release configuration
- [ ] `dotnet pack` produces one `.nupkg` and one `.snupkg`
- [ ] Version bumped in the `.csproj`
- [ ] Tag created `vX.Y.Z`
- [ ] CI artifacts verified; (optional) nuget.org push succeeded


> **Note:** Consider adopting [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) later for repo-driven versioning (optional; not bundled in the template).
