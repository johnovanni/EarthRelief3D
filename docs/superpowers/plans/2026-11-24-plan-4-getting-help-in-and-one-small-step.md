# Plan 4: Getting Help In and Your One Small Step

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** EarthRelief3D gains:
- the **Getting help in** layer group and panel section: ACAPS humanitarian access scores, appeal funding from FTS, and UN depots, airports and ports with straight-line arcs from the selected country to the nearest of each;
- **Your one small step**: the UN pooled fund (or CERF), vetted US charities, and official people to contact;
- the **vetting tool** the maintainer runs before each release;
- a plain-language **explainer** per crisis kind.

Every value carries its source and as-of date.

**Architecture:**
- **Live feeds** (GlobeCore `IFeed`, results in `IndicatorStore`, cached for free): `AcapsAccessFeed` (HDX XLSX) and `FtsFundingFeed` (HDX CSV).
- **Bundled datasets** made by the existing DataTool (new verbs `airports`, `ports`, `senators`, `workbook`); hubs drawn through the existing `PointMarkerLayer`.
- **Curated tables** (JSON, drafts until the owner approves, a generated review document, tests that fail on a missing citation): UNHRD depots, explainers (in `data/curated/`), and the help catalog (`data/countries/US/`, `data/help/`).
- Arcs are a new app `SceneLayer` plus an `OverlayLayer` for distance labels; **GlobeCore is not changed**.
- A new console project `src/EarthRelief3D.Vetting` checks the charity list against IRS files and writes a report; it never edits the list.

**Tech Stack:** C# / .NET 10, WPF, HelixToolkit.Wpf 3.1.2 (through GlobeCore.Wpf), CommunityToolkit.Mvvm 8.4.0, xunit 2.9.2. `EarthRelief3D.Core` adds ExcelDataReader 3.7.0 (MIT; already used by the DataTool) to read ACAPS's workbook at run time.

**Spec:** `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md`. Relevant parts: §1 (success criteria, anti-goals), §4 (`AccessAssessment`, `FundingStatus`, `LogisticsHub`, `HubLink`, `HelpOption`, `CountryProfile`, `Explainer`, `NearestHubFinder`, `HelpCatalog`, `VettingRules`), §5.4, §8, §9, §10, §11, §13; calls C3, C5, C6, C10, C12, C13; P3-8 (shelter supply = UNHRD depots).

## Sources checked while writing this plan (2026-11-24)

Each source was checked live on 2026-11-24 (notes in the session scratchpad; Task 1 re-checks and records them in `docs/data-sources.md`).

| Source | What it gives | Access | Licence | Use |
|---|---|---|---|---|
| ACAPS Humanitarian Access, HDX `acaps-humanitarian-access-dataset` | XLSX, newest resource `acaps-humanitarian-access-dataset_dec_2025.xlsx` (1.16 MB). Sheet `csv`: `Crisis_name, Crisis ID, P1, P2, P3, ACCESS, Info_gap, Trend, Country level, Country Name, ISO` (83 rows; ACCESS 0–5, pillars 0–5, Trend Improving/Stable/Deteriorating/New). Sheet `indicators`: nine indicator columns `1. Denial of existence of needs` … `9. Environmental constraints` (0–3), joined on `Crisis ID` | CKAN, no key | HDX: CC BY. **ACAPS's own copyright page: CC BY-NC-ND 4.0** for ACAPS-branded products | Live feed only (P4-2) |
| FTS requirements and funding, HDX `global-requirements-and-funding-data` | `fts_requirements_funding_global.csv` (281 KB, daily): `countryCode, id, name, code, typeId, typeName, startDate, endDate, year, requirements, funding, percentFunded` | CKAN, no key | HDX CC BY-IGO; FTS site CC BY 4.0; FTS terms prescribe the credit: "Financial Tracking Service (FTS), <date>, <URL>" | Live feed |
| OCHA country-based pooled funds | 20 fund pages on unocha.org/country-based-pooled-funds (5 regional); CBPF OData `Poolfund` (46 rows, no "active" flag). **No per-fund donate page**: OCHA's Donate goes to crisisrelief.un.org; CERF has cerf.un.org/donate ("for U.S. tax-deductible gifts" through the UN Foundation) | Open | Portal CC BY 4.0 | Curated `pooled-funds.json` |
| NGA World Port Index (Pub 150) | msi.nga.mil returns 503 (maintenance); its link `https://msi.nga.mil/apology_objects/WPI.zip` works (378 KB, `UpdatedPub150.csv`, 3,809 ports, file date 2026-11-05). Columns `Main Port Name, UN/LOCODE, Country Code` (a country **name**), `Harbor Size` (Large 174, Medium 376), `Latitude, Longitude` | No key | No statement readable today; a US federal work (17 U.S.C. §105) | Bundled extract |
| OurAirports `ourairports-data` (GitHub) | `airports.csv` (12.8 MB, daily): `type, name, latitude_deg, longitude_deg, iso_country` (ISO2), `scheduled_service, iata_code`; large 1,174 (1,153 scheduled), medium 4,108 (2,117 scheduled) | No key | The Unlicense | Bundled extract |
| UNHRD (unhrd.org) | The five hubs | **403 to scripts** (unhrd.org and wfp.org) | Facts | Curated drafts; Task 1 tries the built-in browser |
| Logistics Cluster | Country pages `https://logcluster.org/en/where-we-work/{iso3}/{slug}` | Readable | Terms: no commercial use, credit | Links only |
| HDX HAPI operational presence (3W) | `hdx_hapi_operational_presence_global.csv` (9.3 MB, about 21 countries): `location_code, org_acronym, org_name, org_type_description, sector_name, reference_period_end` | No key (the HAPI **API** needs an email-derived identifier: not used) | CC BY-IGO | Drafting evidence only (Task 8), not shipped |
| IRS Publication 78 and auto-revocation | `https://apps.irs.gov/pub/epostcard/data-download-pub78.zip`, `…/data-download-revocation.zip`, pipe-delimited `EIN|Name|City|State|Country|Deductibility Code`, monthly | No key | US government work | Vetting tool |
| IRS Form 990 e-file XML | `https://apps.irs.gov/pub/epostcard/990/xml/<year>/index_<year>.csv` (`RETURN_ID, FILING_TYPE, EIN, TAX_PERIOD, SUB_DATE, TAXPAYER_NAME, RETURN_TYPE, DLN, OBJECT_ID, XML_BATCH_ID`), zips `…/<year>/<year>_TEOS_XML_01A.zip` …; Part IX line 25 `TotalFunctionalExpensesGrp/TotalAmt` and `/ProgramServicesAmt` | No key | US government work | Vetting tool |
| US Senate `senators_cfm.xml` | 100 members: `member_full, last_name, first_name, party, state, address, phone, email` (a contact-form URL), `website, bioguide_id`; `last_updated` | No key | US government work | Bundled `senators.json` |
| House "Find your representative" | `https://www.house.gov/representatives/find-your-representative` | Link; House FAQ: no restriction on linking | — | Link |
| US State Department contact page | — | **403 to scripts** | — | Task 1 tries the built-in browser |
| UN Blue Book | `https://www.un.org/dgacm/en/content/protocol/blue-book` | Open | UN terms: personal, non-commercial, **no compiling or redistribution** | One link, no per-country list (P4-10) |
| Charity Navigator, BBB Wise Giving Alliance (give.org) | Profiles `https://www.charitynavigator.org/ein/<EIN>`; give.org uses internal ids | Open in a browser | CN terms bar republishing and bots or scraping; give.org: linking allowed for some, framing needs permission | Links only; **the owner confirms ratings** (P4-8) |

