# EarthRelief3D and GlobeCore — Design

**Date:** 2026-11-21
**Status:** Approved by the owner 2026-11-21

This document covers two projects, built in order:

1. **GlobeCore**: a shared, body-agnostic 3D globe library taken out of EarthWatch3D, with EarthWatch3D switched over
   to it. It is meant to be reused later by apps about the Moon, the Sun and the planets.
2. **EarthRelief3D**: a new Windows app built on GlobeCore that shows where people are suffering, what they lack, where
   resources are, how hard it is to get help in, and what one person can do ("Your one small step").

---

## 1. Purpose

EarthRelief3D is a Windows desktop app (C#, WPF) that shows NASA's Blue Marble Earth in 3D, which you can rotate and
zoom. On it are:

- **Crises:** armed conflict, genocides and mass atrocities, mass incarceration and prison camps, famine and food
  crises, disease outbreaks, and people forced from their homes.
- **Basic needs:** food, clean water, sanitation, shelter and health care. Each is shown two ways: where people lack it,
  and where supply sits.
- **Industrial resources:** oil and gas, power plants, steel, and fresh water.
- **Getting help in:** how hard it is for aid to reach each crisis, and lines to the nearest port, airport and UN
  humanitarian depot.
- **Your one small step:** for each crisis, vetted ways to give money and official people to contact.

The goal is to **educate** people about these crises and show **how an individual or group can act to help**. It is for
the general public, starting with people in the United States, installing a packaged app.

### Success criteria

- Someone who knows little about these crises can, within a few minutes, see where the worst suffering is, understand
  why help isn't getting there, and leave with at least one verified thing they can do.
- Every layer and every number on screen shows its **source** and an **"as of" date**.
- Every charity shown has passed all three checks (§9.2) and was reviewed within the last 12 months.
- Every genocide or atrocity label names the authority that made it, what kind of authority it is, and the date.
- Nothing important depends on sound (the owner is hard of hearing; this is a rule for all of the owner's apps).
- The app starts and shows cached data with no network, and recovers by itself when the network returns.
- It installs, updates itself and uninstalls cleanly as a signed MSIX, like EarthWatch3D.

### Decisions made during brainstorming

| Topic | Decision |
|---|---|
| Platform | C#, WPF, .NET 10, HelixToolkit.Wpf (the owner's request, and what EarthWatch3D uses) |
| Build approach | A new app on a new shared globe library (GlobeCore) taken out of EarthWatch3D, which switches to it first. Chosen over adding layers to EarthWatch3D (blurs two apps) and copying the code (two copies drift) |
| GlobeCore scope | Body-agnostic: Earth, Moon, Sun and planets are data, not code. Bodies are ellipsoids; rotation is a plug-in; layers are placed by latitude and longitude on any body |
| Transport difficulty | Option 3, "both, kept simple": an access score per crisis plus straight-line links to the nearest major port, airport and UN humanitarian depot. No route modelling |
| Basic needs | Added by the owner: food, clean water, sanitation, shelter, health care (§7) |
| Help | Split into **giving money** and **contacting decision-makers** (owner). Charities must be legally able to take donations, independently rated, and actually working in that crisis (§9) |
| Country | **US first**, but country rules live in data files, so adding a country needs no code |
| Name | **EarthRelief3D**; the help panel is headed **"Your one small step"** (owner, after Armstrong). CrisisWatch3D (International Crisis Group's CrisisWatch), WorldHope3D (World Hope International), OneSmallStep (StoryCorps) and ReliefGlobe3D (physical relief globes) were set aside after a quick web check. No trademark search has been done |
| Time | No replay. Each crisis shows its own history (when it began, getting better or worse) |
| Teaching | A short plain-language explainer per kind of crisis, with sources |
| Anti-goals | The app never handles money, never ranks by its own opinion, shows no graphic images, sounds no alarms, collects no personal data and never depends on sound |
| Shipping | Public GitHub repo and MSIX installer, like EarthWatch3D |
| Charity ratings (Q1) | The reviewer includes a charity only if it has a qualifying rating (Charity Navigator 3★ or higher, or meets all 20 BBB Wise Giving Alliance standards). The app says which evaluators rate it and links to their reports, but does not copy the ratings (their licences forbid it). The programme-spending share is computed from the charity's own IRS Form 990 (public domain) and shown with its tax year |
| Atrocity designations (Q2) | Shown on the map when made by **international courts** or **UN commissions and bodies**, plus **government formal determinations from the countries the user chooses** (a Settings option, defaulting to the user's own country; more can be added) |
| Disputed territories (Q3) | Clickable country units follow the units the UN humanitarian sources use, and the map carries the standard UN note that boundaries and names shown do not imply official endorsement or acceptance |

### Calls made while writing this document (for the owner to check)

| # | Call | Reason |
|---|---|---|
| C1 | The details panel is **per country**, with a section per crisis. Clicking a crisis marker opens its country's panel at that crisis | Help, access, needs and contacts are all published per country; a per-crisis panel would repeat them |
| C2 | Earth stays a **sphere of mean radius 6371 km** in GlobeCore's Earth pack | The WGS84 flattening (21 km) is invisible at globe scale, and keeping the sphere lets EarthWatch3D's switch-over be checked pixel for pixel. Bodies where flattening shows (Jupiter, Saturn) are ellipsoids |
| C3 | Distances to hubs are **straight-line (great-circle)** and labelled "straight-line distance" | Road and sea distances would be invented numbers (the owner chose "kept simple") |
| C4 | **UCDP** for conflict, not ACLED | ACLED's licence forbids redistribution and requires "transformative" outputs; current ACLED data needs a paid licence. UCDP is CC BY 4.0 |
| C5 | The vetted charity list **ships with the app** and updates arrive with app updates | No live source vets charities; MSIX updates already arrive by themselves |
| C6 | Data under non-commercial or share-alike licences is **fetched at run time**, never committed to the repo | Keeps the repo's own licence clean |
| C7 | Severity shown on markers uses each source's **own scale** (for example IPC Phase 1–5); a common 1–5 band is used only for marker size and colour, with the mapping documented in About | Inventing a cross-source "suffering score" would be the app's own opinion |
| C8 | Text size can be made larger in Settings, and colours are colour-blind safe with shape as a second cue | The owner and many users are older; colour alone fails some viewers |
| C14 | Under Q2: NGO alerts and risk lists (Genocide Watch, the USHMM Early Warning Project) appear only as **labelled links** ("alert", "risk"), not as map designations; **pending court cases** appear labelled "pending: no finding yet"; provisional orders and arrest warrants are labelled as such, never as verdicts; **past genocides** (for example Rwanda, Srebrenica) appear in explainers and the panel history, and on the map only when the designating body or a current UN report (last 12 months) says the situation continues | Extends the owner's rule to the cases the rule doesn't name, without adding the app's own judgment |
| C15 | The "government determinations from" list in Settings offers **only governments whose determinations have been curated completely** (every formal determination that government has made, each cited). The US is curated for v1; others are added one government at a time | A half-curated government would mislead by omission |
| C16 | Country polygons: Natural Earth admin-0 (default view), regrouped where needed to match the UN humanitarian units used by HAPI and OCHA (for example Somaliland data counted under Somalia, as the sources do); a click opens the panel of the country the data is reported under | Follows Q3; the regrouping list lives in the app's rules file, `data/boundary-rules.json` (a `mergeUnits` rule, SOL into SOM), with the reason for each, applied through GlobeCore's `BoundaryRules` to every boundary set that is loaded. Superseded by Plan 2b: it was first planned as `data/curated/country-units.json` |
| C10 | **Curated content is drafted by Claude with citations and reviewed by the owner**, who signs off each charity entry and each designation. v1 covers the countries that meet a source-defined severity threshold (C11) in at least one crisis kind; a country without vetted entries yet shows its UN pooled fund (or the global CERF) and "No vetted charities listed yet" | Research is the biggest cost of Plans 3–4; this bounds it and keeps the owner as the final check |
| C11 | **A "crisis" (marker and panel section) exists only where a source's own threshold says so:** UCDP active conflict (25+ battle-related deaths in a year); IPC Phase 3 or higher; any WHO Disease Outbreak News report in the last 90 days (WHO already publishes only significant events); a curated designation or documented camp. Incarceration rates and displacement numbers are **layer values** shown for every country (and in the panel with their trend), not crisis markers | Keeps the map about the worst suffering without the app inventing thresholds |
| C12 | **No personal identifier or secret token ships in the app or the repo.** The HAPI app identifier uses a dedicated project contact address the owner creates. A source that needs a personal secret token is refreshed by the maintainer and bundled, not fetched live | The repo and MSIX are public |
| C13 | **Permission-free sources first.** World Prison Brief, ASPI and the USHMM risk list are add-ons, each behind a data switch, turned on only when written permission arrives; until then detention uses UNODC data if its licence allows (spike) and curated sites, and the risk list is a link | Plans don't wait on replies that may never come |
| C9 | A correction to the brainstorming conversation: there are **five** UN Humanitarian Response Depots now (Accra, Brindisi, Dubai, Kuala Lumpur/Shah Alam, Panama City). Las Palmas is no longer listed (unhrd.org, checked 2026-11-21) | Earlier conversation said six |

### Rulings made while building Plan 1 (for the owner to check)

| # | Ruling | Reason |
|---|---|---|
| P1-1 | The Sun's rotation is an `IauRotationModel` with the IAU Sun elements, not a separate `CarringtonRotation` class | One model type covers every body; the IAU Sun elements give the Sun's rotation directly |
| P1-2 | Rotation models exist only for the Sun, the Moon, the Earth and Mars until an app needs more | No app needs the others yet; adding one is a new `IauRotationModel` from the IAU elements |
| P1-3 | `CountryIndex` moves to Plan 2 | EarthWatch3D does not need it, and Plan 1 changes no behaviour |
| P1-4 | These stay in EarthWatch3D until a second app needs them: the Sun and Moon markers and arrows, `MoonPhaseGlyph`, `TimelineViewModel`, `SunMoonViewModel`, the status-dot view, the base `LaunchOptions` parser and `ISurfaceImagery` (§3.3, §3.5). Plan 2 moves the status dots and the launch-option parser, which EarthRelief3D needs | Moving code only one app uses adds risk and no benefit |
| P1-5 | The Moon's rotation uses the IAU 2009 E1–E13 series (from NAIF `pck00011`), because the 2015 report has no Moon series. Worst errors against Horizons: Moon 0.012°, Mars 0.029°, Sun 0.004° (limit 0.05°) | The 2015 report defers the Moon to the 2009 series |
| P1-6 | Snapshot scenes freeze the clocks (display clock, quake-ring phase, feed clock). The measured noise floor between two runs is 0 pixels, so parity is checked at `--tolerance 0` | An unfrozen baseline differed by up to 1,119 pixels, which would hide real changes |
| P1-7 | GlobeCore has no `LICENSE` file yet | The licence is the owner's choice. **Update 2026-11-22:** the owner chose MIT (Copyright (c) 2026 John M. Apolloni) for GlobeCore, AstroCore and EarthWatch3D; pull requests AstroCore#1, GlobeCore#1, EarthWatch3D#8 |
| P1-8 | `ISettingsStore<T>` and `SettingsSaver<T>` are generic; `AtomicFile` is public | The originals were typed to Earth's `AppSettings`; the app's code needs `AtomicFile` too (wider public surface instead of `InternalsVisibleTo`) |
| P1-9 | `LayerHost` and the layer classes gained members the plan did not list (`DisplayTimeUtc`, `IlluminationAt`, `RecomposeIfStale`, `RequestSceneUpdate`, `ComposedUtc`, `WhenFirstComposeDisplayed`, `SelectionCleared`, `CameraPoseChanged`, and layer hooks such as `InvalidateNow`, `PrepareForComposeAsync`, `OnComposeDisplayed`, `OnFrame`, `Clear`) | Needed to carry today's behaviour; nothing briefed was removed |
| P1-10 | Layers may be invalidated from any thread: the host marshals to the UI thread (interlocked flag plus `BeginInvoke`, as `SceneController` does), and its public compose methods check the calling thread | The feeds that drive the layers run on background threads |
| P1-11 | GlobeCore's `Directory.Build.props` imports the nearest parent `Directory.Build.props`, so the app's `AstroCoreRoot` wins. The app's other properties (version, short-path block) also apply to GlobeCore projects in the app's build | The plan's version shadowed the app's props, so `AstroCoreRoot` never reached GlobeCore. Standalone, any props file above the repo is imported too |
| P1-12 | ~~`EarthSphere.RadiusKm` is repeated in `EarthWatch3D.Core`; a test pins it to `EarthBody.RadiusKm`~~ Superseded by P1-22: the Earth pack has no UI now, `EarthWatch3D.Core` references it, and `EarthSphere` (with its pinning test) is gone | Core could not reference the WPF Earth pack |
| P1-13 | Accepted small differences: the light is read before the plate image is awaited (corrects itself within 250 ms); `--snapshot` waits up to 250 ms longer. (The `PERF_LOG` compose lines first went to `%TEMP%\GlobeCore-perf.log`; P1-19 puts them back in `%TEMP%\EarthWatch3D-perf.log`) | None is visible to a user |
| P1-14 | **For the owner decided (D1): accepted 2026-11-22.** events, markers and hover now appear when the globe image appears (about 1 s after start) instead of over the black viewport while the image decodes. Also: if the globe image fails to load, no layer host is built, so events, markers, labels and arrows never appear (before, they were shown over the black viewport); the error message box still appears. Recommended: accept | The layer host starts the layers once the first image is composed; accepted by the owner on 2026-11-22 |
| P1-15 | (R12) The idle note (the tooltip text on a feed that is not being polled, which is app wording) is passed to `FeedScheduler`'s constructor, which puts it in the `FeedStatus` values it creates, instead of being a `FeedStatus` constructor parameter | Same effect; the scheduler creates the statuses |
| P1-16 | (R15) `GlobeImageComposerPerfTests` (the Debug timing test that already failed before Plan 1) stays in EarthWatch3D and exercises the moved composer from there | Copying it would add a known-failing test to GlobeCore; perf coverage lives in the app repo |
| P1-17 | (R16) `IDialogService` and `DialogService` stay in EarthWatch3D, against Appendix A's move list | They depend on the app's own windows and view models |
| P1-18 | (R22) GlobeCore's geo and rasterizer APIs keep `AstroCore.Earth.GeoPosition` for Plan 1 (as the plan wrote them); whether to use `SurfacePoint` everywhere (§3.2) is decided in Plan 2, before EarthRelief3D builds on them | Changing them now would touch every Earth call for no behaviour gain; cost: an API migration later |
| P1-19 | (R23) GlobeCore.Wpf's `PerfLog` is public (in `PERF_LOG` builds); EarthWatch3D calls `PerfLog.Configure("EarthWatch3D")` at start-up and writes its own lines through it, so the compose timings are back in `%TEMP%\EarthWatch3D-perf.log` with the rest, one file and one writer (supersedes the `GlobeCore-perf.log` item of P1-13) | Restores the pre-Plan 1 log |
| P1-20 | Final review F1: `BodyMesh` puts each vertex on the radial line of its row's planetocentric latitude, at the ellipsoid's radius (`Ellipsoid.ModelPoint` / `SurfacePointOf`); before, a flattened mesh used the parametric latitude, so on Mars texture and picking disagreed by up to 0.17°. A sphere's mesh is unchanged to the bit | Latitudes are planetocentric everywhere (picking, markers, composer) |
| P1-21 | Final review F2: the twilight comes from the body: `IlluminationState.TwilightDeg` (default −12°) is used by the composer, and `Body.IlluminationAt(utc, nightStrength, drawTerminator)` builds the light from the body's sub-solar point and `SunLit` twilight. The layer host still has no light of its own: the app sets `IlluminationAt` (EarthWatch3D keeps its own, with the default −12°) | `SunLit(TwilightDeg)` was declared but never used; −12° composes exactly as before |
| P1-22 | Final review F3: the Earth pack (`GlobeCore.Earth`) is `net10.0` with no UI, so no-UI projects (`EarthWatch3D.Core`, a future `EarthRelief3D.Core`) can use it; `EarthAssets.LoadBlueMarble` is gone and the WPF side decodes with `ImageConversions.DecodeToBgra32(EarthAssets.Open(EarthAssets.BlueMarbleFile), width)`. `EarthSphere` is removed from EarthWatch3D (`EarthBody.RadiusKm` instead) | Only the Blue Marble decode needed WPF; a WPF-side helper in GlobeCore.Wpf would have made the body-neutral library depend on the Earth pack |

### Rulings made while building Plan 2 (for the owner to check)

P2-1 to P2-8 were made while writing the Plan 2 plan; R1 to R16 while building it (the SDD ledger); R17 records the
owner's decision on R16's question (2026-11-23).

| # | Ruling | Reason | Cost |
|---|---|---|---|
| P2-1 | Lat/lon stays `AstroCore.Earth.GeoPosition` in apps and GlobeCore's Geo, rasterizer and feed APIs; `SurfacePoint` only on the body-generic API (settles P1-18) | Both are east-positive; migrating every signature is churn with no behaviour gain | Two types that convert implicitly |
| P2-2 | Plan 2 sources need no key or account: UCDP Candidate CSV downloads, IPC national figures from HDX, WHO Disease Outbreak News, the UNHCR population API; HDX HAPI waits | Checked live 2026-11-22; nothing waits on the owner | IPC areas and HAPI later |
| P2-3 | Food crises are shown per country, shaded and outlined by the rule in R17 (first built as "the IPC colour of the highest phase with people in it", replaced by R17 on 2026-11-23); IPC area polygons move to Plan 3 | The national file has no areas; areas need an IPC key or OCHA boundaries | Coarser than §6's IPC areas (see R17) |
| P2-4 | An IPC analysis whose current period ended more than 12 months ago is not a crisis; the panel lists it as the last analysis | An old analysis is not evidence of a current crisis (C11) | None |
| P2-5 | The WHO test sample is synthetic (same shape, invented text); no WHO text is committed | C6: CC BY-NC-SA data is never committed | Tests do not use real titles |
| P2-6 | GlobeCore.Wpf gets a generic status-dots view; EarthWatch3D switches to it in a later small plan | Keeps this plan off a shipped app | A temporary duplicate |
| P2-7 | EarthRelief3D is a local git repo only, MIT, version 0.1.0 | Nothing is pushed without the owner's OK | None |
| P2-8 | UCDP window: the newest quarterly cumulative Candidate file plus every later monthly file (about 9 to 12 months); trend = last 3 complete months against the 3 before | The downloads page carries only the current files; older releases need the yearly GED (hundreds of MB) | §6's "last 12 months" is really this 9-to-12-month window |
| R1 | The controller ran `git init` and the `.gitignore` before Task 1, so the SDD workspace could exist | Workspace needed first | None |
| R2 | GlobeCore's push, pull request and re-pin (Task 2 Step 5) run at the end with the owner | The plan says so | None |
| R3 | `data/*.json` live at the repo root and are linked into Core's, the app's and the tests' outputs | One copy | None |
| R4 | A launch option `--open-panel <ISO3>` (and the scenes' `panel` field) opens a country panel for the snapshot scenes | The sudan-panel and no-crisis-panel scenes need it | One more option |
| R5 | The GlobeCore task does not touch EarthRelief3D; the controller pins the submodule | Avoids two writers in one repo | One controller commit |
| R6 | Tasks 2 (GlobeCore) and 3 (Core model) ran in parallel | Different repos, no shared types | None |
| R7 | GlobeCore's `LoadNaturalEarth` carries a small built-in alias list (Türkiye, Czechia, Cabo Verde, Timor-Leste, Burma, Ivory Coast, Swaziland) | Official renamings Natural Earth lacks; apps add more | List maintenance in GlobeCore |
| R8 | Conflict bands 1 to 4 (boundaries 25, 1,000, 10,000, 50,000); band 5 is not used for conflict | The plan's four boundaries give four bands; bands only size markers | None |
| R9 | The UCDP 25-deaths-a-year threshold is on the whole conflict (all countries); one crisis per country it has events in | C11 is about the conflict | A country's small share still gets a marker (band 1 at least) |
| R10 | UNHCR as-of = 30 June of the data year ("Mid-year"), or 31 December ("End-year") once the clock is past 1 July of the next year; never the clock | The API has no date; UNHCR's release pattern | The label may lag a release by weeks |
| R11 | Food shows "IPC analysis period <from>–<to> (analysed <date>)", and the legend's date is the newest Date of analysis | The period end is often in the future: never "as of" a future date | None |
| R12 | Each feed records when it last ran successfully (also with nothing found); "not yet checked" vs "none recorded · checked <date>" | "No crisis" must not be said for a source never checked | A Core model change and a cache schema bump |
| R13 | Task 7's own review was folded into the whole-branch final review | Same ground | One review fewer |
| R14 | ~~A UCDP event belongs to the country containing its coordinates; UCDP's country name only when the point is in no country~~ Replaced by R14b | The 1:50m outlines put 41 of 4,494 sample events in a neighbour's panel (Sumy oblast in Russia, Goma in Rwanda, ...) | Withdrawn |
| R14b | A UCDP event belongs to the country UCDP names (through the aliases). Its coordinates override that only when the point is in a territory UCDP does not list (`data/ucdp-territories.json`, starting with PSE: Gaza and the West Bank are coded Israel) or when the name is unknown; an unknown name at sea is listed as "Not placed on the map" | UCDP's coding is reliable near borders, where the 1:50m outlines are not; the territory list keeps Palestine separate (owner's Q3) | The territory list is kept by hand (one entry so far) |
| R15 | Crimea and Sevastopol are part of Ukraine (owner's Q3: UN practice; UN GA resolution 68/262): the app's rules file (`data/boundary-rules.json`) moves Natural Earth's own admin-1 polygons from RUS to UKR, and drops the Perekop border line, as GlobeCore's `BoundaryRules` loads each boundary set | Natural Earth's point-of-view files are 1:10m only and change many other countries | Superseded by Plan 2b: first built as an asset and a fix-up in GlobeCore; now a `moveAdmin1` rule (UA-43, UA-40 from RUS to UKR, the Perekop line removed) in the app's rules file, `data/boundary-rules.json`, applied through GlobeCore's `BoundaryRules` at every load. EarthWatch3D has the same rule in its own rules file (Plan 2b, B-7); GlobeCore applies no rules of its own |
| R16 | The food panel and tooltip show the people in Phases 3, 4 and 5 ("Phase 5 Catastrophe/Famine"); the shading rule itself is the owner's question (recommended: IPC's 20% area rule). **Decided by the owner on 2026-11-23: see R17** | A small Phase 5 count must not read as a country in famine | None (the decision's cost is under R17) |
| R17 | (Owner's decision, 2026-11-23.) Each country is shaded by IPC's 20% rule: the worst phase p from 5 to 3 such that the people in Phase p or higher are at least 20% of the people analysed (exact counts, never the rounded percentages; one denominator D for the rule, the figures and the bar: "Population analyzed current" when it is at least the sum of the Phase 1 to 5 counts, else that sum (when larger, or when the people analysed is blank; the panel says which, and a sum up to 1% over is named as rounding: "of 47,535,794 people analysed (the phase numbers add up to 30 more)"), else the share is unknown and nothing is shaded); Phases 1 and 2 are never shaded. The country is outlined (a band about 2 px wide just inside its edge, narrower for a small country so some fill shows, opaque, none along the ±180° split) in the colour of the worst phase that has people in it when that is worse than the shading; a country with that outline but no shaded phase also gets a faint tint of the same colour (about 18% opacity, a third of a shaded fill; owner's decision 2026-11-23, round 2), and a shaded country never gets one; one record per country, the latest (as in the panel). The panel's "Country shading" row names the rule, and a phase bar shows D stacked to 100% (the people in no reported phase in grey, then Phases 1 to 5) with a "worst 20%" marker that always falls in the rule's phase, a caption, a row per phase (also for a past analysis), and the famine note when shaded Phase 5. The tooltip names the shaded phase, then the worst phase with people (or "faint Phase N tint and outline: some people are in Phase N"). The legend describes the rule, has an outline key and a faint-tint key, and notes that a country shaded Phase 5 is not an IPC famine classification | IPC's own area rule; a small Phase 5 count shows as an outline, not as a country in famine; the bar shows how the rule was applied | Our country-wide use of an area rule (IPC applies it to areas, and IPC area maps come in Plan 3); with the sample, 29 of 36 current analyses have only the faint tint and outline. One function (`IpcAreaRule`) serves the map, tooltip and panel. A cache schema bump (4) |

Notes on the spec: §6's "last 12 months" for conflict is the UCDP window of about 9 to 12 months (P2-8). P1-4 said Plan 2
would move the launch-option parser to GlobeCore; it stayed in the app (`src/EarthRelief3D.App/Diagnostics/LaunchOptions.cs`),
and only the status dots moved (P2-6).

Plan 2b (updatable boundaries, built 2026-11-22) has its own rulings, B-1 to B-9 and R1 to R9, in
`docs/superpowers/plans/2026-11-22-plan-2b-boundaries.md`; they refine C16 and R15 above.

### Rulings made while building Plan 3 (for the owner to check)

P3-1 to P3-11 were made while writing the Plan 3 plan (`docs/superpowers/plans/2026-11-23-plan-3-atrocities-needs-resources.md`).
R1 to R20, R22 to R25, R14b, R17b and R24b were made while building it (its SDD ledger); **these R numbers are Plan 3's own**, not Plan 2's
R1 to R17 above. Plan 3 has an R17b but no R17 of its own: where a Plan 3 row needs the food shading rule, it says
"Plan 2's R17". D1, T7-1 to T7-3, T8-1 and T8-2 are implementers' decisions. Task 7 (the GEM marker layers) waited for the
owner's GEM files (R18) and ran on 2026-11-24.

| # | Ruling | Reason | Cost |
|---|---|---|---|
| P3-1 | No source needs a key, an account or the owner's email. HAPI, the IPC API, the EIA API and the healthsites.io API are not used. HNO and IPC data come from HDX CKAN files, oil from EIA's bulk file | C12; the plan doesn't wait on the owner | Slightly older or coarser data than the keyed APIs |
| P3-2 | IPC areas come from HDX's per-country `ipc_<iso3>.geojson`. Each area is shaded in IPC's own area classification (`overall_phase` 3–5). For a country with current area data, the areas replace the country-wide shading of Plan 2's R17; elsewhere Plan 2's R17 stays. Areas are used only when they belong to the same analysis as the country's national record (see R4, R10 and R11); if they don't, the country keeps Plan 2's R17 and the log says why | P2-3 moved areas here. IPC classifies areas itself, so no rule of ours is needed for them | Countries without matching area data stay on Plan 2's R17 |
| P3-3 | Detention in Plan 3 is curated camp sites only. UNODC's terms forbid redistribution and derivative works. Permission requests (UNODC, ICPR World Prison Brief, ASPI, USHMM) are drafted in `docs/permission-requests.md` for the owner to send; each data source arrives in a later plan only after permission | C13 | No incarceration-rate layer until a reply comes |
| P3-4 | Curated rows are drafted by Claude and shown only after the owner approves them (C10). Each row has `review: { status: "draft" or "approved", approvedOn }`. Drafts appear only with the diagnostic `--show-drafts` (or a snapshot scene's `"showDrafts": true`, T8-1), labelled "Draft: not yet reviewed". `docs/curated-review.md` lists every draft with its citations for the owner | C10: the owner is the final check on designations | Until the owner reviews, the atrocity and camp layers show nothing in normal runs |
| P3-5 | Government determinations: the US is curated completely; no other government in Plan 3 (C15). Settings offers "Formal determinations by: United States government" (on by default). The list is driven by `data/curated/governments.json`, so adding a government needs no code | C15 | None (R13 says what "completely" covers) |
| P3-6 | One shading layer at a time across all groups: Food crisis, Water, Sanitation, Health care, Hunger, Shelter need, Oil production and Water stress. Turning one on turns the shown one off, and the strip shows the change. Marker layers (conflict, outbreaks, displacement, atrocities, camps, power plants, oil and gas, steel) stack | §6 | A user may miss that food shading went off; the legend says which shading is shown |
| P3-7 | Shading scales: a colour-blind-safe sequential scale per group, different from the IPC colours. Needs: ColorBrewer BuPu, 5 classes `#EDF8FB #B3CDE3 #8C96C6 #8856A7 #810F7C`. Resources: YlGnBu, 5 classes `#FFFFCC #A1DAB4 #41B6C4 #2C7FB8 #253494`. A country with no value is not filled, and the legend says "no figure: not shaded". Breaks: a source's own classes where it has them (FAO hunger-map classes for undernourishment; WRI's 0–5 categories for water stress), otherwise fixed breaks written in the legend | C7, C8; no invented severity | The breaks shape the impression; they are shown, never hidden |
| P3-8 | "Where supply sits" in Plan 3: food, dietary energy supply adequacy (FAOSTAT); water, water stress (Aqueduct); energy and industry, GEM markers (Task 7, deferred). Health facilities (healthsites.io) are not in Plan 3: the API needs a key, and the per-country HDX files are ODbL (live only, about 200 downloads). Shelter supply (UNHRD depots) is Plan 4 | §5.2 left healthsites to Plan 3 | The health-care "supply" view is missing in v1 |
| P3-9 | Marker layers are capped at 5,000 markers each, filtered at extract time, with the filter written in the dataset manifest and the legend: power, operating plants of 500 MW or more (units summed per plant); steel, operating plants of 1 Mt/yr or more; oil and gas, operating fields with coordinates. If a filter still leaves more than 5,000, its threshold is raised and recorded | Rendering budget; the filter is shown, not hidden | Small plants are not shown (when Task 7 runs) |
| P3-10 | A new `tools/EarthRelief3D.DataTool` (net10.0 console) makes every bundled extract from the maintainer's downloaded files: it trims to the columns used, normalises country codes to ISO3, and writes `data.csv` plus `manifest.json` (source, version, as-of, licence, attribution, the original file's SHA-256 and the command used). Original downloads are never committed | §10 | None |
| P3-11 | FAOSTAT area codes: M49 maps to ISO3 through Natural Earth's `UN_A3` field. FAO's "China, mainland" (FAO area code 41) maps to CHN; Hong Kong, Macao and Taiwan keep their own codes; FAO's "China" aggregate (FAO code 351) and the other aggregates (regions, income groups) are dropped. Any M49 code that doesn't map is listed in the manifest | Avoids double counting China | None |
| R1 | The IPC GeoJSON test sample was trimmed with a one-off script kept outside the repo (or by hand), not with the DataTool | The DataTool did not exist before Task 6 | None |
| R2 | A shading or marker toggle whose data is not loaded yet shows its legend line with "Data not loaded yet" and draws nothing | Keeps the layer strip final from Task 2 on | An interim build (or a missing dataset) shows empty layers |
| R3 | Hunger uses the FAO Hunger Map's five classes, lower bounds 0, 5, 15, 25 and 35 ("under 5%", "5–15%", "15–25%", "25–35%", "35% or more"), so every country with a figure is filled. A FAO value written "<2.5" is stored as 2.5 with the flag "<2.5" and shown as "under 2.5%" | P3-7: an unfilled country means "no figure" | The bottom class merges FAO's <2.5 and 2.5–5 |
| R4 | A country's IPC area set is used when its GeoJSON's areas with `ipc_period` C share one `anl_id`, and the same dataset's `ipc_<iso3>_national_long_latest.csv` gives that analysis a current period equal to the national record's From and To; otherwise the country keeps Plan 2's R17. One extra small CSV per country | The GeoJSON carries no dates | A few countries stay on Plan 2's R17 |
| R5 | The WHO GHO test samples are synthetic: the same JSON shape, 15 named countries, invented values, and a README note (as P2-5 for WHO's outbreak news). The app still fetches real GHO data live | The GHO page states no licence of its own; synthetic samples remove any doubt for a public repo | None for the tests; the offline pictures show invented water, sanitation and health figures |
| R6 | Aqueduct is bundled only if WRI's own data-licence page confirms CC BY 4.0; otherwise the water-stress layer is "link only". Task 6 found CC BY 4.0 on WRI's Open Data Commitment page, so it is bundled | The Aqueduct Read Me states no licence | A layer missing until confirmed |
| R7 | HNO cluster rows exist for 11 countries only (Sudan and Yemen have only the ALL row). The panel shows whatever rows exist; Shelter need shading covers only countries with an SHL row | That is what the data gives | None |
| R8 | The power-plant marker is a disc with a dark edge (◉), unlike conflict's plain dots | C8: the two can show together | A shape tweak |
| R9 | The panel has two needs sections, "Basic needs" (WHO) and "People in need (UN humanitarian needs overview <year>)" (OCHA; the final review's I2: the data is a needs overview, HNO, not a response plan, and a country not in the file reads "Not in OCHA's <year> needs-overview file on HDX"), and the HNO file is chosen from the HDX package by its URL's file name | A panel section has one source line; the CKAN resource name is not the file name | Layout only |
| R10 | R4 also requires the CSV's Date of analysis to equal the national record's | A cheap guard that both are the same analysis | None |
| R11 | For R10, if either side's Date of analysis is blank, the area set is rejected: the country keeps Plan 2's R17 and the log says why | It cannot be confirmed as the same analysis | One country stays on Plan 2's R17 |
| R12 | The ICJ's Gambia v. Myanmar merits row stays out until it is confirmed from the Court's own text. `docs/curated-review.md` lists it as "awaiting confirmation" with the Court's links | The judgment PDF sits behind a bot check, and bot checks are never bypassed | One designation missing until the owner confirms |
| R13 | US completeness (C15) covers formal Secretary of State determinations published on state.gov and its archives (2001 onward). The 1990s US statements are listed for the owner in `docs/curated-review.md`; `governments.json` records this scope, and Settings shows it | The owner decides whether that counts as complete | The scope text changes |
| R14 | A camp counts as a current crisis (C11) only if its newest cited report does not say it closed. A camp reported closed is listed in the panel's history as "reported closed (<source>, <year>)", with no marker | A closed camp is not a current crisis | One fewer marker |
| R14b | Corrects R14's Camp 18 example (the rule follows the data): Camp 18's newest cited report (HRNK, 2024) says parts remain active, so it keeps its marker. Its panel row and tooltip show both "UN Commission of Inquiry (2014): reported closed in 2006" and "HRNK (2024): parts remain active". The owner question stays on the row | The example contradicted the rule; the rule is the principle | One marker |
| R15 | Reading `UN_A3` from GlobeCore's public Earth-assets countries file (read-only) is acceptable for P3-11 | No GlobeCore change; its public API is used | None |
| R16 | Codes with no map unit and a value of 0 (EIA's 8 zero-production territories) are not listed under "Not placed on the map"; they stay recorded in the manifest. Codes with a non-zero value are still listed | A zero value with no map unit tells the user nothing | None |
| R17b | The Water stress legend shows the grey arid key only when some country in the data has that category | No empty classes | None |
| R18 | The GEM files are not in `.downloads\gem`, so Task 8 runs before Task 7, and Task 8's `resources` scene shows oil production shading only. When the owner's files arrive, Task 7 re-takes that one scene with one extra launch | The plan must not wait on the owner | One extra launch later |
| R19 | Task 8 drops the planned `nigeria-areas` scene: the sample's Nigeria national record is projection-only, so R4 cannot match offline, and a synthetic national record would contaminate the shared samples. Area drawing is covered by `FoodAreaMapTests`' pixel tests | No fake data in pictures | No picture of real-density areas until the owner's hands-on check |
| R20 | The strip's Needs and Resources groups are collapsible; Crises stays open. A group starts open when one of its layers is on (an active shading or a shown marker layer); otherwise it starts as the user last left it, and closed if the user never chose. It never closes by itself while the user works (turning Food on does not close Needs). The user's open or closed choice per group is saved in settings (`OpenGroups`; a Plan 2 file has none and still loads). A snapshot scene opens exactly the groups holding its layers and saves no choice. A panel section focused by a scene or a marker click opens with its heading at the top of the panel's view | The tall strip squeezed the legend and hid the active toggle (Resources out of view); a tall focused section opened mid-way | One more click to open a group |
| R22 | GEM power rows with a TransitionZero id (`TZ: …` in `Other IDs (unit/phase)`) are never bundled, and a plant with any such unit is left out whole; the test excerpts hold no such row (the test adds a made-up id) | The power workbook's About sheet says those records come partly or wholly from TransitionZero's Solar Asset Mapper, CC BY-NC 4.0; C6 keeps non-commercial data out of the repository | None in this release: 18,723 plants carry one, none of them 500 MW or more |
| R23 | Oil and gas fields: 6,055 operating fields have coordinates, over the 5,000 cap (P3-9), so the fields GEM reports producing under a threshold are left out, raised from 0.1 in 0.1 million boe/y steps (0.2 in the March 2026 release: 4,933 fields). Production is each fuel's newest Data Year before the release year, in boe (gas at 6,000 cubic feet per barrel). A field with no usable figure stays | P3-9 gives oil and gas no threshold; GEM reports some of the largest fields only at project level, so dropping the unmeasured would hide giants | 1,122 small fields not shown; the boe conversion is a convention, used only to rank |
| R24 | GEM credits use GEM's recommended citation, publisher first, verbatim: "Global Energy Monitor, Global Integrated Power Tracker, September 2026 release." (no "V2"; the file-only version is the manifest's `version` and shown after the release, "September 2026 release (file V2)"); steel "..., September 2026 (V1) release."; oil and gas, which recommends none, the same order with "March 2026 release". A plant of several fuels lists each fuel's MW in its tooltip and takes its largest fuel's colour ("coloured by its largest fuel") | The review found the owner's strings reordered GEM's and put the file's V2 inside GEM's citation; Taichung showed "coal" for 280 MW of oil and gas | None; data unchanged |
| R24b | The GEM status dot keeps the short release form ("power September 2026 release (file V2); renewables …; oil and gas March 2026 release; steel September 2026 (V1) release"); the full credit (citation, licence link, changes) lives in the legend and the manifests | The dot's tooltip is a status line, not a credit | None |
| R25 | `MarkerLayer.Renewables` (Resources, after Power plants), from the same GIPT file: GEM Types wind, utility-scale solar (its Solar Thermal technology included), hydropower, geothermal and bioenergy; operating units summed per plant, R22 applied. Threshold tried at 100, 150, 200, 300 MW (then 100 MW steps) until 5,000 or fewer remain: 100 MW 9,047, 150 MW 5,379, 200 MW 4,021, so 200 MW (4,020 drawn). A filled hexagon with a dark edge in the power fuel colours; the legend names the threshold and says large plants also appear in Power plants. Its picture is its own scene, `renewables` (Europe, water stress), not added to `resources` | The owner asked for a renewables layer; four marker layers in `resources` smothered India and China in yellow hexagons and hid the power discs | 1,058 plants appear in both layers when both are on; plants under 200 MW are not shown |
| T7-1 | The three GEM extracts share one status dot, "GEM", which names each tracker's release, or which one did not load | One source; eleven dots are already many | A failure in one tracker turns the shared dot amber |
| T7-2 | A GEM release is dated by its month only: `CitationDate.Release`, shown as GEM cites it ("September 2026 release"), with a version GEM gives only in the file name after it ("September 2026 release (file V2)", R24); the manifest's `published` is the 1st of the month, for ordering only | GEM gives no day; never invent one on screen | None |
| T7-3 | Steel counts "operating pre-retirement" capacity as operating (the status shows it); oil and gas fields are one size (no size classes) | Those plants still produce; field production comes in different units and years, and 653 kept fields have no figure | Taranto and IJmuiden appear, marked pre-retirement |
| D1 | `data/curated/camps.json`'s `documentedBy` lists the UN Commission of Inquiry (A/HRC/25/CRP.1) as well as the HRNK reports (the Task 5 brief said HRNK) | The COI is the authority that names the camps | None |
| T8-1 | A snapshot scene may set `"showDrafts": true`: the curated drafts are shown for that picture only (`CuratedStore.SetShowDrafts`), as `--show-drafts` does for a whole run, and the window title says so. A scene's `panelFocus` may also be `atrocities` or `camps` | The five Plan 2 scenes keep their drafts off, so they still compare with the baseline | One more scenes-file field |
| T8-2 | The atrocity triangles' and camp squares' hover tooltips wrap a line longer than 110 characters at a space, with an indent (`TooltipText.Wrap`) | GlobeCore's tooltip does not wrap; Palestine's ICC warrants line was 215 characters, about 1,140 px wide on a 1,200 px window (now 577 px) | None; the wording is unchanged |
| OR-1 | **Owner review, 2026-11-24:** "Drafts are reviewed and approved." All 31 designations and 5 camp sites are approved as drafted (`approvedOn` 2026-11-24), so the app shows them without `--show-drafts`. Read as answers to the review's questions: the US Sudan genocide determination (2025) keeps its continuing statement and counts toward Sudan's triangle; Camp 15 keeps its marker on HRNK's 2015 report (no recency rule for camps); Camp 18 keeps its marker with both statements (R14b); the ICC warrants for Netanyahu and Gallant stay listed as interim steps. The answered questions were removed from the JSON | The owner approved the rows as drafted, with no corrections | If the owner meant otherwise on a question, one JSON edit per row |
| OR-2 | Not changed by the approval: the Gambia v. Myanmar merits judgment stays "awaiting confirmation" (it is not a draft row; it needs its operative paragraph from the ICJ's own text), and the four pre-2001 US genocide conclusions stay unlisted | Neither was a draft; both still need a source Claude could not open or that does not exist on state.gov | Myanmar has no ICJ merits row until confirmed |
| OR-3 | **Owner review, 2026-11-25:** "All approved." The 8 explainers, 5 UN depots, 21 pooled-fund rows and 4 US contacts are approved as drafted (`approvedOn` 2026-11-25). The 9 charity drafts are not yet marked approved: each needs a rating record naming the evaluator whose report the owner saw (Charity Navigator 3 stars or more, or all 20 BBB standards), which the owner has been asked for (settled by OR-8) | Approving a charity without saying which evaluator qualifies it would put an unchecked "Rated by" line on screen | Until the owner names the evaluators, every country shows its fund and "No vetted charities listed yet" |
| OR-4 | **Second batch of charities (owner, 2026-11-25: "Yes, second batch"):** P4-9 is re-run counting each international movement's branches as one organisation (for example MSF France, Spain, Switzerland and OCB together; Oxfam affiliates together), same 3W period and the same bar of 13 countries; organisations that newly clear it and have a US public charity are drafted; the first nine stay | The first count split MSF into its sections, so MSF (14 countries together) was missed | A movement that is in fact several independent charities is counted once |
| OR-5 | **Charities the owner names** (Doctors Without Borders, the American Red Cross) are drafted with the same checks: Publication 78 code PC, the owner's rating confirmation, and in each country the charity's own page showing its work there; OCHA 3W is not required for them (the American Red Cross never appears in 3W) | The owner asked for them by name; §9.2 check 3 accepts the charity's own published programme | A named charity with no country pages is listed nowhere (said in its note) |
| OR-6 | **Evidence on an international organisation's site** counts when the US charity's own site states it is that organisation's US member, affiliate or partner (a hyperlink is not required); the statement's page is cited in the charity's note. Applied to USA for UNHCR, Plan International USA and Oxfam America (Oxfam: three countries whose pages showed no work after 2019 removed) | The first batch's "the US site links to it" test was a stand-in for affiliation; the review found no links on three US sites that do state their membership | A US charity's statement of membership is taken at its word |
| OR-7 | **A country page must describe the charity's work in that country**; country names listed on a regional page do not count (Doctors of the World USA now cites its own country pages, linked from its regional pages) | The review found regional name lists only | Some cited pages describe work from 2021–2024; the OCHA 3W rows show the current presence |
| OR-8 | **Ratings checked with the owner supervising (2026-11-25).** The owner had approved the first nine charities assuming Claude had checked their ratings; nobody had (P4-8 left it to the owner). At the owner's request ("approach 2 with my supervision"), Claude opened each charity's Charity Navigator profile once in the built-in browser while the owner watched: all 16 are rated 3 stars or more. Each charity carries one rating record (Charity Navigator, its report link, 2026-11-25, "Claude in the built-in browser, owner supervising"); the star level is not stored or shown. The first nine are approved (reviewer: the owner, 2026-11-25); the seven second-batch charities await the owner's approval | The owner chose speed with supervision over checking 16 pages by hand | Charity Navigator's terms bar automated access; this was one supervised page view per charity, not scraping |
| OR-9 | **Owner review, 2026-11-25:** "Approve all seven." The second batch (American Red Cross, CARE USA, Doctors of the World USA, Doctors Without Borders USA, Oxfam America, Plan International USA, USA for UNHCR) is approved, with its weak points disclosed in each note and in `docs/help-review.md`; all 16 charities are now shown where they work | The owner read the weak points before approving | One JSON edit per row if the owner changes a decision |

### Rulings made while building Plan 4 (for the owner to check)

P4-1 to P4-15 were made while writing the Plan 4 plan (`docs/superpowers/plans/2026-11-24-plan-4-getting-help-in-and-one-small-step.md`, "Rulings made while writing this plan"; each is one line here, the plan has the reason and the cost).
R1 to R22 were made while building it (its SDD ledger; R20 to R22 in the final review's fix wave); **these R numbers are Plan 4's own**, not Plan 2's R1 to R17 or Plan 3's R1 to R25 above.
Everything curated in Plan 4 (depots, pooled funds, contacts, charities, explainers) is a draft until the owner approves it (P4-13).

- P4-1: no source needs a key, an account or an email (HAPI's API is not used; FTS, ACAPS and 3W come from HDX files).
- P4-2: ACAPS is treated by its stricter licence (CC BY-NC-ND 4.0): fetched live from HDX only, never committed, values shown unchanged, the credit names both licences, test samples synthetic; a note to ACAPS is in `docs/permission-requests.md`.
- P4-3: the ACAPS edition is the newest XLSX resource on HDX, named from its file name (R4 changes how "newest" is decided).
- P4-4: appeal funding is FTS's rows for the clock's year; a plan seen for more than one country, or called "Regional", is shown as "figures for the whole plan, all its countries" and never summed into a country.
- P4-5: hubs are scheduled-service large and medium airports (OurAirports), large and medium ports (World Port Index) and the UNHRD depots; each is an off-by-default marker layer with one status dot, "Hubs".
- P4-6: the crisis place for arcs is the country's `LabelPoint`; distances are great-circle on a 6,371 km sphere, always written "straight-line".
- P4-7: arcs show when the panel is open and "Lines to nearest hubs" is on (default on): at most three, to the nearest approved depot, airport and port; the panel lists the same three.
- P4-8: ratings are confirmed by the owner, not by scripts or agents; no task fetches charitynavigator.org or give.org pages; a charity without a confirmed qualifying rating is hidden.
- P4-9: the first charities follow a rule (3W presence in at least 5 countries within 12 months, US public charity in Publication 78, at most 15 organisations), not a choice.
- P4-10: UN missions are one link to the UN Blue Book; no list is compiled.
- P4-11: "Give" for the UN is the country's pooled fund with OCHA's links, otherwise CERF with the UN Foundation note.
- P4-12: "Your one small step" shows for a country with a current crisis or an FTS appeal for the year; Getting help in shows for every country.
- P4-13: contacts, pooled funds, depots and explainers are curated drafts with the Plan 3 review record, shown only after approval (or with `--show-drafts`).
- P4-14: new marker shapes (depot star, airport hexagon outline, port diamond outline, hub arc) and the Access shading's colours (R5 changes it to six classes, 0 to 5).
- P4-15: Settings gain "Your country", "Your state (optional)" and "Distances" (km or miles); nothing else about the user is stored.

| # | Ruling | Reason | Cost |
|---|---|---|---|
| R1 | The synthetic ACAPS HDX package names its newest XLSX resource `acaps-humanitarian-access-dataset_dec_2025.xlsx` | The tests expect "December 2025 edition" | Test strings change |
| R2 | The controller (not a subagent) opened unhrd.org and state.gov in the built-in browser and handed the findings over as a file | A subagent may lack the browser tools | None |
| R3 | Layer ids: `access` (shading), `un-depots`, `airports`, `ports`, `hub-lines` | The plan named only `access`; Task 10's scenes and `--layers` need the rest | A rename |
| R4 | The ACAPS feed picks the XLSX with the latest edition parsed from its file name, not the latest `last_modified`; a name that does not parse is skipped and logged; none parsing is a format error | On HDX the July 2025 file's `last_modified` is 3 s later than December 2025's, so P4-3's rule would pick the older edition | None (the file names are HDX's own) |
| R5 | ACAPS's ACCESS score 0 is a figure ("No constraints"), not a missing one: the Access scale has six classes 0 to 5 (YlOrBr 6) labelled with ACAPS's own level names, and the legend says "ACAPS score 0-5" | P4-14 assumed 0 had no name; a real figure must never look missing | One class |
| R6 | Senators' office phones and contact forms in the Senate sample are public-office data (US government work), not personal data | The "no personal data" rule is about app users and private people | Trim the sample |
| R7 | The ACAPS sample has 12 placed assessments, not 11 | Task 1's synthetic sheet has 13 country-level rows, one unplaced (XXK) | None |
| R8 | An ACAPS indicator cell that is not 0 to 3 (an information gap, "X") is left out of the data; the panel lists all nine indicators and shows a missing one as "No score (information gap)" | No figure must never look like a low score | Wording |
| R9 | The edition parser accepts ACAPS's real name variants (a `-` or `_` separator, a month in full or as 3 letters, an optional `-` before `_`); an unparsed name is logged at Warn | The plan's example was narrower than HDX's real names; a silently stale feed breaks "every fact shows its date" | None |
| R10 | The Getting help in group may start open on a fresh install because the hub lines are on by default | It shows a new user the group exists | One default |
| R11 | Each charity's "Working here: <evidence title> (checked <date>)" line links its evidence page | §9.1 shows evidence with the entry; every fact shows its source | One link per line |
| R12 | Subagent commits keep the trailer of the model that wrote them | Truthful attribution; no history rewrite | None |
| R13 | A programme share's tax year is the return's own IRS `TaxYr` (fallback: the calendar year the tax period begins), and the report also prints the period end | The app must say what the filing says; the period-end year was a year late for a June year-end | One year in a label |
| R14 | The vetting tool's link checker follows redirects by hand (at most 10) and refuses an evaluator host at every hop (reported as "not checked by the tool (evaluator's terms)"), and normalises a trailing dot | A redirect to charitynavigator.org or give.org would break P4-8's rule that no script fetches them | A few lines |
| R15 | For drafting charities, a country "has a current crisis" when its 3W rows have `has_hrp` or `in_gho` true | Drafting needs a fixed, source-defined list; inclusion in OCHA's Global Humanitarian Overview is OCHA's own threshold | A charity listed where there is no crisis stays hidden there anyway |
| R16 | `Charity` gains an optional `Note` (from the JSON's "note"), shown in `docs/help-review.md` and not in the app | A fact such as "EIN matched by name in Publication 78 only" would otherwise be silently dropped before the owner approves | One field |
| R17 | The `Note` carries the reviewer's disclosures: Save the Children US's site does not state membership of Save the Children International in so many words; where a charity's country evidence is on the international organisation's site (IOM, UNFPA, UNICEF USA, NRC USA) the note says so; NRC USA's EIN note stays | The owner must see judgement calls when approving | Wording |
| R18 | Three evaluator method pages (Charity Navigator's ratings method, give.org's BBB standards and Overhead Myth) were opened once each in the built-in browser and are cited as the "why these" sources | P4-8's purpose is no automated fetching of ratings or profiles (Charity Navigator's terms bar bots); a reader-style view of a method page to cite a threshold the spec itself states is not scraping | Drop three citations (the IRS and FTC sources remain); **owner: say if P4-8 is to be read strictly** |
| R19 | A snapshot scene may set `"explainer"` (an explainer id; needs a panel) to open the explainer card for that picture | No scene could open the card, so it could not be checked visually | One scenes-file field |
| R20 | The Airports marker layer draws only the large airports with scheduled service (about 1,150); the nearest-airport search and the panel row keep large and medium (P4-5 unchanged for the search), and the legend says "large airports with scheduled flights (the panel's nearest airport may be a medium one)" | Some 3,250 hexagons smothered the globe and hid the arcs (final review) | Medium airports are not drawn (they are still named in the panel and by the arc) |
| R21 | Each UN depot's position is the airport its own address names or sits beside, from the bundled OurAirports data (The Unlicense), not OpenStreetMap: Accra at Kotoka International, Dubai at Al Maktoum International (DWC), Panama at Panamá Pacífico International (Howard), Kuala Lumpur at Sultan Abdul Aziz Shah (Subang), Brindisi at Brindisi Airport; each row says the marker is at that airport, quotes the address, and is approximate | C6 forbids committing share-alike data (OpenStreetMap is ODbL) | A marker a few km from the depot; distances are rounded anyway |
| R22 | When no `cerf` row is shown (none approved, outside a `--show-drafts` run), the panel shows a built-in CERF line with P4-11's text and link | Every country keeps a UN fund to give to while the curated rows wait for the owner; it was accepted while building but not shown to the owner | None |

### Rulings made while building Plan 5 (for the owner to check)

P5-1 to P5-24 were made while writing the Plan 5 plan (`docs/superpowers/plans/2026-11-25-plan-5-shipping.md`, "Rulings made while writing this plan"; each is one line here, the plan has the reason and the cost; P5-23 and P5-24 were added at the owner's request on 2026-11-25, and R11 amends P5-21).
R1 to R15 were made while building it (its SDD ledger; R14 in Task 11); **these R numbers are Plan 5's own**, not Plans 2 to 4's above. Plan 5 does not change the data sources or the crisis layers: it adds the shipping, the cards, the text size and the update check. Tasks 1 to 11 are on the branch `plan-5`, local only until the owner says otherwise.

- P5-1: the installer is a signed Inno Setup `EarthRelief3D-Setup.exe` (not an MSIX; this supersedes §1 and §15's "MSIX"), installing to `C:\Program Files\Qapow\EarthRelief3D`, publisher "John Apolloni", as EarthWatch3D's; settings, cache and logs stay in `%LOCALAPPDATA%\EarthRelief3D`.
- P5-2: the app checks for and installs updates itself (owner, 2026-11-25), because Inno does not update an install; About shows the version and links "Releases on GitHub".
- P5-3: a public clone builds without a token (GlobeCore and AstroCore are public); the workflow still accepts a `SUBMODULES_TOKEN` secret if GlobeCore goes private again.
- P5-4: signing uses EarthWatch3D's Azure Trusted Signing account and profile, the `release` environment and a federated credential with this repository's ids; seven of the app's own files, then the installer, are signed.
- P5-5: the release workflow also runs the tests before publishing; pull requests build unsigned and never reach Azure.
- P5-6: version 1.0.0 in `Directory.Build.props`; tagging, pushing and releasing are the owner's.
- P5-7: Welcome, About and Data are cards in the main window (one at a time, Esc closes the card first), never separate windows or message boxes; a scene opens one with `"card"`.
- P5-8: text size 100%, 125% or 150% (`AppSettings.TextScale`), applied as a layout transform on every card and a factor on the globe's labels; the panel is `min(420 x scale, 0.45 x window width)` wide; GlobeCore's hover tooltip is not scaled.
- P5-9: one source registry, `data/sources.json`; every credit fragment must appear verbatim in `docs/data-sources.md` and `THIRD-PARTY-NOTICES.txt`; every status dot and bundled dataset has a row; every link is https.
- P5-10: About documents the severity bands (C7), one line per crisis kind built from `SeverityBands`, with a link to `docs/severity-bands.md`.
- P5-11: the Welcome shows once per user (`AppSettings.WelcomeShown`), never in a diagnostic or offline run; its key rows come from `LayerStyle` and `LayerCatalog`, so card and map cannot disagree; it offers the text size; About has "Show the welcome again".
- P5-12: FTS in January keeps a country's previous-year plans when it has none for the new year (1 to 31 January), says "No appeal in FTS for <year> yet", and counts a regional plan within its own year.
- P5-13: the WPI licence note is an input of the DataTool (`--portal-note`), not a hard-coded date.
- P5-14: each hub kind has its own arc lift factor so the short port arc stands above the depot arc (R14 changes the three factors after the pictures).
- P5-15: the `sudan-charities` picture opens Sudan's panel at the first charity entry (`panelFocus` `"charities"`); the scene clock (22 Nov) is before the charities' review date (25 Nov), which `VettingRules` does not reject.
- P5-16: the app icon is an orthographic Blue Marble globe centred on 10 N, 30 E, drawn by `tools/make-icon.py`.
- P5-17: the installer is not built on the owner's PC (nothing is installed here); Task 11 takes the pictures with the published exe; the update path cannot be tried end to end before two releases exist.
- P5-18: the update check runs 30 seconds after the window shows, then every 24 hours, with one request to GitHub's releases API; never in a diagnostic or offline run; a failure is logged and shown in About; Settings can turn it off.
- P5-19: the offer is a banner (no sound, nothing modal) with "Download and install", "What's new" and "Later".
- P5-20: only the asset `EarthRelief3D-Setup.exe` of the release `v<version>` is downloaded, with size, SHA-256 and Authenticode signer checks (same signer as the running exe); any failed check deletes the file and offers only "Open the download page".
- P5-21: "Install now" starts the installer silently with `/CLOSEAPPLICATIONS`, and the `.iss` restarts the app as the user (R11 amends how it is started).
- P5-22: after 30 days without a successful check the banner says the included data may be out of date; picture runs read a synthetic release only for a scene with `"update"`.
- P5-23: the "Layers" header closes the whole strip (owner, 2026-11-25); the icons-only mode is removed.
- P5-24: the legend folds away and starts closed (owner, 2026-11-25), keeping its header row and the UN boundary note; the choice is saved (`AppSettings.LegendOpen`).

| # | Ruling | Reason | Cost |
|---|---|---|---|
| R1 | The scene field `card` accepts welcome, about, data and settings from Task 4; a card whose view model is not registered fails the scene with a plain message | Keeps the field final | None |
| R2 | `TextScaleLayout.CardBounds` takes the legend's open or closed state, and the overlap test covers both | The legend's height changes with Task 2 | One parameter |
| R3 | About's version test asserts `"Version " + AppInfo.Version`, not 1.0.0 | Task 9 sets the version later | None |
| R4 | About's data paragraph names the daily GitHub check before Task 10 builds it | Both ship in the same release | None |
| R5 | A registry licence carries the qualifier `docs/data-sources.md` gives: UNHCR "CC BY 4.0 (from UNHCR's published terms; not yet confirmed for the API's data)", WHO GHO "CC BY 4.0 (WHO's data terms; not yet confirmed for GHO API rows, which may be CC BY-NC-SA 3.0 IGO)" | Every fact must say what is known and no more | Wording; the owner confirms both licences |
| R6 | The registry has 27 rows: IPC is split into `ipc` (country figures) and `ipc-areas` (area analyses), both on the "IPC" dot | The documents record two credits | One row |
| R7 | The legend header counts every legend line shown, hub lines included, so a fresh app reads "Legend (5 layers)" | The count must match what the legend lists | Wording |
| R8 | The panel is 420 px wide at 100% (it was 380) | More width suits larger text; every panel picture changes | One constant |
| R9 | With no room beside the panel (150%, or a window 1,000 wide) the explainer card sits under the panel in its column and both scroll | No overlap is the binding rule | Layout code |
| R10 | A country shown in January with only last year's plans counts as having an FTS appeal for P4-12 | That appeal is still the live one until the new year's is posted | The step section shows a few weeks longer for a few countries |
| R11 | (amends P5-21) The app starts the installer without the `runas` verb and Inno elevates itself, so the `.iss` restarts the app as the original, non-elevated user; the installer's Restart Manager closes the app; if the installer exits while the app is still running the banner says the update was not installed and why it may be (Windows' permission), with "Open the download page" and "Later" | Restarting the app as administrator would be a security fault | The refusal message is less certain about the cause |
| R12 | After the last checks the app holds the installer open with `FileShare.Read` until the installer process has started | Closes the check-to-run gap | A few lines |
| R13 | While the update banner shows, the panel and the explainer start below it (their top margin grows by its height); the banner spans from the strip's right edge to the window's right edge | It never covers a card | Layout code |
| R14 | Task 11's tuning: the arc lift factors are port 2.0, airport 1.25, depot 0.5 (P5-14 had 1.5, 1.25, 1.0), because in the first picture run the port arc was visible for only about 35 pixels (an estimate by eye); after the change about 60 and **is still mostly hidden under the depot arc** (the 3D test measures height, not what the camera sees); `PanelFocus.Charities` scrolls to the entry once the panel has been laid out | The pictures decide | The port arc is still hard to see; a 2D overlay or separation by bearing would be a later fix |
| R15 | When the strip is closed, Settings and About stay visible under the "Layers" header (P5-23 said only the header) | They are commands, not layers, and would otherwise be unreachable while the strip is closed | Two buttons |

### Out of scope (for now)

- Countries other than the US in the help panel (the data shape allows them, §9.4).
- Route planning, travel times, and road or sea networks.
- Live news text. ReliefWeb's terms forbid re-hosting report text, so the app links to reports (title and date) only.
- Accounts, donations through the app, or anything that stores who the user is.
- Deep zoom with map tiles (GlobeCore leaves room for it, §3.5).
- Saturn's rings and other non-surface features (GlobeCore leaves room for them).
- Translations.

---

## 2. Repositories and projects

```
C:\repos\AstroCore                       existing: shared astronomy (net10.0, no UI)
  src/AstroCore/Bodies                   NEW: IBodyRotation, EarthRotationModel, IauRotationModel (also the
                                         Sun's, P1-1), BodyConstants (IAU radii), SubPoints for any body
C:\repos\GlobeCore                       NEW repo: shared globe library
  external/AstroCore                     git submodule (for GlobeCore's own build and tests)
  src/GlobeCore                          net10.0, no UI: bodies, geo, image composition, feeds, storage, logging, time
  src/GlobeCore.Wpf                      net10.0-windows: viewport, camera, layers, picking, labels, styles, diagnostics
  src/GlobeCore.Earth                    Earth pack (net10.0, no UI, P1-22): Earth body, Blue Marble, Natural Earth,
                                         CountryIndex (Plan 2, P1-3), plates, clouds
  tests/GlobeCore.Tests                  moved generic tests + new body and layer tests
C:\repos\EarthWatch3D                    existing: switches to GlobeCore (Plan 1)
  external/AstroCore, external/GlobeCore submodules side by side
C:\repos\EarthRelief3D                   NEW repo
  external/AstroCore, external/GlobeCore submodules side by side
  src/EarthRelief3D.Core                 net10.0, no UI: domain model, providers, stores, help catalog, vetting rules
  src/EarthRelief3D.App                  net10.0-windows WPF app (CommunityToolkit.Mvvm)
  src/EarthRelief3D.Vetting              console tool for the maintainer: checks the charity list (§9.3)
  data/                                  bundled datasets, curated tables, country profiles, explainers (§10)
  packaging/, tools/, tests/EarthRelief3D.Tests, tests/Samples
```

- **One copy of AstroCore per app build.** Each app has AstroCore and GlobeCore as sibling submodules. GlobeCore's
  projects find AstroCore through an MSBuild property `AstroCoreRoot`, which defaults to GlobeCore's own submodule (for
  building GlobeCore alone) and is set by each app's `Directory.Build.props` to the app's copy.
- Repos are created locally. Nothing is pushed to GitHub until the owner asks.
- GlobeCore is public, like AstroCore.

---

## 3. GlobeCore

### 3.1 Bodies (AstroCore additions)

All existing AstroCore conventions stay: J2000 mean ecliptic frame, TT Julian dates.

- `IBodyRotation`: gives the body's north pole direction and prime-meridian angle W at a time, and so converts an
  inertial direction into body-fixed latitude and longitude.
  - `EarthRotationModel` wraps the existing `EarthRotation` (GMST with precession), so Earth results do not change.
  - `IauRotationModel` uses the IAU Working Group on Cartographic Coordinates and Rotational Elements formulas
    (pole α0, δ0 and W as polynomials plus periodic terms), from the 2015 report (Archinal et al. 2018). The Moon uses
    the report's mean-Earth/polar-axis model terms.
  - The Sun: an `IauRotationModel` with the IAU Sun elements (the solid-body rate; superseding the
    `CarringtonRotation` class first planned here, P1-1). Differential rotation is out of scope.
- `BodyConstants`: equatorial and polar radii from the same report.
- `SubPoints.SubSolarPoint(IBodyRotation, bodyPosition, t)` and `SubObserverPoint(...)` for any body. The existing
  Earth-only functions stay as wrappers.
- **Accuracy test:** sub-solar and sub-observer longitude and latitude for the Moon and Mars against JPL Horizons
  quantities 14 and 15 at several dates, to 0.05°. The existing Earth tests must still pass unchanged.

### 3.2 Core library (`GlobeCore`, no UI)

- `Body`: an immutable description: `Id`, `Name`, `Shape` (`Ellipsoid(equatorialKm, polarKm)`), `Rotation`
  (`IBodyRotation`), `LongitudeConvention` (how longitudes are shown: east-positive or west-positive), and
  `Illumination` (`SunLit` with twilight width, or `SelfLuminous` for the Sun). A body catalog is data.
- `SurfacePoint` (latitude, longitude): a body-neutral type, replacing direct use of `AstroCore.Earth.GeoPosition` in
  GlobeCore. A conversion keeps EarthWatch3D code working.
- **Geo:** `GeoLine`, `GeoPolygon`, `DateLine` (antimeridian splitting), `GeoJsonLines`, `GeoFormat`, `Units`, and
  `GeoMath`, which now takes the body's mean radius instead of the hard-coded 6371 km. Distances use a sphere of mean
  radius (accurate to about 0.5% on Earth, which the "straight-line distance" label tolerates).
- **Image composition:** `Bgra32Image`, `SparseOverlay`, and `GlobeImageComposer`, which now composes an **ordered
  list of `IImageLayer`** (each with a z-order) instead of the fixed `GlobeLayers` slots, then applies an
  `IIllumination` (night shade, tint, terminator line) computed from the sub-solar point of the body. The image size
  comes from the base texture, not a constant.
- **Feeds:** `IFeed` (renamed from `IEventFeed`), `FeedScheduler`, `FeedStatus`, `Backoff`, `FeedHttp` (the User-Agent
  is passed in, not read from `AppInfo`), `DownloadMeter`, `SampleHttpHandler` for offline tests.
- **Stores:** `SnapshotStore<TSnapshot>`, the lock-free immutable snapshot plus `Changed` event pattern generalised
  from EarthWatch3D's `EventStore`.
- **Storage, logging, time:** `AtomicFile`, `ISettingsStore`, `SettingsSaver`, `IProtector`, `AppPaths(appName)`,
  `PackageIdentity`, `IAppLog`/`FileLog`, `IClock`.

### 3.3 WPF library (`GlobeCore.Wpf`)

- `GlobeViewport`: the Helix viewport with an ellipsoid `BodyMesh` (equirectangular texture coordinates), lighting and
  the composed texture. Distances are in body equatorial radii, so the same code draws a body of any size.
- `CameraRules(body)`, `CameraAnimator`, `Projector` (ray-ellipsoid hit), `DirectionIndicator` (off-screen arrows).
- **Layers.** A small class hierarchy replaces EarthWatch3D's hand wiring:
  - `GlobeLayer` (abstract): `Id`, `Title`, `IsVisible`, `ZOrder`, `Source` and `AsOf` (for the legend), and
    `LegendItems`.
  - `ImageLayer : GlobeLayer`: draws into the globe texture (areas, borders, shading).
  - `SceneLayer : GlobeLayer`: owns 3D visuals above the surface (markers, arcs, tracks).
  - `OverlayLayer : GlobeLayer`: 2D screen-space content (labels with overlap avoidance).
  - `IPickable`: layers that can be hovered and clicked return a `PickResult` (an id and a tooltip line).
  - `LayerHost`: owns the layers, schedules texture recomposition (`RecomposeSchedule`, `Throttle`), routes hover and
    click to `IPickable` layers in z-order, and raises `Selected`. `LazyLayer<T>` loads layer data on first show.
- **Helpers moved as they are:** `LineRasterizer`, `AreaRasterizer`, `SphereLines`, `LabelOverlay`, `StaWorker`,
  `ImageConversions`.
- **Shared look:** `CardStyles.xaml`, `TimeBarStyles.xaml`, a `StatusDotsView` bound to `FeedStatus`.
- **Diagnostics:** `SnapshotRenderer` renders the window to a PNG with `RenderTargetBitmap` (never a screen capture),
  `CrashHandler`, `PerfLog`, and a base `LaunchOptions` parser that apps extend.

### 3.4 Earth pack (`GlobeCore.Earth`)

`EarthBody` (C2), the Blue Marble texture, Natural Earth coastlines and borders, `CountryIndex` (Natural Earth admin-0
polygons with ISO 3166 codes, point-to-country lookup, centroids and name matching, needed by EarthRelief3D), PB2002
plate boundaries, and the GIBS cloud layer with its Blue Marble colour key. Weather, quake and lightning feeds stay in
EarthWatch3D; PureHDF moves with lightning, not into GlobeCore.

### 3.5 Room left for later

- Tiled imagery for deep zoom: `BodyMesh` takes its texture from an `ISurfaceImagery`, which today returns one image.
- Rings and other non-surface features: `SceneLayer` can draw any geometry in the body frame.

### 3.6 EarthWatch3D switch-over (Plan 1)

1. Move code into GlobeCore as classified in Appendix A; update namespaces and `InternalsVisibleTo`.
2. Split `SceneController` (934 lines) into layers on `LayerHost`: base and borders, plates, clouds, areas, events,
   lightning, Sun and Moon markers. Earth-specific wiring stays in EarthWatch3D.
3. Fix the tests that find the repo root by walking up folders (`PackagingTemplateTests`, `MtgLiveTests`).
4. **Proof of no change:** all ~563 EarthWatch3D tests and all AstroCore tests pass; and `--snapshot` PNGs of fixed
   offline scenes (several display times, layers and camera positions) match the pre-switch PNGs pixel for pixel, or
   any difference is explained and accepted by the owner.

---

## 4. EarthRelief3D domain model (`EarthRelief3D.Core`)

The core rule: **every fact carries its provenance.** Records are immutable C# records.

- `SourceCitation`: publisher, dataset, version or edition, URL, licence and attribution text, `AsOf` (the date the
  data describes) and `RetrievedUtc`.
- `Place`: either a `SurfacePoint`, an admin area (ISO3 + admin code + polygon) or a whole country (ISO3).
- `Crisis` (abstract): `Id`, `Kind`, `Country` (ISO3), `Place`, `Title`, `NativeSeverity` (value plus the source's
  scale name), `Band` (1–5, C7), `Began` (if known), `Trend` (a short series plus the comparison used), `Citations`.
  Subclasses add what each kind needs:
  - `ConflictCrisis`: UCDP conflict id, sides, fatalities by month.
  - `AtrocityCrisis`: a list of `Designation`s (authority, `AuthorityType` = court judgment, government
    determination, UN commission or body finding, or NGO alert, and the term used, the date and the document), plus
    risk-list entries labelled as **risk, not finding**.
  - `DetentionCrisis`: country incarceration rate and/or documented sites (each with its report).
  - `FoodCrisis`: IPC phase per area, people in Phase 3+, the analysis period.
  - `OutbreakCrisis`: disease, WHO report date, link.
  - `DisplacementCrisis`: refugees from, asylum-seekers, internally displaced people of concern to UNHCR, year.
- `NeedIndicator`: `Need` (Food, Water, Sanitation, Shelter, Health), place, value, unit, what it measures (for
  example "people in need" or "% with basic sanitation"), citation.
- `ResourceSite`: `ResourceKind` (OilGas, Power, Steel, FreshWater, HealthFacility, AidDepot), location, capacity
  and unit, status, citation.
- `AccessAssessment`: country, overall score and the nine indicator scores on the source's 0–5 scale, the edition
  window, citation. Plus `FundingStatus`: appeal name, requirement, funding, % funded, citation.
- `LogisticsHub`: `HubKind` (Port, Airport, UnhrdDepot), name, location, size class, citation. `HubLink`: from a
  crisis place to a hub, straight-line km.
- `HelpOption` (abstract) → `DonationOption` and `ContactOption` (§9).
- `CountryProfile`: the rules for one user country (§9.4).
- `Explainer`: kind, plain-language text, citations, reviewed date.

### Services

- **Providers.** Each data source is one class implementing GlobeCore's `IFeed` (live sources) or `IBundledDataset`
  (bundled files), producing domain records, for example `UcdpCandidateFeed`, `HapiFoodSecurityFeed`,
  `WhoOutbreakFeed`, `UnhcrDisplacementFeed`, `HapiNeedsFeed`, `HapiFundingFeed`, `AcapsAccessDataset`,
  `GemPowerDataset`. Each owns its parsing, schedule, cache and citation. Adding a source is adding a class.
- **Stores:** `CrisisStore`, `NeedsStore`, `ResourceStore`, `AccessStore` (each a `SnapshotStore`), cached on disk so a
  restart without network shows the last data.
- `CountryResolver`: maps source country names and codes to ISO3 using `CountryIndex` (WHO reports give country
  names in text).
- `SeverityBands`: the documented mapping from each native scale to the 1–5 band (C7).
- `TrendCalculator`: per kind, for example conflict compares fatalities in the last 3 months with the 3 before; the
  panel shows the numbers, not only an arrow.
- `NearestHubFinder`: the nearest UNHRD depot, nearest large or medium airport and nearest large or medium port to a
  crisis place.
- `HelpCatalog` and `VettingRules` (§9).

---

## 5. Data sources

Checked on the sources' own sites on 2026-11-21. Licences are summarised; the exact attribution text goes in About and
`THIRD-PARTY-NOTICES.txt`. **Bundled** means the data ships with the app (refreshed by the maintainer); **live** means
the app fetches it on a schedule.

### 5.1 Crises

| Layer | Source | How | Licence | Fresh as of | Notes |
|---|---|---|---|---|---|
| Armed conflict | UCDP Candidate events (monthly) and GED (yearly) | Live, monthly release | CC BY 4.0 + dataset papers | Candidate 26.0.10 = October 2026 | The API needs a free token requested by email; bulk files may not (§12) |
| Genocide and atrocities | Hand-curated table of designations (US State Department determinations, ICJ judgments, UN commission findings) | Bundled, curated (§10.2) | Facts with citations | Each entry's date | No machine-readable list exists. The USHMM Early Warning Project risk list only with USHMM's permission; otherwise a link. Genocide Watch: link to its country reports |
| Mass incarceration | World Prison Brief (rates per country) | Bundled, only with permission | No licence stated | Monthly updates on site | Ask ICPR. Alternative to check: UNODC prison data (§12) |
| Prison camps | Hand-curated sites from cited reports (for example HRNK satellite reports on North Korea); ASPI's Xinjiang dataset only with ASPI's permission | Bundled, curated | Facts with citations | Each report's date | ASPI is all rights reserved; its data is from 2020–2022 |
| Famine and food crises | IPC phases via HDX HAPI | Live, daily check | CC BY / CC0 | Per analysis period | HAPI needs an `app_identifier` (base64 of an app name and an email address) |
| Disease outbreaks | WHO Disease Outbreak News (the site's content API) | Live, every 3 hours | CC BY-NC-SA 3.0 IGO | Each report's date | Undocumented endpoint: if it breaks, the layer shows "source unavailable" and keeps its cache. Country names resolved by `CountryResolver` |
| Forced from home | UNHCR Refugee Data Finder API | Live, weekly check | CC BY 4.0 ("UNHCR Refugee Population Statistics Database") | Mid-2026 | No key. IOM DTM data via HAPI is **not** used (no redistribution) |

### 5.2 Basic needs

| Need | Who lacks it | Where supply sits |
|---|---|---|
| Food | IPC (above); HAPI humanitarian needs, food security sector (CC BY-IGO, admin 1/2, 2024–2026) | FAOSTAT crop production by country (bulk files, CC BY 4.0; the API now needs sign-in) |
| Clean water | WHO GHO `WSH_WATER_*` indicators (country, to 2024); HAPI needs, WASH sector | WRI Aqueduct 4.0 water stress (CC BY 4.0, 2023), shown as the fresh-water resource |
| Sanitation | WHO GHO `WSH_SANITATION_*` indicators | — |
| Shelter | HAPI needs, Emergency Shelter and NFI sector; UNHCR displacement | UNHRD depots |
| Health care | HAPI needs, health sector; WHO GHO service coverage indicators | Health facilities from healthsites.io (ODbL, needs an OpenStreetMap-account key: Plan 3 decides, §12) |

- WHO GHO is used instead of washdata.org because UNICEF's terms need written permission to republish. WHO's data
  terms (public-health purpose, figures not altered, WHO cited) fit this app. The GHO API was flagged for
  deprecation but still works; the provider isolates it so a successor endpoint can be swapped in.
- **Not used:** WFP HungerMap LIVE (personal use only, no redistribution, no public API).

### 5.3 Industrial resources

| Resource | Source | Licence | Fresh as of |
|---|---|---|---|
| Power plants | Global Energy Monitor, Global Integrated Power Tracker | CC BY 4.0 | September 2026 release |
| Oil and gas fields | GEM Global Oil and Gas Extraction Tracker | CC BY 4.0 | March 2026 release |
| Steel plants | GEM Global Iron and Steel Tracker (formerly the Global Steel Plant Tracker) | CC BY 4.0 | March 2026, with quarterly updates |
| Oil production by country | US EIA API v2 `/international` | Public domain, with credit | Annual, to 2025 |
| Fresh water | WRI Aqueduct 4.0 | CC BY 4.0 | 2023 |

GEM files come through a web form (name, email, organisation, use case), so the maintainer downloads them by hand and
the app bundles a pinned, cleaned copy with its release month (§10). The unmaintained WRI Global Power Plant Database is
not used. worldsteel's data is paid or all-rights-reserved, so it is not used.

### 5.4 Getting help in

| What | Source | Licence | Fresh as of |
|---|---|---|---|
| Access score (0–5, nine indicators) | ACAPS Humanitarian Access dataset via HDX | CC BY | Twice a year (Jul/Aug 2026 edition: Dec 2025–May 2026) |
| Appeal funding (% funded) | OCHA FTS via HAPI | CC BY | Daily |
| Ports | NGA World Port Index | US government work (statement to confirm, §12) | Edition to confirm |
| Airports | OurAirports (GitHub mirror) | The Unlicense | Daily |
| UN depots | UNHRD's five hubs, hard-coded with addresses from unhrd.org | Facts | Checked 2026-11-21 |
| UN pooled funds | OCHA CBPF API (`Poolfund`) | To confirm (§12) | Daily |
| Access constraints detail | Logistics Cluster maps (PDF): **links only** | Non-commercial, credit | Per map |

Funding is shown because "the appeal is 23% funded" explains why help isn't arriving as plainly as an access score.

---

## 6. Crisis layers on the globe

- Conflict: dots at UCDP event locations for the last 12 months, sized by fatalities, coloured by age; the country is
  outlined when it has an active UCDP conflict.
- Atrocities: a distinct glyph (not a dot) at the country, with its designation types shown in the tooltip
  ("Court judgment: genocide (ICJ, 2026)").
- Detention: country shading by incarceration rate; site markers where documented.
- Food: IPC phase areas in the IPC's own colour scale, with a legend that names each phase.
- Outbreaks: a marker per open WHO report, at the country, labelled with the disease.
- Displacement: country circles sized by people displaced from that country.
- Every layer's legend line shows its source and "as of" date. A source that is unavailable shows its cached date and
  an amber or red status dot, as in EarthWatch3D.
- Layer groups in the layer strip: **Crises**, **Needs**, **Resources**, **Getting help in**. Needs and resources
  layers draw as country or basin shading (lack) and point markers (supply). Only one shading layer shows at a time, so
  colours stay readable; markers can stack.

## 7. Basic needs and resources

See §5.2 and §5.3. The legend for each shading layer says exactly what is measured ("Share of people using at least
basic sanitation, 2024"), never a vague label like "sanitation".

## 8. Getting help in

- In the country panel: the ACAPS access score with its nine indicators in plain words, the appeal's funding with
  requirement and % funded, and links to the Logistics Cluster's maps when one exists.
- On the globe, when a country is selected: great-circle arcs to its nearest UNHRD depot, large airport and port, each
  labelled with the straight-line distance (C3).

## 9. Your one small step

### 9.1 The panel

```
┌ <Country> ──────────────────────────────────────────────── as of <date> ┐
│ What is happening   (one section per crisis; history; explainer link)   │
│ Getting help in     access 4.2/5 · appeal 23% funded · nearest depot    │
│ YOUR ONE SMALL STEP                                                      │
│   Give                                                                   │
│    ▸ UN pooled fund for <country> (OCHA)                → donate page    │
│    ▸ <Charity A>  tax-deductible (IRS) · 84% on programmes (990, 2024)   │
│        rated by Charity Navigator, BBB Wise Giving  → their reports      │
│        working here: <evidence> · reviewed <date>        → donate page    │
│   Contact                                                                │
│    ▸ Your senators (<state>)                    → official contact forms │
│    ▸ Your representative                        → house.gov ZIP finder   │
│    ▸ US State Department                        → official contact form  │
│    ▸ <Country>'s mission to the UN               → UN Blue Book           │
│ Why these? · Sources · List last reviewed <date>                         │
└──────────────────────────────────────────────────────────────────────────┘
```

All links open in the user's browser. The app never handles money or asks for an address. The user may pick their
state once in Settings (optional) so the panel can list their two senators from the Senate's own contact XML; the
House is a link to the official ZIP finder.

### 9.2 The three checks for a charity

1. **Legally able to take donations.** In the US: listed in IRS Publication 78 (bulk file, updated monthly) and not on
   the IRS automatic-revocation list. UN pooled funds pass by being UN funds.
2. **Independently rated.** The reviewer confirms a qualifying rating (Charity Navigator 3★ or higher, or meets all 20
   BBB Wise Giving Alliance standards) and records the evaluator, the report link and the date checked. The app shows
   "Rated by <evaluators>" with links to their reports, never the ratings themselves. Next to it, the app shows the
   programme-spending share from the charity's own Form 990 (§9.3) with its tax year.
3. **Actually working in that crisis.** Evidence recorded by the reviewer, for example OCHA's operational presence data
   (3W, CC BY-IGO via HAPI) or the charity's own published programme in that country, with a link and the date checked.

Each entry records its evidence, the reviewer, the review date, and expires 12 months after review: an expired entry is
hidden, not shown out of date. The app never ranks charities by its own judgment; entries are listed alphabetically
after the UN pooled fund.

### 9.3 Keeping the list honest (`EarthRelief3D.Vetting`)

A console tool the maintainer runs before each release:

- checks every EIN against the latest Publication 78 and revocation files;
- computes the **share of expenses spent on programmes** from the charity's latest IRS Form 990 (Part IX, public-domain
  990 XML files), with the tax year, so this number comes from the government filing, not from a third party;
- lists entries expiring in the next 60 days and entries whose evidence links fail;
- writes a report the maintainer reviews; it never edits the list by itself.

The "Why these?" link explains the checks and the caution that a programme share alone can mislead: groups working in
hard-to-reach places spend more on logistics and staff, and that is the real work.

### 9.4 Countries

`data/countries/<ISO2>/profile.json` holds a user country's rules: the charity registry and how to check it, which
evaluators count, the contact roles and their official pages. `data/countries/<ISO2>/charities.json` holds the vetted
list. Settings has a country choice showing only countries with a profile (today, the US). `data/contacts/missions.json`
holds each crisis country's UN mission link and embassy page, with the date checked. The State Department's Diplomatic
List is discontinued, so embassy links are curated.

## 10. Bundled and curated data

- `data/datasets/<name>/`: cleaned extracts (GEM, Aqueduct summarised by basin, World Port Index, UNHRD) with a
  `manifest.json` per dataset: source, version, as-of date, licence, attribution, SHA-256 of the original download,
  and the `tools/` script that cleaned it. Large inputs (Aqueduct is a 262 MB zip) are reduced by the script, not
  committed.
- **10.2 Curated tables** (`data/curated/`): atrocity designations, documented camp sites, explainers. Every row needs
  a citation URL and date; a test fails the build if one is missing. The inclusion rule is the same for every country:
  a designation is shown if it was made by an international court or a UN commission or body, or is a formal
  determination by a government the user has chosen in Settings (default: the user's own country; C15). Its type
  (court judgment, UN finding, government determination and which government) is always shown; C14 covers alerts,
  pending cases and past genocides. Recent items are confirmed on the authority's own site before entry (for
  example, a reported ICJ merits judgment of 19 Nov 2026 in The Gambia v. Myanmar must be confirmed on icj-cij.org).
- Non-commercial or share-alike data (WHO, IDMC if used) is fetched at run time and cached in the user's data folder,
  never committed (C6).

## 11. User interface

Built like EarthWatch3D (MVVM, CommunityToolkit.Mvvm), reusing GlobeCore's styles:

- Layer strip (top left) with the four groups; legend with source and "as of" per layer.
- Country panel (right) as in §9.1, opened by clicking a country or a marker; hover shows a one-line tooltip.
- Explainers open from the panel: "What is this, and why does it happen?"
- Status dots (bottom right) per source; a "Data" view lists every source, its as-of date, licence and link.
- Settings: country (US), state (optional), "government determinations from" (default: your country; C15), km or
  miles, text size.
- The map shows the UN boundary note (Q3) in About and as a small line under the legend.
- Welcome on first start explains what the colours mean and that every fact has a source; About lists credits and
  licences.
- No sound anywhere; no graphic images; no pop-up alarms.

## 12. Open items

**For the owner (decisions):**

- Q1–Q3 were settled by the owner on 2026-11-21 (see Decisions). Background for Q1: Charity Navigator's API licence
  covers websites only and its site terms forbid republishing; BBB Wise Giving Alliance has no feed and reserves all
  rights; Candid is paid and forbids redistribution.
- Permission requests: see C13. Claude can draft the emails; the owner sends them.

**For the owner (actions only they can take):** requesting a UCDP token if the bulk files need one; requesting a
ReliefWeb app name if report links are used; choosing the contact email inside the HAPI app identifier (it is sent
with every request); downloading GEM files through its form; reviewing and signing off each charity entry.

**For the builder (verify early, Plan 2 Task 1 spike):** whether UCDP's monthly Candidate bulk files download without
a token; IPC data through HAPI covers the app's needs; the WHO outbreak endpoint's paging and fields; the World Port
Index licence statement and current edition (the NGA site was down on 2026-11-21); the CBPF API licence; UNODC prison
data access and licence as a World Prison Brief alternative; whether healthsites.io is worth a key in Plan 3.

## 13. Testing

- GlobeCore: moved tests, plus body rotation accuracy (§3.1), ellipsoid mesh and ray hit, layer ordering and picking,
  composer with several image layers, and the EarthWatch3D pixel comparison (§3.6).
- EarthRelief3D: a parser test per provider against captured samples in `tests/Samples`; `CountryResolver` against
  every country name the sources use; `SeverityBands`; `TrendCalculator`; `NearestHubFinder` (including across the
  date line and near the poles); `VettingRules` (expired entries hidden, a missing check hides the entry); curated
  tables (every row cited); help panel view model for a fixed country.
- Offline mode `--offline-samples` and `--snapshot` PNGs (RenderTargetBitmap) for visual checks, as in EarthWatch3D.

## 14. Error handling

As in EarthWatch3D: a failed source affects only its layer, which keeps its cache and shows its age; errors are logged
once and then counted; the app never closes on a feed error. A source that changes shape (a parser failure) turns its
dot red with "format changed" so the maintainer notices.

## 15. Plans

1. **GlobeCore:** AstroCore body rotation; GlobeCore and the Earth pack; EarthWatch3D switched over with every test
   and the pixel comparison passing.
2. **EarthRelief3D foundation:** the app on GlobeCore; domain model and citations; spike (§12); conflict, food,
   outbreaks and displacement layers; country panel (what is happening); status dots; offline samples.
3. **Atrocities, detention, needs and resources:** curated tables and their tests; WHO GHO and HAPI needs; GEM, EIA
   and Aqueduct datasets with their cleaning scripts.
4. **Getting help in and Your one small step:** ACAPS, funding, hubs and arcs; help catalog, vetting rules, the
   vetting tool, contacts; explainers.
5. **Shipping:** Welcome, About and licences, MSIX and signing, as EarthWatch3D does it.

---

## Appendix A — EarthWatch3D code: where it goes

Classification from a survey of EarthWatch3D at main 18b7296. **G** = moves to GlobeCore as is; **G\*** = moves with
Earth assumptions removed; **E** = Earth pack or stays in EarthWatch3D; **A** = stays in the app.

| Area | G | G* (what changes) | E / A |
|---|---|---|---|
| Core/Time, Logging | all | | |
| Core/Globe | `Bgra32Image`, `SparseOverlay` | `GlobeImageComposer` (layer list, illumination from body, size from texture) | `CloudLayer` → Earth pack |
| Core/Geo | `DateLine`, `GeoShapes`, `GeoJsonLines`, `GeoFormat`, `Units` | `GeoMath` (body radius), `ViewState.CameraSubPoint` | `ViewState.Faces`, `PlateBoundaries` → Earth pack |
| Core/Feeds | `IEventFeed`→`IFeed`, `FeedScheduler`, `FeedStatus`, `Backoff`, `DownloadMeter`, `SampleHttpHandler` | `FeedHttp` (User-Agent injected), `FeedStatus.IdleNote` text | USGS, GDACS, NHC, NWS, EONET, GIBS (GIBS → Earth pack) |
| Core/Events | `EventStore` pattern → `SnapshotStore<T>` | | model, merger, visibility: EarthWatch3D |
| Core/Lightning | | | EarthWatch3D (with PureHDF) |
| Core/Storage | `AtomicFile`, `IProtector`, `ISettingsStore`, `SettingsSaver` | `AppPaths(appName)`, `PackageIdentity` | `AppSettings`, `EventCache*` |
| App/Controllers | `LazyLayer<T>`, `Throttle`, `RecomposeSchedule` | | `SceneController` → split into layers on `LayerHost` |
| App/Rendering | `Projector`, `CameraAnimator`, `LabelOverlay`, `LineRasterizer`, `AreaRasterizer`, `SphereLines`, `StaWorker`, `ImageConversions`, `DirectionIndicator` | `GlobeMesh` → ellipsoid `BodyMesh`; `CameraRules` (body radius); Sun/Moon markers and `MoonPhaseGlyph` (any body) | `EventScene`, `EventPicker`, `AreasLayer`, styles, `LightningLayer` |
| App/Services | `IDialogService`, `DialogService`, `ILinkOpener`, `DpapiProtector` | | |
| App/ViewModels | | `TimelineViewModel` (range as a parameter), `SunMoonViewModel` | the rest |
| App/Views | `CardStyles.xaml`, `TimeBarStyles.xaml` | | the rest |
| App/Diagnostics | `CrashHandler`, `PerfLog` | `LaunchOptions` (base parser), `SnapshotDiagnostic` (readiness through `LayerHost`) | `OfflineSamples`, `PerfRun` |
