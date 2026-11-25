# Plan 4: second batch of charity drafts (OR-4, OR-5), 2026-11-25

Drafted by Claude on 2026-11-25 for the owner's review. Every fact below comes from a file or a page opened on 2026-11-25.
Nothing here is approved: the seven new entries are drafts with no rating, hidden in the app until the owner confirms an
evaluator (P4-8, OR-3). No charitynavigator.org or give.org page was opened by any tool (the vetting tool refuses those
hosts; links to them seen on planusa.org were not followed).

## Result

| Charity (new draft) | Why drafted | EIN (source) | Countries listed | Programme share (Form 990) |
|---|---|---|---|---|
| American Red Cross | OR-5 (owner named); OR-4 Movement count 17 | 53-0196605 (Pub 78 name match, American National Red Cross; given by the owner) | 4: COL, MOZ, TCD, VEN | 90.8% (2024) |
| CARE USA | OR-4: CARE confederation 14 | 13-1685039 (care.org FAQ and page footer; Pub 78 name "C A R E, Inc.") | 14 | 89.8% (2024) |
| Doctors of the World USA | OR-4: Médecins du Monde network 13 | 35-2426718 (its home page) | 12 | 96.6% (2024) |
| Doctors Without Borders USA | OR-5 (owner named); OR-4: MSF sections 14 | 13-3433452 (its page footer) | 23 | 83.5% (2024) |
| Oxfam America | OR-4: Oxfam confederation 13 | 23-7069110 (its home page) | 13 | 71.2% (2025) |
| Plan International USA | OR-4: Plan International 13 | 13-5661832 (Pub 78 name match; planusa.org links to a GuideStar profile with this EIN) | 12 | 68.9% (2024) |
| USA for UNHCR | OR-4: UNHCR 16 (name variants merged) | 52-1662800 (its page footer) | 16 | 66.3% (2025) |

All seven are in IRS Publication 78 with code **PC** (file of 2026-11-11) and none is on the auto-revocation list (file of
2026-11-17). The `legal` and `programme` values were copied from the vetting report unchanged. Programme shares are facts
from each charity's own Form 990, not a ranking.

The list now has 16 charities (9 + 7). Dropped (no draft): World Vision, Danish Refugee Council, ACTED, FAO, WHO, Première
Urgence Internationale, Solidarités International; Caritas was not merged (and Catholic Relief Services is not in Pub 78).

## Step 1 (OR-4): the 3W count with movements merged

**Data.** `.downloads/hdx/hdx_hapi_operational_presence_global.csv` (HDX HAPI 3W, downloaded 2026-11-24, 40,187 rows), read
with `python -I` from the session scratchpad; nothing in `.downloads` was run. Current period: `reference_period_end` on or
after 2025-11-24 (37,908 rows). Crisis countries (R15, `has_hrp` or `in_gho` true): the same 19 as the first batch: AFG, BFA,
CAF, CMR, COD, COL, ETH, HTI, MLI, MOZ, NER, NGA, SDN, SOM, SSD, SYR, TCD, VEN, YEM (LBN and PAK have only older rows). Bar:
13 countries.

**Method.** Each row's `org_name` and `org_acronym` (accents removed, lower case) were matched against an ordered list of
rules, one per organisation, then distinct countries were counted. Two kinds of merge:
- *Movement merges* (OR-4): branches, sections, affiliates and country offices of one international movement count as one
  organisation (listed below).
- *Spelling merges* (as the first batch did): one organisation written differently, mostly Somalia and Syria rows written
  "Name (ACRONYM)", Spanish names in Colombia and Venezuela, French names, and acronym-only names in Burkina Faso.

The 3W periods in the evidence titles are the earliest start and latest end of the merged organisation's rows in that
country; for the first nine charities this reproduces the first batch's 138 3W titles exactly.

**Corrections made after auditing every merged group's members:**
- "Street Child Organization" [SC] (NGA) and "Street Child United Kingdom" [SC-UK] (COD) were caught by the Save the Children
  acronym rule; removed (Save the Children stays at 15).
- "Kurdish Red Crescent (KRC)" (SYR) removed from the Red Cross and Red Crescent Movement: it is not a recognised component
  of the Movement (Syria still counts through the Syrian Arab Red Crescent, the Palestine Red Crescent Society and the Qatar
  Red Crescent Society).
