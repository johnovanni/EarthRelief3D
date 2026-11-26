# Plan 5: Shipping

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** EarthRelief3D becomes a release that a member of the public can install:
- a **Welcome** card on first start (what the marks and colours mean; every fact has a source);
- an **About** card (version, every credit and licence, how markers are sized, the UN boundary note) and a `THIRD-PARTY-NOTICES.txt` installed with the app;
- a **Data** card listing every source with its as-of date, licence and link;
- a **Text size** setting (C8);
- an app icon, a **signed installer** and a tag-triggered **release workflow** modelled on EarthWatch3D's;
- an **update check** (owner, 2026-11-25: "it should check for a new release and warn the user … ask if the user wants to download the new update and do it for them … the data that ships with it must be kept up to date"): once a day the app asks GitHub for the newest release; when there is one, a banner says so, and on the user's yes the app downloads the installer, checks its checksum and signature, and runs it;
- the layer strip that really closes and a legend that folds away, closed by default (owner, 2026-11-25);
- the Plan 4 follow-ups: FTS appeals in January (m6, m13), the hard-coded WPI note (m18), the port arc hidden under the depot arc, and a picture of a charity entry.

**Architecture:**
- One **source registry** (`data/sources.json`, read by `EarthRelief3D.Core.Sources.SourceRegistry`) holds each source's name, use, licence, credit and link. About, the Data card and `THIRD-PARTY-NOTICES.txt` all come from it, and tests fail when a credit drifts from `docs/data-sources.md` or from the notices file.
- Welcome, About and Data are **cards inside the main window** (like the Settings flyout and the explainer card), so `--snapshot-scenes` can picture them; one card is open at a time.
- Text size is a `LayoutTransform` on the cards plus a factor on the globe's label fonts; the 3D globe is not scaled.
- The installer is **Inno Setup**, signed with **Azure Artifact Signing**, built by `.github/workflows/release.yml` on a version tag, as EarthWatch3D does it today.
- **Updates:** `EarthRelief3D.Core.Updates` reads GitHub's "latest release" JSON and decides whether it is newer; the App's `UpdateService` downloads the installer into the data folder, checks GitHub's SHA-256 digest and the Authenticode signer against the running app's, and starts the installer, which closes the app and starts it again as the user.
- **GlobeCore is not changed.**

**Tech Stack:** C# / .NET 10, WPF, HelixToolkit.Wpf 3.1.2 (through GlobeCore.Wpf), CommunityToolkit.Mvvm 8.4.0, ExcelDataReader 3.7.0, xunit 2.9.2; Inno Setup 6 (on the GitHub runner only); Azure Artifact Signing (`Azure/artifact-signing-action@v2.0.0`, `azure/login@v3.1.0`); Python 3 with Pillow 13 for the icon script (maintainer tool, not shipped).

**Spec:** `C:\repos\EarthRelief3D\docs\superpowers\specs\2026-11-21-earthrelief3d-design.md`. Relevant parts: §1 (success criteria, anti-goals, "Shipping"), §5 (the exact attribution text goes in About and `THIRD-PARTY-NOTICES.txt`), §11 (Data view, text size, UN boundary note in About and under the legend, Welcome, About), §15 item 5; calls C7 (the severity-band mapping is documented in About), C8, C12; decision Q3. Model to follow: `C:\repos\EarthWatch3D\.github\workflows\release.yml`, `packaging\installer\EarthWatch3D.iss`, `docs\release.md`, `src\EarthWatch3D.App\ViewModels\AboutViewModel.cs` and `WelcomeViewModel.cs`, `THIRD-PARTY-NOTICES.txt`. Follow-ups from `docs/plan-4-results.md` and `docs/superpowers/plans/plan-4-sdd-ledger.md`.

## What was checked while writing this plan (2026-11-25)

| Fact | Finding | Consequence |
|---|---|---|
| How EarthWatch3D ships | Since v1.0.1 (commit 959ad9b) a signed **Inno Setup** installer `EarthWatch3D-Setup.exe`, installing to `C:\Program Files\Qapow\EarthWatch3D`; MSIX (`tools/build-release.ps1`) "is no longer used for releases" (`docs/release.md`). The workflow signs the app's own DLLs, then the installer, in the `release` environment with GitHub OIDC | P5-1 |
| GlobeCore's visibility | **Public** now: `gh api repos/johnovanni/GlobeCore` says `"visibility":"public"`, and an anonymous `git ls-remote https://github.com/johnovanni/GlobeCore.git` answers `fa83558…`, the commit EarthRelief3D pins. AstroCore is public too (`81cdb28…`, pinned). EarthWatch3D's workflow comment ("private for now") is out of date | P5-3 |
| Submodule URLs | `.gitmodules` uses relative URLs `../AstroCore`, `../GlobeCore`; on GitHub they resolve to `johnovanni/AstroCore` and `johnovanni/GlobeCore` | No change needed |
| EarthRelief3D on GitHub | Public, MIT; repository id 1468090118, owner id 11181099; **no environments, no variables, no secrets** yet | Owner items 2–3 |
| App icon | EarthRelief3D has none (EarthWatch3D's comes from `tools/make-icons.py`, Pillow) | Task 9 makes one |
| Text sizes in the app | 67 fixed `FontSize=` in the XAML, plus `LabelsOverlayLayer.FontSize` and `HubLabelsLayer.FontSize` (12) | P5-8: scale, not per-font edits |
| UN boundary note | Already under the legend since Plan 2b (`LegendViewModel.BoundaryNote`, inside `BoundaryText`) | Kept; About repeats it (Task 5) |
| Charities in Sudan | 13 of the 16 approved charities list SDN | The charity picture uses Sudan (Task 11) |
| Pillow | 13.0.0 installed | Icon script runs here |
| GitHub's release API (EarthWatch3D's v1.0.2 as the example) | `GET https://api.github.com/repos/<owner>/<repo>/releases/latest` (no key) gives `tag_name` "v1.0.2", `draft`, `prerelease`, `published_at`, `html_url`, and per asset `name`, `size` (the installer: 53,180,168 bytes), `browser_download_url` (`https://github.com/<owner>/<repo>/releases/download/v1.0.2/EarthWatch3D-Setup.exe`, answering 302 to the file) and **`digest` "sha256:…"**, computed by GitHub. `/releases/latest` never returns a draft or a prerelease | P5-18 to P5-22 |

## Rulings made while writing this plan

