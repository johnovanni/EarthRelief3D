<#
  Captures the test samples in tests/Samples from the live sources (needs network, no keys or accounts).
  Run from anywhere:  pwsh tools/fetch-samples.ps1            (all)
                      pwsh tools/fetch-samples.ps1 -Set Plan3  (only the Plan 3 samples: GHO, HNO, IPC packages)
                      pwsh tools/fetch-samples.ps1 -Set Plan2  (only the Plan 2 samples)
  Needs PowerShell 7.5 or later (the GHO check uses ConvertFrom-Json -DateKind String).
  Sends only the User-Agent below; nothing identifying the person who runs it.
  The WHO sample is NOT fetched: WHO text is CC BY-NC-SA and never goes into the repo (tests/Samples/who_don_synthetic.json is hand-made).
#>
param([ValidateSet('All', 'Plan2', 'Plan3')][string]$Set = 'All')
$ErrorActionPreference = 'Stop'
$ua = 'EarthRelief3D/0.1.0 (+https://github.com/johnovanni/EarthRelief3D)'
$out = Join-Path $PSScriptRoot '..\tests\Samples' | Resolve-Path
$utf8 = [System.Text.UTF8Encoding]::new($false)
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('er3d-samples-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $tmp | Out-Null

function Get-Text([string]$url) {
    (Invoke-WebRequest -Uri $url -UserAgent $ua -UseBasicParsing).Content
}
function Save-File([string]$url, [string]$path) {
    Invoke-WebRequest -Uri $url -UserAgent $ua -UseBasicParsing -OutFile $path
}
# UCDP's country strings, exactly as the files spell them ("Sudan" must not match "South Sudan").
$countries = @('Sudan', 'Ukraine', 'Myanmar (Burma)', 'DR Congo (Zaire)', 'Haiti')

try {
  if ($Set -ne 'Plan3') {
    # --- UCDP Candidate: discover the files from the downloads page ---
    $pageUrl = 'https://ucdp.uu.se/downloads/'
    $html = Get-Text $pageUrl
    [System.IO.File]::WriteAllText("$out\ucdp_downloads.html", $html, $utf8)
    $links = [regex]::Matches($html, 'href="([^"]*candidateged/GEDEvent_v[^"]*\.csv)"', 'IgnoreCase') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    $monthly = $links | Where-Object { $_ -match 'GEDEvent_v\d+_0_\d+\.csv$' } | Select-Object -First 1
    $quarterly = $links | Where-Object { $_ -match 'GEDEvent_v\d+_\d+_\d+_\d+\.csv$' } | Select-Object -First 1
    if (-not $monthly -or -not $quarterly) { throw "UCDP downloads page: monthly or quarterly Candidate CSV link not found ($($links -join ', '))" }
    foreach ($item in @(@($monthly, 'ucdp_candidate_monthly.csv'), @($quarterly, 'ucdp_candidate_quarterly.csv'))) {
        $url = [uri]::new([uri]$pageUrl, $item[0]).AbsoluteUri
        $raw = Join-Path $tmp ([System.IO.Path]::GetFileName($url))
        Save-File $url $raw
        $rows = Import-Csv $raw -Encoding utf8
        $kept = @($rows | Where-Object { $countries -contains $_.country })
        $kept | Export-Csv "$out\$($item[1])" -Encoding utf8NoBOM -UseQuotes AsNeeded
        Write-Host "$url : $($rows.Count) rows, kept $($kept.Count)"
    }

    # --- IPC national, wide, latest (HDX CKAN) ---
    $pkgText = Get-Text 'https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data'
    [System.IO.File]::WriteAllText("$out\hdx_ipc_package.json", $pkgText, $utf8)
    $pkg = $pkgText | ConvertFrom-Json
    $res = $pkg.result.resources | Where-Object { $_.name -eq 'ipc_global_national_wide_latest.csv' } | Select-Object -First 1
    if (-not $res) { throw 'HDX package has no resource ipc_global_national_wide_latest.csv' }
    Save-File $res.url "$out\ipc_national_wide_latest.csv"
    Write-Host "IPC: $($res.url)"

    # --- UNHCR population by country of origin; this year if it has items, else last year ---
    $thisYear = (Get-Date).Year
    foreach ($year in @($thisYear, ($thisYear - 1))) {
        $text = Get-Text "https://api.unhcr.org/population/v1/population/?year=$year&coo_all=true&limit=500"
        if (@(($text | ConvertFrom-Json).items).Count -gt 0) {
            [System.IO.File]::WriteAllText("$out\unhcr_population_coo.json", $text, $utf8)
            Write-Host "UNHCR: year $year"
            break
        }
    }
  }

  if ($Set -ne 'Plan2') {
    # ===== Plan 3 sources =====
    $root = Join-Path $PSScriptRoot '..' | Resolve-Path
    $downloads = Join-Path $root '.downloads'   # git-ignored; the full IPC GeoJSON lands here for the one-off trim (ruling R1)
    New-Item -ItemType Directory -Force $downloads | Out-Null

    # --- WHO GHO OData: shape check only (ruling R5). Real GHO data is never written into tests/Samples:
    # tests/Samples/gho_*.json are SYNTHETIC (invented values, hand-made, same shape). This only fetches the live answer to the
    # scratch folder and fails if a field the app relies on is missing. (The API is announced as deprecated; no successor URL.)
    foreach ($code in @('WSH_WATER_BASIC', 'WSH_SANITATION_BASIC', 'UHC_INDEX_REPORTED')) {
        $text = Get-Text "https://ghoapi.azureedge.net/api/$code"
        [System.IO.File]::WriteAllText("$tmp\gho_$code.json", $text, $utf8)
        $rows = @(($text | ConvertFrom-Json -DateKind String).value)
        foreach ($need in @('SpatialDim', 'SpatialDimType', 'TimeDim', 'NumericValue', 'Dim1')) {
            if (-not ($rows[0].PSObject.Properties.Name -contains $need)) { throw "GHO ${code}: field $need missing" }
        }
        Write-Host "GHO ${code}: $($rows.Count) rows, shape ok (live answer in the scratch folder only, deleted at the end)"
    }

    # --- HNO (HDX CKAN): package, and the 2026 national CSV (whole file) ---
    $hnoText = Get-Text 'https://data.humdata.org/api/3/action/package_show?id=global-hpc-hno'
    [System.IO.File]::WriteAllText("$out\hdx_hno_package.json", $hnoText, $utf8)
    $hnoRes = ($hnoText | ConvertFrom-Json).result.resources | Where-Object { $_.url -match '/hpc_hno_2026\.csv$' } | Select-Object -First 1
    if (-not $hnoRes) { throw 'HDX package global-hpc-hno has no resource hpc_hno_2026.csv' }
    Save-File $hnoRes.url "$out\hpc_hno_2026.csv"
    Write-Host "HNO: $($hnoRes.url)"

    # --- IPC per-country datasets (HDX CKAN): Nigeria (with GeoJSON) and Eswatini's old duplicate (no GeoJSON) ---
    $ipcText = Get-Text 'https://data.humdata.org/api/3/action/package_show?id=nigeria-acute-food-insecurity-country-data'
    [System.IO.File]::WriteAllText("$out\hdx_ipc_nga_package.json", $ipcText, $utf8)
    $ipcPkg = $ipcText | ConvertFrom-Json
    $geo = $ipcPkg.result.resources | Where-Object { $_.name -eq 'ipc_nga.geojson' } | Select-Object -First 1
    if (-not $geo) { throw 'IPC Nigeria package has no ipc_nga.geojson' }
    Save-File $geo.url "$downloads\ipc_nga.geojson"
    Write-Host "IPC NGA GeoJSON (full, 795 areas) saved to .downloads; tests/Samples/ipc_nga.geojson is trimmed by hand/one-off script (R1), NOT by this script"
    $natRes = $ipcPkg.result.resources | Where-Object { $_.name -eq 'ipc_nga_national_long_latest.csv' } | Select-Object -First 1
    if (-not $natRes) { throw 'IPC Nigeria package has no ipc_nga_national_long_latest.csv' }
    Save-File $natRes.url "$out\ipc_nga_national_long_latest.csv"
    $nogeoText = Get-Text 'https://data.humdata.org/api/3/action/package_show?id=kingdom-of-eswatini-acute-food-insecurity-country-data'
    [System.IO.File]::WriteAllText("$out\hdx_ipc_nogeo_package.json", $nogeoText, $utf8)
    if (@(($nogeoText | ConvertFrom-Json).result.resources | Where-Object { $_.name -like '*.geojson' }).Count -ne 0) { Write-Warning 'The Eswatini package now has a GeoJSON: pick another no-GeoJSON dataset for the no-areas test.' }
  }
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
