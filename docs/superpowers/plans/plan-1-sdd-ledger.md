# SDD ledger — plan: C:\repos\EarthRelief3D\docs\superpowers\plans\2026-11-21-plan-1-globecore.md

Spec: C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md (reachable).
Repos/branches: EarthWatch3D `globecore` (from main 18b7296), AstroCore `bodies` (from 54a76ee), GlobeCore new repo `main` (Task 4).
Owner OK'd local commits on these branches (2026-11-21); nothing pushed.

## Pre-flight scan

| Pair / task | Produces vs consumes | Finding |
|---|---|---|
| T1 self | branch creation in Step 1 | Controller created both branches before dispatch → R1 |
| T2→T3 | IBodyRotation, BodySubPoints (Func<double,Vector3d>) used by IAU tests | consistent |
| T3→T4 | GlobeCore submodule pinned to `bodies` tip | consistent; T4 must pin after T3 commits |
| T2/T3→T5 | Body.SubSolarPoint calls BodySubPoints.SubSolarPoint(Func?,…) | consistent after plan edit |
| T4→T5 | Bgra32Image/SparseOverlay moved in T4, composer in T5 | consistent |
| T4→T8 | AppDataRoot paths vs AppPaths(AppDataRoot Data) | consistent |
| T5→T9 | ImagePass/Order values (clouds S10, areas S20, lines A10) | T5 adapter maps dense lines A10 + sparse A20; T9 uses one merged sparse lines layer A10 — matches today's merged SparseOverlay; OK |
| T6 self | BodyMeshTests pastes old GlobeMesh as reference | Verbatim duplication by design → R2 |
| T7 self | BlueMarbleDownscaleMatchesOld pastes old code | same → R2 |
| T7→T8/T9 | LayerHost, EarthAssets, EarthBody | consistent |
| T3 self | Step 6 "stop for owner" if 0.05° missed | → R3 |
| T9 self | Step 5 owner manual mouse check | human step → R4 |
| Global | app launches limited (antivirus) | → R5 |