| # | Ruling | Reason | Cost if wrong |
|---|---|---|---|
| P5-1 | **The installer is a signed Inno Setup `EarthRelief3D-Setup.exe`, not an MSIX**, installing to `C:\Program Files\Qapow\EarthRelief3D` (the user may pick another folder), publisher "John Apolloni", a Start menu entry under Qapow, as EarthWatch3D's `.iss`. Settings, cache and logs stay in `%LOCALAPPDATA%\EarthRelief3D` | The task says "as EarthWatch3D does it", and EarthWatch3D moved from MSIX to this installer at v1.0.1. The spec (§1 "a signed MSIX", §15 "MSIX and signing") predates that move | Windows does not update an Inno install by itself, so the app does it (P5-2) |
| P5-2 | **The app checks for and installs updates itself** (owner, 2026-11-25), replacing MSIX's App Installer updates: P5-18 to P5-22. About also shows the version and links "Releases on GitHub" (`https://github.com/johnovanni/EarthRelief3D/releases`) | The bundled data (charities, extracts, curated rows) only reaches users through new versions, and charity entries expire 12 months after review | EarthWatch3D has no such check; this is new code with its own tests (Task 10) |
| P5-18 | **When it checks:** 30 seconds after the window first shows, then every 24 hours while the app runs, with one request to `https://api.github.com/repos/johnovanni/EarthRelief3D/releases/latest` (no key; the same host the Borders feed already asks daily). Settings "Updates: check for a new version once a day", on by default. Never in a diagnostic or offline run (a picture run reads a sample release only when a scene asks, P5-22). A failure is logged and shown in About ("Could not check for a new version on <date>: <reason>"); the next check is the next day. No status dot: the dots are data sources | One small request a day; the user can turn it off | None |
| P5-19 | **The offer is a banner, not a pop-up:** at the top centre of the window, no sound, nothing modal: "A new version of EarthRelief3D is available: 1.1.0 (released 3 Feb 2027). You have 1.0.0. New versions bring updated data and fixes." Buttons "Download and install", "What's new" (the release page) and "Later". "Later" hides it until the app next starts, or until a still newer version is found; it comes back at each start while an update waits | Anti-goal "no pop-up alarms"; the owner wants the user asked | A user can keep choosing Later |
| P5-20 | **The download and its checks:** only the asset named exactly `EarthRelief3D-Setup.exe` of a release whose tag is `v<version>`, from `https://github.com/johnovanni/EarthRelief3D/releases/download/v<version>/EarthRelief3D-Setup.exe` (redirects followed, https only, at most 10); at most 300 MB and exactly the API's `size`; its SHA-256 equal to the API's `digest` (when a release has no digest, the `EarthRelief3D-Setup.exe.sha256` asset's); its Authenticode signature valid (`WinVerifyTrust`) with the **same signer subject as the running app's exe**. Into `%LOCALAPPDATA%\EarthRelief3D\updates\<version>\`, with progress ("12 of 53 MB") and Cancel in the banner; older update folders are deleted at start. If the running app is not signed (a developer build), or any check fails, the file is deleted and the banner offers only "Open the download page", saying why | A downloaded program must be the one the owner's workflow signed; GitHub's digest guards the transfer, the signer check guards the source | A release built without signing can never be installed by the app (by design) |
| P5-21 | **Installing:** the banner says "Ready to install 1.1.0. EarthRelief3D will close, the installer will run (Windows will ask for your permission), and EarthRelief3D will start again." with "Install now" and "Later". "Install now" starts the installer with `/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS` (shell verb `runas`); only once Windows has started it does the app close normally (settings and cache saved). If permission is refused, the app stays open and says "Windows did not give permission, so the update was not installed." The `.iss` gains a second `[Run]` entry with `Flags: nowait runasoriginaluser skipifnotsilent`, so a silent update starts the app again **as the user, not as administrator** | The user sees Windows' own prompt and the installer's progress; nothing is hidden | None |
| P5-22 | **Stale copies say so:** if no update check has succeeded for 30 days (`AppSettings.LastUpdateCheckOkUtc`; a copy that has never succeeded counts from its first attempt, saved as `AppSettings.FirstUpdateCheckUtc`; with checking turned off, from the day it was turned off), the banner reads "EarthRelief3D has not been able to check for a new version for 30 days. Its included data may be out of date: Data shows each source's date." with "Check now" and "Later". Picture runs read the synthetic `tests/Samples/github_release_latest_newer.json` only for a scene with `"update"` set, so the banner states can be pictured | The owner's reason for updates is data freshness; a copy that cannot check must not look current | One more banner state |
| P5-3 | **A public clone builds without a token**, because GlobeCore and AstroCore are public (checked above). The workflow still checks out with `token: ${{ secrets.SUBMODULES_TOKEN \|\| github.token }}`, so if GlobeCore were made private again the owner only adds that secret (a fine-grained token with read-only Contents on `johnovanni/GlobeCore`); `docs/release.md` says so. No secret is needed today | Same line as EarthWatch3D; nothing to set up now | None |
| P5-4 | **Signing** uses EarthWatch3D's account and profile (`JohnApolloni`, `JohnApolloniPublic`, endpoint `https://cus.codesigning.azure.net/`), the `release` environment and the repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. The Entra app needs a federated credential with subject `repo:johnovanni@11181099/EarthRelief3D@1468090118:environment:release` (EarthWatch3D's form, with this repository's ids). Signed: `EarthRelief3D.App.exe`, `EarthRelief3D.App.dll`, `EarthRelief3D.Core.dll`, `AstroCore.dll`, `GlobeCore.dll`, `GlobeCore.Wpf.dll`, `GlobeCore.Earth.dll`, then the installer. Third-party DLLs keep their publishers' signatures | One signing set-up for all the owner's apps | The owner's Azure set-up (owner items 2–3) |
| P5-5 | **The release workflow also runs the tests** (`dotnet test EarthRelief3D.sln -c Release`) before publishing; EarthWatch3D's does not. Pull requests touching `.github/workflows/release.yml`, `packaging/**` or `src/**` build unsigned and need no Azure | A release must not be built from a red tree | A flaky test (Plan 4's `CachedIpcAreasLoadOffTheUiThreadAndReachTheMap`) can fail a run: re-run it, and fix the test if it recurs |
| P5-6 | **Version 1.0.0** in `Directory.Build.props` (Task 9). Tagging `v1.0.0`, pushing, and creating the release are the owner's | The first public release | One edit |
| P5-7 | **Welcome, About and Data are cards in the main window**, not separate windows (EarthWatch3D uses windows and a `--dialog` diagnostic). One card at a time over a dimmed globe; Esc closes the card first, then the explainer, then the panel. A snapshot scene opens one with `"card"` | The Settings flyout and the explainer are already cards, and cards can be pictured with RenderTargetBitmap | None |
| P5-8 | **Text size:** Settings "Text size" with 100% (default), 125%, 150%, saved as `AppSettings.TextScale`; any other saved value reads as 1.0. Applied as a `ScaleTransform` `LayoutTransform` on every card (layer strip with its flyout, legend, panel, explainer, Sources card, Welcome, About, Data) and as a factor on the globe's label fonts. The panel's on-screen width is `min(420 × scale, 0.45 × window width)`; the legend and the explainer fit the space left and wrap more. GlobeCore's hover tooltip is not scaled (GlobeCore is not changed; recorded in the results) | One transform scales the 67 fixed sizes consistently; C8 asks for larger text for older users | The hover tooltip stays at 100% |
| P5-9 | **One source registry** (`data/sources.json`). Each row's `credit` is the source's own credit wording, as recorded in `docs/data-sources.md`; a `{date}` placeholder is filled with the as-of date on screen and written `<date>` in the notices file. Tests: every credit fragment (the text between placeholders) appears verbatim in `docs/data-sources.md` and in `THIRD-PARTY-NOTICES.txt`; every status dot and every bundled dataset folder has a row; every link is https | §5: "the exact attribution text goes in About and THIRD-PARTY-NOTICES.txt"; one list cannot drift | A new source needs one JSON row |
| P5-10 | **About also documents the severity bands** (C7): one line per crisis kind, built from `SeverityBands` and its constants, and a link to `docs/severity-bands.md` on GitHub | C7 says the mapping is documented in About | None |
| P5-11 | **Welcome shows once** per user (`AppSettings.WelcomeShown`), never in a diagnostic or offline run. A settings file from Plan 4 has no flag, so an existing user sees it once. About has "Show the welcome again". The Welcome's key rows come from `LayerStyle` and `LayerCatalog` for the layers on by default, so the card and the map cannot disagree. The Welcome offers the text-size choice itself | Older users may need larger text before they can read anything else | None |
| P5-12 | **FTS in January (m6, m13):** from 1 to 31 January of year Y, a country with no plan for Y keeps its Y−1 plans; from 1 February only Y's plans are kept. When the file has no Y plans at all, Y−1 is used whatever the date (as today, logged at Warn). The panel heads such a country's section "Humanitarian appeals <Y−1>" and starts it with "No appeal in FTS for <Y> yet". A regional plan is regional within its own year | FTS publishes new-year plans over several weeks; one early plan must not hide every other country's appeal | The January window is our choice, written in the legend's FTS note |
| P5-13 | **The WPI licence note is an input** (m18): `DataTool ports … --portal-note "<what NGA's portal showed, with the date>"`; without it the note says the portal was not re-checked for this extract. The committed manifest is not rebuilt (its note is true for 2026-11-24) | A hard-coded date goes stale at the next refresh | None |
| P5-14 | **Port arc (sudan-help):** each hub kind gets its own lift factor, port 1.5, airport 1.25, depot 1.0 (`h = min(0.08 × k, 0.25 × angle × k)`), so the short port arc stands above the depot arc that leaves Sudan in nearly the same direction. The cause in the picture is not proven; the picture decides (Task 11 may change only these three factors in its one tuning run) | Plan 4 found the port arc lying under the depot ribbon | Arcs a little higher; labels move with the midpoints |
| P5-15 | **The charity picture** (`sudan-charities`) opens Sudan's panel scrolled to the first charity entry, through a new `panelFocus` `"charities"`. The scene clock (2026-11-22) is before the charities' review date (2026-11-25); `VettingRules` does not reject a future review, so they show. Real users' clocks are later than the review | The layout of a charity entry has never been seen (Plan 4 results) | The picture shows "Reviewed 25 Nov 2026" at a 22 Nov clock |
| P5-16 | **App icon:** `tools/make-icon.py` renders an orthographic Blue Marble globe centred on 10° N, 30° E (Sudan and the Horn of Africa), fully lit, with a pale rim and a transparent background, into `src/EarthRelief3D.App/app.ico` (16, 24, 32, 48, 64, 128, 256 px). It differs from EarthWatch3D's night-shaded globe | Needed by the installer and the window | Owner may want another design |
| P5-23 | **The "≡ Layers" header closes the whole strip** (owner, 2026-11-25: "When you try to close the Layers window, it doesn't actually close. Instead it removes all the names of the layers"). It was a labels-on/off switch from EarthWatch3D; the icons-only mode is removed, because a symbol without its name does not say what a layer is. Closed, only the header shows (▸ "Layers", "closed"). Open by default; the saved name `LayerStripExpanded` is kept and now means open, so a file that hid the labels opens with the strip closed | The owner expected a close button | A user who liked icons only loses that mode |
| P5-24 | **The legend folds away and starts closed** (owner, 2026-11-25: "the amount of space the explanatory window on the bottom left takes is enormous … it probably should be minimized by default"). Closed, it is one header row, "Legend (4 layers): colours, sources and dates", plus the UN boundary note, which stays on the map (Q3, §11). Open, it is today's legend, at most half the window's height (it scrolls). The choice is saved (`AppSettings.LegendOpen`, default false). Every fact still shows its source on screen without the legend: the panel's source lines, the hover tooltips, the status dots and the Data card; the Welcome says where the legend is | The owner's request; the globe is the main view | A new user does not see the colour key until opening it (the Welcome shows the main marks, Task 6) |
| P5-17 | **The installer is not built on this PC.** Inno Setup is not installed here and installing it changes the machine; the first installer build is the GitHub pull-request run (owner item 4). Locally, Task 9 runs `dotnet publish` and checks its contents, and Task 11 takes the pictures with the **published** exe, which proves the self-contained output runs. **The update path cannot be tried end to end before two releases exist**: the first real test is installing v1.0.0, then releasing v1.0.1 (owner item 6) | No software installed without the owner | The `.iss` and the updater are checked only by tests until then |

