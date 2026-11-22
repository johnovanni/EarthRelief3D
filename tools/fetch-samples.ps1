<#
  Captures the test samples in tests/Samples from the live Plan 2 sources (needs network, no keys or accounts).
  Run from anywhere:  pwsh tools/fetch-samples.ps1
  Sends only the User-Agent below; nothing identifying the person who runs it.
  The WHO sample is NOT fetched: WHO text is CC BY-NC-SA and never goes into the repo (tests/Samples/who_don_synthetic.json is hand-made).
#>
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
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
