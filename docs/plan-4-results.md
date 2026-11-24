# Plan 4 (getting help in, and your one small step) results

Built 2026-11-24 on branch `plan-4` (local commits only; not pushed, not merged), then corrected in one fix wave after
the final whole-branch review (see "Final fix wave" below). Plan 4 added: the Getting help in
group (ACAPS Access shading, UN depot, airport and port markers, hub arcs with distance labels), the "Getting help in"
and funding panel sections (FTS), "Your one small step" (pooled fund or CERF, vetted charities, contacts), the Settings
groups Distances, Your country and Your state, the vetting tool (`src/EarthRelief3D.Vetting`), nine draft charities and
eight draft explainers with an in-app card. The rulings are in the spec, "Rulings made while building Plan 4".
Every curated Plan 4 row (depots, pooled funds, contacts, charities, explainers) is a draft, so a scene needs
`"showDrafts": true` to show it.

## Tests

`dotnet build EarthRelief3D.sln -c Release`: 0 warnings, 0 errors. `dotnet test EarthRelief3D.sln -c Release`:
**592 passed, 0 failed, 0 skipped** after the fix wave (582 at the end of Task 10; 421 after Task 1; 578 after Task 9; Task 10 added 4: the scene field `explainer`
opens the card and is validated like `panelFocus` (two bad-file cases and one good case), and the committed file holds
the four Plan 4 scenes). One flaky failure of `AppStartupTests.CachedIpcAreasLoadOffTheUiThreadAndReachTheMap` was seen
in one full run in Task 7 and once in this task's first full run (it passed on the next run); it is a timing test of
the app's start-up and is not related to Plan 4. GlobeCore is unchanged.

Test-first note: the `explainer` code and its tests were written together, so there was no clean RED run for that field;
the one failing test seen first was the committed-scenes test (the file had not yet been edited), which then went green.

## Scenes

Fourteen scenes, all at 2026-11-22T12:00:00Z: the ten earlier ones unchanged, and four new:

| Scene | What it sets |
|---|---|
| `access` | Access shading only; look at 15,30 |
| `sudan-access` | Access shading; panel SDN opened at "Getting help in" (`panelFocus` "access") |
| `sudan-help` | Layers access, un-depots, airports, ports, hub-lines; look at 15,35; panel SDN opened at "Your one small step" (`panelFocus` "help"); `showDrafts` true |
| `sudan-explainer-food` | The four Plan 2 crisis layers; panel SDN at its food section; the explainer card for "food" open (ruling R19); `showDrafts` true |

New scene field `explainer` (ruling R19): an explainer id (conflict, food, outbreaks, displacement, atrocities, camps,
access, why-these); it needs a panel, is validated like `panelFocus`, and a scene whose explainer is not shown (drafts
off) fails with a plain message. `SetLayers` replaces the marker set, so a scene lists `hub-lines` to show arcs.

## App launches (all 4 allowed: 3 in Task 10, the spare in the fix wave)

1. **Snapshot run** into `artifacts\snapshot-plan4-first` (named `snapshot-plan4` when taken, renamed afterwards):
   14 pictures, exit code 0. Every picture was looked at; the layout problems below were found here.
2. **Re-run after tuning** into `artifacts\snapshot-plan4`: 14 pictures, exit code 0.
3. **Repeat check** into `artifacts\snapshot-plan4-b`: 14 pictures, exit code 0. **PngDiff reported all 14 identical**
   between runs 2 and 3 (exit code 0).

