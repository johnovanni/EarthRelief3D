# Plan 3 (atrocities, needs and resources) results

Built 2026-11-23 on branch `plan-3`: the curated atrocity designations and prison camps (drafts for the owner's
review), IPC food-crisis areas, the Needs shadings (water, sanitation, health care, hunger, shelter need), the Resources
shadings (oil production, water stress), the DataTool and its three bundled extracts. **Task 7 (the GEM power, oil and
gas, and steel marker layers) is deferred** until the owner's GEM files arrive (ruling R18). The rulings are in the
spec, "Rulings made while building Plan 3".

## Tests

`dotnet build EarthRelief3D.sln` in Release and in Debug (`--no-incremental`): 0 warnings, 0 errors. `dotnet test` in
Release and in Debug: **377 passed, 0 failed, 0 skipped** (226 before Plan 3; 371 after Task 6; Task 8 added 6:
the scenes' `showDrafts` and curated `panelFocus`, switching the drafts per scene, the Palestine and camp tooltip sizes,
and the tooltip wrap). GlobeCore is unchanged (fa83558).

## Counts in the offline run (samples judged 2026-11-22)

From the run's log:

- Bundled: FAO undernourishment 169 countries and dietary energy adequacy 160; US EIA crude oil 210; WRI Aqueduct
  water stress 189; no code left unplaced.
- WHO GHO (synthetic samples, ruling R5): water, sanitation and UHC 15 countries each.
- UN HNO: plan 2026, 20 countries, 64 rows; a shelter share for 11.
- IPC: 44 countries with people in Phase 3 or higher (8 stale), 6 with a projection only. IPC areas: 0 countries drawn
  by area offline (the offline search finds no area package), 36 keep the 20% rule.
- UCDP 28 conflicts from 4,494 events; WHO 7 outbreak markers from 5 reports; UNHCR 201 countries of origin.
- Curated (with the drafts shown, at 22 Nov 2026): the atrocity legend counts 3 countries with a triangle; 5 camp
  squares.

## Snapshots

- Command: `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json
  --snapshot-out artifacts\snapshot-plan3`, then the same into `artifacts\snapshot-plan3-2` for the repeat. Pictures
  are 1482 x 903 pixels (RenderTargetBitmap of the window).
- Nine scenes, all at 2026-11-22T12:00:00Z: the five Plan 2 scenes unchanged, and
  - `water` (10,25; the Water shading only);
  - `resources` (25,50, the Persian Gulf; the Oil production shading only, no GEM layers: R18);
  - `myanmar-panel` (21,96; atrocities on; panel MMR focused on "Genocide and mass atrocities"; `"showDrafts": true`);
  - `dprk-camps` (40,127; camps on; panel PRK focused on "Prison camps"; `"showDrafts": true`).
- The planned `nigeria-areas` scene was dropped (ruling R19): the sample's Nigeria national record is projection-only,
  so its areas cannot match offline (R4). **Area drawing is covered by `FoodAreaMapTests`' pixel tests; a picture of
  real-data areas awaits the owner's hands-on check.**
- A scene's `showDrafts` turns the curated drafts on for that picture only (the five Plan 2 scenes keep them off), as
  `--show-drafts` does for a whole run; `panelFocus` may now be `atrocities` or `camps`.
