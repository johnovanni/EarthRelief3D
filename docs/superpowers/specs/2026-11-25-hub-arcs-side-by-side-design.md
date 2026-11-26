# Hub arcs side by side: design

Date: 2026-11-25. Branch `hub-arcs-side-by-side` (from `plan-5`). Size: a small change.

## Why

In the `sudan-help` picture the teal arc from Sudan's label point to Port Sudan is visible for only about 60 px and then
lies under the blue arc to the Dubai depot (Plan 5 owner item 15, ruling R14). The camera looks almost straight down on
Sudan, both arcs leave in nearly the same direction, and Port Sudan lies on the path to Dubai. Lifting the port arc
higher (P5-14, R14) cannot move it on screen from that camera, and the picture did not even follow the 3D heights: by
the numbers the port arc is nearer the camera, yet it was drawn under. The 3D height test passed while the picture
failed.

## Goal, non-goals, anti-goals

- **Goal:** every hub arc is clearly visible from end to end, in `sudan-help` and anywhere else arcs leave in similar
  directions. Done = the port arc clearly visible end to end in a new `sudan-help` picture, tests green with 0 warnings.
- **Non-goals:** new hubs, new labels, a different distance (C3 stays: distances on a 6,371 km sphere), changing
  GlobeCore.
- **Anti-goals:** a line that no longer follows its real path (the owner turned down bowed curves for that reason); a
  line that looks like it changes colour part-way (one arc lying exactly on another).

## Approaches (owner chose 1)

1. **Flat on the screen, side by side (chosen).** The arcs are drawn as 2D lines on the globe's overlay, along their
   true paths, each with a thin dark edge. Arcs that run together are drawn next to each other, about 4 px apart, like
   lines on a transit map. Each moves at most about 2 px off its true path (amended, H4: up to about 3 px where paths cross
   on screen, about 4 px for the outer arcs of three). A test can measure the gap on screen.
2. **Bowed apart.** Each arc curves to one side. Clearest, but the line no longer follows its real path. Turned down.
3. **Stay in 3D** (more height or tilt). The picture did not follow the heights; nothing a test checks would match what
   the camera shows. Turned down.

## The design

- `HubArcLayer` becomes an overlay layer (GlobeCore's `OverlayLayer`, as `HubLabelsLayer` is). Each frame it projects
  every arc's path to the screen and draws it as a WPF polyline: a 5 px dark edge (`LayerStyle.MarkerEdgeColor`) under a
  3 px line in the hub's colour, round joins and ends. It stays below the place names and distance labels (a lower
  Z-index on the shared canvas) and, being 2D, above every marker (final review F2 holds without the 1.008 clearance).
- **The path:** the same great circle with the same gentle rise as before (`ArcPoints`), one lift for every kind of
  hub. P5-14's per-kind lift factors and R14's tuning are withdrawn: from this camera they made no difference, and the
  separation now comes from the screen. The distance labels move with the arcs' midpoints, as now.
- **Behind the globe:** a part of an arc whose line of sight from the camera passes through the globe is not drawn
  (exact ray and sphere test), so an arc going over the horizon stops at the edge rather than showing through.
- **Side by side:** two arcs run together when much of the shorter one's screen path lies within a few pixels of the
  other's. Arcs that run together form a bundle; within a bundle they are pushed apart sideways, point by point, only
  where they are close, just enough that their centre lines are 4 px apart (each of a pair moving half), so where they
  lie on top of each other each moves about 2 px, and up to about 3 px where their true paths cross on screen or a hub
  lies on another arc. Each pair keeps one side all along (the side that needs less pushing; near-ties go by the bearing
  on the ground, so they do not swap sides as the globe turns), so they never cross. Arcs that leave in different
  directions are not moved. (Amended after the final review, ruling H4: the first version shifted each whole line by
  one offset, which pushed crossing lines onto each other in turned and zoomed views.)
- **Order:** longest first, shortest last, so where arcs meet at the start the shortest is on top.

## Constraints

- GlobeCore (`external/`) is not changed.
- Checked visually only with the app's own `--snapshot-scenes` pictures (`--offline-samples tests\Samples`), at most 2
  launches; never a screen capture.
- New rulings go in the design's ruling tables, in their own table (H1 onward): R18 to R20 are taken in earlier plans' tables.

## Left to the builder

- The exact thresholds for "run together" (distance and share of the path), within the rule above.
- How the test builds the `sudan-help` camera (the app's own look-at code if it can run without a window).
- Pooling of the polylines on the canvas.
