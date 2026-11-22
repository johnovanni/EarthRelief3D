# Plan 2b — Updatable Boundaries in GlobeCore; EarthWatch3D "Boundaries" Layer

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Political boundaries stay in GlobeCore but stop being frozen. GlobeCore bundles a starting set, checks
online for a newer one, and uses an update only after it passes validation. Each app supplies its own adjustment
rules (for example Crimea as part of Ukraine), which are applied to whatever set is loaded. The design is
body-agnostic, so a Moon app could use the same machinery for named regions. EarthWatch3D adopts it and its
"Coastlines" layer button becomes "Boundaries" (issue EW-002).

**Decided by the owner (2026-11-22):**
- Boundaries stay in GlobeCore, with a bundled initial set for when there is no connection.
- GlobeCore checks periodically for updates, and updated values override the bundled set.
- EarthWatch3D's layer is renamed "Boundaries", fixing EW-002.

**Safeguards proposed by Claude and accepted:**
- Adjustment rules are data applied at every load.
- A download replaces nothing until it passes validation; the last good set is kept on disk.
- The edition in use is shown on screen.

**Spec:** `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md`, plus this header.
**Repos:**
- GlobeCore branch `country-index` (HEAD 68f9dc5, not merged; it gets a pull request at the end).
- EarthRelief3D `main` (local only).
- EarthWatch3D: new branch `boundaries` from `main` c70cbde, with a pull request at the end.

## Rulings made while writing this plan

| # | Ruling | Reason |
|---|---|---|
| B-1 | Generic types live in `GlobeCore` (no UI, body-agnostic): `Region`, `RegionIndex`, `BoundarySet`, `BoundaryRules`, `BoundaryValidator`, `BoundaryUpdateFeed` with an `IBoundarySource`. Earth specifics live in `GlobeCore.Earth`: a `NaturalEarthBoundarySource` (GitHub releases of `nvkelso/natural-earth-vector`), the bundled Natural Earth 5.1.2 files, and `CountryIndex`, kept as the Earth facade over `RegionIndex` with ISO3 and name properties | A Moon app plugs in its own source and region files. Earth apps keep today's API |
| B-2 | A **boundary set** is the countries polygon file, the land boundary-lines file and the admin-1 file the rules need, plus an `Edition` (`"Natural Earth 5.1.2 (2022-05-13)"` from the release tag and date) | The polygons and lines must come from the same edition |
| B-3 | **Rules are data** (`boundary-rules.json`, supplied by the app), applied at every load. A rule with no reason is rejected. Rule kinds: `moveAdmin1` (a list of admin-1 codes from one ISO3 to another; also removes land boundary lines whose every point lies on those admin-1 rings, like Perekop), `mergeUnits` (C16), `alias`. The Crimea fix-up becomes a `moveAdmin1` rule and `CrimeaFixup.cs` goes. If a rule doesn't match the loaded set exactly, that set is **rejected**, never half-applied | Owner decisions must survive updates, and a reshaped file must not quietly undo them |
| B-4 | **Validation** before a downloaded set is used: the rules apply cleanly; the region count is within ±10% of the bundled count; every app probe point (`boundary-probes.json`: Khartoum→SDN, Kyiv→UKR, Crimea→UKR (EarthRelief3D), Fiji both sides of 180°, …) lands in its expected region; and every name the app's data sources use still resolves (a hook the app supplies). A failure leaves the current set in place, turns the "Borders" dot amber with the reason, and logs it | A bad download never reaches the screen |
| B-5 | **Update check:** `BoundaryUpdateFeed` (`IFeed`, `Name = "Borders"`, `Interval = 24 h`) asks `https://api.github.com/repos/nvkelso/natural-earth-vector/releases/latest` (unauthenticated, generic User-Agent, ETag/If-None-Match). Only when the tag is newer than the edition in use does it download the three files from `https://raw.githubusercontent.com/nvkelso/natural-earth-vector/<tag>/geojson/<file>`, about 6.2 MB (countries 3.08 MB, lines 0.76 MB, admin-1 2.33 MB; first estimated as ~3.5 MB, corrected in Task 2), counted by `DownloadMeter` (see R4 and R5 below). A validated set is stored under `<data>/cache/boundaries/<tag>/` with a `manifest.json` (tag, date, SHA-256 per file), and loaded at the next start before the bundled one. `BoundaryUpdateFeed.Changed` swaps the live set (borders layer and country index) without a restart | Checked 2026-11-22: the latest release is v5.1.2 (2022-05-13), raw files are reachable, and the lines file is 760 KB |
| B-6 | **Edition on screen:** EarthRelief3D's legend boundary note and EarthWatch3D's layer tooltip show the edition and any applied rules, e.g. "Borders: Natural Earth 5.1.2 (2022-05-13); Crimea shown as part of Ukraine (UN General Assembly resolution 68/262)". The log lists the regions whose shapes changed when an update is adopted | Updates no longer pass through the owner's review before users see them |
| B-7 | **EarthWatch3D uses the same UN-view rules as EarthRelief3D** (Crimea as Ukraine, Perekop line removed) | The owner's Q3 view applied consistently across his apps. The only visible change is the Perekop line. Corrected after Task 4: there are 8 baseline scenes, and the Perekop line disappears in two of them (default and night-asia); every other pixel must stay identical, apart from the status strip's new Borders dot |
| B-8 | **EW-002:** the EarthWatch3D layer button and legend title become "Boundaries" (the owner chose this wording over the issue's "Borders"). The saved setting key `ShowCoastlines` and the `--layers coastlines` name stay for compatibility, `--layers boundaries` is added as an alias, and README and `docs/manual-test.md` are updated. EW-002 is marked Fixed in `private/EarthWatch3D-issues.xlsx` with the commit, and a note that the owner chose "Boundaries" | Existing settings and commands keep working |
| B-9 | Coastlines, the Blue Marble and the plates stay bundled-only | They are physical, not political |

**Rulings from the final review (they refine B-5):**

- **R4:** each app keeps its own User-Agent for the release check (B-5 said generic). It matches every other feed and
  carries no personal data.
- **R5:** no `DownloadMeter` for EarthRelief3D; instead the boundary feed (both apps) does not download a release's files
  again for 6 hours after a failed download (network error, timeout, 5xx). The release check keeps its schedule.
- **R6:** GlobeCore's transitional built-in Crimea rule and the parameterless `CountryIndex.LoadNaturalEarth()` and
  `NaturalEarthLines.LandBoundaries()` are deleted before `country-index` merges: the rules are the app's data (B-3).
- **R7:** a remembered rejection is stored with a fingerprint, the SHA-256 of the rules file bytes, the probes file bytes
  and GlobeCore's informational version. A different (or missing) fingerprint forgets the rejection; there is no expiry.

## Global Constraints

- Everything from Plan 2's Global Constraints (sources and dates on screen, no sound, no personal data, local commits
  only, no push without the owner's OK, the commit attribution line, at most 3 app launches per repo in this plan,
  snapshots only through `RenderTargetBitmap`).