Rulings:
- R1: Branches created by the controller (globecore, bodies) before Task 1 — Step 1 then only records baseline counts — cost if wrong: none.
- R2: Test-local frozen copies of old GlobeMesh / downscale code are intended oracles, not duplication defects — the plan mandates them as references — cost: a reviewer flag to adjudicate.
- R3: If a Horizons sub-point misses 0.05°, the implementer reports the measured error; the controller keeps 0.05° (never loosens), and parks the item for the owner at the end of the plan rather than halting; dependent tasks continue (GlobeCore does not depend on Mars/Moon accuracy) — cost: owner may want a different model later.
- R4: Task 9 Step 5 (owner's hands-on check) is recorded as "pending owner" and execution continues; it is listed in the final report — cost: an input bug found later.
- R5: Each implementer may launch the app only for the snapshot runs the plan names (T1: 2, T8: 1, T9: 1, T10: 1); extra diagnostic launches need a reason in the report — cost: slower debugging.

## Progress
- Task 1: dispatched implementer (sonnet), BASE 18b7296 (EarthWatch3D globecore)
- Task 1: implementer DONE_WITH_CONCERNS c275b2b; baseline nondeterministic (≤1119 px, delta 247). Ruling R6: freeze clock per scene + retake baseline before review — parity proof is the spec's acceptance test (§3.6) — cost: 2–4 extra launches. Pre-existing failure: GlobeImageComposerPerfTests.Composes8192UnderBudget (Debug timing) — expected to stay failing (R7: not this plan's concern; note in final report).
- Task 1: review dispatched (sonnet) on 18b7296..dd0d825
- Task 2: dispatched implementer (sonnet), BASE 54a76ee (AstroCore bodies)
- Ruling R8: BodySubPoints.SubObserverPoint (light time) implemented in Task 2 rather than split across T2/T3 — the brief declares it in T2 — cost: T3 reviewer sees it as pre-existing.
- Task 1: complete (commits 18b7296..dd0d825, review clean; ⚠️ items checked: baseline file OK, Save creates folder). Parked minors (carry into Task 9, which edits SnapshotDiagnostic): P1 RunScenesAsync outer catch(Exception) → log + exit 1 (hang risk); P2 LoadScenes UnauthorizedAccessException; P3 FrozenRingTime placement; P4 using order; P5 no test for scenes/out pairing.
- Task 2: implementer DONE 9ffaa0b; review dispatched (sonnet)
- Task 2: complete (commits 54a76ee..9ffaa0b, review clean; trailer checked). Parked minor P6: Local→UTC + Earth arithmetic duplicated between SubPoints and EarthRotationModel (brief mandated same arithmetic).
- Task 3: dispatched implementer (opus — accuracy-critical transcription + Horizons), BASE 9ffaa0b (AstroCore bodies)
- Task 3: implementer DONE_WITH_CONCERNS 9c0580a; worst errors Moon 0.012°, Mars 0.029°, Sun 0.004°. Ruling R9: Moon uses the IAU 2009 E1–E13 series (the 2015 report defers the Moon to it), via pck00011 — no 2015 Moon series exists — cost: none known. Review dispatched (sonnet) with element spot-check vs pck00011.
- Task 3: complete (commits 9ffaa0b..9c0580a, review clean; pck00011 element check: no mismatch; Mars elements match SolarSystem3D). Parked P7: BodySubPoints.SubSolarPoint has no observer-aware (retarded-time) overload — callers showing "as seen from Earth" must subtract light time; P8: pole test covers latitude only.
- Ruling R10: GlobeCore gets no LICENSE file — AstroCore and EarthWatch3D have none and the licence is the owner's choice — cost: owner adds one before publishing (final report item).
- Task 4: dispatched implementer (sonnet), new repo C:\repos\GlobeCore; AstroCore submodule pinned to 9c0580a
- Task 4: implementer DONE_WITH_CONCERNS cb749ab (GlobeCore main), 67 tests.
  - Ruling R11: ISettingsStore<T>/SettingsSaver<T> made generic (the originals were typed to Earth's AppSettings) — required to keep Earth code out — cost: Task 8 uses ISettingsStore<AppSettings>.
  - Ruling R12: idle note injected via FeedScheduler ctor (it creates the FeedStatus values) instead of a FeedStatus ctor parameter — same effect — cost: none.
  - Ruling R13: 5 tests not copied (AppSettings.DailyLimitBytes, embedded coastline, GeoMath distance, 2 composer sparse tests) stay in EarthWatch3D; Task 5 copies the GeoMath + composer ones with those types, Task 7 the coastline one, Task 8 keeps the AppSettings one — tracked so the count constraint holds.
  - P9: FeedSchedulerTests.SlowFeedDoesNotDelayOthers race fixed in GlobeCore copy only; the EarthWatch3D copy is deleted in Task 8 anyway.
  - Review dispatched (sonnet).
- Task 4: review Approved with 2 Important: I1 FileLog hard-coded 'earthwatch3d-' prefix; I2 AtomicFile internal but EarthWatch3D prod code needs it. Ruling R14: make AtomicFile public (general utility, in the move list) rather than widening InternalsVisibleTo — cost: larger public surface. Minors deferred: Task 4: minor (deferred): app-specific '--snapshot-recompose' wording in Bgra32Image/OffsetClock docs; test project could be net10.0 (keep: later WPF tests); SnapshotStore doc on event ordering; 'ew3d-meter-' temp prefix. Fix round 1 dispatched to original implementer.
- Task 4: fix round 1/5 (2 addressed, 0 open; commits cb749ab..130d77d)
- Task 4: complete (commits root..130d77d, review clean). Task 4: minor (deferred): FileLog prune glob prefix overlap ("app-" matches "app-beta-…"); FileLog ctor doc missing <summary>.
- Ruling R15: GlobeImageComposerPerfTests (the pre-existing Debug-timing failure) stays in EarthWatch3D, exercising the composer through the moved code — copying it would add a failing test to GlobeCore — cost: perf coverage lives in the app repo.
- Task 5: dispatched implementer (sonnet), BASE 130d77d (GlobeCore main)
- Task 5: implementer DONE 27022cb, 92 tests; review dispatched (sonnet)
- Task 5: complete (commits 130d77d..27022cb, review clean; composer arithmetic verified identical; copied test asserts identical to originals). Task 5: minor (deferred): SunLit.TwilightDeg declared but unused (composer fixed −12°); wrong-size layer ParamName now type name; Gradient test helper duplicated; SelfLuminousSkipsNight could include a layer; LINQ/array alloc per compose (check perf in Task 8).
- Task 6: dispatched implementer (sonnet), BASE 27022cb (GlobeCore main)
- Task 6: implementer DONE 565bb46, 195 tests. Carry into Task 8 (CARRY list): C1 DpapiProtector entropy must be "EarthWatch3D.EUMETSAT" (existing blobs must decrypt); C2 CrashHandler appName / PerfLog.Configure(appName) for EarthWatch3D names unchanged; C3 CameraRules take CameraLimits.EarthDefault; C4 FileLog prefix "earthwatch3d-"; C5 ISettingsStore<AppSettings>; C6 FeedHttp(userAgent,…), FeedScheduler idleNote; C7 IDialogService/DialogService stay in EarthWatch3D (app-coupled). Ruling R16: dialog services stay in the app — they depend on app windows/VMs — cost: none for Plan 1.
- Task 6: review dispatched (sonnet)
- Task 6: complete (commits 27022cb..565bb46, review clean). Task 6: minor (deferred): RayHitsEllipsoid/TryHitBody don't validate polarRatio ≤ 0; DirectionIndicator.IsOccludedByEarth Earth-named and unit-sphere; CrashHandler Message/ClosingMessage const→instance (grep in Task 8); CRLF/.gitattributes.
- Task 7: dispatched implementer (opus — LayerHost extraction needs judgment), BASE 565bb46 (GlobeCore main)
- Task 7: implementer DONE_WITH_CONCERNS a71d996, 253 tests. Ruling R17: accept the additive LayerHost/layer members (DisplayTimeUtc, IlluminationAt, RecomposeIfStale, RequestSceneUpdate, ComposedUtc, WhenFirstComposeDisplayed, SelectionCleared, CameraPoseChanged; ImageLayer InvalidateNow/PrepareForComposeAsync/OnComposeDisplayed; SceneLayer OnFrame/Invalidate; OverlayLayer Invalidate/Clear) — needed to carry today's behaviour; nothing briefed removed — cost: wider API. CARRY C8 (Task 9): host.Dispose() before StaWorker dispose; set HoverTip.Style. Asset README/PB2002 notice copied verbatim name EarthWatch3D (fix wording in Task 10 docs).
- Task 7: review dispatched (opus; geojson/jpg excluded from diff)
- Task 7: review Needs fixes (opus): I1 start-up ordering gives an extra first compose / base-only first compose; I2 no marshalling of off-thread invalidation. Ruling R18: marshal (Interlocked + BeginInvoke, as SceneController) for layer invalidation; VerifyAccess on host public compose methods — layers in Task 9 are fed by feed threads — cost: none. Task 7: minor (deferred): light read before await (ordering only); camera sub-point one frame late; per-frame ToArray/Projector allocs; IsComposeIdle stricter; wall-clock timing tests; speculative IVT EarthRelief3D.Tests in GlobeCore.Earth.csproj; Illumination setter overwritten doc. Fix round 1 sent to implementer.
- Task 7: fix round 1/5 (2 addressed, 0 open; commits a71d996..07ebd5e)
- Task 7: complete (commits 565bb46..07ebd5e, review clean). Task 7: minor (deferred): no cross-thread OverlayLayer test; unsynchronised _closed read (benign); no Remove(layer).
- Task 8: dispatched implementer (opus — integration), BASE dd0d825 (EarthWatch3D globecore); GlobeCore pinned at 07ebd5e, AstroCore at 9c0580a
- Task 8: implementer NEEDS_CONTEXT (uncommitted): plan defect — GlobeCore's Directory.Build.props shadows the app's, so AstroCoreRoot never reaches GlobeCore projects. Ruling R19: GlobeCore props imports the nearest parent Directory.Build.props via GetPathOfFileAbove (implementer's tested 3-line block) — standard MSBuild pattern; app's AstroCoreRoot wins — cost: app props (Version, short-root block) also apply to GlobeCore projects in app builds. R20: EarthSphere.RadiusKm duplicated in EarthWatch3D.Core, test pins = EarthBody.RadiusKm (Core can't ref WPF Earth pack). Counts so far: 51+258+588 = 897 ≥ 816; PngDiff 7/7 identical at tol 0. Resumed implementer to make the GlobeCore change and commit.
- Task 8: implementer DONE a5bd19b (EarthWatch3D) + GlobeCore 6b29fbc; 897 tests; fresh clone OK; review dispatched (sonnet; deletions listed by name)
- Task 8: complete (commits dd0d825..a5bd19b + GlobeCore 07ebd5e..6b29fbc, review clean). ⚠️ snapshot ran on pre-R19 build — covered by Task 9's snapshot on the final code. Task 8: minor (deferred): SceneController private EarthRadiusKm duplicates EarthSphere.RadiusKm; AppInfoTests pins constants not their use; R19 imports any parent props (README line); prepare-assets.ps1 writes into submodule.
- Task 9: dispatched implementer (opus), BASE a5bd19b
- Task 9: implementer DONE_WITH_CONCERNS 606ded0; 593 tests; PngDiff 7/7 identical tol 0; SceneController 399 lines. Concerns: light read before plate await; events/markers start only after globe image loads; PERF_LOG compose lines → GlobeCore-perf.log; diagnostics wait ≤250 ms longer; ComposeFallbackTests duplicates GlobeCore tests. Review dispatched (opus).
- Task 9: review Needs fixes (opus): I1 removed _firstCompose fault observer → later crash-style message after a load failure. Fix round 1 sent (+ minors 7, 8 folded in as same-file trivia). Ruling R21: accept reported diffs — light read before plate await (self-correcting ≤250 ms), diagnostics wait ≤250 ms longer, PERF_LOG compose lines in GlobeCore-perf.log (fix README.md:161, docs/perf.md:24 in Task 10) — not user-visible. OWNER DECISION D1: events/markers/hover now appear when the globe image appears (~1 s after start) instead of over the black viewport during decode — recommend accept. Task 9: minor (deferred): HoverBlockers copied once into host; ComposeFallbackTests duplicates GlobeCore tests. Owner manual check (R4) pending; checklist in task-9-report.md.
- Task 9: fix round 1/5 (1 Important + 2 minors addressed, 0 open; commits 606ded0..ac2cb14)
- Task 9: complete (commits a5bd19b..ac2cb14, review clean). Owner Step 5 manual check pending (R4).
- Task 10: dispatched implementer (sonnet), BASE ac2cb14; Step 4 (whole-branch review) is the controller's
- Task 10: implementer DONE 82cc015 (EarthWatch3D) + GlobeCore 0ab857c; totals 51+258+594 = 903 Release, 0 failed; About check OK. Review dispatched (sonnet).
- Task 10: complete (commits ac2cb14..82cc015 + GlobeCore 6b29fbc..0ab857c, review clean; GIBS notice grep checked). Task 10: minor (deferred): rulings table omits R15/R16; README.md:163 long line; asset notices duplicated in app and GlobeCore (drift risk).
- Final whole-branch review dispatched (opus) over AstroCore 54a76ee..9c0580a, GlobeCore root..0ab857c, EarthWatch3D 18b7296..82cc015
- Final review (opus): With fixes. Important: F1 BodyMesh uses parametric not planetocentric latitude on flattened bodies; F2 SunLit.TwilightDeg ignored (−12° fixed); F3 GlobeCore.Earth is WPF so no-UI apps can't use it (forced EarthSphere dup); F4 push order (AstroCore bodies→main ff + push, create GlobeCore remote after licence, then EarthWatch3D). Minors 1–12 + triage table (see final review report in this session).
- Ruling R22: keep AstroCore.Earth.GeoPosition in GlobeCore's Geo/rasterizer APIs for Plan 1 (the plan kept it; spec §3.2 said SurfacePoint everywhere) — decide in Plan 2 before EarthRelief3D builds on it — cost: an API migration later.
- Ruling R23: make GlobeCore.Wpf PerfLog.Configure public and have EarthWatch3D call Configure("EarthWatch3D") so compose lines return to EarthWatch3D-perf.log (restores pre-plan behaviour; supersedes the R21 PERF_LOG item) — cost: docs from Task 10 re-edited.
- Ruling R24: F4 is a finishing/push concern, not code — handled at finish (no pushes without the owner) — cost: none.
- Deferred to Plan 2 (not in fix wave): Minor 9 (InternalsVisibleTo for app test assemblies), Minor 11 (EventStore on SnapshotStore), Minor 5 noted in docs only.
- Final fix wave dispatched (opus): F1, F2, F3, Minors 2,3,4,5(doc),6(doc),7(spec D1 text),8(spec table + CarringtonRotation refs),10,12.
- Final fix wave: DONE GlobeCore bf559f7, EarthWatch3D b692271; 946 tests; PngDiff 7/7 tol 0; scoped re-review dispatched (sonnet). Concern: GC-timing test failed 1/6 runs (flaky) — note for owner/Plan 2.
- Final fix wave re-review: all addressed, clean. Out-of-scope deferred to Plan 2: SphereLines.Densify / CameraRules.LookDownOn sphere-only; EarthWatch3D builds light by hand (equal for Earth); flaky GC-timing test.