- **Repeat check: PngDiff reported all nine pictures identical** between the two runs (exit code 0).
- **Against `artifacts\snapshot-baseline`** (the five old scenes): all five differ, as expected. A pixel diff by region
  shows **no change on the globe in any of the five**; the changes are:
  - the layer strip (16 toggles in four groups now, in place of four toggles and three "coming in a later version"
    lines; it scrolls, see below) and so the legend card, which starts lower and is shorter;
  - the legend's food line (Task 4's wording for IPC areas);
  - the Sources card (10 dots: the 7 feeds and the 3 bundled extracts, in place of 5);
  - the country panel: in `sudan-panel` and `sudan-panel-food` only the panel's scroll bar (the panel is longer, so
    its thumb is smaller; the visible text is identical); in `no-crisis-panel`, Iceland's panel gains the "Basic needs"
    section (the GHO figures in it are the synthetic sample's) in place of the "coming in a later version" note.
  The panel changes are beyond "the legend and layer strip" but are what Plan 3 adds; no other change was found.
- Not copied over `artifacts\snapshot-baseline`: the owner decides. Copy `artifacts\snapshot-plan3\*.png` over it to
  make the nine pictures the baseline.

### What was seen

- `water`: the legend reads "Share of people without at least basic drinking water, 2024", with the five BuPu classes,
  "no figure: not shaded" and WHO's source line ("estimate year 2024"). The sample's 15 countries are shaded: in view,
  Sudan, South Sudan, Somalia, Yemen and Ukraine in the top class (50% or more), Chad, Niger and China in 30–50%,
  DR Congo and Afghanistan in 15–30%; every other country shows the plain map, clearly different from the tinted ones.
  The values are invented (R5).
- `resources`: the legend reads "Crude oil production, including lease condensate, in thousand barrels per day, 2025",
  with the five YlGnBu classes and "no figure: not shaded"; Saudi Arabia and Russia in the top class, Iraq and Iran in
  the next. EIA has a figure for 210 countries, so an unshaded "no figure" country is hard to find in this view. The lowest class ("under 10",
  pale cream at about 60%) over desert is close to the unshaded desert colour: legible, but subtle.
- `myanmar-panel`: a hollow white triangle with a dark edge over Myanmar, clearly visible. A second, foreshortened
  triangle sits on Palestine near the globe's edge. The panel opens at "Genocide and mass atrocities", and its row is
  labelled "Draft: not yet reviewed · UN body finding: crimes against humanity", with the authority, the date, the
  situation, the group and the IIMM's July 2026 continuing statement; the section's source line ends "drafts shown: not
  yet reviewed". The legend's atrocity line explains the triangle, counts 3 countries and says "Drafts shown: not yet
  reviewed (diagnostic run)".
- `dprk-camps`: yellow squares in North Korea in two clusters, one north-east of Pyongyang (Camps 14, 15 and 18, about 39.6° N, 126.1–126.9° E) and one in
  the north-east near Chongjin (Camps 16 and 25, about 41.2–41.8° N, 129.4–129.7° E), where the camp list puts them. At the whole-globe view the squares of each cluster overlap, so five
  separate squares cannot be counted in the picture (the count of 5 is checked by `Camp18KeepsItsMarkerAndShowsBothSources`).
  The panel's rows are labelled "Draft: not yet reviewed · Political Prison Camp No. 15 (Yodok) (Kwan-li-so No. 15)" and
  so on; the legend says "Drafts shown: not yet reviewed (diagnostic run)".
- The layer strip at the default window size: 16 toggles in Crises, Needs and Resources; it is capped at 62% of the
  height and scrolls. Visible without scrolling: the six Crises toggles and Water, Sanitation, Health care and Hunger.
  Shelter need, the Resources group and the Settings gear are below the fold; in `resources` the active "Oil
  production" toggle is not in view (the legend names the shading). The strip's width changes slightly with which
  toggles are off ("off" labels widen it).

### Found, not fixed (one launch was left, and it was needed for the repeat check)

- **`dprk-camps`: the panel focused on "Prison camps" opens in the middle of the section,** not at its heading: the
  picture shows the end of Camp 14's row, then Camps 15 and 16. The section is taller than the panel, and
  `CountryPanel.xaml.cs` calls `BringIntoView()` on the whole section, which then scrolls only as far as needed. A click
  on a camp square in the app does the same. A likely fix is to bring only the section's top into view
  (`item.BringIntoView(new Rect(0, 0, item.ActualWidth, Math.Min(item.ActualHeight, Scroller.ViewportHeight)))`); it
  needs one launch to confirm in a picture.
- The atrocity legend says "checked 23 Nov 2026" while the scenes' display time is 22 Nov 2026 (the samples' day): the
  curated tables were checked a day after the samples were taken.

## Not checked by a picture

- **Settings flyout** ("⚙ Settings" at the end of the strip): no scene can open it, so its layout is not checked by a
  picture. `SettingsViewModelTests` covers its content.
