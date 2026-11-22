# EarthRelief3D

A Windows (WPF, .NET 10) app that shows humanitarian crises on a 3D globe: conflict, food insecurity, disease
outbreaks and displacement, each with its source and an "as of" date. It is built on the shared libraries
GlobeCore and AstroCore (git submodules in `external/`).

Status: version 0.1.0, work in progress. Plan 2 (foundation) covers crises only: the data sources, their parsing
and the crisis rules. The globe and its layers come in later plans. This is a local repo with no remote yet.

## Build

```
git submodule update --init --recursive   # relative submodule URLs; needs sibling AstroCore and GlobeCore repos
dotnet build EarthRelief3D.sln
dotnet test EarthRelief3D.sln
```

Requires the .NET 10 SDK on Windows. In a very deep folder, `Directory.Build.props` moves bin/obj to a short folder
under `%TEMP%\EarthRelief3D.build`.

## Data

See [docs/data-sources.md](docs/data-sources.md) for each source, its licence and its "as of" field. Test samples are
in `tests/Samples` (refresh with `pwsh tools/fetch-samples.ps1`; the WHO sample is synthetic). The app collects no
personal data and plays no sound.

## Licence

MIT, see [LICENSE](LICENSE). Data from the sources keeps its own licence (see data-sources.md).
