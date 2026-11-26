# Hub arcs side by side: results

2026-11-25, branch `hub-arcs-side-by-side` (from `plan-5` 8d036a4), not pushed. Owner item 15 of Plan 5: the teal port
arc from Sudan to Port Sudan lay under the blue arc to the Dubai depot in `sudan-help`.

Design: `docs/superpowers/specs/2026-11-25-hub-arcs-side-by-side-design.md` (the owner chose "side by side" over bowed
curves and further 3D tuning). Plan: `docs/superpowers/plans/2026-11-25-hub-arcs-side-by-side.md`.

## What changed

- The hub arcs are drawn as **2D lines on the globe's overlay** (`HubArcLayer` is now an `OverlayLayer`): a 3 px line in
  the hub's colour over a 5 px dark edge, under the place names and distance labels, with the parts behind the globe cut.
- **Arcs that run together on screen are drawn side by side**, 4 px between their centre lines, each at most about 2 px
  off its true path; the longest is drawn first, so the shortest is on top where they meet (`HubArcScreen`).
- **One lift for every kind of hub** again: P5-14's per-kind factors (port 2.0, airport 1.25, depot 0.5) are withdrawn.
  The distance labels still sit at the arcs' midpoints; distances are unchanged (C3).
- GlobeCore (`external/`) is not changed.

## Rulings (spec, "Hub arcs side by side")

- **H1** (owner): 2D lines, side by side, one lift; withdraws P5-14 and R14.
- **H2**: two arcs run together when at least half of the shorter one's points more than 12 px from its start lie within
  6 px of the other.
- **H3**: the `sudan-help` test asks for centre lines at least 3 px apart (the line width), not the plan's 3.5: at one
  point, 23 px from the start, the port and depot lines come to 3.45 px, because Port Sudan's path is not exactly
  parallel to Dubai's. The port line is on top with its own dark edge, so it is never covered.

The rulings are H1 to H3, not R18 to R20 as the plan said: R18 to R20 are already used in earlier plans' tables of the
same spec. The Task 2 commit message still says "R18, R19"; it means H1 and H2.

## Tests

- New: `HubArcScreenTests` (8: visibility behind the globe, runs cut at the edge, running together, a tiny arc not moved,
  two and three arcs spread in order, the sideways shift, the drawing order) and, in `HubArcGeometryTests`,
  `PortArcBesideDepotArcInSudanHelp` (the `sudan-help` camera rebuilt in the test: the port line stays at least 3 px from
  the depot line along its whole length, ends within 3 px of Port Sudan, the Dongola line is not moved, the depot line is
  drawn first), `ArcsDrawUnderLabels`, `ClearsWhenNoArcs`, `OneLiftForEveryKind`, `ArcsAboveTheMarkers`.
- Removed: `KindsLiftedDifferently`, `PortArcAboveDepotArcFromSudan` (it measured 3D height, not what the camera sees),
  `RibbonIsAStripOfTheGivenWidth`, `ArcsAreWideAndAboveTheMarkers` (replaced).
- `dotnet build -c Release --no-incremental`: 0 warnings, 0 errors. `dotnet test -c Release`: 839 passed, 0 failed.

## Pictures (1 launch of the 2 allowed; the spare was not used)

Published self-contained exe (`dotnet publish src/EarthRelief3D.App -c Release -r win-x64 --self-contained true -o
artifacts\publish-hubarcs`), then `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes
tests\SnapshotScenes\hub-arcs.json --snapshot-out artifacts\snapshot-hubarcs`: 3 pictures, exit code 0. No screen
capture: the pictures are the app's own RenderTargetBitmap.

- **`sudan-help`:** the teal port arc is now visible from Sudan's label point to Port Sudan, beside the blue depot arc,
  with a dark line between them; the blue arc continues to Dubai; the white Dongola arc leaves northwards on its own path.
  Seen at 5x and 10x enlargement. **Still there:** the "Dongola Airport · 337 km straight-line" label crosses the last
  third of the port arc and the depot arc (the labels are drawn above the arcs, as before); the teal line shows between
  the letters but is less clear there. The label placement was not part of this change.
- **`yemen-help`:** the arcs to Aden, Sanaa airport and the Dubai depot leave in different directions and are drawn on
  their true paths, not moved.
- **`sudan-help-tilted`** (looking down on 45 N 5 E): **not useful.** From that angle Sudan lies under the panel, so its
  arcs are hidden; the arcs seen at an angle and an arc crossing the globe's edge were **not checked in a picture** (the
  edge cut is covered by `VisibleRunsStopAtTheEdge` only).

## Not checked

- An arc seen at a steep angle, or crossing the globe's edge, in a picture (see above).
- The app in its normal mode (moving and zooming the globe with the arcs shown): not launched.
- Other countries' arcs, beyond Sudan and Yemen.
- The camera in `PortArcBesideDepotArcInSudanHelp` is rebuilt from the app's camera rules for a 1482 x 903 window; it
  matches the picture closely (the arcs start at the same place on screen) but was not compared pixel by pixel.

## Possible follow-up

- Keep distance labels off the arcs (the Dongola label over the port arc in `sudan-help`).
