# Data sources (Plan 2)

Checked live on 2026-11-22 with `tools/fetch-samples.ps1` (User-Agent `EarthRelief3D/0.1.0 (+https://github.com/johnovanni/EarthRelief3D)`).
No source needs a key or an account. Samples are in `tests/Samples` (see its README).

## 1. UCDP Georeferenced Event Dataset, Candidate (conflict events)

- **URLs:** discover from https://ucdp.uu.se/downloads/ (links under `candidateged/`). On 2026-11-22:
  - monthly: https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv (October 2026; pattern `GEDEvent_v<yy>_0_<m>.csv`)
  - quarterly cumulative: https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv (Jan to Sep 2026; pattern `GEDEvent_v<yy>_<m1>_<yy>_<m2>.csv`)
  - an `.xlsx` twin and a codebook (`ucdp-candidate-codebook1.5.pdf`) sit beside them; the `.xlsx` link for the monthly file is spelled `GEDevent_` (different case), so match `.csv` only.
- **Licence / attribution:** CC BY 4.0 (stated on the downloads page). Attribute: "Uppsala Conflict Data Program (UCDP), Candidate GED, ucdp.uu.se".
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

## 2. IPC national figures via HDX (food insecurity)

- **URL:** https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data ; resource named `ipc_global_national_wide_latest.csv` (12,972 bytes; 50 rows). Do not hard-code the resource URL; read it from the package.
- **Licence / attribution:** CC0 / public domain (CKAN `license_id` `other-pd-nr`). Attribute anyway: "Integrated Food Security Phase Classification (IPC), via HDX; National IPC Technical Working Groups".
- **Cadence:** CKAN `data_update_frequency` is -2 (as needed); the resource was last modified 2026-11-20.
- **As-of field:** `Date of analysis` (e.g. `Sep 2026`) with the period `Current from` / `Current to`; CKAN `last_modified` is the file's age.
- **Checked columns (present):** `Date of analysis, Country, Total country population, Current from, Current to, Population analyzed current, Phase 3+ number current, Phase 3+ percentage current, Phase 1..5 number current, Phase 1..5 percentage current`, and the same for first and second projection.
- **Differences and gotchas found:**
  - `Country` is an ISO3 code (AFG, COD, SDN, HTI...), not a name.
  - Six countries have **no current period** (`Current from`/`Current to` and all current numbers blank): CAF, GTM, HND, HTI, NGA, SSD. HTI and CAF have a first projection only; the others have no projection either. They give no crisis marker.
  - `Total country population` is `0` for AFG, NGA and TCD (unknown); do not divide by it.
  - Of the five trimmed UCDP countries only COD, HTI and SDN appear; Ukraine and Myanmar are not in the national file.
  - Stale analyses are present (e.g. AGO Jun 2021, ETH May 2021, SLV Aug 2021): see ruling P2-4.

## 3. UNHCR Refugee Data Finder population API (displacement)

- **URL:** https://api.unhcr.org/population/v1/population/?year=2026&coo_all=true&limit=500 (one page: `maxPages` 1, 206 items). With `year=2025`: 208 items.
- **Licence / attribution:** UNHCR Refugee Data Finder, CC BY 4.0 (from UNHCR's published terms; not stated in the API response, so confirm before release). Attribute: "UNHCR Refugee Population Statistics Database".
- **Cadence:** UNHCR updates through the year (mid-year and annual releases).
- **As-of field:** the item's `year`. The response has no timestamp; the retrieval date stands for freshness.
- **Checked fields:** `coo_iso, refugees, asylum_seekers, idps, year` are present (also `coo_name`, `coo`, `returned_refugees`, `returned_idps`, `stateless`, `ooc`, `oip`, `hst`).
- **Differences and gotchas found:**
  - **2026 has items, so 2026 was captured** (the plan named 2025).
  - Number types are mixed: `refugees` and `idps` are strings (`"1234886"`), `asylum_seekers` is a JSON number in the first item; some values are `"-"` (not reported). Parse tolerantly.
  - `total` is 0 in the paging block even though `items` is filled; use `maxPages` and the items instead.
  - Sample values (2026): SDN idps 8,685,273; UKR refugees 5,191,571; MMR idps 3,948,628.

## 4. WHO Disease Outbreak News (outbreaks)

- **URL:** https://www.who.int/api/news/diseaseoutbreaknews (OData; `$top` and `$orderby=PublicationDate desc` work). Fetched once on 2026-11-22 to see the shape; **no WHO text is stored in the repo.**
- **Licence / attribution:** WHO website content is CC BY-NC-SA 3.0 IGO (non-commercial, share-alike), so it is fetched at run time and cached in the user's data folder only (C6, ruling P2-5). Attribute: "World Health Organization, Disease Outbreak News".
- **Cadence:** irregular, a few reports per month.
- **As-of field:** `PublicationDate` (also `PublicationDateAndTime`, `LastModified`).
- **Shape found:** `{"@odata.context": ..., "value": [ { Id, LastModified, PublicationDate, DateCreated, IncludeInSitemap, SystemSourceKey, UrlName, ItemDefaultUrl, Response, FurtherInformation, Summary, PublicationDateAndTime, TitleSuffix, UseOverrideTitle, Title, Epidemiology, OverrideTitle, Advice, Assessment, Overview, DonId, Provider } ]}`. The default order is oldest first, so ask for `$orderby=PublicationDate desc`.
- **Gotchas found:** titles separate disease and place with a hyphen, an en dash, or a hyphen with no space before it; some items have `UseOverrideTitle` true with `OverrideTitle` set; `Overview` and `Summary` hold HTML. The synthetic sample mimics these (hyphen and en dash, a multi-country list, accented and territory names).
