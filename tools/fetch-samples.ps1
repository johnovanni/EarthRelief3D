<#
  Captures the test samples in tests/Samples from the live sources (needs network, no keys or accounts).
  Run from anywhere:  pwsh tools/fetch-samples.ps1            (all)
                      pwsh tools/fetch-samples.ps1 -Set Plan3  (only the Plan 3 samples: GHO, HNO, IPC packages)
                      pwsh tools/fetch-samples.ps1 -Set Plan2  (only the Plan 2 samples)
                      pwsh tools/fetch-samples.ps1 -Set Plan4  (only the Plan 4 samples: ACAPS and FTS packages, the FTS CSV, shape checks)
  Needs PowerShell 7.5 or later (the GHO check uses ConvertFrom-Json -DateKind String).
  Sends only the User-Agent below; nothing identifying the person who runs it.
  The ACAPS workbook is NOT committed (its own licence is CC BY-NC-ND, ruling P4-2): tests/Samples/acaps_access_synthetic.* are invented values
  made by `DataTool workbook`. The Plan 4 sample excerpts of OurAirports, the NGA World Port Index and the Senate list were cut by a one-off script
  from the files this script leaves in .downloads\plan4 (see tests/Samples/README.md).
  The WHO sample is NOT fetched: WHO text is CC BY-NC-SA and never goes into the repo (tests/Samples/who_don_synthetic.json is hand-made).
#>
param([ValidateSet('All', 'Plan2', 'Plan3', 'Plan4')][string]$Set = 'All')
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
  if ($Set -in 'All', 'Plan2') {
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

  if ($Set -in 'All', 'Plan3') {
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
  if ($Set -in 'All', 'Plan4') {
    # ===== Plan 4 sources (checked 2026-11-24) =====
    $root4 = Join-Path $PSScriptRoot '..' | Resolve-Path
    $dl4 = Join-Path $root4 '.downloads\plan4'   # git-ignored; one folder per download, never run from
    New-Item -ItemType Directory -Force "$dl4\acaps", "$dl4\fts", "$dl4\airports" | Out-Null

    # --- ACAPS (HDX): the package, trimmed to its two newest resources. The workbook itself is saved in .downloads only to check its
    # sheets and columns (DataTool describe); it is never committed (ruling P4-2). The edition is read from the resource's description
    # (last_modified does not order the editions).
    $acapsText = Get-Text 'https://data.humdata.org/api/3/action/package_show?id=acaps-humanitarian-access-dataset'
    $acaps = $acapsText | ConvertFrom-Json -DateKind String
    $acapsRes = @($acaps.result.resources | Where-Object { $_.format -eq 'XLSX' })
    if ($acapsRes.Count -lt 2 -or $acapsRes[0].url -notmatch '/acaps-humanitarian-access-dataset[^/]*\.xlsx$') { throw 'ACAPS package: newest XLSX resource not found' }
    Write-Host "ACAPS newest: $($acapsRes[0].description) <- $($acapsRes[0].url)"
    $acaps.result.resources = @($acaps.result.resources | Select-Object -First 2)
    [System.IO.File]::WriteAllText("$out\hdx_acaps_package.json", ($acaps | ConvertTo-Json -Depth 50 -Compress), $utf8)
    Save-File $acapsRes[0].url "$dl4\acaps\newest.xlsx"
    Write-Host "ACAPS workbook saved to .downloads\plan4\acaps (check: dotnet run --project tools/EarthRelief3D.DataTool -- describe <file> --sheet csv)"

    # --- FTS (HDX): the package, and the global CSV trimmed to 2026 plus the 2025 rows of AFG, SDN and HTI
    $ftsText = Get-Text 'https://data.humdata.org/api/3/action/package_show?id=global-requirements-and-funding-data'
    [System.IO.File]::WriteAllText("$out\hdx_fts_package.json", $ftsText, $utf8)
    $ftsRes = ($ftsText | ConvertFrom-Json).result.resources | Where-Object { $_.name -eq 'fts_requirements_funding_global.csv' } | Select-Object -First 1
    if (-not $ftsRes) { throw 'HDX package global-requirements-and-funding-data has no resource fts_requirements_funding_global.csv' }
    Save-File $ftsRes.url "$dl4\fts\fts_requirements_funding_global.csv"
    $ftsRows = Import-Csv "$dl4\fts\fts_requirements_funding_global.csv" -Encoding utf8
    foreach ($need in 'countryCode', 'id', 'name', 'code', 'typeId', 'typeName', 'startDate', 'endDate', 'year', 'requirements', 'funding', 'percentFunded') {
        if (-not ($ftsRows[0].PSObject.Properties.Name -contains $need)) { throw "FTS: column $need missing" }
    }
    $ftsKept = @($ftsRows | Where-Object { $_.year -eq '2026' -or ($_.year -eq '2025' -and @('AFG', 'SDN', 'HTI') -contains $_.countryCode) })
    $ftsKept | Export-Csv "$out\fts_requirements_funding_global.csv" -Encoding utf8NoBOM -UseQuotes AsNeeded
    Write-Host "FTS: $($ftsRows.Count) rows, kept $($ftsKept.Count)"

    # --- OurAirports, Senate list and the IRS Range check: shape checks only (the excerpts were cut by a one-off script)
    Save-File 'https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv' "$dl4\airports\airports.csv"
    $air = Import-Csv "$dl4\airports\airports.csv" -Encoding utf8
    foreach ($need in 'type', 'name', 'latitude_deg', 'longitude_deg', 'iso_country', 'scheduled_service', 'iata_code') {
        if (-not ($air[0].PSObject.Properties.Name -contains $need)) { throw "OurAirports: column $need missing" }
    }
    Write-Host "OurAirports: $($air.Count) rows, columns ok"
    $range = Invoke-WebRequest -Uri 'https://apps.irs.gov/pub/epostcard/990/xml/2026/2026_TEOS_XML_01A.zip' -UserAgent $ua -UseBasicParsing -Headers @{ Range = 'bytes=0-99' }
    Write-Host "IRS Range request: HTTP $($range.StatusCode) (206 is needed), Content-Range $($range.Headers['Content-Range'])"
    if ($range.StatusCode -ne 206) { Write-Warning 'apps.irs.gov did not answer the Range request with 206: the vetting tool cannot read zip members remotely.' }
  }
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