- "Organisation Centrafricaine pour le Développement Economique et Social" [OCADES] (CAF) removed from Caritas (Burkina
  Faso's OCADES is Caritas Burkina; the CAF one is unrelated).
- PAHO rows (COL, HTI, VEN) count with WHO: PAHO is WHO's regional office for the Americas and the rows name both.

### Merged counts (organisations in 9 or more of the 19 countries)

| Organisation (merged) | Countries | Which |
|---|---|---|
| Norwegian Refugee Council | 19 | all 19 |
| Action Against Hunger | 18 | all but SSD |
| IOM | 18 | all but BFA |
| UNICEF | 18 | all but ETH |
| International Rescue Committee | 17 | all but MOZ, SSD |
| Red Cross and Red Crescent Movement | 17 | all but AFG, MOZ |
| WFP | 17 | all but ETH, VEN |
| Caritas (sensitivity only, not used) | 16 | BFA, CAF, CMR, COD, COL, ETH, HTI, MLI, MOZ, NER, NGA, SDN, SYR, TCD, VEN, YEM |
| Danish Refugee Council | 16 | AFG, BFA, CAF, CMR, COD, COL, ETH, MLI, NER, NGA, SDN, SOM, SYR, TCD, VEN, YEM |
| FAO | 16 | AFG, BFA, CAF, CMR, COD, HTI, MLI, MOZ, NER, NGA, SDN, SOM, SSD, SYR, TCD, YEM |
| UNHCR | 16 | AFG, BFA, CAF, CMR, COD, COL, ETH, MLI, NER, NGA, SDN, SOM, SYR, TCD, VEN, YEM |
| World Vision | 16 | AFG, BFA, CAF, COD, COL, ETH, HTI, MLI, MOZ, NER, SDN, SOM, SSD, SYR, TCD, VEN |
| Save the Children | 15 | AFG, BFA, COD, COL, ETH, HTI, MLI, MOZ, NER, NGA, SDN, SOM, SYR, VEN, YEM |
| Solidarités International | 15 | AFG, BFA, CAF, CMR, COD, HTI, MLI, MOZ, NER, NGA, SDN, SSD, SYR, TCD, YEM |
| WHO / PAHO | 15 | AFG, CAF, CMR, COD, COL, HTI, MLI, MOZ, NER, NGA, SDN, SYR, TCD, VEN, YEM |
| ACTED | 14 | AFG, BFA, CAF, COD, ETH, HTI, MLI, MOZ, SDN, SOM, SSD, SYR, TCD, YEM |
| CARE | 14 | AFG, CMR, COD, COL, ETH, HTI, MLI, MOZ, NER, SDN, SOM, SYR, TCD, YEM |
| MSF | 14 | AFG, BFA, CAF, CMR, COD, HTI, MLI, MOZ, NER, NGA, SDN, SYR, TCD, YEM |
| Première Urgence Internationale | 14 | AFG, BFA, CAF, CMR, COD, ETH, MLI, NER, NGA, SDN, SYR, TCD, VEN, YEM |
| UNFPA | 14 | AFG, CAF, CMR, COD, HTI, MLI, MOZ, NGA, SDN, SOM, SYR, TCD, VEN, YEM |
| International Medical Corps | 13 | AFG, CAF, CMR, COD, ETH, MLI, NGA, SDN, SOM, SYR, TCD, VEN, YEM |
| Médecins du Monde / Doctors of the World | 13 | AFG, BFA, CAF, CMR, COD, ETH, HTI, MLI, NER, NGA, SYR, VEN, YEM |
| Oxfam | 13 | BFA, CAF, COD, COL, ETH, MLI, MOZ, NER, NGA, SOM, SYR, TCD, YEM |
| Plan International | 13 | CAF, CMR, COL, ETH, HTI, MLI, MOZ, NER, NGA, SDN, SOM, SYR, TCD |
| Humanity & Inclusion | 12 | AFG, BFA, CAF, ETH, HTI, MLI, MOZ, SDN, SYR, TCD, VEN, YEM |
| INTERSOS | 12 | AFG, BFA, CAF, CMR, COD, MLI, NER, NGA, SDN, SYR, TCD, YEM |
| ADRA | 11 | CMR, COD, HTI, MLI, MOZ, SDN, SOM, SYR, TCD, VEN, YEM |
| ALIMA | 11 | BFA, CAF, CMR, COD, ETH, HTI, MLI, NER, NGA, SDN, TCD |
| Catholic Relief Services | 11 | AFG, BFA, CAF, COD, HTI, MLI, NER, NGA, SDN, SYR, TCD |
| Concern Worldwide | 11 | AFG, BFA, COD, ETH, HTI, NER, SDN, SOM, SYR, TCD, YEM |
| Mercy Corps | 11 | AFG, COD, COL, ETH, HTI, MLI, NER, NGA, SDN, SYR, YEM |
| Islamic Relief | 9 | AFG, ETH, MLI, NER, SDN, SOM, SYR, TCD, YEM |
| Welthungerhilfe | 9 | AFG, BFA, CAF, COD, HTI, MLI, NER, SDN, SYR |
| COOPI (Cooperazione Internazionale) | 9 | CAF, COD, ETH, MLI, NER, SDN, SOM, SYR, TCD |

### Every merge (3W names as written, with their countries)

Movement merges:
- **MSF** (14): Doctors Without Borders France [MSF-F] (CAF, COD, HTI, TCD); Doctors Without Borders Spain [MSF-E] (CAF, HTI,
  NER, TCD); Doctors Without Borders Switzerland [MSF-CH] (COD, NER, TCD, YEM); Doctors Without Borders [MSF] (AFG, CMR, SDN);
  Doctors Without Borders Belgium [MSF-B] (HTI, MLI); Doctors Without Borders Holland [MSF-H] (HTI, TCD); MSF France (BFA);
  MSF Suisse (BFA); MSF OCB (MOZ); Medecin Sans Frontiere Operational Centre Geneva [MSF OCB] (NGA); Medecins Sans Fontieres
  - Belgium (MSF-BE), - France (MSF-FR), - Holland (MSF-Holland) (SYR); Medecins Sans Frontieres - Spain [MSF Spain] (SDN);
  Médecins Sans Frontière West Africa and Central Africa [MSF-WaCA] (NER); Médecins sans frontières West and Central Africa
  [MSF WaCA] (TCD); Médecins Sans Frontières Belgique [MSF-Belgique] (TCD); Médecins Sans frontrière Espagne [MSF-E] (MLI).
- **Red Cross and Red Crescent Movement** (17): International Committee of the Red Cross [ICRC] (CAF, CMR, HTI, MLI, NGA,
  TCD), International Committee of the Red Cross (ICRC) (SOM), CICR (BFA), Comité Internationale de la Croix-Rouge [CICR]
  (COD); International Federation of Red Cross and Red Crescent Societies [IFRC] (TCD, and CMR as "The International
  Federation..."); national societies: Croix Rouge Luxembourgeoise [CRLUX] (MLI, TCD), Aide Internationale de la Croix Rouge
  Luxembourgeoise [AICRL] (TCD), Croix-Rouge Française [CRF] (CMR, TCD), Croix Rouge Camerounaise (CMR), Croix Rouge
  Centrafricaine (CAF), Croix Rouge Espagnole (HTI), Cruz Roja Espanola and Spanish Red Cross (SDN), Croix Rouge Tchadienne
  (TCD), Croix-Rouge Nigérienne (NER), Cruz Roja Colombia (COL), Cruz Roja Venezolana (VEN), Ethiopian Red Cross Society
  (ETH), German Red Cross (SDN), Haitian Red Cross (HTI), Netherlands Red Cross (HTI), Mali Red Cross (MLI), Nigerian Red Cross
  Society (NGA), Palestine Red Crescent Society (SYR), Qatar Red Crescent (SYR, SOM, YEM), Somali Red Crescent Society (SOM),
  South Sudan Red Cross (SSD), Sudanese Red Crescent Society (SDN), Syrian Arab Red Crescent (SYR), Yemen Red Crescent
  Society (YEM). Excluded: Kurdish Red Crescent (SYR).
- **Oxfam** (13): OXFAM (BFA, COL, ETH, MOZ, SOM, TCD, YEM); OXFAM (OXFAM) (SOM); OXFAM GB (COD); OXFAM Intermón (CAF); OXFAM
  International (NGA), OXFAM International (OXFAM) (SYR); OXFAM Netherlands (NOVIB) (SOM); Oxfam Intenational Niger (NER);
  Oxford Committee for Famine Relief [OXFAM] (MLI).
- **CARE** (14): CARE International (CMR, COD, COL, MLI, MOZ, TCD, YEM); Care International (CARE) (SOM, SYR); Care
  International in Afghanistan (AFG); CARE International in Sudan (SDN); CARE International Switzeralnd (SDN); Cooperative
  for Assistance and Relief Everywhere [CARE] (ETH, NER) and "...Every where" (ETH); Cooperative For Assistance And Relief
  Everywhere Haiti (HTI).
- **World Vision** (16): World Vision International (AFG, BFA, CAF, COD, COL, ETH, HTI, MLI, MOZ, NER, SDN, SSD, TCD); World
  Vision International (WVI) (SOM, SYR); Wold Vision International (COD); World Vision Japan (SYR); World Vision South Sudan
  (SSD); WV - World Vision Venezuela (VEN).
- **Save the Children** (15): Save the Children International (AFG, BFA, COD, ETH, HTI, MLI, MOZ, NER, NGA, SDN, VEN, YEM);
  Save The Children International (SCI) (SOM); Save the Children (SCI) (COL); Save The Children (STC) (SYR).
- **Médecins du Monde / Doctors of the World** (13): Médecins du Monde [MDM] (AFG, CAF, NGA, YEM); Médecins du Monde -
  Belgique / MDM-BE (COD, MLI, NER); Médecins du Monde France (COD); Médecins du Monde Suisse (HTI, CMR); Medecins Du Monde -
  Germany (ETH); Medecins Du Monde Argentine (HTI); MdM-Espagne (BFA); MdM Spain - Médicos del Mundo Spain (VEN); Medicos del
  Mundo (MdM) - Spain, Doctors of the World (MdM), Doctors of the World - France (Medecins du Monde) (SYR).
- **Plan International** (13): Plan International [PI] (CAF, CMR, ETH, HTI, MLI, MOZ, NGA, SDN); Plan International Niger
  (NER); PLAN INTERNATIONAL TCHAD (TCD); Plan Internacional (COL); PLAN International (PI) (SOM); Plan International (PLAN)
  (SYR).
- **Action Against Hunger** (18): Action against Hunger [ACF] (12 countries); Action against Hunger - Spain (MLI, NER, SDN);
  Action against Hunger - US (AFG); ACF-France (MOZ); Action Contre La Faim - France (YEM); Action Contre la Faim (SDN); ACH -
  Fundación Acción Contra el Hambre (VEN); Action Against Hunger (ACF) (SOM); Action Against Hunger (AAH) (ACF) (SYR).
- **Humanity & Inclusion** (12): Handicap International - Humanity & Inclusion; Humanity & Inclusion; Humanity and Inclusion
  (HI) (SYR); Handicap International Federation (SDN); HI - Handicap International / Humanity & Inclusion (VEN).
- **ADRA** (11): Adventist Development and Relief Agency; ADRA (SOM, SYR); Agence Adventiste d'Aide et de Développement
  (MLI); ADRA VEN (VEN).
- **Islamic Relief** (9): Islamic Relief Worldwide (AFG, SDN, TCD; SOM as "(IRW)"); Islamic Relief Niger, Mali, Ethiopia,
  Yemen, Syria; Islamic Relief Worldwide - Syria. ("Islamic Relief Agency Organization" (SDN) is a different organisation and
  was not merged.)
- **Caritas** (16, sensitivity only): diocesan and national Caritas bodies (Bukavu, Bunia, Goma, Kasongo, Kongolo, Mahagi,
  Uvira, RDC; Beira, Nacala, Pemba; Haiti, Nigeria, Sudan (Sudanaid), Tchad, Venezuela, Carúpano, Syria, Mali/Mopti,
  Colombiana, BELACD, CODAS Cameroun, CADEV Niger), European members (Switzerland, Austria, Germany, Belgium), "Caritas", and
  OCADES (BFA).

Spelling merges (one organisation, several spellings):
- UNHCR (16): UN Refugee Agency (12 countries); United Nations High Commissioner for Refugees (UNHCR) (SOM, SYR); Alto
  Comisionado de las Naciones Unidas para los Refugiados (ACNUR) (COL); ACNUR - United Nations High Commissioner for Refugees
  (VEN). Without these it would be 12, which is why the first batch did not reach it.
- IOM: + OIM (COD), Organización Internacional para las Migraciones (COL), the "(IOM)" form (SOM, SYR). UNICEF: + Fondo de las
  Naciones Unidas para la Infancia (COL), "(UNICEF)" form (SOM, SYR). WFP: "(WFP)" form (COL, SOM, SYR). UNFPA, FAO, IMC, PUI,
  Solidarités, Mercy Corps, INTERSOS (+ "Internazional SOS" in COD), Welthungerhilfe (+ WHH-AAA), Concern (CONCERN, Concern
  World Wide): the "(ACRONYM)" form in SOM/SYR and small variants. IRC: + Comité Internacional de Rescate (COL, VEN). NRC: +
  Conseil Norvégien pour les Réfugiés (COD, NER), Consejo Noruego (COL), NRC - Norwegian Refugee Council (VEN). DRC: + Consejo
  Danés (COL). WHO: + World Health Organization (WHO) (SYR), PAHO/WHO (HTI), OPS (VEN), OPS/OMS (COL). Missed by my rules
  and noticed in the audit: Burkina Faso's acronym-only rows "OMS" (probably WHO; WHO would be 16) and "CRBF" (probably the
  Burkina Faso Red Cross; BFA already counts for the Movement). Neither changes any outcome (WHO has no US public charity). ALIMA: + ALIMA-KEOOGO-SOS MEDECINS BF (BFA),
  ALIMA/BEFEN (NER), AMCP SP ALIMA (MLI), Alliance for Medical Action (ETH), ALIMA / Alerte Santé (TCD).

### Organisations that clear 13 and are not among the first nine

| Organisation | Count | US public charity | Outcome |
|---|---|---|---|
| Red Cross and Red Crescent Movement | 17 | American National Red Cross 53-0196605, PC | drafted once, under OR-5 rules (below) |
| Caritas (if merged) | 16 | Catholic Relief Services is the US member, but CRS is not in Pub 78 under its own name (no row for "Catholic Relief Services") | not merged, not drafted (ruling S2 below) |
| Danish Refugee Council | 16 | Pub 78 does list "Danish Refugee Council USA" (33-1679900, Washington DC, PC) | dropped: drc.ngo (home, Contact, Partners and donors) neither names a US affiliate nor links to a US donate page, and a web search found no site or donate page of DRC USA's own (same reason as the first batch) |
| FAO | 16 | none in Pub 78 | dropped (same as first batch) |
| UNHCR | 16 | United States Association for UNHCR 52-1662800, PC | **drafted: USA for UNHCR** |
| World Vision | 16 | the only "World Vision International" row (95-3202116, Monrovia CA) has code **GROUP**; World Vision, Inc. (the US office) has no row under its own name ("World Vision Foundation", Federal Way WA, 48-1265565, is SOUNK) | dropped (same reason as first batch, re-checked) |
| Solidarités International | 15 | none in Pub 78 | dropped (same as first batch) |
| WHO / PAHO | 15 | none ("WHO Foundation" has no row) | dropped |
| ACTED | 14 | Pub 78 lists "Friends of Acted Inc." (26-4240190, Washington DC, PC) | dropped: its site acted-us.org (home, About, Our program) has no donate page, no country pages and no statement of its tie to ACTED; acted.org could not be opened by script (certificate error) and was not needed |
| CARE | 14 | C A R E, Inc. 13-1685039, PC | **drafted: CARE USA** |
| MSF | 14 | Medecins Sans Frontieres USA Inc. 13-3433452, PC | **drafted once: Doctors Without Borders USA** (also OR-5) |
| Première Urgence Internationale | 14 | none in Pub 78 | dropped (same as first batch) |
| Médecins du Monde | 13 | Doctors of the World-Usa Inc. 35-2426718, PC | **drafted: Doctors of the World USA** |
| Oxfam | 13 | Oxfam-America Inc. 23-7069110, PC | **drafted: Oxfam America** |
| Plan International | 13 | Plan International USA Inc. 13-5661832, PC | **drafted: Plan International USA** |

The first nine all still clear 13 under the merged count (International Medical Corps exactly 13).

## Step 2 (OR-5): the owner's two charities

- **Doctors Without Borders USA**: country pages on its own site `doctorswithoutborders.org/what-we-do/where-we-work/<country>`.
  Each was opened in the built-in browser (the site answers scripts with a "Checking connection" page; the browser passed it
  without any action of mine and no CAPTCHA appeared). Each page has "Our work in <country>" and "How we're helping in
  <country>" in the present tense. Kept: all 19 R15 countries, plus LBN, MMR, PSE, UKR.
- **American Red Cross**: redcross.org has no current country pages for international work. It has an International Services
  section with a Haiti page (past work, dated 2020-2021) and a Ukraine crisis page (dated 2024); neither shows current work,
  so neither is used. Its 2026 news pages about its own work in a country were used: Colombia (earthquake response; staff based
  in the country and responders deployed), Venezuela (responders deployed, funds to the IFRC appeal), Chad (it funds and
  staffs the measles-rubella platform), and the International Services Activity Update of 20 March 2026 (a contribution to
  the IFRC appeal for the Mozambique floods). Left out: Sudan (2024 article), Myanmar (2025 article), Haiti and Ukraine (above),
  Madagascar and Sri Lanka (in the same update; not R15 countries, and not sure it is current crisis work).
- OCHA 3W rows were added for MSF in the 14 countries where MSF's sections are in 3W; none for the American Red Cross (its
  name never appears in 3W).

