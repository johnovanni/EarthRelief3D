# Data sources (Plan 2)

Checked live on 2026-11-22 with `tools/fetch-samples.ps1` (User-Agent `EarthRelief3D/0.1.0 (+https://github.com/johnovanni/EarthRelief3D)`).
No source needs a key or an account. Samples are in `tests/Samples` (see its README).

## 1. UCDP Georeferenced Event Dataset, Candidate (conflict events)

- **URLs:** discover from https://ucdp.uu.se/downloads/ (links under `candidateged/`). On 2026-11-22:
  - monthly: https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv (October 2026; pattern `GEDEvent_v<yy>_0_<m>.csv`)
  - quarterly cumulative: https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv (Jan to Sep 2026; pattern `GEDEvent_v<yy>_<m1>_<yy>_<m2>.csv`)
  - an `.xlsx` twin and a codebook (`ucdp-candidate-codebook1.5.pdf`) sit beside them; the `.xlsx` link for the monthly file is spelled `GEDevent_` (different case), so match `.csv` only.
- **Licence / attribution:** CC BY 4.0 (stated on the downloads page). Attribute: "Uppsala Conflict Data Program (UCDP) Candidate Events Dataset, Hegre et al. 2020".
- **Cadence:** monthly releases, "not more than a month's lag" (page text); a quarterly cumulative file covers the earlier months.
- **As-of field:** `date_end` of the newest event; the file name gives the release (the monthly 2026-10 file runs to 2026-11-06 in this sample).
- **Checked columns (all present, 49 in total):** `id, year, type_of_violence, conflict_new_id, conflict_name, side_a, side_b, country, country_id, latitude, longitude, date_start, date_end, best` (plus the rest of the GED set, e.g. `deaths_*`, `high`, `low`, `adm_1`, `where_description`).
- **Country strings as UCDP spells them:** `Sudan`, `Ukraine`, `Myanmar (Burma)`, `DR Congo (Zaire)`, `Haiti`. `South Sudan` is a separate country: match whole strings, never substrings.
- **Differences and gotchas found:**
  - Dates are `2026-10-25 00:00:00.000` (date and time text), not plain dates.
  - The monthly and quarterly files overlap: the monthly file starts at 2026-09-27 and the quarterly one runs to 2026-10-04; 4 event ids occur in both in the trimmed samples. Dedupe by `id` when joining the windows (P2-8).
  - Some events carry placeholder names (`XXX490` in `conflict_name`/`side_a`/`side_b`; 6 of 457 monthly rows and 51 of 4,041 quarterly rows in the trim). Show them without inventing a name.
  - `type_of_violence` values seen: 1 (state-based), 2 (non-state), 3 (one-sided). `best`, `latitude` and `longitude` are never empty in the samples.
  - Sizes: the downloads page is 138 KB of HTML; the untrimmed CSVs are 2 MB (monthly) and 13 MB (quarterly).
- **Which country an event belongs to (ruling R14):** the country whose Natural Earth 1:50m polygon (GlobeCore
  `CountryIndex`, with `data/country-units.json` applied) contains the event's `latitude`/`longitude`, because that is
  where the map draws it. UCDP's `country` name is used only when the point is in no country (at sea or just off a
  coast). An event of a qualifying conflict that has neither is listed under "Not placed on the map" (source UCDP, the
  country string, the number of events and deaths), never dropped. Why: UCDP's country names follow its own state list,
  which has no Palestine, so events in Gaza can carry the country `Israel` (the final review's example; the trimmed
  samples hold no such event, so a test uses a hand-made row); by their coordinates they go to the State of Palestine
  (PSE). The crisis threshold is still counted over the whole conflict (R9).
  - **Cost, measured on the trimmed samples** (4,494 distinct events): 41 events (0.9%) land in another country than
    their `country` name. All 41 are within 4.7 km of the named country's 1:50m outline, and UCDP's own `adm_1` or
    place description puts each of them in the named country, so these come from the coarse outlines, not from
    cross-border events: 20 Ukraine events (Sumy and Kharkiv oblasts) fall in Russia; 9 in and near Goma (North Kivu,
    DR Congo) in Rwanda; 3 DR Congo events (Ituri, North Kivu) in Uganda; 5 Sudan events (Blue Nile) in Ethiopia; 2
    (North Darfur) in Libya and Egypt; 2 Myanmar events (Tanintharyi) in Thailand. 46 events are at sea or off a coast
    and are placed by name (Ukraine 21, Myanmar 15, Haiti 10).
    The dots are drawn at UCDP's coordinates either way; only the panel they are listed in changes.

## 2. IPC national figures via HDX (food insecurity)

- **URL:** https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data ; resource named `ipc_global_national_wide_latest.csv` (12,972 bytes; 50 rows). Do not hard-code the resource URL; read it from the package.
- **Licence / attribution:** CC0 / public domain (CKAN `license_id` `other-pd-nr`). Attribute anyway: "Integrated Food Security Phase Classification (IPC), via HDX; National IPC Technical Working Groups".
- **Cadence:** CKAN `data_update_frequency` is -2 (as needed); the resource was last modified 2026-11-20.
- **As-of field:** `Date of analysis` (e.g. `Sep 2026`) with the period `Current from` / `Current to`; CKAN `last_modified` is the file's age.
- **Checked columns (present):** `Date of analysis, Country, Total country population, Current from, Current to, Population analyzed current, Phase 3+ number current, Phase 3+ percentage current, Phase 1..5 number current, Phase 1..5 percentage current`, and the same for first and second projection.
- **Differences and gotchas found:**
  - `Country` is an ISO3 code (AFG, COD, SDN, HTI...), not a name.
  - Six countries have **no current period** (`Current from`/`Current to` and all current numbers blank): CAF, GTM, HND, HTI, NGA, SSD. Each has a projection (checked in `tests/Samples/ipc_national_wide_latest.csv`, re-read 2026-11-22): CAF, HTI and NGA have a first projection and no second; GTM, HND and SSD have a second projection and no first. They give no crisis marker and no shading. The app keeps them as **projection-only** records: the panel says "IPC analysis <Date of analysis>: projection only (<from>–<to>), no current-period figures" with the projected Phase 3+ number, and neither the panel headline nor the hover calls such a country "no crisis recorded" (they say "IPC: projection only"). A row with neither a current period nor a projection would be skipped and counted in the log.
  - `Total country population` is `0` for AFG, NGA and TCD (unknown); do not divide by it.
  - Of the five trimmed UCDP countries only COD, HTI and SDN appear; Ukraine and Myanmar are not in the national file.
  - Stale analyses are present (e.g. AGO Jun 2021, ETH May 2021, SLV Aug 2021): see ruling P2-4.

