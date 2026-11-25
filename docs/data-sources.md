# Data sources (Plans 2, 3 and 4)

Sections 1 to 4 (Plan 2) were checked live on 2026-11-22 and sections 5 to 10 (Plan 3) on 2026-11-23, with `tools/fetch-samples.ps1` (User-Agent `EarthRelief3D/0.1.0 (+https://github.com/johnovanni/EarthRelief3D)`).
No source needs a key or an account. Samples are in `tests/Samples` (see its README). Sections 12 to 20 (Plan 4) were checked on 2026-11-24.

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
- **Which country an event belongs to (ruling R14b, replacing R14):** UCDP's own `country` value, resolved through
  `data/country-aliases.json`. The event's coordinates (the Natural Earth 1:50m polygon that contains them, GlobeCore
  `CountryIndex` of the live boundary set, with the rules in `data/boundary-rules.json` applied: Crimea in
  Ukraine, Somaliland in Somalia) override it in only two cases:
  1. the point lies in a territory UCDP does not list as a country, kept with a reason per entry in
     `data/ucdp-territories.json` (it starts with PSE: UCDP codes events in Gaza and the West Bank under `Israel`; the map
     and panels show Palestine separately, owner ruling Q3, UN humanitarian units). The rule: if the containing polygon's
     ISO3 is in that list and differs from the name's ISO3, the polygon's is used;
  2. the country name does not resolve: the containing polygon's country is used. If the point is in no country either
     (at sea), the event is listed under "Not placed on the map" (source UCDP, the country string, the number of events
     and deaths), never dropped.

  The crisis threshold is still counted over the whole conflict (R9). The dots are drawn at UCDP's coordinates either
  way; only the panel an event is listed in depends on this rule.
  - **Why not the coordinates everywhere (R14, withdrawn):** the 1:50m outlines are too coarse near borders. On the
    trimmed samples (4,494 distinct events) R14 moved 41 events (0.9%) out of the country UCDP names, all within 4.7 km
    of that country's 1:50m outline and all placed in that country by UCDP's own `adm_1` or place description: 20 Ukraine
    events (Sumy and Kharkiv oblasts) into Russia, 9 in and near Goma (DR Congo) into Rwanda, 3 DR Congo events into
    Uganda, 5 Sudan events (Blue Nile) into Ethiopia, 2 (North Darfur) into Libya and Egypt, and 2 Myanmar events
    (Tanintharyi) into Thailand.
  - **Checked under R14b** on the same samples: 0 events differ from UCDP's own country (the samples hold no event in a
    listed territory and every country name resolves). The Gaza case is pinned by a hand-made row
    (`UcdpFeedFixTests.GazaEventCodedIsraelGoesToPalestine`), the Sumy case by one of the 41 sample points
    (`.SumyBorderEventCodedUkraineStaysInUkraine`).

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
  - The percentage columns are rounded to two decimals (PAK: 10,553,872 of 53,441,149 people analysed is 19.7%, given as
    `0.2`). The shading never uses them; it uses the exact counts.
