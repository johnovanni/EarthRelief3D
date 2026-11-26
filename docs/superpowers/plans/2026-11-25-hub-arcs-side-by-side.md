# Hub Arcs Side by Side Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Draw the hub arcs as 2D lines on the globe's overlay, side by side where they run together, so the port arc in `sudan-help` is visible from end to end.

**Architecture:** A new pure class `HubArcScreen` turns each arc's 3D path into screen polylines (hidden parts cut), finds arcs that run together and gives each a sideways offset. `HubArcLayer` becomes a GlobeCore `OverlayLayer` that draws those polylines (dark edge under a coloured line) on the shared overlay canvas, below the labels. Per-kind lift factors are removed.

**Tech Stack:** C# / .NET 10 WPF, GlobeCore (`external/`, not changed), xUnit.

**Spec:** `docs/superpowers/specs/2026-11-25-hub-arcs-side-by-side-design.md`

## Global Constraints

- GlobeCore (`external/`) is not changed.
- Visual check only with the app's own `--snapshot-scenes` pictures (`--offline-samples tests\Samples`), at most 2 launches; never a screen capture.
- Line: 3 px in the hub's colour over a 5 px edge in `LayerStyle.MarkerEdgeColor`; neighbours' centre lines 4 px apart.
- Build and tests: 0 warnings, all green (`dotnet test -c Release`).
- Commits on `hub-arcs-side-by-side`; no push.

## Review Focus

1. An arc that crosses the globe's edge (user turns the globe): the hidden part is not drawn and the visible part stops at the edge (`VisibleRuns` test in Task 1).
2. A very short arc (a few pixels) next to a long one: it is not shifted on a guess, and nothing throws (`TinyArcIsNotBundled`, Task 1).
3. Three arcs in one bundle: offsets −4, 0, +4 in the order of their sideways position, never crossing (`ThreeArcsFanInOrder`, Task 1).
4. The panel closes or the toggle goes off: every line is removed from the canvas (`ClearsWhenNoArcs`, Task 2).
5. Labels and arcs on one canvas: the arcs stay under every label (`ArcsDrawUnderLabels`, Task 2).

---

### Task 1: Screen geometry (`HubArcScreen`)

**Files:**
- Create: `src/EarthRelief3D.App/Layers/HubArcScreen.cs`
- Test: `tests/EarthRelief3D.Tests/HubArcScreenTests.cs`