**Ruling S1 (extra countries for OR-5).** The brief allows a country outside the 19 only if the charity's own page shows
current crisis work and the country is a crisis in the app's data. I took "a crisis in the app's data" to mean a 2026
country plan of type "Humanitarian needs and response plan" or "Flash appeal" in the app's FTS appeal data (the
`fts_requirements_funding_global.csv` the app's feed reads, as downloaded 2026-11-24): this is the `has_hrp` test of R15
applied outside 3W, and P4-12 shows "Your one small step" for appeal countries. Those countries are HND, LBN, MDG, MMR, NPL,
PSE, UKR (plus the 19). MSF kept LBN, MMR, PSE, UKR (present-tense work on the page); left out HND (the page describes
general health care, not the drought the appeal is for), MDG (latest work described is 2025) and NPL (past responses,
2015 onwards). Cost if wrong: four MSF rows the owner can delete.

**Ruling S2 (Caritas).** Not merged as a movement: its 3W rows are mostly diocesan and national church agencies (national
NGOs in 3W), its US member Catholic Relief Services does not carry the Caritas name, and CRS has no Publication 78 row of its
own, so a merged count could not produce a draft anyway. Cost if wrong: none for this batch.

## Step 3: evidence per charity (all pages opened 2026-11-25; titles copied as shown)