- With no network, both apps behave exactly as now. **EarthRelief3D's 5 and EarthWatch3D's 8 (first written as 7)
  baseline snapshot scenes must be pixel-identical** (B-7), apart from text that now names the boundary edition, which
  is reviewed by eye, and the Perekop line (see B-7).
- GlobeCore stays body-agnostic in `GlobeCore`; anything Natural Earth or ISO3 lives in `GlobeCore.Earth`.

## Review Focus

1. **A malformed or partial download** (truncated file, HTML error page, 404 on one of the three files) must leave the
   current set and say why. Pinned by `BoundaryUpdateFeedTests.TruncatedFileIsRejectedAndCurrentSetKept` and
   `.MissingFileRejectsTheWholeSet`.
2. **A new edition that reshapes Crimea** so the rule no longer matches must be rejected with "rule moveAdmin1
   UA-43 no longer matches", not half-applied. Pinned by `BoundaryRulesTests.MismatchedRuleRejectsTheSet`.
3. **Restart offline after an update** must load the stored validated set, not the bundled one. Pinned by
   `BoundaryStoreTests.StoredSetLoadsBeforeBundled`.
4. **GitHub rate limiting (403/429) or no network** turns the dot amber and retries, and never deletes the stored
   set. Pinned by `BoundaryUpdateFeedTests.RateLimitedKeepsSet`.
5. **Swapping sets while the app runs** must not race the compose or picking. The index and lines are replaced
   atomically, and layers invalidate through `LayerHost`'s marshalling. Pinned by
   `BoundarySwapTests.SwapFromFeedThreadIsAtomic` (GlobeCore) and an app-level test in each app.

