# SDD ledger — plan: C:/repos/EarthRelief3D/docs/superpowers/plans/2026-11-23-plan-3-atrocities-needs-resources.md
Spec: C:/repos/EarthRelief3D/docs/superpowers/specs/2026-11-21-earthrelief3d-design.md
Owner chose subagent-driven on 2026-11-23. Local commits are OK; no push without the owner.
Branch `plan-3` was made from main c514ef5; the plan was committed as 029e3ac. GlobeCore stays at fa83558 and is not changed.

## Pre-flight scan

| Pair or task | Shared file or interface | Finding |
|---|---|---|
| T1→T3, T4, T6 | samples, data-sources.md, the IPC GeoJSON analysis property and its CKAN query | Consistent. T1 says it may simplify the sample "by the DataTool's simplify (Task 6) or by hand", but the DataTool doesn't exist yet → ruling R1 |
| T2→T3 | IndicatorStore and the series ids gho.*; T3 adds the HNO list | Consistent |
| T2→T5 | PointMarkerLayer for camps; the MarkerLayer enum has Atrocities and Camps | Consistent |
| T2→T6, T7 | Series fao.undernourishment, eia.crudeOil, wri.bws; marker layers for GEM | The toggles exist from T2, but the data only arrives in T6 and T7 → ruling R2 |
| T2 (internal) | The Hunger scale says "values under 2.5 are shown as 'under 2.5%: not shaded'", but P3-7 says an unfilled country means "no figure" | Conflict → ruling R3 |
| T3↔T6 | The panel's "Basic needs" section: T6 adds the food-supply row | Consistent |
| T4 vs T2 | LegendViewModel and FoodImageLayer; tasks run in sequence | Consistent |
| T5→T8 | `--show-drafts` (T5) and the scenes' "showDrafts" (T8) | Consistent |
| T6→T7 | The DataTool command set | Consistent |
| T8 vs Global | Launch budget: T8 uses 3, T4 at most 1, 4 in all | Consistent |
| T1, T3, T4, T5, T6, T7, T8 (internal) | Tests against code | Each task's tests match its files; no test that asserts nothing |

## Rulings
- R1: in Task 1, the IPC GeoJSON sample is trimmed and simplified with a one-off script kept outside the repo (or by hand), not with the DataTool, which Task 6 creates — no DataTool before Task 6 — cost if wrong: none.
- R2: in Task 2, a shading or marker toggle whose data isn't loaded yet shows its legend line with "Data not loaded yet" and draws nothing. Tasks 6 and 7 fill it in. — keeps the strip final from Task 2 — cost if wrong: an interim build shows empty layers.
- R3: Hunger uses the FAO Hunger Map's five classes, with lower bounds 0, 5, 15, 25 and 35 (labels "under 5%", "5–15%", "15–25%", "25–35%", "35% or more"), so every country with a figure is filled. A FAO value written "<2.5" is stored as 2.5 with the flag "<2.5" and shown as "under 2.5%" — P3-7 says unfilled means "no figure" — cost if wrong: the bottom class merges FAO's <2.5 and 2.5–5.

