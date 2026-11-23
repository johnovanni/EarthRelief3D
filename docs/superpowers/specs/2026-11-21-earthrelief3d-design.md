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
| R17 | (Owner's decision, 2026-11-23.) Each country is shaded by IPC's 20% rule: the worst phase p from 5 to 3 such that the people in Phase p or higher are at least 20% of the people analysed (exact counts, never the rounded percentages; the denominator is "Population analyzed current", else the sum of the Phase 1 to 5 counts, else the share is unknown and nothing is shaded); Phases 1 and 2 are never shaded. The country is outlined (a band about 2 px wide just inside its edge, opaque, none along the ±180° split) in the colour of the worst phase that has people in it when that is worse than the shading. The panel's "Country shading" row names the rule, and a phase bar shows Phases 1 to 5 stacked to 100% with a "worst 20%" marker, a caption and a row per phase (also for a past analysis). The tooltip names the shaded phase, then the worst phase with people. The legend describes the rule, has an outline key, and notes that a country shaded Phase 5 is not an IPC famine classification | IPC's own area rule; a small Phase 5 count shows as an outline, not as a country in famine; the bar shows how the rule was applied | Our country-wide use of an area rule (IPC applies it to areas, and IPC area maps come in Plan 3); with the sample, 29 of 36 current analyses are only outlined. One function (`IpcAreaRule`) serves the map, tooltip and panel. A cache schema bump (4) |

Notes on the spec: §6's "last 12 months" for conflict is the UCDP window of about 9 to 12 months (P2-8). P1-4 said Plan 2
would move the launch-option parser to GlobeCore; it stayed in the app (`src/EarthRelief3D.App/Diagnostics/LaunchOptions.cs`),
and only the status dots moved (P2-6).

Plan 2b (updatable boundaries, built 2026-11-22) has its own rulings, B-1 to B-9 and R1 to R9, in
`docs/superpowers/plans/2026-11-22-plan-2b-boundaries.md`; they refine C16 and R15 above.

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
