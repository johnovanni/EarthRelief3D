# Plan 3: Atrocities, Detention, Food Areas, Basic Needs and Resources

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** EarthRelief3D gains:
- the remaining crisis kinds: genocides and mass atrocities, and prison camps;
- IPC food-crisis **areas** (replacing country shading where IPC publishes areas);
- the **Needs** and **Resources** layer groups: water, sanitation, health care, hunger and shelter needs; oil, water stress, power plants, oil and gas fields, and steel plants.

Every value carries its source and as-of date.

**Architecture:**
- **Curated tables** (`data/curated/`, JSON) are checked by tests: every row has a citation and a date.
- **Live feeds** (GlobeCore `IFeed`) cover WHO GHO, HDX HNO and the HDX IPC area files.
- **Bundled datasets** (`data/datasets/<name>/`, a cleaned CSV plus `manifest.json`) are made by a new console tool, `tools/EarthRelief3D.DataTool`, from files the maintainer downloads.
- A generic **country-value shading layer** and a generic **point-marker layer** serve every needs and resources layer.
- One shading layer shows at a time; marker layers stack (spec §6).

**Tech Stack:** C# / .NET 10, WPF, HelixToolkit.Wpf 3.1.2 (through GlobeCore.Wpf), CommunityToolkit.Mvvm 8.4.0, xunit 2.9.2. The DataTool only adds ExcelDataReader 3.7.0 and ExcelDataReader.DataSet 3.7.0 (MIT) for the XLSX inputs; the app does not reference them.

**Spec:** `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md`. Relevant parts: §1, §4, §5.1–5.3, §6, §7, §10, §11, §12, §13, §14; calls C6–C8, C10–C16; rulings P2-3, R16 and R17.

## Sources checked while writing this plan (2026-11-23)

Each source was checked live on its own site on 2026-11-23. Task 1 re-checks each one and records it in `docs/data-sources.md`.

