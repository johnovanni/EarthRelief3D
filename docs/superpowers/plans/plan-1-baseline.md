# Plan 1 baseline (recorded 2026-11-21, before any GlobeCore change)

## Test totals (`dotnet test`, Debug)

| Repo / branch | Passed | Skipped | Failed | Total |
|---|---|---|---|---|
| AstroCore, branch `bodies` | 36 | 0 | 0 | 36 |
| EarthWatch3D, branch `globecore`, before the new test | 771 | 0 | 1 | 772 |

The one EarthWatch3D failure is `GlobeImageComposerPerfTests.Composes8192UnderBudget` ("450 ms", a timing budget in a Debug
build); it failed before any change of this plan, so it is part of the baseline. After Task 1 (one new test,
`LaunchOptionsTests.ParsesSnapshotScenes`): 772 passed, 0 skipped, 1 failed (the same one), total 773.

## Snapshot baseline (retaken, deterministic)

`artifacts\snapshot-baseline` (run 1) and `artifacts\snapshot-baseline-2` (run 2) in EarthWatch3D, 7 scenes each, taken with
`--offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json`; pictures are 1357 x 853.

**PngDiff between the two runs: all 7 identical (0 pixels differ). Noise floor: 0. Use `--tolerance 0` (the default).**

How it was made deterministic (the first, unfrozen baseline differed by up to 1119 pixels / delta 247):
- the pulsing quake rings were drawn from a running stopwatch; in a scenes run they are drawn at a fixed phase;
- the app clock ran on in real time (sun, moon, readout); the display clock now stands still at each scene's time;
- the feeds' clock stands still at the first scene's time for the whole run, so the schedule does not run the feeds again
  after the clock jumps (a second try that moved one shared clock still differed in the status dots and the download meter,
  576 pixels, delta 90, in dateline-storm and night-asia, because the feeds re-ran at the day change).

## Test totals after Task 1 (EarthWatch3D)
See the first table: 771 passed before the new tests, plus 1 (ParsesSnapshotScenes) and 7 (SnapshotScenesTests) = 779 passed,
1 failed (the same perf test), total 780.

## Test totals after Task 8 (EarthWatch3D on GlobeCore, without the split)

| Repo / commit | Passed | Skipped | Failed | Total |
|---|---|---|---|---|
| AstroCore, `bodies` 9c0580a | 51 | 0 | 0 | 51 |
| GlobeCore, `main` 07ebd5e (and 6b29fbc, after ruling R19) | 258 | 0 | 0 | 258 |
| EarthWatch3D, `globecore` (Task 8 working tree; rerun at a5bd19b: 588 passed, the timing test passed that run) | 587 | 0 | 1 | 588 |
| **Total** | **896** | **0** | **1** | **897** (baseline 816) |

The one failure is still `GlobeImageComposerPerfTests.Composes8192UnderBudget` (Debug timing budget). EarthWatch3D lost
196 tests whose copies now run in GlobeCore (the mapping is in task-8-report.md) and gained 4
(`AppInfoTests.EveryDataPathIsUnchanged` x2, `AppInfoTests.ValuesPassedToGlobeCoreAreUnchanged`,
`ViewStateTests.EarthRadiusMatchesTheEarthPack`): 780 - 196 + 4 = 588. Snapshot check after Task 8: PngDiff
`artifacts\snapshot-baseline` vs `artifacts\snapshot-task8`, all 7 identical (tolerance 0).

## Final test totals (Task 10, Release, `dotnet test -c Release`, 2026-11-21)

| Repo / commit | Passed | Skipped | Failed | Total |
|---|---|---|---|---|
| AstroCore, `bodies` 9c0580a | 51 | 0 | 0 | 51 |
| GlobeCore, `main` 0ab857c | 258 | 0 | 0 | 258 |
| EarthWatch3D, `globecore` (working tree on ac2cb14, docs only) | 594 | 0 | 0 | 594 |
| **Total** | **903** | **0** | **0** | **903** (baseline 816, no new skips) |

`GlobeImageComposerPerfTests.Composes8192UnderBudget` passed in this Release run (it is a Debug timing budget and flaky
there). About-window check: `artifacts\about-task10.png` lists NASA GIBS, NASA Blue Marble, Natural Earth and Plate
boundaries (PB2002), and the Licence texts button is present.

## Owner's hands-on check for the SceneController split (pending)


