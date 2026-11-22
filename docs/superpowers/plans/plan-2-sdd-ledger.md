# SDD ledger — plan: C:\repos\EarthRelief3D\docs\superpowers\plans\2026-11-22-plan-2-foundation.md
Spec: C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md (reachable). Owner chose subagent-driven 2026-11-22; local commits OK; no push.
Setup by controller: git init -b main in C:\repos\EarthRelief3D with EarthWatch3D's .gitignore (ignores .superpowers/), nothing committed yet.

## Pre-flight scan
| Pair / task | Produces vs consumes | Finding |
|---|---|---|
| T1 self | creates repo/projects; first commit = existing docs | controller already ran git init → R1 |
| T2→T3–T6 | CountryIndex/Country/StatusDotsViewModel in GlobeCore branch country-index; EarthRelief3D pins branch head | consistent; Step 5 (PR) deferred to finish → R2 |
| T3→T4/T5/T6 | Crisis records, CrisisStore, SourceCitation, SeverityBands, Trend | consistent |
| T4/T5 → T6 | feeds Name strings "UCDP","IPC","WHO","UNHCR" used by status dots | consistent |
| T5 self | data/*.json copied to App output but files live under src? plan says data/ at repo root | → R3 |
| T6→T7 | snapshot scenes need panel-open option (sudan-panel) | T7 adds a launch option for opening a panel → R4 |
| T7 self | PngDiff copy vs path reference left to implementer | fine (recorded by implementer) |
| Global | ≤3 app launches in the plan | T7 uses 2; T6 none → fine |

Rulings:
- R1: Controller ran `git init -b main` + .gitignore before Task 1 so the SDD workspace could exist; Task 1 does everything else — cost: none.
- R2: Task 2 Step 5 (push GlobeCore branch, PR, merge, re-pin) runs at finish with the owner, not mid-plan — the plan says so — cost: none.
- R3: `data/*.json` live at repo root `data/` and are linked into Core's output as content (`<None Include="..\..\data\**" CopyToOutputDirectory=...>`) and also into App/tests outputs — cost: none.
- R4: Task 7 adds `--open-panel <ISO3>` per scene ("panel" field in scenes.json) to support sudan-panel/no-crisis-panel — the plan's scenes need it — cost: one more option.

## Progress
- Task 1: dispatched implementer (sonnet), BASE = empty repo
- Task 1: DONE af83465, 1b62d1a, 7f77a9f; build clean. Notes for later tasks (from data-sources.md): UCDP monthly+quarterly overlap → dedupe by id; UCDP dates "yyyy-MM-dd HH:mm:ss.fff"; XXX placeholder names; IPC Country = ISO3, some blank current periods, no UKR/MMR rows; UNHCR mixed string/number with "-"; UNHCR sample is 2026.
- Task 1: review dispatched (sonnet; sample data excluded from diff)
- Task 2: dispatched implementer (sonnet) in GlobeCore branch country-index from 62d9a5b; ruling R5: implementer does not touch EarthRelief3D — controller pins the submodule after Task 1's review closes (avoids two writers in one repo) — cost: one controller commit
- Task 1: complete (af83465..7f77a9f, review clean; trailer checked). Task 1: minor (deferred): data-sources.md says UNHCR total is 0 (it is []); UCDP sample re-quoted by Export-Csv (documented); odd sln folder GUID.
- Ruling R6: Task 3 runs in parallel with Task 2 — different repos (EarthRelief3D vs C:\repos\GlobeCore), Task 3 needs no Task 2 types — cost: none unless Task 3 edits the submodule (forbidden in its dispatch).
- Task 3: dispatched implementer (sonnet), BASE 7f77a9f
- Task 2: DONE 22d6ba4 (GlobeCore country-index), 319 tests. Ruling R7: accept the small built-in alias list in LoadNaturalEarth (official renamings Natural Earth lacks: Türkiye, Czechia, Cabo Verde, Timor-Leste, Burma, Ivory Coast, Swaziland) — generic Earth data, apps add more via WithAliases — cost: list maintenance in GlobeCore. Fiji test points moved onto land (brief's points were sea). Review dispatched (sonnet).
- Task 3: DONE 4c6c0d6, 20 tests. Ruling R8: conflict bands 1–4 (25 / 1,000 / 10,000 / 50,000 boundaries), band 5 unused for conflict — the plan's four boundaries give four bands; bands only size markers and docs/severity-bands.md says so — cost: none. CARRY to Task 4: Trend.FromMonths needs all 6 months present → the UCDP feed must store 0 for months with no deaths. Review dispatched (sonnet).
- Task 2: complete (62d9a5b..22d6ba4 GlobeCore country-index, review clean). Task 2: minor (deferred): AUS ISO3 shared by 3 NE units (ByIso3 returns Australia; document+test); unknown alias ISO3 silently ignored; hyphen/space variants of apostrophe names; Resolve("Congo")=COG — apps must alias UCDP 'DR Congo (Zaire)', 'Myanmar (Burma)' etc.; dispatcher branch untested. CARRY to Task 4: UCDP country strings need aliases → Task 4 creates data/country-aliases.json (Task 5 extends).
- Task 3: review Needs fixes — I1 CrisisCache misses NotSupportedException (missing $type → crash every launch). Fix round 1 sent (+ minors: severity doc calendar-year wording; Trend sums same-month keys). Task 3: minor (deferred): BandsMatchDoc weak; no base-typed list change test; transient IOException deletes cache (as EarthWatch3D).
- Controller: pinned external/GlobeCore to 22d6ba4 (country-index) in bd2e0b6; Release build 0 warnings; 23 tests pass.
- Task 3: fix round 1/5 (1 Important + 2 folded minors addressed; 4c6c0d6..f14cc3e)
- Task 3: complete (7f77a9f..f14cc3e, review clean)
- Task 4: dispatched implementer (sonnet), BASE bd2e0b6
- Task 4: DONE e68e9e7, 39 tests. Notes: UCDP file cache in memory (restart re-downloads; acceptable — crisis cache shows data meanwhile); format change = FeedFormatException (scheduler marks failure). Review dispatched (sonnet).
- Task 4: review Needs fixes — I1 threshold per (conflict,country) not per conflict; I2 same-named UCDP files never refreshed. Ruling R9: threshold per conflict over all its events, one crisis per country with events (brief + C11). Fix round 1 sent (+ folded: zero-events = format changed; latest complete month from file name; conflict Began=null and 'since <month>' wording; data-sources attribution). CARRY to Task 6: IPC AsOf (period end) can be in the future — panel/legend must show 'analysis period <from>–<to>' rather than a future 'as of'; Phase3PlusShare is a fraction. Task 4: minor (deferred): IPC 'else 3' branch untested; href regex double quotes only.
- Task 4: fix round 1/5 (2 Important + 4 folded addressed; e68e9e7..23c515d)
- Task 4: complete (bd2e0b6..23c515d, review clean). Task 4: minor (deferred): SeverityBands param name deathsLast12Months stale; 'older monthlies stay cached' untested; spec §6 says 'last 12 months' (window ~9–12) — note in Plan 2 rulings record at finish.
- Task 5: dispatched implementer (sonnet), BASE 23c515d
- Task 5: DONE bb681f2, 61 tests. Review dispatched (sonnet) with named risk: UNHCR as-of = 31 Dec of data year capped at clock may misstate mid-year figures.
- Task 5: review Needs fixes — I1 UNHCR AsOf capped to clock misstates mid-year figures; I2 ';' separator missing. Ruling R10: UNHCR AsOf = 30 Jun of data year ('Mid-year <y> figures') unless the data year is earlier and clock ≥ 1 Jul of the following year → 31 Dec ('End-year <y> figures'); never the clock — UNHCR publishes mid-year (≈Nov) and end-year (≈mid-June next year) releases; API has no date field — cost: label may lag a release by weeks. Fix round 1 sent (+ distinct-neighbour resolution tests).
- Task 5: fix round 1/5 (2 Important + 1 folded addressed; bb681f2..a4810f6)
- Task 5: complete (23c515d..a4810f6, review clean). Task 5: minor (deferred): current-year UNHCR AsOf 30 Jun can be after retrieval before July (API then usually has no current-year items).
- Task 6: dispatched implementer (opus — UI integration), BASE a4810f6
- Task 6: DONE 1653ee6, 96 tests, 0 warnings; IPC colours from IPC docs via search text + FEWS NET page (PDF not opened). Notes for T7: WHO sample has no Sudan → sudan-panel shows 3 sections; tuning likely. Review dispatched (opus).
- Task 6: review Needs fixes (opus) — I1 'No crisis recorded' when nothing checked / WHO-checked-empty shown as 'no data'; I2 country hover lacks source+date; I3 MessageBox Warning/Error play sounds. Ruling R11: accept food source line 'IPC analysis period …(analysed …)' and legend as-of = newest Date of analysis (avoids future as-of). Ruling R12: CrisisSnapshot.CheckedUtc per feed (set on successful runs incl. empty), cache schema bump; 'Not yet checked' vs 'none recorded · checked <date>' wording — cost: Core model change in a UI task. Fix round 1 sent (+ folded: UNHCR future as-of hidden, LayerStyle URL, fixture Version, Phase 5 wording, Dataset label, brush caching). Task 6: minor (deferred): dispatcher paths untested (dispatcher null); no app-level Fiji test; CrisisCacheWriter file split.
- Task 6: fix round 1/5 (3 Important + 5 folded addressed; 1 folded minor open: LayerStrip.xaml:102 tooltip 'Phase 5 Famine' → carried into Task 7; 1653ee6..2311575)
- Task 6: complete (a4810f6..2311575, review clean on Important). Task 6: minor (deferred): DataDate vs inline UNHCR date formatting duplicated; MarkChecked triggers redraw+cache save every run.
- Task 7: dispatched implementer (sonnet), BASE 2311575
- Task 7: DONE 1cee271; 133 tests; PngDiff 5/5 identical; 3 launches; controller viewed africa + sudan-panel (correct). Ruling R13: Task 7's task review is folded into the whole-branch final review (opus) — cost: one review seat fewer. Final review dispatched.
- Final review (opus): With fixes. I1 IPC projection-only rows shown as "none recorded"; I2 WHO "Disease- Place" separator; I3 UCDP unresolved names silently dropped (+ Gaza/Israel risk); I4 Crimea picks Russia (NE default worldview) vs Q3; I5 food shading "highest phase with any people" misleads (plan-level).
- Ruling R14: UCDP events are assigned to the country containing their coordinates (CountryIndex.Find, units applied), falling back to UCDP's country name only when the point is at sea/unresolved; unresolvable events become UnplacedReports — geography is what the map shows; fixes Gaza and GW-name gaps — cost: border-adjacent events may land in the neighbour's panel.
- Ruling R15: Crimea shown as part of Ukraine per owner's Q3 (UN humanitarian units; UN GA res. 68/262) — implemented in GlobeCore country-index branch via a Natural Earth POV file only if its diff from the default is limited to UN-consistent changes, else a documented polygon fix-up — cost: an asset/code change in GlobeCore.
- Ruling R16: I5 smallest fix now (show the count of people in each phase incl. Phase 5 in panel and tooltip); the shading rule itself goes to the owner as a question at finish (recommendation: IPC's 20% area-classification rule) — cost: none until decided.
- Deferred-minor triage (fix now): data-sources.md corrections (projections, UNHCR total []); CrisisCache must not delete on IOException/UnauthorizedAccessException; rename deathsLast12Months; severity-bands/legend/README wording per (conflict,country) band; WHO order guard; offline-run log kept; WHO 90-day & IPC stale re-judged against the clock at display; Plan 2 rulings table in the spec. Rest deferred to Plan 3+/GlobeCore PR.
- Final fix wave dispatched (opus).
- Final fix wave: DONE GlobeCore 68f9dc5 (Crimea fix-up via NE admin-1 UA-43/UA-40; Perekop line removed — affects EarthWatch3D's borders when it takes this GlobeCore: note in the GlobeCore PR); EarthRelief3D c6d4af1, de3e186; 163 tests. Ruling R14b (replaces R14): UCDP's country name decides; coordinates override only for listed territories UCDP doesn't treat as countries (data/ucdp-territories.json: PSE) or unresolvable names; else UnplacedReport — R14 put 41/4,494 border events (Sumy/Kharkiv → Russia, Goma → Rwanda) in neighbours on 1:50m outlines — cost: a territory list to maintain. Sent to fixer.
- R14b: DONE 82bb4e3; 167 + 322 tests; 0/4,494 sample events differ from UCDP's country; snapshots re-baselined. Scoped re-review of the fix wave dispatched (sonnet).
- Final fix wave re-review: all findings addressed, no new Critical/Important. Minor fixed by controller: plan-2-results.md now leads with the final counts (167/322) — commit after 82bb4e3. Minor deferred: scene layers re-judge WHO 90-day window only on rebuild; PSE edge events within ~3.5–6.2 km may stay ISR (1:50m polygon).
- Plan 2 complete pending owner: GlobeCore country-index PR (Task 2 Step 5; note Perekop line removal affects EarthWatch3D) and the food-shading rule question (R16).