- **Shading rule (ruling R17, the owner's decision of 2026-11-23):** each country with a current analysis is shaded by
  IPC's "20% rule" for area classification, applied here to the whole country (our adaptation; the national file has no
  areas): the worst phase p (5, 4 or 3) such that the people in Phase p or higher are at least 20% of the people analysed.
  One denominator D serves the rule, the panel's figures and the bar: `Population analyzed current` when it is at least
  the sum of the Phase 1 to 5 current numbers; the sum when that is larger or when the people analysed is blank. In the
  sample, 22 of the 36 current rows add up to slightly more than the people analysed (rounding; Sudan by 30): up to 1%
  over, the panel says "of 47,535,794 people analysed (the phase numbers add up to 30 more)", and only a bigger gap is
  named as the five phases adding up to more (and shares are then "of the N people in the five phases"). When the people
  analysed and the sum are both 0, the share is unknown and the country is not shaded. When the Phase 3 to 5 numbers are
  blank, the file's "Phase 3+ number current" stands for them (a Phase 3+ share above 100% is shown as "more than all the
  people analysed: the file's numbers disagree"). Phases 1 and 2 are never shaded. The country also gets an outline in
  the colour of the worst phase from 3 to 5 that has people in it, when that phase is worse than the shading (or nothing
  is shaded). A country that has such an outline but no shaded phase (some people in Phase 3 or worse, but no phase
  covers 20%) also gets a faint tint of the same colour (alpha 46 of 255, about 18%, a third of a shaded fill's 60%; the
  owner's decision of 2026-11-23): "Not shaded (no phase covers 20% of the people); faint Phase 4 tint and outline: some
  people are in Phase 4". A shaded country gets no faint tint. One record per country is used, the latest period (as in the panel); stale and projection-only records are
  neither shaded nor outlined. The panel shows the people analysed as a stacked bar (the people in no reported phase in
  grey first, then Phases 1 to 5) with a "worst 20%" marker that always falls in the rule's phase. A country shaded Phase
  5 is not an IPC famine classification, which also needs malnutrition and death-rate data (said in the legend, and
  under the bar of a country shaded Phase 5). With the sample file (judged on 2026-11-22): 36 current analyses;
  7 are shaded Phase 3 (AFG, COD, LBN, PSE, SOM and YEM with a Phase 4 outline; SDN with a Phase 5 outline); 29 have the
  faint tint and outline (19 in Phase 4's colour, 10 in Phase 3's); none reaches 20% in Phase 4 or higher.

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

## 5. WHO Global Health Observatory OData (water, sanitation and health-care coverage; Plan 3)

Checked live 2026-11-23 (User-Agent as above). Samples: `tests/Samples/gho_wsh_water_basic.json`, `gho_wsh_sanitation_basic.json`, `gho_uhc_index.json`.

- **URLs:** base `https://ghoapi.azureedge.net/api/` (one constant in the app). Indicators:
  - `WSH_WATER_BASIC`: "Population using at least basic drinking-water services (%)", 14,875 rows, 8.8 MB, 2000 to 2024;
  - `WSH_SANITATION_BASIC`: "Population using at least basic sanitation services (%)", 14,697 rows, 8.8 MB, 2000 to 2024;
  - `UHC_INDEX_REPORTED`: "UHC Service Coverage Index (SDG 3.8.1)", 5,160 rows, 2.9 MB, 2000 to 2023 (latest year 2023, not 2024).
  - Names come from `/api/Indicator?$filter=IndicatorCode in (...)`. A whole indicator is one response (no paging).
- **Licence / attribution:** WHO's data terms (https://data.who.int/about/data/terms-and-conditions, read 2026-11-23) say the datasets are provided under CC BY 4.0 "unless specifically indicated otherwise", with additional terms: do not de-anonymise or misrepresent the data; do not state or imply that WHO endorses the user or the output; no use of WHO's name or emblem without written approval; disputes go to conciliation, then UNCITRAL arbitration. Citation: use the attribution in the metadata, and if there is none, the form `World Health Organization {YEAR} data.who.int, {ITEM_NAME} [{ITEM_TYPE}]. {URL} (Accessed on {ACCESS DATE})`. The app shows: "World Health Organization, Global Health Observatory: <indicator name>, <year>; CC BY 4.0; retrieved <date>".
  - **Open question for the owner:** those terms are on data.who.int. The GHO OData page (https://www.who.int/data/gho/info/gho-odata-api) states no licence, and WHO's "Permissions and licensing" page (https://www.who.int/about/policies/publishing/copyright) names CC BY-NC-SA 3.0 IGO for WHO publications. Nothing we found says which applies to GHO API rows. GHO and data.who.int are "driven by a single underlying data source" (WHO's legacy page), so CC BY 4.0 is the likelier reading, but the owner should confirm. The committed samples are synthetic (R5), so the answer only affects what the app may show live and cache (C6: if it is NC-SA, the app fetches live and caches in the user's data folder only, as for WHO DON).
- **Deprecation (checked):** https://www.who.int/data/gho/legacy says "The current GHO OData API will be deprecated near the end of 2025" and lists "GHO OData API (deprecated end of 2025)" next to "World Health Data Hub OData API (available, end of 2025)". The second link is `href="#"`: **no successor URL is published**. On 2026-11-23 the old API still answers (HTTP 200), so keep it, keep the base URL in one constant, and treat a failure as "source unavailable".
- **Cadence:** WHO updates when estimates are published (WASH: JMP update; UHC: biennial). In the live answer of 2026-11-23, every WASH row's `Date` was 2026-01-23 and the UHC rows' was 2025-12-05.
- **As-of field:** `TimeDim` (the year of the estimate). The row's `Date` is when WHO loaded the row, not the as-of date.
- **Checked columns (present):** `SpatialDim` (ISO3 when `SpatialDimType` is COUNTRY), `TimeDim` (number), `NumericValue` (double), `Dim1`. Also `Value` (rounded text), `Low` and `High` (always null here), `ParentLocationCode`, `DataSourceDim` (null).
- **Differences and gotchas found:**
  - Rows include `SpatialDimType` `REGION` and `GLOBAL` (UHC also `WORLDBANKREGION`, `WORLDBANKINCOMEGROUP`): keep only `COUNTRY` (226 country codes in the water file).
  - WASH rows come three times per country and year in `Dim1`: `RESIDENCEAREATYPE_TOTL`, `_URB`, `_RUR`. Use `_TOTL`. `UHC_INDEX_REPORTED` has `Dim1` null.
  - Years differ by country: the USA has `_TOTL` water rows for 20 of the 25 years (its `_URB` has 25). Take each country's newest year, not a fixed year.
  - `Value` is the rounded text ("65"), `NumericValue` the exact double (64.89171295). Use `NumericValue`.
  - Latest values in the sample (`_TOTL`), water 2024: SDN 64.9, YEM 75.0, AFG 80.8, NER 53.4, TCD 52.2, ISL 100, USA 99.98, CHN 96.1, PSE 98.8. UHC 2023: SDN 48, YEM 43, AFG 42, NER 39, TCD 26, ISL 90, USA 88, CHN 85, PSE 65.
  - "Share of people without at least basic sanitation, 2024" is 100 minus the file's value; the legend must say so.
- **Samples are synthetic (ruling R5, as P2-5 for WHO DON):** the three `gho_*.json` files hold **invented values** in the exact shape of the live answer checked on 2026-11-23 (`@odata.context`, a `value` array, the same field names and types). 15 countries (SDN, YEM, AFG, NER, TCD, ISL, USA, CHN, PSE, SOM, SSD, UKR, HTI, COD, MMR), three years each (USA's WASH rows skip 2023, a gap like the live data's), `RESIDENCEAREATYPE_TOTL` rows plus one `_URB` and one `_RUR` series for SDN so the filter can be tested; UHC has `Dim1` null. Each file is about 25 to 30 KB. Real GHO rows are fetched live by the app. `tools/fetch-samples.ps1` does not write GHO data into `tests/Samples`: it only fetches the live answer to a scratch folder to check that the fields are still there. (The "Latest values in the sample" line above describes the live answer, not the files.)

## 6. OCHA Humanitarian Needs Overview, national, via HDX (people in need; Plan 3)

Checked live 2026-11-23. Samples: `tests/Samples/hdx_hno_package.json`, `hpc_hno_2026.csv`.

- **URL:** https://data.humdata.org/api/3/action/package_show?id=global-hpc-hno ("Global Humanitarian Programme Cycle, Humanitarian Needs", organisation "OCHA Humanitarian Programme Cycle Tools (HPC Tools)"). Read the 2026 file's URL from the package. Its resource is named **"Global HPC HNO 2026 CSV"** (not `hpc_hno_2026.csv`; that is the last part of its URL), 6,682 bytes (6.7 KB). Siblings "Global HPC HNO 2025 CSV" (27 MB) and 2024 (32 MB) are not used.
- **Licence / attribution:** CKAN `license_id` `cc-by-igo` ("Creative Commons Attribution for Intergovernmental Organisations (CC BY-IGO)", http://creativecommons.org/licenses/by/3.0/igo/legalcode). `dataset_source`: "Humanitarian partners". The package has no attribution text of its own. The app shows: "OCHA Humanitarian Programme Cycle Tools (HPC Tools), via HDX: Global HPC HNO 2026; CC BY-IGO".
- **Cadence:** `data_update_frequency` 365 (yearly). The 2026 file's `last_modified` is 2026-02-13; the package `dataset_date` is `[2024-01-01 TO 2026-12-31]`.
- **As-of field:** none per row. The year is in the file's name (2026) and its `last_modified` (2026-02-13) says when it was loaded. Say "Humanitarian Needs Overview 2026 (file loaded 2026-02-13)", never today's date.
- **Checked columns (all 10 present):** `Country ISO3, Description, Cluster, Category, Population, In Need, Targeted, Affected, Reached, Info`. `Category`, `Affected`, `Reached` and `Info` are empty in all 134 rows.
- **Cluster codes (all five present):** `ALL` (20 rows, one per country), `FSC` (11), `WSH` (11), `SHL` (11), `HEA` (11). Others: `EDU, NUT, PRO, CCM, MPC, MS, PRO-CPN, PRO-GBV, PRO-MIN, PRO-HLP`.
- **Differences and gotchas found:**
  - **Only 20 countries:** AFG, BFA, CAF, CMR, COD, COL, HTI, MLI, MMR, MOZ, NER, NGA, SDN, SOM, SSD, SYR, TCD, UKR, VEN, YEM.
  - **SDN and YEM have only the `ALL` row** (no FSC, WSH, SHL or HEA split); so do BFA, CMR, COL, MLI, NER, SYR and VEN. Eleven countries have FSC and WSH: AFG, CAF, COD, HTI, MMR, MOZ, NGA, SOM, SSD, TCD, UKR. A cluster with no row means "not reported in this file", never zero.
  - `Description` is free text in the country's language (French for CAF, COD and HTI: "Sécurité alimentaire", "EPAH"); use `Cluster`.
  - `ALL` rows are described as "Final", "Final caseload", "Plan caseload", "GHO Estimates" and so on. The `GHO Estimates` rows (BFA, CMR, COL, MLI, NER, SYR, VEN, YEM) are not from a country's own Humanitarian Needs Overview: show `Description` beside the figure.
  - `Population` is filled only on the `ALL` row. Cluster rows have `In Need` and `Targeted` only (a few `In Need` cells are blank, e.g. `MPC`).
  - `PRO` repeats within CAF, COD, MOZ, TCD and UKR (sub-sector rows sharing a cluster code). The five codes the app reads have no repeats in this file; reject a repeat rather than add it.
  - UTF-8 without BOM, comma separated, quoted where needed.
- **User-Agent:** HDX accepted the app's own User-Agent for the package and the CSV (HTTP 200), and also plain `curl`'s default User-Agent, on 2026-11-23. The earlier finding that HDX needs a browser-like User-Agent was **not reproduced**. Keep sending the project's own and treat a 403 as "source unavailable".

## 7. IPC acute food insecurity, per-country areas, via HDX (Plan 3)

Checked live 2026-11-23. Samples: `tests/Samples/hdx_ipc_nga_package.json`, `ipc_nga.geojson` (trimmed), `ipc_nga_national_long_latest.csv`, `hdx_ipc_nogeo_package.json`.

- **Datasets:** each country has its own dataset on HDX, organisation `ipc` ("Integrated Food Security Phase Classification (IPC)"), e.g. https://data.humdata.org/api/3/action/package_show?id=nigeria-acute-food-insecurity-country-data. Dataset names use long country names (`democratic-republic-of-the-congo-`, `state-of-palestine-`, `cote-d-ivoire-`, `united-republic-of-tanzania-`, and both `eswatini-` and an old `kingdom-of-eswatini-`), so **do not build the id from a name**: query by group.
- **CKAN query that finds a country's GeoJSON (decided for Task 4):**
  `GET https://data.humdata.org/api/3/action/package_search?fq=organization:ipc AND groups:<iso3 lower-case>&rows=10&fl=name,license_id,res_name,res_url`
  (URL-encode the `fq`.) Take the package whose `res_name` list holds exactly `ipc_<iso3 lower-case>.geojson` and read that file's URL from the entry of `res_url` at the same position; ignore the other packages. Checked on 2026-11-23: `groups:cod`, `pse`, `sdn`, `civ` return the country package plus `ipc-country-data` (an archive with only `All Countries-2017-2023.xlsx` and no GeoJSON); `groups:ukr` and `groups:mmr` return 0 packages (no IPC data at all: "no areas"). Group codes are ISO3 in lower case. `package_show?id=<name>` gives the same resources with `size` and `last_modified` once the name is known.
- **Coverage on 2026-11-23:** 56 packages in the organisation: 52 country groups have an `ipc_<iso3>.geojson`; the other four packages are `global-acute-food-insecurity-country-data` (group `world`), `ipc-country-data` (the xlsx archive), `lac-region-tri-national-acute-food-insecurity-country-data` (one xlsx, groups GTM, HND, SLV) and the old `kingdom-of-eswatini-acute-food-insecurity-country-data` (2017 to 2024, no GeoJSON, same group `swz` as the current `eswatini-...` dataset that has `ipc_swz.geojson`). **No current country dataset lacks a GeoJSON,** so the no-areas sample (`hdx_ipc_nogeo_package.json`) is the stale Eswatini duplicate, and the "no package at all" case is `groups:ukr`. A reader must choose by resource name, never take the first package.
- **Licence / attribution:** all 56 packages have `license_id` `other-pd-nr` ("Public Domain / No restrictions (CC0)"). `dataset_source`: "National IPC Technical Working Group". The app shows: "Integrated Food Security Phase Classification (IPC), via HDX; National IPC Technical Working Group, analysis <Date of analysis>" (credit is a courtesy, not required).
- **Cadence:** `data_update_frequency` -2 (as needed). Nigeria `last_modified` 2026-10-12; Sudan and Yemen 2026-10-05.
- **Resources per country dataset:** `ipc_<iso3>.geojson` (Nigeria: 795 features, 1,873,647 bytes) and `ipc_<iso3>_{national,level1,area}_{long,wide}[_latest].csv`.
- **GeoJSON feature properties (identical keys on every feature; checked on NGA, SDN, YEM and HTI; no `crs`, no feature `id`, degrees):** `aar_id, confidence_level, ipc_period, overall_phase, population_min, population_percentage_min, justification, prolonged_crisis, population_phase_worse, population_percentage_phase_worse, anl_id, admin_type, title, view_level, hide_settlement_labels, hide_area_labels, hide_area_borders, show_area_popups, color, estimated_population` and `phase1_population, phase1_percent, phase1_color` through `phase5_...`. `overall_phase` is 0 to 5 (0: no classification). `aar_id`, `anl_id`, `estimated_population` and `population_min` are strings; the phase populations are numbers.
- **Which property identifies the analysis and its period (decides P3-2 and Task 4):**
  - **Analysis: `anl_id`** (string). One value per file: Nigeria `87143447` (all 795 features), Sudan `87143417`, Yemen `87143419`, Haiti `87143410`. `aar_id` is the unique id of one area's result (no duplicates), not of the analysis.
  - **Period kind: `ipc_period`**, one letter on every feature: `C` (current) in Sudan (196 areas) and Yemen (118); `P` (projection) in Nigeria (795) and Haiti (30). It does not say first or second projection.
  - **The validity period (from, to) and the date of analysis are NOT in the GeoJSON:** no property carries them. They are in the same dataset's CSVs: `ipc_<iso3>_national_long_latest.csv` (one analysis for the country: `Date of analysis, Country, Total country population, Validity period, From, To, Phase, Number, Percentage`; Nigeria: "Mar 2026", `first projection`, 2026-06-01 to 2026-08-31) and `ipc_<iso3>_area_wide_latest.csv` (one row per area: `Date of analysis`, `Current from/to`, `First projection from/to`, `Second projection from/to`; join on `Area` = GeoJSON `title`).
  - Nigeria: all 736 area rows carry "Mar 2026" and the first projection 2026-06-01 to 2026-08-31; 59 of the 795 GeoJSON titles have no area row; for 531 of the 736 the CSV's Phase 3 count equals `phase3_population` (the rest differ or are blank). Sudan: "Jan 2026", current 2026-02-01 to 2026-05-31, first projection 2026-06-01 to 2026-09-30, second projection 2026-10-01 to 2027-01-31; all 196 titles match; the GeoJSON's `C` areas are the current period, which had **already ended on 2026-11-23**.
  - Consequence for Task 4: the GeoJSON alone cannot tell the period; the country's `ipc_<iso3>_national_long_latest.csv` (or the area CSV) must be fetched with it, its `Date of analysis` compared with the national record R17 uses, and a `P` set matched to "first" or "second" projection through the CSV (Nigeria's lists only the first). If the dates can't be matched, keep R17 and log why. That is a second file per country (`ipc_<iso3>_national_long_latest.csv` next to `ipc_<iso3>.geojson`), found in the same CKAN package, not a field of the GeoJSON.
  - **As built (Task 4, ruling R4):** a country is drawn by area only when its latest national record is current, the dataset's `license_id` is CC0 or public domain, the national CSV's `current` rows give one period equal to the national record's From and To, its `Date of analysis` equals the national record's (trimmed, any case; ruling R10; a blank one on either side means "cannot confirm", R11), and the GeoJSON's `ipc_period = C` areas share one `anl_id`. Only `C` areas are used (a `P` set is never matched to a projection), so Nigeria (P only) keeps R17, while Sudan and Yemen (C sets) can be drawn by area for as long as their national records are not stale (P2-4). The CSV is read first, so the GeoJSON is not downloaded for a period that cannot match. Accepted files are kept in `cache\ipc-areas\` and fetched again only when the national analysis (date of analysis and period) changes; a GeoJSON turned down is tried again after 24 h, a country turned down at the search or CSV stage after 12 h, and the countries that go to HDX in one run are 500 ms apart. Cached areas are parsed off the UI thread at start-up. The step runs inside the IPC feed and reports through its status dot.
- **Other gotchas found:**
  - Geometry types are mixed: Nigeria has 761 Polygon, 13 MultiPolygon and **21 Point** features (`admin_type` still says `polygon`; the points are IDP settlements such as "Bama IDPs"). Never treat a point as an area.
  - Areas with `overall_phase` 0 are present (Nigeria 220 of 795): not shaded, and not "no crisis". Nigeria's phases: 0 (220), 2 (374), 3 (185), 4 (16).
  - Phase colours are in the data (`color`, `phaseN_color`); the app uses its own IPC colours.
  - `title` is unique in each file but is an area name, not a p-code; there are no p-codes (as the plan said).
- **Sample trim (ruling R1, one-off script kept outside the repo):** 40 Nigeria features (2 Point, 2 MultiPolygon, 36 Polygon; `overall_phase` 0 (6), 2 (12), 3 (13), 4 (9)) picked with a fixed random seed; Douglas-Peucker simplification at 0.004 degrees, coordinates rounded to 3 decimals, properties untouched; 62,798 bytes. `tools/fetch-samples.ps1 -Set Plan3` saves the full download to `.downloads/` (git-ignored) but does not trim it.

## 8. FAOSTAT Suite of Food Security Indicators (undernourishment and energy supply; Plan 3, bundled in Task 6)

Checked 2026-11-23 by a download into a scratch folder (nothing in the repo).

- **URL:** https://bulks-faostat.fao.org/production/Food_Security_Data_E_All_Data_(Normalized).zip : 2,199,068 bytes (2.1 MB), `Last-Modified` 2026-11-23. FAO's catalogue (https://bulks-faostat.fao.org/production/datasets_E.xml) lists it as dataset `FS`, "Food Security and Nutrition: Suite of Food Security Indicators", `DateUpdate` 2026-11-23, 286,185 rows.
- **Zip contents:** the normalised CSV (52 MB), `..._AreaCodes.csv`, `..._Elements.csv`, `..._Flags.csv`, `..._ItemCodes.csv`.
- **Licence / attribution:** FAO's database terms (https://www.fao.org/contact-us/terms/db-terms-of-use/en/, read 2026-11-23): datasets are CC BY 4.0 unless their metadata says otherwise; users may not state that FAO has approved or endorsed their use. Citation format: `FAO. [year of last update]. [Name of database: Name of dataset]. Accessed on [date]. [URL] Licence: CC-BY-4.0.` The app shows, for example: "FAO. 2026. FAOSTAT: Suite of Food Security Indicators. Accessed on 23 November 2026. Licence: CC-BY-4.0."
- **Cadence / as-of:** updated several times a year (this file 2026-11-23). The as-of is the row's `Year`, not the file date.
- **Checked columns:** `Area Code, Area Code (M49), Area, Item Code, Item, Element Code, Element, Year Code, Year, Unit, Value, Flag, Note`.
- **Gotchas found:**
  - **Latin-1**, quoted fields. `Area Code (M49)` carries a leading apostrophe (`'004`; Sudan `'729`, Yemen `'887`, "China, mainland" `'156`).
  - `Year` is a single year (`2025`) for annual items and a three-year range text (`2022-2024`, `2023-2025`) for "(3-year average)" items; `Year Code` is `20222024` for the range.
  - Items for the plan: `210041` "Prevalence of undernourishment (percent) (3-year average)" and `21010` "Average dietary energy supply adequacy (percent) (3-year average)"; the newest range is 2023-2025. `Flag` values: E (estimated), X (international organisation figure), O (missing), Q, A, L.
  - Aggregates (regions, income groups) share the `Area` list: drop them (Task 6, ruling P3-11).
- **As built (Task 6, 2026-11-23):** `DataTool fao` writes `data/datasets/fao-food-security/` (`data.csv`, `manifest.json`): `fao.undernourishment` (169 countries) and `fao.energyAdequacy` (160), each country's newest range that has a value. A range such as 2023-2025 is stored with the year 2025 (the `period` column keeps the range). The manifest's `dateMeaning` is `ThreeYearAverage` (fix round 1), so the app shows every figure with its range (hover: "…: 10.9%, average 2020–2022 · FAO"; panel: "2023–2025"), the legend gives the ranges ("2017–2019 to 2023–2025 (each country's latest average)") with a note that some are older, and the legend's source line and the status dot say "3-year averages, latest ending 2025"; never one year. Aggregates are FAO area codes 5000 and up, 351 (China) and 420 (Sub-Saharan Africa): 42 dropped, listed in the manifest. M49 codes map through Natural Earth's `UN_A3`, or `ISO_N3_EH` where `UN_A3` is missing (Norway is -99, Taiwan -099 in 5.1.2); no code was left unplaced. "<2.5" was the only qualifier form seen (1,452 rows of the two items); flags seen: E and O. **Yemen has no undernourishment value in any range** (all flagged O), and **Sudan's newest is 2020-2022** (later ranges O); North Korea's newest is 2017-2019. The download answered HTTP 200 to the app's User-Agent.

## 9. US EIA International bulk file (oil production; Plan 3, bundled in Task 6)

Checked 2026-11-23 with HEAD requests and the reuse page only; the file was not downloaded.

- **URL:** https://www.eia.gov/opendata/bulk/INTL.zip : HTTP 200, 24,714,575 bytes (24.7 MB), `Last-Modified` 2026-11-19. The bulk-files page (https://www.eia.gov/opendata/bulk/) answered 403 to the app's User-Agent, so its text was not read; the zip needs no key.
- **Licence / attribution:** https://www.eia.gov/about/copyrights_reuse.php (read 2026-11-23): US government publications are in the public domain; a user "should use an acknowledgment, which includes the publication date", for example "Source: U.S. Energy Information Administration (Oct 2008)." The app shows: "Source: U.S. Energy Information Administration (<month and year>)". The page warns that material EIA gets from private contributors may be protected; the INTL series are EIA's own.
- **As-of:** the series year; the file's `Last-Modified` (2026-11-19) gives the publication date to quote.
- **As built (Task 6, 2026-11-23):** downloaded with the app's User-Agent (HTTP 200); the bulk page still answered 403 to it and to curl's default. `INTL.txt` is one JSON object per line (105,082 lines). Series `INTL.57-1-<code>-TBPD.A` ("Crude oil including lease condensate production, <country>, Annual", thousand barrels per day): 261. **There is no per-value estimate flag:** every series has `f` "A" and "--" marks no value (651 points). `DataTool eia` keeps each country's newest year with a number, counting a year as full only when the series was last updated after it ended (all 2025 here). It drops region series (a `geography` listing several countries, or a region code such as WP27 whose `geography` is one country's, which would count it twice) and series with no figure in the last 10 years (USSR, Yugoslavia, Serbia and Montenegro), 43 in all, listed in the manifest. EIA's Kosovo code XKS has no figures. 210 countries; 8 codes with no unit in the boundary set (MTQ, GIB, GLP, WAK, USIQ, NLDA, REU, GUF) all produced 0 in 2025: ruling R16 records them in the manifest (under `dropped`) and does not list them "Not placed on the map"; a code with no unit and a figure above 0 would still be listed. Top 5 in 2025: USA 13,662, RUS 9,885, SAU 9,549, CAN 4,964, IRQ 4,388. The panel and legend link https://www.eia.gov/international/ (HTTP 200). The figures are full years, not estimates, so the manifest's `dateMeaning` is "Year" and the screen says "year 2025" (final fix wave C4; it said "estimate year 2025" before).

## 10. WRI Aqueduct 4.0 country rankings (water stress; Plan 3, bundled in Task 6)

Checked 2026-11-23 by a download into a scratch folder (nothing in the repo).

- **URL:** https://files.wri.org/aqueduct/aqueduct-4-0-country-rankings.zip : 12,790,153 bytes (12.8 MB), `Last-Modified` 2023-08-07. Landing page: https://www.wri.org/data/aqueduct-40-country-rankings (dated 16 August 2023). The zip holds one workbook, `Aqueduct40_rankings_download_Y2023M07D05/Aqueduct40_rankings_download_Y2023M07D05.xlsx` (13.8 MB), with sheets `Read Me, country_baseline, country_future, province_baseline, province_future`.
- **Licence (superseded: see "Licence confirmed" below, Task 6):** not confirmed by Task 1 on 2026-11-23, because the Read Me sheet states no licence and WRI's landing pages say only "License: Creative Commons". Task 6 then confirmed CC BY 4.0 on WRI's Open Data Commitment page (ruling R6).
- **Attribution text (Read Me sheet, "Citations"):** "Kuzma, S., M.F.P. Bierkens, S. Lakshman, T. Luo, L. Saccoccia, E. H. Sutanudjaja, and R. Van Beek. 2023. Aqueduct 4.0: Updated decision-relevant global water risk indicators. Technical Note. Washington, DC: World Resources Institute. Available online at: doi.org/10.46830/writn.23.00061." The sheet also credits GADM 3.6 for the boundaries. The data cover the baseline 1979 to 2019 and projections for 2030, 2050 and 2080.
- **Cautions in the Read Me (repeat in the legend's note):** the indicators cannot be measured directly and have not been validated; interbasin transfers are not modelled; the 2023 data should be compared with the 2019 release only with caution.
- **Checked columns of `country_baseline` (the header strings are present in the workbook):** `gid_0, name_0, indicator_name, weight, score, score_ranked, cat, label, un_region, wb_region`; `bws` and weight `Tot` (also `Dom, Ind, Irr`) occur. `gid_0` is ISO3; `label` is the category text (for example "Low (<10%)", "Medium - High (0.6-0.8)").
- **Licence confirmed (Task 6, ruling R6, 2026-11-23):** WRI's Open Data Commitment (https://www.wri.org/about/open-data-commitment) says WRI has adopted the Creative Commons Attribution 4.0 International License as its standard licence for publications and data. The extract is bundled under CC BY 4.0 with WRI's citation.
- **As built (Task 6, 2026-11-23):** `country_baseline` has 2,456 rows: indicators `bws`, `drr`, `rfr`; weights `Tot, Dom, Ind, Irr, Liv, One`. `bws` with `Tot`: 189 countries, every `gid_0` the app's ISO3. Categories and labels: -9999 "NoData" (25), 0 "Low (<10%)" (59), 1 "Low - Medium (10-20%)" (35), 2 "Medium - High (20-40%)" (23), 3 "High (40-80%)" (22), 4 "Extremely High (>80%)" (25). **No country row has cat -1** ("Arid and Low Water Use"), so the legend leaves out the grey arid key (ruling R17b: it shows only when some country has that category); the app's labels for 0 to 4 match WRI's exactly. `DataTool aqueduct` keeps `gid_0, score, cat, label`; the value is `cat` and WRI's label is shown as written; the figures' year is 2019, the end of the 1979-2019 baseline. The figure is a modelled baseline for 1979–2019, published in 2023, so the manifest's date is `dateMeaning` "Baseline" with `dateText` "baseline 1979–2019 (published 2023)", and the legend, source line, status dot, hover and panel all say that, with no year appended (final fix wave I3; it said "estimate year 2019" before). The legend repeats the Read Me's cautions.

## 11. Global Energy Monitor trackers (power plants, renewable plants, oil and gas fields, steel plants; Plan 3, bundled in Task 7)

Built 2026-11-24 from the owner's downloads in `.downloads\gem\` (git-ignored; never committed, never opened except
through the DataTool's ExcelDataReader). GEM's download form is not scriptable, so the owner downloads new releases.

- **Licence (confirmed 2026-11-24):** GEM's licence page https://globalenergymonitor.org/creative-commons-license gives the
  full Creative Commons Attribution 4.0 International Public License (CC BY 4.0); each workbook's About sheet says the same.
  Bundling is allowed with credit, a link to the licence (https://creativecommons.org/licenses/by/4.0/) and a note of the
  changes. Each manifest (`attribution`, `licence`, `licenceUrl`, `changes`, `filter`) and each legend line carries all
  three. The citations used (GEM's recommended form, publisher first, ruling R24):
  - "Global Energy Monitor, Global Integrated Power Tracker, September 2026 release." (the About sheet's text, verbatim;
    it has no "V2": that is the file name's, so the manifest's `version` is "September 2026 release, file V2" and the
    screen dates it "September 2026 release (file V2)", outside the citation)
  - "Global Energy Monitor, Global Oil and Gas Extraction Tracker, March 2026 release." (its About sheet gives no
    recommended citation, only "Global Oil and Gas Extraction Tracker, 2026 release": the same order is used)
  - "Global Energy Monitor, Global Iron and Steel Tracker, September 2026 (V1) release." (the "(V1)" is GEM's own)

  (Before R24 the manifests put the tracker first and "(V2)" inside the citation; the owner's brief had that form.)
- **Non-commercial rows (ruling R22):** the power workbook's About sheet says records with a "TZ ID" in
  `Other IDs (unit/phase)` (entries `TZ: <number>`) come partly or wholly from TransitionZero's Solar Asset Mapper
  (August 2025), licensed **CC BY-NC 4.0**. Non-commercial data is never bundled (C6), so a plant (GEM location ID) with
  any such unit is left out whole. In this release 18,723 plants with operating units carry one, **none of them 500 MW or
  more**, so nothing on the map is lost.
- **Pages (HTTP 200, 2026-11-24):** https://globalenergymonitor.org/projects/global-integrated-power-tracker/ ,
  https://globalenergymonitor.org/projects/global-oil-gas-extraction-tracker/ ,
  https://globalenergymonitor.org/projects/global-iron-and-steel-tracker/ ; each page's download form is at `#download`.
- **Dates:** GEM names a release by month only. The manifests' `dateMeaning` is `Release` with `dateText` the release as
  cited, with a file-only version after it ("September 2026 release (file V2)"), and `published` is the 1st of that month, for ordering only: the screen never
  shows a day ("GEM Global Integrated Power Tracker, September 2026 release (file V2)").
- **Checking a new release:** `DataTool describe <xlsx> [--sheet <name>] [--distinct <column>]` lists sheets, columns,
  row counts and value counts; the extractors refuse a file whose sheet or column is missing, or whose About sheet does
  not name the release's month and year.

### 11a. Power: `DataTool gem-power` → `data/datasets/gem-power/`

- **File:** `Global Integrated Power September 2026 V2.xlsx` (34 MB). Sheets: `About`, `Power facilities` (183,404 unit
  rows, 54 columns), `Regions, area, and countries` (252 rows).
- **Columns used (real names):** `Type, Country/area, Plant / Project name, Unit / Phase name, Capacity (MW), Status,
  Latitude, Longitude, Location accuracy, Other IDs (unit/phase), GEM location ID, GEM unit/phase ID`; from the regions
  sheet `GEM Standard Country Name/Area, ISO-alpha3 Code`.
- **Values seen:** `Type`: utility-scale solar 103,940, wind 35,089, oil/gas 15,203, coal 14,674, hydropower 7,023,
  bioenergy 4,815, nuclear 1,825, geothermal 835. `Status`: operating 128,100, pre-construction 21,923, announced 7,578,
  cancelled 7,209, construction 6,235, retired 4,389, "cancelled - inferred 4 y" 2,989, "shelved - inferred 2 y" 2,809,
  shelved 1,468, mothballed 704. `Location accuracy`: exact, approximate (and 71 "Approximate").
- **Filter (P3-9):** units with `Status` "operating", `Capacity (MW)` summed per `GEM location ID` (108,956 plants);
  plants of **500 MW or more**: **4,113 plants in 118 countries** (under the cap; not raised). A plant's position is its
  first operating unit's; its type the `Type` with the most capacity (82 plants mix types, kept in the `types` column).
  Countries map through the regions sheet's ISO3, then the app's names (GEM's XKX for Kosovo is placed by name as KOS).
  No code was left unplaced. GEM writes "timepoint" rows for converted oil/gas units: no unit has two operating
  timepoints (checked), so nothing is counted twice.
- **Extract columns:** `name, iso3, lat, lon, capacity (MW), unit, status, type`, then `gem_location_id, gem_country,
  units, types, location_accuracy`.
- **On screen:** a disc with a dark edge (R8), filled by its largest fuel's group (R24; the legend says "coloured by its largest fuel", and a plant of several fuels lists each in its tooltip, "coal 5,500 MW, oil/gas 280 MW", from the `types` column): fossil (coal, oil/gas) #E69F00, nuclear #CC79A7,
  hydro #0072B2, wind or solar #F0E442, other (bioenergy, geothermal) #BBBBBB; sized by class (under 1,000 MW,
  1,000–2,499, 2,500–4,999, 5,000 or more), each class and group a legend key.

### 11b. Oil and gas: `DataTool gem-oilgas` → `data/datasets/gem-oilgas/`

- **File:** `Global-Oil-and-Gas-Extraction-Tracker-March-2026.xlsx` (5.4 MB). Sheets: `About`, `Field-level main data`
  (7,673 fields), `Field-level reserves data`, `Field-level production data` (11,834 rows), and project-level sheets
  (999 rows each, not used).
- **Columns used:** main: `Unit ID, Unit Name, Fuel type, Country/Area, Status, Latitude, Longitude, Location accuracy,
  Onshore/Offshore, Production Type`; production: `Unit ID, Fuel description, Quantity (converted), Units (converted),
  Data Year`.
- **Values seen:** `Status`: operating 6,481, discovered 455, mothballed 240, in-development 229, blank 213, abandoned
  21, decommissioning 17, cancelled 10, "underground gas storage" 6, exploration 1. `Fuel type`: oil and gas 4,945, oil
  1,376, gas 1,296, gas and condensate 56. `Units (converted)`: million bbl/y 6,749, million m³/y 5,025, million boe/y
  60. `Data Year` runs from 1975 to 2029.
- **Filter (P3-9, ruling R23):** operating fields with coordinates number 6,055 (426 operating fields have none), over the
  5,000 cap. So the fields GEM reports producing under a threshold are left out, the threshold rising from 0.1 million
  barrels of oil equivalent a year in 0.1 steps: at **0.2 million boe/y**, 1,122 fields go and **4,933 fields in 79
  countries** remain. A field with no usable field-level figure (653 of those kept) stays: GEM reports some of the
  largest only at project level, so dropping the unmeasured would hide giants. A field's production is each fuel's
  newest `Data Year` before the release year (a 2026 or later year, such as Wafra's 2028, is a forecast: not used; a
  row with no year is not used), summed in boe: oil, condensate and NGL in million bbl/y as they are, gas at 6,000 cubic
  feet per boe (1 million m³ = 0.0058858 million boe), GEM's own million boe/y as given.
- **Extract columns:** `name, iso3, lat, lon, capacity (million boe/y, or blank), unit, status, type (Fuel type)`, then
  `gem_unit_id, gem_country, detail` (production as GEM gives it, "oil 518 million bbl/y (2022)"), `production_type,
  onshore_offshore, location_accuracy`.
- **On screen:** a vermillion diamond, one size (years and units differ, and many fields have no figure); the tooltip
  gives GEM's production figures with their year, or "no production figure in GEM's field data".

### 11c. Steel: `DataTool gem-steel` → `data/datasets/gem-steel/`

- **File chosen:** `Plant-level data - Global Iron and Steel Tracker - September 2026 (V1).xlsx`, the only one of the
  three that gives each plant's coordinates with its status and capacity. The `Steel unit data` and `Iron unit data`
  files list furnaces (`Current capacity (ttpa)`, `Unit status`) with no coordinates; the met-coal summary is not needed.
- **Sheets and columns used:** `Plant data` (1,293 plants): `GEM plant ID, Plant name (English), Country/area,
  Coordinates, Coordinate accuracy`; `Plant capacities and status` (1,848 rows, one per plant, status and equipment):
  `GEM plant ID, Status, Nominal crude steel capacity (ttpa), Main production equipment`. `Coordinates` is one text cell,
  "36.7474130, 36.2173300".
- **Values seen:** `Status`: operating 935, announced 288, retired 233, construction 140, "operating pre-retirement" 97,
  cancelled 76, mothballed 72, "mothballed pre-retirement" 7. Capacities are numbers or "N/A", "unknown", ">0".
- **Filter (P3-9):** a plant's `Nominal crude steel capacity (ttpa)` summed over its rows with `Status` "operating" or
  "operating pre-retirement" (still producing; the status says which), divided by 1,000 to give million tonnes a year;
  plants of **1 Mt/yr or more**: **600 plants in 60 countries** (918 plants have an operating steel capacity; 29 kept
  plants sum two rows). Iron-only capacity (a DRI line with no steel) is not counted.
- **Extract columns:** `name, iso3, lat, lon, capacity (Mt/yr), unit, status, type (Main production equipment)`, then
  `gem_plant_id, gem_country, crude_steel_ttpa, coordinate_accuracy`.
- **On screen:** a bluish-green square outline, sized by class (1–2.9, 3–9.9, 10 or more million tonnes a year); the
  tooltip names the furnaces in words ("blast furnace, basic oxygen furnace").

### 11d. Hand checks against the raw files (2026-11-24)

- Bruce nuclear power plant (Canada): units A1–A4 and B5–B8 operating, 868 + 836 + 865 + 868 + 872 + 891 + 872 + 872 =
  **6,944 MW**; C1–C4 (1,000 MW each) announced, not counted. Three Gorges: one row, 22,500 MW, operating.
- Kosovo B: Unit 1 and Unit 2, 340 MW each, operating: **680 MW** (neither unit passes 500 MW alone). Kosovo A: units 3
  to 5 operating (610 MW), units 1 and 2 retired.
- Rumaila (Iraq): oil 518.3 million bbl/y, Data Year 2022. Burgan (Kuwait): "hydrocarbons" 620.5 million boe/y, 2023.
  Ain Dar (Ghawar, Saudi Arabia): oil 63.9 million bbl/y, 2020.
- POSCO Gwangyang: BF; BOF operating 22,999 ttpa (EAF 2,500 under construction, not counted): **23.0 Mt/yr**. Taranto:
  BF; BOF "operating pre-retirement" 7,800 ttpa. Voestalpine Linz: 4,000 operating + 2,000 operating pre-retirement.
  Port Talbot is not in the extract (its blast furnaces are retired).

### 11e. Renewables (ruling R25): `DataTool gem-renewables` → `data/datasets/gem-renewables/`

- **File and columns:** the same power workbook and columns as §11a, with the same citation, licence, R22 rule and
  version handling (`--release "September 2026 release" --file-version V2`).
- **Fuels (GEM's own `Type` names):** `wind`, `utility-scale solar`, `hydropower`, `geothermal`, `bioenergy`. GEM has no
  separate solar thermal Type: "Solar Thermal" is a `Technology` within `utility-scale solar`, so it is included with
  it. Not included: `coal`, `oil/gas`, `nuclear`.
- **Rows:** units of those Types with `Status` "operating", summed per `GEM location ID` (101,731 plants). A plant's
  units of other fuels are not counted here (7 kept plants have some; the Power plants layer counts every fuel). Those units are written to an extra `other_fuels` column (R26, e.g. Drax "oil/gas 96 MW"), and the tooltip says "renewable units only; the plant also has 96 MW of oil/gas"; the legend measure ends "a plant's units of other fuels are not counted". The gem-power extract does not have the column and is unchanged. R22: a
  plant with any TransitionZero unit is left out whole (18,723 plants; none of 200 MW or more).
- **Threshold (P3-9 cap, R25):** tried in turn, counting plants with coordinates: **100 MW: 9,047; 150 MW: 5,379;
  200 MW: 4,021**. Kept at **200 MW**: **4,020 plants in 109 countries** (one plant in Réunion, 212 MW, has no unit in
  the boundary set and is listed "Not placed on the map"). The manifest's `thresholds` records each step and its count.
- **By largest Type:** wind 1,429, utility-scale solar 1,271, hydropower 1,265, bioenergy 41, geothermal 14. Largest per
  fuel: Three Gorges (hydropower, 22,500 MW), Gansu–Shandong Huanxian wind farm (4,501 MW), Xinjiang Ruoqiang solar farm
  (4,000 MW), Drax (bioenergy 2,580 MW: its oil/gas units are not counted here), Makban geothermal (362 MW).
- **Overlap:** 1,058 of these plants are 500 MW or more, so they also appear in the Power plants layer when both are on;
  the legend says so.
- **On screen:** a filled hexagon with a dark edge (no other layer has it, C8), in the power layer's fuel colours by the
  plant's largest fuel; legend keys for the fuel groups present (hydro, wind or solar, other) and size classes (under
  500 MW, 500–999, 1,000–4,999, 5,000 or more); the tooltip as the power layer's.

## 12. ACAPS Humanitarian Access Dataset via HDX (Plan 4; live only, never committed)

Checked live 2026-11-24. Samples: `tests/Samples/hdx_acaps_package.json` (the real package, trimmed to its two newest resources) and `acaps_access_synthetic.json` / `.xlsx` (**synthetic, ACAPS layout**, no ACAPS data; ruling P4-2).

- **URL:** https://data.humdata.org/api/3/action/package_show?id=acaps-humanitarian-access-dataset (dataset id `b0e0bf77-29a3-46ee-9f78-1830e59c43d1`, `dataset_source` "ACAPS"). `resources[0]` is the newest: https://data.humdata.org/dataset/b0e0bf77-29a3-46ee-9f78-1830e59c43d1/resource/2b5fed9e-83d9-410e-b590-934e5be5dda3/download/acaps-humanitarian-access-dataset_dec_2025.xlsx (1,160,458 bytes, HTTP 200, no key). Ten XLSX resources, July 2021 to December 2025.
- **The resource fields that name the edition:** the file name at the end of `url` (`acaps-humanitarian-access-dataset_dec_2025.xlsx`), `name` ("ACAPS' Humanitarian Access Dataset_December 2025.xlsx") and `description` ("ACAPS' Humanitarian Access Dataset - December 2025"). File names and `name`s are not uniform across editions (`..._dec-_2024.xlsx`, `...-july_2025.xlsx`, and the 2021 and 2022 files are named `2021xxxx_acaps_...`), while `description` always writes the edition in words.
- **`last_modified` does not order the editions.** On 2026-11-24 the July 2025 resource's `last_modified` (2026-08-03T13:01:23) is three seconds *later* than the December 2025 resource's (13:01:20), and December 2022's (13:01:43) is later still, because HDX re-saved several resources together. The `resources` order (`position` 0 is December 2025, 1 is July 2025, and so on) is newest first. **Ruling P4-3 says "newest by `last_modified`": that would pick July 2025 today.** Pick the edition from `description` or the file name (newest first), not from `last_modified`.
- **Package fields:** `license_id` `cc-by`, `license_title` "Creative Commons Attribution International (CC BY)"; package `last_modified` 2026-08-03; `dataset_date` `[2020-12-15T00:00:00 TO 2025-12-31T23:59:59]`; `data_update_frequency` 180; `update_status` `needs_update`. HDX notes: "updated twice a year, each edition covering a six-month reporting period."
- **Newer edition not on HDX:** the ACAPS overview page (https://www.acaps.org/en/thematics/all-topics/humanitarian-access-overview) shows the "July 2026" edition (December 2025 to May 2026, 66 countries, PDF dated 10 August 2026). Its XLSX is not on HDX. The app shows the December 2025 edition and says so (P4-3).
- **Workbook sheets:** `About`, `pillars` (147 rows), `indicators` (147), `trends` (386), `dictionary` (11), `log`, `list` (386), `csv` (83 rows). The `About` sheet's text is in a drawing (text boxes), not in cells, so a cell reader sees it empty.
- **`csv` sheet columns (11, checked):** `Crisis_name, Crisis ID, P1, P2, P3, ACCESS, Info_gap, Trend, Country level, Country Name, ISO`. Scores are real numbers; `Info_gap` runs 0 to 1 (for example 0.0611); `Trend` is Stable (36), Deteriorating (18), Improving (15) or New (14); all 83 rows have `Country level` "Yes"; `ISO` is "ISO3 alpha code" (the dictionary's words). `ACCESS` counts: 0 (6), 1 (19), 2 (21), 3 (18), 4 (11), 5 (8).
- **`indicators` sheet columns (14, checked, spacing exactly as in the file; `3.Impediments...` has no space after the dot):** `Crisis Name, Crisis ID, 1. Denial of existence of needs, 2. Restriction and obstruction to access services, 3.Impediments to entry into the country, 4.Restriction of movement within the country, 5.Interference with implementation of humanitarian activities, 6. Violence against personnel, facilities and assets, 7. Ongoing insecurity, 8. Presence of mines and explosive devices, 9. Environmental constraints, Information Gap, Country level, Country`. No ISO column: join on `Crisis ID` (its first three letters are the ISO3, for example `AFG001`). The sheet's 147 rows include crises that are not the country row (for example `AFG007` "2025 Earthquake in Eastern Provinces", `Country level` "No").
- **Scale wording (the `About` sheet's own words):** "Indicators are scored from 0 to 3, where 3 represents the highest level of constraint." and "Pillars and the overall access score are expressed from 0 to 5": 5 Extreme access constraints, 4 Very high access constraints, 3 High access constraints, 2 Moderate access constraints, 1 Low access constraints, 0 No constraints. **Every ACCESS level has a name, and 0 is "No constraints".** The indicator scale (0 to 3) has only the one sentence above: no names for 0, 1 or 2. The `dictionary` sheet gives the codes only (`0-5`, Integer).
- **The nine indicators as the overview page words them (quoted, 2026-11-24):**
  1. "Denial of existence of humanitarian needs or entitlements to assistance"
  2. "Restriction and obstruction of access to services and assistance"
  3. "Impediments to enter the country"
  4. "Restriction of movement within the country"
  5. "Interference into implementation of humanitarian activities"
  6. "Violence against humanitarian personnel, facilities, and assets"
  7. "Insecurity or hostilities affecting humanitarian assistance"
  8. "Presence of landmines, improvised explosive devices, explosive remnants of war and unexploded ordnance"
  9. "Physical constraints in the environment"

  Pillars: 1 "Access of people in need to humanitarian aid" (indicators 1 and 2), 2 "Access of humanitarian responders to people in need" (3 to 6), 3 "Physical, environmental and security constraints" (7 to 9). The page's scale sentence: "each indicator is given a score from 0-3 and X when there is an information gap", and a "final score on a scale of 0-5". The workbook's short column names differ from the page's wording; show the page's wording with the workbook's score unchanged.
- **Licence (two statements that conflict):** HDX says CC BY. ACAPS's copyright page (https://www.acaps.org/copyright-policy) says all ACAPS-branded products "are available under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International licence". It prescribes no credit wording; the footer reads "(c) ACAPS 2026". The Terms of service (https://www.acaps.org/terms-service) state no data licence. Treated by the stricter one (P4-2): fetched live from HDX only, never committed; credit "ACAPS, Humanitarian Access Dataset, <edition>, via HDX (CC BY on HDX; ACAPS: CC BY-NC-ND 4.0)". A permission request is drafted in `docs/permission-requests.md` section 5 (not sent).
- **Samples:** the real `About` sheet names a staff member's email address; it is copied nowhere. The synthetic workbook has `indicators` and `csv` only (the real file's order) and 14 invented crises: the 12 countries SDN, PSE, YEM, AFG, COD, HTI, MMR, SOM, UKR, SSD, NGA, VEN, `XXK` (Kosovo, an ISO3 the boundary set does not have) and `AFG007` with `Country level` "No". Values are invented. Made from the JSON by `dotnet run --project tools/EarthRelief3D.DataTool -- workbook tests/Samples/acaps_access_synthetic.json --out tests/Samples/acaps_access_synthetic.xlsx`.

## 13. OCHA FTS requirements and funding via HDX (Plan 4)

Checked live 2026-11-24. Samples: `tests/Samples/hdx_fts_package.json` (untrimmed), `fts_requirements_funding_global.csv` (real, trimmed).

- **URL:** https://data.humdata.org/api/3/action/package_show?id=global-requirements-and-funding-data; the resource `fts_requirements_funding_global.csv` is https://data.humdata.org/dataset/b2bbb33c-2cfb-4809-8dd3-6bbdc080cbb9/resource/b3232da8-f1e4-41ab-9642-b22dae10a1d7/download/fts_requirements_funding_global.csv (280,855 bytes, updated daily). The package has seven resources; the cluster and incoming-funding files are not used.
- **Columns (12, checked):** `countryCode, id, name, code, typeId, typeName, startDate, endDate, year, requirements, funding, percentFunded`. UTF-8, CRLF line endings, 3,889 rows (1999 to 2031). `countryCode` is ISO3; `requirements` and `funding` are USD; `percentFunded` is a whole percent.
- **A blank `id`:** 2,635 rows. They read `name` "Not specified" with blank `code`, `typeId`, `typeName`, dates, `requirements` and `percentFunded`, and a `funding` figure: money given to the country that is not tied to a plan. It is not an appeal and has no percent, so it is ignored for appeal funding. Years after the current one (2027 to 2031) exist only as such rows.
- **2026:** 166 rows, 57 with a plan id; `typeName` blank (109), "Regional response plan" (28), "Humanitarian needs and response plan" (20), "Flash appeal" (5), "Other" (4). 2025 for AFG, SDN and HTI: one plan row and one "Not specified" row each.
- **Licence and credit:** HDX `cc-by-igo`; every FTS page footer: "Except where otherwise noted, content on this site is licensed under a Creative Commons BY 4.0 International license." FTS terms (https://fts.unocha.org/content/terms-use): data may be used "provided that suitable attribution of FTS as data source is given. Such attribution should cite the Financial Tracking Service (FTS), the publication date of the figures used and the relevant URL." The credit the app shows: "Financial Tracking Service (FTS), <date>, <URL>"; if amended or combined, "based on Financial Tracking Service (FTS), <date>, <URL>". The FTS logo is not used.
- **Credit (as the source registry holds it):** "Financial Tracking Service (FTS), <date>, https://fts.unocha.org" (`<date>` is the figures' publication date; the URL is FTS's home page, because the app reads the whole file, not one page).
- **Sample:** all 166 rows of 2026 and the six 2025 rows of AFG, SDN and HTI (172 rows), as published (bytes unchanged).

## 14. OCHA country-based pooled funds and CERF (Plan 4)

Checked 2026-11-24 (the plan's source check read the pages; this task re-read the CBPF footer). No sample: the curated `pooled-funds.json` cites the pages.

- **Portal licence:** https://cbpf.data.unocha.org/ footer: "Except where otherwise noted, content on this site is licensed under a Creative Commons Attribution 4.0 International license". The OData API (https://cbpfapi.unocha.org/vo2/odata/Poolfund?$format=json, 46 rows, no "active" flag) states no licence of its own.
- **Credit:** "OCHA country-based pooled funds and CERF, https://cbpf.data.unocha.org/" (wording chosen by the app; the source prescribes none). Each fund row also cites its own OCHA page.
- **Giving:** there is no per-fund donate page. https://www.unocha.org/our-funding explains how to give; https://cerf.un.org/donate is CERF's page ("for U.S. tax-deductible gifts" go through the UN Foundation).
- **Extract (Plan 4 Task 6):** `data/help/pooled-funds.json`: OCHA's 20 fund pages plus `cerf`, all drafts checked 2026-11-24. A regional fund covers its CBPF sub-funds' countries (Asia-Pacific: BGD, FJI, LAO, NPL, PAK, PNG, PHL, SLB, LKA, TON, VUT; West and Central Africa: BFA, TCD, MLI, NER; Latin America and the Caribbean: COL, SLV, GTM, HTI, HND; Eastern and Southern Africa: KEN, UGA); the Syria Cross-border fund maps to SYR, which has two funds. Each row's citation is its OCHA page; `docs/help-review.md` lists them for the owner.

## 15. NGA World Port Index (Pub 150; Plan 4)

Checked 2026-11-24. Sample: `tests/Samples/wpi_excerpt.csv` (100 real rows, all 118 columns, header unchanged).

- **Portal:** https://msi.nga.mil/Publications/WPI and https://msi.nga.mil/ still answer HTTP 503 (under maintenance). **Portal in maintenance on 2026-11-24; file date 2026-11-05.** No licence statement and no edition number could be read. The maintenance page links https://msi.nga.mil/apology_objects/WPI.zip (HTTP 200, 377,921 bytes, one file `UpdatedPub150.csv`, dated 2026-11-05). Re-check when the portal is back.
- **Basis for use:** a US federal government work (17 U.S.C. section 105); not confirmed from NGA's own page (secondary sources only).
- **Credit:** "US National Geospatial-Intelligence Agency (NGA), World Port Index (Pub 150), public domain." (wording chosen by the app; the source prescribes none).
- **File:** UTF-8 with BOM, CRLF, 3,809 data rows, 118 columns. Used: `Main Port Name`, `UN/LOCODE`, `Country Code` (holds the country **name**, for example "Myanmar (Burma)"), `Harbor Size` (US spelling), `Latitude`, `Longitude`, `World Port Index Number` (written "8225.0"). `Harbor Size`: Very Small 2,109; Small 1,023; Medium 376; Large 174; blank 127, where "blank" is one space. 460 rows have no `UN/LOCODE`, also written as one space, among them 2 Large and 9 Medium. Some LOCODEs have no space ("HTPAP", "NGBON"). Coordinates are long decimals ("38.983333000000002").
- **Sample:** Large 24, Medium 34, Small 13, Very Small 17 and blank 12; 11 rows with an empty `UN/LOCODE`; includes ports of Sudan, Yemen, Fiji, Haiti, Somalia, Ukraine, Nigeria and Venezuela. Written back with the BOM and CRLF.
- **Extract (Task 4, `DataTool ports WPI.zip`, run 2026-11-24 on the 377,921-byte zip, CSV dated 2026-11-05):** `data/datasets/wpi-ports`: 546 ports in 108 countries (Large 174, Medium 372) of the 550 Large or Medium rows; 4 are not placed (Christmas Island, Guadeloupe, Martinique, Gibraltar: no unit in the boundary set). The country is the first two letters of the UN/LOCODE (536 ports); 10 ports with no LOCODE use the country name. Its date is the zip entry's, as-of 2026-11-05.

## 16. OurAirports (Plan 4)

Checked 2026-11-24. Sample: `tests/Samples/ourairports_excerpt.csv` (200 real rows).

- **URL:** https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv (12,763,634 bytes, 86,387 rows, rebuilt daily).
- **Columns (19, checked):** `id, ident, type, name, latitude_deg, longitude_deg, elevation_ft, continent, iso_country, iso_region, municipality, scheduled_service, icao_code, iata_code, gps_code, local_code, home_link, wikipedia_link, keywords`. `iso_country` is ISO2; `scheduled_service` is "yes" or "no" for every type. Types: small_airport 42,787; heliport 23,294; closed 13,688; medium_airport 4,108 (2,117 scheduled); seaplane_base 1,274; large_airport 1,174 (1,153 scheduled); balloonport 62.
- **Licence:** the repository's LICENSE (https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/LICENSE) begins "This is free and unencumbered software released into the public domain." (The Unlicense). Credit given anyway: "OurAirports (ourairports.com), public domain (The Unlicense)". The data is community-edited. ourairports.com had an expired certificate on 2026-11-24 and was not used.
- **Sample (header unchanged; every type, both `scheduled_service` values):** large 51, medium 101, small 21, heliport 8, closed 8, seaplane_base 8, balloonport 3. All large and medium airports of AF (10 rows), SD (10), FJ (10, including the fields either side of 180 degrees) and IS (13); 17 of NO, 15 of them above 70 degrees N; 52 of US. Re-written by a script (quoting only where needed, LF line endings).
- **Extract (Task 4, `DataTool airports airports.csv`, run 2026-11-24):** `data/datasets/ourairports`: 3,254 airports in 223 countries (large 1,144, medium 2,110) of the 3,270 that are large or medium with scheduled service; 16 are not placed (ten codes: BQ, CC, CX, GF, GI, GP, MQ, RE, UM, YT). The file has no date, so the as-of is the download day, 2026-11-24.

## 17. US Senate member contact list (Plan 4)

Checked 2026-11-24. Sample: `tests/Samples/senators_cfm_excerpt.xml` (6 members of ME, MD and WI, real; the entries are copied as published).

- **URL:** https://www.senate.gov/general/contact_information/senators_cfm.xml (52,652 bytes, 100 `member` elements). Per member: `member_full, last_name, first_name, party, state, address, phone, email` (the value is a contact-form URL, not an email address), `website, class, bioguide_id`; the root also holds `last_updated` (2026-11-18T12:08-05:00).
- **Terms:** Senate.gov privacy policy (https://www.senate.gov/general/privacy.htm): information on the site "is considered public information" and "may be distributed or copied unless otherwise specified"; "Use of appropriate byline/photo/image credits is requested." A US government work. Credit: "United States Senate, senate.gov, <last_updated date>".
- **House of Representatives:** https://www.house.gov/representatives/find-your-representative is a link only. Its FAQ: "There is currently no restriction on a link being posted to the Find Your Representative page."
- **Extract (Plan 4 Task 6, `DataTool senators senators_cfm.xml`, run 2026-11-24):** `data/countries/US/senators.json`: 100 senators in 50 states (two each), `asOf` 2026-11-18 (the file's `last_updated`). Kept per senator: state code, name as "First Last", party letter, the contact form (the `email` element) and the website, all https; the street address, phone, class and bioguide id are not kept. The download is untrusted (a DOCTYPE is refused).

## 18. UN Humanitarian Response Depots and the US State Department contact page (Plan 4)

Both sites answer scripts with HTTP 403. They were opened as ordinary pages in the desktop app's built-in browser on 2026-11-24 (no challenge or CAPTCHA appeared). Nothing is fetched by script.

- **UNHRD** (https://unhrd.org/contacts): the home page says "Our network of five strategically located humanitarian support hubs". The contacts page gives each hub's postal address, and staff contact details that are not copied anywhere in this repository. The menu has no per-hub pages, and the pages show no coordinates and no date. Citation for each depot row: title "Contacts | UNHRD - Humanitarian Response Depot", publisher "UNHRD (WFP)", URL https://unhrd.org/contacts, checked 2026-11-24 (the page is undated). Hubs as the page writes them:
  1. Accra: World Food Programme, UNHRD Accra, Courier Enclave, P.O. Box 1423, Kotoka International Airport, Accra, Ghana.
  2. Brindisi: World Food Programme, UNHRD Brindisi, Ex SS 16 Km 906.9 OR, P.za Del Vento 1, 72100 Brindisi, Italy.
  3. Dubai: UNHRD c/o WFP, Dubai Humanitarian, Dubai Industrial City - Phase 2, Nearest Landmark - DWC, PO Box 506003 (United Arab Emirates).
  4. Kuala Lumpur: World Food Programme, UNHRD Kuala Lumpur, Lot 14104 (PT12136), Jalan TUDM, Section U7, 40150 Shah Alam, Selangor, Malaysia.
  5. Panama City: World Food Programme, UNHRD Panama, Paseo de la Americas Panama Pacifico, Howard, Arraijan, Panama, Republic of Panama.
- **Depot positions (ruling R21, final fix wave):** UNHRD gives no coordinates, so each depot is marked at the airport its own address names or sits beside, with the airport's position from the bundled OurAirports extract (`data/datasets/ourairports`, The Unlicense, as of 2026-11-24): Accra → Kotoka International (DGAA; the address names it); Brindisi → Brindisi Airport (LIBR; the address is on the former SS16 by the airport); Dubai → Al Maktoum International (OMDW, IATA DWC; "Nearest Landmark - DWC"); Kuala Lumpur → Sultan Abdul Aziz Shah International, Subang (WMSA; Jalan TUDM, the air force road); Panama → Panamá Pacífico International (MPPA, the former Howard air base; "Panama Pacifico, Howard"). Each position is approximate. No share-alike data (C6) is used for them.
- **US State Department:** www.state.gov's "Contact Us" link goes to https://register.state.gov/contactus/contactusform (title "U.S. Department of State - Contact Us"). It lists topic contacts (emergencies abroad, passports, visas and more) and ends with a "Contact Us Form" for inquiries "unrelated to the topics above"; it warns "we cannot guarantee we will be able to respond to all inquiries." That URL is the profile's State Department contact, noting that the form is at the end of the page.
- **Credit, UNHRD:** "UNHRD (WFP), Contacts | UNHRD - Humanitarian Response Depot, https://unhrd.org/contacts" (wording chosen by the app, from the citation above; the source prescribes none). Licence: no statement was read on the pages (they were read for facts, not for a licence), so the registry says so.
- **Credit, official contact links (House finder, State Department, UN Blue Book):** "US House of Representatives, Find Your Representative (house.gov); US Department of State, Contact Us; United Nations, The Blue Book" (wording chosen by the app; the sources prescribe none; links only, nothing is copied).

## 19. Logistics Cluster country pages (Plan 4; links only)

Checked 2026-11-24 (curl gets HTTP 403; read through the web-fetch tool). `data/help/logistics-cluster.json` has one row per page opened: `{ iso3, url, title, checkedOn }` (19 rows; `title` is the page's HTML title).

- **List:** https://logcluster.org/en/where-we-work lists 218 country links. Only the 18 with an operation slug (`/en/where-we-work/<iso3>/<slug>`) plus Myanmar's `/mmr` were opened and recorded; the rest are bare `/<iso3>` pages. A country without a row has no Logistics Cluster link in the app.
- **Terms** (https://logcluster.org/en/terms-of-use, as the plan's check read them): "You agree not to use or distribute the information for commercial purposes", and the Logistics Cluster must be credited. Links only; nothing is copied.
- **Credit:** "Logistics Cluster, https://logcluster.org/en/where-we-work" (wording chosen by the app, to meet the terms' "must be credited"; the source prescribes no wording).

## 20. IRS Form 990 e-file bulk files: Range requests (Plan 4; Task 7 relies on this)

Checked 2026-11-24 with `curl -H "Range: bytes=0-99" https://apps.irs.gov/pub/epostcard/990/xml/2026/2026_TEOS_XML_01A.zip` (the project's User-Agent; the 100 bytes were discarded).

- **Result: HTTP 206 Partial Content**, `Accept-Ranges: bytes`, `Content-Range: bytes 0-99/71497607`, `Content-Length: 100`, `Content-Type: application/zip`, `Last-Modified: Thu, 19 Feb 2026 18:32:24 GMT`. The zip is 71,497,607 bytes, so the vetting tool can read a zip's central directory and one member by byte ranges instead of downloading the whole file.
- **Credit (IRS Tax Exempt Organization Search: Publication 78 and the revocation list; Form 990 e-file data):** "US Internal Revenue Service (IRS), Tax Exempt Organization Search (Publication 78, revocation list) and Form 990 e-file data, https://apps.irs.gov/app/eos/" (wording chosen by the app; the source prescribes none). Licence basis: a US federal government work (17 U.S.C. section 105); not confirmed from the IRS's own page.

## 21. Credits for links, curated tables and the globe picture (Plan 5)

Written 2026-11-25 so that every row of `data/sources.json` has its credit in this document. No data is fetched for these.

- **Charity evaluators (Charity Navigator, https://www.charitynavigator.org/, and BBB Wise Giving Alliance, https://give.org/):** the app links to their pages and says which rating a charity must have to be listed (`docs/help-review.md`); no rating is copied. **Credit:** "Charity Navigator (charitynavigator.org) and BBB Wise Giving Alliance (give.org): links only, ratings are not copied" (wording chosen by the app; the sources prescribe none). Checked 2026-11-24.
- **Curated tables (`data/curated`: designations, camp sites, explainers; `data/help`):** written for this app from cited documents; every row carries its own citation. **Credit:** "Each curated entry cites its own source documents" (wording chosen by the app). Licence: the app's own, MIT. Checked 2026-11-24.
- **Globe picture, NASA Blue Marble** (GlobeCore, `external/GlobeCore/THIRD-PARTY-NOTICES.txt`): **Credit:** "NASA Earth Observatory, Blue Marble Next Generation with Topography and Bathymetry (July 2004, R. Stockli)", from GlobeCore's notices, which add "NASA imagery is generally not copyrighted (NASA Media Usage Guidelines)". Read 2026-11-25.

## Offline samples (`--offline-samples`)

The app's diagnostic handler (`src/EarthRelief3D.App/Diagnostics/OfflineSamples.cs`) maps each live URL to a file in
`tests/Samples`; anything else is a 404. `OfflineSamplesTests` runs the six data feeds (the four sources, the two needs feeds,
and the IPC feed with its area step) through it and checks that they are every data feed the app has, so a URL that
changes in a feed, or a new feed with no sample, fails a test.

| Live URL | Sample file |
|---|---|
| https://ucdp.uu.se/downloads/ | `ucdp_downloads.html` |
| https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv | `ucdp_candidate_monthly.csv` |
| https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv | `ucdp_candidate_quarterly.csv` |
| https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data | `hdx_ipc_package.json` |
| the `ipc_global_national_wide_latest.csv` resource URL named in that package | `ipc_national_wide_latest.csv` |
| https://www.who.int/api/news/diseaseoutbreaknews (any query) | `who_don_synthetic.json` (synthetic, no WHO text) |
| https://api.unhcr.org/population/v1/population/?year=2026&coo_all=true&limit=500 | `unhcr_population_coo.json` |
| https://ghoapi.azureedge.net/api/WSH_WATER_BASIC, `.../WSH_SANITATION_BASIC`, `.../UHC_INDEX_REPORTED` | `gho_wsh_water_basic.json`, `gho_wsh_sanitation_basic.json`, `gho_uhc_index.json` (synthetic) |
| https://data.humdata.org/api/3/action/package_show?id=global-hpc-hno | `hdx_hno_package.json` |
| the `hpc_hno_2026.csv` resource URL named in that package | `hpc_hno_2026.csv` |
| https://data.humdata.org/api/3/action/package_search (the IPC area search, any country) | inline: no package (as HDX answers for `groups:ukr`), so every country keeps R17 offline |
| https://api.github.com/repos/nvkelso/natural-earth-vector/releases/latest (Borders) | inline: v5.1.2, the bundled edition, so nothing is downloaded |

Not through the handler, and the same in every run, offline too: the bundled extracts (FAO, US EIA, WRI Aqueduct; sections
8 to 10; and GEM's power, oil and gas, and steel points, section 11) load from `data/datasets` beside the program, and the curated designations and camp sites from `data/curated`.

The Nigeria area samples (`hdx_ipc_nga_package.json`, `ipc_nga.geojson`, `ipc_nga_national_long_latest.csv`) are not served
offline: their analysis is a projection (`ipc_period` P), as is Nigeria's record in the national sample, so R4 could
never match it and an offline picture would show nothing more. `IpcAreaFeedTests` uses them, with a current-period
variant made in the test, and `FoodAreaMapTests` checks the drawing pixel by pixel on small synthetic areas (Plan 3
ruling R19).

The samples are from Nov 2026, so offline runs use `--display-time 2026-11-22T12:00:00Z`. The WHO sample has no report for
Sudan, so Sudan's panel shows conflict, food crisis and displacement (three crisis sections), then the Plan 3 sections
(basic needs, people in need, resources); that is what the samples hold. The GHO samples are synthetic (Plan 3 ruling
R5), so the water, sanitation and health-care figures in offline runs and pictures are invented.

## Boundaries (Natural Earth, updatable; Plan 2b)

- **Source:** Natural Earth 1:50m countries, land boundary lines and admin-1 (states and provinces), public domain. The program
  ships edition 5.1.2 (2022-05-13) and, once a day, asks `https://api.github.com/repos/nvkelso/natural-earth-vector/releases/latest`
  (GlobeCore's `BoundaryUpdateFeed`, the "Borders" status dot). Only a newer tag is downloaded (the three GeoJSON files from
  `raw.githubusercontent.com`) and only after it passes validation; the last good set stays in
  `%LOCALAPPDATA%\EarthRelief3D\cache\boundaries\<tag>\`. Natural Earth's latest release is still v5.1.2 (checked 2026-11-22),
  so today nothing is downloaded.
- **Credit:** "Natural Earth (https://www.naturalearthdata.com/), public domain" (wording follows GlobeCore's notices, `external/GlobeCore/THIRD-PARTY-NOTICES.txt`, which give "Natural Earth (https://www.naturalearthdata.com/), 1:50m coastline and land boundary lines. Public domain."; not a wording Natural Earth prescribes).
- **Download size:** adopting a new Natural Earth release downloads about 6.2 MB (countries 3.08 MB, lines 0.76 MB, admin-1
  2.33 MB), once per release; the daily check is a few KB. A rejected release is not downloaded again (until the app's
  rules or probes, or GlobeCore, change), and after a failed download the next try waits 6 hours.
- **Rules (`data/boundary-rules.json`)** are the owner's boundary decisions, applied to every set that is loaded: Crimea and
  Sevastopol move to Ukraine (UN General Assembly resolution 68/262; the Perekop line goes) and Somaliland (SOL) joins
  Somalia (C16). Every rule has a reason. A set the rules do not fit exactly is rejected.
- **Probes (`data/boundary-probes.json`)** are points that must stay in their country (Khartoum, Kyiv, Crimea, Sevastopol,
  Fiji on both sides of the date line, Hargeisa, Gaza). **`data/ucdp-country-names.json`** lists the UCDP country strings
  the app has met; with `data/country-aliases.json` they must all still resolve. A set that fails any of these is not used,
  the Borders dot turns amber with the reason and the log has it.
- **Aliases** (`data/country-aliases.json`) stay as the app's name matching (they are not boundary policy). `Serbia (Yugoslavia)` is UCDP's own spelling of Serbia (it is in the Candidate GED files' `country` column), added when `ucdp-country-names.json` was built from those files by `tools/get-ucdp-country-names.ps1`.
- **On screen:** the legend's note names the edition and the rules in use, e.g. "Borders: Natural Earth 5.1.2 (2022-05-13);
  Crimea shown as part of Ukraine (UN General Assembly resolution 68/262); Somaliland shown as part of Somalia (UN humanitarian data). The boundaries and names shown on this map do not imply official endorsement or acceptance by the
  United Nations."
