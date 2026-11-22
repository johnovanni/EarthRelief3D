# EarthRelief3D

A Windows (WPF, .NET 10) app that shows humanitarian crises on a 3D globe: conflict, food insecurity, disease
outbreaks and displacement, each with its source and an "as of" date. It is built on the shared libraries
GlobeCore and AstroCore (git submodules in `external/`).

Status: version 0.1.0, work in progress (a local repo with no remote yet). It shows four crisis layers from
open, key-free sources: armed conflict (UCDP), food crises (IPC), disease outbreaks (WHO) and people forced from home
(UNHCR). The Needs, Resources and Getting help in groups are named in the layer strip and come in a later version.

## Features

- A 3D globe (GlobeCore) with country borders and a day/night shade.
- **Armed conflict:** a dot per UCDP event of a conflict with 25 or more deaths in a calendar year; bigger dots for
  more deaths in the conflict, lighter dots for newer events.
- **Food crisis:** each country with people in IPC Phase 3 or higher is shaded in the IPC's phase colour (Phase 3 Crisis,
  Phase 4 Emergency, Phase 5 Catastrophe/Famine).
- **Disease outbreaks:** a cross at each country with a WHO Disease Outbreak News report in the last 90 days, labelled
  with the disease.
- **People forced from home:** a ring per country of origin, its area growing with the number (refugees, asylum-seekers,
  internally displaced). Displacement is a layer value, not a crisis.
- A legend whose every line names its source and "as of" date; hover tips and a country panel (click a country or a
  marker) that list every source's finding with its source and date, or "No crisis recorded by these sources".
- Status dots for the four sources; the last good data is cached in your data folder and shown at once, offline too.
- No sound, no graphic images, no pop-up alarms and no personal data collected.

## Build

```
git submodule update --init --recursive   # relative submodule URLs; needs sibling AstroCore and GlobeCore repos
dotnet build EarthRelief3D.sln
dotnet test EarthRelief3D.sln
```

Requires the .NET 10 SDK on Windows. In a very deep folder, `Directory.Build.props` moves bin/obj to a short folder
under `%TEMP%\EarthRelief3D.build`.

## Data

See [docs/data-sources.md](docs/data-sources.md) for each source, its licence and its "as of" field, and
[docs/severity-bands.md](docs/severity-bands.md) for how each source's own scale sets marker sizes and colours. Test
samples are in `tests/Samples` (refresh with `pwsh tools/fetch-samples.ps1`; the WHO sample is synthetic).

## Diagnostics

Command-line options for checking the app without a network or a screen capture (the pictures are the app drawing
itself to a PNG with WPF's `RenderTargetBitmap`):

| Option | Meaning |
|---|---|
| `--offline-samples <dir>` | Answer every feed request from the sample files in `<dir>` (use `tests\Samples`). The cache and settings live in a temporary folder that is deleted on exit, and the window title says SAMPLE DATA. |
| `--display-time <ISO UTC>` | Start the app's clock at that time (the sample data is from Nov 2026: `2026-11-22T12:00:00Z`). |
| `--snapshot-scenes <json> --snapshot-out <dir>` | Take one picture per scene, saved as `<dir>\<name>.png`, in one launch, then exit. Needs `--offline-samples`. Exit code 1 if any picture failed. |
| `--snapshot <png>` | One picture, with `--look-at lat,lon`, `--layers conflict,food,outbreaks,displacement` (default all) and `--open-panel ISO3`. |
| `--delay-ms N` | Wait N ms after the data is drawn before each picture (default 1500). |

A scenes file (`tests/SnapshotScenes/scenes.json`) is
`{"scenes":[{"name","displayTimeUtc","lookAt":[lat,lon],"layers":[...],"panel":"ISO3","panelFocus":"conflict|food|outbreaks|displacement"}]}`.
Runs repeat exactly: the display clock stands still at each scene's time, the feeds' clock stands still at the first
scene's time (so the feeds run once and their "checked" dates do not move), and the camera moves at once.

```
src\EarthRelief3D.App\bin\Release\net10.0-windows\EarthRelief3D.App.exe --offline-samples tests\Samples ^
  --snapshot-scenes tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-baseline
```

To compare two runs, use EarthWatch3D's PngDiff, referenced by path (it is not copied into this repo):
`dotnet run --project C:\repos\EarthWatch3D\tools\PngDiff -c Release -- artifacts\snapshot-baseline artifacts\snapshot-baseline-2`
(exit code 0 only when every picture is identical). Pictures go to `artifacts\`, which git ignores. Results of the
first baseline are in [docs/plan-2-results.md](docs/plan-2-results.md).

## Licence

MIT, see [LICENSE](LICENSE). Data from the sources keeps its own licence (see data-sources.md).
