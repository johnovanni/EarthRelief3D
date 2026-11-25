# Plan 5 (shipping) results

Built 2026-11-25 on branch `plan-5` (local commits only; not pushed, not tagged, not merged). Plan 5 added: a signed
Inno Setup installer and release workflow (version 1.0.0), an app icon, the source registry (`data/sources.json`) with
the About and Data cards, the Welcome card, text size 100/125/150%, the folding layer strip and legend, the FTS January
rule, the port-arc lift, and the app's own update check and installer launch. The rulings are in the spec, "Rulings
made while building Plan 5" (P5-1 to P5-24 and R1 to R14).

## Tests

`dotnet build EarthRelief3D.sln -c Release`: 0 warnings, 0 errors. `dotnet test EarthRelief3D.sln -c Release`:
**819 passed, 0 failed, 0 skipped** (817 at the end of Task 10; Task 11 added two: `HelpPanelTests.CharitiesFocusPointsAtTheFirstCharity`
and `SnapshotScenesTests.CharitiesFocusIsAccepted`; the committed-scenes tests now expect 24 scenes and pin the new
ones' fields). `AppStartupTests.CachedIpcAreasLoadOffTheUiThreadAndReachTheMap` failed once in the first full run
(an assertion on a count, which was 14) and passed on the re-run, as in Plans 3 and 4; it is a start-up timing test, not
related to Plan 5. GlobeCore is unchanged.

Test-first note: the two new tests were written after the first version of the code; the RED run was taken afterwards
by setting the source changes aside (the tests did not compile: no `PanelFocus.Charities`, no `FocusedEntryIndex`),
then the code was put back and all tests went green. The view's scroll to the entry has no unit test (it needs a laid-out
panel); the picture `sudan-charities` is its check, and the first run found it not working (see "Tuned").

## Scenes

Twenty-four scenes, all at 2026-11-22T12:00:00Z: the fourteen of Plans 2 to 4 unchanged, and ten new:

| Scene | What it sets |
|---|---|
| `first-start` | The four Plan 2 layers, look at 10,25, legend closed, the Welcome card |
| `about` | The same, the About card |
| `data` | The same, the Data card |
| `text-150` | Plan 2 layers, panel SDN, text 150%, the Settings flyout opened (`card` settings) |
| `text-150-narrow` | As `text-150`, window 1,000 x 760, no card (Review Focus 1) |
| `sudan-charities` | Panel SDN scrolled to the first charity (`panelFocus` charities, P5-15) |
| `closed-cards` | Strip closed, legend closed |
| `update-offer`, `update-ready`, `update-stale` | The update banner in that state (synthetic release 1.1.0), look at 10,25 |

New `panelFocus` value `"charities"` (P5-15): `PanelFocus.Charities` and `CountryPanelViewModel.FocusedEntryIndex` (the
index, in "Your one small step", of the first charity entry: the first entry after the pooled fund or CERF that is not
the "No vetted charities listed yet" line; -1 otherwise). The view scrolls that entry's top to the top of the panel.

## App launches (3 of the 4 allowed; the spare was not used)

All from PowerShell with an argument array, the **published** self-contained exe (`dotnet publish ... -r win-x64
--self-contained true`, output `artifacts\publish`), `--offline-samples tests\Samples`. No screen capture of any kind:
the pictures are the app's own RenderTargetBitmap.

1. **Pictures run** into `artifacts\snapshot-plan5`: 24 pictures, exit code 0. Every picture of the ten new scenes was
   looked at, and `sudan-help`, `sudan-explainer-food` and `sudan-panel` of the old ones.
2. **Re-run after tuning** into `artifacts\snapshot-plan5-2`: 24 pictures, exit code 0 (the exe was published again after
   the tuning commit, so it carried it).
3. **Repeat run** into `artifacts\snapshot-plan5-b`: 24 pictures, exit code 0. **PngDiff reported all 24 identical**
   between runs 2 and 3 (exit code 0).
4. The spare: not used.

## PngDiff

Run with `dotnet run --project C:\repos\EarthWatch3D\tools\PngDiff -c Release -- <dirA> <dirB>`.

- **Run 2 against run 3:** 24 of 24 identical.
- **Run 1 against run 2 (what the tuning changed):** only `sudan-charities` (the panel's scroll position, 61,566 pixels
  inside x 959-1449, y 95-789) and `sudan-help` (the arcs and their labels, 6,561 pixels inside x 692-1066, y 357-461).
  The other 22 are identical.
