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

## One-time set-up (the owner's)

1. **GitHub.** Create the `release` environment in the repository (Settings, Environments). Add repository **variables**
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` (the same values EarthWatch3D uses).
2. **Azure.** EarthRelief3D signs with EarthWatch3D's Artifact Signing account `JohnApolloni` and profile
   `JohnApolloniPublic` (endpoint `https://cus.codesigning.azure.net/`). In the Entra app that EarthWatch3D uses, add a
   federated credential for GitHub Actions with the subject
   `repo:johnovanni@11181099/EarthRelief3D@1468090118:environment:release`. The app already holds the
   "Artifact Signing Certificate Profile Signer" role on the account.
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
