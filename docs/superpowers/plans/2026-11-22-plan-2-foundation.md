# Plan 2 — EarthRelief3D Foundation: App on GlobeCore, Crisis Layers, Country Panel

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A working EarthRelief3D Windows app. It shows the Blue Marble globe (on GlobeCore) with four crisis layers:
armed conflict (UCDP), food crises (IPC), disease outbreaks (WHO) and displacement (UNHCR). Each layer shows its
source and an "as of" date. Clicking a country or marker opens a per-country panel saying what is happening, with
sources.

**Architecture:**
- **`EarthRelief3D.Core`** (net10.0, no UI): the domain model (every fact carries a `SourceCitation`), one feed class
  per source (GlobeCore `IFeed`), a `CrisisStore` (GlobeCore `SnapshotStore`) cached on disk, and pure helpers for
  severity bands, trends and country resolution.
- **`EarthRelief3D.App`** (WPF): builds a GlobeCore `LayerHost` on `EarthBody`, adds one layer per crisis kind,
  and shows a country panel plus status dots.
- **GlobeCore additions:** `CountryIndex` (Earth pack) and a shared status-dots view.

**Tech Stack:** C# / .NET 10, WPF, HelixToolkit.Wpf 3.1.2 (through GlobeCore.Wpf), CommunityToolkit.Mvvm 8.4.0,
xunit 2.9.2.

**Spec:** `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md` (§1, §2, §4, §5.1,
§6, §11, §13, §14, calls C1, C4, C6, C7, C8, C11, C12, C16, and the Plan 1 rulings P1-1 to P1-22).

## Rulings made while writing this plan