4. **Fix-wave run** (the plan's spare) into `artifacts\snapshot-plan4-fix`: 14 pictures, exit code 0. There is no
   launch left, so there was **no repeat run** of the fixed pictures; what still reads badly is described below.

All launches were from PowerShell with an argument array, Release build, `--offline-samples tests\Samples`. No screen
capture of any kind was used; the pictures are the app's own RenderTargetBitmap.

## What each new picture showed (Task 10's run; the fix wave's pictures are under "Final fix wave")

- `access`: the sample's 12 invented assessments by class: Sudan and Yemen darkest (5, Extreme); South Sudan, DR Congo
  and Afghanistan a mid orange (4, Very high); Somalia and Ukraine a lighter orange (3, High); Nigeria pale yellow
  (2, Moderate). The strip has Getting help in open with
  "Access constraints" on and a "one shading at a time" note; "UN depots" is the next toggle (the rest scroll). The
  legend reads "Humanitarian access constraints (ACAPS score 0-5)" with the six classes (No constraints to Extreme) and
  "no figure: not shaded", then "ACAPS, via HDX, Humanitarian Access dataset, December 2025 edition". The brown ramp
  reads clearly against the dark oceans and the tan Sahara; the two lightest classes are close to the desert colour
  (the pale yellow Nigeria is distinguishable by its edge and tint).
- `sudan-access`: the panel opens at the "Getting help in" heading: "Access constraints 5 of 5: Extreme access
  constraints", "Trend Deteriorating", then the nine ACAPS indicators each as "n of 3" under ACAPS's own wording
  (the section continues below the view). After tuning, indicator 8 ("Presence of landmines, improvised
  explosive devices, explosive remnants of war and unexploded ordnance", 2 of 3, five lines) is in view; before tuning
  it was cut off. Indicator 9 is below the view.
- `sudan-help`: the panel opens at "Your one small step". Give: "UN pooled fund for Sudan (OCHA)", "Draft: not yet
  reviewed", the fund's name, OCHA's explanation, "Checked 24 Nov 2026" and two links. Charities: "No vetted charities
  listed yet" (no charity has a confirmed rating yet, so all nine drafts are hidden, drafts shown or not). Contact: "Your two senators" asks the user to choose a state in Settings; the House
  "find your representative" page (with the note that the app never asks for an address or ZIP code); the State
  Department; and Sudan's mission to the UN (below the fold). **The layout of a charity entry has not been seen in any
  picture:** every charity stays hidden until the owner confirms its rating (P4-8), even with drafts shown; its rows are
  covered by tests only. On the globe: airport hexagons and port diamonds all over
  (the whole of the airport layer), and two arcs from Sudan's label point: one to "Dongola Airport . 337 km
  straight-line", one to "UNHRD Dubai . 2,842 km straight-line" (the Dubai label runs under the panel; see below). The
  legend adds the UN depots line with the draft note. The port arc is short (Port Sudan) and hidden under the
  clutter at this zoom.
- `sudan-explainer-food`: the explainer card sits left of the panel: "Food crisis: what is it, and why does it
  happen?", "Draft: not yet reviewed", five paragraphs (IPC's five phases, what famine means, why it happens, what the
  map marks) in a scroller, with the sources below its fold. The panel behind it is at "Food crisis" with its
  "What is this, and why does it happen?" link in view.

## PngDiff

**Against `artifacts\snapshot-baseline`** (five old scenes only): all five differ. Against Plan 3's last run
`artifacts\snapshot-plan3-final` (nine scenes, a better reference): the globes are unchanged in `africa`, `pacific`,
`water` and the globe area of the others; the differences are:

- **The layer strip** (`africa`, `pacific`, `water`, `resources`, every panel scene): the strip gains the collapsible
  "Getting help in" group (closed, or open with the Access, UN depots, Airports, Ports and Lines toggles when a scene
  has one on) in place of Plan 3's "Getting help in: coming in a later version" line, so the strip and the legend card
  below it change height. The legend starts higher or lower accordingly.
- **The Sources card**: 14 status dots (the feeds ACAPS and FTS, and the Hubs dots, added) in place of 10.
- **The panel scenes** (`sudan-panel`, `sudan-panel-food`, `no-crisis-panel`, `myanmar-panel`, `dprk-camps`): the panel
  is longer (Getting help in and the step section are added), so its scroll thumb is smaller; in `myanmar-panel` and
  `dprk-camps` the pictures are also different because **the owner approved the Plan 3 curated rows on 2026-11-24**
  (OR-1): the triangle and the camp squares show, and the panel rows and legend no longer say "Draft: not yet
  reviewed" / "Drafts shown". In `sudan-panel` and `sudan-panel-food` Sudan's approved atrocity row is now in the panel
  (the "Genocide and mass atrocities" section follows "People forced from home").
- **The row-label column** (tuning, below) is wider in every panel: values start 20 px further right.
- `renewables`, `resources`, `water`: strip and status dots only. `renewables` has no earlier picture in the Plan 3
  final folder (it was added after it).

Between the first and second Plan 4 runs (the tuning): `access`, `africa`, `pacific`, `renewables`, `resources` and
`water` identical; the panel scenes differ in the panel only (the wider label column); `sudan-explainer-food` differs
(the card now draws over the legend). `sudan-help` differed by 21 pixels of the panel's scroll thumb.

## Tuned (one re-run)

- **Row-label column 130 px to 150 px** (`CountryPanel.xaml`): ACAPS's long indicator wordings wrapped to five and six
  short lines ("Denial of existence / of humanitarian / needs or / entitlements to / assistance") so the first eight rows used
  more than the panel's height and indicator 8 was cut off. At 150 px most wrap to three or four lines and eight rows
  fit in the view (the ninth is below). Values still fit ("5 of 5: Extreme access constraints" now wraps to two lines).
- **The explainer card is drawn over the legend** (`MainWindow.xaml`: the card is after the legend in the grid): the
  legend (700 px wide, semi-transparent) lay over the card's lower half and its text mixed with the card's. After this
  tuning the card was drawn on top, but it was still slightly see-through (`#F7…`), so the legend's text still showed
  faintly through it (m52); the fix wave made it opaque. The card's position next to the panel (left of it, 12 px gap)
  read well.

## Seen, not tuned

- **Sudan's hub labels under the panel** (still so after the fix wave): in `sudan-help` the "UNHRD Dubai . 2,842 km s..." label runs under the
  country panel. The label is placed at the arc's midpoint; with the panel open, labels in the east of the globe can
  fall under it. A real-app behaviour; the scene's look-at (15,35) is fixed by the plan.
- The Dongola label's text sits across its own arc line (the label has its dark halo, so it reads).
- The panel's right edge: a few values ("(41.0% of the people", a House sentence) end at the scroll bar's edge: the
  same as in the earlier panels, not a Plan 4 change.
- The airport layer at the whole-globe zoom was a dense field of hexagons (3,254) that hid the port arc; the fix wave
  draws the large airports only (ruling R20).
- "Not placed on the map (16)": airports 10, ports 4, ACAPS 1 (the synthetic XXK), GEM renewables 1; each is listed.
- Settings flyout, hover tooltips and the Distances/Your state controls are not in any picture (no scene opens them);
  their content is covered by tests.

## Counts in the offline run (samples judged 2026-11-22)

Airports 3,254 placed (10 not placed); ports 546 (4 not placed); FTS 2026: 57 appeal rows, 28 regional; ACAPS December
2025 edition, 12 countries (synthetic sample), 1 not placed; the five depots, the nine draft charities and the
explainers are curated and shown only with drafts. After the fix wave the airport layer draws the 1,144 large airports of
the extract; the nearest-airport search still uses all 3,254.

## Final fix wave (after the whole-branch review)

The final review (opus) said "ready to merge with fixes". One fix wave, on `plan-4` after 5daa3af, fixed:

- **F1, the panel kept jumping:** choosing a state or a unit, a feed refresh or the hub store rebuilt the open panel and
  scrolled it back to the top. Now only opening a country (or a new focus) scrolls; a rebuild keeps the place
  (`HelpPanelTests.RebuildOfTheSameCountryDoesNotScroll`, `AppStartupTests.ARebuildKeepsWhereThePanelWasScrolledTo`).
- **F2, arcs lost among the markers:** the arcs draw above every point-marker layer, 3 px wide and lifted above the
  markers; arc labels that would overlap move a line apart instead of being dropped; the Airports layer draws the
  large airports only (ruling R20), and its legend line says the panel's nearest airport may be a medium one.
- **Hub rows:** "Nearest airport with scheduled flights", "Nearest large or medium port", "Nearest UN depot"; a hub in
  another country names it ("Auckland, New Zealand (NZ AKL) · … straight-line"); a draft depot's row starts "Draft:
  not yet reviewed" and its arc label says "(draft)".
- **F3, food explainer:** the unsupported clause ("make each analysis together") is gone; famine now reads "at least one
  household in five", "at least three children in ten", "at least two people in every 10,000", cited to WFP's
  "Understanding famine" page (an IPC partner; FEWS NET, re-opened, words the food threshold per population). The
  access explainer uses ACAPS's level names ("No constraints" to "Extreme"), as the panel and legend do.
- **F4, the explainer card:** opaque; closes when the panel shows another country; at most 420 px wide and its left edge
  follows the layer strip, so it shrinks rather than cover the strip; the window has a minimum width of 1,000 px.
- **F5:** the committed IRS sample keeps only what the tests read (no names, compensation, preparer or phone numbers).
- **F6:** the UN depots are marked at the airport each depot's address names or sits beside (OurAirports, The Unlicense;
  ruling R21); every OpenStreetMap reference is gone from the data and docs.