The note on each entry (shown in `docs/help-review.md`, never in the app) carries these disclosures for the owner.

**USA for UNHCR (16).** Affiliate evidence: its FAQ describes it as a nonprofit raising funds and awareness to support UNHCR.
Own emergency pages (`unrefugees.org/emergencies/...`): AFG, CAF, ETH, SDN, SYR, VEN, YEM. UNHCR's own country pages
(`unhcr.org/where-we-work/countries/...`): BFA, CMR, COD, COL, MLI, NER, NGA, SOM, TCD. The unrefugees.org pages opened
(home, About, FAQ, emergency pages) do not link to unhcr.org: said in the note (R17). Its Sahel crisis page names BFA, MLI,
NER and TCD but was not used, as UNHCR's dedicated country pages exist. Both sites refuse scripts; opened in the built-in
browser. Donate link `https://www.unrefugees.org/donate/` forwards to a form titled for monthly giving (said in the note).

**CARE USA (14).** Own pages `care.org/our-work/where-we-work/<country>/` for all 14 3W countries (titles "CARE's
humanitarian work in <country> - CARE"); several show 2026 participant counts. Affiliate evidence: CARE International's
"How we're run" page lists CARE USA among its members; care.org's CARE International Offices page gives
care-international.org. 3W rows name CARE International or its country offices, not CARE USA.

**Doctors of the World USA (12).** Its site has regional pages only (Africa, Asia, Americas & The Caribbean, Middle East),
each saying it works in the region and naming countries under "Explore some of our work" without a description per country.
Kept: AFG (Asia), BFA, CAF, COD, ETH, MLI, NER, NGA (Africa), HTI, VEN (Americas), SYR, YEM (Middle East). Left out: CMR (in
3W, not named on its site). Affiliate evidence: its home page says it is backed by the Médecins du Monde international
network. This is the weakest country evidence in the batch: said in the note.

**Doctors Without Borders USA (23).** Above. Note includes the BBB caution (a give.org page seen earlier this week by a
sub-agent listed MSF USA as not meeting BBB standards; not re-checked; the owner should check Charity Navigator).

**Oxfam America (13).** Own pages on oxfamamerica.org: ETH, SYR, YEM. oxfam.org (Oxfam International) country pages: BFA,
CAF, COD, COL, MLI, MOZ, NER, NGA, SOM, TCD; oxfamamerica.org links to oxfam.org from its home page and its "Countries we work
in" page, and oxfam.org's "How We Are Organized" page lists Oxfam America (USA) among current affiliates. Oxfam America's
own Colombia page is a list of older stories with no statement of current work, so oxfam.org's Colombia page was used instead.

**Plan International USA (12).** Affiliate evidence: planusa.org's "Who we are" page says Plan International USA is the U.S.
member of the global organization. planusa.org has regional pages only, so country evidence is Plan International's
country-office pages on plan-international.org ("Plan International <country>"): CAF, CMR, COL, ETH, HTI, MLI, MOZ, NER,
NGA, SDN, SOM, TCD. Left out: SYR (in 3W; plan-international.org has no Syria page, its address redirects to the news page).
The planusa.org pages opened do not link to plan-international.org: said in the note.

**American Red Cross (4).** Above.

## Step 4: vetting tool

`dotnet run --project src/EarthRelief3D.Vetting -c Release -- check --today 2026-11-25`: 16 charities checked; 0 expiring;
0 broken links; 57 links blocked to scripts (report `artifacts/vetting/vetting-report-2026-11-25.md`, not committed). All
seven new charities: Pub 78 listed, PC; not revoked; programme shares as in the table. The tool flags each name difference
("check that it is the same organisation"): American National Red Cross, C A R E Inc., Doctors of the World-Usa Inc.,
Medecins Sans Frontieres USA Inc., Oxfam-America Inc., Plan International USA Inc., United States Association for Unhcr;
each EIN was confirmed on the charity's own site except the American Red Cross and Plan International USA (see notes).

Blocked to the tool's script (all opened fine in the built-in browser or by my own script earlier today):
- American Red Cross: 6 (website, donate page, all 4 evidence pages). redcross.org answered my own script with 200 earlier today.
- Plan International USA: 2 (website, donate page).
- USA for UNHCR: 18 (website, donate page, all 16 evidence pages; unrefugees.org and unhcr.org).
- The other 31 are the first batch's Save the Children US and UNICEF USA links, as before.

## Pages that refused or challenged

- Scripts got a bot page ("Just a moment..." or "Checking connection", HTTP 403) from planusa.org, unrefugees.org,
  unhcr.org and doctorswithoutborders.org; each was opened as a normal page view in the built-in browser.
- **Blocked:** donate.planusa.org (where planusa.org/donate/ forwards) showed "Just a moment..." in the built-in browser too; I
  stopped there. The donate URL in the JSON is `https://planusa.org/donate/`; the form behind it was not seen.
- doctorsoftheworld.org, care-international.org and acted.org failed certificate checks for my Python script (incomplete
  certificate chains); the first two were opened in the built-in browser; acted.org was not needed.
- No CAPTCHA was solved or bypassed. A cookie banner on planusa.org and acted-us.org was left untouched.

## Tests and review document

- `HelpTablesTests.EveryRowIsCited` (the shipped-data test) now pins 16 charities and allows a country with only the
  charity's own page for the two OR-5 charities; every other country still needs both a 3W row and the charity's own page.
- `docs/help-review.md` regenerated (16 charities).
- Full suite: 592 passed, 0 failed; build 0 warnings.

## Concerns for the owner

1. **Doctors of the World USA's country evidence is a list of country names on regional pages**, not country pages. If that is
   not enough, remove it or ask for a stricter rule.
2. **Evidence on the international organisation's site with no link from the US site**: USA for UNHCR (9 countries on
   unhcr.org) and Plan International USA (all 12 on plan-international.org). Each affiliate states its tie on its own site.
