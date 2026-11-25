# EarthRelief3D

A Windows (WPF, .NET 10) app that shows humanitarian crises on a 3D globe: conflict, food insecurity, disease
outbreaks, displacement, genocide and mass atrocities, prison camps, basic needs and where some resources sit, each
with its source and an "as of" date. It is built on the shared libraries GlobeCore and AstroCore (git submodules in
`external/`).

Status: Version 1.0.0 (Plan 5: shipping). Not yet released: see `docs/release.md` and `docs/plan-5-results.md`.
Public repository: https://github.com/johnovanni/EarthRelief3D (since
2026-11-22). Plan 2 built the four crisis layers from open, key-free sources: armed conflict (UCDP), food crises (IPC),
disease outbreaks (WHO) and people forced from home (UNHCR). Plan 3 (2026-11-23) added IPC food-crisis areas, the
curated atrocity designations and prison camps (drafts until the owner reviews them), the Needs shadings (water,
sanitation, health care, hunger, shelter need) and the Resources shadings (oil production, water stress). Task 7
(2026-11-24) added the Resources marker layers from Global Energy Monitor's trackers (CC BY 4.0): power plants of
500 MW or more coloured by fuel, renewable plants (wind, solar, hydro, geothermal, bioenergy) of 200 MW or more, oil
and gas fields, and steel plants of 1 million tonnes a year or more. Plan 4 (2026-11-24) added the Getting help in group, "Your one
small step", a plain-language explainer for each crisis kind, and the vetting tool the maintainer runs before a
release. Everything curated in Plan 4 (UN depots, pooled funds, contacts, charities, explainers) is a draft until the
owner approves it. Plan 5 (2026-11-25) added the signed installer and release workflow, the About, Data and Welcome
cards, text size, a combined source list and an update check.

## Features

- A 3D globe (GlobeCore) with country borders and a day/night shade.
- **Armed conflict:** a dot per UCDP event of a conflict with 25 or more deaths in a calendar year (counted over the
  whole conflict); bigger dots for more deaths of that conflict in that country over the data window (band 1 at least),
  lighter dots for newer events. Each event belongs to the country UCDP names, except that events in a territory UCDP
  does not list as a country (Gaza and the West Bank, coded Israel) belong to that territory.