Run `dotnet run --project src/EarthWatch3D.App -c Release` (or with `--offline-samples tests\Samples` for events) and
check, from docs/manual-test.md:
1. Left-drag rotates; close in, the same drag turns it less.
2. Wheel zooms in to about 300 km and out to about 20 Earth radii without jumps.
3. Home swings back to 20° N, 40° W; double-click turns a point to the centre (also near each pole: no spin or flip).
4. Face Sun and Face Moon centre their markers.
5. Hover a quake: tooltip "M… · … km deep · … ago"; click it: details panel. Hover and click a storm (glyph, tracks,
   cone), an alert area, an EONET dot.
6. Click empty ocean, the × or Esc closes the panel.
7. Move the mouse onto the card or out of the window: the tooltip closes and does not come back by itself.
8. Hover the Sun or Moon marker: its own tooltip; clicking it does not select an event.
9. Layer strip: every toggle (quakes, cyclones, alerts, EONET, coastlines, plates, lightning, clouds) hides and shows its
   layer at once; plates appear the first time after a moment.
10. Night-shade slider drags smoothly; day/night line toggle removes the gold line.
11. Time bar: step back, slider and play move the night side and events; lightning hides in replay.
12. Minimize and restore: the Sun/Moon arrows are back, the globe is current.


## Rulings made by the controller while executing Plan 1 (from the SDD ledger)