3. **USA for UNHCR newly qualifies because UNHCR's Spanish and Somalia/Syria name variants were merged** (16 countries; 12
   without). This settles the first batch's owner item about USA for UNHCR versus International Medical Corps: both now clear 13.
4. **MSF USA and BBB**: the note asks the owner to check Charity Navigator rather than rely on BBB.
5. **American Red Cross** is listed in only 4 countries, from 2026 news pages, one of them (Mozambique) a page covering several
   countries. Its Red Cross Movement 3W count (17) is the ICRC's, the IFRC's and other societies' presence, not its own.
6. **Ruling S1** (extra countries from FTS 2026 response plans and flash appeals) and **ruling S2** (Caritas not merged) are
   judgement calls.
7. Danish Refugee Council USA and Friends of ACTED are in Publication 78 as PC but have no donate page or country pages of
   their own that I could find; if the owner knows their sites, they can be drafted by the same checks.

## Fix pass (2026-11-25, after the controller's review)

Every page below was opened in the built-in browser on 2026-11-25. No charitynavigator.org or give.org page was opened in
this pass; no bot check was met. EINs did not change, so the vetting tool was not rerun.

### OR-6: the US entity's own statement that it belongs to the international organisation

- **USA for UNHCR**: kept all 16. Its FAQ (`https://www.unrefugees.org/about-us/frequently-asked-questions/`) says USA for
  UNHCR is UNHCR's independent U.S. national partner, raising funds and awareness for UNHCR's work. The 9 unhcr.org countries
  (BFA, CMR, COD, COL, MLI, NER, NGA, SOM, TCD) count on that statement. The About and FAQ pages do not link to unhcr.org (the
  note says so).
