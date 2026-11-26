# Hub arcs side by side: results

2026-11-25, branch `hub-arcs-side-by-side` (from `plan-5` 8d036a4), not pushed. Owner item 15 of Plan 5: the teal port
arc from Sudan to Port Sudan lay under the blue arc to the Dubai depot in `sudan-help`.

Design: `docs/superpowers/specs/2026-11-25-hub-arcs-side-by-side-design.md` (the owner chose "side by side" over bowed
curves and further 3D tuning). Plan: `docs/superpowers/plans/2026-11-25-hub-arcs-side-by-side.md`.

## What changed

- The hub arcs are drawn as **2D lines on the globe's overlay** (`HubArcLayer` is now an `OverlayLayer`): a 3 px line in
  the hub's colour over a 5 px dark edge, under the place names and distance labels, with the parts behind the globe cut.
  The overlay canvas has a soft black drop shadow (`MainWindow.xaml`), so the lines now carry that halo too.
- **Arcs that run together on screen are pushed apart where they are close** (`HubArcScreen.SideBySide`): point by point,
  just enough for 4 px between their centre lines, each of a pair moving half; where they are already apart nothing moves.
  The longest is drawn first, so the shortest is on top where they meet. Each pair keeps one side all along: the side
  that needs less pushing, or, in a near-tie, the side the bearing on the ground gives, so they do not swap as the globe
  turns.
- **How far a line moves off its true path:** about 2 px where two lines lie on top of each other; up to about 3 px where
  their screen paths cross or a hub lies on the other arc (Port Sudan lies on the Dubai line on screen, so the port line
  ends about 3.1 px from the port's centre, on its 4 px marker); about 4 px for the outer lines of three that run together.
- **One lift for every kind of hub** again: P5-14's per-kind factors (port 2.0, airport 1.25, depot 0.5) are withdrawn.
  The distance labels still sit at the arcs' midpoints; distances are unchanged (C3).
- GlobeCore (`external/`) is not changed.

## Rulings (spec, "Hub arcs side by side")

- **H1** (owner): 2D lines, side by side, one lift; withdraws P5-14 and R14.
- **H2**: two arcs run together when at least half of the shorter one's points more than 12 px from its start lie within
  6 px of the other.
- **H3**: the `sudan-help` test asks for centre lines at least 3 px apart (the line width; the plan said 3.5) and a port
  line ending within 4 px of Port Sudan (the marker's radius; the plan said 3).
- **H4** (after the final review): the point-by-point push replaces the plan's single sideways offset per line. The
  reviewer found that on screen the port arc can start 1-2 px on one side of the depot arc and leave on the other, so
  one offset for the whole line pushed them onto each other in turned or zoomed views (0.02 px apart looking down on
  0 N 35 E, zoomed in). The cost is the larger moves listed above, and a line can still change side with a small jump
  when the two sides' needs cross outside the 1 px tie band.

The rulings are H1 to H4, not R18 onward as the plan said: R18 to R20 are already used in earlier plans' tables of the
same spec. The commit "Hub arcs drawn on the overlay..." still says "R18, R19"; it means H1 and H2.

## Tests

- New: `HubArcScreenTests` (11: visibility behind the globe, runs cut at the edge, running together, a tiny arc not
  moved, two and three arcs pushed apart in order, crossing lines kept apart, the bearing tie-break, the sideways shift,
  the drawing order) and, in `HubArcGeometryTests`: `PortArcBesideDepotArcInSudanHelp` (the `sudan-help` camera rebuilt in
  the test: the port line stays at least 3 px from the depot line along its whole length, ends within 4 px of Port Sudan,
  the Dongola line is not moved, the depot line is drawn first); `PortArcBesideDepotArcWhenTheGlobeIsTurned` (five
  turned or zoomed cameras from the review); `ArcsDrawUnderLabels`; `ClearsWhenNoArcs`; `OneLiftForEveryKind`;
  `ArcsAboveTheMarkers`.
- Removed: `KindsLiftedDifferently`, `PortArcAboveDepotArcFromSudan` (it measured 3D height, not what the camera sees),
  `RibbonIsAStripOfTheGivenWidth`, `ArcsAreWideAndAboveTheMarkers` (replaced).
- `dotnet build -c Release --no-incremental`: 0 warnings, 0 errors. `dotnet test -c Release`: 846 passed, 0 failed.
  One earlier full run had `AppStartupTests.CachedIpcAreasLoadOffTheUiThreadAndReachTheMap` fail; it passed alone three
  times and in the next full run. It is the known flaky start-up test another session is fixing; it does not touch the arcs.

## Final review

A fresh reviewer read the whole branch and probed 585 cameras. Its one Important finding (I1, the side chosen from the
whole line) is fixed by H4, test-first: the five-camera test failed (0.02, 1.20 and 2.81 px), then passed. Its note that
"at most about 2 px" was not true for three lines is fixed in the design and rulings. Left as they are (minor): a run
stops at the last visible point, up to 1 degree short of the globe's edge; the line pool belongs to the first canvas it
is given (the app's never changes); a few test gaps (no label on the canvas in `ArcsDrawUnderLabels`; hiding the layer
not tested; arcs of 13-20 px bundle on one or two points); a stale comment at `SceneController.cs:187`.

## Pictures (2 launches of the 2 allowed)

Published self-contained exe (`dotnet publish src/EarthRelief3D.App -c Release -r win-x64 --self-contained true -o
artifacts\publish-hubarcs`), then `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes
tests\SnapshotScenes\hub-arcs.json --snapshot-out <folder>`, exit code 0 both times. No screen capture: the pictures are
the app's own RenderTargetBitmap.

1. `artifacts\snapshot-hubarcs` (the first version, one offset per line). `sudan-help`: the port line was visible beside
   the depot line end to end. A tilted scene (looking down on 45 N 5 E) was useless: Sudan lay under the panel.
2. `artifacts\snapshot-hubarcs-2` (after the review fix; the tilted scene replaced by `sudan-help-turned`, looking down on
   0 N 35 E):
   - **`sudan-help`:** the teal port line runs beside the blue depot line, on its upper side, from Sudan's label point
     to Port Sudan, with a dark line between them; the blue line goes on to Dubai with **a small jog of about 2 px where
     the port line ends** (the push fades there); the white Dongola line leaves northwards on its own path. Seen at 5x.
   - **`sudan-help-turned`:** the teal line runs below the blue one, separate from start to end. Seen at 5x.
   - **`yemen-help`:** the arcs to Aden, Sanaa airport and the Dubai depot leave in different directions and are on
     their true paths (the file is byte for byte the same size as run 1's).
   - **Still there in both Sudan pictures:** the "Dongola Airport · 337 km straight-line" label crosses the port line's
     last part (the labels are drawn above the arcs, as before); the teal shows between the letters but is less clear
     there. The label placement was not part of this change.

## Not checked

- An arc crossing the globe's edge, in a picture (covered by `VisibleRunsStopAtTheEdge` only).
- The app in its normal mode (dragging and zooming the globe with the arcs shown): not launched, so a side jump while
  turning was not seen either way.
- Other countries' arcs, beyond Sudan and Yemen.
- The cameras in the tests are rebuilt from the app's camera rules for a 1482 x 903 window; they match the pictures
  closely (the arcs start at the same place on screen) but were not compared pixel by pixel.

## Possible follow-ups

- Keep distance labels off the arcs (the Dongola label over the port line in `sudan-help`).
- Ease the push in over a few more pixels where a line ends, to soften the 2 px jog in the depot line.