## Rulings made while writing this plan

| # | Ruling | Reason | Cost if wrong |
|---|---|---|---|
| P4-1 | **No source needs a key, an account or an email** (C12). HAPI's API is not used; FTS, ACAPS and 3W come from HDX CKAN files | C12; nothing waits on the owner | None |
| P4-2 | **ACAPS is treated by its stricter licence** (CC BY-NC-ND 4.0) although HDX says CC BY: fetched live from HDX only, cached in the user's data folder, never committed; values are shown unchanged (scores as published, ACAPS's own wording, no score of ours); the credit names both licences. Test samples are synthetic in ACAPS's layout (as R5 did for WHO). `docs/permission-requests.md` gains a short note asking ACAPS to confirm | The two statements conflict; C6 already covers the stricter case | If ACAPS objects even to live use, the layer and section are removed (one feed) |
| P4-3 | **The ACAPS edition is the newest XLSX resource on HDX** (by the edition parsed from its file name; ledger R4: HDX's `last_modified` can be later for an older edition), named from its file name (`…_dec_2025.xlsx` → "December 2025 edition", `CitationDate.Release` with that `DateText`). ACAPS's July 2026 edition is not on HDX; the app shows the edition it has, and moves on when HDX posts a newer one | Honest date; no scraping of acaps.org | The map shows an edition about a year old until HDX updates |
| P4-4 | **Appeal funding:** FTS rows for the clock's year with a plan id (the latest year with plans, at most last year, in early January). A plan whose id appears for more than one country, or whose `typeName` contains "Regional", is a **regional plan**: shown with "figures for the whole plan, all its countries", never summed into a country figure. `percentFunded` is shown as FTS publishes it | FTS publishes per plan, not per country, for regional plans | — |
| P4-5 | **Hubs:** airports = OurAirports `large_airport` and `medium_airport` with `scheduled_service = yes` (3,270 today); ports = WPI `Harbor Size` Large or Medium (550); UN depots = UNHRD's hubs, curated (drafts until the owner approves). Each is a marker layer in Getting help in, off by default. One status dot, "Hubs" | Spec §4 "large or medium"; scheduled service keeps the airport set open and under the 5,000 cap | Small airstrips used by aid flights are not shown |
| P4-6 | **The crisis place for arcs is the country's `LabelPoint`** (GlobeCore's point inside the country). Distances are great-circle on a 6,371 km sphere, rounded to whole km or miles, always written "straight-line" (C3) | The panel is per country (C1); a point inside the country is honest | Large countries' arcs start from the middle, not the crisis area |
| P4-7 | **Arcs show when the panel is open and the "Lines to nearest hubs" toggle is on** (default on). Three arcs at most: nearest approved UN depot, airport, port. The panel lists the same three with distances | Spec §8 | — |
| P4-8 | **Ratings are confirmed by the owner, not by scripts or agents.** Charity Navigator's terms bar bots and scraping, so no task fetches its pages or give.org's. Drafts carry the profile links; the owner opens them and fills `ratings` (evaluator, report URL, date checked) when approving. An entry without a confirmed qualifying rating is hidden (§9.2 check 2) | Respects the evaluators' terms; the owner is the reviewer (C10) | Until the owner confirms, every country shows the fund and "No vetted charities listed yet" |
| P4-9 | **The first batch of charities follows a rule, not a choice:** organisations in HDX's 3W file with a reference period ending within the last 12 months, present in at least 5 of its countries, that are (or have a US affiliate that is) a US public charity in Publication 78 (deductibility code `PC`). If more than 15 qualify, the country count rises by 1 until 15 or fewer remain (recorded). Each charity is listed only in countries where both 3W and the charity's own published programme page show it working. Others can be added later by the same checks | §9.2 check 3 asks for evidence like 3W; a rule avoids the app's own opinion (anti-goal "never ranks by its own opinion") | Charities absent from 3W's 21 countries are not in the first batch |
| P4-10 | **UN missions:** one link to the UN Blue Book ("<Country>'s mission to the UN: UN Blue Book"); no `missions.json` and no embassy links in Plan 4 | The Blue Book's terms forbid compiling its list | The user looks the mission up in the Blue Book |
| P4-11 | **"Give" for UN funds:** a country with an OCHA pooled fund shows "UN pooled fund for <country> (OCHA)" with links to the fund's OCHA page and to OCHA's "how to give" page (`https://www.unocha.org/our-funding`). A regional fund covers the countries the CBPF API lists as its sub-funds. Every other country shows CERF with `https://cerf.un.org/donate` and the note "US tax-deductible gifts go through the UN Foundation; CERF's page explains how" | No per-fund donate page exists | — |
| P4-12 | **"Your one small step" shows for a country with a current crisis** (the panel's existing count: conflict, outbreak, current food, atrocity triangle or camp) **or an FTS appeal for the year**. Getting help in shows for every country | §9 is "for each crisis" | — |
| P4-13 | **Contacts, pooled funds, depots and explainers are curated drafts** with the Plan 3 `Review` record, shown only after approval (or with `--show-drafts`, labelled "Draft: not yet reviewed"), listed for the owner in generated review documents | C10 | — |
| P4-14 | **New marker shapes:** UN depots `Star` (★, `#4FC3F7`), airports `HexagonOutline` (⬡, `#E8E8E8`), ports `DiamondOutline` (◇, `#80CBC4`); hub lines `Arc` (⌒, each arc in its hub's colour). Access shading: ColorBrewer YlOrBr, 6 classes for scores 0–5 `#FFFFD4 #FEE391 #FEC44F #FE9929 #D95F0E #993404` (ledger R5: ACAPS names 0 "No constraints") | C8 colour plus shape; no shape is shared with another layer | — |
| P4-15 | **Settings gain** "Your country" (only countries with a profile: United States), "Your state (optional)" (the 50 states in `senators.json`, plus "Not chosen", the default) and "Distances" (km, default, or miles). Nothing else about the user is stored | §9.1, §11; anti-goal "collects no personal data" (a state is not personal data and is optional) | — |

## Owner items

- **After Task 1:** if the built-in browser could not open unhrd.org or state.gov, Task 1's report names the pages; open them in a normal browser and say what they show (the rows wait as drafts meanwhile).
- **After the plan:**
  - Review `docs/help-review.md`: for each charity, open its Charity Navigator and BBB links, and if it qualifies (Charity Navigator 3★ or more, or all 20 BBB standards met), say so; Claude fills in the rating record and approval. Approve or correct the pooled funds, contacts and depots there too.
  - Review the explainers in `docs/curated-review.md`.
  - Optionally send the ACAPS note in `docs/permission-requests.md`.

## Global Constraints

- Everything in Plans 2 and 3's Global Constraints still holds. In particular:
  - every fact on screen shows its source and an "as of" date;
  - no sound, no graphic images, no pop-up alarms, and no personal data;
  - C8 colour plus shape; C11 crisis thresholds;
  - C6: non-commercial, no-derivatives and share-alike data is fetched live only, never committed;
  - local commits only, and no push without the owner's OK; the commit attribution line;
  - User-Agent `EarthRelief3D/<version> (+<project page>)`;
  - visual checks only through `--snapshot-scenes` (RenderTargetBitmap); never a screen capture;
  - country codes join through `CountryResolver`/`CountryCodes` against the boundary units (C16); an unresolved code is listed under "Not placed on the map", never dropped silently.
- **At most 4 app launches in this plan:** Task 10's snapshot run, one re-run after tuning, the repeat check, and one spare.
- **The app never handles money and never asks for an address or ZIP code** (§9.1). All links open in the user's browser and are `https` (a test checks every link in the help catalog).
- **The app never ranks charities:** the UN fund first, then charities alphabetically by name (§9.2).
- **Ratings are never copied:** the app shows "Rated by <evaluators>" with links to their reports, never stars or scores (Q1).
- **An entry is hidden when any check is missing or its review is older than 12 months** (§9.2); hidden, never shown out of date.
- **Distances are labelled "straight-line"** and computed on a 6,371 km sphere (C2, C3).
- **No script or agent fetches charitynavigator.org or give.org pages** (P4-8). A site that refuses scripts (403) may be opened once in the built-in browser for a normal page view; if a challenge or CAPTCHA appears, stop and leave it to the owner.
- **Curated rows:** every row has a citation URL (https), a title and a date; a test fails the build if one is missing; drafts are hidden in normal runs (P4-13).
- **GlobeCore is not changed in this plan.** If a task finds it must change GlobeCore, it stops and reports.

## Review Focus

1. **A country with no figure must say so, never look fine.** No ACAPS row: "No ACAPS assessment for <country> in the <edition>"; no appeal: "No appeal in FTS for <year>"; no pooled fund: the CERF line; no charity: "No vetted charities listed yet". Not yet loaded: "Data not loaded yet". Pinned by `AccessPanelTests.NoAssessmentSaysSo`, `FundingPanelTests.NoAppealSaysSo` (Task 3), `HelpPanelTests.CerfWhenNoFund` and `.NoCharitiesSaysSo` (Task 6).
2. **Regional appeals are not country figures.** A regional plan reads "figures for the whole plan, all its countries" and is never added to a country's total. Pinned by `FtsFundingFeedTests.RegionalPlanFlagged` (Task 2) and `FundingPanelTests.RegionalPlanSaysWholePlan` (Task 3).
3. **A stale or incomplete charity entry is hidden.** Exactly 12 months after review it disappears; a draft, a missing IRS check, a missing or unlisted rating, or no evidence for this country hides it. Pinned by `VettingRulesTests` (Task 6).
4. **Arcs and nearest hubs near the dateline and the poles.** Fiji's hubs either side of 180°, a point at 89.9° N, ties broken by name. Pinned by `NearestHubFinderTests.AcrossDateLine`, `.NearPole`, `.TieBrokenByName` and `HubArcGeometryTests.DateLineArcIsContinuous` (Task 5).
5. **No state chosen.** The senators line asks the user to choose a state in Settings; choosing one updates the open panel at once. Pinned by `HelpPanelTests.NoStateAsksForState` and `.StateChangeRebuildsPanel` (Task 6).

---

### Task 1: Sources re-check, samples, docs

**Files:**
- Modify: `tools/fetch-samples.ps1` (a `-Set Plan4`), `docs/data-sources.md` (a section per new source, as in Plan 3), `docs/permission-requests.md` (the ACAPS note, P4-2), `tests/Samples/README.md`
- Create in `tests/Samples/`:
  - `hdx_acaps_package.json` (CKAN `package_show`, trimmed to the two newest resources);
  - `acaps_access_synthetic.json` and `acaps_access_synthetic.xlsx`: **synthetic** values for 12 countries (SDN, PSE, YEM, AFG, COD, HTI, MMR, SOM, UKR, SSD, NGA, VEN), ACAPS's exact sheet and column names, one row with an ISO3 the boundary set does not have (`XXK`) and one with `Country level` not "Yes"; the README says "synthetic, ACAPS layout" (P4-2);
  - `hdx_fts_package.json`; `fts_requirements_funding_global.csv` trimmed to the 2026 rows plus the 2025 rows of AFG, SDN and HTI (real data, CC BY-IGO);
  - `ourairports_excerpt.csv` (200 real rows: every type, both `scheduled_service` values, AFG, SDN, FJI, USA, ISL, NOR above 70° N);
  - `wpi_excerpt.csv` (100 real rows, header unchanged: every `Harbor Size` value including blank, a row with an empty `UN/LOCODE`);
  - `senators_cfm_excerpt.xml` (6 members, 3 states, real; public domain).
- Create: `data/help/logistics-cluster.json`: `[{ "iso3", "url", "title", "checkedOn" }]`, one row per country page Task 1 opens on logcluster.org's "Where we work" list.
- Modify `tools/EarthRelief3D.DataTool`: verb `workbook <json> --out <xlsx>` writing an XLSX from `{ "sheets": [ { "name", "rows": [[...]] } ] }` (move the workbook writer that the DataTool tests already use into the DataTool as `XlsxWriter`; the tests call it from there).

- [ ] **Step 1:** Run the fetch script (network). Confirm and record in `docs/data-sources.md`:
  - ACAPS: the `csv` and `indicators` sheet columns above; the ACCESS level names and the indicator scale (0–3) as the workbook's `About` or `dictionary` sheet words them, and whether 0 has a name; the website wording of the nine indicators (the overview page; quote it); the resource fields that name the edition.
  - FTS: the 12 columns; what a blank `id` row means.
  - WPI: re-open `msi.nga.mil`; record the licence statement and edition if it is back, otherwise "portal in maintenance on <date>; file date 2026-11-05".
  - OurAirports and the Senate XML columns above.
  - IRS: whether `apps.irs.gov` answers a `Range: bytes=0-99` request on a `…_TEOS_XML_…zip` with 206 (record it: Task 7 relies on it).
  If a field this plan relies on is missing, stop and report.
- [ ] **Step 2:** Open in the **built-in browser** (P4 constraint): `https://unhrd.org` (each hub's page: name, city, country, street address, any coordinates) and state.gov's public contact page (its URL). Record what each page shows, with the date. If a page shows a challenge, stop there and name it in the report for the owner.
- [ ] **Step 3:** Confirm each licence on its own page and record the exact credit text (ACAPS both statements, FTS terms sentence, CBPF portal footer, OurAirports LICENSE, NGA if readable, Senate/House).
- [ ] **Step 4:** Write the ACAPS note, the data-sources sections, the samples (build the synthetic XLSX with `dotnet run --project tools/EarthRelief3D.DataTool -- workbook tests/Samples/acaps_access_synthetic.json --out tests/Samples/acaps_access_synthetic.xlsx`) and `logistics-cluster.json`. Run `dotnet test EarthRelief3D.sln -c Release`: all pass. Commit `Plan 4: sources re-checked, samples, docs`.

### Task 2: ACAPS access and FTS funding feeds

**Files:**
- Create: `src/EarthRelief3D.Core/Model/AccessAssessment.cs`, `src/EarthRelief3D.Core/Model/AppealFunding.cs`, `src/EarthRelief3D.Core/Feeds/AcapsAccessFeed.cs`, `src/EarthRelief3D.Core/Feeds/FtsFundingFeed.cs`
- Modify: `src/EarthRelief3D.Core/Store/IndicatorStore.cs`, `src/EarthRelief3D.Core/Store/IndicatorCache.cs`, `src/EarthRelief3D.Core/EarthRelief3D.Core.csproj` (ExcelDataReader 3.7.0), `src/EarthRelief3D.App/Presentation/SourceDates.cs`, `src/EarthRelief3D.App/Services/AppServices.cs`, `src/EarthRelief3D.App/Diagnostics/OfflineSamples.cs`
- Test: `tests/EarthRelief3D.Tests/AcapsAccessFeedTests.cs`, `FtsFundingFeedTests.cs`, `IndicatorStoreTests.cs`, `AppStartupTests.cs`, `FeedTestSupport.cs` (`Samples.AcapsHttp()`, `Samples.FtsHttp()`)

**Interfaces:**
- Produces:
  - `public sealed record AccessIndicator(int Number, string Name, int Score);` (`Name` = the workbook's column text without its number)
  - `public sealed record AccessAssessment(string Iso3, int Score, IReadOnlyList<int> Pillars, IReadOnlyList<AccessIndicator> Indicators, string Trend, double? InformationGap, string Edition, SourceCitation Citation);`
  - `public sealed record AppealFunding(string Iso3, int PlanId, string PlanName, string PlanType, int Year, long? RequirementsUsd, long FundingUsd, int? PercentFunded, bool Regional, SourceCitation Citation);`
  - `IndicatorSnapshot` gains `IReadOnlyList<AccessAssessment> Access` and `IReadOnlyList<AppealFunding> Appeals` (empty by default), `AccessAssessment? AccessOf(string iso3)`, `IReadOnlyList<AppealFunding> AppealsOf(string iso3)`.
  - `IndicatorStore.ReplaceAccess(IReadOnlyList<AccessAssessment> access, IndicatorSeries score) -> bool` (one event) and `IndicatorStore.ReplaceAppeals(IReadOnlyList<AppealFunding> appeals) -> bool`.
  - `IndicatorCache` DTO gains optional `Access` and `Appeals` (no schema bump, as `Needs`).
  - `AcapsAccessFeed(FeedHttp, IndicatorStore, CrisisStore, IClock, IAppLog, CountryResolver? = null) : IFeed`: `FeedName = "ACAPS"`, `Interval` 7 days, `ScoreSeriesId = "acaps.access"`, `PackageUrl = https://data.humdata.org/api/3/action/package_show?id=acaps-humanitarian-access-dataset`, `PageUrl = https://data.humdata.org/dataset/acaps-humanitarian-access-dataset`, `static string EditionFromFileName(string fileName)`.
  - `FtsFundingFeed(...) : IFeed` with the same constructor: `FeedName = "FTS"`, `Interval` 1 day, `PackageUrl = …package_show?id=global-requirements-and-funding-data`, resource `fts_requirements_funding_global.csv`, `CreditUrl = https://fts.unocha.org`.
  - `SourceDates.HelpFeedNames = [AcapsAccessFeed.FeedName, FtsFundingFeed.FeedName]`; `AppServices.FeedNames` = Plan 3's list with `HelpFeedNames` before `"Borders"`.

- [ ] **Step 1: Write the failing tests.**
  - `AcapsAccessFeedTests.ReadsCountryLevelRows`: 11 assessments (12 rows minus the non-country-level one; `XXK` unplaced); SDN's `Score`, three `Pillars` and nine `Indicators` (numbers 1–9) equal the synthetic file's; series `acaps.access` has one `CountryValue` per placed country.
  - `.EditionFromFileName`: `"acaps-humanitarian-access-dataset_dec_2025.xlsx"` → `"December 2025 edition"`; `"…_jul_2026.xlsx"` → `"July 2026 edition"`; an unparsable name throws `FeedFormatException`.
  - `.CitationNamesBothLicences`: `Licence` contains `"CC BY"` and `"CC BY-NC-ND 4.0"`; `DateMeaning == CitationDate.Release`; `DateText == "December 2025 edition"`.
  - `.NewestEditionIsUsed` (two resources; the older edition has the later `last_modified`; the newer edition by file name wins, R4), `.ReshapedWorkbookIsFormatChanged` (a missing `ACCESS` column), `.UnknownIsoIsUnplaced` (`XXK` in `CrisisStore` unplaced under "ACAPS"), `.FailureKeepsCache`.
  - `FtsFundingFeedTests.CurrentYearOnly` (clock 2026-11-24: only 2026 rows), `.EarlyJanuaryFallsBackToLastYear` (clock 2027-01-02 and no 2027 rows: the 2026 rows), `.PercentAsPublished` (AFG plan 1502: requirements 1,714,181,496, funding 580,680,803, 34%), `.BlankRequirementsIsNull`, `.RowsWithoutPlanIdAreSkipped`, `.RegionalPlanFlagged` (one id under three country codes: `Regional` true for all three; a `typeName` containing "Regional" alone also sets it), `.CreditFollowsFtsTerms` (`Attribution` == `"Financial Tracking Service (FTS), <d MMM yyyy>, https://fts.unocha.org"` with the resource's `last_modified` date).
  - `IndicatorStoreTests.AccessAndAppealsRoundTripThroughCache`.
  - `AppStartupTests`: the feed and dot lists include "ACAPS" and "FTS" before "Borders".
- [ ] **Step 2:** Run `dotnet test EarthRelief3D.sln -c Release --filter "FullyQualifiedName~AcapsAccessFeedTests|FullyQualifiedName~FtsFundingFeedTests"`: fails (types missing).
- [ ] **Step 3:** Implement. ACAPS: `package_show` → the XLSX resource with the latest edition parsed from its file name (R4; unparsable names skipped and logged; none parsable → `FeedFormatException`) → ExcelDataReader → sheet `csv` rows with `Country level == "Yes"` → `indicators` sheet joined on `Crisis ID` → `ReplaceAccess`; `MarkChecked`; unplaced via `ReplaceUnplaced`. Citation: publisher `"ACAPS, via HDX"`, dataset `"Humanitarian Access dataset"`, licence `"CC BY (as stated on HDX); ACAPS's own terms: CC BY-NC-ND 4.0"`, `AsOfUtc` = last day of the edition month. FTS: `package_show` → the CSV → rows per P4-4 → `ReplaceAppeals`. Register both feeds in `AppServices.Create` and map their URLs in `OfflineSamples.CreateHandler` and `Files`.
- [ ] **Step 4:** Run all tests (`dotnet test EarthRelief3D.sln -c Release`): all pass, 0 warnings.
- [ ] **Step 5:** Commit `ACAPS access and FTS funding feeds`.

### Task 3: The Getting help in group, access shading and panel section

**Files:**
- Modify: `src/EarthRelief3D.App/Presentation/LayerStyle.cs` (`ShadingLayer.Access` appended), `Presentation/LayerCatalog.cs` (`HelpShadings = [ShadingLayer.Access]`, Id `"access"`, label "Access constraints"), `Presentation/ShadingScale.cs` (`Access`: categorical, breaks `[0, 1, 2, 3, 4, 5]`, P4-14 colours (R5), class labels = ACAPS's level names from Task 1), `Layers/CountryValueLayer.cs` (`Layers` includes `HelpShadings`), `ViewModels/LayerStripViewModel.cs` (`GettingHelpGroup = "Getting help in"`, a collapsible group; `ComingGroups` removed), `Views/LayerStrip.xaml` (the coming-groups block removed), `ViewModels/LegendViewModel.cs`, `ViewModels/CountryPanelViewModel.cs`, `Views/CountryPanel.xaml` (the "coming in a later version" line removed), `Diagnostics/SnapshotScenes.cs` (`panelFocus` `"access"`)
- Create: `src/EarthRelief3D.Core/Help/LogisticsClusterLinks.cs` (loads `data/help/logistics-cluster.json`)
- Test: `AccessPanelTests.cs`, `FundingPanelTests.cs`, `LayerStripTests.cs`, `ShadingScaleTests.cs`, `LegendViewModelTests.cs`, `SnapshotScenesTests.cs`

**Interfaces:**
- Consumes: Task 2's `IndicatorSnapshot.AccessOf/AppealsOf`, series `acaps.access`.
- Produces: `CountryPanelViewModel.GettingHelpHeading = "Getting help in"`, `FundingHeading` = `"Humanitarian appeals <year>"` (built per year), `AccessPanel.IndicatorWording` (the nine website texts, Task 1), `LayerStripViewModel.GettingHelpGroup`, `LayerCatalog.HelpShadings`; `CountryPanelViewModel.Show(string iso3, CrisisLayer? focus = null, MarkerLayer? marker = null, PanelFocus? section = null)` with `public enum PanelFocus { Access, Help }` (Task 6 uses `Help`).

- [ ] **Step 1: Write the failing tests.**
  - `AccessPanelTests.ScoreUsesAcapsWords`: SDN score 5 → row `("Access constraints", "5 of 5: <ACAPS's name for 5>")`; nine indicator rows labelled with the website wording, values `"<n> of 3"`, and an indicator the assessment leaves out reads `"No score (information gap)"` (ledger R8); a `Trend` row; the source line `Source: ACAPS, via HDX, Humanitarian Access dataset · December 2025 edition`.
  - `.NoAssessmentSaysSo`: ISL → `"No ACAPS assessment for Iceland in the December 2025 edition"`; before the feed runs → `"Data not loaded yet"`.
  - `.ZeroIsNoConstraints` (R5): a score of 0 is shaded the lightest class and reads "0 of 5: No constraints" in the tooltip and panel.
  - `FundingPanelTests.PlanRow`: AFG → `("Afghanistan Humanitarian Needs and Response Plan 2026", "34% funded: $580,680,803 of $1,714,181,496")`; `.RegionalPlanSaysWholePlan` (the value ends `" (regional plan: figures for the whole plan, all its countries)"`); `.NoAppealSaysSo` (`"No appeal in FTS for 2026"`); `.LogisticsClusterLinkWhenListed` (SDN has a `PanelLink("Logistics Cluster: Sudan operation", …)`; ISL none).
  - `LayerStripTests.GettingHelpGroupReplacesPlaceholder` (four groups; no `ComingGroups`; the new group holds Access), `.AccessIsOneOfTheShadings` (turning Access on turns Water off).
  - `ShadingScaleTests.AccessScale`, `LegendViewModelTests.AccessLine` (`"Humanitarian access constraints (ACAPS score 0–5) · December 2025 edition"`).
  - `SnapshotScenesTests.AccessFocusIsAccepted`.
- [ ] **Step 2:** Run the new tests: they fail.
- [ ] **Step 3:** Implement. The Getting help in section comes after the needs and resources sections and is built for every country; the funding section follows it.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Getting help in group, access shading and panel section`.

### Task 4: Hub datasets and markers

**Files:**
- Modify `tools/EarthRelief3D.DataTool`: verbs `airports <airports.csv>` → `data/datasets/ourairports/`, `ports <WPI.zip or UpdatedPub150.csv>` → `data/datasets/wpi-ports/`; create `HubExtractor.cs`; `CountryCodes.FromIso2(string)`
- Create: `data/curated/unhrd-depots.json` (drafts from Task 1's record), `data/datasets/ourairports/`, `data/datasets/wpi-ports/` (run the verbs on downloads saved under `.downloads/hubs/`, a new empty folder)
- Modify: `src/EarthRelief3D.Core/Curated/CuratedTables.cs` (`Depots`, file `unhrd-depots.json`, `Validate`), `Curated/CuratedReview.cs` (a "UN depots" section), `Core/Feeds/BundledPoints.cs` (`Citation` also accepts `dateMeaning` `"AsOf"` without `dateText`), `App/Presentation/LayerStyle.cs` (`MarkerLayer.UnDepots, Airports, Ports` appended; `MarkerShape.Star, HexagonOutline, DiamondOutline` appended; P4-14 colours and symbols), `App/Layers/MarkerMeshes.cs` (`AddStar`, `AddDiamondOutline`), `App/Layers/PointMarkerLayer.cs`, create `App/Layers/HubMarkers.cs` (like `ResourceMarkers`: `DotName = "Hubs"`, `Datasets = [(Airports, "ourairports"), (Ports, "wpi-ports")]`, depots from approved curated rows), `LayerCatalog` (`HelpMarkers = [UnDepots, Airports, Ports]`), `LayerStripViewModel`, `LegendViewModel`, `AppServices`
- Test: `DataToolHubsTests.cs`, `HubMarkersTests.cs`, `CuratedTablesTests.cs`, `LayerStripTests.cs`, `PointMarkerLayerTests.cs`

**Interfaces:**
- Produces: `public sealed record Depot(string Id, string Name, string City, string Iso3, string Address, GeoPosition Position, DocumentCitation PositionSource, DocumentCitation Source, Review Review, DateOnly CheckedOn) { public string? Verification { get; init; } }`; `CuratedTables.Depots`, `CuratedTables.VisibleDepots(CuratedFilter)`; `HubMarkers.For(MarkerLayer, BundledPointsLoad)`, `HubMarkers.ForDepots(CuratedStore) -> PointSet?`.

- [ ] **Step 1: Write the failing tests.**
  - `DataToolHubsTests.AirportsKeepScheduledLargeAndMedium` (excerpt: only `large_airport`/`medium_airport` with `scheduled_service == "yes"`; `type` large/medium; `detail` = IATA code), `.PortsKeepLargeAndMedium` (blank and Small/Very Small dropped, the counts written to the manifest), `.PortCountryFromUnLocode` (`"SD PZU"` → SDN; empty UN/LOCODE → the country name through `CountryCodes.FromName`; unknown → manifest `notPlaced`), `.ManifestsCiteTheSource` (OurAirports: licence `"The Unlicense (public domain)"`, `dateMeaning` `"AsOf"` = the download date; WPI: publisher `"US National Geospatial-Intelligence Agency (NGA)"`, dataset `"World Port Index (Pub 150)"`, licence `"Public domain (US government work, 17 U.S.C. 105)"`, as-of the CSV's date in the zip).
  - `CuratedTablesTests.EveryDepotIsCited` (position source and source cited, https, dated), `.DraftDepotHidden`.
  - `HubMarkersTests.OneDotNamesBothDatasets`, `.TooltipSaysWhatItIs` (`"Port Sudan · port (large harbour) · NGA World Port Index"`).
  - `LayerStripTests.GettingHelpGroupHoldsHubs` (Access, UN depots, Airports, Ports; markers off by default).
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement; download `airports.csv` and `WPI.zip` into `.downloads/hubs/` and run both verbs; check the counts against the sources table (about 3,270 airports, 550 ports) and record them in the manifests' notes.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Hub datasets and markers: UN depots, airports, ports`.

### Task 5: Nearest hubs and arcs

**Files:**
- Create: `src/EarthRelief3D.Core/Logic/NearestHubFinder.cs`, `src/EarthRelief3D.App/Layers/HubArcLayer.cs` (`SceneLayer`, z-order 125), `src/EarthRelief3D.App/Layers/HubLabelsLayer.cs` (`OverlayLayer`, z-order 201)
- Modify: `App/Settings/AppSettings.cs` (`DistanceUnit Units = DistanceUnit.Kilometres`), `App/ViewModels/SettingsViewModel.cs` and `Views/SettingsFlyout.xaml` ("Distances": km or miles), `App/Presentation/CrisisText.cs` (`Distance`), `LayerStyle.cs` (`MarkerLayer.HubLines`, `MarkerShape.Arc` "⌒", default on), `LayerCatalog`, `CountryPanelViewModel` (hub rows in the Getting help in section), `App/Controllers/SceneController.cs` (add the two layers; redraw on panel open, close and country change)
- Test: `NearestHubFinderTests.cs`, `HubArcGeometryTests.cs`, `DistanceTextTests.cs`, `HubPanelTests.cs`, `SettingsViewModelTests.cs`

**Interfaces:**
- Produces:
  - `public enum HubKind { UnDepot, Airport, Port }`
  - `public sealed record Hub(HubKind Kind, string Name, string Iso3, SurfacePoint Position, string Detail, SourceCitation Citation);`
  - `public sealed record HubLink(Hub Hub, double DistanceKm);`
  - `public static class NearestHubFinder { public const double EarthRadiusKm = 6371.0; public static IReadOnlyList<HubLink> Nearest(SurfacePoint from, IReadOnlyList<Hub> hubs); }`: at most one link per kind, in `HubKind` order; ties broken by `Name` (ordinal); a kind with no hub is left out.
  - `HubMarkers.Hubs(IReadOnlyList<(MarkerLayer Layer, BundledPointsLoad Load)> loads, IReadOnlyList<Depot> visibleDepots) -> IReadOnlyList<Hub>` (Task 4's loads and `CuratedTables.VisibleDepots`), rebuilt when the curated store changes. `MarkerLayer.HubLines` is a toggle only: it is not in `PointMarkerLayer.Layers`.
  - `public enum DistanceUnit { Kilometres, Miles }`; `CrisisText.Distance(double km, DistanceUnit unit) -> string` ("812 km", "505 mi"; whole units, thousands separator; 1 mi = 1.609344 km).
  - `HubArcLayer.ArcPoints(SurfacePoint from, SurfacePoint to) -> IReadOnlyList<Point3D>`: `SphereLines.Densify` at 1° steps, lifted by `1 + h·sin(πt)` with `h = min(0.08, 0.25 × central angle in radians)`.

- [ ] **Step 1: Write the failing tests.**
  - `NearestHubFinderTests.AcrossDateLine` (from 17.7° S, 178.0° E: an airport at 179.9° W is nearer than one at 170.0° E), `.NearPole` (from 89.9° N, distances finite and ordered), `.TieBrokenByName`, `.OnePerKind`, `.EmptyKindOmitted`, `.DistanceMatchesGeoMath` (equals `GeoMath.DistanceKm` with 6,371 km).
  - `HubArcGeometryTests.EndsAtBothPoints`, `.DateLineArcIsContinuous` (no step longer than 1.5° along the path from 178° E to 178° W), `.LiftedInTheMiddle`.
  - `DistanceTextTests.KilometresAndMiles` (`Distance(812.4, Kilometres) == "812 km"`, `Distance(812.4, Miles) == "505 mi"`, `Distance(12345, Kilometres) == "12,345 km"`).
  - `HubPanelTests.RowsSayStraightLine` (SDN: `("Nearest airport", "<name> (<IATA>) · <d> km straight-line")`), `.NoApprovedDepotSaysSo` (`("Nearest UN depot", "No UN depot confirmed yet")`), `.UnitsSettingChangesRows`.
  - `SettingsViewModelTests.DistancesSaved`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. Arcs: a ribbon about 2 px wide (`Picking.WorldPerPixel`), each in its hub's colour; drawn only while the panel is open and `HubLines` is on; labels at each arc's midpoint `"<hub name> · <distance> straight-line"`, skipped when the midpoint faces away.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Nearest hubs with straight-line arcs and distances`.

### Task 6: Help catalog, vetting rules and the "Your one small step" section

**Files:**
- Create in `src/EarthRelief3D.Core/Help/`: `CountryProfile.cs`, `Charity.cs`, `PooledFund.cs`, `Senator.cs`, `HelpTables.cs` (`Load`, `Validate`), `VettingRules.cs`, `HelpCatalog.cs`, `HelpReview.cs` (renders `docs/help-review.md`)
- Create data: `data/countries/US/profile.json` (drafts; contact URLs from Task 1), `data/countries/US/charities.json` (`[]` until Task 8), `data/countries/US/senators.json` (DataTool verb `senators <senators_cfm.xml>`, with `asOf` = the XML's `last_updated`), `data/help/pooled-funds.json` (the 20 OCHA funds plus `cerf`, drafts)
- Modify: `App/Settings/AppSettings.cs` (`string UserCountry = "US"`, `string? UserState`), `SettingsViewModel` and `SettingsFlyout.xaml` ("Your country", "Your state (optional)"), `CountryPanelViewModel` (`PanelSection` gains `IReadOnlyList<HelpEntry>? Entries` and `string? ExplainerId`; the step section), `Views/CountryPanel.xaml` (an entries template), `AppServices` (load the help tables; on error log and use `HelpTables.Empty`), `Diagnostics/SnapshotScenes.cs` (`panelFocus` `"help"`)
- Test: `VettingRulesTests.cs`, `HelpTablesTests.cs`, `HelpPanelTests.cs`, `DataToolSenatorsTests.cs`, `SettingsViewModelTests.cs`

**Interfaces:**
- Produces (namespace `EarthRelief3D.Help`):
  - `public sealed record Evaluator(string Name, string Qualifies, Uri Site);` (US: Charity Navigator "3 stars or more"; BBB Wise Giving Alliance "meets all 20 Standards for Charity Accountability")
  - `public sealed record ContactRole(string Id, string Title, ContactKind Kind, Uri? Url, string Note, DocumentCitation Source, Review Review, DateOnly CheckedOn);` with `public enum ContactKind { Senators, Representative, Link, UnMission }`
  - `public sealed record CountryProfile(string Iso2, string Iso3, string Name, string Registry, Uri RegistryUrl, IReadOnlyList<Evaluator> Evaluators, IReadOnlyList<ContactRole> Contacts, DateOnly CheckedOn);`
  - `public sealed record CharityRating(string Evaluator, Uri ReportUrl, DateOnly CheckedOn, string CheckedBy);`
  - `public sealed record LegalCheck(DateOnly CheckedOn, string Pub78File, string DeductibilityCode);`
  - `public sealed record ProgrammeShare(double Share, int TaxYear, string ObjectId, DateOnly ComputedOn);`
  - `public sealed record CountryWork(string Iso3, Uri Evidence, string EvidenceTitle, string EvidenceKind, DateOnly CheckedOn);` (`EvidenceKind` `"charity's own page"` or `"OCHA 3W"`)
  - `public sealed record Charity(string Id, string Name, string Ein, Uri Website, Uri DonateUrl, LegalCheck? Legal, IReadOnlyList<CharityRating> Ratings, ProgrammeShare? Programme, IReadOnlyList<CountryWork> Countries, Review Review, DateOnly ReviewedOn, string Reviewer) { public IReadOnlyList<Uri> ProfileLinks { get; init; } public string? OwnerAction { get; init; } }`
  - `public sealed record PooledFund(string Id, string Name, IReadOnlyList<string> Iso3, Uri Page, Uri GiveUrl, string GiveNote, Review Review, DateOnly CheckedOn);`
  - `public sealed record Senator(string State, string Name, string Party, Uri ContactForm, Uri Website);`
  - `public sealed record HelpTables(CountryProfile? Profile, IReadOnlyList<Charity> Charities, IReadOnlyList<PooledFund> Funds, IReadOnlyList<Senator> Senators, DateOnly SenatorsAsOf) { public static HelpTables Empty; }`; `HelpTables.Load(string dataDir, string iso2) -> HelpTables`; `HelpTables.Validate(HelpTables) -> IReadOnlyList<string>`
  - `public sealed record VettingResult(bool Shown, IReadOnlyList<string> Reasons);` `public static class VettingRules { public const int ReviewValidMonths = 12; public static VettingResult Check(Charity c, string iso3, IReadOnlyList<Evaluator> evaluators, DateOnly today, bool showDrafts); }`
  - `public sealed record HelpEntry(string Group, string Title, IReadOnlyList<string> Lines, IReadOnlyList<PanelLink> Links);` (`Group` `"Give"` or `"Contact"`)
  - `HelpCatalog.Step(HelpTables t, string iso3, string countryName, DateOnly today, string? state, bool showDrafts) -> IReadOnlyList<HelpEntry>`; `CountryPanelViewModel.StepHeading = "Your one small step"`

- [ ] **Step 1: Write the failing tests.**
  - `VettingRulesTests`: `.ShownWhenAllChecksPass`; `.ExpiresTwelveMonthsAfterReview` (reviewed 2025-11-24: shown on 2026-11-23, hidden on 2026-11-24); `.DraftHidden` (shown with `showDrafts`); `.MissingLegalCheckHidden`; `.NoRatingHidden`; `.UnlistedEvaluatorHidden` (a rating from an evaluator the profile does not list); `.NotWorkingHereHidden`; `.EvidenceOlderThanTwelveMonthsHidden`. Each hidden result names its reason.
  - `HelpPanelTests`: `.FundFirstThenAlphabetical` (SDN: the Sudan Humanitarian Fund, then charities "Alpha", "beta", "Gamma" in that order, case-insensitive); `.CerfWhenNoFund` (ISL with a crisis fixture: CERF with `https://cerf.un.org/donate` and the P4-11 note); `.RegionalFundCoversItsCountries` (KEN gets the Eastern and Southern Africa fund); `.NoCharitiesSaysSo` (`"No vetted charities listed yet"`); `.CharityLines` (`"Tax-deductible (IRS Publication 78, checked <date>)"`, `"84% spent on programmes (Form 990, tax year 2024)"`, `"Rated by Charity Navigator, BBB Wise Giving Alliance"` with one link per rating report, `"Working here: <evidence title> (checked <date>)"`, `"Reviewed <date>"`, a `"Donate page"` link; **no** star, score or rating word from the evaluators); `.NoStateAsksForState` (`"Choose your state in Settings to list your two senators"`); `.StateShowsTwoSenators`; `.StateChangeRebuildsPanel`; `.UnMissionIsTheBlueBook`; `.EveryLinkIsHttps` (every `PanelLink` in every country's step section); `.NoCrisisNoStep` (ISL without a crisis or appeal: no step section); `.FooterRows` (`"Why these?"` explainer id `"why-these"`; `"List last reviewed <date>"`).
  - `HelpTablesTests.EveryRowIsCited`, `.FundsCoverOchaList` (20 funds plus `cerf`), `.ReviewDocumentIsCurrent` (as `CuratedTablesTests`, with `ER3D_WRITE_REVIEW=1`), `.RealFilesLoad`.
  - `DataToolSenatorsTests.ReadsExcerpt` (6 senators, contact forms are https, `asOf` = `last_updated`).
  - `SettingsViewModelTests.CountryListsOnlyProfiles` (United States only), `.StateSaved`, `.StateDefaultsToNotChosen`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. The step section follows the Getting help in and funding sections (P4-12). Draft rows show only with `--show-drafts`, labelled "Draft: not yet reviewed". Fill `pooled-funds.json` from Task 1's list (regional funds' countries from the CBPF `Poolfund` sub-funds), and `profile.json` (contacts: senators, the House finder, the State Department page Task 1 found, the UN Blue Book).
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Your one small step: help catalog, vetting rules, panel section`.

### Task 7: The vetting tool

**Files:**
- Create: `src/EarthRelief3D.Vetting/EarthRelief3D.Vetting.csproj` (net10.0 Exe, references Core; added to the solution; the test project references it), `Program.cs`, `Pub78Reader.cs`, `RevocationReader.cs`, `Form990Index.cs`, `Form990Reader.cs`, `ZipRangeReader.cs`, `LinkChecker.cs`, `VettingReport.cs`
- Modify: `.gitignore` (`.downloads/` is already ignored; add `artifacts/vetting/` if not covered by `artifacts/`)
- Create samples: `tests/Samples/pub78_excerpt.txt`, `revocation_excerpt.txt`, `index_2026_excerpt.csv`, `irs990_sample.xml` (one real return, public domain)
- Test: `VettingToolTests.cs`

**Interfaces:**
- Produces: `dotnet run --project src/EarthRelief3D.Vetting -- check [--today yyyy-MM-dd] [--offline <dir>] [--out <file>]` (default out `artifacts/vetting/vetting-report-<date>.md`; downloads cached in `.downloads/irs/`); exit codes 0 ok, 1 error, 2 usage. Library: `Pub78Reader.Read(Stream zip) -> IReadOnlyDictionary<string, Pub78Entry>` with `record Pub78Entry(string Ein, string Name, string City, string State, string Country, string DeductibilityCode)`; `RevocationReader.Read(Stream zip) -> IReadOnlySet<string>`; `Form990Index.Latest(Stream csv, string ein) -> Form990Ref?` with `record Form990Ref(string Ein, string TaxPeriod, string ReturnType, string ObjectId, string BatchId)`; `Form990Reader.ProgrammeShare(XDocument doc) -> (long Programme, long Total, int TaxYear)?` (`TotalFunctionalExpensesGrp/ProgramServicesAmt` and `/TotalAmt`; `null` for 990EZ/990PF); `ZipRangeReader.ReadEntryAsync(FeedHttp http, Uri zip, string entryName, CancellationToken ct) -> byte[]` (HTTP range: end-of-central-directory, central directory, one entry; a full cached download if the server answers 200 to a range request); `LinkChecker.CheckAsync(Uri) -> LinkStatus` with `enum LinkStatus { Ok, Blocked, Broken }`; `VettingReport.Render(...) -> string`.

- [ ] **Step 1: Write the failing tests.**
  - `VettingToolTests.Pub78Listed`, `.RevokedIsFlagged`, `.ProgrammeShareFromPartIX` (the sample: share = programme ÷ total, tax year from `TaxPeriod`), `.Ez990HasNoShare`, `.LatestReturnChosen` (index excerpt with two periods), `.ZipRangeReadsOneEntry` (an in-memory zip behind a handler that honours `Range`), `.ZipRangeFallsBackToFullDownload` (handler ignores `Range`), `.ExpiringWithinSixtyDays` (reviewed 2026-01-10, today 2026-11-24: listed), `.BlockedLinkIsNotBroken` (403 → `Blocked`, "check in a browser"), `.EvaluatorLinksNotFetched` (no request to charitynavigator.org or give.org; the report says "not checked by the tool (evaluator's terms)"), `.NeverEditsTheList` (SHA-256 of every file under `data/countries/` unchanged after a run).
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. The report lists per charity: Pub 78 (listed, name as the IRS has it, code), revocation, latest 990 and programme share with tax year and object id, review expiry, and each evidence and donate link's status.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings. Run the tool once for real on the empty list (it downloads the IRS files) and check the report says "0 charities".
- [ ] **Step 5:** Commit `Vetting tool: IRS checks, programme share, expiry and links`.

### Task 8: The first batch of charity drafts

**Files:**
- Modify: `data/countries/US/charities.json`, `docs/help-review.md` (regenerated)
- Working files in `.downloads/hdx/` (the 3W CSV) and `.downloads/irs/` (from Task 7), never committed

- [ ] **Step 1:** Download the 3W CSV (sources table) into `.downloads/hdx/`. Apply P4-9: list organisations by the number of countries with a reference period ending within 12 months of today; take those in 5 or more countries (raise the count until 15 or fewer remain; record each step).
- [ ] **Step 2:** For each, find its US public charity (itself or its US affiliate: the affiliate's own site must say so) in Publication 78 with code `PC`; drop those with none (record why).
- [ ] **Step 3:** For each kept charity, for each 3W country that has a current crisis: find the charity's own programme page for that country (its own site); record it as `CountryWork` (kind `"charity's own page"`) with the date checked; record the 3W row as a second `CountryWork` (kind `"OCHA 3W"`, the HDX dataset page). A country with only one of the two is left out (record it).
- [ ] **Step 4:** Fill `Website`, `DonateUrl` (the charity's own donate page), `ProfileLinks` (`https://www.charitynavigator.org/ein/<EIN without the dash>` and a give.org search link), `Ratings` empty, `OwnerAction` = "Open both profile links; say whether it is rated 3★ or more by Charity Navigator or meets all 20 BBB standards", `Review` draft. **Do not open the evaluator pages** (P4-8).
- [ ] **Step 5:** Run the vetting tool; copy each charity's `LegalCheck` and `ProgrammeShare` from the report into the JSON. Regenerate `docs/help-review.md` (`ER3D_WRITE_REVIEW=1`). Run all tests: all pass. Commit `First batch of charity drafts (P4-9)`.

### Task 9: Explainers

**Files:**
- Create: `data/curated/explainers.json` (drafts), `src/EarthRelief3D.Core/Curated/Explainer.cs`, `src/EarthRelief3D.App/ViewModels/ExplainerViewModel.cs`, `src/EarthRelief3D.App/Views/ExplainerCard.xaml(.cs)`
- Modify: `CuratedTables` (`Explainers`, file `explainers.json`, `Validate`), `CuratedReview` (an "Explainers" section), `CountryPanelViewModel` (each crisis section and the Getting help in section set `ExplainerId`), `Views/CountryPanel.xaml` (a "What is this, and why does it happen?" link per section with an approved explainer; the footer "Why these?"), `MainWindow.xaml` (the card to the left of the panel, 420 px wide; Esc closes it first)
- Test: `ExplainerTests.cs`

**Interfaces:**
- Produces: `public sealed record Explainer(string Id, string Title, IReadOnlyList<string> Paragraphs, IReadOnlyList<DocumentCitation> Sources, Review Review, DateOnly ReviewedOn);` ids `conflict, atrocities, camps, food, outbreaks, displacement, access, why-these`; `ExplainerViewModel.Open(string id)`, `Close`, `IsOpen`, `Title`, `Paragraphs`, `Sources` (links), `ReviewedText` ("Reviewed <date>").

- [ ] **Step 1: Write the failing tests.**
  - `ExplainerTests.EveryExplainerCited` (at least 2 sources each, https, dated, from the authority's own site), `.PlainAndShort` (300 words or fewer; no quotation longer than 15 words), `.AllEightIds`, `.SectionLinksItsExplainer` (SDN's food section: `ExplainerId == "food"`), `.DraftHasNoLink` (a draft explainer: no link unless drafts are shown), `.OpensAndCloses`, `.WhyTheseExplainsTheThreeChecks` (mentions Publication 78, the two evaluators and evidence of work, and the caution about programme shares from §9.3).
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Draft the eight texts in plain language from the cited sources (ICRC/UN for conflict, the Genocide Convention and UN bodies for atrocities, UN COI for camps, the IPC manual for food, WHO for outbreaks, UNHCR for displacement, ACAPS's methodology for access, the IRS and the evaluators' public pages for why-these), paraphrased, never copied. Implement the card and links.
- [ ] **Step 4:** Regenerate `docs/curated-review.md`; run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Explainers: one per crisis kind, plus access and why these`.

### Task 10: Samples, snapshots, docs and final check

**Files:**
- Modify: `tests/SnapshotScenes/scenes.json` (add `access`: shading access, look at 15,30; `sudan-help`: panel SDN, `panelFocus` `help`, layers access, un-depots, airports, ports and hub lines, look at 15,35, `showDrafts` true; `sudan-access`: panel SDN, `panelFocus` `access`), `docs/data-sources.md`, the spec's rulings section (a "Rulings made while building Plan 4" table: P4-1 to P4-15 plus the ledger's R numbers), `README.md` (what the app now shows)
- Create: `docs/plan-4-results.md`

- [ ] **Step 1:** Run all tests: all pass, 0 warnings; record the count.
- [ ] **Step 2:** One app launch: `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-plan4`. Look at every picture; note what each shows (the brown access shading and its legend; Sudan's arcs and labels with "straight-line"; the help section's fund, "No vetted charities listed yet" or the drafts, contacts and footer; the access section's nine rows). The earlier scenes now also show the owner-approved atrocity and camp rows: compare with `artifacts\snapshot-baseline` with PngDiff and explain every difference.
- [ ] **Step 3:** Tune what reads badly (at most one re-run), then the repeat run into `artifacts\snapshot-plan4-b`: PngDiff must report every scene identical.
- [ ] **Step 4:** Write `docs/plan-4-results.md` (tests, launches, what each picture shows, what was tuned, owner items). Do not copy over the baseline pictures (that needs the owner's OK). Commit `Plan 4: snapshots, docs and results`.