- **Food crisis:** each country is shaded by IPC's 20% rule in the IPC's phase colour (Phase 3 Crisis, Phase 4
  Emergency, Phase 5 Catastrophe/Famine), with an outline for a worse phase that has people in it. Where IPC publishes
  areas for the same analysis (HDX's per-country GeoJSON), each area is shaded in IPC's own area classification instead.
- **Disease outbreaks:** a cross at each country with a WHO Disease Outbreak News report in the last 90 days, labelled
  with the disease.
- **People forced from home:** a ring per country of origin, its area growing with the number (refugees, asylum-seekers,
  internally displaced). Displacement is a layer value, not a crisis.
- **Genocide and mass atrocities:** a hollow triangle on a country with a current court judgment, UN body finding or
  government determination (with a statement from the last 12 months that the situation continues). Provisional
  measures, arrest warrants and pending cases are labelled as such and never draw a triangle alone; past designations
  are listed in the country panel's history. The rows are curated with citations (`data/curated`); each is shown only
  after the owner approves it (`docs/curated-review.md`). Settings chooses whose government determinations are shown.
- **Prison camps:** a square at each documented North Korean political prison camp (UN Commission of Inquiry and HRNK),
  curated and reviewed like the designations.
- **Needs** (one shading at a time, across all groups): the share of people without at least basic drinking water or
  sanitation and the essential health services not covered (WHO GHO), undernourishment (FAOSTAT) and people in need of
  shelter (UN humanitarian needs overviews). The country panel adds "Basic needs", "People in need" and "Resources".
- **Resources:** crude oil production (US EIA) and baseline water stress (WRI Aqueduct). FAOSTAT, EIA and Aqueduct are
  small extracts bundled in `data/datasets`, made by `tools/EarthRelief3D.DataTool` from the downloaded files.
- **Getting help in:** a brown Access shading (ACAPS humanitarian access scores, 0 to 5, read live from HDX and never
  stored in the repository), markers for the UN humanitarian response depots, scheduled-service airports and large and
  medium ports, and curved lines from the open country to the nearest depot, airport and port, each labelled with its
  straight-line distance (km, or miles in Settings). The country panel has a "Getting help in" section (ACAPS's nine
  access indicators, each as published) and appeal funding from OCHA's FTS, with a regional plan labelled as a figure for
  the whole plan.
- **Your one small step:** for a country in crisis, the UN pooled fund for it (or CERF), the vetted US charities
  working there (only entries with an IRS check, evidence and a rating the owner confirmed; never ranked, never with
  stars or scores, with links to the evaluators' own reports), and the official people to contact (your two senators once
  you choose a state in Settings, the House's own "find your representative" page, the State Department, the country's
  mission through the UN Blue Book). The app never handles money and never asks for an address.
- **Explainers:** a short, sourced, plain-language card for each crisis kind, "Access" and "Why these?", opened from a
  section's "What is this, and why does it happen?" link.
- **Vetting tool** (`src/EarthRelief3D.Vetting`): checks the charity list against IRS Publication 78, the revocation
  list and Form 990 files and writes a report for the owner; it never edits the list.
- A legend whose every line names its source and "as of" date (a shading's line says exactly what is measured, with the
  year, and "no figure: not shaded"); hover tips and a country panel (click a country or a
  marker) that list every source's finding with its source and date, or "No crisis recorded by these sources".
- Status dots for every source (the live feeds, Borders and the bundled extracts); the last good data is cached in your data folder and shown at once, offline too.
- No sound, no graphic images, no pop-up alarms and no personal data collected.

## Install

Download `EarthRelief3D-Setup.exe` from the [latest release](https://github.com/johnovanni/EarthRelief3D/releases/latest/download/EarthRelief3D-Setup.exe)
and run it. It is signed by John Apolloni. It installs the app, with the .NET runtime it needs, in
`C:\Program Files\Qapow\EarthRelief3D` (you can pick another folder) and adds "Qapow > EarthRelief3D" to the Start menu.
Your settings, cached data and logs are kept apart, in `%LOCALAPPDATA%\EarthRelief3D`; uninstalling leaves them there.

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
samples are in `tests/Samples` (refresh with `pwsh tools/fetch-samples.ps1`; the WHO and WHO GHO samples are synthetic).
The bundled extracts in `data/datasets` are rebuilt with `dotnet run --project tools/EarthRelief3D.DataTool -- fao|eia|aqueduct
<downloaded file> --published <date>` (the downloads go in the git-ignored `.downloads/`). The curated designations and
camp sites are in `data/curated`, listed for review in [docs/curated-review.md](docs/curated-review.md).

## Diagnostics

Command-line options for checking the app without a network or a screen capture (the pictures are the app drawing
itself to a PNG with WPF's `RenderTargetBitmap`):

| Option | Meaning |
|---|---|
| `--offline-samples <dir>` | Answer every feed request from the sample files in `<dir>` (use `tests\Samples`). The cache and settings live in a temporary folder that is deleted on exit (the log stays in the usual logs folder, `%LOCALAPPDATA%\EarthRelief3D\logs`), and the window title says SAMPLE DATA. |
| `--display-time <ISO UTC>` | Start the app's clock at that time (the sample data is from Nov 2026: `2026-11-22T12:00:00Z`). |
| `--snapshot-scenes <json> --snapshot-out <dir>` | Take one picture per scene, saved as `<dir>\<name>.png`, in one launch, then exit. Needs `--offline-samples`. Exit code 1 if any picture failed. |
| `--snapshot <png>` | One picture, with `--look-at lat,lon`, `--layers` (a comma list: at most one shading of food, water, sanitation, health, hunger, shelter, oil, waterstress, access, and any of the markers conflict, outbreaks, displacement, atrocities, camps, power, oilgas, steel, renewables, un-depots, airports, ports, hub-lines; default `conflict,food,outbreaks,displacement`) and `--open-panel ISO3`. Scenes files take the same names. |
| `--delay-ms N` | Wait N ms after the data is drawn before each picture (default 1500). |
| `--show-drafts` | Also show the curated rows the owner has not approved yet, each labelled "Draft: not yet reviewed"; the window title says DRAFTS SHOWN. |

A scenes file (`tests/SnapshotScenes/scenes.json`) is
`{"scenes":[{"name","displayTimeUtc","lookAt":[lat,lon],"layers":[...],"panel":"ISO3","panelFocus":"conflict|food|outbreaks|displacement|atrocities|camps|access|help","explainer":"food","showDrafts":true}]}`;
`showDrafts` shows the drafts for that picture only, as `--show-drafts` does for a whole run; `explainer` (an explainer
id: conflict, food, outbreaks, displacement, atrocities, camps, access or why-these; needs a panel) opens the explainer
card, and a scene must list `hub-lines` to show the arcs.
Runs repeat exactly: the display clock stands still at each scene's time, the feeds' clock stands still at the first
scene's time (so the feeds run once and their "checked" dates do not move), and the camera moves at once.

```
src\EarthRelief3D.App\bin\Release\net10.0-windows\EarthRelief3D.App.exe --offline-samples tests\Samples ^
  --snapshot-scenes tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-baseline
```

To compare two runs, use EarthWatch3D's PngDiff, referenced by path (it is not copied into this repo):
`dotnet run --project C:\repos\EarthWatch3D\tools\PngDiff -c Release -- artifacts\snapshot-baseline artifacts\snapshot-baseline-2`
(exit code 0 only when every picture is identical). Pictures go to `artifacts\`, which git ignores. Results of the
first baseline are in [docs/plan-2-results.md](docs/plan-2-results.md), of Plan 3's nine scenes in
[docs/plan-3-results.md](docs/plan-3-results.md) and of Plan 4's fourteen in [docs/plan-4-results.md](docs/plan-4-results.md).

## Licence

MIT, see [LICENSE](LICENSE). Data from the sources keeps its own licence (see data-sources.md).
