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

## Food shading (2026-11-23)

The owner decided R16's question: ruling **R17** (IPC's 20% rule per country, an outline for a worse phase with people,
and a phase bar in the panel; see the spec and `docs/data-sources.md`).

- Tests: `dotnet build EarthRelief3D.sln -c Release`: 0 warnings, 0 errors; `dotnet test` in Release: **226 passed,
  0 failed, 0 skipped** (178 before R17; 210 after the first build, 221 after review fix round 1).
- Counts with the sample file (judged 2026-11-22): 36 current analyses (8 more are stale). 7 are shaded Phase 3 Crisis:
  AFG, COD, LBN, PSE, SOM and YEM with a Phase 4 outline, SDN with a Phase 5 outline. 29 are not shaded but outlined
  (since round 2 with a faint tint too): 19 in Phase
  4's colour, 10 in Phase 3's. None reaches 20% in Phase 4 or higher, so nothing is shaded Phase 4 or 5 (before R17,
  all 36 were shaded, Sudan Phase 5 and Somalia and Afghanistan Phase 4). Pakistan (19.7%, which the file rounds to 0.2)
  is not shaded (faint tint and outline).
- Snapshots: the five scenes into `artifacts\snapshot-food`. PngDiff against the old `artifacts\snapshot-baseline`:
  `pacific` identical (no IPC rows there); `africa`, `sudan-panel`, `sudan-panel-food` and `no-crisis-panel` differ, as
  expected (the shading, the outlines and the taller legend; the panel's food section).
- Repeat check: the scenes again into `artifacts\snapshot-food-2`; **PngDiff: all five identical** (exit code 0).
- Seen in the pictures: Sudan orange (Phase 3) with a dark-red (Phase 5) band just inside its border, clearly darker than
  the bright-red Phase 4 outlines of Yemen, Somalia and the West African countries, which are now outlined only; the legend
  with the rule, the outline key (a red ring) and the famine note; the Sudan panel's "Country shading" and "Outline" rows,
  the phase bar with its "worst 20%" marker in the Phase 3 segment, the caption "The line falls in Phase 3, so Sudan is
  shaded Crisis, with a Phase 5 outline." and the five phase rows (Phase 5: 134,808, 0.3%). The dateline seam cannot be
  seen with these samples (no IPC row for Fiji or Russia); it rests on `FoodMapTests`.
- Tuned after the first look: the outline was first drawn centred on the border, 2 px wide; it was thin, half hidden
  under the border line, and two outlined neighbours overwrote each other on their shared border. It is now a band about
  2.5 px wide just inside each country's edge (about 2 px show beside the border line). The bar's caption did not wrap (an implicit TextBlock style
  does not reach into a template) and was cut off; it now wraps.
- App launches: 3 (the first snapshot run, the run after tuning, the repeat run). The last two are the ones above.
- Not done in this task: copying the new pictures over `artifacts\snapshot-baseline` (the copy was refused by the
  session's permission check). Copy `artifacts\snapshot-food\*.png` over it to make them the baseline.

### Round 2 (2026-11-23): faint tint for outline-only countries

- Owner's decision: a country with people in Phase 3 or worse but no phase covering 20% also gets a faint tint in its
  outline's colour (alpha 46 of 255, about 18%; a shaded fill is about 60%). Review fix round 1 (one denominator for the
  rule and the bar, captions, small countries, one record per country) and the round 2 minors are in the same build.
- Tests: Release, **226 passed, 0 failed, 0 skipped**; 0 warnings.
- Snapshots: the five scenes into `artifacts\snapshot-food-r2`. PngDiff against `artifacts\snapshot-food`: `pacific`
  identical; `africa`, `sudan-panel`, `sudan-panel-food` and `no-crisis-panel` differ (the tints, the legend's longer
  food line and new key, and the panel's food wording).
- Repeat check: the scenes again into `artifacts\snapshot-food-r2b`; **PngDiff: all five identical** (exit code 0).
- Seen in the pictures: the 29 outline-only countries (Mali, Niger, Chad, Kenya, Mauritania, Mozambique and others) now
  carry a light salmon (Phase 4) or pale orange (Phase 3) tint inside their outlines, clearly lighter than the orange
  shaded countries (Sudan, Somalia, Yemen, DR Congo, Afghanistan); Sudan's map is unchanged (orange, dark-red Phase 5
  band); the Sudan panel says "of 47,535,794 people analysed (the phase numbers add up to 30 more)"; the legend's food
  line is two lines longer (the rule's last clause and the faint-tint key).
- App launches: 2 (the snapshot run and the repeat).
- The baseline is **not yet replaced**: `artifacts\snapshot-baseline` still holds the pictures from before R17. The owner
  decides whether `artifacts\snapshot-food-r2` becomes the baseline.
