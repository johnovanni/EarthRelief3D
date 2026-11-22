# Plan 1 — GlobeCore: Body-Agnostic Globe Library and EarthWatch3D Switch-Over

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take EarthWatch3D's general-purpose globe code out into a new shared library, GlobeCore, that can draw any
body (Earth, Moon, Sun, Mars). Add body rotation to AstroCore. Then switch EarthWatch3D over to GlobeCore with every
test still passing and its pictures unchanged pixel for pixel.

**Architecture:** AstroCore gains `IBodyRotation`, with an Earth model that reproduces today's GMST results exactly
and an IAU model for the Sun, the Moon and Mars. GlobeCore is a new repo with three projects:

- **`GlobeCore`** (no UI): bodies, geo, image composition through an ordered list of image layers, feeds, stores,
  storage, logging and time.
- **`GlobeCore.Wpf`**: ellipsoid mesh, camera, picking, a layer class hierarchy and `LayerHost`, shared styles and
  diagnostics.
- **`GlobeCore.Earth`**: the Earth body, Blue Marble, Natural Earth lines, plates and clouds.

EarthWatch3D references GlobeCore as a submodule, and its 934-line `SceneController` is split into layers on
`LayerHost`.

**Tech Stack:** C# / .NET 10, WPF, HelixToolkit.Wpf 3.1.2, CommunityToolkit.Mvvm 8.4.0 (apps only), xunit 2.9.2.

**Spec:** `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md` (§2, §3, §13, Appendix A).

## Global Constraints

- **.NET 10 everywhere:**
  - `net10.0` for no-UI projects, `net10.0-windows` with `UseWPF` for WPF projects.
  - `ImplicitUsings` and `Nullable` enabled, as in the existing projects.
- **Conventions (unchanged):**
  - AstroCore: J2000 mean ecliptic frame; times as TT Julian dates or `DateTime` UTC. Local `DateTime` is converted to
    UTC; Unspecified is treated as UTC, as in `SubPoints`.
  - Body-fixed coordinates: +X to (0°, 0°), +Y to (0°, 90°E), +Z to the north pole.
  - Longitudes are planetocentric, east-positive, normalised to (−180, 180]. Latitudes are planetocentric.
- **Earth stays a sphere** of radius 6371 km in GlobeCore's Earth pack (spec C2).
- **Accuracy:** sub-solar and sub-observer points for the Moon and Mars must match JPL Horizons to **0.05°**.
  Existing Earth results must be unchanged, to 1e-9°.