- R1: Branches created by the controller (globecore, bodies) before Task 1 — Step 1 then only records baseline counts — cost if wrong: none.
- R2: Test-local frozen copies of old GlobeMesh / downscale code are intended oracles, not duplication defects — the plan mandates them as references — cost: a reviewer flag to adjudicate.
- R3: If a Horizons sub-point misses 0.05°, the implementer reports the measured error; the controller keeps 0.05° (never loosens), and parks the item for the owner at the end of the plan rather than halting; dependent tasks continue (GlobeCore does not depend on Mars/Moon accuracy) — cost: owner may want a different model later.
- R4: Task 9 Step 5 (owner's hands-on check) is recorded as "pending owner" and execution continues; it is listed in the final report — cost: an input bug found later.
- R5: Each implementer may launch the app only for the snapshot runs the plan names (T1: 2, T8: 1, T9: 1, T10: 1); extra diagnostic launches need a reason in the report — cost: slower debugging.
- Task 1: implementer DONE_WITH_CONCERNS c275b2b; baseline nondeterministic (≤1119 px, delta 247). Ruling R6: freeze clock per scene + retake baseline before review — parity proof is the spec's acceptance test (§3.6) — cost: 2–4 extra launches. Pre-existing failure: GlobeImageComposerPerfTests.Composes8192UnderBudget (Debug timing) — expected to stay failing (R7: not this plan's concern; note in final report).
- Ruling R8: BodySubPoints.SubObserverPoint (light time) implemented in Task 2 rather than split across T2/T3 — the brief declares it in T2 — cost: T3 reviewer sees it as pre-existing.
- Task 3: implementer DONE_WITH_CONCERNS 9c0580a; worst errors Moon 0.012°, Mars 0.029°, Sun 0.004°. Ruling R9: Moon uses the IAU 2009 E1–E13 series (the 2015 report defers the Moon to it), via pck00011 — no 2015 Moon series exists — cost: none known. Review dispatched (sonnet) with element spot-check vs pck00011.
- Ruling R10: GlobeCore gets no LICENSE file — AstroCore and EarthWatch3D have none and the licence is the owner's choice — cost: owner adds one before publishing (final report item).
- Ruling R11: ISettingsStore<T>/SettingsSaver<T> made generic (the originals were typed to Earth's AppSettings) — required to keep Earth code out — cost: Task 8 uses ISettingsStore<AppSettings>.
- Ruling R12: idle note injected via FeedScheduler ctor (it creates the FeedStatus values) instead of a FeedStatus ctor parameter — same effect — cost: none.
- Ruling R13: 5 tests not copied (AppSettings.DailyLimitBytes, embedded coastline, GeoMath distance, 2 composer sparse tests) stay in EarthWatch3D; Task 5 copies the GeoMath + composer ones with those types, Task 7 the coastline one, Task 8 keeps the AppSettings one — tracked so the count constraint holds.
- Task 4: review Approved with 2 Important: I1 FileLog hard-coded 'earthwatch3d-' prefix; I2 AtomicFile internal but EarthWatch3D prod code needs it. Ruling R14: make AtomicFile public (general utility, in the move list) rather than widening InternalsVisibleTo — cost: larger public surface. Minors deferred: Task 4: minor (deferred): app-specific '--snapshot-recompose' wording in Bgra32Image/OffsetClock docs; test project could be net10.0 (keep: later WPF tests); SnapshotStore doc on event ordering; 'ew3d-meter-' temp prefix. Fix round 1 dispatched to original implementer.
- Ruling R15: GlobeImageComposerPerfTests (the pre-existing Debug-timing failure) stays in EarthWatch3D, exercising the composer through the moved code — copying it would add a failing test to GlobeCore — cost: perf coverage lives in the app repo.
- Task 6: implementer DONE 565bb46, 195 tests. Carry into Task 8 (CARRY list): C1 DpapiProtector entropy must be "EarthWatch3D.EUMETSAT" (existing blobs must decrypt); C2 CrashHandler appName / PerfLog.Configure(appName) for EarthWatch3D names unchanged; C3 CameraRules take CameraLimits.EarthDefault; C4 FileLog prefix "earthwatch3d-"; C5 ISettingsStore<AppSettings>; C6 FeedHttp(userAgent,…), FeedScheduler idleNote; C7 IDialogService/DialogService stay in EarthWatch3D (app-coupled). Ruling R16: dialog services stay in the app — they depend on app windows/VMs — cost: none for Plan 1.
- Task 7: implementer DONE_WITH_CONCERNS a71d996, 253 tests. Ruling R17: accept the additive LayerHost/layer members (DisplayTimeUtc, IlluminationAt, RecomposeIfStale, RequestSceneUpdate, ComposedUtc, WhenFirstComposeDisplayed, SelectionCleared, CameraPoseChanged; ImageLayer InvalidateNow/PrepareForComposeAsync/OnComposeDisplayed; SceneLayer OnFrame/Invalidate; OverlayLayer Invalidate/Clear) — needed to carry today's behaviour; nothing briefed removed — cost: wider API. CARRY C8 (Task 9): host.Dispose() before StaWorker dispose; set HoverTip.Style. Asset README/PB2002 notice copied verbatim name EarthWatch3D (fix wording in Task 10 docs).
- Task 7: review Needs fixes (opus): I1 start-up ordering gives an extra first compose / base-only first compose; I2 no marshalling of off-thread invalidation. Ruling R18: marshal (Interlocked + BeginInvoke, as SceneController) for layer invalidation; VerifyAccess on host public compose methods — layers in Task 9 are fed by feed threads — cost: none. Task 7: minor (deferred): light read before await (ordering only); camera sub-point one frame late; per-frame ToArray/Projector allocs; IsComposeIdle stricter; wall-clock timing tests; speculative IVT EarthRelief3D.Tests in GlobeCore.Earth.csproj; Illumination setter overwritten doc. Fix round 1 sent to implementer.
- Task 8: implementer NEEDS_CONTEXT (uncommitted): plan defect — GlobeCore's Directory.Build.props shadows the app's, so AstroCoreRoot never reaches GlobeCore projects. Ruling R19: GlobeCore props imports the nearest parent Directory.Build.props via GetPathOfFileAbove (implementer's tested 3-line block) — standard MSBuild pattern; app's AstroCoreRoot wins — cost: app props (Version, short-root block) also apply to GlobeCore projects in app builds. R20: EarthSphere.RadiusKm duplicated in EarthWatch3D.Core, test pins = EarthBody.RadiusKm (Core can't ref WPF Earth pack). Counts so far: 51+258+588 = 897 ≥ 816; PngDiff 7/7 identical at tol 0. Resumed implementer to make the GlobeCore change and commit.
- Task 9: review Needs fixes (opus): I1 removed _firstCompose fault observer → later crash-style message after a load failure. Fix round 1 sent (+ minors 7, 8 folded in as same-file trivia). Ruling R21: accept reported diffs — light read before plate await (self-correcting ≤250 ms), diagnostics wait ≤250 ms longer, PERF_LOG compose lines in GlobeCore-perf.log (fix README.md:161, docs/perf.md:24 in Task 10) — not user-visible. OWNER DECISION D1: events/markers/hover now appear when the globe image appears (~1 s after start) instead of over the black viewport during decode — recommend accept. Task 9: minor (deferred): HoverBlockers copied once into host; ComposeFallbackTests duplicates GlobeCore tests. Owner manual check (R4) pending; checklist in task-9-report.md.
- Ruling R22: keep AstroCore.Earth.GeoPosition in GlobeCore's Geo/rasterizer APIs for Plan 1 (the plan kept it; spec §3.2 said SurfacePoint everywhere) — decide in Plan 2 before EarthRelief3D builds on it — cost: an API migration later.
- Ruling R23: make GlobeCore.Wpf PerfLog.Configure public and have EarthWatch3D call Configure("EarthWatch3D") so compose lines return to EarthWatch3D-perf.log (restores pre-plan behaviour; supersedes the R21 PERF_LOG item) — cost: docs from Task 10 re-edited.
- Ruling R24: F4 is a finishing/push concern, not code — handled at finish (no pushes without the owner) — cost: none.
