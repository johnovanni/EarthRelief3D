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