- **Plan International USA**: kept all 12. `https://planusa.org/who-we-are/` says Plan International USA is the U.S. member
  of the global organization; the page does not link to plan-international.org (note says so).
- **Oxfam America**: kept 10, removed 3. Its Keeping people safe page (`https://www.oxfamamerica.org/about-us/safeguarding/`)
  says Oxfam America is part of the Oxfam confederation, made up of more than 20 affiliate organisations. The old note was
  wrong: neither the home page nor "Countries we work in" links to oxfam.org (corrected in the note). Each oxfam.org country
  page was read for current work. Rule used: the page describes Oxfam's work there in the present tense and nothing dates
  it to before 2020.
  - Kept: CAF, COD, MOZ, NER, SOM (rewritten pages: "Oxfam's Role"); MLI (looks ahead to 2026-2030); COL (present tense
    but undated, and its newest linked item is a 2019 report; the weakest, said in the note).
  - Removed (both rows, own page and 3W): **TCD** (2018 Human Development Index ranking, refugee figures of that era),
    **BFA** (2018 HDI ranking), **NGA** (2018 HDI ranking and Oxfam's 2019 inequality index). None shows later work.
  - ETH, SYR and YEM stay on oxfamamerica.org's own pages.

### OR-7: Doctors of the World USA

Its home page says it is "backed by the Médecins du Monde international network". The photos on its regional pages link
to its own country pages (`doctorsoftheworld.org/projects/<country>/`), and each of those pages describes the network's work in
that country. All 12 kept, each now citing its country page: AFG, BFA, CAF, COD, ETH, HTI, MLI, NER, NGA, SYR, YEM. For VEN the
Americas page's Venezuela photo links to a blog post of 25 January 2024 about Doctors of the World Spain's malaria work in
Sucre state since 2021 (there is no Venezuela project page). Cameroon still has no page and stays out. These pages are old:
where they give dates, the work they describe is from 2021 to 2023 (Venezuela January 2024), and none describes work in 2025
or 2026. OR-7 asks only that a page describes the work, so they are kept and this is said in the note.

### American Red Cross, Mozambique

The page was opened again. It is the International Services Activity Update of 20 March 2026, and it says the American Red
Cross gave $300,000 to the IFRC Emergency Appeal for the Mozambique Red Cross. The evidence title now reads "Helping All Around
the World: American Red Cross contribution to the IFRC Mozambique flood appeal (multi-country International Services Activity
Update, 20 Mar 2026)".

### Ratings, owner actions, approvals

- All 16 charities now have one rating record: evaluator Charity Navigator, report `https://www.charitynavigator.org/ein/<EIN>`,
  checked 2026-11-25 by "Claude in the built-in browser, owner supervising". The source is the controller's supervised
  checks. No star level is written anywhere in the repo.
- `ownerAction` dropped from all 16, because validation does not require it. The CN and give.org profile links are kept for the owner.
- The first nine are approved (`approved`, approvedOn 2026-11-25): Action Against Hunger USA, International Medical
  Corps, International Rescue Committee, Norwegian Refugee Council USA, Save the Children US, UNICEF USA, USA for IOM, USA for
  UNFPA, World Food Program USA. Validation accepted all nine (no "approved but would be hidden"). The seven second-batch
  charities stay drafts.
- Left as they were (not in the brief): `reviewedOn` of the first nine stays 2026-11-24, so the app shows "Reviewed 24 Nov 2026"
  and the review expires a day earlier than if it were the approval date. `reviewer` stays "Claude (draft)" on the approved rows.

### Tests and documents

- `docs/help-review.md` regenerated.
- `HelpTablesTests.EveryRowIsCited` now pins the nine approved and seven drafts, one Charity Navigator rating each and no
  ownerAction. `HelpPanelTests.HelpTablesLoadedFromTheShippedFilesShowTheFundAndTheApprovedCharities` (was
  `...AndNoCharities`): Sudan now lists the nine approved charities alphabetically and no longer says "No vetted charities
  listed yet". With drafts shown, the four second-batch drafts that work in Sudan appear, labelled as drafts. The
  `AppStartupTests` Sudan check was updated to match. Draft behaviour is still covered by the fixture tests.
- Full suite: 592 passed, 0 failed. Release rebuild: 0 warnings, 0 errors.