---

### Task 1: GlobeCore — regions, boundary sets, rules, validation, store

**Files (GlobeCore branch `country-index`):**
- Create in `src/GlobeCore/Regions/`: `Region.cs`, `RegionIndex.cs`, `BoundarySet.cs`, `BoundaryRules.cs`,
  `BoundaryValidator.cs`, `BoundaryStore.cs`
- Create: `src/GlobeCore.Earth/NaturalEarthBoundaries.cs`, which loads the bundled set from the embedded assets and
  maps Natural Earth properties to `Region`
- Modify: `src/GlobeCore.Earth/CountryIndex.cs`, which becomes a facade over `RegionIndex` with the same public API,
  so existing callers compile
- Modify: `NaturalEarthLines.LandBoundaries()`, which now comes from the current `BoundarySet`
- Delete: `CrimeaFixup.cs`. Its logic becomes the `moveAdmin1` rule.

**Interfaces (produced):**
- `public sealed record Region(string Id, string Name, IReadOnlyList<string> OtherNames, GeoPosition LabelPoint, IReadOnlyList<GeoPolygon> Polygons)`
- `public sealed class RegionIndex` with `Find(GeoPosition)`, `ById(string)`, `Resolve(string name)`, `All`.
  Normalisation and point-in-polygon move here from `CountryIndex`.
- `public sealed record BoundarySet(string Edition, DateOnly EditionDate, RegionIndex Regions, IReadOnlyList<GeoLine> Lines)`
- `public sealed class BoundaryRules`:
  - `static BoundaryRules Parse(Stream json)`
  - `BoundarySet Apply(RawBoundaryData raw)`, which throws `BoundaryRuleException` with the rule name on any mismatch
  - `IReadOnlyList<string> Descriptions`, the text shown on screen
- `public sealed class BoundaryValidator(IReadOnlyList<(GeoPosition Point, string ExpectedId)> probes, Func<RegionIndex, IReadOnlyList<string>> unresolvedNames, int bundledRegionCount)`
  with `ValidationResult Validate(BoundarySet)`.
- `public sealed class BoundaryStore(string cacheDir)` with `TryLoadNewest(...)` and `Save(tag, date, files)`. It
  writes atomically and records a SHA-256 per file in `manifest.json`.
- `public sealed class CurrentBoundaries`, which holds the live `BoundarySet`, with `Swap(BoundarySet)` and
  `event Changed` (raised after the swap; readers take the current reference once per operation).