- **Hover tooltips** are not in pictures. Palestine's atrocity tooltip (6 current entries with the drafts shown: the
  pending case, three provisional-measures orders, the ICC warrants and the COI finding) is measured in a test,
  `PalestineTooltipFitsOnScreen`: its ICC warrants line was 215 characters, about 1,140 px wide on a 1,200 px window.
  The atrocity and camp tooltips now wrap lines over 110 characters at a space (`TooltipText.Wrap`): Palestine's is
  11 lines, about 577 x 190 px; the widest camp tooltip about 600 px.
- IPC areas on the map (R19, above), the Sanitation, Health care, Hunger, Shelter need and Water stress shadings, and
  the panel's "People in need" and "Resources" sections (tests only: `CountryValueLayerTests`, `NeedsPanelTests`,
  `ResourcesPanelTests`).
- The GEM marker layers (Task 7 has not run).

## App launches in this task

3:
1. A first launch from Git Bash lost the backslashes in its arguments (`testsSamples`), so the app showed its
   command-line error box and was closed; no pictures. My mistake in quoting.
2. The snapshot run into `artifacts\snapshot-plan3`.
3. The repeat run into `artifacts\snapshot-plan3-2`.

No re-run after tuning was possible within the budget, so nothing was tuned after looking. The tooltip wrap was made
before the snapshot run, from the test's measurement.

## Final fixes (after the final review, 2026-11-23)

- Fixed: collapsible Needs and Resources groups (R20); a focused panel section opens at its heading; the camp line says
  "checked <date>" and each camp tooltip its newest report; "UN humanitarian needs overview" (not "plan"), and a
  country not in the HNO file reads "Not in OCHA's 2026 needs-overview file on HDX"; water stress is dated "baseline
  1979–2019 (published 2023)" with no year appended; EIA says "year 2025"; hidden shadings free their image; an
  unreadable IPC feature no longer stops a run; IPC rejections are saved across restarts; a bundled extract that fails
  shows nothing (not an older cached copy); the panel's "Basic needs" shows FAO's undernourishment; interim steps with
  no finding sit under "Genocide and mass atrocities, interim steps: not on the map"; the US scope says "before 2001";
  the spec's rulings table has R20; the README no longer names Task 7.
- Tests: `dotnet build EarthRelief3D.sln` in Release and Debug (`--no-incremental`): 0 warnings, 0 errors.
  `dotnet test` in Release and Debug: **390 passed, 0 failed, 0 skipped** (377 before).