- **The fourteen earlier scenes against `artifacts\snapshot-plan4-fix`:** all fourteen differ. Every difference was
  traced to a planned change (below); the globe itself is unchanged outside the cards (checked by comparing the globe's
  middle: the only differing pixels there lie under the legend, which now starts higher and is wider).

**Expected differences, and why:**

- **The strip header:** a ▾ arrow and the "Layers" header that closes the whole strip (P5-23), and an About button at
  the end of the strip next to Settings (Task 5). The strip's width is capped at 320 x scale.
- **The legend:** it is open in every old scene (scenes default to `legend` open, so the old pictures still show it), now
  with a header row "Legend (4 layers): colours, sources and dates" and a fold arrow (P5-24), and it is kept clear of
  the Sources card, so it is narrower (Task 4's `LegendMaxWidth`).
- **The Sources card:** a "Data" button before the word "Sources" (Task 4), which widens the card.
- **The panel** is 420 px wide (ruling R8; it was 380): every panel scene changes, its wrapping changes, and the
  scroll thumb changes. `no-crisis-panel`, `sudan-panel`, `sudan-panel-food` and the Plan 3 panels differ in the panel
  only (besides the cards above).
- **`sudan-explainer-food`:** the explainer card moves with the wider panel and the legend; 409,536 pixels (the most),
  because the panel, the card and the legend all moved.
- **`sudan-help`:** the arcs (P5-14, then R14) and the 420 px panel; the first charity entries now show (all sixteen
  charities are approved) instead of "No vetted charities listed yet".
- **The window icon** is not drawn: RenderTargetBitmap draws the window's content (the pictures show no title bar).

## What each new picture showed

**Text at 150%.**

- `text-150` (1,482 x 903 window): the strip (capped, with a scroll bar), the legend under it, the panel on the right
  and the Sources card at the bottom: **no card overlaps another** (strip 15-610 wide, legend ends at 785, panel starts
  at 800; the legend stops above the Sources card). The panel's text wraps and is not cut off: the rows' values end a
  few pixels short of the scroll bar ("1,025 deaths in the last 3 months, 652 in the 3 before" fills the line to the
  bar's edge). The explainer card is not open in this scene.
- `text-150-narrow` (1,000 x 760): the strip, legend and panel again do not overlap (strip to 525, legend to 583, panel
  from 663); the legend header wraps to two lines ("...sources and dates"); legend rows are readable; "Genocide and mass
  atrocities" and its "off" are not clipped by the strip's width cap. The panel's values wrap to three lines at this
  width ("Sudan: / Government"), nothing is cut at the right edge.
- **Not seen: the Settings flyout.** `text-150` opens the flyout, but it sits at the end of the strip, below the strip's
  fold (the strip scrolls, and its scroll bar is showing), so the Text size control is not in the picture. It is
  reachable by scrolling the strip; the picture cannot prove that. See "Seen, not tuned".
- **Not seen: the IPC phase bar at 150%** (the food section is below the fold in these scenes). `PhaseBarView` is not a
  fixed width in practice: its `MeasureOverride` uses the available width (300 only when the width is unbounded), and at
  100% in `sudan-explainer-food` the bar fills the panel's width. Its labels at 150% were not pictured.

**Welcome (`first-start`).** The card reads: "Welcome to EarthRelief3D", two paragraphs (the globe shows where people
are suffering, what they lack, why help is hard to get in and one small thing to do; every fact comes with its source and
date; where the legend, the dots and Data are), "What the marks mean" (a key per layer), a shapes-as-well-as-colours
note, a "Click a country" line, a Text size choice (100/125/150%), the optional state note, and "Settings" and "Start"
buttons. The keys: Armed conflict (pink dot), **"Food: Phase 3 Crisis" (orange), "Food: Phase 4 Emergency" (red), "Food:
Phase 5 Catastrophe/Famine" (dark red)**, Disease outbreaks (blue cross), People forced from home (white ring) and
"Lines to nearest hubs". The shapes and colours match the map's markers in the scene (pink dots, orange and red food
shading, blue crosses, white rings). "Lines to nearest hubs" is a default-on layer in a fresh app but the scene's four
layers do not include it, so the scene's strip has no hub lines to match it; they match in a real first start. The
legend behind the card is closed: one header row ("Legend (4 layers) ... closed") plus the UN boundary note, and the card
(centred) covers part of that header. A fresh app would say "5 layers" (ruling R7); the scene sets four.

**About (`about`).** "About EarthRelief3D", "Version 1.0.0", the MIT licence line with Copyright (c) 2026 John M.
Apolloni, "Licence texts" and "Releases on GitHub" links, the no-money / no-ranking / no-images / no-sound / no-personal-data
paragraph, then "Data sources": each as a link with its credit line (UCDP, IPC country figures, IPC area analyses,
UNHCR, WHO outbreak news, WHO GHO, OCHA HNO "CC BY-IGO", FAOSTAT with its licence, EIA, WRI Aqueduct, GEM, ...). **The
severity band lines and the UN boundary note are below the card's fold (it scrolls) and were not seen in a picture;**
they are covered by About's tests.

**Data (`data`).** A table: Source (a link), What it gives, How ("fetched by the app"), As of, Licence. Rows seen:
UCDP "as of 6 Nov 2026 · checked 22 Nov 2026" (CC BY 4.0); IPC country figures "as of Sep 2026 · checked 22 Nov 2026"
(CC0 / public domain); IPC area analyses "no area analyses loaded · checked 22 Nov 2026" (the offline sample has none, so
it says so); UNHCR "as of 30 Jun 2026 · checked 22 Nov 2026" with the qualified licence (ruling R5: "CC BY 4.0 (from
UNHCR's published terms; not yet confirmed for the API's data)"); WHO outbreak news "as of 18 Nov 2026" (CC BY-NC-SA 3.0
IGO); WHO GHO "estimate year 2024" with the qualified licence; OCHA HNO "file loaded 13 Feb 2026" (CC BY-IGO). **Compared
with the legend: only UCDP could be, "as of 6 Nov 2026" in both** (the legend's other date lines are below its fold in
every scene, and the Data card's later rows are below its own fold), so two of the three comparisons the plan wanted
were not made in a picture.

**`sudan-charities`.** After the tuning the panel opens at the first charity, "Action Against Hunger USA": "Tax-deductible
(IRS Publication 78, checked 24 Nov 2026)", "90% spent on programmes (Form 990, tax year 2024)", "Rated by Charity
Navigator", "Working here: Hunger Relief in Sudan | Action Against Hunger (checked 24 Nov 2026)", "Reviewed 25 Nov 2026",
then three links ("Charity Navigator report", "Evidence: ...", "Donate page"), each with ↗; CARE USA and Doctors Without
Borders USA follow in the same layout. **The scene clock is 22 Nov 2026, before "checked 24 Nov" and "Reviewed 25
Nov 2026"** (P5-15): the entries show anyway because `VettingRules` does not reject a future review; a real user's clock
is later. The long evidence titles wrap ("Evidence: Sudan | Our Work & How to Help | Doctors Without Borders - USA ↗").
This is the first picture of a charity entry (Plan 4 never saw one).

**`sudan-help`: the port arc against the depot arc (P5-14).** Run 1 (port 1.5, depot 1.0): Sudan's label point to Port
Sudan and to the Dubai depot leave in the same direction, and the teal port arc showed only for its first ~35 pixels,
then lay under the blue depot ribbon (the depot arc is longer and climbs higher after the first third). The factors
alone could not separate the two where they meet the surface, so the port was raised and the depot lowered (R14: port
2.0, depot 0.5, airport 1.25 unchanged). Run 2: the teal ribbon now runs from Sudan along almost the whole way to the Port
Sudan marker (it is still hidden for roughly its last eighth, where it must land on the surface and the depot arc passes over
it). The white Dongola airport arc (north) is clear. **Still reads badly:** the "Port Sudan · 918 km straight-line" label
sits two lines below the arc (Plan 4's label collision rule moves it), so the label is not beside its arc; and the "UNHRD
Dubai · 2,85..." label still runs under the panel.

**`closed-cards`.** The legend is closed to one header row plus the UN boundary note (one short card at the bottom left).
The strip is closed to its header, "Layers" with the word "closed" and a ▸ arrow, **plus the Settings and About rows**
(the Settings and About buttons stay when the list is closed; P5-23 said only the header shows). The globe is fully
visible with the Sources card at the bottom right.

**Update banners.** `update-offer`: "A new version of EarthRelief3D is available: 1.1.0 (released 3 Feb 2027). You have
1.0.0. New versions bring updated data and fixes." with "Download and install", "What's new" and "Later".
`update-ready`: "Ready to install 1.1.0. EarthRelief3D will close, the installer will run (Windows will ask for your
permission), and EarthRelief3D will start again." with "Install now" and "Later". `update-stale`: "EarthRelief3D has not
been able to check for a new version for 30 days. Its included data may be out of date: Data shows each source's date."
with "Check now" and "Later". The banner is a rounded, bordered bar from the strip's right edge to the window's right
edge at the top, one line of text and a row of buttons; it covers the top of the globe only. **No scene combines a
banner with a panel or a card, so ruling R13 (the panel and the explainer start below the banner) is not in a picture;**
it is pinned by `TextScaleLayoutTests` at 150% in 1,000 x 760.

## Tuned (one re-run)

- **`PanelFocus.Charities` did not scroll to the entry** (`Views/CountryPanel.xaml.cs`): run 1 opened the panel at the
  "Your one small step" heading, not the first charity. The view looked for the entry's element before the panel's
  first layout pass, found none and fell back to the section. It now calls `UpdateLayout()` first. Run 2 shows the first
  charity at the top.
- **Lift factors** (`HubArcLayer.LiftFactor`): port 2.0 (was 1.5), airport 1.25 (unchanged), depot 0.5 (was 1.0), ruling R14.
  `HubArcGeometryTests` pin the new values and check that the port arc is above the depot arc for the first three
  quarters of its length (it was the first half).

## Seen, not tuned

- **The Settings flyout at 150%** is below the strip's fold: the strip is capped in height (so the legend fits under it),
  scrolls, and the Settings and About rows, with the flyout under Settings, are reached by scrolling the strip. Review
  Focus 1 asked that the Text size control stay reachable; it is reachable by scrolling but a user at 150% may not
  think to scroll. A larger fix (Settings above the list, or the flyout as a card) is a redesign. The Welcome also offers
  the Text size choice itself, so a new user need not find the flyout.
- **A panel's right edge:** values and the IPC percent column end a few pixels from the scroll bar (e.g. "25.4%" in the
  food section at 100%). Nothing is cut off in any picture; a small right margin would give air. As in Plan 4.