## 3. UNHCR Refugee Data Finder population API (displacement)

- **URL:** https://api.unhcr.org/population/v1/population/?year=2026&coo_all=true&limit=500 (one page: `maxPages` 1, 206 items). With `year=2025`: 208 items.
- **Licence / attribution:** UNHCR Refugee Data Finder, CC BY 4.0 (from UNHCR's published terms; not stated in the API response, so confirm before release). Attribute: "UNHCR Refugee Population Statistics Database".
- **Cadence:** UNHCR updates through the year (mid-year and annual releases).
- **As-of field:** the item's `year`; the response has no date. Rule R10 (an inference from UNHCR's usual releases, not a field): data for the clock year is mid-year, as of 30 June; an earlier year is end-year, as of 31 December, only from 1 July of the following year, otherwise 30 June of that year. The clock is never the as-of date; the retrieval date is kept separately.
- **Checked fields:** `coo_iso, refugees, asylum_seekers, idps, year` are present (also `coo_name`, `coo`, `returned_refugees`, `returned_idps`, `stateless`, `ooc`, `oip`, `hst`).
- **Differences and gotchas found:**
  - **2026 has items, so 2026 was captured** (the plan named 2025).
  - Number types are mixed: `refugees` and `idps` are strings (`"1234886"`), `asylum_seekers` is a JSON number in the first item; some values are `"-"` (not reported). Parse tolerantly.
  - `total` in the paging block is an empty list (`"total": []`), not a count, even though `items` is filled; use `maxPages` and the items instead.
  - Sample values (2026): SDN idps 8,685,273; UKR refugees 5,191,571; MMR idps 3,948,628.

## 4. WHO Disease Outbreak News (outbreaks)

- **URL:** https://www.who.int/api/news/diseaseoutbreaknews (OData; `$top` and `$orderby=PublicationDate desc` work). Fetched once on 2026-11-22 to see the shape; **no WHO text is stored in the repo.**
- **Licence / attribution:** WHO website content is CC BY-NC-SA 3.0 IGO (non-commercial, share-alike), so it is fetched at run time and cached in the user's data folder only (C6, ruling P2-5). Attribute: "World Health Organization, Disease Outbreak News".
- **Cadence:** irregular, a few reports per month.
- **As-of field:** `PublicationDate` (also `PublicationDateAndTime`, `LastModified`).
- **Shape found:** `{"@odata.context": ..., "value": [ { Id, LastModified, PublicationDate, DateCreated, IncludeInSitemap, SystemSourceKey, UrlName, ItemDefaultUrl, Response, FurtherInformation, Summary, PublicationDateAndTime, TitleSuffix, UseOverrideTitle, Title, Epidemiology, OverrideTitle, Advice, Assessment, Overview, DonId, Provider } ]}`. The default order is oldest first, so ask for `$orderby=PublicationDate desc`.
- **Gotchas found:** titles separate disease and place with a hyphen, an en dash, or a hyphen with no space before it; some items have `UseOverrideTitle` true with `OverrideTitle` set; `Overview` and `Summary` hold HTML. The synthetic sample mimics these (hyphen and en dash, a multi-country list, accented and territory names); the no-space form ("Disease- Place") is pinned by rows of `WhoOutbreakTests.ParsesDiseaseAndPlaces`, while a dash inside a word ("cholera-like") is not a separator. The feed reads only the newest 50 reports, so a response that is not newest first is treated as a format change ("format changed: not sorted by publication date").

## Offline samples (`--offline-samples`)

The app's diagnostic handler (`src/EarthRelief3D.App/Diagnostics/OfflineSamples.cs`) maps each live URL to a file in
`tests/Samples`; anything else is a 404. `OfflineSamplesTests` runs the four feeds through it, so a URL that changes in a
feed fails a test.

| Live URL | Sample file |
|---|---|
| https://ucdp.uu.se/downloads/ | `ucdp_downloads.html` |
| https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv | `ucdp_candidate_monthly.csv` |
| https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv | `ucdp_candidate_quarterly.csv` |
| https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data | `hdx_ipc_package.json` |
| the `ipc_global_national_wide_latest.csv` resource URL named in that package | `ipc_national_wide_latest.csv` |
| https://www.who.int/api/news/diseaseoutbreaknews (any query) | `who_don_synthetic.json` (synthetic, no WHO text) |
| https://api.unhcr.org/population/v1/population/?year=2026&coo_all=true&limit=500 | `unhcr_population_coo.json` |

The samples are from Nov 2026, so offline runs use `--display-time 2026-11-22T12:00:00Z`. The WHO sample has no report for
Sudan, so Sudan's panel shows conflict, food crisis and displacement (three sections); that is what the samples hold.