| Source | What it gives | Access | Licence | Use |
|---|---|---|---|---|
| WHO GHO OData `https://ghoapi.azureedge.net/api/` | `WSH_WATER_BASIC`, `WSH_SANITATION_BASIC` (%; filter `Dim1 = RESIDENCEAREATYPE_TOTL`), `UHC_INDEX_REPORTED`; latest year 2024 | Live, no key. WHO says this API is deprecated "near the end of 2025", successor URL not yet published | WHO data terms: CC BY 4.0 with WHO's additional terms (public-health purposes, no endorsement) | Live feed, base URL in one constant |
| HDX `global-hpc-hno` | `hpc_hno_2026.csv` (national, 20 countries): `Country ISO3, Description, Cluster, Category, Population, In Need, Targeted, Affected, Reached, Info`; cluster codes FSC, WSH, SHL, HEA, NUT, ALL… | Live CKAN, no key (browser-like User-Agent) | CC BY-IGO | Live feed |
| HDX IPC (`global-acute-food-insecurity-country-data` and the per-country `<country>-acute-food-insecurity-country-data`) | Per-country `ipc_<iso3>.geojson` area polygons with `overall_phase` and `phaseN_population/percent` (Nigeria: 795 areas, 1.9 MB); no p-codes | Live CKAN, no key | CC0 (check each dataset's `license_id`) | Live feed per country |
| FAOSTAT `Food_Security_Data_E_All_Data_(Normalized).zip` | Prevalence of undernourishment (%) and dietary energy supply adequacy (%), M49 area codes, Latin-1, latest year 2025 | No sign-in, 2.1 MB | CC BY 4.0 | Bundled extract |
| US EIA `https://www.eia.gov/opendata/bulk/INTL.zip` | International energy series including crude oil production (thousand barrels/day) | No key, 24.7 MB (the API needs a personal key, so not used) | Public domain, credit EIA | Bundled extract |
| WRI Aqueduct 4.0 country rankings `aqueduct-4-0-country-rankings.zip` | Sheet `country_baseline`, indicator `bws`, `weight = Tot`: score 0–5, `cat`, `label` | No key, 12.8 MB | "Creative Commons" (version to confirm from the file's Read Me) | Bundled extract |
| GEM Integrated Power (Sept 2026), Oil and Gas Extraction (Mar 2026), Iron and Steel (Sept 2026 V1) | Plant or field locations, capacity, status | Download form (fields not visible to scripts) | Reported CC BY 4.0, to be confirmed in the download dialog | Owner downloads, then bundled extract |
| UNODC prisons (`data.unodc.org`) | Persons held, rate per 100,000, 2003–2024 | XLSX, no login | UN terms: personal, non-commercial, **no redistribution or derivative works** | **Not used** until written permission (C13) |
| UN COI DPRK (A/HRC/25/63 and A/HRC/25/CRP.1, 2014); HRNK imagery reports (2014–2024) | Kwan-li-so Nos. 14, 15, 16 and 25 (and No. 18) named and documented | Public reports | Facts with citations | Curated camp sites |
| HDX HAPI; IPC API; healthsites.io API | — | Need an email-derived identifier or a personal key | — | **Not used** (C12) |

## Rulings made while writing this plan

| # | Ruling | Reason | Cost if wrong |
|---|---|---|---|
| P3-1 | **No source needs a key, an account or the owner's email.** HAPI, the IPC API, the EIA API and the healthsites.io API are not used. HNO and IPC data come from HDX CKAN files, oil from EIA's bulk file | C12; the plan doesn't wait on the owner | Slightly older or coarser data than the keyed APIs |
| P3-2 | **IPC areas come from HDX's per-country `ipc_<iso3>.geojson`.** Each area is shaded in IPC's own area classification (`overall_phase` 3–5). For a country with current area data, the areas replace the country-wide R17 shading. Elsewhere R17 stays. Areas are used only when they belong to the same analysis as the country's national record (Task 1 confirms which property identifies the analysis and its period). If they don't, the country keeps R17 and the log says why | P2-3 moved areas here. IPC classifies areas itself, so no rule of ours is needed for them | If the GeoJSON lacks the period, Task 4 stops at "areas deferred" and R17 stays everywhere |
| P3-3 | **Detention in Plan 3 is curated camp sites only.** UNODC's terms forbid redistribution and derivative works. Task 1 drafts permission requests (UNODC, ICPR World Prison Brief, ASPI, USHMM) for the owner to send. Each data source arrives in a later plan only after permission | C13 | No incarceration-rate layer until a reply comes |
| P3-4 | **Curated rows are drafted by Claude and shown only after the owner approves them** (C10). Each row has `review: { status: "draft" or "approved", approvedOn }`. Drafts appear only with the diagnostic `--show-drafts`, labelled "Draft: not yet reviewed". `docs/curated-review.md` lists every draft with its citations for the owner | C10: the owner is the final check on designations | Until the owner reviews, the atrocity and camp layers show nothing in normal runs |
| P3-5 | **Government determinations: the US is curated completely; no other government in Plan 3** (C15). Settings offers "Formal determinations by: United States government" (on by default). The list is driven by `data/curated/governments.json`, so adding a government needs no code | C15 | — |
| P3-6 | **One shading layer at a time across all groups:** Food crisis, Water, Sanitation, Health care, Hunger, Shelter need, Oil production and Water stress. Turning one on turns the shown one off, and the strip shows the change. Marker layers (conflict, outbreaks, displacement, atrocities, camps, power plants, oil and gas, steel) stack | Spec §6 | A user may miss that food shading went off; the legend says which shading is shown |
| P3-7 | **Shading scales:** a colour-blind-safe sequential scale per group, different from the IPC colours.<br>• Needs: ColorBrewer BuPu, 5 classes `#EDF8FB #B3CDE3 #8C96C6 #8856A7 #810F7C`.<br>• Resources: YlGnBu, 5 classes `#FFFFCC #A1DAB4 #41B6C4 #2C7FB8 #253494`.<br>A country with no value is not filled, and the legend says "no figure: not shaded". Breaks: a source's own classes where it has them (FAO hunger-map classes for undernourishment; WRI's 0–5 categories for water stress), otherwise fixed breaks written in the legend (table in Task 2) | C7, C8; no invented severity | The breaks shape the impression; they are shown, never hidden |
| P3-8 | **"Where supply sits" in Plan 3:**<br>• Food: dietary energy supply adequacy (FAOSTAT).<br>• Water: water stress (Aqueduct).<br>• Energy and industry: GEM markers.<br>• Health facilities (healthsites.io) are **not** in Plan 3: the API needs a key, and the per-country HDX files are ODbL (live only, about 200 downloads).<br>• Shelter supply (UNHRD depots) is Plan 4 | Spec §5.2 left healthsites to Plan 3 | The health-care "supply" view is missing in v1 |
| P3-9 | **Marker layers are capped at 5,000 markers each,** filtered at extract time. The filter is written in the dataset manifest and the legend:<br>• power: operating plants of 500 MW or more (units summed per plant);<br>• steel: operating plants of 1 Mt/yr or more;<br>• oil and gas: operating fields with coordinates.<br>If a filter still leaves more than 5,000, raise its threshold and record it | Rendering budget; the filter is shown, not hidden | Small plants are not shown |
| P3-10 | **A new `tools/EarthRelief3D.DataTool`** (net10.0 console) makes every bundled extract from the maintainer's downloaded files: it trims to the columns used, normalises country codes to ISO3, and writes `data.csv` plus `manifest.json`. The manifest holds the source, version, as-of, licence, attribution, the original file's SHA-256 and the command used. Original downloads are never committed | Spec §10 | — |
| P3-11 | **FAOSTAT area codes:** M49 maps to ISO3 through Natural Earth's `UN_A3` field. FAO's "China, mainland" (FAO area code 41) maps to CHN. Hong Kong, Macao and Taiwan keep their own codes, and FAO's "China" aggregate (FAO code 351) is dropped. Aggregates (regions, income groups) are dropped. Any M49 code that doesn't map is listed in the manifest | Avoids double counting China | — |

## Owner items

- **Before Task 7:**
  - **GEM files.** Download the three GEM trackers through GEM's form: Integrated Power (September 2026 release), Oil and Gas Extraction (March 2026) and Iron and Steel (September 2026 V1). Put them, unopened and unrenamed, in `C:\repos\EarthRelief3D\.downloads\gem\`, a git-ignored folder made in Task 1.
  - **GEM licence.** Note the licence shown in the download dialog. If it is not CC BY 4.0, or forbids redistribution, say so: Task 7 then becomes "links only".
- **After the plan:**
  - Review `docs/curated-review.md` and approve or correct each designation and camp entry (P3-4).
  - Send the permission requests in `docs/permission-requests.md` (P3-3), if you wish.

## Global Constraints

- Everything in Plan 2's Global Constraints still holds. In particular:
  - every fact on screen shows its source and an "as of" date;
  - no sound, no graphic images, no pop-up alarms, and no personal data;
  - C8 colour plus shape;
  - C11 crisis thresholds;
  - C6: non-commercial and share-alike data is fetched live only;
  - local commits only, and no push without the owner's OK;
  - the commit attribution line;
  - User-Agent `EarthRelief3D/<version> (+<project page>)`;
  - visual checks only through `--snapshot-scenes` (RenderTargetBitmap).
- **At most 4 app launches in this plan:** Task 8's snapshot run, one re-run after tuning, and the repeat check. Task 4 may use one more if its area picture needs it.
- **Every curated row has a citation URL, a document title and a date. A test fails the build if one is missing** (spec §10.2).
  - A row's facts are checked on the authority's own site before entry: icj-cij.org, the court's or the UN body's site, state.gov or the archived state.gov.
  - A claim that cannot be confirmed there is not entered.
- **Labels follow C14 exactly:**
  - court judgment, UN body finding and government determination are named as such;
  - provisional measures and arrest warrants are labelled as such, never as verdicts;
  - pending cases say "pending: no finding yet";
  - NGO alerts and risk lists are links labelled "alert" or "risk", never map designations;
  - a past genocide is on the map only when the designating body, or a UN report in the last 12 months, says the situation continues.
- **Legends say exactly what is measured, with the year:** "Share of people without at least basic sanitation, 2024", never "sanitation" (§7).
- **Country codes:** every source joins on ISO3 through the existing `CountryResolver` and `data/country-aliases.json`, and is resolved against the boundary set's units (C16: Somaliland in Somalia, Crimea in Ukraine). An unresolved code is logged and listed under "Not placed on the map", never dropped silently.
- **GlobeCore is not changed in this plan.** If a task finds it must change GlobeCore, it stops and reports.

## Review Focus

1. **"No figure" must never look like "low need".**
   - A country a source doesn't cover (a non-HNO country, a GHO gap, or a small territory) is not filled.
   - Its tooltip and panel say "No figure from <source>".
   - Pinned by `CountryValueLayerTests.MissingValueIsNotShadedAndSaysSo` (Task 2) and `NeedsPanelTests.NonHnoCountrySaysNoPlan` (Task 3).
2. **Turning on a shading layer turns another off.**
   - The other toggle visibly unchecks, the legend names the one shown, and the choice survives a restart.
   - Pinned by `LayerStripTests.OneShadingAtATime` and `.ShadingChoiceIsSaved` (Task 2).
3. **Country codes that differ between sources:**
   - FAOSTAT's China entries;
   - Kosovo (XKX/KOS);
   - Palestine (PSE);
   - Somaliland (SOL counted under SOM);
   - Western Sahara.

   Each resolves to one boundary unit or is listed as not placed. Pinned by `DataToolFaoTests.ChinaIsCountedOnce`, `.AggregatesAreDropped` (Task 6) and `NeedsFeedTests.CodesResolveToBoundaryUnits` (Task 3).
4. **Curated rows go stale or are mislabelled.**
   - A row without a citation, a document title or a date fails the build.
   - A provisional measure or an arrest warrant never reads as a verdict.
   - A designation with no "continuing" evidence in the last 12 months is shown in the panel history, not on the map.
   - Pinned by `CuratedTablesTests.EveryRowIsCited`, `.InterimStepsAreNeverVerdicts` and `AtrocityLayerTests.PastDesignationIsHistoryNotMarker` (Task 5).
5. **WHO moves the GHO API** (announced for the end of 2025).
   - A failed or reshaped answer turns the dot red with "format changed" or "unavailable", and the cached values stay with their dates.
   - The base URL is one constant.
   - Pinned by `GhoFeedTests.ReshapedAnswerIsFormatChanged` and `.FailureKeepsCache` (Task 3).

---

### Task 1: Sources re-check, samples, permission drafts

**Files:**
- Modify: `tools/fetch-samples.ps1` (add the new sources), `.gitignore` (add `.downloads/`)
- Create in `tests/Samples/`:
  - `gho_wsh_water_basic.json`, trimmed to 15 countries including SDN, YEM, AFG, NER, TCD, ISL, USA, CHN, PSE;
  - `gho_wsh_sanitation_basic.json` (the same trim);
  - `gho_uhc_index.json` (the same trim);
  - `hdx_hno_package.json`;
  - `hpc_hno_2026.csv` (the whole file; 6.7 KB);
  - `hdx_ipc_nga_package.json`;
  - `ipc_nga.geojson`, trimmed to 40 areas in at least 3 phases, with geometry simplified by the DataTool's `simplify` (Task 6) or by hand to under 300 KB;
  - `hdx_ipc_nogeo_package.json`: the CKAN package of one country whose IPC dataset has no GeoJSON (Task 1 picks it and names it in the samples README), for the no-areas test.
- Create: `docs/permission-requests.md`, with four short draft emails for the owner to send:
  - UNODC (unodc-stats@un.org), for prison rates;
  - ICPR (World Prison Brief);
  - ASPI (the Xinjiang dataset);
  - USHMM (the Early Warning Project risk list).

  Each draft says what the app is (public, free, educational, MIT, GitHub link) and exactly what would be shown, with attribution. It must not include the owner's personal email address; the owner adds the sender.
- Modify: `docs/data-sources.md` (a section per new source, as in Plan 2: URL, licence and attribution text, cadence, as-of field, checked columns, gotchas, date checked)

- [ ] **Step 1:** Run the fetch script once (network). For each sample, confirm the fields this plan relies on:
  - GHO rows have `SpatialDim, TimeDim, NumericValue, Dim1`;
  - the HNO CSV has the 10 columns in the sources table above, with cluster codes `FSC, WSH, SHL, HEA, ALL`;
  - the IPC GeoJSON has `overall_phase`, `phaseN_population`, `estimated_population` and a property naming the analysis and its period.

  Record that property's exact name, or its absence, in `docs/data-sources.md`, together with the CKAN query that finds a country's `ipc_<iso3>.geojson`. Both decide P3-2 and Task 4. If a field the plan relies on is missing, stop and report.
- [ ] **Step 2:** For each source, confirm its licence on its own page and record the exact attribution text:
  - each IPC country dataset's `license_id`;
  - the HNO dataset's `license_id`;
  - WHO's data-terms sentence;
  - FAO's citation format;
  - EIA's credit line;
  - the Read Me sheet of the Aqueduct workbook.
- [ ] **Step 3:** Write `docs/permission-requests.md` and the data-sources sections. Commit with the message `Plan 3: sources re-checked, samples, permission drafts`.

### Task 2: Country-value shading, marker layers, and the Needs and Resources groups

**Files:**
- Create: `src/EarthRelief3D.Core/Model/CountryValue.cs`:
  - `record CountryValue(string Iso3, double Value, int Year, SourceCitation Citation)`;
  - `record IndicatorSeries(string Id, string Measures, string Unit, IReadOnlyList<CountryValue> Values)`, where `Measures` is the exact legend wording (§7).
- Create: `src/EarthRelief3D.Core/Store/IndicatorStore.cs`, a `SnapshotStore` of series by id, cached on disk like `CrisisCache` with its own schema version 1.
- Create: `src/EarthRelief3D.App/Layers/CountryValueLayer.cs`:
  - an `ImageLayer` and `IPickable`;
  - it fills each country with a value in its class colour, using the existing `AreaRasterizer` path and `FillBoost` as the food layer does.
- Create: `src/EarthRelief3D.App/Layers/PointMarkerLayer.cs`, a scene layer drawing up to 5,000 points as one batched mesh. It reuses `MarkerMeshes`, and the shape is given per layer (C8).
- Create: `src/EarthRelief3D.App/Presentation/ShadingScale.cs`:
  - `record ShadingScale(IReadOnlyList<double> Breaks, IReadOnlyList<Color> Colors, IReadOnlyList<string> ClassLabels)`;
  - `int? ClassOf(double value)`.
- Modify: `LayerStyle.cs`. Add `enum ShadingLayer { None, Food, Water, Sanitation, Health, Hunger, ShelterNeed, OilProduction, WaterStress }` and `enum MarkerLayer { Conflict, Outbreaks, Displacement, Atrocities, Camps, PowerPlants, OilGasFields, SteelPlants }`, plus a symbol and colour for each.
- Modify: `LayerStripViewModel.cs`, `LayerStrip.xaml`, `AppSettings.cs` (`Shading` and a set of shown marker layers, defaulting to today's: Food plus the three crisis markers), `LegendViewModel.cs`.
- Test: `CountryValueLayerTests`, `ShadingScaleTests`, `LayerStripTests`, `PointMarkerLayerTests`.

**Interfaces:**
- Produces:
  - `IndicatorStore.Replace(string seriesId, IndicatorSeries series)`;
  - `IndicatorStore.Snapshot.Get(string seriesId)`;
  - `CountryValueLayer(ShadingLayer which, Func<IndicatorSeries?> series, ShadingScale scale, …)`;
  - `PointMarkerLayer(MarkerLayer which, Func<IReadOnlyList<PointItem>> items, …)` with `record PointItem(GeoPosition Position, double Size, string Tooltip, string? CountryIso3)`;
  - `ShadingScales.For(ShadingLayer)`.

The scales follow P3-7. Breaks are lower bounds, and each class runs from its break to the next:

| Layer | Value shown | Breaks |
|---|---|---|
| Water | % **without** at least basic drinking water (100 − GHO) | 0, 5, 15, 30, 50 |
| Sanitation | % without at least basic sanitation | 0, 5, 15, 30, 50 |
| Health | 100 − UHC index | 0, 20, 40, 60, 80 |
| Hunger | prevalence of undernourishment % (FAO hunger-map classes) | 2.5, 5, 15, 25, 35 (values under 2.5 are shown as "under 2.5%: not shaded") |
| ShelterNeed | SHL people in need ÷ HNO population, % | 0, 5, 15, 30, 50 |
| OilProduction | thousand barrels per day | 0, 10, 100, 1,000, 5,000 |
| WaterStress | Aqueduct `bws` category | 0–4 as WRI labels them. "Arid and low water use" is grey `#BDBDBD`, labelled so |

- [ ] **Step 1: Write the failing tests:**
  - `ShadingScaleTests.ClassOfUsesLowerBounds`: Water 4.9 is class 0, 5 is class 1, 49.9 is class 3, 50 is class 4.
  - `CountryValueLayerTests.MissingValueIsNotShadedAndSaysSo`: a country absent from the series gets no pixels, and its tooltip contains "No figure from".
  - `.TooltipNamesMeasureYearAndSource`: e.g. "Iceland · Share of people without at least basic drinking water, 2024: 0.0% · WHO GHO".
  - `LayerStripTests.OneShadingAtATime`: turning on Water sets `ShowFood` false and `Shading = Water`.
  - `.ShadingChoiceIsSaved`: the choice survives reloading settings.
  - `.MarkersStack`: power plants and conflict are both shown.
  - `PointMarkerLayerTests.CapsAt5000AndSaysSo`: 5,001 items draw 5,000, and an error is logged.
  - `.OnePickPerMarker`.
- [ ] **Step 2:** Run the tests and see them fail.
- [ ] **Step 3:** Implement. The strip shows four groups. Needs and Resources list their layers, and each shading toggle is a radio-style toggle within the shared "one shading" rule (P3-6). The legend shows only the shading layer that is on: its `Measures` line, class swatches with labels, "no figure: not shaded", and the source and as-of. Settings keep `ShowFood` for old files, mapped to `Shading = Food`.
- [ ] **Step 4:** Run all tests; they pass. Commit with the message `Country-value shading, marker layers, Needs and Resources groups`.

### Task 3: Needs feeds: WHO GHO and HDX HNO, with a panel section

**Files:**
- Create: `src/EarthRelief3D.Core/Feeds/GhoFeed.cs`. Base URL `https://ghoapi.azureedge.net/api/` is one constant. It fetches `WSH_WATER_BASIC`, `WSH_SANITATION_BASIC` and `UHC_INDEX_REPORTED`, every 7 days, and keeps each country's latest `TimeDim` with `Dim1 = RESIDENCEAREATYPE_TOTL` where that dimension exists.
- Create: `src/EarthRelief3D.Core/Feeds/HnoFeed.cs`. It reads `hpc_hno_2026.csv` from the `global-hpc-hno` CKAN package, choosing the resource by its name `hpc_hno_<current year>.csv` and falling back to the previous year if missing. Every 7 days.
- Create: `src/EarthRelief3D.Core/Model/HumanitarianNeed.cs`, `record HumanitarianNeed(string Iso3, string Cluster, long? Population, long? InNeed, long? Targeted, int PlanYear, SourceCitation Citation)`.
- Modify: `IndicatorStore` (an HNO list), `AppServices.cs` (register the feeds and their status dots "WHO GHO" and "UN HNO"), `CountryPanelViewModel.cs` and `CountryPanel.xaml` (a section "Basic needs"), `OfflineSamples.cs` (map the new sample URLs).
- Test: `GhoFeedTests`, `HnoFeedTests`, `NeedsPanelTests`, `NeedsFeedTests`.

**Interfaces:**
- Consumes: `IndicatorStore` and series ids `gho.water`, `gho.sanitation`, `gho.uhc` (Task 2).
- Produces: series `hno.shelter` (SHL people in need ÷ Population, %) and `HumanitarianNeed` rows for the panel.

The HNO rows used are the ones with a blank `Category`. They are keyed on `Cluster`, never on `Description`, which varies by language. `ALL` is the total.

The panel section "Basic needs" lists:
- the three GHO values, each with its year and source;
- if the country has an HNO plan: "People in need (UN humanitarian plan 2026)", with rows Total (ALL), Food security (FSC), Water, sanitation and hygiene (WSH), Shelter (SHL) and Health (HEA), each with its number, plus the source line;
- if it has none: "No UN humanitarian needs plan for 2026 lists this country".

- [ ] **Step 1: Write the failing tests:**
  - `GhoFeedTests.ReadsLatestTotalPerCountry`: from the sample, Sudan's water value and year are the newest `RESIDENCEAREATYPE_TOTL` row.
  - `.ReshapedAnswerIsFormatChanged`: a JSON without `value` gives a red dot with "format changed".
  - `.FailureKeepsCache`.
  - `HnoFeedTests.TotalsUseBlankCategoryAndClusterCodes`.
  - `.FrenchDescriptionsStillMatchByCode`.
  - `.ShelterShareIsInNeedOverPopulation`.
  - `NeedsFeedTests.CodesResolveToBoundaryUnits`: every ISO3 in both samples resolves, or is listed as not placed.
  - `NeedsPanelTests.SudanListsPlanRows`.
  - `.NonHnoCountrySaysNoPlan`: Iceland.
- [ ] **Step 2:** Run the tests and see them fail.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests; they pass. Commit with the message `Needs feeds: WHO GHO and UN HNO; Basic needs panel section`.

### Task 4: IPC food-crisis areas

**Files:**
- Create: `src/EarthRelief3D.Core/Feeds/IpcAreaFeed.cs`. For each country with a current national record (not stale, not projection-only), it:
  - finds `ipc_<iso3>.geojson` in the country's HDX package `<slug>-acute-food-insecurity-country-data`, discovered via CKAN `package_search?q=ipc_<iso3>.geojson` or the package's resources;
  - downloads it only when the country's national analysis date changes;
  - checks `license_id` is CC0 or public domain, and skips with a log line otherwise.
- Create: `src/EarthRelief3D.Core/Model/IpcArea.cs`, `record IpcArea(string Iso3, string Name, int? Phase, long? Population, IReadOnlyList<GeoPolygon> Polygons, string AnalysisId, DateOnly From, DateOnly To, SourceCitation Citation)`, using the analysis property names Task 1 recorded.
- Modify:
  - `FoodImageLayer.cs`: a country with matching current areas is drawn by area (fill in the area's IPC colour for phases 3–5; area edges as a 1 px line in the phase colour). Others keep R17.
  - `CrisisText.cs`: the area hover is "<area>, <country> · IPC area classification Phase 4 Emergency · <period> · IPC via HDX".
  - `CountryPanelViewModel.cs`: under the food section, the line "IPC areas: 12 in Phase 3, 5 in Phase 4, 1 in Phase 5 Famine (IPC area classification)".
  - `LegendViewModel.cs`: one line "Where IPC publishes areas, each area is shaded in IPC's own area classification; elsewhere the country is shaded by the 20% rule".
- Test: `IpcAreaFeedTests`, `FoodAreaMapTests`.

Wording: an **area** classified Phase 5 is IPC's Famine classification, so the label is "Phase 5 Famine (IPC area classification)". The country-wide note of R17 stays for countries shaded by the 20% rule.

- [ ] **Step 1: Write the failing tests:**
  - `IpcAreaFeedTests.ReadsAreasWithPhaseAndPolygons`: from the Nigeria sample, the area count and phases match the file.
  - `.SkipsWhenAnalysisDiffersFromNationalRecord`.
  - `.DownloadsOnlyWhenAnalysisDateChanges`.
  - `.NonCc0DatasetIsSkippedAndLogged`.
  - `FoodAreaMapTests.CountryWithAreasIsNotShadedByTheRule`.
  - `.AreaPhase5IsLabelledFamineAreaClassification`.
  - `.PickReturnsTheArea`.
- [ ] **Step 2:** Run the tests and see them fail.
- [ ] **Step 3:** Implement. If Task 1 found no analysis or period property in the GeoJSON, stop here and report, per P3-2.
- [ ] **Step 4:** Run the tests; they pass. Commit with the message `IPC food-crisis areas from HDX (P3-2)`.

### Task 5: Curated atrocity designations and camp sites; atrocities and camps layers; Settings

**Files:**
- Create: `data/curated/designations.json`, `data/curated/camps.json`, `data/curated/governments.json` (`[{ "iso3": "USA", "name": "United States government", "curatedComplete": true, "checkedOn": "<date>" }]`).
- Create: `src/EarthRelief3D.Core/Curated/Designation.cs`:
  - `enum AuthorityType { CourtJudgment, CourtProvisionalMeasures, ArrestWarrant, PendingCase, UnBodyFinding, GovernmentDetermination }`;
  - `record Designation(string Id, string Iso3, string Situation, string Authority, AuthorityType Type, string? GovernmentIso3, string Term, string Group, DateOnly Date, string DocumentTitle, Uri DocumentUrl, Continuing? Continuing, Review Review)`;
  - `record Continuing(string Statement, Uri Url, DateOnly Date)`;
  - `record Review(string Status, DateOnly? ApprovedOn)`.
- Create: `src/EarthRelief3D.Core/Curated/CampSite.cs`, `record CampSite(string Id, string Iso3, string Name, string? LocalName, GeoPosition Position, DocumentCitation PositionSource, string Description, IReadOnlyList<DocumentCitation> DocumentedBy, Review Review)`, with `record DocumentCitation(string Title, string Publisher, Uri Url, DateOnly Date)` (not the feeds' `SourceCitation`).
- Create: `src/EarthRelief3D.Core/Curated/CuratedTables.cs`, which loads and validates the tables (rules in Step 1).
- Create: `src/EarthRelief3D.App/Layers/AtrocityLayer.cs`, a glyph distinct from every other shape: a hollow triangle, colour `#F2F2F2` with a dark edge, at the country's label point. Camps use `PointMarkerLayer` with a square glyph.
- Create: `src/EarthRelief3D.App/Views/SettingsFlyout.xaml(.cs)` and `ViewModels/SettingsViewModel.cs`, with one group "Formal determinations by" built from `governments.json`. The setting is saved.
- Modify: `CountryPanelViewModel.cs` (sections "Genocide and mass atrocities" and "Prison camps"), `LegendViewModel.cs`, `LaunchOptions.cs` (`--show-drafts`), `CrisisSnapshot` or a new `CuratedSnapshot` used by the panel.
- Create: `docs/curated-review.md`, generated by a test helper or the DataTool. It has one block per row: the facts, the citation links, and "approve / correct" for the owner.
- Test: `CuratedTablesTests`, `AtrocityLayerTests`, `SettingsViewModelTests`.

**Research rules for the rows** (a model with web access, at the most capable tier):
- **Designations to research.** Enter each candidate below only if confirmed on the authority's own site (fill in the date, the document title and the exact URL); drop any that don't check out.
  - **Courts:**
    - ICTR, *Akayesu* (1998);
    - ICTY, *Krstić* (2001, and the appeal in 2004), on Srebrenica;
    - ICJ, *Bosnia and Herzegovina v. Serbia and Montenegro* (2007);
    - ICJ, *Croatia v. Serbia* (2015: genocide not proven, so label it as such);
    - ECCC, Case 002/02 (2018);
    - ICJ, *The Gambia v. Myanmar*. Its provisional measures were in 2020. A merits judgment was reported for 19 November 2026; confirm it on icj-cij.org before entering it;
    - ICJ, *South Africa v. Israel* (pending; provisional measures in 2024);
    - ICC arrest warrants that charge genocide or crimes against humanity in current situations, each labelled as a warrant.
  - **UN bodies:**
    - the Commission of Inquiry on the DPRK (2014);
    - the Independent International Fact-Finding Mission on Myanmar (2018);
    - the OHCHR assessment on Xinjiang (2022);
    - the Commission of Inquiry on the Occupied Palestinian Territory (2025);
    - the Independent International Fact-Finding Mission for the Sudan;
    - the commissions on Syria, Burundi and Eritrea, and the ICHREE on Ethiopia, where they make findings of atrocity crimes.
  - **The US government (complete, C15):** every formal Secretary of State genocide, crimes-against-humanity or ethnic-cleansing determination. At least:
    - Darfur (2004);
    - ISIS against Yazidis, Christians and Shia Muslims (2016, and 2017);
    - Xinjiang (19 January 2021);
    - Burma/Rohingya (21 March 2022);
    - Russia in Ukraine, crimes against humanity (February 2023);
    - Ethiopia (March 2023);
    - Sudan: SAF and RSF war crimes, and RSF crimes against humanity and ethnic cleansing (December 2023);
    - Sudan: RSF genocide (7 January 2025).

    Search state.gov and its archives for any others; completeness is the C15 condition.
- **Camps:** Kwan-li-so Nos. 14, 15, 16 and 25 (and No. 18 if HRNK's 2024 report supports it).
  - Name and description from UN COI A/HRC/25/CRP.1, with the paragraph number.
  - Position from the HRNK report's stated coordinates or location map. Its citation is `PositionSource`. If no published coordinate exists, the row is not entered.
  - `DocumentedBy` lists the HRNK reports, newest first.
- **`Continuing`** is set only from a statement dated within 12 months of the check date, by the designating body or a UN report, that the situation continues.
- **Every row starts with `review.status = "draft"`.**

- [ ] **Step 1: Write the failing tests:**
  - `CuratedTablesTests.EveryRowIsCited`: a non-empty title, an absolute https URL and a date for every designation, continuing statement, camp position and camp report. It runs against the real `data/curated` files.
  - `.InterimStepsAreNeverVerdicts`: types `CourtProvisionalMeasures`, `ArrestWarrant` and `PendingCase` get labels "Provisional measures", "Arrest warrant" and "Pending: no finding yet", never the bare term.
  - `.GovernmentDeterminationNamesTheGovernment`.
  - `.OnlyCuratedCompleteGovernmentsAreOffered`.
  - `AtrocityLayerTests.PastDesignationIsHistoryNotMarker`: no `Continuing` within 12 months means no glyph, and a panel history row.
  - `.DeterminationHiddenWhenItsGovernmentIsOff`.
  - `.DraftsHiddenUnlessShowDrafts`, `.DraftsAreLabelled`.
  - `SettingsViewModelTests.DeterminationChoiceIsSaved`.
- [ ] **Step 2:** Run the tests and see them fail.
- [ ] **Step 3:** Implement the model, loader, layers, panel sections and Settings flyout (a gear button at the end of the layer strip).
  - Tooltip example: "Myanmar · Court judgment: genocide (ICJ, 2026)", or "· UN body finding: crimes against humanity (Commission of Inquiry, 2014)".
  - The panel lists every visible designation, newest first, with its type, authority, date and document link, and the continuing statement if any. Then "History" for the rest.
- [ ] **Step 4:** Research and enter the rows per the rules above, all as drafts. Write `docs/curated-review.md`.
- [ ] **Step 5:** Run the tests; they pass. Commit with the message `Curated atrocity designations and camp sites (drafts for owner review); atrocities and camps layers; Settings`.

### Task 6: DataTool and the FAOSTAT, EIA and Aqueduct extracts

**Files:**
- Create: `tools/EarthRelief3D.DataTool/` (net10.0 console; ExcelDataReader 3.7.0 and ExcelDataReader.DataSet 3.7.0; references `EarthRelief3D.Core` for `CountryResolver`). It has these commands:
  - `fao <zip>`: writes `data/datasets/fao-food-security/` with series `fao.undernourishment` (item "Prevalence of undernourishment (percent) (3-year average)") and `fao.energyAdequacy` ("Average dietary energy supply adequacy (percent) (3-year average)"). It keeps the latest year per country and per item, and reads the file as Latin-1. Codes follow P3-11.
  - `eia <INTL.zip>`: writes `data/datasets/eia-crude-oil/`, crude oil including lease condensate, production, TBPD. It keeps the latest year flagged as a full year, not an estimate, and records the flags it saw in the manifest.
  - `aqueduct <zip>`: writes `data/datasets/aqueduct-water-stress/`, sheet `country_baseline`, `indicator_name = bws`, `weight = Tot`, with columns `gid_0, score, cat, label`.
  - `simplify <geojson> <tolerance>`, used for test samples.
- Each output is `data.csv` plus `manifest.json` (P3-10). The licence comes from Task 1's records.
- Create: `src/EarthRelief3D.Core/Feeds/BundledDataset.cs`. It loads a dataset folder into an `IndicatorSeries` with a citation from the manifest. Its status dot shows the dataset's as-of with the note "bundled; updated with the app".
- Modify: `tests/EarthRelief3D.Tests` (a DataTool test project reference, or a `DataTool.Lib` split, whichever keeps the console thin), `AppServices.cs` (load the three datasets into `IndicatorStore` as `fao.undernourishment`, `eia.crudeOil`, `wri.bws`), and the panel (the "Basic needs" section gains "Food supply: dietary energy adequacy N%, <year>, FAO"; a new section "Resources" shows oil production and water stress).
- Test: `DataToolFaoTests`, `DataToolEiaTests`, `DataToolAqueductTests`, `BundledDatasetTests`.

- [ ] **Step 1: Write the failing tests** against small synthetic inputs in the real shapes, with tiny zips built in the test:
  - `DataToolFaoTests.ChinaIsCountedOnce`;
  - `.AggregatesAreDropped`;
  - `.UnmappedCodesAreListedInManifest`;
  - `.LatestYearPerCountry`;
  - `DataToolEiaTests.KeepsLatestFullYear`;
  - `DataToolAqueductTests.TotalWeightOnly`;
  - `.AridCategoryKeptWithLabel`;
  - `BundledDatasetTests.CitationComesFromManifest`;
  - `.MissingFolderIsLoggedNotFatal`.
- [ ] **Step 2:** Run the tests and see them fail.
- [ ] **Step 3:** Implement. Download the three source files into `.downloads/` (no account needed), run the tool for each, and commit the outputs. Never commit the downloads. Check the outputs by hand:
  - Sudan, Yemen and Haiti have undernourishment values;
  - the top 5 oil producers are USA, SAU, RUS, CAN and IRQ, or as EIA's data says;
  - the water-stress category labels match WRI's.

  Record each check in the manifest's `notes`.
- [ ] **Step 4:** Run the tests; they pass. Commit with the message `DataTool; FAOSTAT, EIA and Aqueduct extracts; Hunger, Oil production and Water stress layers`.

### Task 7: GEM marker layers (needs the owner's downloads)

**Files:**
- Modify: DataTool, adding the commands:
  - `gem-power <file>`: operating plants of 500 MW or more, units summed per plant;
  - `gem-oilgas <file>`: operating fields with coordinates;
  - `gem-steel <file>`: operating plants of 1 Mt/yr or more.

  Each writes `data/datasets/gem-<kind>/` with `name, iso3, lat, lon, capacity, unit, status, type` and the filter in the manifest (P3-9). Column names are taken from the real files and recorded in `docs/data-sources.md`.
- Modify: `AppServices.cs`. Three `PointMarkerLayer`s in Resources, with C8 shapes: power is a small disc whose fill colour is by fuel group (fossil, nuclear, hydro, wind or solar, other), with a legend entry for each; oil and gas a diamond; steel a square outline. Size scales by capacity, with the classes in the legend.
- Test: `DataToolGemTests`, `ResourceMarkersTests`.

- [ ] **Step 1:** Check `.downloads\gem\` holds the three files. If not, stop: this task waits for the owner (see Owner items). Record the licence the owner saw. If it is not CC BY 4.0, or forbids redistribution, make these layers "links only" (a legend line linking to the tracker pages, no markers) and record a ruling.
- [ ] **Step 2: Write the failing tests** on a 20-row extract of each real file. This is allowed if the licence is CC BY 4.0, with attribution in `tests/Samples/README.md`; otherwise use synthetic rows in the same columns. The tests:
  - `DataToolGemTests.PowerSumsUnitsPerPlantAndFilters500MW`;
  - `.StatusFilterOperatingOnly`;
  - `.CapAt5000RaisesThresholdAndRecordsIt`;
  - `ResourceMarkersTests.TooltipNamesPlantCapacityStatusAndRelease`, e.g. "<plant> · coal · 1,200 MW · operating · GEM Global Integrated Power Tracker, September 2026".
- [ ] **Step 3:** Implement, run the tool on the owner's files, and commit the outputs.
- [ ] **Step 4:** Run the tests; they pass. Commit with the message `GEM power, oil and gas, and steel marker layers`.

### Task 8: Offline samples, snapshots, docs and final check

**Files:**
- Modify: `OfflineSamples.cs`, so every new feed is answered from `tests/Samples` and the bundled datasets load from `data/datasets`.
- Modify: `tests/SnapshotScenes/scenes.json`. Keep the 5 scenes and add:
  - `water` (Africa, Water shading on, no food);
  - `resources` (the Gulf, oil production shading plus the three GEM layers, if Task 7 ran);
  - `myanmar-panel` (`--show-drafts`, panel MMR, focus atrocities);
  - `nigeria-areas` (food on, look at [9, 8]);
  - `dprk-camps` (`--show-drafts`, camps on, look at [40, 127]).

  The scenes file gains `"showDrafts": true` per scene. `SnapshotScenes.cs` reads it.
- Modify: `README.md`, `docs/data-sources.md`, `docs/plan-2-results.md` → create `docs/plan-3-results.md`, and the spec's rulings table (add P3-1 to P3-11 and any build rulings).

- [ ] **Step 1:** Run all tests: 0 warnings in Release and Debug.
- [ ] **Step 2:** Run the snapshot scenes into `artifacts\snapshot-plan3`, and PngDiff the five old scenes against `artifacts\snapshot-baseline`.
  - The legend and layer strip are expected to change. Anything else is a finding.
  - Read every new picture, and check that:
    - each shading's legend names the measure and the year;
    - unshaded "no figure" countries are distinguishable;
    - the atrocity triangle is visible over Myanmar;
    - the Nigeria areas show IPC colours;
    - DPRK camp squares are placed.
- [ ] **Step 3:** Repeat the run and confirm PngDiff reports identical. Write `docs/plan-3-results.md`: counts, launches, what was seen, and what was not checked by a picture. Do not copy over the baseline (the owner decides).
- [ ] **Step 4:** Commit with the message `Plan 3: samples, snapshots, docs`.
