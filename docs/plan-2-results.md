# Plan 2 (foundation) results

Built 2026-11-22 on `main`: crisis data, the four feeds, the crisis rules, the globe with its layers, the legend, the
country panel, status dots, and the diagnostics.

## Tests

`dotnet build EarthRelief3D.sln -c Release`: 0 warnings, 0 errors. `dotnet test EarthRelief3D.sln` in Release and in
Debug: **133 passed, 0 failed, 0 skipped** (109 before Task 7; Task 7 added 24 in `LaunchOptionsTests`,
`SnapshotScenesTests` and `OfflineSamplesTests`).

## Snapshots

- Command: `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json
  --snapshot-out artifacts\snapshot-baseline` (and `artifacts\snapshot-baseline-2` for the repeat). The pictures are
  RenderTargetBitmap renders of the window (1482 x 903 pixels here), saved by GlobeCore's `SnapshotRenderer.SavePng`.
- Five scenes, all at 2026-11-22T12:00:00Z: `africa` (10,25; all layers), `pacific` (-15,180; outbreaks), `sudan-panel`
  (15,30; panel SDN), `sudan-panel-food` (the same, with the panel scrolled to its last section so the food and
  displacement sections appear in a picture), `no-crisis-panel` (65,-19; panel ISL).
- **Repeat check: PngDiff reported all five pictures identical** between the two runs (exit code 0). Determinism comes
  from a display clock fixed per scene, a feeds clock fixed at the first scene's time, an immediate camera move, no other
  animation, and waiting for the layer host to be idle for 600 ms before each picture.
- Not committed: `artifacts\` is git-ignored. Re-run the command above to regenerate.
- Seen in the pictures: the globe and borders; IPC shading in phase colours (Sudan Phase 5, Somalia and Afghanistan
  Phase 4, others Phase 3); conflict dots over Sudan, Ukraine and DR Congo; sky-blue cross outbreaks (the synthetic
  reports) with disease names; displacement rings; the legend with a source and date on each line; the Sudan panel
  (conflict, food, displacement: the WHO sample has no Sudan report) and Iceland's "No crisis recorded by these sources"
  panel with a line per source.
- Not visible with these samples: Haiti (no current IPC period in the sample, so it is not shaded), Myanmar (no scene
  looks at it), and Fiji on both sides of 180 degrees (the IPC sample has no row for Fiji, so nothing is shaded; the
  `pacific` picture shows the borders either side of the dateline, at night). Picking Fiji on both sides rests on the
  resolver's split-polygon tests, not on a picture.
- Tuned after the first look: card backgrounds made more opaque (legend 80% to 95%, panel 90% to 97%) because map labels
  showed through them, and a soft dark halo behind the map labels so the blue disease names read over land.

## App launches in this task

3: the first baseline run (looked at, then tuned as above); the baseline run after the tuning; the repeat run. The last
two are the ones the results above come from.

## Final review fixes (2026-11-22)

**Final state (after R14b):** EarthRelief3D `dotnet test` in Release: **167 passed, 0 failed, 0 skipped**, 0 warnings;
GlobeCore (branch `country-index` at 68f9dc5): 322 passed. All five snapshot scenes match `artifacts\snapshot-baseline`.
Sudan's conflict deaths are 3,937 (Government of Sudan vs SFA) and 465 (SFA vs civilians), as before R14. The bullets
below record the intermediate R14 run and are kept for the history only.

After the whole-branch review (findings I1 to I5, rulings R14 to R16 and the deferred small items; R14 was then
replaced by R14b, see the last part of this section):

- Tests: `dotnet build EarthRelief3D.sln -c Release`: 0 warnings, 0 errors; `dotnet test` in Release: **163 passed,
  0 failed, 0 skipped**. GlobeCore (branch `country-index`, now pinned at the Crimea commit): 322 passed, 0 warnings.
- One app launch re-took the five scenes into `artifacts\snapshot-final`. PngDiff against the old baseline: `pacific`
  identical; `africa`, `sudan-panel`, `sudan-panel-food` and `no-crisis-panel` differ, as expected:
  - the legend's conflict line now says what the code does (band per conflict and country over the data window), so the
    legend is one line taller;
  - the food section of the Sudan panel lists the people in each phase (Phase 3 Crisis 14,316,494; Phase 4 Emergency
    5,015,231; Phase 5 Catastrophe/Famine 134,808, as in the sample);
  - Sudan's conflict deaths fall slightly (Government of Sudan vs SFA 3,937 to 3,921; SFA vs civilians 465 to 455),
    because events are now placed by their coordinates (R14) and a few border events fall in Ethiopia, Libya and Egypt;
  - for the same reason Russia, Libya and Egypt now carry conflict labels (border events from Ukraine's Sumy and Kharkiv
    oblasts and Sudan's North Darfur; see the cost noted under R14 in `docs/data-sources.md`).
  The new pictures were copied over `artifacts\snapshot-baseline`.
- The offline run's log now stays in the usual logs folder; this run's log shows "IPC: 44 countries with people in
  Phase 3 or higher (8 stale); 6 with a projection only" and "UCDP: ... 38 events placed by their coordinates in
  another country than UCDP's country name, 42 at sea or off a coast placed by the name; events not placed: none".
- **R14b** (replaces R14): UCDP's country name decides, and coordinates override it only for a territory UCDP does not
  list (PSE) or an unknown name. One more launch re-took the scenes; see the final-fix report for the PngDiff. With R14b
  no sample event is placed outside the country UCDP names (0 of 4,494), so the Russia, Libya and Egypt conflict labels
  and the Sudan death changes listed above are gone again.