## Owner items

- **Before building:** review this plan, in particular P5-18 to P5-22 (the update check) and P5-23, P5-24 (the strip and the legend), both added 2026-11-25 at your request, P5-8, P5-11, the Welcome draft text in Task 6 and the banner text in Task 10.
- **Before the first signed release** (GitHub settings; none can be done by Claude):
  1. In `johnovanni/EarthRelief3D` → Settings → Environments, create `release`.
  2. Add repository variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (the values EarthWatch3D uses).
  3. Add the federated credential subject of P5-4 to the Entra app (or run your `trust-repos.ps1` if it covers EarthRelief3D).
- **After Task 11:**
  4. Say whether to push the `plan-5` branch and open a pull request: its run builds the **unsigned** installer on GitHub (the first real check of the `.iss` and the workflow).
  5. After merging: tag `v1.0.0` to build, sign and publish the release (or tell Claude to). Then run the install checklist in `docs/release.md` (install, Start menu entry, first-start Welcome, uninstall leaves `%LOCALAPPDATA%\EarthRelief3D` with your settings).
  6. **Try the update path once:** with v1.0.0 installed, release v1.0.1 (a version bump is enough). Within a minute of starting, 1.0.0 should show the banner; "Download and install" should download, check, ask Windows' permission, install and start 1.0.1 with your settings kept. Until then the updater is tested only by its unit tests (P5-17).
  6a. **Release often enough to keep the data current:** `docs/release.md` gets a "Before each release" data checklist (refresh the bundled extracts, run the vetting tool, re-check charities and curated rows). The approved charities expire 12 months after their review (the first on 25 Nov 2027), so a release with fresh reviews must come before then, or they disappear from every copy.
  7. Check P5-1's names: the install folder `Qapow`, the publisher "John Apolloni", and `AppPublisherURL` (the GitHub page, since there is no qapow.com page for EarthRelief3D).
- **Still open from Plan 4, worth settling before a public release:** the WPI licence (results item 5), ruling R18 (three evaluator method pages), the optional ACAPS note (`docs/permission-requests.md` section 5), and the baseline pictures.

## Global Constraints

- Everything in Plans 2–4's Global Constraints still holds. In particular:
  - every fact on screen shows its source and an "as of" date;
  - no sound (`MessageBoxImage.None` everywhere), no graphic images, no pop-up alarms, no personal data;
  - C6: non-commercial, no-derivatives and share-alike data is fetched live only, never committed or bundled into the installer;
  - C8 colour plus shape;
  - User-Agent `EarthRelief3D/<version> (+https://github.com/johnovanni/EarthRelief3D)`;
  - visual checks only through `--snapshot-scenes` (RenderTargetBitmap), never a screen capture of any kind.
- **Local commits only.** No push, pull request, tag or GitHub release without the owner's OK (owner items 4–5). Commit trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` (subagents keep their own model's, Plan 4 R12).
- **At most 4 app launches**, all in Task 11: the pictures run, one re-run after tuning, the repeat check, one spare.
- **Nothing is installed on the owner's PC** (P5-17), and no Azure command runs here (the owner is signed in to `az`; never run anything that could sign).
- **GlobeCore is not changed.** If a task finds it must change GlobeCore, it stops and reports.
- **Never `MessageBox` or a new window for the cards** (P5-7); cards are in `MainWindow`'s visual tree.
- **Every link opens in the user's browser and is https** (tests check the registry, About and Welcome).
- **Copy rules:** plain words, short sentences, no "click here"; the app's name is "EarthRelief3D"; the help section is "Your one small step".

## Review Focus

1. **Text at 150% in the narrowest window (1,000 × 760).** No card covers another, the panel's text is not cut off at its right edge, and the Settings flyout's text-size control stays reachable. Pinned by `TextScaleLayoutTests.PanelWidthIsCapped` and `.CardsDoNotOverlapAt150PercentIn1000Wide` (Task 3) and the `text-150-narrow` picture (Task 11).
2. **A source that has not loaded, or has failed, says so in the Data card.** "Data not loaded yet" or "not yet checked", never a blank or a stale date shown as current; a failed feed shows its cached date. Pinned by `DataViewModelTests.NotLoadedSaysSo` and `.FailedFeedKeepsCachedDate` (Task 4).
3. **The Welcome never blocks a picture run and shows exactly once.** Closing by Start, by Esc or by opening Settings all mark it shown, once. Pinned by `WelcomeViewModelTests.NotShownInDiagnosticOrOfflineRuns`, `.EveryWayOfClosingMarksItShownOnce` and `.Plan4SettingsFileShowsItOnce` (Task 6).
4. **The updater never runs a file it has not checked, and never leaves the user stuck.** A wrong size, a wrong digest, a different signer, an unsigned running app, a non-GitHub first URL, a cancelled or broken download, or a refused Windows permission each leaves the app running, deletes the file, and says why with "Open the download page". Pinned by `UpdateDownloaderTests.WrongDigestIsDeleted`, `.WrongSizeIsDeleted`, `.DifferentSignerIsRefused`, `.UnsignedAppOffersOnlyTheDownloadPage`, `.CancelDeletesThePartFile`, `UpdateServiceTests.RefusedPermissionKeepsTheAppOpen` (Task 10). (Credits that drift from the docs are pinned in Task 1.)
5. **A release cannot ship unsigned or with the wrong version.** A tag that does not match `<Version>` fails; the files verified are the files signed; pull-request runs never reach Azure. Pinned by `ReleasePackagingTests.TagMustMatchVersion`, `.VerifiedFilesAreTheSignedFiles`, `.PullRequestsDoNotSign` (Task 9).

---

### Task 1: Source registry and the notices file