- **AstroCore location:** one AstroCore copy per app build. GlobeCore projects find AstroCore through the MSBuild
  property `AstroCoreRoot`, which defaults to `$(MSBuildThisFileDirectory)external\AstroCore\` in GlobeCore's own
  `Directory.Build.props`. Each app sets it to its own `external\AstroCore\`.
- **Submodule URLs are relative,** as today: `../AstroCore`, `../GlobeCore`.
- **Long paths:** every repo keeps the "path longer than 110 characters → short bin/obj under %TEMP%" block in
  `Directory.Build.props`. GlobeCore uses `%TEMP%\GlobeCore.build\`.
- **No visual or behaviour change in EarthWatch3D:**
  - Snapshot PNGs match the baseline taken in Task 1, pixel for pixel (or within the noise floor measured in Task 1).
  - The data folder paths are unchanged, both packaged and unpackaged.
- **Test count:** the total number of tests run across AstroCore, GlobeCore and EarthWatch3D is at least the
  baseline total from Task 1, with no new skips.
- **Visual checks:** only through the app's own `--snapshot`, which uses `RenderTargetBitmap`. Never use PowerShell
  screen capture or `Add-Type` (owner's antivirus). Keep app launches few: Task 1 adds a multi-scene snapshot so that
  one launch renders every scene.
- **Git:**
  - Work happens on branch `bodies` in AstroCore and `globecore` in EarthWatch3D; GlobeCore is a new local repo on
    `main`.
  - Commits are local, and the owner approves committing when execution starts. Nothing is pushed until the owner
    asks.
- **Commit message attribution:** end every commit message with
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Embedded resource names change when assets move.** Today they are `EarthWatch3D.App.Assets.*`; after the move
   they become `GlobeCore.Earth.Assets.*`. Expected: every asset EarthWatch3D loads still loads. Pinned by
   `EarthAssetsTests.EveryNamedAssetExists` (Task 7).
2. **Fresh clone builds with non-recursive submodule init.** EarthWatch3D must build after
   `git submodule update --init`, without GlobeCore's nested AstroCore, because `AstroCoreRoot` points to the app's
   copy. Pinned by the fresh-clone build step in Task 8.
3. **Existing users' data folder must not move,** packaged (`LocalCache`) or not. Pinned by
   `AppDataRootTests.PathsMatchEarthWatch3DBefore` (Task 4).
4. **An ellipsoid with polar ratio 1 must reproduce today's sphere exactly** (mesh positions, texture coordinates, ray
   hits). Longitudes near ±180° and latitudes near the poles must stay correct on a flattened body. Pinned by
   `BodyMeshTests` and `ProjectorEllipsoidTests` (Task 6).
5. **A deep worktree path (over 110 characters) must still build GlobeCore** through the short build root. Pinned by
   the worktree build step in Task 8.

---

## File Structure

**AstroCore** (branch `bodies`):

```
src/AstroCore/Bodies/IBodyRotation.cs          interface: direction (J2000 ecliptic) + time → body-fixed GeoPosition
src/AstroCore/Bodies/EarthRotationModel.cs     precession + GMST, identical to today's SubPoints
src/AstroCore/Bodies/IauRotationElements.cs    record: pole and prime-meridian polynomials + periodic terms
src/AstroCore/Bodies/IauRotationModel.cs       IAU WGCCRE transform
src/AstroCore/Bodies/BodyRotations.cs          static instances: Earth, Sun, Moon, Mars
src/AstroCore/Bodies/BodyConstants.cs          radii (equatorial, polar) for Sun, Moon, Earth, Mars
src/AstroCore/Bodies/BodySubPoints.cs          sub-solar and sub-observer points for any body, with light time
src/AstroCore/Orbits/PlanetOrbits.cs           Mars elements (copied from SolarSystem3D's MajorBodyCatalog)
tests/AstroCore.Tests/BodyRotationTests.cs
tests/AstroCore.Tests/HorizonsSubPointTests.cs
tests/AstroCore.Tests/Data/horizons-subpoints.json
tools/fetch-horizons-subpoints.ps1
```

**GlobeCore** (new repo `C:\repos\GlobeCore`):

```
Directory.Build.props, GlobeCore.sln, README.md, LICENSE (same licence as AstroCore), THIRD-PARTY-NOTICES.txt
external/AstroCore                              submodule
src/GlobeCore/        Bodies/ Geo/ Imaging/ Feeds/ Stores/ Storage/ Logging/ Time/
src/GlobeCore.Wpf/    Viewport/ Camera/ Layers/ Rendering/ Services/ Styles/ Diagnostics/
src/GlobeCore.Earth/  EarthBody.cs, EarthAssets.cs, NaturalEarthLines.cs, PlateBoundaries.cs, Clouds/, Assets/
tests/GlobeCore.Tests/                         (net10.0-windows, UseWPF, xunit)
```

**EarthWatch3D** (branch `globecore`): add `external/GlobeCore`, delete the moved files, split
`src/EarthWatch3D.App/Controllers/SceneController.cs` into `src/EarthWatch3D.App/Layers/*.cs`, and add
`tools/PngDiff/` plus `tests/SnapshotScenes/scenes.json`.

---

### Task 1: Snapshot baseline and test-count baseline (EarthWatch3D)

**Files:**
- Modify: `src/EarthWatch3D.App/Diagnostics/LaunchOptions.cs` (new option `--snapshot-scenes <json> --snapshot-out <dir>`)
- Modify: `src/EarthWatch3D.App/Diagnostics/SnapshotDiagnostic.cs`
- Create: `tests/SnapshotScenes/scenes.json`
- Create: `tools/PngDiff/PngDiff.csproj`, `tools/PngDiff/Program.cs` (net10.0-windows, WPF imaging only)
- Test: `tests/EarthWatch3D.Tests/LaunchOptionsTests.cs`

**Interfaces:**
- Produces:
  - `SnapshotScene(string Name, DateTime DisplayTimeUtc, GeoPosition? LookAt, bool FaceSun, string[] Layers)`, with
    `LaunchOptions.SnapshotScenesFile` and `LaunchOptions.SnapshotOutDir`.
  - `PngDiff <dirA> <dirB>`: prints one line per file, `name: identical` or `name: N pixels differ, max channel delta D`,
    and exits 0 only if every file is identical or within `--tolerance D`.

- [ ] **Step 1:** Create branch `globecore` from `main` (18b7296) in EarthWatch3D and branch `bodies` in AstroCore.
  Run `dotnet test` in both repos and record the totals (passed, skipped) in
  `C:\repos\EarthRelief3D\docs\superpowers\plans\plan-1-baseline.md`.
- [ ] **Step 2: Write the failing test** `LaunchOptionsTests.ParsesSnapshotScenes`. Parsing
  `--offline-samples tests\Samples --snapshot-scenes s.json --snapshot-out out` gives `SnapshotScenesFile == "s.json"`,
  `SnapshotOutDir == "out"` and `IsDiagnosticRun == true`. Run it and expect FAIL.
- [ ] **Step 3:** Implement the option. For each scene, the run sets the display time, layers and camera, waits with
  the existing readiness rules (every feed reported once, areas settled, first compose displayed), and saves
  `<out>\<Name>.png`. It then exits with 0, or 1 if any save failed. Layers not named in a scene are off.
- [ ] **Step 4:** Run the test and expect PASS. Then run all EarthWatch3D tests and expect the baseline counts plus 1.
- [ ] **Step 5:** Write `tests/SnapshotScenes/scenes.json` with these 7 scenes, all against `tests\Samples`:

  | Name | Display time (UTC) | Camera | Layers |
  |---|---|---|---|
  | `default` | 2026-11-11T20:00:00Z | default home view | quakes, cyclones, alerts, eonet, coastlines |
  | `pacific-plates` | 2026-11-11T20:00:00Z | look at 10,−165 | coastlines, plates, quakes |
  | `us-alerts` | 2026-11-11T20:00:00Z | look at 38,−97 | alerts, coastlines |
  | `lightning-clouds` | 2026-11-11T19:10:00Z | look at 0,−75 | lightning, clouds, coastlines |
  | `face-sun` | 2026-11-11T20:00:00Z | face Sun | coastlines |
  | `night-asia` | 2026-11-12T08:00:00Z | look at 30,100 | coastlines, quakes |
  | `dateline-storm` | 2026-11-11T20:00:00Z | look at 15,180 | cyclones, coastlines |

- [ ] **Step 6:** Implement `PngDiff` with `BitmapDecoder`/`FormatConvertedBitmap` to Bgra32, comparing byte for
  byte. Do not use System.Drawing or `Add-Type`.
- [ ] **Step 7: Take the baseline twice** (two launches):

  ```
  dotnet run --project src/EarthWatch3D.App -c Release -- --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-baseline
  ```

  The second run writes to `artifacts\snapshot-baseline-2`. Then run
  `dotnet run --project tools/PngDiff -- artifacts\snapshot-baseline artifacts\snapshot-baseline-2`.

  Expected: all 7 identical. If not, record the largest channel delta in `plan-1-baseline.md` as the **noise
  floor**. Every later comparison uses `--tolerance <noise floor>`. `artifacts\` is already ignored by git (check;
  add it to `.gitignore` if not).
- [ ] **Step 8:** Commit with the message `Diagnostics: multi-scene snapshots and PngDiff for the GlobeCore switch-over`.

### Task 2: `IBodyRotation` and the Earth model (AstroCore)

**Files:**
- Create: `src/AstroCore/Bodies/IBodyRotation.cs`, `src/AstroCore/Bodies/EarthRotationModel.cs`,
  `src/AstroCore/Bodies/BodySubPoints.cs`
- Test: `tests/AstroCore.Tests/BodyRotationTests.cs`

**Interfaces:**
- Consumes: `SubPoints`, `EarthRotation.GreenwichMeanSiderealTimeDeg(DateTime)`, `Precession.FromJ2000(double)`,
  `GeoPosition`.
- Produces:
  - `public interface IBodyRotation { GeoPosition ToBodyFixed(Vector3d directionEclipticJ2000, DateTime utc); }`
  - `public sealed class EarthRotationModel : IBodyRotation`
  - `public static class BodySubPoints`, with:
    - `GeoPosition SubSolarPoint(Func<double, Vector3d>? bodyHeliocentric, IBodyRotation rotation, DateTime utc)`:
      the function maps jdTT to the heliocentric J2000 ecliptic position in AU. `null` means the Earth
      (`EarthMoonSystem.Earth.PositionAt`). The Sun direction is `−(heliocentric position)`.
    - `GeoPosition SubObserverPoint(Func<double, Vector3d> bodyHeliocentric, IBodyRotation rotation,
      Func<double, Vector3d> observerHeliocentric, DateTime observerUtc, bool lightTime = true)`

- [ ] **Step 1: Write the failing test** `EarthModelMatchesSubPoints`. For 2000-01-01T12:00Z, 2026-11-10T00:00Z,
  2026-06-21T06:30Z and 2049-12-31T23:59Z, `BodySubPoints.SubSolarPoint(null, new EarthRotationModel(), t)` equals
  `SubPoints.SubSolarPoint(t)` to 1e-9° in latitude and longitude. Run it and expect FAIL (types missing).
- [ ] **Step 2:** Implement `EarthRotationModel.ToBodyFixed`. Rotate ecliptic to equatorial with
  `Matrix3d.RotationX(Frames.ObliquityJ2000)`, apply `Precession.FromJ2000(jdTT)`, take RA and Dec, and return
  `(Dec, Normalize(RA − GMST(utc)))`. This is the same arithmetic as `SubPoints.SubPoint`.
- [ ] **Step 3:** Run the test and expect PASS. Run all AstroCore tests and expect baseline counts plus 1.
- [ ] **Step 4:** Commit with the message `AstroCore: IBodyRotation and an Earth model matching SubPoints`.

### Task 3: IAU rotation for the Sun, the Moon and Mars, with radii and Horizons tests (AstroCore)

**Files:**
- Create: `src/AstroCore/Bodies/IauRotationElements.cs`, `src/AstroCore/Bodies/IauRotationModel.cs`,
  `src/AstroCore/Bodies/BodyRotations.cs`, `src/AstroCore/Bodies/BodyConstants.cs`,
  `src/AstroCore/Orbits/PlanetOrbits.cs`, `tools/fetch-horizons-subpoints.ps1`,
  `tests/AstroCore.Tests/Data/horizons-subpoints.json`
- Test: `tests/AstroCore.Tests/HorizonsSubPointTests.cs`, `tests/AstroCore.Tests/BodyRotationTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record IauRotationElements`, with:
    - `Alpha0`, `Delta0`, `W`: `Polynomial(double C0, double C1, double C2)`. Alpha0 and Delta0 are in T (Julian
      centuries); W is in d (days), both from J2000 TDB.
    - `PeriodicTerms`: `IReadOnlyList<PeriodicTerm>`, where each term is
      `PeriodicTerm(double AlphaAmp, double DeltaAmp, double WAmp, double Phase, double RatePerDay, double RatePerDay2)`.
      Alpha uses sin, Delta uses cos and W uses sin, as in the WGCCRE tables.
  - `public sealed class IauRotationModel(IauRotationElements e) : IBodyRotation`
  - `BodyRotations.Earth` (EarthRotationModel), plus IauRotationModel instances `BodyRotations.Sun`, `.Moon` and
    `.Mars`.
  - `BodyConstants.Radii(string bodyId) → (double EquatorialKm, double PolarKm)` for `"sun"`, `"moon"`, `"earth"`
    and `"mars"`.
  - `PlanetOrbits.Mars` (`PlanetOrbit`).

- [ ] **Step 1:** Write `tools/fetch-horizons-subpoints.ps1`. It calls the public JPL Horizons API
  (`https://ssd.jpl.nasa.gov/api/horizons.api`, `EPHEM_TYPE=OBSERVER`, `CENTER=500@399`, `QUANTITIES='14,15,21'`,
  `ANG_FORMAT=DEG`, generic User-Agent `AstroCore-tests`) for targets 301 (Moon), 499 (Mars) and 10 (Sun). It uses 6
  dates: 2000-01-01, 2010-07-15, 2020-03-20, 2024-04-08, 2026-11-10 and 2035-09-01, all at 00:00 UTC.

  For each target, store in JSON: the observer UTC, the one-way light time, the sub-observer and sub-solar longitude
  and latitude as Horizons gives them, and Horizons' own header note on the longitude sense and the latitude type
  (planetodetic or planetocentric). Run the script once and commit the JSON. Network is needed only for this step.
- [ ] **Step 2: Write the failing tests.**
  - `HorizonsSubPointTests.MoonSubSolar`, `.MoonSubObserver`, `.MarsSubSolar`, `.MarsSubObserver` and
    `.SunSubObserver`: each is within 0.05° of the JSON (great-circle separation).
  - Before comparing, the test converts Horizons' values to planetocentric, east-positive:
    - if the header says west-positive, `lon = −lonW`;
    - if it says planetodetic, `tan φc = tan φd · (b/a)²`, using `BodyConstants`.
  - `BodyRotationTests.RadiiMatchIau2015`: Sun 695700/695700, Moon 1737.4/1737.4, Earth 6378.1366/6356.7519 and Mars
    3396.19/3376.20 km.
  - Run them and expect FAIL.
- [ ] **Step 3:** Implement `IauRotationModel.ToBodyFixed`. Compute d (days) and T (centuries) from TT ≈ TDB. Evaluate
  α0, δ0 and W including the periodic terms. Rotate the direction to J2000 equatorial, then apply
  `Rz(W) · Rx(90° − δ0) · Rz(90° + α0)`. Latitude is `asin(z)` and longitude is `Normalize(atan2(y, x))`.
- [ ] **Step 4:** Transcribe the elements for the Sun, the Moon (including its E1–E13 periodic terms) and Mars
  (including its periodic terms) verbatim from Archinal et al. 2018, *Celest. Mech. Dyn. Astr.* 130:22
  (doi:10.1007/s10569-017-9805-5), Tables 1 and 2. Cite the table in an XML doc comment. Copy the Mars `PlanetOrbit`
  elements from `C:\repos\SolarSystem3D\src\SolarSystem3D.Core\Data\MajorBodyCatalog.cs` into
  `PlanetOrbits.Mars` and credit the source in a comment.
- [ ] **Step 5:** Implement light time in `BodySubPoints.SubObserverPoint`. Iterate t′ = t − |B(t′) − O(t)|/c twice,
  then use the rotation at t′ and the direction `O(t) − B(t′)`.
- [ ] **Step 6:** Run the tests and expect PASS. **If a body misses 0.05°,** do not loosen the tolerance. Record the
  measured error and its likely cause (for example the accuracy of `LunarOrbit` or `PlanetOrbit` itself) and stop for
  the owner's decision.
- [ ] **Step 7:** Add a "Bodies" section to the README listing the bodies, the source tables and the measured worst
  error. Commit with the message `AstroCore: IAU rotation for the Sun, Moon and Mars; body radii; Horizons sub-point
  tests`.

### Task 4: GlobeCore repo and the no-UI infrastructure

**Files:**
- Create: `C:\repos\GlobeCore\` (`git init`, `.gitignore` copied from EarthWatch3D, `Directory.Build.props`,
  `GlobeCore.sln`, submodule `external/AstroCore` → `../AstroCore` at branch `bodies`)
- Create: `src/GlobeCore/GlobeCore.csproj`, referencing `$(AstroCoreRoot)src\AstroCore\AstroCore.csproj`, with
  `InternalsVisibleTo` for `GlobeCore.Tests`, `EarthWatch3D.Tests` and `EarthRelief3D.Tests`
- Move from `EarthWatch3D.Core` (copy now; deleted from EarthWatch3D in Task 8):
  - `Time/*` → `GlobeCore.Time`
  - `Logging/*` → `GlobeCore.Logging`
  - `Storage/{AtomicFile, IProtector, ISettingsStore, SettingsSaver, PackageIdentity}` → `GlobeCore.Storage`
  - `Feeds/{Backoff, FeedHttp, FeedScheduler, FeedStatus, IEventFeed, DownloadMeter, SampleHttpHandler}` →
    `GlobeCore.Feeds`
  - `Geo/{DateLine, GeoShapes, GeoJsonLines, GeoFormat, Units}` → `GlobeCore.Geo`
  - `Globe/{Bgra32Image, SparseOverlay}` → `GlobeCore.Imaging`
- Create: `src/GlobeCore/Storage/AppDataRoot.cs`, `src/GlobeCore/Stores/SnapshotStore.cs`
- Test: `tests/GlobeCore.Tests/`, with the matching test files copied from EarthWatch3D: `OffsetClockTests`,
  `FileLogTests`, `SettingsSaverTests`, the generic parts of `StorageTests`, `FeedHttpTests`, `FeedSchedulerTests`,
  `DownloadMeterTests`, `GeoShapesTests`, `GeoJsonLinesTests`, `GeoFormatTests`, `UnitsTests`, `SparseOverlayTests`.
  Also new: `AppDataRootTests` and `SnapshotStoreTests`.

**Interfaces:**
- Produces:
  - `public interface IFeed` (was `IEventFeed`; same members `Name`, `Interval`, `RunOnceAsync`). `FeedRunResult` is
    unchanged.
  - `FeedHttp(string userAgent, HttpMessageHandler? handler = null, TimeSpan? timeout = null, long maxBodyBytes = 50L * 1024 * 1024)`
  - `FeedStatus.IdleNote`: the satellite wording moves out to a constructor parameter, `string idleNote`.
  - `public sealed record AppDataRoot(string Root)`, with `CacheDir`, `LogsDir`, `SettingsFile`,
    `DownloadMeterFile` and `static AppDataRoot ForUser(string appName, string localAppData, string? packageFamilyName)`.
    The paths are the same formulas as today's `AppPaths`, with "EarthWatch3D" replaced by `appName`.
  - `public sealed class SnapshotStore<T>(T empty) where T : class`, with `T Snapshot`, `event EventHandler? Changed`
    (raised on the writing thread) and `bool Write(Func<T, T?> change)`. A `null` result or the same reference means
    no change and no event.

- [ ] **Step 1:** Create the repo, props and projects. `Directory.Build.props` holds `AstroCoreRoot` (defaulting to
  GlobeCore's own submodule, set only if empty) and the long-path block (`GlobeCore.build`). Run `dotnet build` and
  expect success.
- [ ] **Step 2:** Copy the files listed above. Change namespaces, inject the User-Agent and the idle note, and rename
  `IEventFeed` to `IFeed`. Copy their tests and change their namespaces.
- [ ] **Step 3: Write the failing tests.**
  - `AppDataRootTests.PathsMatchEarthWatch3DBefore`: for `("EarthWatch3D", @"C:\L", null)` and
    `("EarthWatch3D", @"C:\L", "johnovanni.EarthWatch3D_abc")`, `Root`, `LogsDir`, `CacheDir`, `SettingsFile` and
    `DownloadMeterFile` equal the strings today's `AppPaths.ForUser` gives (literal expected strings).
  - `SnapshotStoreTests.WriteRaisesChangedOnce`, `.NullWriteRaisesNothing` and `.ConcurrentWritesAllApply` (100
    parallel increments of a counter record end at 100).
- [ ] **Step 4:** Implement `AppDataRoot` and `SnapshotStore<T>`. `SnapshotStore` uses a lock around the write and a
  volatile read, as `EventStore` does today.
- [ ] **Step 5:** Run `dotnet test` in GlobeCore. Expected: all moved tests plus the new ones pass. Record the count.
- [ ] **Step 6:** Commit with the message `GlobeCore: repo, AstroCore submodule, no-UI infrastructure moved from
  EarthWatch3D`.

### Task 5: Bodies and image composition (GlobeCore)

**Files:**
- Create: `src/GlobeCore/Bodies/Body.cs`, `src/GlobeCore/Bodies/Ellipsoid.cs`, `src/GlobeCore/Bodies/SurfacePoint.cs`,
  `src/GlobeCore/Bodies/Illumination.cs`
- Move and modify: `EarthWatch3D.Core/Geo/GeoMath.cs` → `src/GlobeCore/Geo/GeoMath.cs`
- Move and modify: `EarthWatch3D.Core/Globe/GlobeImageComposer.cs` → `src/GlobeCore/Imaging/GlobeImageComposer.cs`
- Create: `src/GlobeCore/Imaging/IImageLayer.cs`, `src/GlobeCore/Imaging/ImageLayers.cs`
  (`BitmapBlendLayer`, `SparseOverlayLayer`)
- Test: `tests/GlobeCore.Tests/GlobeImageComposerTests.cs`, copied unchanged in its assertions from EarthWatch3D;
  `ComposerLayerListTests.cs`; `BodyTests.cs`

**Interfaces:**
- Produces:
  - `public readonly record struct SurfacePoint(double LatitudeDeg, double LongitudeDeg)`, with implicit conversions
    to and from `AstroCore.Earth.GeoPosition`.
  - `public sealed record Ellipsoid(double EquatorialKm, double PolarKm)`, with `PolarRatio` and `MeanRadiusKm`
    (= (2a + b)/3).
  - `public enum LongitudeConvention { EastPositive, WestPositive }`
  - `public abstract record Illumination`, with two subtypes:
    - `SunLit(double TwilightDeg = -12.0)`
    - `SelfLuminous`
  - `public sealed record Body(string Id, string Name, Ellipsoid Shape, IBodyRotation Rotation,
    Func<double, Vector3d>? HeliocentricPosition, LongitudeConvention LongitudeConvention, Illumination Illumination)`,
    with `SurfacePoint SubSolarPoint(DateTime utc)`, which calls `BodySubPoints.SubSolarPoint`. A `null` position
    means the Earth.
  - `GeoMath.DistanceKm(SurfacePoint a, SurfacePoint b, double radiusKm)`. The old `EarthRadiusKm` constant moves to
    the Earth pack.
  - `public enum ImagePass { Surface, AboveNight }`
  - `public interface IImageLayer { ImagePass Pass { get; } int Order { get; } void BlendRow(int y, Span<byte> bgraRow); }`
  - `public sealed record IlluminationState(SurfacePoint SubSolarPoint, double NightStrength, bool DrawTerminator)`
  - `GlobeImageComposer.ComposeInto(IReadOnlyList<IImageLayer> layers, IlluminationState? light, Bgra32Image target)`
    draws in this order:
    1. the base;
    2. Surface layers by `Order`;
    3. the night shade and terminator (if `light` is set);
    4. AboveNight layers by `Order`.
  - `GlobeLayers` is kept as an adapter. `ComposeInto(GlobeLayers, Bgra32Image)` builds the equivalent list (clouds:
    Surface 10, areas: Surface 20, dense lines: AboveNight 10, sparse: AboveNight 20) and calls the new overload.

- [ ] **Step 1: Write the failing tests.**
  - `ComposerLayerListTests.AdapterMatchesLayerList`: a 64×32 base with clouds, areas, dense lines and sparse lines
    gives byte-identical output through both overloads.
  - `.OrderIsRespectedWithinAPass`: two opaque layers overlapping; the higher `Order` wins.
  - `.SelfLuminousSkipsNight`: `light: null` leaves the base unchanged outside the layers.
  - `BodyTests.MeanRadius`: Mars gives 3389.53 ± 0.01.
  - Run them and expect FAIL.
- [ ] **Step 2:** Implement the types and the composer. The per-row arithmetic is moved, not rewritten, so the copied
  `GlobeImageComposerTests` and the `ComposeFallback` behaviour stay as they are.
- [ ] **Step 3:** Run all GlobeCore tests and expect PASS, including every copied `GlobeImageComposerTests` case
  unchanged.
- [ ] **Step 4:** Commit with the message `GlobeCore: bodies, ellipsoids, layered image composition`.

### Task 6: WPF helpers, ellipsoid mesh, camera and picking (`GlobeCore.Wpf`)

**Files:**
- Create: `src/GlobeCore.Wpf/GlobeCore.Wpf.csproj` (HelixToolkit.Wpf 3.1.2, references GlobeCore)
- Move (namespaces `GlobeCore.Wpf.*`):
  - `Rendering/{Projector, CameraAnimator, LabelOverlay, LineRasterizer, AreaRasterizer, SphereLines, StaWorker, ImageConversions, DirectionIndicator}`
  - `Controllers/{LazyLayer, Throttle, RecomposeSchedule}`
  - `Services/{IDialogService, DialogService, ILinkOpener, DpapiProtector}`
  - `Views/{CardStyles.xaml, TimeBarStyles.xaml}`
  - `Diagnostics/{CrashHandler, PerfLog}`
- Move and modify: `Rendering/GlobeMesh.cs` → `Viewport/BodyMesh.cs`
- Move and modify: `Rendering/CameraRules.cs` → `Camera/CameraRules.cs`
- Test: copy the matching tests (`SurfacePickTests`, `CameraRulesTests`, `GlobeMeshTests` → `BodyMeshTests`,
  `LineRasterizer`/`AreaRasterizerTests`, `SphereLinesTests`, `LabelOverlay`-related, `StaWorkerTests`,
  `LazyLayerTests`, `RecomposeScheduleTests`, `DirectionIndicatorTests`, `DpapiProtectorTests`,
  `CrashHandlerTests`); new `ProjectorEllipsoidTests`

**Interfaces:**
- Produces:
  - `BodyMesh.Create(double polarRatio = 1.0, int lonSegments = 360, int latSegments = 180) → MeshGeometry3D`. It
    uses the same vertex order and texture coordinates as `GlobeMesh.Create`, with z scaled by `polarRatio`.
  - `public sealed record CameraLimits(double MinDistance, double MaxDistance, double ZoomPerNotch)`, with
    `static CameraLimits EarthDefault = new(1.047, 20.0, 1.15)`. `CameraRules` methods take `CameraLimits` where they
    used the constants.
  - `Projector.TryHitBody(Point screen, double polarRatio, out Point3D hit)` and
    `Projector.RayHitsEllipsoid(Point3D origin, Vector3D direction, double polarRatio, out Point3D hit)`.
    `TryHitUnitSphere` stays and calls these with 1.0.

- [ ] **Step 1: Write the failing tests.**
  - `BodyMeshTests.PolarRatioOneEqualsOldMesh`: positions, indices and texture coordinates equal
    `GlobeMesh.Create()`'s. Paste the old function into the test as a private reference.
  - `BodyMeshTests.FlattenedPoleAtPolarRatio`: Saturn-like ratio 0.902; the north pole vertex z is 0.902.
  - `ProjectorEllipsoidTests.HitsFlattenedPole`: a ray straight down the z axis hits z = 0.902.
  - `.MatchesSphereAtRatioOne`: 1,000 random rays give the same hits as `RayHitsUnitSphere` to 1e-12.
  - `.DatelineAndPoleLongitudes`: hits near lon ±180 and lat ±89.9 round-trip through `SurfacePoint` to 1e-9°.
  - Run them and expect FAIL.
- [ ] **Step 2:** Move the files and implement `BodyMesh`, `CameraLimits` and the ellipsoid ray hit. The ray hit
  scales z by 1/polarRatio, intersects the unit sphere, and scales back.
- [ ] **Step 3:** Run all GlobeCore tests and expect PASS.
- [ ] **Step 4:** Commit with the message `GlobeCore.Wpf: viewport helpers, ellipsoid mesh, camera limits, picking`.

### Task 7: Layer framework, `LayerHost`, snapshot renderer, and the Earth pack

**Files:**
- Create in `src/GlobeCore.Wpf/Layers/`: `GlobeLayer.cs`, `ImageLayer.cs`, `SceneLayer.cs`, `OverlayLayer.cs`,
  `IPickable.cs`, `LayerHost.cs`
- Create: `src/GlobeCore.Wpf/Diagnostics/SnapshotRenderer.cs` (moved from the PNG-saving part of
  `SnapshotDiagnostic`)
- Create: `src/GlobeCore.Earth/GlobeCore.Earth.csproj`
- Move assets (embedded) from EarthWatch3D: `bluemarble-8192.jpg`, `ne_50m_coastline.geojson`,
  `ne_50m_admin_0_boundary_lines_land.geojson`, `plate_boundaries.geojson`, with `Assets/README.md` and their notices
- Move code: `CloudLayer`, the GIBS feed (`Feeds/Gibs/*`), `PlateBoundaries`
- Create: `EarthBody.cs`, `EarthAssets.cs`, `NaturalEarthLines.cs`
- Test: `tests/GlobeCore.Tests/LayerHostTests.cs`, `EarthAssetsTests.cs`, plus copies of `CloudLayerTests`,
  `GibsCloudsFeedTests` and `PlateBoundariesTests`

**Interfaces:**
- Consumes: Tasks 4–6.
- Produces:
  - `public abstract class GlobeLayer : INotifyPropertyChanged`, with `string Id`, `string Title`, `bool IsVisible`,
    `int ZOrder`, `string? Source`, `DateTime? AsOfUtc` and `IReadOnlyList<LegendItem> LegendItems`.
    `LegendItem(string Label, Color Color, string? Glyph)`.
  - `public abstract class ImageLayer : GlobeLayer`, with `abstract IImageLayer? Current { get; }` and
    `protected void Invalidate()`. Invalidating requests a recompose.
  - `public abstract class SceneLayer : GlobeLayer`, with `ModelVisual3D Root { get; }` and
    `abstract void Update(SceneContext ctx)`. `SceneContext(PerspectiveCamera Camera, Projector Projector, DateTime DisplayTimeUtc)`.
  - `public abstract class OverlayLayer : GlobeLayer`, with `abstract void Render(Canvas overlay, Projector projector)`.
  - `public interface IPickable { PickResult? HitTest(Point screen, Projector projector, bool hover); }` and
    `PickResult(string LayerId, object Item, string Tooltip)`.
  - `public sealed class LayerHost : IDisposable`, with:
    - constructor `LayerHost(HelixViewport3D viewport, Canvas overlay, Body body, Bgra32Image baseImage, IAppLog log)`;
    - `void Add(GlobeLayer layer)`, `IReadOnlyList<GlobeLayer> Layers`;
    - `IlluminationState? Illumination { get; set; }`;
    - `void RequestRecompose()`, `Task RecomposeNowAsync()`;
    - `bool IsComposeIdle`, `event EventHandler? FirstComposeDisplayed`, `event EventHandler? Composed`;
    - `PickResult? Pick(Point screen, bool hover)`, where the highest `ZOrder` among visible `IPickable` layers wins;
    - `event EventHandler<PickResult>? Selected`.

    Composition runs off the UI thread through the moved `RecomposeSchedule`/`Throttle`, with the existing
    plain-image fallback (`ComposeOrPlain`) moved in.
  - `SnapshotRenderer.SavePng(Window window, string path) → bool`, using `RenderTargetBitmap` at the window's DPI, as
    today.
  - `EarthBody.Instance`: a `Body` with id `"earth"`, an `Ellipsoid(6371, 6371)`, `BodyRotations.Earth`, a `null`
    orbit, `EastPositive` and `SunLit(-12)`. Also `EarthBody.RadiusKm = 6371.0`.
  - `EarthAssets.Open(string fileName) → Stream` and `EarthAssets.Names`.
  - `EarthAssets.LoadBlueMarble(int width) → Bgra32Image`, downscaled exactly as `SceneController` does today.
  - `NaturalEarthLines.Coastlines()` and `NaturalEarthLines.LandBoundaries()`, both returning
    `List<IReadOnlyList<GeoPosition>>`.

- [ ] **Step 1: Write the failing tests.**
  - `LayerHostTests.InvalidateTriggersOneRecompose`: three `Invalidate()` calls within the throttle window give one
    compose.
  - `.PickPrefersHigherZOrder`, `.HiddenLayerIsNotPicked`.
  - `.ComposeFailureShowsPlainImage`: moved from `ComposeFallbackTests`.
  - `EarthAssetsTests.EveryNamedAssetExists`: for each of the four file names, `Open` returns a non-empty stream.
  - `.BlueMarbleDownscaleMatchesOld`: a 4096-wide load equals the old `SceneController` path (paste the old code into
    the test as a reference).
  - Run them and expect FAIL.
- [ ] **Step 2:** Implement the layer classes and `LayerHost`. Move the compose loop from
  `SceneController.ComposeLoopAsync`, `RequestRecompose` and `OnTrailingTick`, and hover and click dispatch from
  `UpdateHover` and `PickEvent`, keeping their timing constants.
- [ ] **Step 3:** Create `GlobeCore.Earth` and move the assets, code and their tests. Add the asset credits (NASA Blue
  Marble, Natural Earth, Peter Bird's PB2002, NASA GIBS) to GlobeCore's `THIRD-PARTY-NOTICES.txt`.
- [ ] **Step 4:** Run all GlobeCore tests and expect PASS. Write GlobeCore's `README.md`: what it is, the three
  projects, how to add a body, and how an app sets `AstroCoreRoot`.
- [ ] **Step 5:** Commit with the message `GlobeCore: layer framework, LayerHost, snapshot renderer, Earth pack`.

### Task 8: EarthWatch3D on GlobeCore, without the split

**Files:**
- Modify: `.gitmodules` (add `external/GlobeCore` → `../GlobeCore`)
- Modify: the `external/AstroCore` pointer (to the `bodies` tip) and `Directory.Build.props`
  (`AstroCoreRoot = $(MSBuildThisFileDirectory)external\AstroCore\`)
- Modify: `EarthWatch3D.Core.csproj` and `EarthWatch3D.App.csproj` (reference GlobeCore, GlobeCore.Wpf and
  GlobeCore.Earth; remove the moved assets from `EmbeddedResource`)
- Modify: every file using a moved type (`using` lines only, plus `IEventFeed` → `IFeed`, `FeedHttp` getting
  `AppInfo.UserAgent`, `AppPaths` wrapping `AppDataRoot`)
- Delete: the moved source files, assets and tests
- Modify: `tests/EarthWatch3D.Tests/PackagingTemplateTests.cs` and `MtgLiveTests.cs` (find the repo root by looking
  for `EarthWatch3D.sln`, not by a fixed number of parent folders)

**Interfaces:**
- Consumes: everything from Tasks 4–7.
- Produces:
  - `EarthWatch3D.Core.Storage.AppPaths(AppDataRoot Data)`, keeping `ZonesDir`, `EventsFile` and `CloudsDir` and
    forwarding the rest. Every existing caller compiles unchanged except for the construction.

- [ ] **Step 1:** Add the submodule, set the property and switch the references. Delete the moved files and fix the
  `using` lines. Run `dotnet build -c Release` and expect success with no new warnings.
- [ ] **Step 2:** Run `dotnet test` in EarthWatch3D, GlobeCore and AstroCore. Expected: all pass, and their total is at
  least the Task 1 baseline total. Write the three counts into `plan-1-baseline.md`.
- [ ] **Step 3: Snapshot check** (one launch). Run the Task 1 command with `--snapshot-out artifacts\snapshot-task8`,
  then `PngDiff artifacts\snapshot-baseline artifacts\snapshot-task8 --tolerance <noise floor>`. Expected: exit 0.
- [ ] **Step 4: Fresh-clone build.** Clone EarthWatch3D's `globecore` branch into a temporary folder, run
  `git submodule update --init` (not recursive), then `dotnet build -c Release`. Expected: success.
- [ ] **Step 5: Deep-path build.** Create a git worktree of GlobeCore under a path longer than 110 characters and run
  `dotnet build` there. Expected: success, with output under `%TEMP%\GlobeCore.build\`. Remove the worktree
  afterwards.
- [ ] **Step 6:** Commit with the message `EarthWatch3D: use GlobeCore (AstroCore bodies branch, GlobeCore submodule);
  no behaviour change`.

### Task 9: Split `SceneController` into layers

**Files:**
- Create in `src/EarthWatch3D.App/Layers/`: `LinesImageLayer.cs` (coastlines, borders and plates, sparse),
  `CloudsImageLayer.cs`, `AreasImageLayer.cs`, `EventsSceneLayer.cs` (wraps `EventScene` and `EventPicker`;
  `IPickable`), `EventLabelsOverlayLayer.cs`, `LightningSceneLayer.cs`, `SunMoonSceneLayer.cs` (globe markers;
  `IPickable` via `MarkerHover`), `SunMoonOverlayLayer.cs` (edge arrows and labels)
- Modify: `Controllers/SceneController.cs`. It keeps only view-model wiring, camera input (mouse, keyboard, wheel,
  `LookAt`, `OnFaceRequested`), power-mode and window events, and builds the `LayerHost` and the layers.
- Modify: `Diagnostics/SnapshotDiagnostic.cs` and `PerfRun.cs` (readiness through `LayerHost.FirstComposeDisplayed`,
  `IsComposeIdle` and an `AreasSettled` flag exposed by `AreasImageLayer`)
- Test: `tests/EarthWatch3D.Tests/LayerWiringTests.cs`

**Interfaces:**
- Consumes: `LayerHost`, the layer base classes, `EarthAssets` and `EarthBody` (Task 7).
- Produces: z-orders that reproduce today's drawing order:

  | Layer | Pass or kind | Order |
  |---|---|---|
  | Clouds | Surface | 10 |
  | Areas | Surface | 20 |
  | Lines | AboveNight | 10 |
  | Events | scene | 100 |
  | Lightning | scene | 110 |
  | Sun and Moon markers | scene | 120 |
  | Labels | overlay | 200 |
  | Sun and Moon arrows | overlay | 210 |

- [ ] **Step 1: Write the failing tests.**
  - `LayerWiringTests.LayersAndOrdersAsDocumented`: the host built by `SceneController` has exactly these layers with
    these passes and orders.
  - `.TogglingLayerVisibilityFollowsViewModel`: setting `Layers.ShowCoastlines = false` hides `LinesImageLayer`'s
    coastlines and requests one recompose.
  - Run them and expect FAIL.
- [ ] **Step 2:** Move the code out of `SceneController` into the layers, one layer at a time, building after each.
  Behaviour, constants and timings move unchanged. Target: `SceneController` under 400 lines.
- [ ] **Step 3:** Run all EarthWatch3D tests and expect PASS, with counts at least those of Task 8 plus 2.
- [ ] **Step 4: Snapshot check** (one launch), as in Task 8 Step 3, into `artifacts\snapshot-task9`. Expected: exit 0.
  If any scene differs, find the cause. Present the difference to the owner only if it is intended; otherwise fix it.
- [ ] **Step 5:** Hand the owner `docs/manual-test.md`'s mouse, keyboard and hover checks for a one-time run in the
  real app (rotate, zoom, hover a quake, click a storm, Face Sun and Face Moon, toggle layers). This is the owner's
  step; record the result.
- [ ] **Step 6:** Commit with the message `EarthWatch3D: SceneController split into GlobeCore layers on LayerHost`.

### Task 10: Documentation and final checks

**Files:**
- Modify: `C:\repos\EarthWatch3D\README.md` (developer section: submodules
  `git submodule update --init`, GlobeCore credit), `THIRD-PARTY-NOTICES.txt` (point to GlobeCore's for moved
  assets), and `docs/superpowers/specs/2026-11-11-earthwatch3d-design.md` §2 (repositories now include GlobeCore)
- Modify: `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md`. Add a "Rulings made
  while building Plan 1" table, including:
  - the Sun uses `IauRotationModel` with the IAU Sun elements instead of a separate `CarringtonRotation` class;
  - `CountryIndex` moves to Plan 2, since EarthWatch3D does not need it;
  - rotation models exist only for the Sun, the Moon, the Earth and Mars until an app needs more;
  - these stay in EarthWatch3D until a second app needs them: the Sun and Moon markers and arrows,
    `MoonPhaseGlyph`, `TimelineViewModel`, `SunMoonViewModel`, the status-dot view, the base `LaunchOptions` parser
    and `ISurfaceImagery` (spec §3.3 and §3.5). EarthRelief3D's Plan 2 moves the status dots and the launch-option
    parser, which it needs;
  - the measured noise floor and Horizons errors.

- [ ] **Step 1:** Update the documents above. Verify that the About window's licence list still opens and names the
  moved assets: run `--dialog about --snapshot` once (one launch) with `--offline-samples`, then read the PNG.
- [ ] **Step 2:** Run all three test suites, Release build, one last time. Write the final counts into
  `plan-1-baseline.md`.
- [ ] **Step 3:** Commit in each repo, with the messages `Docs: GlobeCore switch-over` and `Docs: Plan 1 rulings`.
- [ ] **Step 4:** Request the whole-branch review (superpowers:requesting-code-review) across AstroCore `bodies`,
  GlobeCore `main` and EarthWatch3D `globecore`.