| # | Ruling | Reason |
|---|---|---|
| P2-1 (R22) | **Lat/lon type:** apps and GlobeCore's Geo, rasterizer and feed APIs keep `AstroCore.Earth.GeoPosition`; `SurfacePoint` stays only on GlobeCore's body-generic API (`Body`, `GeoMath`, `IlluminationState`). The two convert implicitly | Both are east-positive (−180, 180] lat/lon; migrating every GlobeCore and EarthWatch3D signature would be churn with no behaviour gain |
| P2-2 | **Plan 2 sources need no key or account:** UCDP Candidate monthly and quarterly **CSV downloads** (the API needs a token; the files don't), IPC **national** figures from HDX (`global-acute-food-insecurity-country-data`, CC0), WHO Disease Outbreak News (the site's content API) and the UNHCR population API. HDX HAPI and its app identifier wait until a later plan | Checked live on 2026-11-22. Nothing waits on the owner |
| P2-3 | **Food crises are shown per country in Plan 2:** the country is shaded in the IPC colour of the highest phase that has people in it in the current period. The national file has no area polygons. IPC **area** polygons need an IPC API key or OCHA boundary files, so they move to Plan 3 | Spec §6 asks for IPC areas. This stages it without inventing areas |
| P2-4 | **Stale IPC analyses** (current period ended more than 12 months before now) are not crisis markers. The panel lists them as "Last IPC analysis: <period>" | C11 uses the current threshold; an old analysis isn't evidence of a current crisis |
| P2-5 | **The WHO test sample is synthetic** (same JSON shape, invented titles and text). No WHO text is committed | C6: CC BY-NC-SA data is never committed |
| P2-6 | **Shared status dots in GlobeCore:** GlobeCore.Wpf gets a generic status-dots view (from EarthWatch3D's StatusStrip, without MTG). EarthWatch3D switches to it in a later small plan, not here | Keeps this plan off a shipped app; the temporary duplicate is recorded |
| P2-7 | **EarthRelief3D is a local git repo only** (no GitHub remote until the owner asks), MIT licence like the other repos, version 0.1.0 | The owner's rule: nothing is pushed without asking |
| P2-8 | **UCDP window:** events from the newest quarterly cumulative Candidate file plus every monthly file published after it (about 9–12 months). Trend = deaths in the last 3 complete months against the 3 before | The downloads page carries the current quarterly and monthly files; older releases need the yearly GED (hundreds of MB) |

## Global Constraints

- .NET 10. `net10.0` for no-UI projects, `net10.0-windows` + `UseWPF` for WPF. `ImplicitUsings` and `Nullable` on.
- **Every fact on screen shows its source and an "as of" date.** Each layer's legend line, every panel section and
  every tooltip names the source and the as-of date (spec §1 success criteria).
- **No sound anywhere; no graphic images; no pop-up alarms; no personal data collected** (spec anti-goals).
- **Colours are colour-blind safe, with shape as a second cue (C8).** Conflict uses dots, outbreaks use a cross
  glyph and displacement uses rings. Food uses the IPC's own phase colours, with the phase number in the legend and
  tooltip.
- **A "crisis" exists only where the source's own threshold says so (C11):**
  - UCDP: a conflict with 25 or more best-estimate deaths in a calendar year;
  - IPC: Phase 3 or higher with people in it;
  - WHO: any Outbreak News report in the last 90 days.

  Displacement numbers are layer values, not crises.
- **Severity bands (C7):** each source's native scale is shown. The 1–5 band only sizes and colours markers, and its
  mapping is written in `docs/severity-bands.md`.
- Non-commercial or share-alike data (WHO) is fetched at run time and cached in the user's data folder, never
  committed (C6).
- **Repos and builds:**
  - Repo `C:\repos\EarthRelief3D`, branch `main`, with submodules `external/AstroCore` (`../AstroCore`) and
    `external/GlobeCore` (`../GlobeCore`). `Directory.Build.props` sets `AstroCoreRoot` and `GlobeCoreRoot` to
    them, plus the long-path block (`EarthRelief3D.build`).
  - Data folder: `AppDataRoot.ForUser("EarthRelief3D", …)`.
  - GlobeCore changes are made on a GlobeCore branch and pinned locally during the plan. They reach GlobeCore
    `main` by pull request at the end, with the owner's OK (Task 2 Step 5).
- **Git:**
  - Commit locally, and never push without the owner's OK.
  - Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Visual checks:**
  - Only through the app's own `--snapshot-scenes` (RenderTargetBitmap). No screen capture, no `Add-Type`.
  - At most 3 app launches in the whole plan.
- **User-Agent:** `EarthRelief3D/<version> (+<project page>)`. It sends no personal data.

## Review Focus

1. **WHO titles name countries in many spellings** (`Democratic Republic of the Congo`, `Côte d'Ivoire`, `Türkiye`,
   `occupied Palestinian territory`, "… and Nigeria"). Each must resolve to one country. A name the index doesn't
   know is logged and shown in the panel's "Not placed on the map" list, never silently dropped. Pinned by
   `WhoOutbreakTests.TitleCountriesResolve` and `.UnknownCountryIsListedNotDropped` (Task 5).
2. **UCDP renames its file every month** (`GEDEvent_v26_0_10.csv` → `_v26_0_11`). The feed finds the files from the
   downloads page. If the page changes shape, the dot turns red with "format changed" and the cache stays. Pinned by
   `UcdpFeedTests.FindsNewestFilesFromPage` and `.PageWithoutLinksIsFormatChanged` (Task 4).
3. **Countries across the date line** (Russia, Fiji, Kiribati) must shade and pick correctly on both sides. Pinned
   by `CountryIndexTests.FindsFijiBothSidesOfTheDateLine` (Task 2) and the `pacific` snapshot scene (Task 7).
4. **Starting with no network:** cached crises show with their as-of dates, and dots turn amber then red. Nothing
   is blank without a reason. Pinned by `CrisisCacheTests.RestartOfflineShowsCachedCrises` (Task 3) and
   `AppStartupTests.OfflineStartShowsCacheAndAmberDots` (Task 6).
5. **A country with several crises** (Sudan: conflict, IPC Phase 5, outbreak, displacement) must show all of them
   in the panel, in a fixed order, each with its own source line. Pinned by
   `CountryPanelViewModelTests.SudanShowsEveryCrisisInOrder` (Task 6).

---

### Task 1: Repo, solution, sources check and samples

**Files:**
- Create: `C:\repos\EarthRelief3D\` git repo (`.gitignore` from EarthWatch3D, `LICENSE` MIT with the same text and
  copyright as EarthWatch3D, `README.md`, `Directory.Build.props` (Version 0.1.0), `EarthRelief3D.sln`), submodules
  at the current GlobeCore and AstroCore `main` commits
- Create: `src/EarthRelief3D.Core/EarthRelief3D.Core.csproj` (net10.0; references GlobeCore and GlobeCore.Earth),
  `src/EarthRelief3D.App/EarthRelief3D.App.csproj` (net10.0-windows WPF, empty window), `tests/EarthRelief3D.Tests`
  (net10.0-windows, UseWPF, xunit; `tests/Samples/**` copied to output)
- Create: `tools/fetch-samples.ps1` and `tests/Samples/` holding:
  - `ucdp_downloads.html` (the downloads page);
  - `ucdp_candidate_monthly.csv` (the monthly file trimmed to Sudan, Ukraine, Myanmar, DR Congo and Haiti rows);
  - `ucdp_candidate_quarterly.csv` (the same trim);
  - `hdx_ipc_package.json` (CKAN `package_show`);
  - `ipc_national_wide_latest.csv`;
  - `unhcr_population_coo.json` (`coo_all=true`, latest year);
  - `who_don_synthetic.json` (hand-made per P2-5, 6 items, including the multi-country and variant-spelling titles
    from Review Focus 1).
- Create: `docs/data-sources.md`. For each source: the exact URL, licence and attribution text, update cadence, the
  field used as "as of", and the date checked.
- Commit the existing `docs/` (design, Plan 1 records, this plan) as the first commit.

- [ ] **Step 1:** Create the repo, submodules, props and projects. `dotnet build` succeeds with an empty
  window app.
- [ ] **Step 2:** Run `tools/fetch-samples.ps1` once (network) and check each file:
  - the UCDP CSV header matches the columns used in Task 4 (`id, year, type_of_violence, conflict_new_id,
    conflict_name, side_a, side_b, country, country_id, latitude, longitude, date_start, date_end, best`);
  - the IPC CSV has the `Phase N number current` columns and `Current from/to`;
  - the UNHCR items have `coo_iso, refugees, asylum_seekers, idps, year`.

  Record anything different in `docs/data-sources.md`, and stop and report if a column the plan relies on is missing.
- [ ] **Step 3:** Write `docs/data-sources.md` and the README (what the app is, how to build, that Plan 2 covers
  crises only).
- [ ] **Step 4:** Commit with the message `EarthRelief3D: repo, solution, data sources checked, samples`.

### Task 2: GlobeCore — CountryIndex and status dots (pull request to GlobeCore)

**Files (GlobeCore, branch `country-index`):**
- Create: `src/GlobeCore.Earth/Assets/ne_50m_admin_0_countries.geojson`. This is Natural Earth 5.1.2 admin-0
  countries, public domain, downloaded from the `nvkelso/natural-earth-vector` repository at tag `v5.1.2`; record the
  source URL and SHA-256 in `Assets/README.md`.
- Create: `src/GlobeCore.Earth/CountryIndex.cs`, `src/GlobeCore.Earth/Country.cs`
- Create: `src/GlobeCore.Wpf/Status/StatusDotsView.xaml(.cs)` and `StatusDotsViewModel.cs`, generic over
  `FeedStatus`: green, amber, red, and blue "Paused", with a tooltip per source. Copied from EarthWatch3D's
  StatusStrip and StatusViewModel without the MTG special case.
- Test: `tests/GlobeCore.Tests/CountryIndexTests.cs`, `StatusDotsViewModelTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record Country(string Iso3, string Name, GeoPosition LabelPoint, IReadOnlyList<GeoPolygon> Polygons)`
  - `public sealed class CountryIndex`:
    - `static CountryIndex LoadNaturalEarth()`, which reads the embedded asset;
    - `CountryIndex WithUnits(IReadOnlyDictionary<string, string> mergeIntoIso3)`, which merges listed units into
      another unit's polygons for C16;
    - `Country? Find(GeoPosition p)`, a point-in-polygon search with a bounding-box prefilter;
    - `Country? ByIso3(string iso3)`;
    - `Country? Resolve(string name)`, which matches names case- and accent-insensitively against `NAME`,
      `NAME_LONG`, `FORMAL_EN`, `ADMIN` and an alias list passed to `WithAliases`;
    - `CountryIndex WithAliases(IReadOnlyDictionary<string, string> aliasToIso3)`;
    - `IReadOnlyList<Country> All`.
  - The ISO3 code comes from `ISO_A3_EH`, falling back to `ADM0_A3` when it is `-99`. The label point comes from
    `LABEL_Y`/`LABEL_X`. Polygons are split at the date line with `DateLine.SplitPolygon`.
  - `public sealed class StatusDotsViewModel(IEnumerable<string> sourceNames)` with `Update(FeedStatus)` and
    `ObservableCollection<StatusDot> Dots`. `StatusDot` has `Name`, `State`, `Tooltip` and `Brush`.

- [ ] **Step 1: Write the failing tests.**
  - `FindsSudanAtKhartoum`: (15.5, 32.53) is SDN.
  - `FindsFijiBothSidesOfTheDateLine`: (−17.0, 179.9) and (−16.9, −179.9) are both FJI.
  - `OceanIsNull`: (0, −140) is null.
  - `ResolvesVariants`: "Democratic Republic of the Congo" is COD, "Côte d'Ivoire" and "Cote d'Ivoire" are CIV,
    "Türkiye" is TUR, and "Republic of Korea" is KOR.
  - `UnitsMergeSomalilandIntoSomalia`: with `{"SOL": "SOM"}`, a point in Hargeisa resolves to SOM.
  - `StatusDotsTurnRedAfterThreeFailures`, matching EarthWatch3D's rule.
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement. Add the Natural Earth credit to GlobeCore's `THIRD-PARTY-NOTICES.txt`.
- [ ] **Step 4:** Run all GlobeCore tests and expect PASS. Commit on GlobeCore branch `country-index`, and pin
  EarthRelief3D's `external/GlobeCore` to that branch head for the rest of the plan. Do not push.
- [ ] **Step 5 (end of plan, with the owner):** push `country-index`, open a pull request to GlobeCore `main` and
  merge it once the owner says so. Squash merges change the hash, so re-pin `external/GlobeCore` to the merged
  `main` commit and rebuild.

### Task 3: Domain model, citations, store and cache (EarthRelief3D.Core)

**Files:**
- Create: `src/EarthRelief3D.Core/Model/SourceCitation.cs`, `Model/Crises.cs`, `Model/DisplacementFigure.cs`,
  `Model/CrisisSnapshot.cs`
- Create: `src/EarthRelief3D.Core/Store/CrisisStore.cs`, `Store/CrisisCache.cs`
- Create: `src/EarthRelief3D.Core/Logic/SeverityBands.cs`, `Logic/Trend.cs`, `docs/severity-bands.md`
- Test: `tests/EarthRelief3D.Tests/ModelTests.cs`, `CrisisCacheTests.cs`, `SeverityBandsTests.cs`, `TrendTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record SourceCitation(string Publisher, string Dataset, string? Version, Uri Url, string Licence, string Attribution, DateTime AsOfUtc, DateTime RetrievedUtc)`
  - `public enum CrisisKind { ArmedConflict, FoodCrisis, Outbreak }`. Atrocity and detention come in Plan 3.
  - `public abstract record Crisis(string Id, CrisisKind Kind, string CountryIso3, string Title, NativeSeverity Severity, int Band, DateTime? Began, Trend? Trend, IReadOnlyList<SourceCitation> Citations)`,
    with `public sealed record NativeSeverity(double Value, string ScaleName, string Text)`.
  - `ConflictCrisis : Crisis`, adding:
    - `int UcdpConflictId`, `string SideA`, `string SideB`;
    - `IReadOnlyList<ConflictEvent> Events`, where
      `ConflictEvent(long Id, DateTime StartUtc, GeoPosition Position, int BestDeaths, int ViolenceType)`;
    - `IReadOnlyDictionary<DateOnly, int> DeathsByMonth`.
  - `FoodCrisis : Crisis`, adding:
    - `int HighestPhase`, `long Phase3PlusPeople`, `double Phase3PlusShare`;
    - `DateOnly PeriodFrom`, `DateOnly PeriodTo`, `bool IsStale`.
  - `OutbreakCrisis : Crisis`, adding `string Disease`, `string DonId`, `DateTime PublishedUtc` and
    `Uri ReportUrl`.
  - `public sealed record DisplacementFigure(string CountryIso3, int Year, long Refugees, long AsylumSeekers, long Idps, SourceCitation Citation)`
  - `public sealed record UnplacedReport(string Source, string Title, string Reason, SourceCitation Citation)`
  - `public sealed record CrisisSnapshot(IReadOnlyList<Crisis> Crises, IReadOnlyList<DisplacementFigure> Displacement, IReadOnlyList<UnplacedReport> Unplaced)`,
    with `Empty` and `ByCountry(string iso3)`.
  - `public sealed class CrisisStore`, which wraps GlobeCore `SnapshotStore<CrisisSnapshot>` with
    `ReplaceKind(CrisisKind, IReadOnlyList<Crisis>)`, `ReplaceDisplacement(...)`, `ReplaceUnplaced(string source, ...)`
    and `Load(CrisisSnapshot)`.
  - `CrisisCache`: JSON with `SchemaVersion = 1` and polymorphic `Crisis` (System.Text.Json `[JsonDerivedType]`),
    written with GlobeCore `AtomicFile` to `<data>/cache/crises.json`.
  - `SeverityBands`:
    - `int ForConflict(int deathsLast12Months)` uses bands of 25, 1,000, 10,000 and 50,000;
    - `int ForFood(int highestPhase)` returns the phase;
    - `int ForOutbreak()` returns 3.

    `docs/severity-bands.md` says exactly this and why.
  - `public sealed record Trend(long Recent, long Previous, string Window)` with
    `static Trend? FromMonths(IReadOnlyDictionary<DateOnly, int> byMonth, DateOnly latestCompleteMonth)`, comparing
    the last 3 months with the 3 before. It is null if fewer than 6 months are covered.

- [ ] **Step 1: Write the failing tests.**
  - `CacheRoundTripsEveryCrisisKind` and `RestartOfflineShowsCachedCrises`: a store loaded from the cache equals
    the one saved.
  - `WrongSchemaIsIgnored`.
  - `BandsMatchDoc`: covers 24 (no crisis), 25, 999, 1,000 and 50,000.
  - `TrendNeedsSixMonths`.
  - `TrendComparesLastThreeWithThreeBefore`.
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS.
- [ ] **Step 5:** Commit with the message `EarthRelief3D.Core: crises, citations, store, cache, bands, trend`.

### Task 4: Conflict (UCDP) and food (IPC) feeds

**Files:**
- Create: `src/EarthRelief3D.Core/Feeds/UcdpCandidateFeed.cs`, `Feeds/UcdpReader.cs`,
  `Feeds/IpcNationalFeed.cs`, `Feeds/IpcNationalReader.cs`
- Test: `tests/EarthRelief3D.Tests/UcdpFeedTests.cs`, `IpcFeedTests.cs`

**Interfaces:**
- Consumes: Task 3 types; GlobeCore `IFeed`, `FeedHttp`, `FeedRunResult`, `SampleHttpHandler`; `CountryIndex`
  (for ISO3 checks only).
- Produces:
  - `UcdpCandidateFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log)`: `Name = "UCDP"`, `Interval = 6 h`.
  - `IpcNationalFeed(FeedHttp http, CrisisStore store, IClock clock, IAppLog log)`: `Name = "IPC"`, `Interval = 12 h`.

Rules:
- **UCDP:**
  - Read `https://ucdp.uu.se/downloads/`. Find `candidateged/GEDEvent_v<yy>_0_<m>.csv` (monthly) and
    `GEDEvent_v<yy>_01_<yy>_<mm>.csv` (quarterly cumulative) links. Download the newest quarterly file plus every
    monthly file newer than its end (P2-8), and only files not already cached (by name).
  - Parse with a CSV reader that handles quoted fields with embedded commas, quotes and newlines (`source_*`
    columns). Do not keep or show the `source_*` text.
  - Group by `conflict_new_id`. It is a crisis when the best-estimate deaths in any calendar year in the window reach
    25 or more.
  - The country comes from the UCDP `country` name through `CountryIndex.Resolve`. A conflict spanning several
    countries gets one crisis per country it has events in.
  - Citation: "UCDP Candidate Events Dataset", the version from the file name, CC BY 4.0. Attribution: "Uppsala
    Conflict Data Program (UCDP) Candidate Events Dataset, Hegre et al. 2020". `AsOf` = the newest `date_end` in the
    files.
- **IPC:**
  - Read the HDX `package_show?id=global-acute-food-insecurity-country-data`, find
    `ipc_global_national_wide_latest.csv`, then download and parse it.
  - It is a crisis when `Phase 3+ number current` > 0. `HighestPhase` = the highest N with
    `Phase N number current` > 0. `IsStale` = `Current to` is more than 12 months before now (P2-4); stale rows are
    kept but are not markers.
  - Citation: IPC via HDX, CC0. `AsOf` = `Current to` (the analysis period end); `Version` = `Date of analysis`.
- **Format changes:** a page or file that no longer matches (missing links, a missing column) returns a failure
  whose message starts "format changed:". The store keeps the previous data.

- [ ] **Step 1: Write the failing tests** (offline with `SampleHttpHandler` and the Task 1 samples).
  - `FindsNewestFilesFromPage` and `PageWithoutLinksIsFormatChanged`.
  - `ParsesQuotedSourceColumns`.
  - `SudanConflictIsACrisisWithEventsAndTrend`.
  - `ConflictBelow25DeathsIsNotACrisis`, using a hand-made row in a test string.
  - `DoesNotRedownloadCachedFiles`.
  - `IpcHighestPhaseAndShare`: use the values from the sample row for SDN, read in the test from the CSV, never
    hard-coded from memory.
  - `StaleIpcIsKeptButMarkedStale`.
  - `IpcCitationAsOfIsPeriodEnd`.
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS.
- [ ] **Step 5:** Commit with the message `Feeds: UCDP Candidate conflicts and IPC national food crises`.

### Task 5: Outbreak (WHO) and displacement (UNHCR) feeds

**Files:**
- Create: `src/EarthRelief3D.Core/Feeds/WhoOutbreakFeed.cs`, `Feeds/WhoTitleParser.cs`,
  `Feeds/UnhcrDisplacementFeed.cs`, `data/country-aliases.json`, `data/country-units.json` (C16; starts with
  `{"SOL": "SOM"}` and a reason per entry)
- Test: `tests/EarthRelief3D.Tests/WhoOutbreakTests.cs`, `UnhcrFeedTests.cs`, `CountryDataTests.cs`

**Interfaces:**
- Produces:
  - `WhoOutbreakFeed(...)`: `Name = "WHO"`, `Interval = 3 h`. It reads
    `https://www.who.int/api/news/diseaseoutbreaknews?$top=50&$orderby=PublicationDate desc` and keeps reports from
    the last 90 days.
  - `static (string Disease, IReadOnlyList<string> Places) WhoTitleParser.Parse(string title)` splits at the last
    " - " or " – ". The left side is the disease; the right side is split on ",", " and " and "; ".
  - `UnhcrDisplacementFeed(...)`: `Name = "UNHCR"`, `Interval = 24 h`. It reads
    `https://api.unhcr.org/population/v1/population/?year=<y>&coo_all=true&limit=500`. It uses the newest year with
    data (try this year, then go back one year at a time, at most 3), summing refugees, asylum-seekers and internally
    displaced people per `coo_iso`. Values `"-"` and `"0"` mean 0. Citation attribution: "UNHCR Refugee Population
    Statistics Database", CC BY 4.0, with a link to the terms.

Rules:
- **WHO:** one `OutbreakCrisis` per place that resolves. A place that doesn't resolve becomes an `UnplacedReport`
  with the reason "country name not recognised: <name>". `ReportUrl` =
  `https://www.who.int/emergencies/disease-outbreak-news/item/<UrlName>`. Citation: WHO, "Disease Outbreak News",
  CC BY-NC-SA 3.0 IGO, `AsOf` = `PublicationDate`.
- **Data files:** `data/*.json` are copied to the app output. `CountryDataTests` checks that every alias and unit
  maps to an ISO3 the index knows.

- [ ] **Step 1: Write the failing tests.**
  - `TitleCountriesResolve`: the synthetic multi-country title gives COD, KEN and NGA.
  - `UnknownCountryIsListedNotDropped`.
  - `OlderThan90DaysIsIgnored`.
  - `ParsesDiseaseAndPlaces`, with the six synthetic titles as a theory.
  - `UnhcrSumsByOrigin`: Afghanistan's refugees, asylum-seekers and internally displaced people from the sample,
    read from the sample.
  - `UnhcrFallsBackToEarlierYear`: the current year answers with no items.
  - `EveryAliasAndUnitIsKnown`.
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS.
- [ ] **Step 5:** Commit with the message `Feeds: WHO outbreaks and UNHCR displacement; country aliases and units`.

### Task 6: App shell, layers, legend and country panel

**Files:**
- Modify: `src/EarthRelief3D.App` with `App.xaml(.cs)` (composition root, as in EarthWatch3D's `App.xaml.cs`:
  data paths, log, settings, cache load, feeds, scheduler), `MainWindow.xaml(.cs)`, and `Controllers/SceneController.cs`
  (camera input, hover and click; builds the `LayerHost` on `EarthBody.Instance` with Blue Marble, borders and
  `Body.IlluminationAt` at night strength 0.6)
- Create: `src/EarthRelief3D.App/Layers/BordersImageLayer.cs`, `FoodImageLayer.cs` (country shading through
  `AreaRasterizer`, IPC colours), `ConflictSceneLayer.cs` (dots sized by band, age colour), `OutbreakSceneLayer.cs`
  (cross glyph at the country label point), `DisplacementSceneLayer.cs` (ring with area ∝ the displaced total, at
  the label point), `LabelsOverlayLayer.cs`
- Create: `src/EarthRelief3D.App/ViewModels/MainViewModel.cs`, `LayerStripViewModel.cs` (the Crises group with 4
  toggles, plus a "Needs", "Resources" and "Getting help in" group header marked "coming"),
  `CountryPanelViewModel.cs`, `LegendViewModel.cs`
- Create: `src/EarthRelief3D.App/Views/LayerStrip.xaml`, `CountryPanel.xaml`, `Legend.xaml`, plus the status dots
  from GlobeCore
- Test: `tests/EarthRelief3D.Tests/CountryPanelViewModelTests.cs`, `LayerStyleTests.cs`, `AppStartupTests.cs`,
  `LegendViewModelTests.cs`

**Interfaces:**
- Consumes: Tasks 2–5, GlobeCore `LayerHost` and the layer classes, `EarthAssets`, `NaturalEarthLines`,
  `ImageConversions`.
- Produces:
  - `CountryPanelViewModel.Show(string iso3)`, with `Sections` (`IReadOnlyList<PanelSection>`) in this order:
    armed conflict, food crisis, disease outbreaks, people forced from home.
  - `PanelSection(string Heading, IReadOnlyList<(string Label, string Value)> Rows, string SourceLine, Uri? Link)`.
    `SourceLine` = "Source: <publisher>, <dataset> · as of <date>".
  - A country with no crisis shows "No crisis recorded by these sources" and the list of sources checked, with
    their dates.

Rules:
- The legend shows one line per visible layer: the symbol, what it measures, the source and the as-of date. The as-of
  date is the newest `AsOfUtc` among that layer's citations.
- The top of the panel shows the country name. Unplaced WHO reports appear in a "Not placed on the map" section in
  the legend flyout.
- **Hover** shows a one-line tooltip with the source, for example "Sudan · armed conflict · 1,234 deaths in the last
  12 months · UCDP, as of 31 Oct 2026".
- **Click** on a country (`CountryIndex.Find`) or a marker opens the panel. Esc or the × button closes it.
- **Start-up sequence:** the LayerHost start-up sequence from GlobeCore's docs; layers invalidate from feed threads
  and `LayerHost` marshals.

- [ ] **Step 1: Write the failing tests.**
  - `SudanShowsEveryCrisisInOrder`, built from the samples.
  - `NoCrisisCountryListsSourcesChecked`.
  - `EverySectionHasASourceLineWithDate`.
  - `LegendShowsNewestAsOfPerLayer`.
  - `LayerStyleTests`: IPC phase colours are the IPC's published hex values, recorded with their source URL in a
    code comment; every glyph is distinct by shape.
  - `OfflineStartShowsCacheAndAmberDots`: the view models are fed a cached snapshot and failing feeds.
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS.
- [ ] **Step 5:** Commit with the message `App: globe, crisis layers, legend, country panel, status dots`.

### Task 7: Diagnostics, snapshots, docs and final check

**Files:**
- Create: `src/EarthRelief3D.App/Diagnostics/LaunchOptions.cs` and `OfflineSamples.cs` (map each live URL to a
  sample), plus `SnapshotScenes.cs`. These follow EarthWatch3D's `--offline-samples`, `--display-time` and
  `--snapshot-scenes/--snapshot-out` design, with frozen clocks per scene, `SnapshotRenderer.SavePng` and exit
  code 1 on failure.
- Create: `tests/SnapshotScenes/scenes.json` with these scenes:
  - `africa` (look at 10, 25; all layers);
  - `pacific` (look at −15, 180; borders, outbreaks);
  - `sudan-panel` (look at 15, 30; the panel open for SDN);
  - `no-crisis-panel` (the panel open for ISL).
- Modify: `README.md` (features, data sources, build, diagnostics) and `docs/data-sources.md`.
- Copy `tools/PngDiff` from EarthWatch3D, or reference it by path. The task decides; record the choice.

- [ ] **Step 1:** Implement the options and add a test, `LaunchOptionsTests.ParsesSceneOptions`.
- [ ] **Step 2: Snapshots** (two launches).
  1. Render the scenes into `artifacts\snapshot-baseline` and `artifacts\snapshot-baseline-2`.
  2. PngDiff them: they must be identical. If they aren't, freeze whatever varies, as EarthWatch3D's Plan 1 Task 1
     did.
  3. Open each PNG with the file-reading tool and check:
     - the globe and borders;
     - IPC shading over Sudan, Somalia, Haiti and Afghanistan in phase colours;
     - conflict dots in Sudan, Ukraine and Myanmar;
     - outbreak crosses for the synthetic reports;
     - displacement rings;
     - legend lines with sources and dates;
     - the Sudan panel's four sections;
     - Iceland's "No crisis recorded…" panel;
     - Fiji shaded or picked on both sides of 180°.
- [ ] **Step 3:** Full suite in Release, with 0 warnings. Write the counts and the snapshot notes into
  `docs/plan-2-results.md`.
- [ ] **Step 4:** Commit with the message `Diagnostics: offline samples, snapshot scenes; README and data-source docs`.
- [ ] **Step 5:** Run the whole-branch review (superpowers:requesting-code-review).