## Progress
- Task 1: dispatched implementer (sonnet), BASE 029e3ac
- Task 1: DONE_WITH_CONCERNS f4f241e, 226 tests. The IPC GeoJSON has anl_id and ipc_period C/P but no dates; the dates are in the same dataset's ipc_<iso3>_national_long_latest.csv. CKAN query: fq=organization:ipc AND groups:<iso3>; pick the package by res_name. GHO samples are 1.5 MB (not trimmed to 15 countries).
- Ruling: R4 Task 4 takes an area set when its GeoJSON's areas with ipc_period = C share one anl_id, and the same dataset's ipc_<iso3>_national_long_latest.csv gives that analysis a current period equal to the national record's From/To. Otherwise the country keeps R17. That is one extra small CSV per country. — the GeoJSON carries no dates — cost if wrong: a few countries stay on R17.
- Ruling: R5 the GHO test samples become synthetic: the same JSON shape, the 15 named countries, invented values, and a README note, like P2-5 for WHO DON. The app still fetches real GHO live. — the GHO page states no licence of its own; synthetic samples remove any doubt for a public repo — cost if wrong: none (tests need the shape, not the values).
- Ruling: R6 Aqueduct is bundled only if WRI's own data-licence page (e.g. its Open Data Commitment) confirms CC BY 4.0. Otherwise the water-stress layer is "link only" and is recorded so in Task 6. — the Read Me states no licence — cost if wrong: a layer missing until confirmed.
- Ruling: R7 HNO cluster rows exist for 11 countries only (Sudan and Yemen have only ALL). The panel shows whatever rows exist; ShelterNeed shading covers only countries with an SHL row. — that is what the data gives — cost if wrong: none.
- Carry to Task 8: the README still says EarthRelief3D has no remote (wrong since 2026-11-22); fix it there.
- Task 1: fix round 1/5 sent (R5: synthetic GHO samples).
- Task 1: squashed f4f241e+9b15111 into b17f265 (real GHO rows out of history; unpushed). Review dispatched (sonnet).
- Task 1: review Needs fixes. I1: data-sources section 5 "delete the 15-country extract if NC-SA" is stale after R5. I2: the README lacks the CC BY-IGO credit on the HNO samples.
- Task 1: minor (deferred): the README line "never edit by hand" vs the synthetic and trimmed files; the GHO sample Date wording; $natRes is not null-checked; ConvertFrom-Json -DateKind needs PS 7.5 (header comment); "fourth download" wording; section 10 should name WRI's Open Data Commitment (R6). Cheap ones are folded into fix round 1.
- CARRY to Task 4: ipc_nga.geojson has only ipc_period = P areas. Task 4 needs a C-period fixture (a synthetic variant made in the test, or a second trimmed sample).
- Task 1: fix round 1/5 (cf03319): 2 Important + minors sent; scoped re-review dispatched (haiku).
- Task 1: re-review clean. Task 1: complete (029e3ac..cf03319, review clean). Task 1: minor (deferred): data-sources section 5 "Latest values in the sample" should say "in the live answer"; the Plan 2 IPC README entries lack the courtesy credit; the WRI Open Data Commitment URL is unverified (Task 6 checks it per R6).
- Task 2: dispatched implementer (opus), BASE cf03319
- Task 2: DONE_WITH_CONCERNS ec1ad57, 259 tests, 0 warnings. Two existing tests adapted (layer order; strip tooltip via the VM).
- Ruling: R8 the power-plant marker is a disc with a dark edge (◉), unlike conflict's plain dots (C8) — the two can show together — cost if wrong: a shape tweak.
- CARRY: Task 3 stores the shown value (100 − GHO, 100 − UHC) with matching Measures wording; Task 6 checks the WRI category labels against the file; Task 8 checks the strip's scrolling at the default size; Task 5 adds the atrocity scene layer.
- Task 2: review dispatched (sonnet).
- Task 2: review Approved with one Important. I1: a value with no class (e.g. WRI -9999, a negative 100 − x) draws nothing, but its tooltip shows a figure. Minors 2 (legend year range should use each country's newest year) and 5 (comment) are folded into the fix. Task 2: minor (deferred): invalidating hidden layers on every indicator change; SetLayers saves once per marker.
- Task 2: fix round 1/5 sent (I1 + M2 + M5).
- Task 2: fix e233a42 (261 tests); CARRY to Task 3: use CountryValueLayer.HasFigure in the panel. Re-review dispatched (haiku).
- Task 2: fix round 1/5 (2 addressed, 1 open — HasFigure's qualifier clause lets a qualified value below the first break show a figure; e233a42)
- Task 2: fix round 2/5 (1 addressed, 0 open; 066faf8). Task 2: complete (cf03319..066faf8, review clean).
- Task 3: dispatched implementer (sonnet), BASE 066faf8
- Task 3: DONE_WITH_CONCERNS 10dfe90, 288 tests. Concerns: the HNO resource is chosen by URL file name; GHO licence still an owner question; two panel sections (WHO and OCHA); HumanitarianNeed.Basis added; health unit 'points'; shelter legend 'as of' is the file load date; volume of unresolved GHO codes unknown. Review dispatched (sonnet).
- Task 3: review Needs fixes. I1: the shelter legend prints the HNO file's load date as "as of" (it must say "file loaded"). Minors 1-5 folded into the fix (GHO "as of 31 Dec" invents a day; row labels "No basic …" must be "Without at least basic …"; absence lines need a checked date; the package last_modified fallback; separate GHO series writes). Task 3: minor (deferred): the SDN section-count test excludes by prefix.
- Note for the final review: attribution strings are stored but printed nowhere yet. Plan 5's About/credits covers this, so it is not a Plan 3 defect.
- Ruling: R9 accept the two panel sections ("Basic needs" from WHO, and "People in need (UN humanitarian plan <year>)" from OCHA) and the HNO choice by URL file name — PanelSection has one source line per section; the CKAN resource name isn't the file name — cost if wrong: layout only.
- Task 3: fix round 1/5 sent.
- Task 3: fix af9cc2c (293 tests): the CitationDate DateMeaning flag (AsOf/FileLoaded/EstimateYear), IndicatorStore.ReplaceAll. Re-review dispatched (sonnet).
- Task 3: fix round 1/5 (5 addressed, 1 Minor partial — the "Not placed on the map" list at LegendViewModel.cs:141 prints the raw AsOfUtc (31 Dec 2024 / a bare load date) instead of DataDate). Task 3: complete (066faf8..af9cc2c, 1 Minor parked → folded into Task 4's dispatch, same file). Task 3: minor (deferred): no test round-trips an old indicators.json without DateMeaning.
- Task 4: dispatched implementer (opus), BASE af9cc2c (carrying R4, the C-period fixture, the Task 3 minor at LegendViewModel.cs:141 and the old-cache round-trip test)
- Task 4: DONE_WITH_CONCERNS 43089e3, 309 tests, no launch. Areas report through the IPC dot; the C-period fixture is synthetic (in the test). Concerns: R4 does not compare Date of analysis; offline runs can't show areas; one log line per run per rule-shaded country.
- Ruling: R10 R4 also requires the CSV's Date of analysis to equal the national record's (the guard is cheap) — cost if wrong: none. To be added in the review fix round.
- CARRY to Task 8: for the nigeria-areas scene, add an offline sample of a current-period area set: a synthetic variant of ipc_nga with ipc_period C and a matching national CSV row, labelled synthetic in the README.
- Task 4: review dispatched (sonnet).
- Task 4: review Needs fixes. I1: R10 is missing (the Date of analysis comparison). Minors folded into the fix: remember search/CSV rejections and space out requests; a bounding-box prefilter for the hover; load cached areas off the UI thread; a test of the area citation's DateMeaning; the legend's "Where" capital. Task 4: minor (deferred): no test of a Phase-1/2 area hover; real-data legibility left for Task 8 or the owner.
- Ruling: R11 for R10, if either side's Date of analysis is blank, reject: the country keeps R17 and the log says why — it can't confirm the same analysis — cost if wrong: one country stays on R17.
- Task 4: fix round 1/5 sent.
- Task 4: fix d3cc058 (314 tests). Re-review dispatched (sonnet).
- Task 4: fix round 1/5 (6 addressed, 0 open; d3cc058). Task 4: complete (af9cc2c..d3cc058, review clean). Task 4: minor (deferred): the pause test's under-150 ms wall-clock bound could flake.
- Task 5: dispatched implementer (opus), BASE d3cc058
- Task 5: DONE_WITH_CONCERNS d61ac89, 335 tests. 31 designations, 5 DPRK camps and the US government row, all drafts; about 10 candidates left out (report). The ICJ Gambia v. Myanmar merits PDF sits behind a bot check (not bypassed); the press release exists.
- Ruling: R12 the Gambia v. Myanmar merits row stays out until it is confirmed from the Court's own text. curated-review.md lists it as "awaiting confirmation" with the Court's links, so the owner can read the judgment and confirm — bot checks are never bypassed — cost if wrong: one designation missing until the owner confirms.
- Ruling: R13 US completeness (C15) covers formal Secretary of State determinations published on state.gov and its archives (2001 onward). 1990s US statements are listed for the owner in curated-review.md; governments.json records this scope, and Settings shows it — the owner decides whether that counts as complete — cost if wrong: the scope text changes.
- Task 5: review dispatched (opus, with spot-checks of citations).
- Task 5: review Needs fixes. Spot-check: 12 rows match primary sources, 1 blocked (ICC), 0 mismatches. Important:
  1. interim steps (pending, provisional measures, warrants) can drive the glyph alone;
  2. the HRV history note implies a lapsed genocide;
  3. the US scope undercounts the earlier conclusions (State says 7 before Burma; 4 before 2001);
  4. the review doc relies on the untracked report (R12/R13 not done);
  5. the Sudan continuing statement should quote para 123 and name A/HRC/63/23 as checked, with the US genocide glyph as an owner question.

  Minors folded in: the fixed-Now date check; the camps' UtcNow fallback and oldest-report link; the Settings wording; Camp 18 (closed 2006) counted as a current crisis; documentedBy includes the COI (record the deviation). Task 5: minor (deferred): check the length of the PSE tooltip in Task 8.
- NOTE: the reviewer's browser opened a file-save dialog for the Camp 18 PDF in the owner's browser pane; tell the owner to cancel it.
- Ruling: R14 a camp counts as a current crisis (C11) only if its newest cited report does not say it closed. A camp reported closed is listed in the panel history as "reported closed (<source>, <year>)", with no marker — a closed camp isn't a current crisis — cost if wrong: one fewer marker.
- Task 5: fix round 1/5 sent.
- Task 5: fix 9e7599c (339 tests): all 5 Important fixes and the minors done; 9 candidates listed in the review doc. Concern: my R14 Camp 18 instruction contradicts R14's own rule, since HRNK 2024 (the newest report) says parts of Camp 18 remain active.
- Ruling: R14b (corrects R14's Camp 18 example): the rule follows the data. Camp 18's newest cited report (HRNK, 2024) says parts remain active, so it keeps its marker. Its panel and tooltip show both facts: "UN Commission of Inquiry (2014): reported closed in 2006" and "HRNK (2024): parts remain active". The owner question stays on the row — my example contradicted my own rule; the rule is the principle — cost if wrong: one marker.
- Task 5: fix round 2/5 sent (R14b only).
- Task 5: fix rounds 1-2 committed (9e7599c, 47f26d0; 340 tests). Scoped re-review over d61ac89..47f26d0 dispatched (sonnet).
- Task 5: fix rounds 1-2 re-review: all addressed, no new Critical or Important; spot-checks confirmed CRP.1 para 739 and A/HRC/61/77 para 123. Task 5: complete (d3cc058..47f26d0, review clean).
- Task 5: minor (deferred, final wave): the US scope says "1989–1995" more firmly than the doc's uncertainty about which four conclusions; the Sudan statement is repeated in 3 rows (drift risk); an interim row with a recent statement but no finding shows "Continuing" under the history note.
- Task 5 decision D1 (recorded here durably): camps.json documentedBy includes the UN COI A/HRC/25/CRP.1 as well as HRNK reports (the brief said HRNK) — the COI is the authority that names the camps.
- Task 6: dispatched implementer (opus), BASE 47f26d0
- Task 6: DONE_WITH_CONCERNS 36e8d5a, 362 tests. Aqueduct confirmed CC BY 4.0 on WRI's Open Data Commitment page, so it is bundled. Oil top 5 (2025): USA, RUS, SAU, CAN, IRQ. Yemen has no FAO undernourishment figure; Sudan's newest is 2020-22.
- Ruling: R15 reading UN_A3 from GlobeCore's public EarthAssets countries file (read-only, GlobeCore unchanged) is acceptable — no GlobeCore change; the public API is used — cost if wrong: none.
- Task 6: review dispatched (sonnet).
- Task 6: review Needs fixes. I1: the Hunger legend source line and status dot say "estimate year 2025" for 3-year averages, and the legend's "2019–2025" mixes end-years. Spot-checks: Haiti 51.4, Sudan 10.9 (2020-22), China <2.5 and Taiwan 3.6 match the raw file; WRI's CC BY 4.0 is confirmed on its Open Data Commitment page; EIA matches the downloaded source only (bulk page 403).
- Ruling: R16 codes with no map unit and a value of 0 (EIA's 8 zero-production territories) are not listed under "Not placed on the map"; they stay recorded in the manifest. Codes with a non-zero value are still listed — a zero value with no map unit carries no information for a user — cost if wrong: none.
- Ruling: R17b the WaterStress legend shows the grey arid key only when some country in the data has that category — no empty classes — cost if wrong: none.
- Task 6: fix round 1/5 sent (I1 + docs §10 + R16 + R17b + hover shows the country's own FAO range).
- Task 6: fix 5e82c7c (371 tests): CitationDate.ThreeYearAverage. Re-review dispatched (haiku).
- Ruling: R18 the GEM files are not in .downloads\gem, so Task 8 runs before Task 7. Task 8's `resources` scene shows oil production shading only (no GEM markers). When the owner's files arrive, Task 7 re-takes that one scene with one extra launch, outside Task 8's budget — the plan must not wait on the owner — cost if wrong: one extra launch later.
- Task 6: fix round 1/5 (4 addressed, 0 open; 5e82c7c). Task 6: complete (47f26d0..5e82c7c, review clean).
- Ruling: R19 Task 8 drops the `nigeria-areas` scene: the sample's NGA national record is projection-only, so R4 can't match offline, and a synthetic national record would contaminate the shared samples. Area drawing is covered by FoodAreaMapTests' pixel tests, and plan-3-results says so — no fake data in pictures — cost if wrong: no picture of real-density areas until the owner's hands-on check.
- Task 7: deferred (owner's GEM files absent). Task 8: dispatched implementer (opus), BASE 5e82c7c
- Task 8: DONE_WITH_CONCERNS a6076bb, 377 tests; 3 launches (1 lost to a quoting error, then 9 scenes, then a PngDiff-identical repeat). Concerns:
  1. dprk-camps: the panel opens mid-section with the heading scrolled off (the fix needs 1 launch);
  2. a tooltip wrap was added (PSE 1,140 → 577 px);
  3. the tall strip squeezes the legend, and Resources is out of view;
  4. "checked 23 Nov" on 22 Nov scenes;
  5. synthetic GHO values in the offline pictures;
  6. camp squares overlap at whole-globe zoom;
  7. the Settings flyout and real areas are not pictured.
- Controller looked at africa, myanmar-panel and water: the triangle shows over Myanmar; the water legend names the measure and year; the strip scrolls, hiding Resources.
- Task 8: review dispatched (sonnet). Launch budget: 3 of 4 used, 1 left for the final fix wave.
- Task 8: review Approved. Task 8: complete (5e82c7c..a6076bb, review clean). Two Important user-facing items go to the final wave, under ruling R20 below.
- Ruling: R20 the final fix wave makes Needs and Resources collapsible (collapsed unless one of their layers is active; the state is saved), and focuses a panel section at its top. africa, resources and dprk-camps are re-taken in the last launch (4 of 4) — the strip squeezed the legend and hid the active toggle; a section opened mid-way — cost if wrong: one more click to open a group.
- Task 8: minor (deferred): spec cells should say "Plan 2's R17"; the README's "Plan 3 Task 7" wording; the PSE tooltip test hard-codes 6 entries and uses the system font; an optional SAMPLE DATA caption; camp squares overlap at globe zoom (no scene zoom).
- Final whole-branch review dispatched (opus): c514ef5..a6076bb.
- Final review: ready to merge with fixes. 377 tests pass. Important:
  - I1: the camp legend and tooltip dates look current; they need "checked" and "newest report".
  - I2: the HNO absence sentence overclaims, and "plan" should be "needs overview".
  - I3: water stress shows "estimate year 2019"; it should be "baseline 1979–2019 (published 2023)".
  Minors 1-8 and the fix-before-merge deferred items are triaged. R20 confirmed (no automatic collapse).
- Final fix wave dispatched (opus) with final-fix-brief.md, BASE a6076bb. It uses the last launch (4 of 4).
- Final fix wave: DONE_WITH_CONCERNS, 8 commits a6076bb..24180e2, 390 tests (Release and Debug), launch 4/4 (9 scenes; africa strip shorter; resources toggle in view; dprk-camps opens at its heading).
- Ruling: R21 accept the fixer's group start rule: a group with an active layer always starts open; otherwise it follows the saved choice, or starts closed if there is none. A failed bundled extract shows nothing and says why. The strip may still scroll when a group is open — the brief's two requirements conflicted; this honours both — cost if wrong: a group stays open by the user's own choice.
- Scoped re-review of the fix wave dispatched (sonnet).
- Final fix re-review: all addressed, no new Critical/Important. Minors (fine to leave): a hidden-layer compose still rasterizes; a Baseline without DateText prints 'baseline ending N'; no test for a corrupt rejection file. Controller verified the Release build (0 warnings) and the tests.
- 2026-11-23: owner chose merge locally. main fast-forwarded to a534db6; 390 tests pass on main; branch plan-3 deleted. Not pushed. The workspace is kept for Task 7.
- 2026-11-24: the owner reports the GEM licence page https://globalenergymonitor.org/creative-commons-license. The controller read it: the full text is the Creative Commons Attribution 4.0 International Public License (CC BY 4.0). Bundling is allowed with attribution, a licence link and an indication of modification. Task 7 proceeds once the files are in .downloads\gem (not there yet).
- 2026-11-24: the owner supplied the GEM files: Power (September 2026 V2), Oil and Gas Extraction (March 2026), and Iron and Steel (September 2026 V1; plant-, steel-unit and iron-unit files from gem-download.zip, extracted to .downloads\gem\steel). Branch plan-3-gem from main a534db6. Task 7: dispatched implementer (opus), BASE a534db6
- Task 7: DONE_WITH_CONCERNS d6ea9a5, 404 tests. Markers: power 4,113 (500 MW or more, summed per plant); oil and gas 4,933; steel 600 (1 Mt/yr or more, plant-level file). One launch: resources.png shows all three layers.
- Ruling: R22 (implementer's, accepted): power units sourced from TransitionZero (CC BY-NC 4.0) are never bundled; a plant containing one is left out (none of 500 MW or more today) — C6: NC data is never committed — cost if wrong: none.
- Ruling: R23 (implementer's, accepted): 6,055 oil and gas fields passed the filters, so the cap is met by leaving out fields GEM reports producing under 0.2 million boe/yr; 653 fields with no production figure are kept — P3-9 — cost if wrong: some small fields missing. Decisions T7-1 to T7-3 are in the task-7 report and the spec table.
- Task 7: review dispatched (sonnet).
- Task 7: review Approved, no Critical/Important. Spot-checks match the raw workbooks exactly: Three Gorges 22,500 MW, Itaipu, Bruce, Taichung; Burgan, Rumaila, Safaniya; POSCO Gwangyang, Baoshan, Tata. All 4,113 power plants are confirmed free of TZ (no non-commercial units).
- Ruling: R24 two of the Minors are about accuracy, so they are fixed in one small round:
  1. GEM's recommended citation verbatim, publisher first ("Global Energy Monitor, Global Integrated Power Tracker, September 2026 release."), with "file V2" as a separate version note;
  2. multi-fuel power tooltips list each fuel's MW.

  Task 7: minor (deferred): the steel key strokes are heavier than the map's; oil/gas vs fossil power are neighbouring oranges; the licence URL form differs from the steel About sheet's (both are GEM's).
- Task 7: fix round 1/5 sent (R24).
- 2026-11-24: the owner asked for a renewables layer.
- Ruling: R25 add MarkerLayer.Renewables to Resources, from the same GIPT file:
  - fuels: wind, utility-scale solar (incl. solar thermal if GEM lists it), hydropower, geothermal, bioenergy;
  - operating units only, summed per plant, R22 still applied;
  - threshold: start at 100 MW, then raise in steps (150, 200, 300) until the count is at most 5,000. The threshold is recorded in the manifest and legend;
  - fills use the same fuel colours as the power layer;
  - shape: a new distinct glyph (a hexagon), not one used by any layer (C8);
  - the power layer stays all fuels at 500 MW or more, so a large renewable plant appears in both layers when both are on, and the legend says so.
  — The owner asked for it; the 500 MW cap hid most wind, solar, geothermal and bioenergy — cost if wrong: duplicate markers when both layers are on.
  Dispatch after the R24 fix round passes re-review (same agent, same files).
- Task 7: R24 fix c20da59 (405 tests; data.csv byte-identical; manifest asOf/command also changed). Re-review dispatched (haiku).
- Task 7: R24 re-review: citations in the manifests and legend credit and multi-fuel tooltips are ADDRESSED; data.csv byte-identical. Open: the status dot shows only the release text, not GEM's full citation.
- Ruling: R24b the status dot keeps the short "<layer> <release>" form, like every other source dot. GEM's full publisher-first citation lives in the legend credit and the manifests, which is where credit is given — R24's "status dot" was my over-reach — cost if wrong: none. Finding closed. Task 7: complete (a534db6..c20da59). Task 7: minor (deferred → folded into R25): the multi-fuel colour test can't tell largest from first-listed.
- R25 dispatched to the Task 7 implementer, BASE c20da59.
- R25: done a31acba (415 tests). The threshold stepped 100 MW → 9,047 plants, 150 → 5,379, 200 → 4,021 (4,020 drawn, 1 Réunion not placed). By fuel: wind 1,429, solar 1,271, hydro 1,265, bioenergy 41, geothermal 14. 1,058 plants are also in the Power layer. Gem-power output is byte-identical. One launch: four layers over the Gulf were unreadable, so a new `renewables` scene (Europe + water stress) was added and `resources` keeps three layers. Review dispatched (sonnet).
- R25 review: Approved. All 4,020 rows match the raw file; the steps and fuel split were recomputed and match; 0 TransitionZero; gem-power unchanged. Spot-checks: Hornsea, Walney, Gemini, Noor Abu Dhabi, Ruoqiang, Bhadla III, Makban, Drax, Three Gorges.
- Ruling: R26 fold two accuracy Minors into one small round:
  1. For a renewable plant that also has non-renewable units, the tooltip says "renewable units only (the plant also has N MW of <fuel>)" and the legend measure says other fuels are left out.
  2. Fix the stale "(V2) release" credit string in plan-3-results.md.

  Deferred, cosmetic: round legend keys for hexagon markers; hydro shares the strip symbol colour; the overlap sentence sits below the legend fold.
- R26 done a3195a9 (416 tests): an other_fuels column; Drax tooltip "renewable units only; the plant also has 96 MW of oil/gas"; legend note; doc string fixed; gem-power byte-identical.
- Ruling: R27 a plant whose renewable units total 200 MW or more stays in the renewables layer even when it has more fossil capacity (4 plants, e.g. Polaniec: 225 MW bioenergy against 1,657 MW coal) — the layer maps renewable capacity, and the tooltip names the fossil part — cost if wrong: 4 markers could suggest a green plant; owner may choose otherwise.
- R26 re-review dispatched (haiku).
- R26 re-review: all addressed, no new Critical/Important. Minor (deferred): a test attribute is on the same line as its method (formatting); OtherFuelsText needs a length guard for malformed parts. Controller re-ran the build and tests on a3195a9 (closes the raw-output evidence gap).
- Task 7 + R24–R27: complete on branch plan-3-gem (a534db6..a3195a9 plus the ledger commit). Awaiting the owner's merge decision.
- 2026-11-24: the owner chose merge locally. main fast-forwarded to 6b46770 (416 tests on main); branch plan-3-gem deleted; not pushed. Plan 3 is complete, and the workspace is deleted.