- Launch (the plan's 4th and last): `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes
  tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-plan3-final` (Release build, from PowerShell),
  exit code 0, nine pictures.
- PngDiff against `artifacts\snapshot-plan3`: **all nine differ** (exit code 1), as expected: every scene has the new
  strip (the closed groups' headers, or a group opened for the scene's layer), and the panel scenes scroll to the
  focused section's top. Not copied over `artifacts\snapshot-baseline`.
- What was seen:
  - `africa`: the strip is shorter (Crises, then "▸ Needs closed" and "▸ Resources closed", "Getting help in" and
    Settings all in view, no scroll bar), so the legend card starts higher and shows more lines (the food line's phase
    keys and outline key now in view).
  - `resources`: the Resources group is open and its active "Oil production" toggle is in view; Needs is closed. The
    legend's source line ends "year 2025".
  - `dprk-camps`: the panel opens with the "Prison camps" heading at the top of its view, then Camp 14's row. The
    legend's camp line reads "... curated camp sites, newest report 18 Jun 2024 · checked 23 Nov 2026".
  - `water`: the Needs group is open with Water on; Resources is below the strip's fold (the strip still scrolls when a
    group is open).
- Not pictured: the water-stress shading (its "baseline 1979–2019 (published 2023)" wording is checked by tests only),
  the camp tooltips' "newest report" (hover), the interim-steps section and the undernourishment row.

## GEM layers (Task 7, 2026-11-24)

- Data: `DataTool gem-power`, `gem-oilgas` and `gem-steel` on the owner's files (docs/data-sources.md §11) wrote
  `data/datasets/gem-power` (**4,113** operating plants of 500 MW or more, units summed per plant, 118 countries),
  `gem-oilgas` (**4,933** operating fields; 6,055 with coordinates passed the 5,000 cap, so the fields GEM reports
  producing under 0.2 million barrels of oil equivalent a year are left out, ruling R23) and `gem-steel` (**600**
  operating plants of 1 million tonnes of crude steel a year or more, 60 countries). No code was left unplaced. Power
  rows from TransitionZero (CC BY-NC 4.0) are never bundled (ruling R22); none was 500 MW or more.
- Tests: Release and Debug builds 0 warnings; `dotnet test` **404 passed, 0 failed** (390 before; `DataToolGemTests` 8,
  `ResourceMarkersTests` 6).
- Launch (outside Task 8's budget, ruling R18): `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes
  tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-gem` (Release, from PowerShell), exit code 0, nine
  pictures. The `resources` scene now turns on `power`, `oilgas` and `steel` over the oil production shading.
- `resources.png` (the Gulf, Europe and South Asia): the three layers draw together. Power plants are edged discs in the
  five fuel colours (orange fossil discs thickest over the Gulf, India and China; pink nuclear and blue hydro in Europe;
  a large blue disc for a big hydro plant); oil and gas fields are vermillion diamonds, densest over the Gulf, the
  Caspian, the Volga-Urals and western Siberia; steel plants are green square outlines (Europe, Iran, India, China). The
  Resources group is open with Oil production and Power plants in view (Oil and gas fields and Steel plants are further
  down the strip). The legend shows the oil shading, then "Power plants: operating plants of 500 MW or more (units summed
  per plant), coloured by fuel" (since R24: "coloured by its largest fuel") with the five fuel keys and four size keys, and GEM's credit (R24 wording) starting "Global Energy
  Monitor, Global Integrated Power Tracker, September 2026 release." with "(file V2)" as the version note, then "Licence: CC BY 4.0, https://…"; the oil and gas and
  steel lines are below the legend's fold (it scrolls). Eleven green status dots (GEM's is the last).
- Not checked by a picture: the hover tooltips (tests pin their wording), the oil and gas and steel legend lines (below
  the fold), and a close-up: at this zoom the markers crowd each other over the Gulf and eastern China.

## Renewables layer (ruling R25, 2026-11-24)

- Data: `DataTool gem-renewables` on the same power workbook wrote `data/datasets/gem-renewables`: operating units of
  GEM's Types wind, utility-scale solar (its Solar Thermal technology included), hydropower, geothermal and bioenergy,
  summed per plant, R22 applied. Thresholds tried: 100 MW 9,047 plants, 150 MW 5,379, 200 MW 4,021, so **200 MW**:
  **4,020 plants** drawn (wind 1,429, utility-scale solar 1,271, hydropower 1,265, bioenergy 41, geothermal 14; one in
  Réunion not placed). docs/data-sources.md §11e.
- Look: a filled hexagon with a dark edge, used by no other layer (C8), in the power layer's fuel colours by the plant's
  largest fuel. The legend names the threshold and the fuel groups present (hydro, wind or solar, other), gives four
  size keys, and says that plants of 500 MW or more also appear in Power plants (1,058 do).
- Tests: Release and Debug builds 0 warnings; **415 passed, 0 failed** (405 before).
- Launch: `EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json
  --snapshot-out artifacts\snapshot-gem-2` (Release, PowerShell argument array), exit code 0, ten pictures. For this run
  `resources` also turned on `renewables`, and a new `renewables` scene was added.
- Decision on the scenes: in `resources.png` with all four marker layers, yellow wind and solar hexagons smother India
  and eastern China and cover the power discs, and over the Gulf the hexagons and discs cannot be told apart. So
  `resources` stays at oil shading plus power, oil and gas, and steel (as pictured in `artifacts\snapshot-gem`), and the
  renewables have their own scene, `renewables` (Europe, lookAt 50, 10, water stress shading, no other markers).
- `renewables.png`: blue hydro hexagons along the Alps, Norway, Sweden, the Caucasus, Turkey and Russia's rivers; yellow
  wind and solar hexagons over the North Sea coasts, Germany, the UK, Spain, Turkey and the Gulf; a few grey "other"
  (bioenergy) around the North Sea and in Sweden. Each hexagon has its dark edge and reads apart from the shading. The legend
  shows the water-stress line, then "Renewable power plants: operating renewable plants of 200 MW or more (renewable units
  summed per plant), coloured by its largest fuel" with the hydro, wind or solar, and other keys and four size keys; the
  overlap sentence starts at the legend's fold. The strip's Renewable power plants toggle is below its fold.