**Interfaces:**
- Produces (all `public static` in `HubArcScreen`):
  - `const double Spacing = 4;` (px between neighbours' centre lines), `const double NearPx = 6;`, `const double ShareTogether = 0.5;`
  - `bool IsVisible(Point3D p, Point3D eye)`: false when the segment eye→p meets the unit sphere before p (ray–sphere, exact).
  - `IReadOnlyList<IReadOnlyList<Point>> VisibleRuns(IReadOnlyList<Point3D> path, Projector projector)`: the projected path split into runs of consecutive visible points (runs of fewer than 2 points dropped).
  - `bool RunTogether(IReadOnlyList<Point> a, IReadOnlyList<Point> b)`: take the shorter polyline (by length); of its points farther than `2 × NearPx` from its first point, at least `ShareTogether` lie within `NearPx` of the other polyline. False if there are no such points.
  - `double[] SideOffsets(IReadOnlyList<IReadOnlyList<Point>> lines)`: one offset (px) per line. Bundles = connected groups under `RunTogether`. In a bundle, the longest line is the reference (position 0); each other line's position is the mean signed distance of its points to the reference (sign = `cross(b − a, p − a)` of the nearest reference segment, so positive means the +normal side used by `Shift`). Sort by position (ties: input order); slot i gets `(i − (n − 1) / 2) × Spacing`. Lines in no bundle get 0.
  - `IReadOnlyList<Point> Shift(IReadOnlyList<Point> line, double offset)`: each point moved by `offset` along the unit normal `(−dy, dx)` of the local direction (central difference; ends one-sided).
  - `int[] DrawOrder(IReadOnlyList<IReadOnlyList<Point>> lines)`: indices longest first (ties: input order).

- [ ] **Step 1: Write the failing tests** in `HubArcScreenTests`:
  - `IsVisibleFrontAndBack`: eye (0,0,3): (0,0,1.01) visible, (0,0,−1.01) not, (1.01,0,0) visible (on the limb side, sight line misses the sphere), (0.3,0,−0.96) not.
  - `VisibleRunsStopAtTheEdge`: camera `CameraRules.LookDownOn(new GeoPosition(0, 0), 3)`, FOV 45, 1000×800; `ArcPoints((0, 60), (0, 120))`: one run; every run point is the projection of a point with `IsVisible` true; the run has fewer points than the path.
  - `ParallelLinesRunTogether`: a = (0,0)→(100,0), b = (0,2)→(60,2) (10 points each): true. c = (0,0)→(0,100): `RunTogether(a, c)` false.
  - `TinyArcIsNotBundled`: d = (0,0)→(8,0): `RunTogether(a, d)` false; `SideOffsets([a, d])` = [0, 0].
  - `TwoArcsSplitApart`: `SideOffsets([a, b])`: b lies on the +normal side of a (with y down, normal of +x is (0,1), so b at y = 2 is +): a → −2, b → +2.
  - `ThreeArcsFanInOrder`: a, b (y = +2), e = (0,−2)→(70,−2): offsets e −4, a 0, b +4.
  - `ShiftMovesAlongTheNormal`: `Shift(a, 2)` all y = 2, x unchanged.
  - `DrawOrderLongestFirst`: `DrawOrder([b, a, d])` = [1, 0, 2].

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/EarthRelief3D.Tests -c Release --filter HubArcScreenTests`
Expected: build error, `HubArcScreen` does not exist.

- [ ] **Step 3: Implement `HubArcScreen`** with the signatures above. Point-to-polyline distance: minimum over segments of the clamped projection.

- [ ] **Step 4: Run them to see them pass** (same command). Expected: 8 passed.

- [ ] **Step 5: Commit** `Hub arcs: screen geometry for side-by-side lines`.

### Task 2: `HubArcLayer` as an overlay

**Files:**
- Modify: `src/EarthRelief3D.App/Layers/HubArcLayer.cs` (base class, drawing; remove `LiftFactor`, `Ribbon`, `SurfaceClearance`, rescale fields)
- Modify: `src/EarthRelief3D.App/Layers/HubLabelsLayer.cs:63` (`ArcPoints(from, to)` with no factor)
- Modify: `src/EarthRelief3D.App/Controllers/SceneController.cs` if the constructor or `Changed` calls need it
- Test: `tests/EarthRelief3D.Tests/HubArcGeometryTests.cs` (remove `KindsLiftedDifferently`, `PortArcAboveDepotArcFromSudan`, `RibbonIsAStripOfTheGivenWidth`; replace `ArcsAreWideAndAboveTheMarkers`), `tests/EarthRelief3D.Tests/AppStartupTests.cs:280-288` only if it no longer compiles

**Interfaces:**
- Consumes: Task 1's `HubArcScreen`.
- Produces:
  - `HubArcLayer : OverlayLayer`, `Order = 150` (unchanged), `WidthPx = 3`, `EdgeWidthPx = 5`, `ZIndex = -1`; constructor unchanged (`Func<IReadOnlyList<HubArcSpec>>`), `Changed()` and `ArcCount` kept.
  - `public static IReadOnlyList<(HubArcSpec Spec, IReadOnlyList<Point> Line)> Lines(IReadOnlyList<HubArcSpec> specs, Projector projector)`: per spec, `ArcPoints(from, hub)` → `VisibleRuns` (each run is its own line, same spec), offsets from `SideOffsets` over all runs, `Shift`ed, returned in `DrawOrder`.
  - `ArcPoints(SurfacePoint from, SurfacePoint to)`: the `liftFactor` parameter removed; formula as before with k = 1.

- [ ] **Step 1: Write the failing tests** (in `HubArcGeometryTests`, STA via `StaAsync.Sta` where WPF objects are made):
  - `PortArcBesideDepotArcInSudanHelp`: the `sudan-help` camera = `CameraRules.LookDownOn(new GeoPosition(15, 35), CameraRules.FitDistance(45, 1482.0 / 903, CameraLimits.EarthDefault))`, FOV 45, projector 1482×903; specs = Sudan's label point (`CountryResolver.LoadDefault().ByIso3("SDN")!.LabelPoint`) to Port Sudan (19.6, 37.2, `HubKind.Port`), the Dubai depot (24.896171, 55.16235, `HubKind.UnDepot`) and Dongola airport (19.15, 30.43, `HubKind.Airport`). Assert: every point of the port line is ≥ 3.5 px from the depot line (point-to-polyline); the port line's last point is within 3 px of Port Sudan's projected surface point; the airport line is unshifted (its first point equals the projected start within 0.01 px); the port line comes after the depot line in `Lines`.
  - `ArcsDrawUnderLabels`: render the layer into a `Canvas` with the projector above: 6 children (`Polyline`), each `Panel.GetZIndex` < 0; edges `StrokeThickness` 5 in `MarkerEdgeColor`, lines 3 in `HubColor(kind)`; for each arc its edge comes before its line in `Children`.
  - `ClearsWhenNoArcs`: render with 3 specs, then with none: no `Polyline` left visible on the canvas, `ArcCount` 0.
  - `OneLiftForEveryKind`: replaces `KindsLiftedDifferently`; `ArcPoints((0,0),(0,6))` mid height = `1 + 0.25 × 6π/180` (3 places).
  - `ArcsAboveTheMarkers` (replaces `ArcsAreWideAndAboveTheMarkers`): `WidthPx` 3, `HubArcLayer` is an `OverlayLayer`, `Order` between every marker layer's and `LabelsOverlayLayer.Order`.

- [ ] **Step 2: Run to see them fail**

Run: `dotnet test tests/EarthRelief3D.Tests -c Release --filter "HubArcGeometryTests"`
Expected: build errors (`Lines`, `EdgeWidthPx` missing).

- [ ] **Step 3: Implement.** `Render(Canvas, Projector)`: pool `Polyline`s (two per line) added to the canvas on first use, `Panel.SetZIndex(-1)`, frozen brushes, `StrokeLineJoin` and caps `Round`; unused pool entries collapsed. `Clear` collapses all. `Changed()` calls `Invalidate()`. Keep the existing `HubArcSpec`, `SpecsFor` and `MidPoint`.

- [ ] **Step 4: Run the whole suite**

Run: `dotnet build -c Release` then `dotnet test -c Release`
Expected: 0 warnings, all tests pass.

- [ ] **Step 5: Commit** `Hub arcs drawn on the overlay, side by side where they run together (R18, R19)`.

### Task 3: Picture, rulings and results note

**Files:**
- Modify: `docs/superpowers/specs/2026-11-21-earthrelief3d-design.md` (ruling table: R18, R19)
- Create: `docs/hub-arcs-results.md`
- Create: `tests/SnapshotScenes/hub-arcs.json` (the `sudan-help` scene, plus `sudan-help-tilted`: same, `lookAt` [35, 10], to see arcs at an angle)

- [ ] **Step 1: Add the rulings.** R18: the hub arcs are 2D lines on the overlay, side by side where they run together (4 px apart, at most about 2 px off their true path); withdraws P5-14 and R14 (one lift for every kind). R19: the thresholds (`NearPx` 6, `ShareTogether` 0.5, the first 12 px ignored).

- [ ] **Step 2: Publish and take the pictures (launch 1 of 2).**

Run: `dotnet publish src/EarthRelief3D.App -c Release -o artifacts\publish-hubarcs` then `artifacts\publish-hubarcs\EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\hub-arcs.json --snapshot-out artifacts\snapshot-hubarcs`
Expected: two PNGs. Look at `sudan-help.png`: the teal port arc visible from Sudan to Port Sudan beside the blue depot arc. Launch 2 is spare, only for one fix.

- [ ] **Step 3: Write `docs/hub-arcs-results.md`:** what changed, what the picture shows (with what was and was not checked), the rulings.

- [ ] **Step 4: Commit** `Hub arcs: rulings, pictures and results`.