- [ ] **Step 1: Write the failing tests.**
  - `RegionIndex` tests moved from `CountryIndex` (all existing `CountryIndexTests` must still pass through the
    facade).
  - `BoundaryRulesTests`: `CrimeaRuleMovesAdmin1AndDropsPerekop` (same results as today's fix-up),
    `MismatchedRuleRejectsTheSet`, `RuleWithoutReasonIsRejected`, `MergeUnitsAndAliasesApply`.
  - `BoundaryValidatorTests`: a probe fails, the count is out of range, an unresolved name.
  - `BoundaryStoreTests`: `StoredSetLoadsBeforeBundled`, `CorruptStoreFallsBackToBundled`.
  - `BoundarySwapTests.SwapFromFeedThreadIsAtomic`.
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS, including all existing GlobeCore tests.
- [ ] **Step 5:** Commit with the message `GlobeCore: regions, boundary sets, data rules, validation, store`.

### Task 2: GlobeCore — boundary update feed

**Files:**
- Create: `src/GlobeCore/Regions/IBoundarySource.cs`, `src/GlobeCore/Regions/BoundaryUpdateFeed.cs`,
  `src/GlobeCore.Earth/NaturalEarthBoundarySource.cs`
- Test: `tests/GlobeCore.Tests/BoundaryUpdateFeedTests.cs`, with samples: a releases JSON for v5.1.2, one for a
  fictional v5.2.0 pointing to sample files, a truncated file and an HTML error page

**Interfaces:**
- `IBoundarySource` with `Task<ReleaseInfo?> LatestAsync(string? etag, CancellationToken)` and
  `Task<RawBoundaryData> DownloadAsync(ReleaseInfo, CancellationToken)`.
- `BoundaryUpdateFeed(IBoundarySource source, BoundaryRules rules, BoundaryValidator validator, BoundaryStore store, CurrentBoundaries current, IAppLog log)`
  as an `IFeed` with `Name = "Borders"` and `Interval = 24 h`. It reports "Using Natural Earth 5.1.2 (2022-05-13);
  no newer release" through the `FeedStatus` note, and "Rejected v5.2.0: <reason>" on a validation failure.

- [ ] **Step 1: Write the failing tests:**
  - `NoNewerReleaseDownloadsNothing` (1 request, ETag honoured)
  - `NewerValidReleaseIsStoredAndSwapped` (log lists changed regions)
  - `TruncatedFileIsRejectedAndCurrentSetKept`
  - `MissingFileRejectsTheWholeSet`
  - `RateLimitedKeepsSet`
  - `DownloadsAreCounted`
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS.
- [ ] **Step 5:** Commit with the message `GlobeCore: boundary update feed (Natural Earth releases)`.

### Task 3: EarthRelief3D — use the boundary sets and feed

**Files:**
- Create: `data/boundary-rules.json` (the Crimea rule with its UN General Assembly resolution 68/262 reason; units
  move here from `country-units.json`, which then goes), and `data/boundary-probes.json`
- Modify: `CountryResolver` (built from `CurrentBoundaries`, rebuilt on `Changed`), `App.xaml.cs` (register
  `BoundaryUpdateFeed` and a "Borders" status dot), `BordersImageLayer` (lines and shading from the current set),
  `LegendViewModel` (boundary note with the edition and the rule descriptions, B-6), and the panel and hover (no
  change beyond resolving through the current index)
- Re-pin `external/GlobeCore` to the Task 2 commit.

- [ ] **Step 1: Write the failing tests:**
  - `BundledSetWithRulesMatchesBefore` (the Crimea, Somaliland and Fiji results unchanged)
  - `LegendNamesBoundaryEdition`
  - `SwapRebuildsResolverAndRedraws`
  - `ValidatorRejectsSetThatLosesAUcdpName`
- [ ] **Step 2:** Run them and expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run the tests and expect PASS.
- [ ] **Step 5:** Snapshots (one launch). The 5 scenes must be pixel-identical apart from the legend's boundary note.
  Open the changed PNGs, check them, and update the baseline.
- [ ] **Step 6:** Commit with the message `Use updatable boundary sets (Natural Earth + UN-view rules)`.

### Task 4: EarthWatch3D — boundary sets, feed, "Boundaries" layer (EW-002)

**Files (EarthWatch3D branch `boundaries` from `main` c70cbde):**
- Re-pin `external/GlobeCore` to the Task 2 commit (branch `country-index`). Note that EarthWatch3D now gets the
  `Lines` of the current set.
- Create: `data/boundary-rules.json` (the same Crimea rule, B-7) and `data/boundary-probes.json`, copied to the
  output
- Modify:
  - `App.xaml.cs`: register `BoundaryUpdateFeed` and add "Borders" to the status dots.
  - `LinesImageLayer`: borders come from `CurrentBoundaries`; rebuild on `Changed`.
  - `Views/LayerStrip.xaml`: the button reads "Boundaries" and the legend title "Boundaries", with the edition
    tooltip (B-6).
  - `Diagnostics/LaunchOptions.cs`: accept `boundaries` as an alias of `coastlines`.
  - README and `docs/manual-test.md` wording.
- Test: update `LayerWiringTests` and `LaunchOptionsTests`; add `BoundariesLayerLabelAndAlias` and
  `SwapRebuildsLinesLayer`.

- [ ] **Step 1:** Write the failing tests, then implement.
- [ ] **Step 2:** Full EarthWatch3D suite in Release: 0 warnings, and the known flaky tests noted.
- [ ] **Step 3: Snapshots** (one launch). The 8 scenes (first written as 7) are compared against `artifacts\snapshot-baseline` with
  PngDiff at tolerance 0, and must be identical. If the layer-strip label changed pixels in a scene, open it, check
  only the label changed, and record that.
- [ ] **Step 4:** In `private/EarthWatch3D-issues.xlsx`, set EW-002 to Fixed with the commit and date, and note that
  the owner chose "Boundaries". Back up the file first, and keep the table's reference range valid.
- [ ] **Step 5:** Commit on `boundaries` with the message `Boundaries layer (EW-002) and updatable boundary sets`.
  Do not push; the owner approves the pull request.