- Minors: "sept" in ACAPS file names (m11); Save the Children's note no longer gives an unsourced member count (m44);
  the vetting tool reports a malformed `charities.json` as a clear error (m37); a test reformatted and made strict
  (m32); the curated review writes west longitudes as "° W" (m20); the spec's Plan 4 table gains R20 to R22.

**What the fix-wave pictures show** (`artifacts\snapshot-plan4-fix`, looked at with the app's own pictures; no repeat run):

- `sudan-help`: far fewer airport hexagons (large airports only), though Europe is still dense at this zoom. All three
  arc labels are placed: "Dongola Airport · 337 km straight-line", "Port Sudan · 918 km straight-line" (moved two lines
  down so it does not sit on the Dongola label) and "UNHRD Dubai (draft) · 2,85…" (the Dubai depot is now at Al
  Maktoum airport; the label still runs under the panel, as before). The blue depot arc and the white airport arc are
  drawn over the markers and read clearly. **Still reads badly:** the port arc (teal) leaves Sudan's label point in
  almost the same direction as the depot arc and bows up the same way, so after its first few pixels it lies under the
  blue depot ribbon; the port is named by its label but its line is hard to see. A later fix could lift each kind of
  arc by a slightly different amount or draw the port arc last. The panel shows "Your one small step" as before.
- `sudan-explainer-food`: the card is opaque (no legend text shows through), sits between the layer strip and the panel
  without touching the strip, and shows the corrected famine paragraph ("At least one household in five faces an
  extreme lack of food. At least three children in ten are acutely malnourished. And at least two people in every
  10,000 die each day.").
- `access`: unchanged from Task 10's run: Sudan and Yemen darkest (Extreme), South Sudan, DR Congo and Afghanistan mid
  orange (Very high), Somalia and Ukraine lighter orange (High), Nigeria pale yellow (Moderate); the legend lists the six
  classes from "No constraints" to "Extreme access constraints".

## Owner items

1. **Charities** (`docs/help-review.md`): for each of the nine draft charities open its Charity Navigator and BBB links;
   if it qualifies (Charity Navigator 3 stars or more, or all 20 BBB standards met) say so and Claude fills in the
   rating record and approval. Read each entry's note (ruling R16, R17: Save the Children US's affiliation wording,
   charities whose country evidence is on the international organisation's site, NRC USA's EIN matched by name).
   Until then every country shows the fund and "No vetted charities listed yet".
2. **Approve or correct** the explainers (`docs/curated-review.md`), the UN depots, pooled funds and contacts
   (`docs/help-review.md`). The food explainer was corrected in the fix wave (see below); its review note says that
   FEWS NET words the famine food threshold per population and WFP per household. The UN depots are now marked at
   airports (ruling R21): check that each airport is the right one.
3. **P4-9 name-merge question:** the charity list follows P4-9's rule using one reading of how organisation names are
   merged in the 3W file. Alternative merges would swap USA for UNHCR in for International Medical Corps in the first 15.
   Say which you prefer.
4. **Ruling R18 (P4-8 read strictly?):** to cite the thresholds the spec states, three evaluator method pages (Charity
   Navigator's ratings method, give.org's BBB standards and its Overhead Myth page) were opened once each in the
   built-in browser: not scripted, not profile pages. If P4-8 is to be read strictly, say so and the three citations are
   dropped (IRS and FTC sources remain). Also: a UN PDF opened a save dialog in the built-in browser during Task 9
   (nothing was saved).
5. **WPI licence:** NGA's own World Port Index page still states no licence we could read (its portal returned 503);
   the file is used as a US federal work (17 U.S.C. section 105). Confirm, or tell us to drop the ports layer.
6. **Optional:** send the ACAPS note in `docs/permission-requests.md` (section 5). The app works without a reply.
7. **Baseline pictures:** nothing was copied over `artifacts\snapshot-baseline`. To make the fourteen new pictures the
   baseline, copy `artifacts\snapshot-plan4\*.png` over it (needs your OK).

8. **Hub lines off after an upgrade:** a settings file saved by Plan 3 keeps its own list of marker layers, so "Lines to
   nearest hubs" stays off for you (a fresh install has it on). Turn it on once in the layer strip (Getting help in).
9. **Old sample in local history:** the fix wave trimmed `tests/Samples/irs990_sample.xml` (names, compensation,
   preparer and phone details removed), but the untrimmed file is still in this branch's earlier local commits. The
   branch has never been pushed; say if you want the history squashed before it is merged or pushed.

## Deferred minor findings

Listed in the ledger (`.superpowers/sdd/2026-11-24-plan-4-getting-help-in-and-one-small-step/progress.md`, m1 to m55;
those the final review chose were fixed in the fix wave),
none blocking: for example the FTS early-January fallback (m6), the per-frame recompute of hub labels (m23), and the
explainers' hard-coded app rules (m47).
