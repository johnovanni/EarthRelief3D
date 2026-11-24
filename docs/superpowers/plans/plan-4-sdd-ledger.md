# SDD ledger — plan: C:\repos\EarthRelief3D\docs\superpowers\plans\2026-11-24-plan-4-getting-help-in-and-one-small-step.md

Spec: C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md
Branch: plan-4 (in C:\repos\EarthRelief3D, same checkout as Plan 3). Base: d3d1281 (plan commit) on main 7088d27.
Owner: "Go ahead with work on Plan 4" (2026-11-24). Local commits only; no push, no merge into main without the owner.
Rulings numbered R1, R2, ... are Plan 4's own (the spec table will call them Plan 4 R numbers).

## Pre-flight scan (2026-11-24)

| Pair / task | Produces → consumes | Finding |
|---|---|---|
| T1 → T2 | synthetic ACAPS xlsx + HDX package, FTS csv + package → feed tests | Test strings say "December 2025 edition": the synthetic package must name its newest resource `…_dec_2025.xlsx` (R1) |
| T1 → T3 | ACAPS level names, indicator wording → `ShadingScales.Access` labels, `AccessPanel.IndicatorWording` | ok |
| T1 → T4/T6 | UNHRD hubs, state.gov contact URL (built-in browser) → depots json, profile.json | Subagents may lack the built-in browser: controller does T1 Step 2 (R2) |
| T2 → T3 | `AccessOf`, `AppealsOf`, series `acaps.access` | ok |
| T3 → T4 → T5 | LayerCatalog/LayerStyle enum appends, Getting help in group | Marker/toggle layer ids not named in the plan; T10 scenes need them (R3) |
| T3 → T6 | `Show(..., PanelFocus? section)`, `PanelFocus.Help` | ok |
| T4 → T5 | `Depot`, `VisibleDepots`, hub loads, `detail` = IATA | ok |
| T5 ↔ T6 | both add AppSettings fields and flyout groups | sequential; ok |
| T6 → T7 | charities.json schema (`LegalCheck`, `ProgrammeShare`) → tool report | ok |
| T6 → T9 | `PanelSection.ExplainerId` ("why-these") → explainer UI | ok |
| T7 → T8 | tool run (Pub 78, 990) | T8 finds EINs by name in the Pub 78 file directly; fine |
| T1 | self | ok (moves the tests' workbook writer into the DataTool) |
| T2 | self | ok |
| T3 | self | ok (legend date via SeriesDate → Release DateText) |
| T4 | self | ok |
| T5 | self | ok |
| T6 | self | ok |
| T7 | self | ok (expiry example checks: 2027-01-10 ≤ 2026-11-24 + 60 days) |
| T8 | self | data task, no tests of its own; reviewer checks the data against P4-9 |
| T9 | self | ok |
| T10 | self | ok (3–4 launches within the budget) |

Ruling R1: the synthetic ACAPS HDX package names its newest XLSX resource `acaps-humanitarian-access-dataset_dec_2025.xlsx` — the T2/T3 tests expect "December 2025 edition" — cost if wrong: test strings change.
Ruling R2: the controller does Task 1 Step 2 (unhrd.org and state.gov in the built-in browser) and hands the result to the Task 1 implementer as a file — subagents may not have the browser tools — cost if wrong: none.
Ruling R3: layer ids `access` (shading), `un-depots`, `airports`, `ports`, `hub-lines` — the plan names only `access`; Task 10's scenes need the rest — cost if wrong: a rename.

## Progress
- Task 1: dispatched (base d3d1281), implementer agent acd167de169069a21 (sonnet). Controller did Step 2 (task-1-browser-findings.md).
- Task 1: implementer DONE_WITH_CONCERNS, commit 979a7be (421 tests). Concerns handled by rulings R4, R5; others noted (WPI portal still 503; IRS Range 206 ok; logistics-cluster.json 19 rows; num_resources not updated in the trimmed package, harmless).
Ruling R4: the ACAPS feed picks the XLSX resource with the latest edition parsed from its file name (`_<mon>_<yyyy>.xlsx`), not the latest `last_modified`; a resource whose name does not parse is skipped (logged), and none parsing is FeedFormatException — on HDX the July 2025 file's last_modified is 3 s later than December 2025's, so P4-3's rule would pick the older edition — cost if wrong: none (file names are HDX's own). Task 2's test `.NewestXlsxResourceIsUsed` becomes `.NewestEditionIsUsed` (two resources, the older edition with the later last_modified: the newer edition wins).
Ruling R5: ACAPS's ACCESS score 0 is a figure ("No constraints"), not "no figure": the Access scale has six classes 0–5 (ColorBrewer YlOrBr 6: #FFFFD4 #FEE391 #FEC44F #FE9929 #D95F0E #993404), class labels ACAPS's level names (No constraints, Low, Moderate, High, Very high, Extreme, as Task 1 recorded them); Task 3's `.ZeroIsNotShaded` becomes `.ZeroIsNoConstraints` (shaded the lightest class, labelled "0 of 5: No constraints") and the legend says "ACAPS score 0–5" — P4-14 assumed 0 had no name; Review Focus 1 forbids showing a real figure as missing — cost if wrong: one class.
- Task 1: review Approved (sonnet reviewer). Minors parked for the final fix wave: (m1) data-sources §13 says the FTS sample is "bytes unchanged" but the script re-exports it; (m2) fetch-samples.ps1 comment claims a Senate shape check that does not exist; (m3) the OurAirports excerpt's NG/CI rows are not documented; (m4) synthetic AFG007 title mirrors a real crisis name; (m5) compact one-line package samples.
Ruling R6: senators' office phones and contact forms in the Senate sample are public-office data (US government work), not personal data — the "no personal data" rule is about app users and private people — cost if wrong: trim the sample.
Task 1: complete (commits d3d1281..979a7be, review clean)
- Task 2: dispatched (base 274d63f), implementer a8b91f8e52bd3e576 (sonnet).
- Task 2: implementer DONE, commit ae05925 (442 tests).
Ruling R7: ACAPS sample count is 12 placed assessments, not the brief's 11 — Task 1's synthetic sheet has 13 country-level rows (XXK unplaced) — cost if wrong: none.
Ruling R8: an ACAPS indicator cell that is not 0–3 (ACAPS marks an information gap "X") is left out of `Indicators`; Task 3's panel lists all nine by number and shows a missing one as "No score (information gap)" — no figure must never look like a low score (Review Focus 1) — cost if wrong: wording.
- Task 2: review Needs fixes (1 Important, plan-mandated: edition regex misses ACAPS's real names `-july_2025`, `_dec-_2024`).
Ruling R9: the edition parser accepts ACAPS's real variants — a separator `-` or `_`, a month as its full English name or a 3-letter prefix (case-insensitive), an optional `-` before `_`, then a 4-digit year, before `.xlsx` — and an XLSX resource whose name still does not parse is logged at Warn (not Info) so the maintainer sees a missed edition; tests for `-july_2026`, `_dec-_2024`, `_dec_2025` — the plan's example was narrower than HDX's real names; a silently stale feed breaks "every fact shows its date" — cost if wrong: none.
Task 2: minor (deferred): (m6) FTS early-January fallback only when no current-year rows exist, so a few new-year plans hide last year's (FtsFundingFeed.cs:552); (m7) FTS citation Dataset/Version holds only the year; (m8) AccessAssessment.GetHashCode omits lists; (m9) an ACAPS country row with an unusable score is only logged, not shown as unplaced.
- Task 2: fix round 1/5 (1 addressed, 0 open — edition regex accepts ACAPS's real names (R9); commits ae05925..ec4d42b). Re-review (haiku): clean.
Task 2: minor (deferred): (m10) task-2-report line 29 stale about R4 strict names; (m11) month list lacks "sept".
Task 2: complete (commits 274d63f..ec4d42b, review clean)
- Plan text commit feb563a (R8). Task 3: dispatched (base feb563a).
  Task 3 implementer: a47011d7d342a70db (sonnet).
- Task 3: implementer DONE_WITH_CONCERNS, commit e3d5af6 (464 tests). Noted: no clean RED run (code written before tests ran); indicator 8's long label wraps in the 130 px column (Task 10 to look); pillars/info gap not shown (not asked).
- Task 3: review Approved (sonnet).
Task 3: minor (deferred): (m12) AccessPanel.EditionOf unused, logic duplicated at CountryPanelViewModel.cs:685; (m13) FundingSections shows an older year's appeals without "No appeal in FTS for <newest year>" when the snapshot holds a newer year (latent; feed loads one year); (m14) stray blank lines (LayerStripViewModel ~870, CountryPanelViewModel ~650); (m15) LogisticsClusterLinks.cs:1088 convoluted add-condition; (m16) Access tooltip casts (int) instead of Math.Round.
Task 3: complete (commits feb563a..e3d5af6, review clean)
- Task 4: dispatched (base e3d5af6).
  Task 4 implementer: a5eee9ceb138ddfd1 (sonnet). Depot positions via one Nominatim lookup each (P4 ruling in the plan's Task 4 notes).
- Task 4: implementer DONE, commit 463c549 (486 tests). Airports 3,254 placed (+16 not placed), ports 546 (+4). Depots: Accra, Dubai, Panama at named places (airport / industrial city / Panama Pacifico), Brindisi street address, KL street. No clean RED for the DataTool extractor.
- Task 4: review Approved (sonnet).
Task 4: minor (deferred): (m17) HubMarkers.Credit/Status and AppServices.ApplyHubPoints near-copies of ResourceMarkers/ApplyBundledPoints; (m18) HubExtractor ports notes hardcode "2026-11-24 ... portal under maintenance"; (m19) LayerCatalog says "the five UN Humanitarian Response Depots" (hardcoded count); (m20) curated review writes "-0.16827° E" for west longitudes; (m21) relaxed BundledPoints.Citation has no per-layer guard keeping GEM on Release; (m22) brief's Port Sudan example says large, real data medium.
Task 4: complete (commits e3d5af6..463c549, review clean)
- Task 5: dispatched (base 463c549).
  Task 5 implementer: a7481c0b0d61afc27 (sonnet).
- Task 5: implementer DONE_WITH_CONCERNS, commit 29c9196 (507 tests). Plan 3 settings files keep their marker list (hub lines off); fresh/flags-only files get them on.
Ruling R10: the Getting help in group may start open on a fresh install because the hub lines are on by default (R20's rule) — it shows a new user the group exists; harmless — cost if wrong: one default.
Note for Task 10: scenes must list "hub-lines" (and showDrafts for depot arcs) since SetLayers replaces the marker set; Fiji's nearest WPI large/medium port is Auckland (no Fijian large/medium port in WPI).
- Task 5: review Approved (sonnet).
Task 5: minor (deferred): (m23) HubLabelsLayer.Render recomputes specs + ArcPoints every frame (cache until Changed); (m24) distance labels may be dropped silently on overlap; (m25) no test for a zero-length or near-antipodal arc; ribbon side fallback (1,0,0) not tangent; (m26) HubMarkers.SourceName vs LayerCatalog.SourceName overlap, depot licence text hardcoded; (m27) stray `using AstroCore.Earth;` ordering in CountryPanelViewModel.
Task 5: complete (commits 463c549..29c9196, review clean)
- Task 6: dispatched (base 29c9196).
  Task 6 implementer: aae86f77a29ad5644 (sonnet).
- Task 6: implementer DONE_WITH_CONCERNS, commit 32a96c8 (548 tests). pooled-funds 21 drafts; profile 4 draft contacts; senators.json 100 (asOf 2026-11-18); charities []. Namespace EarthRelief3D.Core.Help (brief said EarthRelief3D.Help) and PanelLink moved to Core: accepted. Country change applies on restart (only US exists): accepted. CERF built-in fallback when no visible cerf row (P4-11 text): accepted.
Ruling R11: each charity's "Working here: <evidence title> (checked <date>)" line links its evidence page (a PanelLink per CountryWork) — spec §9.1 shows evidence with the entry and every fact must show its source; the brief's link list was incomplete — cost if wrong: one link per line.
- Task 6: review Needs fixes (2 Important: R11 evidence link missing; fund/CERF/contact lines show no date, source line claims "each line gives its own date").
Task 6: minor (deferred): (m28) leap-day review expiry untested (AddMonths clamps to 28 Feb: hidden a day early); (m29) built-in CERF is "Approved" with no date shown; (m30) SenatorsEntry says "Choose your state" for a set-but-unknown code; (m31) HelpTables.Load reads every country's profile, one bad future profile breaks all.
- Task 6: fix round 1/5 (2 addressed, 0 open — R11 evidence link; dates on fund/CERF/contact lines; commits 32a96c8..8cf1f78). Re-review (haiku): clean.
Task 6: minor (deferred): (m32) HelpPanelTests.cs:162 `FundCerfAndContactLinesShowTheirDate` written on one line (newlines lost) — reformat; its contact loop could pass vacuously if the fixture had no contacts; (m33) "Checked <date>" vs "(checked <date>)" capitalisation.
Task 6: complete (commits 29c9196..8cf1f78, review clean)
- Task 7: dispatched (base 8cf1f78).
  Task 7 implementer: a040aca656926c6a6 (sonnet).
- Task 7: implementer DONE_WITH_CONCERNS, commit dc4a4eb (563 tests); real run: "0 charities"; six real charities read correctly from a scratch folder; Zip64/Deflate64 read from live IRS zips (not unit-tested). Uses a new VettingHttp (FeedHttp can't send Range and caps 50 MB; GlobeCore unchanged): accepted. One flaky full-run failure of AppStartupTests.CachedIpcAreasLoad... (passed on rerun): note for the final review.
Ruling R12: subagent commits keep the trailer of the model that wrote them (dc4a4eb says Claude Sonnet 5.5) — truthful attribution; no history rewrite — cost if wrong: none.
Ruling R13: the programme share's tax year is the return's own IRS `TaxYr` element (fallback when absent: the calendar year in which the tax period begins, the IRS convention), and the report also prints the period end — the implementer used the year the period ends (2025 for the Red Cross return ending 2025-06-30 whose TaxYr is 2024); the app must say what the filing says — cost if wrong: one year in a label.
- Task 7: review Needs fixes (1 Important: R13 tax year from TaxPeriodEndDt; test pins 2025).
Ruling R14: the reviewer's Minor 1 (LinkChecker follows redirects, so a charity link redirecting to charitynavigator.org/give.org would be requested; trailing-dot host not matched) is promoted to Important — it breaks the Global Constraint "no script or agent fetches charitynavigator.org or give.org pages" (P4-8) — fix: follow redirects manually (max 10), refusing evaluator hosts at each hop (reported as "not checked by the tool (evaluator's terms)"), and normalise a trailing dot — cost if wrong: a few lines.
Task 7: minor (deferred): (m34) --cache not guarded against data/; (m35) "no Part IX figures" note also used for zero/negative totals, programme > total not flagged; (m36) rare zip edges (65,535 entries w/o zip64, encrypted flag, no download size cap, zip64/Deflate64 untested); (m37) RunAsync doesn't catch JsonException from a malformed charities.json; (m38) a latest 990-EZ hides an earlier 990's share; only two index years consulted.
- Task 7: fix round 1/5 (2 addressed, 0 open — R13 TaxYr; R14 no redirects to evaluators; commits dc4a4eb..f71eddd). Re-review (haiku): clean.
Task 7: minor (deferred): (m39) LinkChecker exception doc comment misplaced above MaxRedirects; (m40) Form990Reader ProgrammeShare summary still says tax year = period-end year; (m41) https→http redirects allowed.
Task 7: complete (commits 8cf1f78..f71eddd, review clean)
Ruling R15: for Task 8, a 3W country "has a current crisis" when its 3W rows have `has_hrp` or `in_gho` true (it is in OCHA's Global Humanitarian Overview) — the app decides crises live from feeds, but drafting needs a fixed, source-defined list; GHO inclusion is OCHA's own threshold — cost if wrong: a charity listed for a country with no crisis stays hidden there anyway (the step section only shows for crisis/appeal countries).
- Task 8: dispatched (base f71eddd), opus (research accuracy).
  Task 8 implementer: aba9ea97f8e661b62 (opus).
- Task 8: implementer DONE_WITH_CONCERNS, commit caa8945 (568 tests). 9 charities, 138 country listings (threshold rose to 13 countries → 15 orgs; dropped DRC, ACTED (no US affiliate statement/donate page), World Vision (Pub 78 code GROUP), FAO, Première Urgence, Solidarités (no US public charity); NRC Haiti left out). Name-merge sensitivity: other readings swap International Medical Corps for USA for UNHCR — OWNER ITEM. UNICEF USA country pages bot-checked → unicef.org pages used. 31 Save the Children/UNICEF USA links blocked to the vetting tool's script (opened fine earlier).
Ruling R16: `Charity` gains an optional `Note` (init property, read from the JSON's "note") shown in docs/help-review.md (and not in the app panel) — NRC USA's "EIN matched by name in Publication 78 only" is otherwise silently dropped and the owner must see it when approving — cost if wrong: one field.
- Task 8: review Approved (sonnet, with spot checks: 3W counts/threshold reproduced, 9 EINs in Pub 78 PC, IOM/NRC/WFP pages opened). Important items are owner disclosures.
Ruling R17: the R16 `Note` carries the reviewer's disclosure items so the owner sees them when approving: Save the Children US — "its US site does not state membership of Save the Children International in so many words; owner to confirm"; charities whose country evidence is on the international organisation's site (USA for IOM: iom.int; USA for UNFPA: unfpa.org; UNICEF USA: unicef.org for 5 countries; NRC USA: nrc.no) — "country evidence is on <domain>, the international organisation's site, which <US site> links to" (verify the link exists; if a US site does not link to it, say so in the note instead); NRC USA's EIN note stays — the review found them real but judgement calls for the owner — cost if wrong: wording.
Task 8: minor (deferred): (m42) Save the Children SSD listing: the reviewer could not attribute 3W rows to Save the Children International in SSD — implementer re-checks in the R16/R17 round; (m43) UNICEF COL left out because the Spanish name was not merged.
- Task 8: fix round 1/5 (3 addressed, 0 open — R16 Note field; R17 disclosures; SSD: controller verified charities.json — save-the-children-us lists no SSD, so nothing to remove, finding recorded in the report; commits caa8945..b199225). Re-review (haiku) marked SSD open only for lack of a diff line; controller check closes it.
Task 8: minor (deferred): (m44) Save the Children US note cites "29 national members" without a source; (m45) NRC USA note answers the US-site link question only by implication; UNICEF bot-check note has no date.
Task 8: complete (commits f71eddd..b199225, review clean)
OWNER ITEM: P4-9 merge reading — alternative name merges would swap USA for UNHCR in for International Medical Corps (Task 8 report concern 1).
- Task 9: dispatched (base b199225), opus.
  Task 9 implementer: a36110f399aa3ec8b (opus).
- Task 9: implementer DONE_WITH_CONCERNS, commit d05b85e (578 tests). 8 draft explainers (213–292 words). ipcinfo.org Cloudflare-blocked → food cites FEWS NET and FAO. A UN PDF triggered a save dialog in the owner's browser pane (not saved) — TELL OWNER. Card at a fixed 404 px from the right.
Ruling R18: keep the three evaluator METHOD pages (Charity Navigator's ratings method, give.org's BBB standards and Overhead Myth) opened once in the built-in browser as why-these sources — my Task 9 dispatch allowed method pages, which conflicts with P4-8's strict wording; P4-8's purpose is no automated fetching of ratings/profiles (CN terms bar bots and scraping); one reader-style view of a method page to cite a threshold the spec itself states (Q1) is not scraping — cost if wrong: drop three citations (IRS and FTC sources remain). OWNER ITEM: say if P4-8 should be read strictly.
Ruling R19: Task 10 adds a snapshot-scene field `explainer` (an explainer id) that opens the card for that picture (drafts shown with showDrafts), so the card is checked visually — no scene could open it — cost if wrong: one field.
- Task 9: review Approved (sonnet; spot-checked UCDP, FEWS NET, ICRC, WHO DON pages; UNHCR 403 — displacement text unchecked by the reviewer).
Task 9: minor (deferred): (m46) quote detection only measures text inside quotation marks; (m47) explainer prose hardcodes app rules (25 deaths, Phase 3, 90 days, 12 months, 3 stars, 20 standards, "camps so far are in North Korea") not tied to constants; (m48) food text "make each analysis together" not supported by the cited sources — trim or cite (ACCURACY: fix in the final wave); (m49) authority allow-list accepts any un.org/ohchr.org page.
Task 9: complete (commits b199225..d05b85e, review clean)
- Task 10: dispatched (base d05b85e).
  Task 10 implementer: a4c032ecc21f3fbc8 (sonnet).
- Task 10: implementer DONE_WITH_CONCERNS, commit b6e6f5e (582 tests). Launches 3 of 4; 14 scenes; repeat run identical. Tuned: label column 130→150 px; explainer card drawn over the legend. Charity entry layout never seen (all charities hidden: no ratings, even with drafts shown).
Controller look at sudan-help.png: airports layer (≈3,250 white hexagon outlines) clutters the whole globe and hides the port arc; "UNHRD Dubai · 2,842 km" label runs under the panel. For the final review.
- Task 10: review Needs fixes (1 Important: spec's "### Out of scope (for now)" heading deleted, its bullets orphaned under the rulings table).
Task 10: minor (deferred): (m50) results doc doesn't say the charity entry layout is unverified visually; (m51) access picture description "brown top classes" loose (Somalia High, South Sudan mid); (m52) explainer card semi-transparent — legend text shows through; doc says it "covers"; (m53) duplicated depots draft/position sentence in the legend; (m54) non-string `explainer` silently ignored (as panelFocus); (m55) README status line over-long.
- Task 10: fix round 1/5 (1 addressed, 0 open — Out of scope heading restored; commits b6e6f5e..5daa3af). Re-review: controller-verified (spec diff vs d05b85e = 44 insertions, 0 deletions; heading at line 252 after the Plan 4 table).
Task 10: complete (commits d05b85e..5daa3af, review clean)
- All tasks complete. Final whole-branch review next (merge base 7088d27 = main).
  Final review: dispatched (opus) agent ade8fd724910e47b6.
- Final review (opus): With fixes. 6 Important: (F1) panel jumps to top on same-country rebuild (state/unit change, feeds, hub store); (F2) arcs/labels lost under markers, airport clutter; (F3) food explainer accuracy (m48 + IPC famine thresholds: households, "at least"); (F4) explainer card see-through (m52); (F5) personal data in tests/Samples/irs990_sample.xml (names, compensation, preparer PTIN, phones); (F6) depot positions from OSM (ODbL share-alike) committed against C6 with no ruling. Minors to fix: m11 "sept"; m44 "29 national members"; m50 doc; hub row wording + hub country; draft label on depot row/arc label; card closes on country change and margin/MinWidth; access explainer "No constraints" wording; results-doc errors (four depots → five; m1–m49 → m55); m32 test reformat; m37 catch JSON in the tool; owner item: Plan 3 settings keep hub lines off.
Ruling R20: the Airports marker layer draws only large airports with scheduled service (≈1,153); the nearest-airport search and panel row keep large and medium (P4-5 unchanged for the search); the legend says so — ~3,250 hexagons smother the globe and hide the arcs (final review F2) — cost if wrong: medium airports not drawn (still named in the panel/arc).
Ruling R21: UN depot positions come from the airport each depot's own address names or sits beside, taken from the bundled OurAirports data (The Unlicense), not OpenStreetMap: Accra → Kotoka International (address names it); Dubai → Al Maktoum International / DWC (address: "Nearest Landmark - DWC"); Panama → Panamá Pacífico International (address: Panama Pacifico, Howard); Kuala Lumpur → Sultan Abdul Aziz Shah (Subang) (address: Jalan TUDM, Shah Alam — the air-force road by Subang); Brindisi → Brindisi Airport (the hub's address on the former SS16 by the airport) — each row's verification says the marker is at that airport, the depot is next to it per its address, and the position is approximate — C6 forbids committing share-alike data and no ruling allowed it (F6) — cost if wrong: a marker a few km from the depot; distances rounded anyway.
Ruling R22: the built-in CERF line (shown when no approved cerf row exists, with the P4-11 text) is recorded in the spec's Plan 4 rulings table for the owner — accepted in the ledger (m29) but not shown to the owner — cost if wrong: none.
- Final fix wave: dispatched (opus) agent a2586deb3c847e9ff, findings file final-fix-findings.md, fix base 5daa3af.
- Final fix wave: DONE_WITH_CONCERNS, commits e7e30c4, a0ea4a9, f7d8bb2 (592 tests). Launch 4 of 4 into artifacts\snapshot-plan4-fix. Port arc hidden under the depot arc in sudan-help (no launch left; described in results). F3 household wording sourced from WFP "Understanding famine" (FEWS NET words it per population); wfp.org allowed for food. Dubai marker at Al Maktoum airport ≈11 km from the depot (approximate, said).
!! The untrimmed irs990_sample.xml (private people's names, compensation, preparer PTIN, phones) remains in earlier local plan-4 commits (dc4a4eb..): history must not be pushed as is — squash-merge or rewrite before any push.
- Final fix wave re-review (sonnet): all addressed, no new Critical/Important. Residual minors (not fixed, disclosed in plan-4-results.md): port arc lies under the depot arc in sudan-help; Dubai label under the panel; forced labels may overlap in crowded scenes.
- Controller verification on f7d8bb2: Release build 0 warnings; 592 passed, 0 failed.
- Plan 4 complete on branch plan-4 (not merged; merging into main needs the owner). History note: squash-merge so the untrimmed IRS sample in earlier commits never reaches main or GitHub.
