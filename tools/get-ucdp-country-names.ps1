<#
  Writes data/ucdp-country-names.json: the distinct `country` values of the current UCDP Candidate GED files (the monthly
  and quarterly CSVs the downloads page links), merged with the hand list below (the minimum the app must always resolve).
  Needs network, no keys. Run:  pwsh tools/get-ucdp-country-names.ps1
  The CSVs go to a temporary folder that is deleted at the end; they are never committed.
#>
$ErrorActionPreference = 'Stop'
$ua = 'EarthRelief3D/0.1.0 (+https://github.com/johnovanni/EarthRelief3D)'
$out = Join-Path $PSScriptRoot '..\data\ucdp-country-names.json'
$hand = @('DR Congo (Zaire)', 'Haiti', 'Myanmar (Burma)', 'Sudan', 'Ukraine', 'Russia (Soviet Union)', 'Yemen (North Yemen)',
          'South Sudan', 'Somalia', 'Ethiopia', 'Nigeria', 'Syria', 'Afghanistan', 'Iraq', 'Israel', 'Mali', 'Niger')
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('er3d-ucdp-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $tmp | Out-Null
try {
    $pageUrl = 'https://ucdp.uu.se/downloads/'
    $html = (Invoke-WebRequest -Uri $pageUrl -UserAgent $ua -UseBasicParsing).Content
    $links = [regex]::Matches($html, 'href="([^"]*candidateged/GEDEvent_v[^"]*\.csv)"', 'IgnoreCase') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    if (-not $links) { throw 'no Candidate GED CSV links on the downloads page' }
    $names = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    $files = @()
    foreach ($l in $links) {
        $url = [uri]::new([uri]$pageUrl, $l).AbsoluteUri
        $raw = Join-Path $tmp ([System.IO.Path]::GetFileName($url))
        Invoke-WebRequest -Uri $url -UserAgent $ua -UseBasicParsing -OutFile $raw
        $c = Import-Csv $raw -Encoding utf8 | ForEach-Object { $_.country } | Where-Object { $_ } | Sort-Object -Unique
        foreach ($n in $c) { [void]$names.Add($n.Trim()) }
        $files += $url
        Write-Host "$url : $(@($c).Count) countries"
    }
    foreach ($n in $hand) { [void]$names.Add($n) }
    $json = [ordered]@{
        retrievedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')
        sources      = $files
        note         = 'Distinct country values of the UCDP Candidate GED files above, merged with the hand list in tools/get-ucdp-country-names.ps1. Every name must resolve (data/country-aliases.json included).'
        names        = @($names)
    } | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText((Resolve-Path (Split-Path $out)).Path + '\ucdp-country-names.json', $json, [System.Text.UTF8Encoding]::new($false))
    Write-Host "$($names.Count) names written"
}
finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