- **The `Port Sudan` label** is two lines under its arc and the Dubai depot label runs under the panel (see above).
- The closed strip still shows Settings and About (see `closed-cards`).
- `first-start` and the modal cards (Welcome, About, Data) cover part of the strip and legend; they are modal over a
  dimmed globe, so this is by design.
- The panel's text at the right of the `text-150` picture is tight against the scroll bar at some lines.

## What was NOT verified, and why

- **The installer build** (`packaging/EarthRelief3D.iss`) and **the release workflow**: never run. Inno Setup is not
  installed here and nothing is installed on the owner's PC (P5-17); `ReleasePackagingTests` read the files as text. The
  first real build is the GitHub pull-request run (owner item 4).
- **Signing:** no Azure command was run; the workflow's signing and its Trusted Signing set-up are untested until a
  release (owner items 1 to 3, 5).
- **Installing, uninstalling, and what the uninstaller leaves behind** (`%LOCALAPPDATA%\EarthRelief3D`).
- **A real update between two releases** (needs v1.0.0 and v1.0.1 published; owner item 6). Specifically unverifiable
  until then (Task 10's list): Inno's self-elevation and the exit code it returns on a refused prompt; the Restart Manager
  close reaching the app's session-ending handler; `runasoriginaluser` restarting the app **not elevated**; whether
  `WinVerifyTrust` and the signer-subject comparison hold for Azure Trusted Signing (timestamped) certificates; the
  shell start with the Windows permission prompt while the app holds the file with `FileShare.Read`; and the real format of
  GitHub's `digest` field (the plan's example came from EarthWatch3D's release).
- **Windows' permission prompt** (UAC): not shown by any picture or test.
- **The hover tooltip's size at 150%:** GlobeCore's tooltip is not scaled (P5-8, GlobeCore unchanged) and no picture
  shows a tooltip.
- **The About card's lower half** (severity bands and the UN boundary note), **the Data card's later rows** and **the
  legend's later date lines** were below the folds of their pictures; two of the three requested Data-against-legend date
  comparisons were therefore not made in a picture.
- **The IPC phase bar's labels at 150%** and **the update banner above a panel or card** (R13) were not pictured.
- **The Settings flyout's Text size control** was not in a picture (see above).
- **The window icon in the title bar and the taskbar** (the pictures are of the window's content only); the icon file was
  viewed in Task 9.
- **The first-start wiring in `App.xaml.cs`** (the Welcome never opening in picture runs, once per user, the Settings
  button leaving the card open) is tested only through its pieces (`WelcomeViewModelTests`); no run started the app in
  its normal mode, because the 4-launch limit left no launch for it.
- **The Welcome's food key wording** "Food: Phase N <IPC name>" is Claude's wording, not reviewed by the owner.

## Owner items

From the plan:

1. **Before building (done in part):** review P5-18 to P5-22 (the update check), P5-23 and P5-24, P5-8, P5-11, the
   Welcome text and the banner texts, all now pictured above.
2. **Before the first signed release (GitHub settings; Claude cannot do them):** create the `release` environment;
   add the repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`; add the federated
   credential subject of P5-4 to the Entra app. Also `docs/release.md`'s statement that the Entra app "already holds" the
   signer role was not checked (ledger m48).
3. **Say whether to push the `plan-5` branch and open a pull request:** its run builds the **unsigned** installer on
   GitHub, the first real check of the `.iss` and the workflow.
4. **After merging, tag `v1.0.0`** to build, sign and publish (or tell Claude to), then run the install checklist in
   `docs/release.md` (install, Start menu entry, first-start Welcome, uninstall leaves `%LOCALAPPDATA%\EarthRelief3D`).
5. **Try the update path once:** with v1.0.0 installed, release v1.0.1 and check that the banner appears, downloads,
   checks, asks Windows' permission, installs and starts 1.0.1 not elevated, with settings kept.
6. **Release often enough to keep the data current:** the first approved charity expires on 25 Nov 2027, so a release with
   fresh reviews must come before then (`docs/release.md` has the "Before each release" checklist).
7. **Check P5-1's names:** the install folder `Qapow`, the publisher "John Apolloni", and `AppPublisherURL`.
8. **Still open from Plan 4:** the WPI licence, ruling R18 (three evaluator method pages) and the optional ACAPS note.
9. **Baseline pictures:** nothing was copied over `artifacts\snapshot-baseline`. To make the twenty-four new pictures
   the baseline, copy `artifacts\snapshot-plan5-2\*.png` over it (needs your OK).

Added by Task 11:

10. **Confirm the UNHCR and WHO GHO licences (ruling R5):** the registry, About, Data and the notices say UNHCR is
    "CC BY 4.0 (from UNHCR's published terms; not yet confirmed for the API's data)" and WHO GHO is "CC BY 4.0 (WHO's
    data terms; not yet confirmed for GHO API rows, which may be CC BY-NC-SA 3.0 IGO)". Confirm, or tell us which wording
    to use; the other app-chosen credits are marked in `docs/data-sources.md` section 21.
11. **The Welcome's food key wording:** "Food: Phase 3 Crisis", "Food: Phase 4 Emergency", "Food: Phase 5
    Catastrophe/Famine" (Claude's wording, from the IPC names the map and legend use). Say if you want another.
12. **R11 changes P5-21:** the app starts the installer **without** the Windows `runas` verb (the installer asks for permission
    itself), so the app is restarted as you and not as an administrator, and a refusal shows "The update was not installed:
    the installer stopped (exit code N). If Windows asked for your permission and it was not given, that is why." Say if you
    would rather the app asked for permission itself (it would restart elevated, which was judged a security fault).
13. **Closed strip:** it keeps Settings and About under the closed header (P5-23 said only the header). Say which you want.
14. **Settings at 150%:** the Text size control is below the strip's fold (scroll to reach it). Say if it should move.

## Deferred minor findings

Listed in the ledger (`.superpowers/sdd/2026-11-25-plan-5-shipping/progress.md`, m1 to m64), none blocking. The ledger
marked these for the final wave and they were **not** fixed in Task 11: m53 (a check that stalls after the headers
hangs the day's checking: add a timeout), m58 (an app the user started elevated restarts elevated: offer only the
download page then), m63 (the failed banner shows raw 64-hex digests: keep them in the log) and m64 (test gaps in the
updater).
