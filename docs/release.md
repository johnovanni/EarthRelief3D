# Releasing EarthRelief3D

Releases ship as one signed installer, `EarthRelief3D-Setup.exe` (Inno Setup script
`packaging/installer/EarthRelief3D.iss`), built, tested and signed by `.github/workflows/release.yml` when a version tag is
pushed. It installs to `C:\Program Files\Qapow\EarthRelief3D` by default (the user may pick another folder). Settings,
cache and logs stay in `%LOCALAPPDATA%\EarthRelief3D`. The installer is not built on the development PC; only GitHub builds it.

## Before each release: the data checklist

- [ ] Re-run the DataTool extracts (`fao`, `eia`, `aqueduct`, and the others in the README's Data section) from **fresh downloads**.
- [ ] Run the vetting tool and act on its report (it never edits the list itself).
- [ ] Re-review the charities and the curated rows that are near their 12-month expiry. The first is due on **25 Nov 2027**.
- [ ] Update `docs/data-sources.md` and `data/sources.json` (versions, "as of" dates, credits).

## How to release

1. Raise `<Version>` in `Directory.Build.props` (for example `1.0.0` to `1.0.1`). It is the only place the version is set.
2. Merge to `main`.
3. Tag `v<version>` (for example `v1.0.1`) and push the tag. The workflow's "Read version" step fails if the tag and `<Version>` differ.

The workflow then runs `dotnet test EarthRelief3D.sln -c Release`, publishes the app (self-contained, win-x64), signs it,
builds and signs the installer, checks every signature, and attaches `EarthRelief3D-Setup.exe` and its `.sha256` to a
GitHub Release. Only the owner tags, pushes and releases.

Pull requests that touch `.github/workflows/release.yml`, `packaging/**` or `src/**` build **unsigned** and never reach Azure.

## How the app finds a new version

EarthRelief3D checks for a new version by itself (Settings, "Updates: check for a new version once a day", on by default),
30 seconds after it starts and then once a day, with one request to
`https://api.github.com/repos/johnovanni/EarthRelief3D/releases/latest` (no key). It never checks in a picture or offline run.

- **Which release.** GitHub's "latest" release only: never a draft or a pre-release. Its tag must be `v<version>`
  (for example `v1.1.0`); a higher version than the running one is offered, by number, not by date.
- **Which file.** Only the asset named exactly `EarthRelief3D-Setup.exe`, from
  `https://github.com/johnovanni/EarthRelief3D/releases/download/v<version>/EarthRelief3D-Setup.exe`, at most 300 MB.
  Its size must equal the size GitHub lists, and its SHA-256 the `digest` GitHub lists for it (or, for a release without one,
  the hash in the `EarthRelief3D-Setup.exe.sha256` asset). Windows must trust its signature, and its signer must be the
  same as the running app's. A copy that is not signed (a developer build) never downloads: it offers the release page.
- **When.** Nothing is downloaded until the user chooses "Download and install". The file goes to
  `%LOCALAPPDATA%\EarthRelief3D\updates\<version>\`. "Install now" checks it again and keeps it locked (nothing can write,
  rename or delete it) until it has started it with `/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS`, as an ordinary
  start, not "run as administrator": the installer asks Windows for permission itself. The installer then closes
  EarthRelief3D (which saves its settings and cache as it closes), installs, and its second `[Run]` entry starts the app
  again as the user. If the installer stops while EarthRelief3D is still open (for example, permission was not given), the
  banner says the update was not installed, with the installer's exit code, and offers the release page. With exit code 0
  (the installer's "success", yet it did not close the app) it says the update may not have been installed.
- **Run as administrator.** When EarthRelief3D itself runs as administrator, the installer would inherit that without a
  Windows prompt and start the app again as administrator, so the app does not install updates itself: the banner names
  the new version and offers only "Open the download page" and "Later".
- **A failed check** (no answer within 30 seconds, body included, or any error) is logged and shown in About; the next
  check is a day later. After 30 days without a successful check a banner says the included data may be out of date; its
  "Check now" shows "Checking…" and then the result.

**Never delete or rename the two assets of a published release** (`EarthRelief3D-Setup.exe` and
`EarthRelief3D-Setup.exe.sha256`) by hand, and never upload a different file under either name: copies already installed
look for exactly those names. Re-running the workflow for the same tag (`gh release upload --clobber`) replaces both with a
rebuilt, re-signed pair; that is allowed, because the updater uses whatever size and digest the release lists when it
checks. A user who checked before the re-run and downloads after it gets a file that does not match what was listed, so
that download is refused (and the next check offers the new pair). To fix a bad release, publish a new version with a
higher number.

## One-time set-up (the owner's)

1. **GitHub.** Create the `release` environment in the repository (Settings, Environments). Add repository **variables**
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` (the same values EarthWatch3D uses).
2. **Azure.** EarthRelief3D signs with EarthWatch3D's Artifact Signing account `JohnApolloni` and profile
   `JohnApolloniPublic` (endpoint `https://cus.codesigning.azure.net/`). In the Entra app that EarthWatch3D uses, add a
   federated credential for GitHub Actions with the subject
   `repo:johnovanni@11181099/EarthRelief3D@1468090118:environment:release`. The Entra app must hold the
   "Artifact Signing Certificate Profile Signer" role on the account (EarthWatch3D's does; not checked for this repository).
3. **First release.** Push the first tag only when 1 and 2 are done, then check the manual install list below on the
   downloaded installer.

## Submodules and the SUBMODULES_TOKEN secret

`external/AstroCore` and `external/GlobeCore` are both public, so a public clone builds without a token, and the workflow
checks out with `token: ${{ secrets.SUBMODULES_TOKEN || github.token }}` and `submodules: recursive`. **No secret is needed
today.** It is needed only if GlobeCore is made private again: then add the repository secret `SUBMODULES_TOKEN`, a
fine-grained personal access token with read-only **Contents** access to `johnovanni/GlobeCore`.

## What is signed

`EarthRelief3D.App.exe`, `EarthRelief3D.App.dll`, `EarthRelief3D.Core.dll`, `AstroCore.dll`, `GlobeCore.dll`,
`GlobeCore.Wpf.dll` and `GlobeCore.Earth.dll`, then `EarthRelief3D-Setup.exe`. The workflow verifies exactly those files
with `Get-AuthenticodeSignature`. Third-party DLLs (HelixToolkit, the .NET runtime and so on) keep their publishers' signatures.

## Checking the published folder locally

```
dotnet publish src/EarthRelief3D.App -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -o artifacts\publish
```

It should hold `EarthRelief3D.App.exe`, the seven DLLs above, `data\` and both notices files. Do not run the installer
build or any signing on the development PC.

## Manual install checklist (on a PC that has not run the app)

- [ ] Install with the default folder (`C:\Program Files\Qapow\EarthRelief3D`).
- [ ] The Start menu has "Qapow > EarthRelief3D".
- [ ] The first start shows the Welcome.
- [ ] About shows "Version 1.0.0" (the released version) and opens the notices.
- [ ] A second install over the first keeps the settings.
- [ ] Uninstall removes `C:\Program Files\Qapow\EarthRelief3D` and leaves `%LOCALAPPDATA%\EarthRelief3D`.
- [ ] Once a newer release is published: the installed older copy shows the banner within a minute of starting;
      "Download and install", then "Install now", updates it, and the restarted app is **not** elevated
      (Task Manager, Details, the "Elevated" column says No). Its settings (for example Text size) are kept.
- [ ] Choosing "No" at Windows' permission prompt leaves the app open, and the banner says the update was not installed,
      with the exit code.
- [ ] Started with "Run as administrator", the older copy's banner offers only "Open the download page" and "Later".
- [ ] After an update, `C:\Program Files\Qapow\EarthRelief3D\data` holds only the new version's files (the installer
      deletes the old data folder first), and the settings are kept.
