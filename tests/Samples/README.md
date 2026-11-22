# Test samples

Captured by `tools/fetch-samples.ps1` on 2026-11-22 (except the WHO file, which is hand-made). Never edit these by hand;
re-run the script. Retrieval date for every line below: 2026-11-22.

- `ucdp_downloads.html`: https://ucdp.uu.se/downloads/ ; licence CC BY 4.0 (stated on the page); the whole page, untrimmed.
- `ucdp_candidate_monthly.csv`: https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_0_10.csv (UCDP Candidate GED, October 2026 monthly release); CC BY 4.0; all columns, header kept, rows trimmed to `country` = Sudan, Ukraine, Myanmar (Burma), DR Congo (Zaire), Haiti (457 of 2,418 rows; "South Sudan" excluded). Re-written by PowerShell `Export-Csv` (quoting only where needed).
- `ucdp_candidate_quarterly.csv`: https://ucdp.uu.se/downloads/candidateged/GEDEvent_v26_01_26_09.csv (UCDP Candidate GED, quarterly cumulative, January to September 2026); CC BY 4.0; same trim (4,041 of 16,633 rows).
- `hdx_ipc_package.json`: https://data.humdata.org/api/3/action/package_show?id=global-acute-food-insecurity-country-data (HDX CKAN `package_show`); CC0 / public domain (`other-pd-nr`); untrimmed.
- `ipc_national_wide_latest.csv`: the resource `ipc_global_national_wide_latest.csv` of that package (https://data.humdata.org/dataset/7a7e7428-b8d7-4d2e-91d3-19100500e016/resource/2e4f7475-105b-4fae-81f7-7c32076096b6/download/ipc_global_national_wide_latest.csv); CC0 / public domain; untrimmed (50 countries).
- `unhcr_population_coo.json`: https://api.unhcr.org/population/v1/population/?year=2026&coo_all=true&limit=500 (year 2026 has 206 items, so 2026 was captured, not 2025); UNHCR Refugee Data Finder terms, CC BY 4.0 (not re-verified from the API response); untrimmed.
- `who_don_synthetic.json`: **SYNTHETIC, invented by hand, contains no WHO text** (WHO content is CC BY-NC-SA and is never committed, ruling P2-5). Same JSON shape as https://www.who.int/api/news/diseaseoutbreaknews (OData object, `value` array); 6 items: a multi-country title, "Côte d'Ivoire", "Türkiye", "occupied Palestinian territory", an en dash separator, and one dated 2025-12-01 (older than 90 days before 2026-11-22). Licence: ours (MIT, with the repo).