**Files:**
- Create: `data/sources.json`, `src/EarthRelief3D.Core/Sources/SourceInfo.cs`, `src/EarthRelief3D.Core/Sources/SourceRegistry.cs`, `THIRD-PARTY-NOTICES.txt`, `THIRD-PARTY-NOTICES-DOTNET-RUNTIME.txt`
- Modify: `src/EarthRelief3D.App/EarthRelief3D.App.csproj` (both notices files linked to the output with `CopyToOutputDirectory="PreserveNewest"`, as EarthWatch3D's csproj lines 16–18)
- Test: `tests/EarthRelief3D.Tests/SourceRegistryTests.cs`

**Interfaces:**
- Produces (namespace `EarthRelief3D.Core.Sources`):
  - `public enum SourceUse { Live, Bundled, Curated, LinksOnly }`
  - `public sealed record SourceInfo(string Id, string Name, string What, SourceUse Use, string Licence, Uri? LicenceUrl, string Credit, Uri Page, IReadOnlyList<string> StatusDots, IReadOnlyList<string> DatasetFolders, DateOnly CheckedOn) { public string CreditFor(string? dateText); }` (`CreditFor` replaces `{date}` with `dateText`, or removes `{date}` and its separating comma when null)
  - `public sealed record SourceRegistry(IReadOnlyList<SourceInfo> Sources) { public static SourceRegistry Load(string dataDir); public static IReadOnlyList<string> Validate(SourceRegistry r); public static IReadOnlyList<string> CreditFragments(string credit); public IReadOnlyList<SourceInfo> ForDot(string dotName); }`
  - `SourceRegistry.FileName = "sources.json"`; JSON fields camelCase; `use` as a string (`"live"`, `"bundled"`, `"curated"`, `"linksOnly"`).
- Row ids (one row each, in this order): `ucdp, ipc, unhcr, who-don, who-gho, ocha-hno, faostat, eia, aqueduct, gem-gipt` (power and renewables, one GEM tracker), `gem-oilgas, gem-steel, acaps, fts, ocha-pooled-funds, nga-wpi, ourairports, unhrd, us-senate, us-contacts` (House finder, State Department, UN Blue Book), `logistics-cluster, irs` (Publication 78, revocation, Form 990), `charity-evaluators` (Charity Navigator and BBB Wise Giving Alliance: links only, ratings never copied), `curated` (designations, camps, explainers: each row cites its own documents), `natural-earth, blue-marble`.

- [ ] **Step 1: Write the failing tests** in `SourceRegistryTests`:
  - `.RealFileLoadsAndValidates`: `Validate(Load(<repo>/data))` is empty; 26 rows; ids as listed above, unique.
  - `.CreditsMatchDataSourcesDoc`: for every row, every fragment from `CreditFragments(credit)` appears verbatim in `docs/data-sources.md` (UCDP's fragment `"Uppsala Conflict Data Program (UCDP) Candidate Events Dataset, Hegre et al. 2020"`; FTS's `"Financial Tracking Service (FTS), "` and `", https://fts.unocha.org"`; GEM's verbatim R24 citations).
  - `.CreditsAreInTheNoticesFile`: each row's `Credit` with `{date}` written `<date>` appears verbatim in `THIRD-PARTY-NOTICES.txt`, and so does its `Licence`.
  - `.EveryLinkIsHttps` (`Page` and `LicenceUrl`).
  - `.CreditForFillsOrDropsTheDate`: FTS `CreditFor("25 Nov 2026") == "Financial Tracking Service (FTS), 25 Nov 2026, https://fts.unocha.org"`; `CreditFor(null) == "Financial Tracking Service (FTS), https://fts.unocha.org"`.
  - `.ValidateNamesEachProblem`: a row with an `http` page, an empty credit, or a duplicate id each gives one message naming the row.
  - `.NoticesNameEveryShippedLibrary`: the notices file has a section for HelixToolkit.Wpf 3.1.2, CommunityToolkit.Mvvm 8.4.0, ExcelDataReader 3.7.0, WPF, the .NET runtime, GlobeCore and AstroCore (MIT, Copyright (c) 2026 John M. Apolloni), and names `THIRD-PARTY-NOTICES-DOTNET-RUNTIME.txt`.
  - `.NoticesShipWithTheApp`: both files are in the App build output folder.
- [ ] **Step 2:** Run `dotnet test EarthRelief3D.sln -c Release --filter FullyQualifiedName~SourceRegistryTests`: fails (types missing).
- [ ] **Step 3:** Write `data/sources.json` from `docs/data-sources.md` (each credit copied from its "Attribute" or "The app shows" text, the dates replaced by `{date}`. Rows whose section gives no credit wording today (at least `ocha-pooled-funds`, `unhrd`, `us-contacts`, `irs`, `charity-evaluators`, `curated`, `natural-earth`, `blue-marble`; Blue Marble and Natural Earth from GlobeCore's notices) first get a "Credit" line added to `docs/data-sources.md`, marked "wording chosen by the app; the source prescribes none" where that is so, so the doc stays the one record; `checkedOn` = the date the section was checked; `statusDots` = the dot names in `AppServices.StatusDotNames`, `datasetFolders` = the folders under `data/datasets/`). Write `THIRD-PARTY-NOTICES.txt` in EarthWatch3D's layout: the library sections with their licence texts read from the restored packages' own LICENSE files (ExcelDataReader's from the NuGet cache), GlobeCore's asset sections copied from `external/GlobeCore/THIRD-PARTY-NOTICES.txt`, then one "Data" section per registry row (name, credit, licence and its URL, what the app changed, for CC BY sources the GEM manifests' `changes` text). `THIRD-PARTY-NOTICES-DOTNET-RUNTIME.txt` is `https://raw.githubusercontent.com/dotnet/runtime/release/10.0/THIRD-PARTY-NOTICES.TXT`, unchanged. Implement the two Core types.
- [ ] **Step 4:** Run all tests (`dotnet test EarthRelief3D.sln -c Release`): all pass, 0 warnings.
- [ ] **Step 5:** Commit `Source registry and third-party notices`.

### Task 2: The layer strip closes; the legend folds away (P5-23, P5-24)

**Files:**
- Modify: `src/EarthRelief3D.App/ViewModels/LayerStripViewModel.cs` (`IsExpanded` becomes `IsOpen`: the whole list of groups shows or not), `Views/LayerStrip.xaml` (the "≡ Layers" header gets the group headers' arrow ▾/▸ and the word "closed"; every label always shows with its symbol; tooltip "Show or hide the list of layers"), `ViewModels/LegendViewModel.cs` (`IsOpen`, `ToggleCommand`, `HeaderText`, `UnNote`), `Views/Legend.xaml` (a header row, the body only when open, the UN note always), `Settings/AppSettings.cs` (`bool LegendOpen` default false), `MainWindow.xaml.cs` (`FitLegend`: the open legend at most half the window's height; the strip's `MaxHeight` uses the closed legend's height when it is closed), `Diagnostics/SnapshotScenes.cs` (scene fields `"legend"`: `"open"` (the default, so the earlier pictures still compare) or `"closed"`, and `"strip"`: `"open"` (default) or `"closed"`)
- Test: `LayerStripTests.cs`, `LegendViewModelTests.cs`, `SettingsViewModelTests.cs`, `SnapshotScenesTests.cs`

**Interfaces:**
- Produces: `LayerStripViewModel.IsOpen` (saved as `AppSettings.LayerStripExpanded`, the old name kept so a Plan 2–4 file still reads; `true` = open); `LegendViewModel.IsOpen` (saved as `AppSettings.LegendOpen`), `LegendViewModel.HeaderText` ("Legend (4 layers): colours, sources and dates"; with no layer on, "Legend: no layers shown"), `LegendViewModel.UnNote` (= `BoundaryNote`), `LegendViewModel.ToggleCommand`; `SnapshotScene.LegendOpen` (bool, default true) and `SnapshotScene.StripOpen` (bool, default true).

- [ ] **Step 1: Write the failing tests:**
  - `LayerStripTests.HeaderClosesTheWholeStrip` (`IsOpen` false: no group is shown; true: the groups as before, each with its labels), `.OpenOrClosedIsSaved`, `.OldFileWithLabelsHiddenReadsAsClosed` (the existing test's file with `"LayerStripExpanded": false`: `IsOpen` false), `.LabelsAlwaysShowWhenOpen` (no toggle is ever symbol-only).
  - `LegendViewModelTests.ClosedByDefault` (a fresh settings file: `IsOpen` false), `.HeaderCountsTheLayersShown` (the Plan 2 defaults: "Legend (4 layers): colours, sources and dates"), `.UnNoteShowsWhenClosed` (`UnNote` equals `BoundaryNote` whether open or not), `.OpenOrClosedIsSaved`.
  - `SnapshotScenesTests.LegendAndStripFieldsAreRead` (default open; `"closed"` read; any other value refused with a plain message).
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. The closed legend is the header row (▸, the header text, the word "closed") and the UN note on one or two lines below it, in the card's small grey style; opening it shows today's legend, the full "Borders: …" text at its foot as now. Clicking anywhere on either header opens or closes it; Enter and Space do too (both are buttons). Neither card ever disappears completely: its header stays, so the user can always open it again.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Layer strip closes from its header; legend folds away, closed by default`.

### Task 3: Text size (C8)

**Files:**
- Create: `src/EarthRelief3D.App/Presentation/TextScaleLayout.cs`
- Modify: `Settings/AppSettings.cs` (`double TextScale = 1.0`; `[JsonIgnore] double EffectiveTextScale` = `TextScale` when it is one of `TextScaleLayout.Choices`, else 1.0), `ViewModels/SettingsViewModel.cs` and `Views/SettingsFlyout.xaml` (group "Text size", three radio buttons "100%", "125%", "150%", first in the flyout), `MainWindow.xaml(.cs)` (a `ScaleTransform` `LayoutTransform` on each card bound to `Settings.TextScale`; panel width from `TextScaleLayout.PanelWidth`; `FitExplainer` uses the scaled widths), `Layers/LabelsOverlayLayer.cs` and `Layers/HubLabelsLayer.cs` (font size × scale; redraw on change), `Diagnostics/SnapshotScenes.cs` (scene fields `"textScale"`: 1, 1.25 or 1.5, and `"windowSize"`: `[width, height]`, at least `[1000, 600]`; both validated with a plain message like `panelFocus`)
- Test: `TextScaleLayoutTests.cs`, `SettingsViewModelTests.cs`, `SnapshotScenesTests.cs`

**Interfaces:**
- Produces: `public static class TextScaleLayout { public static IReadOnlyList<double> Choices { get; } // [1.0, 1.25, 1.5]; public const double PanelBaseWidth = 420; public static double PanelWidth(double windowWidth, double scale); public static Rect[] CardBounds(Size window, double scale, bool panelOpen, bool explainerOpen); }` (`CardBounds` returns the on-screen rectangles of strip, legend, panel, explainer and Sources card, in that order, from the same numbers the XAML uses); `SettingsViewModel.TextScale` (double, observable) and `SettingsViewModel.TextScaleChanged` event; `SettingsViewModel.TextSizeGroup = "Text size"`; `SnapshotScene.TextScale` (double, default 1.0) and `SnapshotScene.WindowSize` (`Size?`).

- [ ] **Step 1: Write the failing tests:**
  - `TextScaleLayoutTests.PanelWidthIsCapped`: `PanelWidth(1200, 1.0) == 420`; `PanelWidth(1200, 1.5) == 540` (0.45 × 1200); `PanelWidth(1000, 1.25) == 450`.
  - `.CardsDoNotOverlapAt150PercentIn1000Wide`: for scale 1.0, 1.25 and 1.5, window 1000 × 760 and 1200 × 760, panel and explainer open: no two of `CardBounds` intersect, and every rectangle lies inside the window.
  - `SettingsViewModelTests.TextScaleSavedAndOtherSettingsKept`, `.UnknownTextScaleReadsAsDefault` (a file with `"TextScale": 2.0` reads 1.0), `.Plan4FileReadsAs100Percent`.
  - `SnapshotScenesTests.TextScaleAndWindowSizeAreRead`, `.BadTextScaleIsRefused` (`1.3` → a message naming the allowed values), `.TooSmallWindowIsRefused`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. A scene with `textScale` sets the scale for its picture only and saves nothing (as `showDrafts`); `windowSize` sets the window's size before the picture and restores it after. When `CardBounds` forces the legend narrower than its content, the legend wraps and scrolls (it already has a `ScrollViewer`).
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Text size setting (C8)`.

### Task 4: The Data card

**Files:**
- Create: `src/EarthRelief3D.App/Presentation/SourceAsOf.cs`, `ViewModels/DataViewModel.cs`, `ViewModels/CardHost.cs`, `Views/DataCard.xaml(.cs)`
- Modify: `ViewModels/MainViewModel.cs` (`Registry`, `Cards`, `Data`), `ViewModels/LegendViewModel.cs` (move the per-source date text it builds into `SourceAsOf` and call it from there, so the legend and the Data card share one function), `MainWindow.xaml(.cs)` (the card host; a "Data" text button at the left of the Sources card; Esc closes the open card first), `Services/AppServices.cs` (load the registry; on error log it and use an empty registry, and the Data card says "The source list could not be read; see the log"), `Diagnostics/SnapshotScenes.cs` (scene field `"card"`: `"welcome"`, `"about"`, `"data"` or `"settings"`)
- Test: `DataViewModelTests.cs`, `SourceCoverageTests.cs`, `SnapshotScenesTests.cs`

**Interfaces:**
- Consumes: Task 1's `SourceRegistry`, `SourceInfo.CreditFor`; Task 3's scaled card style.
- Produces:
  - `public enum AppCard { None, Welcome, About, Data }`; `public sealed partial class CardHost : ObservableObject { public AppCard Open { get; } public void Show(AppCard card); public bool CloseTop(); }` (`CloseTop` returns false when nothing was open; Task 5 and Task 6 add their view models to it).
  - `public sealed record SourceAsOfInputs(CrisisSnapshot Crises, IndicatorSnapshot Indicators, IReadOnlyDictionary<string, SourceCitation> Datasets, CuratedStore Curated, HelpTables Help, BoundarySet? Borders);`
  - `public static class SourceAsOf { public static string For(SourceInfo s, SourceAsOfInputs i, DateTime nowUtc); }`
  - `public sealed record DataRow(string Name, string What, string Use, string AsOf, string Licence, Uri? LicenceUrl, string Credit, Uri Page);`
  - `DataViewModel(SourceRegistry, Func<SourceAsOfInputs>, Func<DateTime> nowUtc, ILinkOpener)`: `Rows`, `Refresh()`, `OpenLinkCommand`, `Message`, `Heading = "Data sources"`, `Intro`.
  - `SnapshotScene.Card` (`string?`: `"welcome"`, `"about"` and `"data"` map to `AppCard`; `"settings"` opens the Settings flyout instead).
- Use labels: Live "fetched by the app", Bundled "included with the app", Curated "written for the app, each entry cited", LinksOnly "links only".
- As-of rules (`SourceAsOf.For`): Live, the legend's date text for that source plus " · checked <date>" when the snapshot has a check time, or "not yet checked"; Bundled, the manifest citation's date as the legend shows it ("September 2026 release (file V2)"; WPI "as of 5 Nov 2026"); Curated, "each entry gives its own date; last reviewed <newest approvedOn>"; LinksOnly, "links only; checked <CheckedOn>"; Borders "Natural Earth 5.1.2 (2022-05-13)" from `BoundarySet.Describe()`.

- [ ] **Step 1: Write the failing tests:**
  - `DataViewModelTests.OneRowPerSource` (26 rows, registry order), `.LiveRowMatchesTheLegend` (UCDP's `AsOf` starts with the legend's UCDP date text for the same snapshot), `.BundledRowUsesTheManifest` (FAOSTAT and GEM GIPT as above), `.NotLoadedSaysSo` (an empty indicator snapshot: ACAPS and FTS read "not yet checked"; a dataset folder that did not load reads "Data not loaded yet"), `.FailedFeedKeepsCachedDate` (a cached UCDP snapshot with a check time two days old and a failed last run: the cached data date and "checked <that date>"), `.CuratedRowGivesNewestReview` (`"each entry gives its own date; last reviewed 25 Nov 2026"` with the real curated files), `.CreditCarriesItsDate` (FTS row's `Credit` holds the resource date), `.LinkThatFailsToOpenShowsAMessage`.
  - `SourceCoverageTests.EveryStatusDotHasASource` (every name in `AppServices.StatusDotNames` is in some row's `StatusDots`), `.EveryBundledDatasetHasASource` (every folder under `data/datasets/` is in some row's `DatasetFolders`).
  - `CardHostTests` (in `DataViewModelTests.cs`): `.OneCardAtATime`, `.EscClosesTheCardBeforeTheExplainer` (`CloseTop` true with a card open, false with none).
  - `SnapshotScenesTests.CardFieldIsRead`, `.UnknownCardIsRefused`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. The card is centred over the globe, at most 900 × 640 at 100% (scaled with Task 3's transform, capped to the window less 24 px), a table with columns Source (the name, linked to `Page`), What it gives, How, As of, Licence (linked when `LicenceUrl`), and the credit under each row in the small grey style; the intro reads "Every fact in EarthRelief3D comes from one of these sources. Each line on the map and in the panel names its source and date." The rows refresh when the crisis or indicator store changes while the card is open.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Data card: every source with its date, licence and link`.

### Task 5: The About card

**Files:**
- Create: `ViewModels/AboutViewModel.cs`, `Views/AboutCard.xaml(.cs)`
- Modify: `Views/LayerStrip.xaml` (an "About" command button "ⓘ About" under Settings), `MainViewModel` (`About`), `CardHost`
- Test: `AboutViewModelTests.cs`

**Interfaces:**
- Consumes: Task 1's registry, Task 4's `CardHost` and `AppCard.Data`.
- Produces: `public sealed record Credit(string Name, string Text, Uri Url);` `AboutViewModel(SourceRegistry registry, ILinkOpener opener, string dataFolder, CardHost cards)`: `Version` ("Version 1.0.0", from `AppInfo.Version`), `Credits` (one per registry row: name, `CreditFor(null)`, page), `Libraries` (HelixToolkit.Wpf, CommunityToolkit.Mvvm, ExcelDataReader, WPF, .NET: name, "MIT licence", project URL), `AppLicence = "EarthRelief3D is free software under the MIT licence. Copyright (c) 2026 John M. Apolloni."`, `BandLines` (C7, P5-10), `BoundaryNote` (= `LegendViewModel.BoundaryNote`), `Principles`, `DataFolder`, `ReleasesUrl` (P5-2), commands `OpenLink`, `OpenNotices` (the installed `THIRD-PARTY-NOTICES.txt`), `OpenDataFolder`, `ShowData` (`cards.Show(AppCard.Data)`), `ShowWelcome` (Task 6), `Message`.
- Copy:
  - `Principles`: "Every fact on screen shows where it comes from and how recent it is. EarthRelief3D never handles money, never ranks charities by its own opinion, shows no graphic images, makes no sound and collects no personal data."
  - The data paragraph: "Your settings and the downloaded data are kept in <DataFolder>. The app sends requests only to the public sources listed under Data, and once a day to GitHub to look for a new version (you can turn that off in Settings)."
  - `BandLines` (one per kind, numbers from `SeverityBands`): conflict "Armed conflict (UCDP deaths in one country in the data window): band 1 under 1,000 (for a conflict that qualifies), 2 from 1,000, 3 from 10,000, 4 from 50,000"; food "Food (IPC): the band is the IPC phase, 3 to 5"; outbreaks "Disease outbreaks (WHO): every report is band 3"; then the line "Bands only set a marker's size; the source's own figure is always shown with it." and a link "How markers are sized" to `https://github.com/johnovanni/EarthRelief3D/blob/main/docs/severity-bands.md`. Read `SeverityBands` and `docs/severity-bands.md` first and make the lines agree with both for every kind it covers.

- [ ] **Step 1: Write the failing tests** (`AboutViewModelTests`, as EarthWatch3D's): `.CreditsComeFromTheRegistry` (one per row, same order, `Text == CreditFor(null)`), `.LibrariesListTheShippedPackages` (names and versions equal the csproj `PackageReference`s of the App and Core projects), `.ShowsTheVersionAndFolder`, `.BandLinesFollowSeverityBands` (the conflict line contains each boundary of `SeverityBands.ForConflict`, written with thousands separators), `.BoundaryNoteIsTheLegendsNote`, `.ShowDataOpensTheDataCard`, `.NoticesOpenThroughTheOpenerAndFailureShowsAMessage`, `.EveryLinkIsHttps`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. Layout, top to bottom: title "About EarthRelief3D", version, `AppLicence` with "Licence texts" (opens the notices) and "Releases on GitHub", `Principles`, "Data sources" (the credits list, each name a link, then a "See dates and licences" button that opens the Data card), "How markers are sized" (`BandLines`), the boundary note, "Software" (`Libraries`), the data paragraph with "Open the data folder", and "Show the welcome again". The card scrolls.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `About card: credits, licences, severity bands, boundary note`.

### Task 6: The Welcome card

**Files:**
- Create: `ViewModels/WelcomeViewModel.cs`, `Views/WelcomeCard.xaml(.cs)`
- Modify: `Settings/AppSettings.cs` (`bool WelcomeShown`), `App.xaml.cs` (after the window is first rendered: `if (WelcomeViewModel.ShouldShow(settings, options)) cards.Show(AppCard.Welcome)`), `MainViewModel` (`Welcome`), `AboutViewModel.ShowWelcome`, `CardHost`
- Test: `WelcomeViewModelTests.cs`

**Interfaces:**
- Consumes: Task 3's `SettingsViewModel.TextScale`, Task 4's `CardHost`, `LayerStyle` and `LayerCatalog` (symbols, colours, labels).
- Produces: `public sealed record WelcomeKey(string Symbol, Color Color, string Text);` `WelcomeViewModel(ISettingsStore<AppSettings> store, SettingsViewModel settings, CardHost cards)`: `static bool ShouldShow(AppSettings s, LaunchOptions o) => !s.WelcomeShown && !o.IsDiagnosticRun`, `Title`, `Paragraphs`, `Keys` (the default-on layers in strip order: conflict, food (one key per IPC Phase 3, 4, 5 in IPC's colours), outbreaks, displacement, then the hub lines), `TextScale` (two-way to `settings.TextScale`), commands `Start`, `OpenSettings` (closes the card, opens the Settings flyout), `Close()` (marks shown once).
- **Draft copy (owner to check):**
  - Title: "Welcome to EarthRelief3D"
  - "This globe shows where people are suffering, what they lack, why help is hard to get in, and one small thing you can do about it."
  - "Every fact comes with its source and its date. The legend at the bottom left (open it from its header) names them for each layer, the dots at the bottom right show whether each source is up to date, and Data lists every source."
  - "What the marks mean" (then `Keys`), followed by "Shapes as well as colours tell the layers apart. More layers are in the strip on the left; only one shaded layer shows at a time."
  - "Click a country to open its panel: what is happening there, how hard it is for help to get in, and Your one small step: vetted ways to give, and official people to contact."
  - "Text size" with the three choices.
  - "If you like, choose your state in Settings so the panel can list your two senators. EarthRelief3D never asks for your address."
  - Buttons "Start" and "Settings".

- [ ] **Step 1: Write the failing tests:** `WelcomeViewModelTests.ShownOnlyOnFirstRun`, `.NotShownInDiagnosticOrOfflineRuns` (`--offline-samples x`, `--snapshot a.png`, `--snapshot-scenes s.json --snapshot-out o`), `.EveryWayOfClosingMarksItShownOnce` (Start, Esc through `CardHost.CloseTop`, OpenSettings: each saves `WelcomeShown` once and keeps every other setting), `.Plan4SettingsFileShowsItOnce` (a file without the flag), `.KeysFollowLayerStyle` (each key's symbol and colour equal `LayerStyle`'s for its layer; the food keys equal the IPC phase colours), `.TextSizeChoiceChangesTheSetting`, `.OpenSettingsOpensTheFlyout`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. The card is centred, at most 620 px wide at 100%, scaled like the others. The flag is saved through the debounced saver.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Welcome card on first start`.

### Task 7: FTS appeals in January (m6, m13)

**Files:**
- Modify: `src/EarthRelief3D.Core/Feeds/FtsFundingFeed.cs`, `src/EarthRelief3D.App/ViewModels/CountryPanelViewModel.cs` (`FundingSections`), `ViewModels/LegendViewModel.cs` (the FTS legend note)
- Test: `FtsFundingFeedTests.cs`, `FundingPanelTests.cs`, `LegendViewModelTests.cs`

**Interfaces:**
- Produces: `FtsFundingFeed.InNewYearWindow(DateTime nowUtc) -> bool` (1 to 31 January, UTC); `FtsFundingFeed.NewYearNote = "In January, a country with no appeal for the new year yet shows last year's."`; `CountryPanelViewModel.NoAppealYet(int year) -> string` ("No appeal in FTS for 2027 yet").

- [ ] **Step 1: Write the failing tests** (CSV text built in the test with FTS's 12 columns):
  - `FtsFundingFeedTests.NewYearKeepsLastYearForCountriesWithoutNewPlans`: clock 2027-01-15; AFG has a 2027 plan and a 2026 plan, SDN only 2026 plans: AFG's appeals are the 2027 plan only, SDN's its 2026 plans.
  - `.FallbackEndsAfterJanuary`: clock 2027-02-01, same rows: SDN has none, AFG the 2027 plan.
  - `.NoNewYearRowsAtAllStillFallsBack`: clock 2027-03-10, no 2027 rows: the 2026 rows (the existing `EarlyJanuaryFallsBackToLastYear` stays and passes).
  - `.RegionalWithinItsOwnYear`: a 2026 plan id under three countries stays regional for the two of them that have no 2027 plan.
  - `FundingPanelTests.LastYearShownSaysNoAppealYet`: SDN in the first case: heading "Humanitarian appeals 2026", first row `("Appeal", "No appeal in FTS for 2027 yet")`, then its 2026 plans.
  - `LegendViewModelTests.FtsLineHasTheNewYearNote` during January only.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement P5-12. The feed's log line names how many countries kept last year's plans.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `FTS: last year's appeals only for countries without one yet, in January (m6, m13)`.

### Task 8: Hub fixes: the port arc and the WPI note (m18)

**Files:**
- Modify: `src/EarthRelief3D.App/Layers/HubArcLayer.cs`, `Layers/HubLabelsLayer.cs` (label points from the lifted path), `tools/EarthRelief3D.DataTool/HubExtractor.cs` and the verb's argument parsing, `docs/data-sources.md` §15 (the `--portal-note` command line)
- Test: `HubArcGeometryTests.cs`, `DataToolHubsTests.cs`

**Interfaces:**
- Produces: `HubArcLayer.LiftFactor(HubKind kind) -> double` (port 1.5, airport 1.25, depot 1.0); `HubArcLayer.ArcPoints(SurfacePoint from, SurfacePoint to, double liftFactor = 1.0)` with `h = min(MaxLift × liftFactor, LiftPerRadian × angle × liftFactor)`; `HubExtractor.Ports(…, string? portalNote)`; DataTool `ports <file> --downloaded <yyyy-MM-dd> [--portal-note "<text>"]`.

- [ ] **Step 1: Write the failing tests:**
  - `HubArcGeometryTests.KindsLiftedDifferently` (same two points: the port path's middle point is 1.5 times as far above the surface as the depot path's, the airport's 1.25 times, below the caps).
  - `.PortArcAboveDepotArcFromSudan`: from Sudan's label point to Port Sudan (19.6° N, 37.2° E) and to the Dubai depot's position (row `unhrd-dubai` in `data/curated/unhrd-depots.json`, 24.896° N, 55.162° E): over the first half of the port path, each point's distance from the centre exceeds that of the depot path's point at the same central angle from the start.
  - The existing `.EndsAtBothPoints`, `.DateLineArcIsContinuous` and `.LiftedInTheMiddle` keep passing with factor 1.
  - `DataToolHubsTests.PortalNoteIsWrittenAsGiven` (`--portal-note "msi.nga.mil answered 503 on 2027-05-02"`: the licence note contains that text and no other date), `.NoPortalNoteSaysNotRechecked` (the note says the portal was not re-checked for this extract and contains no date).
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement P5-13 and P5-14. Do not rebuild `data/datasets/wpi-ports/`.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Hub arcs lifted by kind; WPI licence note from the command line (m18)`.

### Task 9: Icon, installer and release workflow

**Files:**
- Create: `tools/make-icon.py` (P5-16), `src/EarthRelief3D.App/app.ico`, `packaging/installer/EarthRelief3D.iss`, `.github/workflows/release.yml`, `docs/release.md`
- Modify: `src/EarthRelief3D.App/EarthRelief3D.App.csproj` (`<ApplicationIcon>app.ico</ApplicationIcon>`; the icon as a `Resource`), `MainWindow.xaml` (`Icon`), `Directory.Build.props` (`<Version>1.0.0</Version>`, comment "also used for the installer"), `.gitignore` (remove the two Plan 4 MSIX signing lines only if nothing uses them; keep `.downloads/`), `README.md` (an "Install" section: download `EarthRelief3D-Setup.exe` from the latest release; what it installs and where the data folder is)
- Test: `tests/EarthRelief3D.Tests/ReleasePackagingTests.cs`

**Interfaces:**
- The `.iss` is EarthWatch3D's with: `AppName EarthRelief3D`, `AppExe EarthRelief3D.App.exe`, a **new** `AppId` GUID generated once in this task (comment "Never change it"), `AppPublisher=John Apolloni`, `AppPublisherURL` and `AppSupportURL` = `https://github.com/johnovanni/EarthRelief3D`, `DefaultDirName={autopf}\Qapow\EarthRelief3D`, `SetupIconFile=..\..\src\EarthRelief3D.App\app.ico`, `OutputBaseFilename=EarthRelief3D-Setup`; everything else as EarthWatch3D's.
- The workflow is EarthWatch3D's with: `SETUP: EarthRelief3D-Setup.exe`; P5-3's checkout line and comment (GlobeCore public; the secret only if it turns private); pull-request paths `.github/workflows/release.yml`, `packaging/**`, `src/**`; a `Test` step `dotnet test EarthRelief3D.sln -c Release` after setup-dotnet (P5-5); publish `src/EarthRelief3D.App`; P5-4's signed file list in both the sign and the verify steps; the header comment with P5-4's OIDC subject.
- `docs/release.md`: a "Before each release" data checklist (re-run the DataTool extracts from fresh downloads; run the vetting tool and act on its report; re-review charities and curated rows near their 12-month expiry, the first on 25 Nov 2027; update `docs/data-sources.md` and `data/sources.json`), how to release (raise `<Version>`, merge, tag `v<version>`), the owner's one-time GitHub and Azure set-up (owner items 1–3), the SUBMODULES_TOKEN note (P5-3), what is signed, and the manual install checklist (install with the default folder; Start menu entry "Qapow > EarthRelief3D"; first start shows the Welcome; About shows "Version 1.0.0" and opens the notices; a second install over the first keeps settings; uninstall removes `C:\Program Files\Qapow\EarthRelief3D` and leaves `%LOCALAPPDATA%\EarthRelief3D`).

- [ ] **Step 1: Write the failing tests** (`ReleasePackagingTests`, reading the files from the repository root as text):
  - `.TagMustMatchVersion` (the workflow's "Read version" step compares `github.ref_name` with `v<Version>` and throws).
  - `.VerifiedFilesAreTheSignedFiles` (the set of `publish\…` files in "Sign app files" equals the set in "Verify signatures", and equals P5-4's seven names; the installer is signed and verified too).
  - `.PullRequestsDoNotSign` (every Azure step has `if: env.SIGN == 'true'`, and `SIGN` is false for `pull_request`; the environment is empty for pull requests).
  - `.CheckoutFallsBackToTheBuiltInToken` (`secrets.SUBMODULES_TOKEN || github.token`, `submodules: recursive`).
  - `.TestsRunBeforePublish`.
  - `.InstallerNamesTheBuiltExe` (`AppExe` equals the App project's assembly name plus `.exe`; `OutputBaseFilename` + `.exe` equals the workflow's `SETUP`; `AppId` is a GUID different from EarthWatch3D's `F5ED128E-15B1-403B-9C11-F81F253842FE`).
  - `.IconHasEverySize` (`app.ico` holds 16, 24, 32, 48, 64, 128 and 256 px images).
  - `.VersionIsOneZeroZero`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Write the files. Run `python tools/make-icon.py` (from the repository root). Then `dotnet publish src/EarthRelief3D.App -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -o artifacts\publish` and check the folder holds `EarthRelief3D.App.exe`, the seven DLLs of P5-4, `data\` (with `sources.json`, `curated\`, `datasets\`, `countries\US\`), both notices files, and nothing from `tests\` or `.downloads\`; record its size.
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Icon, signed installer and release workflow (version 1.0.0)`.

### Task 10: Update check, download and install (P5-2, P5-18 to P5-22)

**Files:**
- Create in `src/EarthRelief3D.Core/Updates/`: `ReleaseInfo.cs`, `UpdateCheck.cs`
- Create in `src/EarthRelief3D.App/`: `Services/UpdateService.cs`, `Services/UpdateDownloader.cs`, `Services/WinTrustSignatureCheck.cs` (P/Invoke `WinVerifyTrust` in `wintrust.dll` with `WINTRUST_ACTION_GENERIC_VERIFY_V2`, plus `X509Certificate.CreateFromSignedFile` for the subject), `Services/ShellInstallerLauncher.cs`, `ViewModels/UpdateBannerViewModel.cs`, `Views/UpdateBanner.xaml(.cs)`
- Create samples (synthetic, README entry "synthetic, GitHub's release JSON layout"): `tests/Samples/github_release_latest_newer.json` (tag `v1.1.0`, published 2027-02-03, both assets, a `digest`), `github_release_latest_same.json` (tag `v1.0.0`)
- Modify: `Settings/AppSettings.cs` (`bool CheckForUpdates = true`, `DateTime? LastUpdateCheckOkUtc`, `DateTime? FirstUpdateCheckUtc`), `SettingsViewModel` and `SettingsFlyout.xaml` (group "Updates", checkbox "Check for a new version once a day"), `AboutViewModel` and `AboutCard.xaml` (the status line and "Check now"), `MainViewModel` (`Updates`), `MainWindow.xaml` (the banner at the top centre, above the cards' dimming), `App.xaml.cs` (create and start the service unless `options.IsDiagnosticRun`), `Diagnostics/SnapshotScenes.cs` (scene field `"update"`: `"offer"`, `"downloading"`, `"ready"`, `"failed"` or `"stale"`, filled from `github_release_latest_newer.json`, no network), `packaging/installer/EarthRelief3D.iss` (P5-21's second `[Run]` entry), `docs/release.md` (how the updater finds a release; never delete or rename the two assets of a published release)
- Test: `UpdateCheckTests.cs`, `UpdateDownloaderTests.cs`, `UpdateServiceTests.cs`, `WinTrustSignatureCheckTests.cs`, `ReleasePackagingTests.cs`, `SettingsViewModelTests.cs`, `AboutViewModelTests.cs`, `SnapshotScenesTests.cs`

**Interfaces:**
- Consumes: Task 5's `AboutViewModel`, Task 3's scaled card style, Task 9's `.iss` and asset names.
- Produces (namespace `EarthRelief3D.Core.Updates`):
  - `public sealed record ReleaseAsset(string Name, long Size, Uri DownloadUrl, string? Sha256);` (`Sha256` lower-case hex from `digest` "sha256:…", or null)
  - `public sealed record ReleaseInfo(Version Version, string Tag, DateTime PublishedUtc, Uri Page, bool Draft, bool Prerelease, ReleaseAsset? Setup, ReleaseAsset? Checksum);`
  - `public static class UpdateCheck { public const string LatestUrl = "https://api.github.com/repos/johnovanni/EarthRelief3D/releases/latest"; public const string DownloadPrefix = "https://github.com/johnovanni/EarthRelief3D/releases/download/"; public const string SetupName = "EarthRelief3D-Setup.exe"; public const long MaxSetupBytes = 314_572_800; public static ReleaseInfo Parse(string json); public static bool IsNewer(ReleaseInfo r, Version running); public static string? RefuseReason(ReleaseInfo r); }` (`Parse` throws `FormatException` for JSON it cannot read; `RefuseReason` is null when the release may be downloaded, else a plain sentence)
- Produces (App):
  - `public enum UpdateBannerState { Hidden, Offer, Downloading, Ready, Failed, Stale }`
  - `public interface ISignatureCheck { string? SignerSubject(string path); bool IsTrusted(string path); }` (`SignerSubject` null for an unsigned file)
  - `public interface IInstallerLauncher { bool TryStart(string path, string arguments, out string? error); }` (`ShellInstallerLauncher`: `UseShellExecute`, verb `runas`; `Win32Exception` 1223 → false with P5-21's sentence)
  - `public sealed record DownloadResult(string? SetupPath, string? Failure);` `UpdateDownloader(HttpMessageHandler handler, ISignatureCheck signatures, string updatesDir)`: `Task<DownloadResult> DownloadAsync(ReleaseInfo r, string runningExePath, IProgress<long> progress, CancellationToken ct)`, `void DeleteOldFolders(Version running)`
  - `UpdateService(Func<CancellationToken, Task<string>> fetchLatestJson, UpdateDownloader downloader, IInstallerLauncher launcher, ISettingsStore<AppSettings> settings, IClock clock, IAppLog log, Version running, string runningExePath, Action shutdown)`: `Start()`, `Task CheckNowAsync()`, `Banner` (`UpdateBannerViewModel`), `StatusText` (for About), `public const string InstallerArguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"`, `FirstCheckDelay = 30 s`, `Interval = 24 h`, `StaleAfter = 30 days`
  - `UpdateBannerViewModel`: `State`, `Text`, `ProgressText` ("12 of 53 MB"), commands `DownloadAndInstall`, `WhatsNew`, `Later`, `Cancel`, `InstallNow`, `OpenDownloadPage`, `CheckNow`; `ShowForPicture(ReleaseInfo r, UpdateBannerState s)` for scenes.
- Banner copy: Offer and Ready as P5-19 and P5-21; Downloading "Downloading EarthRelief3D <v>: <n> of <m> MB" with Cancel; Failed "The update could not be used: <reason> You can download it from its release page instead." with "Open the download page" and "Later"; Stale as P5-22. About's status line: "Checked for a new version on 25 Nov 2026: you have the newest (1.0.0)", or "… 1.1.0 is available", or "Could not check for a new version on <date>: <reason>", or "Checking for new versions is off (Settings)".

- [ ] **Step 1: Write the failing tests:**
  - `UpdateCheckTests.ParsesGithubRelease` (the newer sample: version 1.1.0, tag "v1.1.0", published 3 Feb 2027, page, setup size and SHA-256), `.NewerByVersionNotByDate` (1.0.10 is newer than 1.0.9; 1.0.0 is not newer than 1.0.0; 0.9.0 is not newer than 1.0.0), `.RefusesBadTags` ("1.1.0", "v1.1", "v1.1.0-beta"), `.RefusesDraftOrPrerelease`, `.RefusesForeignOrPlainHttpUrl` (an asset of another repository; `http://`), `.RefusesOversizeOrEmpty`, `.MissingSetupAssetIsRefused` (the banner still offers the release page), `.BadDigestFormatIsRefused`, `.UnreadableJsonThrowsFormatException`.
  - `UpdateDownloaderTests` (a fake handler that serves bytes, redirects once to another https host, or fails half-way; a fake `ISignatureCheck`): `.GoodDownloadIsKept`, `.WrongDigestIsDeleted`, `.WrongSizeIsDeleted`, `.DifferentSignerIsRefused`, `.UntrustedSignatureIsRefused`, `.UnsignedAppOffersOnlyTheDownloadPage` (the running exe has no signer: no request is made), `.CancelDeletesThePartFile`, `.BrokenConnectionDeletesThePartFile`, `.ChecksumAssetUsedWhenNoDigest`, `.OldUpdateFoldersDeletedAtStart`. Every failure's `Failure` is a sentence that names what did not match.
  - `UpdateServiceTests` (settable clock, fake fetch and launcher): `.FirstCheckThirtySecondsAfterStart`, `.ChecksDaily`, `.OffInSettingsMeansNoRequest`, `.SameVersionShowsNothing`, `.LaterHidesUntilNextStart`, `.LaterThenNewerVersionShowsAgain`, `.FailureIsShownInAboutAndRetriedNextDay`, `.StaleAfterThirtyDaysWithoutACheck` (`LastUpdateCheckOkUtc` 31 days ago and the check fails: Stale; 29 days: Hidden; never succeeded, first attempt 31 days ago: Stale; checking turned off 31 days ago: Stale), `.SuccessfulCheckSavesTheTime`, `.InstallStartsTheInstallerThenShutsDown` (the launcher gets the file and `InstallerArguments`, then `shutdown` runs, in that order), `.RefusedPermissionKeepsTheAppOpen` (no shutdown; Failed with P5-21's sentence).
  - `WinTrustSignatureCheckTests.UnsignedFileHasNoSigner` (a copy of the test assembly), `.MicrosoftSignedFileIsTrusted` (the `dotnet.exe` running the tests, from `Environment.ProcessPath` or `DOTNET_ROOT`: `IsTrusted` true and the subject contains "Microsoft Corporation").
  - `ReleasePackagingTests.SilentUpdateRestartsTheAppAsTheUser` (a `[Run]` line for `{app}\EarthRelief3D.App.exe` with `nowait`, `runasoriginaluser` and `skipifnotsilent`; the first `[Run]` line keeps `skipifsilent`).
  - `SettingsViewModelTests.UpdatesOnByDefaultAndSaved`; `AboutViewModelTests.ShowsTheUpdateStatus`; `SnapshotScenesTests.UpdateFieldIsRead`, `.UnknownUpdateStateIsRefused`.
- [ ] **Step 2:** Run them: they fail.
- [ ] **Step 3:** Implement. Requests carry `AppInfo.UserAgent` and `Accept: application/vnd.github+json`; a 403 or 429 from GitHub (rate limit) counts as a failed check. The banner is not focusable on its own (no keyboard trap) and never covers the layer strip or the panel's header. Nothing is downloaded before the user chooses "Download and install".
- [ ] **Step 4:** Run all tests: all pass, 0 warnings.
- [ ] **Step 5:** Commit `Update check: offer, download, verify and install new versions`.

### Task 11: Pictures, docs and final check

**Files:**
- Modify: `tests/SnapshotScenes/scenes.json`, `src/EarthRelief3D.App/ViewModels/CountryPanelViewModel.cs` and `Views/CountryPanel.xaml.cs` (`PanelFocus.Charities`, P5-15), `Diagnostics/SnapshotScenes.cs` (`panelFocus` `"charities"`), the spec (a "Rulings made while building Plan 5" table: P5-1 to P5-24 one line each, then the ledger's R numbers, as Plan 4's), `docs/data-sources.md` (a short "Where the credits live" note pointing at `data/sources.json`), `README.md` (status line)
- Create: `docs/plan-5-results.md`
- Test: `HelpPanelTests.cs` (`.CharitiesFocusPointsAtTheFirstCharity`), `SnapshotScenesTests.cs`

**Interfaces:**
- Produces: `PanelFocus.Charities` (appended); `CountryPanelViewModel.FocusedEntryIndex` (the index, in the focused section's `Entries`, of the first charity entry; -1 otherwise); the view scrolls that entry's top to the top of the panel's view.
- New scenes (all at 2026-11-22T12:00:00Z):
  - `first-start`: the app's defaults as a new user sees them behind the Welcome: Plan 2 layers, look at 10,25, `legend` closed, `card` welcome.
  - `about`: Plan 2 layers, look at 10,25, `card` about.
  - `data`: the same, `card` data.
  - `text-150`: Plan 2 layers, panel SDN, `textScale` 1.5, `card` settings.
  - `text-150-narrow`: as `text-150` with `windowSize` [1000, 760] and no card (Review Focus 1).
  - `sudan-charities`: Plan 2 layers, panel SDN, `panelFocus` charities.
  - `closed-cards`: Plan 2 layers, `strip` closed, `legend` closed (how little room the two take).
  - `update-offer`, `update-ready` and `update-stale`: Plan 2 layers, look at 10,25, `update` offer, ready and stale (synthetic release 1.1.0).

- [ ] **Step 1: Write the failing test** `HelpPanelTests.CharitiesFocusPointsAtTheFirstCharity` (SDN with the real help tables: `FocusedEntryIndex` is the first entry after the pooled fund) and `SnapshotScenesTests.CharitiesFocusIsAccepted`; run them: they fail; implement; run all tests: all pass, 0 warnings; record the count.
- [ ] **Step 2: Launch 1** (the published exe, P5-17): `artifacts\publish\EarthRelief3D.App.exe --offline-samples tests\Samples --snapshot-scenes tests\SnapshotScenes\scenes.json --snapshot-out artifacts\snapshot-plan5` (first `dotnet publish` again as in Task 9 Step 3). Look at every picture and write down what each shows: the Welcome's keys against the map's markers; the About's credits, bands and boundary note; the Data card's dates (compare three against the legend); 150% text with nothing overlapping or cut off, in both window sizes; Sudan's first charity entry (its lines, links, "Reviewed" date); the `sudan-help` port arc against the depot arc; the three update banners (wording, buttons, nothing covered that matters); `first-start` and `closed-cards` (the closed legend one header row plus the UN note; the closed strip one header). Compare the fourteen earlier scenes with `artifacts\snapshot-plan4-fix` with PngDiff and explain every difference (expected: the strip's header arrow, the legend's new header row, the Sources card's "Data" button, the strip's About button, the sudan-help arcs, the window icon if drawn).
- [ ] **Step 3:** Tune what reads badly (P5-14's factors at most for the arcs) with **one** re-run into `artifacts\snapshot-plan5-2`, then **the repeat run** into `artifacts\snapshot-plan5-b`: PngDiff reports every scene identical. The fourth launch is the spare.
- [ ] **Step 4:** Write `docs/plan-5-results.md` as Plan 4's (tests, launches, what each picture shows, what was tuned, what was not verified: the installer build, signing, installing and uninstalling, a real update from one release to the next, Windows' permission prompt, the hover tooltip's size; owner items). Add the rulings table to the spec. Do not copy over the baseline pictures. Commit `Plan 5: pictures, docs and results`.
