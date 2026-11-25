# Curated designations and camp sites: for the owner's review

Generated from `data/curated/*.json` by `CuratedTablesTests.ReviewDocumentIsCurrent` (run the tests with
`ER3D_WRITE_REVIEW=1` to rewrite it). Do not edit by hand: correct the JSON, then regenerate.

Every row starts as a **draft** (ruling P3-4). The app shows drafts only with the diagnostic `--show-drafts`,
labelled "Draft: not yet reviewed". To approve a row, set its `review` to
`{ "status": "approved", "approvedOn": "<date>" }` in the JSON. To correct it, change the JSON (or tell Claude what to change).

How the app uses a row (C14):
- The label always names the authority, what kind of authority it is, and the date.
- Provisional measures, arrest warrants and pending cases say they are not verdicts.
- A country gets a triangle only from a court judgment, UN body finding or government determination whose
  continuing statement is dated within the last 12 months. Provisional measures, warrants and pending cases never draw
  one on their own; when the country has a triangle, those with a recent statement are listed with it, and when it
  has none, under "interim steps: not on the map". The rest is listed under the panel's history.
- A court judgment whose outcome is "not established" is never drawn and never counts as a crisis; the panel lists it
  under "Cases decided: not established".
- A camp keeps its marker unless its newest cited report says it closed (`reportedClosed`, ruling R14b); a closed camp
  is listed as history. Where sources differ on a camp's status, each statement is shown with its source (`statusNotes`).

Totals: 31 designations, 5 camp sites, 5 UN depots, 8 explainers, 1 government(s), 9 candidates awaiting confirmation or not entered (last section).

## Governments offered in Settings (C15)

- **United States government** (`USA`): curated completely: yes; checked 23 Nov 2026.
  Scope: Secretary of State determinations published on state.gov and its archives, 2001 onward. Not listed: four earlier genocide conclusions (before 2001) that the State Department counts but that are not on those sites

## Designations

### BDI

#### `un-coi-burundi-2017`: UN body finding: crimes against humanity (Commission of Inquiry, 2017)

- Authority: Commission of Inquiry on Burundi (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity
- Group: the civilian population of Burundi
- Situation: Burundi, since April 2015: State officials at the highest level, the National Intelligence Service, the police, the army and the Imbonerakure
- Date: 11 Aug 2017
- Document: [Report of the Commission of Inquiry on Burundi (A/HRC/36/54)](https://documents.un.org/doc/UNDOC/GEN/G17/237/46/PDF/G1723746.PDF)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/36/54 on documents.un.org (dated 11 August 2017; original French). Summary: 'The Commission has reasonable grounds to believe that crimes against humanity have been committed in Burundi since April 2015.' Para. 69: 'having established that the elements of crimes against humanity are present'; para. 77: 'attributable primarily to State officials at the highest level and to senior officers and members of the National Intelligence Service, the police, the army and the Imbonerakure'. History only.
- Status: **approved** on 24 Nov 2026

### BIH

#### `icty-krstic-trial-2001`: Court judgment: genocide (ICTY, 2001)

- Authority: International Criminal Tribunal for the former Yugoslavia, Trial Chamber (court); type `CourtJudgment`
- Term: genocide
- Group: the Bosnian Muslims of Srebrenica
- Situation: Srebrenica, July 1995: General Radislav Krstić (Drina Corps, Bosnian Serb Army) convicted; the Chamber found that genocide was committed in Srebrenica
- Date: 2 Aug 2001
- Document: [Prosecutor v. Radislav Krstić, Case No. IT-98-33-T, Judgement](https://www.icty.org/x/cases/krstic/tjug/en/krs-tj010802e.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the judgement PDF on icty.org: Trial Chamber, Case No. IT-98-33-T, dated 2 August 2001; para. 645 'General Krstic is guilty of genocide pursuant to Article 4(2)(a)'. Also opened ICTY's case information sheet (icty.org/x/cases/krstic/cis/en/cis_krstic_en.pdf): 'The Trial Chamber found that genocide was committed in Srebrenica'; '2 August 2001, convicted of genocide, crimes against humanity and violations of the laws or customs of war'.
- Status: **approved** on 24 Nov 2026

#### `icty-krstic-appeal-2004`: Court judgment: genocide (Krstić convicted of aiding and abetting it) (ICTY, 2004)

- Authority: International Criminal Tribunal for the former Yugoslavia, Appeals Chamber (court); type `CourtJudgment`
- Term: genocide (Krstić convicted of aiding and abetting it)
- Group: the Bosnian Muslims of Srebrenica
- Situation: Srebrenica, July 1995: on appeal, Radislav Krstić convicted of aiding and abetting genocide; the Appeals Chamber held that genocide was committed at Srebrenica
- Date: 19 Apr 2004
- Document: [Prosecutor v. Radislav Krstić, Case No. IT-98-33-A, Judgement (Appeals Chamber)](https://www.icty.org/x/cases/krstic/acjug/en/krs-aj040419e.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the appeal judgement PDF on icty.org: Appeals Chamber, Case No. IT-98-33-A, 19 April 2004. Para. 39: 'the crime of genocide was committed at Srebrenica in July 1995, a determination which the Trial Chamber correctly made'; 'By seeking to eliminate a part of the Bosnian Muslims, the Bosnian Serb forces committed genocide.' ICTY's case information sheet: '19 April 2004, convicted of aiding and abetting genocide, crimes against humanity and violations of the laws or customs of war'; not guilty of genocide as a principal perpetrator.
- Status: **approved** on 24 Nov 2026

#### `icj-bosnia-v-serbia-2007`: Court judgment: genocide (ICJ, 2007)

- Authority: International Court of Justice (court); type `CourtJudgment`
- Term: genocide
- Group: the Bosnian Muslims of Srebrenica
- Situation: Srebrenica, July 1995: the Court found that the killings were genocide; Serbia did not commit it, but violated its obligations under the Genocide Convention to prevent it and to punish it
- Date: 26 Feb 2007
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide (Bosnia and Herzegovina v. Serbia and Montenegro), Judgment of 26 February 2007](https://www.icj-cij.org/sites/default/files/case-related/91/091-20070226-JUD-01-00-EN.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/91 ('Overview of the case'): the Srebrenica killings 'were committed with the specific intent to destroy in part the group of Bosnian Muslims in that area'; the acts were not proved attributable to Serbia; Serbia 'had violated its obligation contained in Article 1 of the Genocide Convention to prevent' the Srebrenica genocide and 'had violated its obligation to punish the perpetrators of genocide'. The judgment's PDF URL is as listed on that page; the PDF itself could not be opened (icj-cij.org serves PDFs behind a bot check).
- Status: **approved** on 24 Nov 2026

### CHN

#### `us-xinjiang-2021`: United States government determination: genocide; crimes against humanity (US Secretary of State, 2021)

- Authority: United States Secretary of State (Michael R. Pompeo) (government: United States government); type `GovernmentDetermination`
- Term: genocide; crimes against humanity
- Group: the predominantly Muslim Uyghurs and other members of ethnic and religious minority groups in Xinjiang
- Situation: Xinjiang, since at least March 2017: the People's Republic of China, under the direction and control of the Chinese Communist Party
- Date: 19 Jan 2021
- Document: [Determination of the Secretary of State on Atrocities in Xinjiang](https://2017-2021.state.gov/determination-of-the-secretary-of-state-on-atrocities-in-xinjiang/)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the determination on 2017-2021.state.gov (State Department archive), dated January 19, 2021: 'I have determined that since at least March 2017, the People's Republic of China (PRC), under the direction and control of the Chinese Communist Party (CCP), has committed crimes against humanity against the predominantly Muslim Uyghurs and other members of ethnic and religious minority groups'; and 'has committed genocide against the predominantly Muslim Uyghurs and other ethnic and religious minority groups in Xinjiang'. History only.
- Status: **approved** on 24 Nov 2026

#### `un-ohchr-xinjiang-2022`: UN body finding: may constitute crimes against humanity (OHCHR, 2022)

- Authority: Office of the United Nations High Commissioner for Human Rights (OHCHR) (UN body); type `UnBodyFinding`
- Term: may constitute crimes against humanity
- Group: Uyghur and other predominantly Muslim groups
- Situation: Xinjiang Uyghur Autonomous Region, 2017–2019 and potentially thereafter: arbitrary and discriminatory detention
- Date: 31 Aug 2022
- Document: [OHCHR Assessment of human rights concerns in the Xinjiang Uyghur Autonomous Region, People's Republic of China](https://www.ohchr.org/sites/default/files/documents/countries/2022-08-31/22-08-31-final-assesment.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the assessment on ohchr.org (dated 31 August 2022). Para. 148: 'The extent of arbitrary and discriminatory detention of members of Uyghur and other predominantly Muslim groups ... may constitute international crimes, in particular crimes against humanity.' The term keeps OHCHR's 'may constitute'. No UN statement from the last 12 months saying it continues was found, so it is history only.
- Status: **approved** on 24 Nov 2026

### ERI

#### `un-coi-eritrea-2016`: UN body finding: crimes against humanity (Commission of Inquiry, 2016)

- Authority: Commission of Inquiry on Human Rights in Eritrea (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity
- Group: the country's civilian population
- Situation: Eritrea, since 1991: Eritrean officials
- Date: 9 May 2016
- Document: [Report of the commission of inquiry on human rights in Eritrea (A/HRC/32/47)](https://documents.un.org/doc/UNDOC/GEN/G16/093/42/PDF/G1609342.PDF)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/32/47 on documents.un.org (dated 9 May 2016). Para. 112: 'there are reasonable grounds to believe that crimes against humanity have been committed in Eritrea since 1991. Eritrean officials have engaged in a persistent, widespread and systematic attack against the country's civilian population since 1991.' History only (no statement from the last 12 months found).
- Status: **approved** on 24 Nov 2026

### ETH

#### `un-ichree-ethiopia-2022`: UN body finding: crimes against humanity (murder, torture, rape and sexual violence) (Commission of Human Rights Experts, 2022)

- Authority: International Commission of Human Rights Experts on Ethiopia (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity (murder, torture, rape and sexual violence)
- Group: the civilian population of Tigray
- Situation: Tigray, northern Ethiopia, since November 2020: members of the Ethiopian National Defence Forces
- Date: 5 Oct 2022
- Document: [Report of the International Commission of Human Rights Experts on Ethiopia (A/HRC/51/46)](https://documents.un.org/doc/UNDOC/GEN/G22/515/29/PDF/G2251529.PDF)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/51/46 on documents.un.org (Distr. 5 October 2022). Para. 98: 'the Commission has reasonable grounds to believe that the Federal Government has committed the crimes against humanity of murder, torture, rape and sexual violence' against the civilian population of Tigray. The Commission's later report (2023) was not researched. History only.
- Status: **approved** on 24 Nov 2026

#### `us-ethiopia-2023`: United States government determination: crimes against humanity; ethnic cleansing (US Secretary of State, 2023)

- Authority: United States Secretary of State (Antony J. Blinken) (government: United States government); type `GovernmentDetermination`
- Term: crimes against humanity; ethnic cleansing
- Group: civilians in northern Ethiopia
- Situation: Northern Ethiopia, 2020–2022: crimes against humanity by members of the Ethiopian National Defense Forces, Eritrean Defense Forces and Amhara forces; ethnic cleansing by members of the Amhara forces in western Tigray (war crimes by all parties, including the TPLF)
- Date: 20 Mar 2023
- Document: [War Crimes, Crimes Against Humanity, and Ethnic Cleansing in Ethiopia (press statement)](https://2021-2025.state.gov/war-crimes-crimes-against-humanity-and-ethnic-cleansing-in-ethiopia/)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the press statement on 2021-2025.state.gov (State Department archive), dated March 20, 2023: war crimes by members of the ENDF, EDF, TPLF forces and Amhara forces; 'Members of the ENDF, EDF, and Amhara forces also committed crimes against humanity'; 'Members of the Amhara forces also committed the crime against humanity of deportation or forcible transfer and committed ethnic cleansing in western Tigray.' History only.
- Status: **approved** on 24 Nov 2026

### HRV

#### `icj-croatia-v-serbia-2015`: Court judgment: genocide not established (ICJ, 2015)

- Authority: International Court of Justice (court); type `CourtJudgment`; outcome **not established**
- Term: genocide
- Group: Croats (Croatia's claim); the Serbs of Croatia (Serbia's counter-claim)
- Situation: Croatia, 1991–1995: Croatia's claim against Serbia and Serbia's counter-claim (about Operation Storm, 1995) were both rejected. The Court found acts that were the physical element of genocide, but not the intent to destroy the group, so genocide was not proved
- Date: 3 Feb 2015
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide (Croatia v. Serbia), Judgment of 3 February 2015](https://www.icj-cij.org/sites/default/files/case-related/118/118-20150203-JUD-01-00-EN.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/118 ('Overview of the case'): acts against Croats 'constituted the actus reus of genocide within the meaning of Article II (a) and (b)', but 'In the absence of evidence of the required intent, the Court found that Croatia had not proved its allegations'; Serbia's counter-claim: the acts 'had not been committed on a scale such that they could only point to the existence of a genocidal intent', and 'it rejected Serbia's counter-claim in its entirety'. Press release 2015/4, 'The Court rejects Croatia's claim and Serbia's counter-claim'. Entered as a court judgment with outcome NotEstablished, so the app labels it 'Court judgment: genocide not established', never draws it on the map, and never counts it as a crisis. The judgment's PDF URL is as listed on the case page (the PDF itself is behind icj-cij.org's bot check).
- Status: **approved** on 24 Nov 2026

### IRQ

#### `us-isis-2016-irq`: United States government determination: genocide; crimes against humanity; ethnic cleansing (US Secretary of State, 2016)

- Authority: United States Secretary of State (John Kerry) (government: United States government); type `GovernmentDetermination`
- Term: genocide; crimes against humanity; ethnic cleansing
- Group: Yezidis, Christians and Shia Muslims (crimes against humanity and ethnic cleansing also, in some cases, against Sunni Muslims, Kurds and other minorities)
- Situation: Areas under the control of Daesh (ISIS) in Iraq and Syria (also entered under Syria)
- Date: 17 Mar 2016
- Document: [Remarks on Daesh and Genocide](https://2009-2017.state.gov/secretary/remarks/2016/03/254782.htm)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the remarks on 2009-2017.state.gov (State Department archive), dated March 17, 2016: 'in my judgment, Daesh is responsible for genocide against groups in areas under its control, including Yezidis, Christians, and Shia Muslims. Daesh is also responsible for crimes against humanity and ethnic cleansing directed at these same groups and in some cases also against Sunni Muslims, Kurds, and other minorities.' One determination covering Iraq and Syria, entered once for each country. Secretary Tillerson's reaffirmation of 15 August 2017 is not entered: its page on 2017-2021.state.gov returned 'Technical Difficulties' every time.
- Status: **approved** on 24 Nov 2026

### KHM

#### `eccc-002-02-2018`: Court judgment: genocide; crimes against humanity; grave breaches of the Geneva Conventions (ECCC, 2018)

- Authority: Extraordinary Chambers in the Courts of Cambodia, Trial Chamber (court); type `CourtJudgment`
- Term: genocide; crimes against humanity; grave breaches of the Geneva Conventions
- Group: the Vietnamese and the Cham
- Situation: Democratic Kampuchea, 17 April 1975 to 6 January 1979: Nuon Chea and Khieu Samphan convicted (Case 002/02); genocide of the Vietnamese (both) and of the Cham (Nuon Chea, as a superior)
- Date: 16 Nov 2018
- Document: [Trial Chamber Summary of Judgement, Case 002/02, 16 November 2018](https://www.eccc.gov.kh/sites/default/files/documents/courtdoc/%5Bdate-in-tz%5D/20181217 Summary of Judgement Case 002-02 ENG_FINAL FOR PUBLICATION.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the Trial Chamber's summary of judgement on eccc.gov.kh (16 November 2018). Para. 71: Nuon Chea guilty of crimes against humanity, grave breaches and 'genocide by killing members of the Vietnamese and Cham groups'; para. 72: Khieu Samphan guilty of crimes against humanity, grave breaches and 'genocide by killing members of the Vietnamese group'; paras 30 and 34: genocide committed against the Cham and against the Vietnamese. The full judgement (E465) and the Supreme Court Chamber's appeal judgement were not opened, so the appeal is not entered.
- Status: **approved** on 24 Nov 2026

### MMR

#### `us-burma-2017`: United States government determination: ethnic cleansing (US Secretary of State, 2017)

- Authority: United States Secretary of State (Rex W. Tillerson) (government: United States government); type `GovernmentDetermination`
- Term: ethnic cleansing
- Group: the Rohingya
- Situation: Northern Rakhine State, 2017
- Date: 22 Nov 2017
- Document: [Efforts To Address Burma's Rakhine State Crisis (press statement)](https://2017-2021.state.gov/efforts-to-address-burmas-rakhine-state-crisis/)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the press statement on 2017-2021.state.gov (State Department archive), dated November 22, 2017: 'After a careful and thorough analysis of available facts, it is clear that the situation in northern Rakhine state constitutes ethnic cleansing against the Rohingya.' History only.
- Status: **approved** on 24 Nov 2026

#### `un-ffm-myanmar-2018`: UN body finding: crimes against humanity (Fact-Finding Mission, 2018)

- Authority: Independent International Fact-Finding Mission on Myanmar (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity
- Group: civilians in Kachin, Rakhine and Shan States, including the Rohingya in Rakhine State
- Situation: Kachin, Rakhine and Shan States, since 2011, principally by the Myanmar military (Tatmadaw); for Rakhine State the mission found sufficient information to warrant investigating and prosecuting senior officials so that a competent court can determine their liability for genocide
- Date: 12 Sep 2018
- Document: [Report of the independent international fact-finding mission on Myanmar (A/HRC/39/64)](https://documents.un.org/doc/undoc/gen/g18/274/54/pdf/g1827454.pdf)
- Continuing: "Over the past year, the Mechanism's investigations showed that the frequency and intensity of war crimes and crimes against humanity committed by the Myanmar military and by various armed groups continued to escalate (summary; covering 1 July 2025 to 30 June 2026)."
  Source: [Report of the Independent Investigative Mechanism for Myanmar (A/HRC/63/19)](https://iimm.un.org/sites/default/files/2026/08/2026 IIMM Annual Report.pdf), 13 Jul 2026. On the map until 13 Jul 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/39/64 on documents.un.org (dated 12 September 2018). Para. 88: 'the mission finds that crimes against humanity have been committed in Kachin, Rakhine and Shan States, principally by the Tatmadaw'. Para. 87 (genocide): 'sufficient information to warrant the investigation and prosecution of senior officials in the Tatmadaw chain of command, so that a competent court can determine their liability for genocide in relation to the situation in Rakhine State': so the term entered is crimes against humanity, and the genocide wording is in the situation, not presented as a finding of genocide. Continuing: opened the Independent Investigative Mechanism for Myanmar's annual report A/HRC/63/19 (dated 13 July 2026) on iimm.un.org; a UN report saying crimes against humanity by the military continued.
- Status: **approved** on 24 Nov 2026

#### `icj-gambia-v-myanmar-pm-2020`: Provisional measures in a case alleging genocide: not a finding (ICJ, 2020)

- Authority: International Court of Justice (court); type `CourtProvisionalMeasures`
- Term: genocide
- Group: the Rohingya in Myanmar
- Situation: Myanmar: The Gambia's case against Myanmar under the Genocide Convention; the Court indicated provisional measures 'for the protection of the Rohingya in Myanmar'
- Date: 23 Jan 2020
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide (The Gambia v. Myanmar), Order of 23 January 2020: Request for the indication of provisional measures](https://www.icj-cij.org/sites/default/files/case-related/178/178-20200123-ORD-01-00-EN.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/178: the Order of 23 January 2020 ('Request for the indication of provisional measures') and press release 2020/3, 'The Court indicates provisional measures in order to preserve certain rights claimed by The Gambia for the protection of the Rohingya in Myanmar'. The order's PDF URL is as listed on the page (PDF behind icj-cij.org's bot check). The same page lists the Judgment of 19 November 2026 on the merits (press release 2026/30); that judgment's findings could not be read on icj-cij.org and it is NOT entered (see the Task 5 report).
- Status: **approved** on 24 Nov 2026

#### `us-burma-2022`: United States government determination: genocide; crimes against humanity (US Secretary of State, 2022)

- Authority: United States Secretary of State (Antony J. Blinken) (government: United States government); type `GovernmentDetermination`
- Term: genocide; crimes against humanity
- Group: the Rohingya
- Situation: Burma (Myanmar), 2016–2017: members of the Burmese military
- Date: 21 Mar 2022
- Document: [Secretary Antony J. Blinken on the Genocide and Crimes Against Humanity in Burma](https://2021-2025.state.gov/secretary-antony-j-blinken-at-the-united-states-holocaust-memorial-museum/)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the remarks on 2021-2025.state.gov (State Department archive), dated March 21, 2022: 'I have determined that members of the Burmese military committed genocide and crimes against humanity against Rohingya.' The State Department's 'Burma Genocide' page (2021-2025.state.gov/burma-genocide/) says the same. History only (no UN statement from the last 12 months specifically about the Rohingya was used).
- Status: **approved** on 24 Nov 2026

### PRK

#### `un-coi-dprk-2014`: UN body finding: crimes against humanity (Commission of Inquiry, 2014)

- Authority: Commission of Inquiry on Human Rights in the Democratic People's Republic of Korea (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity
- Group: persons detained in political and other prison camps, those who try to flee the State, Christians and others considered to introduce subversive influences
- Situation: Democratic People's Republic of Korea: crimes against humanity committed pursuant to policies established at the highest level of the State
- Date: 7 Feb 2014
- Document: [Report of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/63)](https://documents.un.org/doc/undoc/gen/g14/108/66/pdf/g1410866.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/25/63 on documents.un.org (dated 7 February 2014). Para. 75: 'the commission finds that the body of testimony and other information it received establishes that crimes against humanity have been committed in the Democratic People's Republic of Korea, pursuant to policies established at the highest level of the State'; para. 77 names the primary targets (the group above). No continuing statement entered: the Human Rights Council's resolution 61/27 (30 March 2026, A/HRC/RES/61/27) 'condemns ... the long-standing and ongoing systematic, widespread and gross human rights violations' and recalls the commission's findings, but it is a resolution, not a report, and does not itself say the crimes against humanity continue. The owner may decide whether it qualifies.
- Status: **approved** on 24 Nov 2026

### PSE

#### `icj-south-africa-v-israel-2023`: Pending: no finding yet (case alleging genocide) (ICJ, 2023)

- Authority: International Court of Justice (court); type `PendingCase`
- Term: genocide
- Group: people in the Gaza Strip
- Situation: Gaza Strip: South Africa's case against Israel under the Genocide Convention, filed 29 December 2023; no judgment on the merits
- Date: 29 Dec 2023
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide in the Gaza Strip (South Africa v. Israel): Application instituting proceedings and request for the indication of provisional measures](https://www.icj-cij.org/sites/default/files/case-related/192/192-20231228-app-01-00-en.pdf)
- Continuing: "The Commission finds on reasonable grounds that the Israeli authorities and the Israeli security forces have continued to commit the crime of genocide, crimes against humanity and war crimes in the Gaza Strip (para. 331; the paper covers events to 31 March 2026)."
  Source: ["The essence of childhood has been destroyed": Israel's deliberate targeting of Palestinian children in the Occupied Palestinian Territory since 7 October 2023 (A/HRC/62/CRP.2), UN Commission of Inquiry](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session62/a-hrc-62-crp-2.pdf), 18 Jun 2026. On the map until 18 Jun 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/192: application dated 29 December 2023 (its PDF file name says 28 December); Orders of 26 January, 28 March and 24 May 2024 on provisional measures; no judgment on the merits or on preliminary objections listed; latest entry press release 2026/24 of 22 September 2026 (Colombia withdraws its declaration of intervention). So the case is pending: 'Pending: no finding yet'. The continuing statement is a UN report (the Commission of Inquiry's paper A/HRC/62/CRP.2, dated 18 June 2026, para. 331), opened on ohchr.org; it is used for every row about Gaza since October 2023.
- Status: **approved** on 24 Nov 2026

#### `icj-south-africa-v-israel-pm-2024-01`: Provisional measures in a case alleging genocide: not a finding (ICJ, 2024)

- Authority: International Court of Justice (court); type `CourtProvisionalMeasures`
- Term: genocide
- Group: people in the Gaza Strip
- Situation: Gaza Strip: South Africa v. Israel; the Court indicated provisional measures
- Date: 26 Jan 2024
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide in the Gaza Strip (South Africa v. Israel), Order of 26 January 2024: Request for the indication of provisional measures](https://www.icj-cij.org/sites/default/files/case-related/192/192-20240126-ord-01-00-en.pdf)
- Continuing: "The Commission finds on reasonable grounds that the Israeli authorities and the Israeli security forces have continued to commit the crime of genocide, crimes against humanity and war crimes in the Gaza Strip (para. 331; the paper covers events to 31 March 2026)."
  Source: ["The essence of childhood has been destroyed": Israel's deliberate targeting of Palestinian children in the Occupied Palestinian Territory since 7 October 2023 (A/HRC/62/CRP.2), UN Commission of Inquiry](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session62/a-hrc-62-crp-2.pdf), 18 Jun 2026. On the map until 18 Jun 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/192: Order of 26 January 2024, 'Request for the indication of provisional measures'; press release title 'The Court indicates provisional measures'. PDF URL as listed on the page (behind the bot check). Continuing statement as for the pending-case row.
- Status: **approved** on 24 Nov 2026

#### `icj-south-africa-v-israel-pm-2024-03`: Provisional measures in a case alleging genocide: not a finding (ICJ, 2024)

- Authority: International Court of Justice (court); type `CourtProvisionalMeasures`
- Term: genocide
- Group: people in the Gaza Strip
- Situation: Gaza Strip: South Africa v. Israel; the Court indicated additional provisional measures
- Date: 28 Mar 2024
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide in the Gaza Strip (South Africa v. Israel), Order of 28 March 2024: Request for the modification of the Order of 26 January 2024 indicating provisional measures](https://www.icj-cij.org/sites/default/files/case-related/192/192-20240328-ord-01-00-en.pdf)
- Continuing: "The Commission finds on reasonable grounds that the Israeli authorities and the Israeli security forces have continued to commit the crime of genocide, crimes against humanity and war crimes in the Gaza Strip (para. 331; the paper covers events to 31 March 2026)."
  Source: ["The essence of childhood has been destroyed": Israel's deliberate targeting of Palestinian children in the Occupied Palestinian Territory since 7 October 2023 (A/HRC/62/CRP.2), UN Commission of Inquiry](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session62/a-hrc-62-crp-2.pdf), 18 Jun 2026. On the map until 18 Jun 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/192: Order of 28 March 2024, 'Request for the modification of the Order of 26 January 2024 indicating provisional measures'; press release title 'The Court indicates additional provisional measures'. PDF URL as listed on the page. Continuing statement as for the pending-case row.
- Status: **approved** on 24 Nov 2026

#### `icj-south-africa-v-israel-pm-2024-05`: Provisional measures in a case alleging genocide: not a finding (ICJ, 2024)

- Authority: International Court of Justice (court); type `CourtProvisionalMeasures`
- Term: genocide
- Group: people in the Gaza Strip
- Situation: Gaza Strip: South Africa v. Israel; the Court reaffirmed its previous provisional measures and indicated new measures
- Date: 24 May 2024
- Document: [Application of the Convention on the Prevention and Punishment of the Crime of Genocide in the Gaza Strip (South Africa v. Israel), Order of 24 May 2024: Request for the modification of the Order of 28 March 2024 indicating provisional measures](https://www.icj-cij.org/sites/default/files/case-related/192/192-20240524-ord-01-00-en.pdf)
- Continuing: "The Commission finds on reasonable grounds that the Israeli authorities and the Israeli security forces have continued to commit the crime of genocide, crimes against humanity and war crimes in the Gaza Strip (para. 331; the paper covers events to 31 March 2026)."
  Source: ["The essence of childhood has been destroyed": Israel's deliberate targeting of Palestinian children in the Occupied Palestinian Territory since 7 October 2023 (A/HRC/62/CRP.2), UN Commission of Inquiry](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session62/a-hrc-62-crp-2.pdf), 18 Jun 2026. On the map until 18 Jun 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Read the case page icj-cij.org/case/192: Order of 24 May 2024, 'Request for the modification of the Order of 28 March 2024 indicating provisional measures'; press release title 'The Court reaffirms its previous provisional measures and indicates new measures'. PDF URL as listed on the page. Continuing statement as for the pending-case row.
- Status: **approved** on 24 Nov 2026

#### `icc-palestine-warrants-2024`: Arrest warrant on charges of crimes against humanity (murder, persecution, other inhumane acts) and the war crime of starvation as a method of warfare: not a verdict (ICC, 2024)

- Authority: International Criminal Court, Pre-Trial Chamber I (court); type `ArrestWarrant`
- Term: crimes against humanity (murder, persecution, other inhumane acts) and the war crime of starvation as a method of warfare
- Group: the civilian population of Gaza
- Situation: Gaza, from at least 8 October 2023 until at least 20 May 2024: warrants of arrest for Benjamin Netanyahu (Prime Minister of Israel) and Yoav Gallant (then Minister of Defence)
- Date: 21 Nov 2024
- Document: [Situation in the State of Palestine: ICC Pre-Trial Chamber I rejects the State of Israel's challenges to jurisdiction and issues warrants of arrest for Benjamin Netanyahu and Yoav Gallant (press release)](https://www.icc-cpi.int/news/situation-state-palestine-icc-pre-trial-chamber-i-rejects-state-israels-challenges)
- Continuing: "The Commission finds on reasonable grounds that the Israeli authorities and the Israeli security forces have continued to commit the crime of genocide, crimes against humanity and war crimes in the Gaza Strip (para. 331; the paper covers events to 31 March 2026)."
  Source: ["The essence of childhood has been destroyed": Israel's deliberate targeting of Palestinian children in the Occupied Palestinian Territory since 7 October 2023 (A/HRC/62/CRP.2), UN Commission of Inquiry](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session62/a-hrc-62-crp-2.pdf), 18 Jun 2026. On the map until 18 Jun 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Opened the ICC press release of 21 November 2024 on icc-cpi.int: Pre-Trial Chamber I issued warrants of arrest for Mr Netanyahu and Mr Gallant 'for crimes against humanity and war crimes committed from at least 8 October 2023 until at least 20 May 2024'; the crimes: 'the war crime of starvation as a method of warfare; and the crimes against humanity of murder, persecution, and other inhumane acts'; 'part of a widespread and systematic attack against the civilian population of Gaza'. NOT checked: what happened to these warrants after 21 November 2024 (icc-cpi.int blocked further automated requests). The owner should confirm on icc-cpi.int that they are still in force before approving.
- Status: **approved** on 24 Nov 2026

#### `un-coi-opt-2025`: UN body finding: genocide (Commission of Inquiry, 2025)

- Authority: Independent International Commission of Inquiry on the Occupied Palestinian Territory, including East Jerusalem, and Israel (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: genocide
- Group: Palestinians in the Gaza Strip
- Situation: Gaza Strip, since 7 October 2023: the Commission concluded that Israeli authorities and security forces have committed and are continuing to commit genocide, and that the State of Israel bears responsibility
- Date: 16 Sep 2025
- Document: [Legal analysis of the conduct of Israel in Gaza pursuant to the Convention on the Prevention and Punishment of the Crime of Genocide (A/HRC/60/CRP.3)](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session60/advance-version/a-hrc-60-crp-3.pdf)
- Continuing: "The Commission finds on reasonable grounds that the Israeli authorities and the Israeli security forces have continued to commit the crime of genocide, crimes against humanity and war crimes in the Gaza Strip (para. 331; the paper covers events to 31 March 2026)."
  Source: ["The essence of childhood has been destroyed": Israel's deliberate targeting of Palestinian children in the Occupied Palestinian Territory since 7 October 2023 (A/HRC/62/CRP.2), UN Commission of Inquiry](https://www.ohchr.org/sites/default/files/documents/hrbodies/hrcouncil/sessions-regular/session62/a-hrc-62-crp-2.pdf), 18 Jun 2026. On the map until 18 Jun 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/60/CRP.3 on ohchr.org (dated 16 September 2025, advance version). Paras 252–255: the Commission 'concludes on reasonable grounds that the Israeli authorities and Israeli security forces have committed and are continuing to commit the following actus reus of genocide against the Palestinians in the Gaza Strip'; genocidal intent 'the only reasonable inference'; 'the State of Israel bears responsibility for the failure to prevent genocide, the commission of genocide and the failure to punish genocide'. Continuing: opened the same Commission's A/HRC/62/CRP.2 (dated 18 June 2026) on ohchr.org, para. 331.
- Status: **approved** on 24 Nov 2026

### RWA

#### `ictr-akayesu-1998`: Court judgment: genocide; crimes against humanity (ICTR, 1998)

- Authority: International Criminal Tribunal for Rwanda, Trial Chamber I (court); type `CourtJudgment`
- Term: genocide; crimes against humanity
- Group: the Tutsi
- Situation: Rwanda, 1994 (Taba commune): Jean-Paul Akayesu convicted; the Chamber found that genocide was committed in Rwanda in 1994 against the Tutsi as a group
- Date: 2 Sep 1998
- Document: [The Prosecutor versus Jean-Paul Akayesu, Case No. ICTR-96-4-T, Judgement](https://unictr.irmct.org/sites/unictr.org/files/case-documents/ictr-96-4/trial-judgements/en/980902.pdf)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the judgement PDF on unictr.irmct.org (the ICTR archive kept by the UN International Residual Mechanism for Criminal Tribunals). Front page: Trial Chamber I, decision of 2 September 1998, Case No. ICTR-96-4-T. Section 8, Verdict: Count 1 'Guilty of Genocide'; Counts 3, 5, 7, 9, 11, 13 and 14 guilty of crimes against humanity; Count 4 guilty of direct and public incitement to commit genocide. Para. 126: 'genocide was, indeed, committed in Rwanda in 1994 against the Tutsi as a group'. The case page unictr.irmct.org/en/cases/ictr-96-4 refused automated access (HTTP 403), so the appeal (2001) was not checked and is not entered.
- Status: **approved** on 24 Nov 2026

### SDN

#### `us-darfur-2004`: United States government determination: genocide (US Secretary of State, 2004)

- Authority: United States Secretary of State (Colin L. Powell) (government: United States government); type `GovernmentDetermination`
- Term: genocide
- Group: non-Arab villagers in Darfur
- Situation: Darfur, 2003–2004: the Government of Sudan and the Jingaweit (Janjaweed) militias
- Date: 9 Sep 2004
- Document: [The Crisis in Darfur: Secretary Colin L. Powell, Testimony Before the Senate Foreign Relations Committee](https://2001-2009.state.gov/secretary/former/powell/remarks/36042.htm)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the testimony on 2001-2009.state.gov (State Department archive), dated September 9, 2004: 'we concluded, I concluded, that genocide has been committed in Darfur and that the Government of Sudan and the Jingaweit bear responsibility'; attacks 'against non-Arab villagers'. History only.
- Status: **approved** on 24 Nov 2026

#### `us-sudan-2023`: United States government determination: crimes against humanity; ethnic cleansing (US Secretary of State, 2023)

- Authority: United States Secretary of State (Antony J. Blinken) (government: United States government); type `GovernmentDetermination`
- Term: crimes against humanity; ethnic cleansing
- Group: civilians in Sudan, including Masalit civilians in West Darfur
- Situation: Sudan, since April 2023: crimes against humanity and ethnic cleansing by members of the Rapid Support Forces and allied militias (war crimes by both the Sudanese Armed Forces and the RSF)
- Date: 6 Dec 2023
- Document: [War Crimes, Crimes Against Humanity, and Ethnic Cleansing Determination in Sudan (press statement)](https://2021-2025.state.gov/war-crimes-crimes-against-humanity-and-ethnic-cleansing-determination-in-sudan/)
- Continuing: "The atrocities committed in and around El Fasher are aggravations of earlier patterns of violence in Darfur, including those previously documented by the mission in Zamzam Camp and elsewhere, demonstrating continuity and escalation. In the absence of effective prevention and accountability, the mission considers that the risk of further genocidal violence remains acute (para. 123). ... As the conflict now moves further, in particular to Kordofan, marked by the same modus operandi, the international community must act decisively to prevent further atrocities (para. 124)."
  Source: [Sudan: hallmarks of genocide in El Fasher. Report of the independent international fact-finding mission for the Sudan (A/HRC/61/77)](https://documents.un.org/doc/UNDOC/GEN/G26/019/45/PDF/G2601945.PDF), 10 Mar 2026. On the map until 10 Mar 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Opened the press statement on 2021-2025.state.gov (State Department archive), dated December 6, 2023: 'I have determined that members of the SAF and the RSF have committed war crimes in Sudan. I have also determined that members of the RSF and allied militias have committed crimes against humanity and ethnic cleansing.' The statement names Masalit civilians. Continuing: a UN report, the fact-finding mission's A/HRC/61/77 (10 March 2026), paras 123-124: RSF atrocities showing 'continuity and escalation', and the same modus operandi moving to Kordofan. The mission's later report A/HRC/63/23 (September 2026): the Task 5 code reviewer read it on 2026-11-23 and found that it does not say atrocity crimes continue; I could not open it (see un-ffm-sudan-2026).
- Status: **approved** on 24 Nov 2026

#### `us-sudan-2025`: United States government determination: genocide (US Secretary of State, 2025)

- Authority: United States Secretary of State (Antony J. Blinken) (government: United States government); type `GovernmentDetermination`
- Term: genocide
- Group: men and boys targeted on an ethnic basis, and women and girls from certain ethnic groups
- Situation: Sudan, since April 2023: members of the Rapid Support Forces and allied militias
- Date: 7 Jan 2025
- Document: [Genocide Determination in Sudan and Imposing Accountability Measures (press statement)](https://2021-2025.state.gov/genocide-determination-in-sudan-and-imposing-accountability-measures/)
- Continuing: "The atrocities committed in and around El Fasher are aggravations of earlier patterns of violence in Darfur, including those previously documented by the mission in Zamzam Camp and elsewhere, demonstrating continuity and escalation. In the absence of effective prevention and accountability, the mission considers that the risk of further genocidal violence remains acute (para. 123). ... As the conflict now moves further, in particular to Kordofan, marked by the same modus operandi, the international community must act decisively to prevent further atrocities (para. 124)."
  Source: [Sudan: hallmarks of genocide in El Fasher. Report of the independent international fact-finding mission for the Sudan (A/HRC/61/77)](https://documents.un.org/doc/UNDOC/GEN/G26/019/45/PDF/G2601945.PDF), 10 Mar 2026. On the map until 10 Mar 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Opened the press statement on 2021-2025.state.gov (State Department archive), dated January 7, 2025: 'Based on this information, I have now concluded that members of the RSF and allied militias have committed genocide in Sudan.' It describes the RSF and allied militias having 'systematically murdered men and boys ... on an ethnic basis, and deliberately targeted women and girls from certain ethnic groups'. Continuing as for us-sudan-2023 (A/HRC/61/77, paras 123-124). A/HRC/63/23: the Task 5 code reviewer read it on 2026-11-23 and found that it does not say atrocity crimes continue; I could not open it.
- Status: **approved** on 24 Nov 2026

#### `un-ffm-sudan-2026`: UN body finding: crimes against humanity; indications pointing to genocide (Fact-Finding Mission, 2026)

- Authority: Independent International Fact-Finding Mission for the Sudan (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity; indications pointing to genocide
- Group: non-Arab communities, in particular the Zaghawa and the Fur
- Situation: El Fasher and surrounding areas, Darfur, 2025 (takeover on or around 26–27 October 2025, after an 18-month siege), by the Rapid Support Forces
- Date: 10 Mar 2026
- Document: [Sudan: hallmarks of genocide in El Fasher. Report of the independent international fact-finding mission for the Sudan (A/HRC/61/77)](https://documents.un.org/doc/UNDOC/GEN/G26/019/45/PDF/G2601945.PDF)
- Continuing: "The atrocities committed in and around El Fasher are aggravations of earlier patterns of violence in Darfur, including those previously documented by the mission in Zamzam Camp and elsewhere, demonstrating continuity and escalation. In the absence of effective prevention and accountability, the mission considers that the risk of further genocidal violence remains acute (para. 123). ... As the conflict now moves further, in particular to Kordofan, marked by the same modus operandi, the international community must act decisively to prevent further atrocities (para. 124)."
  Source: [Sudan: hallmarks of genocide in El Fasher. Report of the independent international fact-finding mission for the Sudan (A/HRC/61/77)](https://documents.un.org/doc/UNDOC/GEN/G26/019/45/PDF/G2601945.PDF), 10 Mar 2026. On the map until 10 Mar 2027 unless a newer statement is added.
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/61/77 on documents.un.org (Distr. 10 March 2026; the mission's press release of February 2026 announced it). Para. 121: 'present indications pointing to genocide in and around El Fasher'; para. 122: 'crimes against humanity, notably extermination and persecution on ethnic, gender and political grounds, as well as war crimes ... were committed'; summary: 'indicators of a genocidal path'. The term keeps the mission's wording (indications, not a finding of genocide). Continuing statement: the same report, paras 123-124, re-read in the document on 2026-11-23 and quoted exactly ('demonstrating continuity and escalation'; 'the risk of further genocidal violence remains acute'; the conflict moving to Kordofan 'marked by the same modus operandi'). It supports continuing RSF atrocities, not a continuing genocide in so many words. Not used: (a) a July 2026 paper reported in UN News as A/HRC/62/CRP.4, whose ohchr.org link returned 'page not found'; (b) the mission's report A/HRC/63/23 ('Fuelling Sudan's conflict: weapons, fighters and external support networks', presented September 2026): documents.un.org redirected every request to its error page and no ohchr.org copy was found, so I could not open it. The Task 5 code reviewer read A/HRC/63/23 on 2026-11-23 and found that it does not say atrocity crimes continue; that is the reviewer's check, not mine.
- Status: **approved** on 24 Nov 2026

### SYR

#### `un-coi-syria-2011`: UN body finding: crimes against humanity (Commission of Inquiry, 2011)

- Authority: Independent International Commission of Inquiry on the Syrian Arab Republic (UN Human Rights Council) (UN body); type `UnBodyFinding`
- Term: crimes against humanity
- Group: civilians in the Syrian Arab Republic
- Situation: Syrian Arab Republic, 2011: members of the Syrian military and security forces, against protesters and other civilians
- Date: 23 Nov 2011
- Document: [Report of the independent international commission of inquiry on the Syrian Arab Republic (A/HRC/S-17/2/Add.1)](https://documents.un.org/doc/UNDOC/GEN/G11/170/97/PDF/G1117097.PDF)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened A/HRC/S-17/2/Add.1 on documents.un.org (dated 23 November 2011). Para. 100: 'its reasons for concluding that members of the Syrian military and security forces have committed crimes against humanity in 2011'; para. 108: 'gravely concerned that crimes against humanity of murder, torture, rape ... imprisonment ... have been committed'. This is the commission's first finding; its many later reports (including on ISIS and the Yazidis, 2016) were not researched in this task. History only.
- Status: **approved** on 24 Nov 2026

#### `us-isis-2016-syr`: United States government determination: genocide; crimes against humanity; ethnic cleansing (US Secretary of State, 2016)

- Authority: United States Secretary of State (John Kerry) (government: United States government); type `GovernmentDetermination`
- Term: genocide; crimes against humanity; ethnic cleansing
- Group: Yezidis, Christians and Shia Muslims (crimes against humanity and ethnic cleansing also, in some cases, against Sunni Muslims, Kurds and other minorities)
- Situation: Areas under the control of Daesh (ISIS) in Iraq and Syria (also entered under Iraq)
- Date: 17 Mar 2016
- Document: [Remarks on Daesh and Genocide](https://2009-2017.state.gov/secretary/remarks/2016/03/254782.htm)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Same document as us-isis-2016-irq (one determination, entered once per country).
- Status: **approved** on 24 Nov 2026

### UKR

#### `us-russia-ukraine-2023`: United States government determination: crimes against humanity (US Secretary of State, 2023)

- Authority: United States Secretary of State (Antony J. Blinken) (government: United States government); type `GovernmentDetermination`
- Term: crimes against humanity
- Group: civilians in Ukraine (execution-style killings, torture, rape, deportation)
- Situation: Ukraine, since February 2022: members of Russia's forces and other Russian officials
- Date: 18 Feb 2023
- Document: [Crimes Against Humanity in Ukraine (press statement)](https://2021-2025.state.gov/crimes-against-humanity-in-ukraine/)
- Continuing: none, so it is panel history only (not on the map).
- Checked on: 23 Nov 2026
- What was checked and where: Opened the press statement on 2021-2025.state.gov (State Department archive), dated February 18, 2023: 'I have determined that members of Russia's forces and other Russian officials have committed crimes against humanity in Ukraine.' No continuing statement was researched (the UN Commission of Inquiry on Ukraine was outside this task's list), so it is history only.
- Status: **approved** on 24 Nov 2026

## Camp sites

### `prk-kwanliso-14`: Political Prison Camp No. 14 (Kaechon) (Kwan-li-so No. 14)

- Country: PRK
- Description: Covers 150 square kilometres of a mountainous area near Kaechon City in South Pyongan Province. It appears to have existed since the 1960s and moved to its present location in the early 1980s. All inmates are held for life (UN Commission of Inquiry, A/HRC/25/CRP.1, para. 735).
- Position: 39.646810° N, 126.117058° E, as published in [North Korea's Political Prison Camp, Kwan-li-so No. 14, Update 1](https://www.hrnk.org/wp-content/uploads/2024/07/Camp-14-v.8.pdf) (HRNK (Committee for Human Rights in North Korea), 22 Dec 2021)
- Documented by (newest first):
  - [North Korea's Political Prison Camp, Kwan-li-so No. 14, Update 1](https://www.hrnk.org/documentations/north-koreas-political-prison-camp-kwan-li-so-no-14-update-1/) (HRNK, 22 Dec 2021)
  - [North Korea: Imagery Analysis of Camp 14](https://www.hrnk.org/documentations/north-korea-imagery-analysis-of-camp-14/) (HRNK, 30 Nov 2015)
  - [Report of the detailed findings of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/CRP.1)](https://www.ohchr.org/sites/default/files/Documents/HRBodies/HRCouncil/CoIDPRK/Report/A.HRC.25.CRP.1_ENG.doc) (UN Commission of Inquiry on Human Rights in the DPRK, 7 Feb 2014)
- Status on the map: a marker (its newest cited report does not say it closed)
- Checked on: 23 Nov 2026
- What was checked and where: Name and description: A/HRC/25/CRP.1 (Word file on ohchr.org, dated 7 February 2014), para. 735, in section IV.E 'Arbitrary detention, torture, executions, enforced disappearance and political prison camps' (paras 693-845, as the table of contents says). Position: the HRNK report's own 'CenterPoint Coordinates 39.646810, 126.117058' (report dated December 22, 2021, same as its web page). Note: HRNK's 2015 report gives 'Coordinates 39.557817, 126.011499', about 13 km away; the 2021 report puts the camp's south-west entrance at 39.558234, 126.012311, so the 2015 figure seems to mark the entrance. The 2015 report's cover says November 30, 2015 and its inner page 'Date of Report: November 24, 2015'; the cover date is used.
- Status: **approved** on 24 Nov 2026

### `prk-kwanliso-15`: Political Prison Camp No. 15 (Yodok) (Kwan-li-so No. 15)

- Country: PRK
- Description: Spread over about 370 square kilometres covering several valleys in Yodok County. It was divided into a total control zone, whose inmates are held for life, and a revolutionizing zone, from which some prisoners were released (UN Commission of Inquiry, A/HRC/25/CRP.1, para. 735). HRNK's imagery analysis of September 2015 found that the revolutionizing zone's detainee facility was razed in 2013–2014 and is no longer used to house detainees.
- Position: 39.671450° N, 126.852357° E, as published in [North Korea: Imagery Analysis of Camp 15 "Yodok": Closure of the "Revolutionizing Zone"](https://hrnk.org/wp-content/uploads/2024/07/ASA_HRNK_Camp15_RevZone_HQ.pdf) (HRNK (Committee for Human Rights in North Korea), 18 Sep 2015)
- Documented by (newest first):
  - [North Korea: Imagery Analysis of Camp 15 "Yodok": Closure of the "Revolutionizing Zone"](https://www.hrnk.org/documentations/imagery-analysis-of-camp-15-yodok-closure-of-the-revolutionizing-zone/) (HRNK, 18 Sep 2015)
  - [North Korea: Imagery Analysis of Camp 15](https://www.hrnk.org/documentations/north-korea-imagery-analysis-of-camp-15/) (HRNK, 13 Feb 2015)
  - [Report of the detailed findings of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/CRP.1)](https://www.ohchr.org/sites/default/files/Documents/HRBodies/HRCouncil/CoIDPRK/Report/A.HRC.25.CRP.1_ENG.doc) (UN Commission of Inquiry on Human Rights in the DPRK, 7 Feb 2014)
- Status on the map: a marker (its newest cited report does not say it closed)
- Checked on: 23 Nov 2026
- What was checked and where: Name and description: A/HRC/25/CRP.1, para. 735. Position: 'Coordinates: 39.671450, 126.852357' printed in both HRNK reports (18 September 2015 and 13 February 2015). Dates are the reports' own: the web page of the February 2015 report says 2024-02-17, a site-migration date. Inconsistency: the COI places Yodok County in South Pyongan Province; HRNK's reports give Hamgyong-namdo (South Hamgyong), which is where Yodok County is; the description names the county only. HRNK (September 2015): 'This imagery analysis confirms the detainee facility and a small number of support buildings were razed between 2013-2014 and that the former "Revolutionizing Zone" is no longer used to house detainees.' The camp's later status was not researched.
- Status: **approved** on 24 Nov 2026

### `prk-kwanliso-16`: Political Prison Camp No. 16 (Myonggan, also Hwasong) (Kwan-li-so No. 16)

- Country: PRK
- Description: Covers about 560 square kilometres of rugged terrain in Myonggan, North Hamgyong Province, close to the P'unggye-ri nuclear test site. Witness testimony indicates it has existed since the 1970s, when it was much smaller; inmates live in two settlement areas (UN Commission of Inquiry, A/HRC/25/CRP.1, para. 735).
- Position: 41.231091° N, 129.412216° E, as published in [North Korea: Imagery Analysis of Camp 16](https://www.hrnk.org/wp-content/uploads/pdfs/publications/eng/ASA_HRNK_Camp16_v8_fullres_FINAL_12_15_15.pdf) (HRNK (Committee for Human Rights in North Korea), 15 Dec 2015)
- Documented by (newest first):
  - [Switchback: Evidence of a Connection between Kwan-li-so No. 16 and the Punggye-ri Nuclear Test Facility?](https://www.hrnk.org/documentations/switchback-evidence-of-a-connection-between-kwan-li-so-no-16-and-the-punggye-ri-nuclear-test-facility/) (HRNK, 17 Oct 2023)
  - [North Korea: Imagery Analysis of Camp 16](https://www.hrnk.org/documentations/north-korea-imagery-analysis-of-camp-16/) (HRNK, 15 Dec 2015)
  - [Report of the detailed findings of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/CRP.1)](https://www.ohchr.org/sites/default/files/Documents/HRBodies/HRCouncil/CoIDPRK/Report/A.HRC.25.CRP.1_ENG.doc) (UN Commission of Inquiry on Human Rights in the DPRK, 7 Feb 2014)
- Status on the map: a marker (its newest cited report does not say it closed)
- Checked on: 23 Nov 2026
- What was checked and where: Name and description: A/HRC/25/CRP.1, para. 735 (it says 'Myonggan'; HRNK's report gives the location as Hwasong-gun, Hamgyong-bukto). Position: 'Coordinates: 41.231091 N, 129.412216 E' in HRNK's 2015 report; its cover says December 15, 2015 (the web page's date too) and its inner page 'Date of Report: November 16, 2015'; the cover date is used. The 2023 'Switchback' report's coordinates (41.2831 N, 129.0981 E) are the Punggye-ri test site's, not the camp's, so they are not used.
- Status: **approved** on 24 Nov 2026

### `prk-kwanliso-18`: Political Prison Camp No. 18 (Pukchang) (Kwan-li-so No. 18)

- Country: PRK
- Description: In Bukchang (Pukchang) County, South Pyongan Province, on the south bank of the Taedong River near Camp No. 14. The UN Commission of Inquiry reported in 2014 that it was gradually downsized, that the site was closed in 2006 and that a short-term labour detention facility was placed on its premises (A/HRC/25/CRP.1, para. 739). HRNK's 2024 satellite-imagery report found that smaller sections appear to remain active as a detention facility.
- Position: 39.552435° N, 126.073063° E, as published in [North Korea's Political Prison Camp Kwan-li-so No. 18 (Pukch'ang)](https://www.hrnk.org/wp-content/uploads/2024/07/camp18_240618.pdf) (HRNK (Committee for Human Rights in North Korea), 18 Jun 2024)
- Documented by (newest first):
  - [North Korea's Political Prison Camp Kwan-li-so No. 18 (Pukch'ang)](https://www.hrnk.org/documentations/north-koreas-political-prison-camp-kwan-li-so-no-18-pukchang/) (HRNK, 18 Jun 2024)
  - [Report of the detailed findings of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/CRP.1)](https://www.ohchr.org/sites/default/files/Documents/HRBodies/HRCouncil/CoIDPRK/Report/A.HRC.25.CRP.1_ENG.doc) (UN Commission of Inquiry on Human Rights in the DPRK, 7 Feb 2014)
- Status on the map: a marker (its newest cited report does not say it closed)
- Status note: UN Commission of Inquiry (2014): reported closed in 2006 (A/HRC/25/CRP.1, para. 739) ([Report of the detailed findings of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/CRP.1)](https://www.ohchr.org/sites/default/files/Documents/HRBodies/HRCouncil/CoIDPRK/Report/A.HRC.25.CRP.1_ENG.doc), UN Commission of Inquiry on Human Rights in the DPRK, 7 Feb 2014)
- Status note: HRNK (2024): parts remain active ([North Korea's Political Prison Camp Kwan-li-so No. 18 (Pukch'ang)](https://www.hrnk.org/wp-content/uploads/2024/07/camp18_240618.pdf), HRNK (Committee for Human Rights in North Korea), 18 Jun 2024)
- Checked on: 23 Nov 2026
- What was checked and where: Name and description: A/HRC/25/CRP.1, para. 739 (camps of the Ministry of People's Security until 2006: 'Political Prison Camp No. 18 was gradually downsized until the site in Bukchang ... was closed down in 2006. Today, a short-term labour detention facility has been placed on the premises'). HRNK (report dated June 18, 2024; its web page says 2024-06-23): 'large sections of Kwan-li-so No. 18 were deactivated over a period of at least eight years and ... smaller sections remain active as a detention facility'; 'Whether the official designation of this facility remains Kwan-li-so No. 18 is unknown.' The brief allowed No. 18 if HRNK's 2024 report supports it; it does, but the two sources differ on its status, and the description gives both. Position: HRNK's 'CenterPoint Coordinates: 39.552435 N, 126.073063 E'.
- Status: **approved** on 24 Nov 2026

### `prk-kwanliso-25`: Political Prison Camp No. 25 (Chongjin) (Kwan-li-so No. 25)

- Country: PRK
- Description: A detention facility near Chongjin City, North Hamgyong Province, with a few thousand prisoners; it looks more like a maximum-security prison, with a main block surrounded by high walls. Its prisoners are held for life without trial on political grounds (UN Commission of Inquiry, A/HRC/25/CRP.1, para. 735).
- Position: 41.834384° N, 129.725280° E, as published in [North Korea's Political Prison Camp Kwan-li-so No. 25, Update 4](https://hrnk.org/wp-content/uploads/2024/07/camp25_update4.pdf) (HRNK (Committee for Human Rights in North Korea), 17 Feb 2024)
- Documented by (newest first):
  - [North Korea's Political Prison Camp Kwan-li-so No. 25, Update 4](https://www.hrnk.org/documentations/north-koreas-political-prison-camp-kwan-li-so-no-25-update/) (HRNK, 17 Feb 2024)
  - [North Korea's Political Prison Camp, Kwan-li-so No. 25, Update 3](https://www.hrnk.org/documentations/north-koreas-political-prison-camp-kwan-li-so-no-25-update-3/) (HRNK, 30 Sep 2021)
  - [North Korea: Camp No. 25 - Update 2](https://www.hrnk.org/documentations/north-korea-camp-no-25-update-2/) (HRNK, 29 Nov 2016)
  - [North Korea's Camp No. 25, Update](https://www.hrnk.org/documentations/north-koreas-camp-no-25-update/) (HRNK, 5 Jun 2014)
  - [Report of the detailed findings of the commission of inquiry on human rights in the Democratic People's Republic of Korea (A/HRC/25/CRP.1)](https://www.ohchr.org/sites/default/files/Documents/HRBodies/HRCouncil/CoIDPRK/Report/A.HRC.25.CRP.1_ENG.doc) (UN Commission of Inquiry on Human Rights in the DPRK, 7 Feb 2014)
  - [North Korea's Camp No. 25](https://www.hrnk.org/documentations/north-koreas-camp-no-25/) (HRNK, 25 Feb 2013)
- Status on the map: a marker (its newest cited report does not say it closed)
- Checked on: 23 Nov 2026
- What was checked and where: Name and description: A/HRC/25/CRP.1, para. 735. Left out of the description: the COI's figure that the camp 'now covers an area of 980 square metres', which cannot be right for a camp HRNK measures at about 1,400 by 1,000 metres (2014 update). Position: 'CenterPoint Coordinates: 41.834384 N, 129.725280 E' in Update 4 (February 17, 2024); the same figure is in Update 3 (September 30, 2021) and Update 2 (November 29, 2016). All dates are the reports' own cover dates. The 2013 report was prepared by DigitalGlobe Analytics for HRNK.
- Status: **approved** on 24 Nov 2026

## UN depots

UNHRD (the UN Humanitarian Response Depot network, run by the World Food Programme) has five hubs; each address is as UNHRD's
contacts page writes it (no staff names, emails or phone numbers are kept). UNHRD publishes **no coordinates**, so each depot is
marked at the airport its own address names or sits beside, with the airport's position from the bundled OurAirports data (The
Unlicense; ruling R21). Each row says which airport, quotes the part of the address that ties the depot to it, and is approximate:
the marker is at the airport, not at the depot's building. The nearest-depot distances and arcs use these points. Approve a row,
or correct its position.

### `unhrd-dubai`: UNHRD Dubai

- Country: ARE; city: Dubai
- Address (as UNHRD writes it): UNHRD c/o WFP, Dubai Humanitarian, Dubai Industrial City - Phase 2, Nearest Landmark - DWC, PO Box 506003 (United Arab Emirates)
- Position: 24.89617° N, 55.16235° E, at the airport beside the depot (approximate): [OurAirports: Al Maktoum International Airport (OMDW)](https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv) (OurAirports (The Unlicense), 24 Nov 2026)
- Source: [Contacts | UNHRD - Humanitarian Response Depot](https://unhrd.org/contacts) (UNHRD (WFP), 24 Nov 2026)
- Checked on: 24 Nov 2026
- What was checked and where: Address as written on UNHRD's contacts page (2026-11-24). UNHRD's contacts page shows no date, so the citation date is the day it was read (2026-11-24, in the built-in browser; scripts get HTTP 403). The page's staff names, emails and phone numbers are not copied. Position: the marker is at Al Maktoum International Airport (OMDW, IATA DWC; OurAirports also calls it Dubai World Central), taken from the bundled OurAirports extract (data/datasets/ourairports, OurAirports' airports.csv as downloaded on 2026-11-24, The Unlicense; ruling R21). The depot's own address gives the airport as its landmark: 'Dubai Industrial City - Phase 2, Nearest Landmark - DWC'. Dubai Industrial City is a large area, so the depot may be several kilometres from the airport. The position is approximate: the marker is at the airport's reference point in OurAirports, not at the depot's own building, and distances measured to it are rounded straight-line figures anyway.
- Status: **approved** on 25 Nov 2026

### `unhrd-accra`: UNHRD Accra

- Country: GHA; city: Accra
- Address (as UNHRD writes it): World Food Programme, UNHRD Accra, Courier Enclave, P.O. Box 1423, Kotoka International Airport, Accra, Ghana
- Position: 5.60519° N, 0.16679° W, at the airport beside the depot (approximate): [OurAirports: Kotoka International Airport (DGAA)](https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv) (OurAirports (The Unlicense), 24 Nov 2026)
- Source: [Contacts | UNHRD - Humanitarian Response Depot](https://unhrd.org/contacts) (UNHRD (WFP), 24 Nov 2026)
- Checked on: 24 Nov 2026
- What was checked and where: Address as written on UNHRD's contacts page (2026-11-24). UNHRD's contacts page shows no date, so the citation date is the day it was read (2026-11-24, in the built-in browser; scripts get HTTP 403). The page's staff names, emails and phone numbers are not copied. Position: the marker is at Kotoka International Airport (DGAA), taken from the bundled OurAirports extract (data/datasets/ourairports, OurAirports' airports.csv as downloaded on 2026-11-24, The Unlicense; ruling R21). The depot is at the airport per its own address, which names it: 'Courier Enclave, P.O. Box 1423, Kotoka International Airport, Accra'. The position is approximate: the marker is at the airport's reference point in OurAirports, not at the depot's own building, and distances measured to it are rounded straight-line figures anyway.
- Status: **approved** on 25 Nov 2026

### `unhrd-brindisi`: UNHRD Brindisi

- Country: ITA; city: Brindisi
- Address (as UNHRD writes it): World Food Programme, UNHRD Brindisi, Ex SS 16 Km 906.9 OR, P.za Del Vento 1, 72100 Brindisi, Italy
- Position: 40.65760° N, 17.94700° E, at the airport beside the depot (approximate): [OurAirports: Brindisi Airport (LIBR)](https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv) (OurAirports (The Unlicense), 24 Nov 2026)
- Source: [Contacts | UNHRD - Humanitarian Response Depot](https://unhrd.org/contacts) (UNHRD (WFP), 24 Nov 2026)
- Checked on: 24 Nov 2026
- What was checked and where: Address as written on UNHRD's contacts page (2026-11-24). UNHRD's contacts page shows no date, so the citation date is the day it was read (2026-11-24, in the built-in browser; scripts get HTTP 403). The page's staff names, emails and phone numbers are not copied. Position: the marker is at Brindisi Airport (LIBR), taken from the bundled OurAirports extract (data/datasets/ourairports, OurAirports' airports.csv as downloaded on 2026-11-24, The Unlicense; ruling R21). The depot sits beside the airport per its own address on the former SS16 road: 'Ex SS 16 Km 906.9 OR, P.za Del Vento 1, 72100 Brindisi'. The position is approximate: the marker is at the airport's reference point in OurAirports, not at the depot's own building, and distances measured to it are rounded straight-line figures anyway.
- Status: **approved** on 25 Nov 2026

### `unhrd-kuala-lumpur`: UNHRD Kuala Lumpur

- Country: MYS; city: Kuala Lumpur
- Address (as UNHRD writes it): World Food Programme, UNHRD Kuala Lumpur, Lot 14104 (PT12136), Jalan TUDM, Section U7, 40150 Shah Alam, Selangor, Malaysia
- Position: 3.13058° N, 101.54900° E, at the airport beside the depot (approximate): [OurAirports: Sultan Abdul Aziz Shah International Airport (WMSA)](https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv) (OurAirports (The Unlicense), 24 Nov 2026)
- Source: [Contacts | UNHRD - Humanitarian Response Depot](https://unhrd.org/contacts) (UNHRD (WFP), 24 Nov 2026)
- Checked on: 24 Nov 2026
- What was checked and where: Address as written on UNHRD's contacts page (2026-11-24). UNHRD's contacts page shows no date, so the citation date is the day it was read (2026-11-24, in the built-in browser; scripts get HTTP 403). The page's staff names, emails and phone numbers are not copied. Position: the marker is at Sultan Abdul Aziz Shah International Airport (WMSA, Subang), taken from the bundled OurAirports extract (data/datasets/ourairports, OurAirports' airports.csv as downloaded on 2026-11-24, The Unlicense; ruling R21). The depot sits beside the airport per its own address, on the air force road: 'Lot 14104 (PT12136), Jalan TUDM, Section U7, 40150 Shah Alam' (TUDM is the Royal Malaysian Air Force, which has its base at Subang). The position is approximate: the marker is at the airport's reference point in OurAirports, not at the depot's own building, and distances measured to it are rounded straight-line figures anyway.
- Status: **approved** on 25 Nov 2026

### `unhrd-panama`: UNHRD Panama

- Country: PAN; city: Panama City
- Address (as UNHRD writes it): World Food Programme, UNHRD Panama, Paseo de la Americas Panama Pacifico, Howard, Arraijan, Panama, Republic of Panama
- Position: 8.91479° N, 79.59960° W, at the airport beside the depot (approximate): [OurAirports: Panamá Pacífico International Airport (MPPA)](https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv) (OurAirports (The Unlicense), 24 Nov 2026)
- Source: [Contacts | UNHRD - Humanitarian Response Depot](https://unhrd.org/contacts) (UNHRD (WFP), 24 Nov 2026)
- Checked on: 24 Nov 2026
- What was checked and where: Address as written on UNHRD's contacts page (2026-11-24). UNHRD's contacts page shows no date, so the citation date is the day it was read (2026-11-24, in the built-in browser; scripts get HTTP 403). The page's staff names, emails and phone numbers are not copied. Position: the marker is at Panamá Pacífico International Airport (MPPA; OurAirports lists it under the ident MPHO and gives Howard Air Force Base and Panama Pacifico as its other names), taken from the bundled OurAirports extract (data/datasets/ourairports, OurAirports' airports.csv as downloaded on 2026-11-24, The Unlicense; ruling R21). The depot sits beside the airport per its own address: 'Paseo de la Americas Panama Pacifico, Howard, Arraijan'. The position is approximate: the marker is at the airport's reference point in OurAirports, not at the depot's own building, and distances measured to it are rounded straight-line figures anyway.
- Status: **approved** on 25 Nov 2026

## Explainers

The plain-language explainers the panel opens ("What is this, and why does it happen?"; "Why these?" for Your one small step).
Each is 300 words or fewer, paraphrased from at least 2 pages on the authority's own site (no quotation
longer than 15 words), and gives no figures that go stale: the panel's own sourced numbers say how many. Read each
one against its sources, then approve it or say what to change.

### `conflict`: Armed conflict: what is it, and why does it happen?

Text (213 words):

> An armed conflict is fighting between organised armed forces. It can be between two countries. It can also be between a government and an armed group, or between armed groups inside one country.
>
> The International Committee of the Red Cross (ICRC) explains how the law of war decides this. Between two countries, any hostile use of armed force can be enough, even a small clash. Inside one country, the fighting must reach a certain intensity, and the armed groups must be organised enough. No one has to declare war: the facts on the ground decide.
>
> The law of war is also called international humanitarian law. Its rules bind every side. People who take no part in the fighting, or who no longer do, must be treated humanely.
>
> Why does it happen? The Uppsala Conflict Data Program (UCDP) supplies this map's conflict data. It describes a conflict as a dispute over who governs a country, over territory, or both, fought with armed force. UCDP also records fighting between armed groups with no government involved, and deliberate attacks on civilians.
>
> This map marks a conflict where UCDP records at least 25 deaths in battle in a year. The panel gives UCDP's own figures for the country, with their dates and a link to the source.

- Sources:
  - [UCDP Definitions](https://www.uu.se/en/department/peace-and-conflict-research/research/ucdp/ucdp-definitions) (Uppsala University, Department of Peace and Conflict Research (UCDP), 12 May 2026)
  - [Frequently Asked Questions: International Armed Conflict (IAC)](https://www.icrc.org/en/article/faq-international-armed-conflict) (International Committee of the Red Cross (ICRC), 23 Dec 2025)
  - [ICRC 2024 Opinion Paper: How is the term 'Armed Conflict' defined in international humanitarian law?](https://www.icrc.org/en/document/icrc-opinion-paper-how-term-armed-conflict-defined-international-humanitarian-law) (International Committee of the Red Cross (ICRC), 16 Apr 2024)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24: UCDP Definitions (uu.se, last modified 12 May 2026: state-based armed conflict is a contested incompatibility over government and/or territory with armed force and at least 25 battle-related deaths in a calendar year; non-state conflict and one-sided violence against civilians also use 25 deaths a year). ICRC FAQ on international armed conflict (23 Dec 2025: a non-international armed conflict needs a certain level of intensity and sufficiently organised groups; people not or no longer taking part must be treated humanely). ICRC 2024 opinion paper page (16 Apr 2024: the ICRC classifies on facts and the legal criteria only) and its PDF (armed_conflict_defined_in_ihl.pdf, April 2024, read as text: armed conflict is a de facto state of hostilities, depending on neither a declaration nor recognition of war; no specific intensity is needed between states, even minor skirmishes may spark one, but the force must be a hostile act). ICRC's 2008 page (icrc.org/en/article/armed-conflict-international-humanitarian-law) was also read; the 2024 paper replaces it. The 25-deaths rule for the map is the app's C11, from UCDP.
- Status: **approved** on 25 Nov 2026

### `atrocities`: Genocide and mass atrocities: what are they, and why do they happen?

Text (292 words):

> Genocide is a crime under international law. The 1948 Genocide Convention defines it narrowly. It covers acts such as killing or seriously harming members of a national, ethnic, racial or religious group. They must be done with intent to destroy that group, in whole or in part.
>
> That intent is the hardest part to prove. The UN Office on Genocide Prevention notes that destroying a group's culture, or only driving it away, is not enough. Political groups are not covered.
>
> Crimes against humanity are acts such as murder, torture, enslavement or forced removal, when they are part of a widespread or systematic attack on civilians. They can happen in peace as well as in war. War crimes are serious breaches of the law of war. Ethnic cleansing is not a separate crime in law, but the acts it describes can be any of these crimes.
>
> Who decides? Courts give judgments. UN bodies, such as commissions of inquiry, publish findings. Some governments make formal determinations. The panel always names who made a finding, what kind of body it is, and when.
>
> Some steps are not verdicts. A court may order provisional measures, which are urgent steps while a case goes on. An arrest warrant or a pending case is not a finding of guilt: everyone charged with a crime has the right to be presumed innocent until proved guilty.
>
> Why do these crimes happen? The UN says they rarely happen suddenly. They build up over time, which gives chances to act early. In Rwanda, years of hate speech and false information came before the 1994 genocide against the Tutsi. The International Court of Justice found that the killings at Srebrenica in 1995 were genocide. The panel lists past findings like these as history.

- Sources:
  - [Definitions of Genocide and Related Crimes](https://www.un.org/en/genocide-prevention/definition) (United Nations Office on Genocide Prevention and the Responsibility to Protect, 24 Nov 2026)
  - [UN launches new tool to help to stop atrocity crimes 'before tensions escalate'](https://news.un.org/en/story/2014/12/486102-un-launches-new-tool-help-stop-atrocity-crimes-tensions-escalate) (UN News, 11 Dec 2014)
  - [Human Rights Council-mandated Investigative Bodies](https://www.ohchr.org/en/hr-bodies/hrc/list-hrc-mandat) (Office of the UN High Commissioner for Human Rights (OHCHR), 24 Nov 2026)
  - [How the Court works](https://www.icj-cij.org/how-the-court-works) (International Court of Justice, 24 Nov 2026)
  - [Universal Declaration of Human Rights (Article 11)](https://www.un.org/en/about-us/universal-declaration-of-human-rights) (United Nations, 10 Dec 1948)
  - [Outreach Programme on the 1994 Genocide against the Tutsi in Rwanda and the United Nations](https://www.un.org/en/preventgenocide/rwanda/) (United Nations, 24 Nov 2026)
  - [Application of the Convention on the Prevention and Punishment of the Crime of Genocide (Bosnia and Herzegovina v. Serbia and Montenegro): judgment of 26 February 2007](https://www.icj-cij.org/case/91) (International Court of Justice, 26 Feb 2007)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24. UN Office on Genocide Prevention, Definitions page (un.org; scripts get HTTP 403, read in the built-in browser): Article II of the Convention (five acts; intent to destroy, in whole or in part, a national, ethnical, racial or religious group); intent is the hardest element; cultural destruction or an intent only to disperse a group does not suffice; political groups excluded; Rome Statute Article 7 (crimes against humanity: widespread or systematic attack on civilians, in peace or war); war crimes; ethnic cleansing not an independent crime, its acts can be crimes against humanity, war crimes or genocide. UN News, 11 Dec 2014: atrocity crimes rarely happen suddenly and evolve over time; the Framework of Analysis uses risk factors and indicators. The Framework PDF itself could not be read (it downloads instead of opening; not retried). OHCHR list of Human Rights Council investigative bodies (commissions of inquiry, fact-finding missions). ICJ How the Court works: judgments final and binding; provisional measures are interim measures when a party's rights are in immediate danger. UDHR Article 11: presumption of innocence. UN Rwanda outreach page: the 1994 genocide against the Tutsi followed years of misinformation, disinformation and hate speech. ICJ case 91 page: the Court found the Srebrenica killings of July 1995 were genocide (26 Feb 2007). The ICC's own pages refused the browser (security block); not used.
- Status: **approved** on 25 Nov 2026

### `camps`: Prison camps: what are they, and why do they exist?

Text (257 words):

> Some governments lock people up without a fair process, often for what they believe or say, or for who they are. The UN calls this arbitrary detention.
>
> The UN Working Group on Arbitrary Detention names the main cases. A person may be held with no legal basis at all. They may be held for using basic freedoms, such as speaking out or practising a religion. They may be held after a trial so unfair that the detention is arbitrary. They may also be held because of their birth, origin, beliefs or another status.
>
> The camps on this map so far are in North Korea, where political prison camps are called kwanliso. In 2014 a UN Commission of Inquiry, set up by the UN Human Rights Council, reported on North Korea. It found that crimes against humanity were being committed there, including against prisoners in these camps.
>
> Why do they exist? The Commission found that the state uses surveillance and fear to stop any criticism. Making people disappear into political prison camps is one of its harshest tools. The government did not answer the Commission's requests to visit, so it relied on hundreds of witnesses who had left the country.
>
> In 2025 the UN Human Rights Office reported that the camps still operate. It asked the government to close them and to end guilt by association, which means punishing people for what a relative is said to have done.
>
> Each marker's position comes from a published study of satellite images. The panel names the study and its date.

- Sources:
  - [North Korea: UN Commission documents wide-ranging and ongoing crimes against humanity, urges referral to ICC](https://www.ohchr.org/en/press-releases/2014/02/north-korea-un-commission-documents-wide-ranging-and-ongoing-crimes-against) (Office of the UN High Commissioner for Human Rights (OHCHR), 17 Feb 2014)
  - [DPRK: UN report finds 10 years of increased suffering, repression and fear](https://www.ohchr.org/en/press-releases/2025/09/dprk-un-report-finds-10-years-increased-suffering-repression-and-fear) (Office of the UN High Commissioner for Human Rights (OHCHR), 12 Sep 2025)
  - [About arbitrary detention (Working Group on Arbitrary Detention)](https://www.ohchr.org/en/about-arbitrary-detention) (Office of the UN High Commissioner for Human Rights (OHCHR), 24 Nov 2026)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24 in the built-in browser (ohchr.org gives scripts HTTP 403). OHCHR press release of 17 Feb 2014 on the Commission of Inquiry on the DPRK (set up by the Human Rights Council in March 2013): crimes against humanity committed and ongoing; surveillance, coercion, fear and punishment used to preclude dissent; enforced disappearance to political prison camps; atrocities against inmates of the kwanliso; no access granted, so testimony from about 80 witnesses at public hearings and more than 240 confidential interviews ('hundreds of witnesses'). OHCHR press release of 12 Sep 2025: political prison camps continue to operate; it asks for an end to the system of political prison camps and guilt by association. The meaning of guilt by association in the text (punishing people for a relative's alleged acts) is the common meaning; the 2025 release names the practice without defining it. OHCHR About arbitrary detention: the Working Group's five categories (no legal basis; exercise of rights and freedoms; grave breach of fair-trial norms; prolonged custody of asylum-seekers, not mentioned in the text; discrimination). The satellite-image studies behind each marker are the camps table's own position sources (HRNK). The OHCHR Commission of Inquiry page could not be found (404).
- Status: **approved** on 25 Nov 2026

### `food`: Food crisis: what is it, and why does it happen?

Text (218 words):

> A food crisis is when many people cannot get enough food. Governments and aid groups measure it with one shared scale, the Integrated Food Security Phase Classification, or IPC. A partnership of government and non-government agencies built it.
>
> The IPC has five phases: 1 Minimal, 2 Stressed, 3 Crisis, 4 Emergency and 5 Famine. Each step up means a deeper lack of food. An area is put in a phase when at least one person in five there is in that phase or a worse one. So some people may be worse off than their area's phase.
>
> Famine is the most severe phase. An area is in famine only when three things are true. At least one household in five faces an extreme lack of food. At least three children in ten are acutely malnourished. And at least two people in every 10,000 die each day.
>
> Why does it happen? UN agencies and their partners name three main drivers of hunger crises: conflict, extreme weather, and economic shocks. Conflict is the biggest.
>
> This map marks a food crisis where the latest IPC analysis puts people in Phase 3 or worse. An analysis whose period ended more than 12 months ago is shown as past, not current. The panel gives the IPC's own figures and the period they cover.

- Sources:
  - [What is the IPC?](https://fews.net/about/integrated-phase-classification) (Famine Early Warning Systems Network (FEWS NET), 24 Nov 2026)
  - [Acute food insecurity and malnutrition remain alarmingly high as crises deepen, UN, EU and partners warn in new report](https://www.fao.org/newsroom/detail/acute-food-insecurity-and-malnutrition-remain-alarmingly-high-as-crises-deepen--un--eu-and-partners-warn-in-new-report/en) (Food and Agriculture Organization of the United Nations (FAO), 24 Apr 2026)
  - [Understanding famine: what it is, how you can act](https://www.wfp.org/stories/understanding-famine-what-it-how-you-can-act) (World Food Programme (WFP), 10 Jun 2025)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24. The IPC's own site (ipcinfo.org) showed a bot check (a Cloudflare challenge) to scripts and to the built-in browser, so it was left there, as the plan requires; the IPC Technical Manual on fsinplatform.org refused scripts (403). FEWS NET's What is the IPC? page (no date shown; FEWS NET helped design and implement the IPC and uses version 3.1): a global partnership of governmental and non-governmental agencies devised the IPC; five phases Minimal, Stressed, Crisis, Emergency, Famine; an area takes a phase when at least 20 percent of its population meets that phase or a higher one; Famine when at least 20 percent of the population has an extreme lack of food, global acute malnutrition (weight-for-height) reaches 30 percent, and the crude death rate reaches 2 per 10,000 per day. Re-opened on 2026-11-24 in the final fix wave: the page does not say the partners make each analysis together (only that IPC protocols ask for analysis on a consensual basis), so that clause was removed. WFP's Understanding famine page (10 Jun 2025; WFP is an IPC partner agency): famine only when at least 2 people per 10,000 die daily, at least 20 percent of households face extreme food shortages, and at least 30 percent of children suffer from acute malnutrition; the famine paragraph follows this wording (households, children, 'at least'). Note for the owner: FEWS NET words the food threshold as 20 percent of the population and WFP as 20 percent of households; the IPC manual itself (households) could not be opened. FAO newsroom, 24 Apr 2026 (Global Report on Food Crises 2026): conflict the primary driver; climate shocks and economic shocks also named. The Phase 3 and 12-month rules are the app's (C11, P2-4).
- Status: **approved** on 25 Nov 2026

### `outbreaks`: Disease outbreaks: what are they, and why do they happen?

Text (232 words):

> This layer shows serious public health events that the World Health Organization (WHO) reports in its Disease Outbreak News. They may be outbreaks of a known disease, such as cholera, or of an illness no one has identified yet.
>
> WHO writes these reports for three kinds of event. One is an illness of unknown cause that could affect other countries, or travel and trade. Another is a known disease that has shown it can do serious harm and spread across borders. The third is an event of such public concern that it could disrupt health work, travel or trade.
>
> WHO says the reports do not list every event it responds to. So a country with no report may still have outbreaks.
>
> Countries share health information under the International Health Regulations. These rules are legally binding on every WHO member state. They require countries to tell WHO about health events that could spread to other countries.
>
> Why do outbreaks happen? Take cholera. It spreads through food or water that carries the bacteria. WHO links it to unsafe water and poor sanitation. Conflict and people fleeing their homes can break water and sanitation services. Floods, cyclones and drought can damage them too. So outbreaks can follow other crises.
>
> This map marks an outbreak when WHO has published a report about the country in the last 90 days. The panel links to WHO's own report.

- Sources:
  - [Disease Outbreak News](https://www.who.int/emergencies/disease-outbreak-news) (World Health Organization (WHO), 24 Nov 2026)
  - [International Health Regulations](https://www.who.int/health-topics/international-health-regulations) (World Health Organization (WHO), 24 Nov 2026)
  - [Cholera (fact sheet)](https://www.who.int/news-room/fact-sheets/detail/cholera) (World Health Organization (WHO), 29 Sep 2026)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24. WHO Disease Outbreak News page: DONs give information on confirmed and potential acute public health events; the three kinds of event (unknown cause with potential international concern affecting travel or trade; known cause shown able to cause serious impact and spread internationally; high public concern that may disrupt interventions, travel or trade); not an exhaustive list of events WHO responds to; published under IHR Article 11.4. WHO International Health Regulations page (no page date; the amended text in force since 19 Sep 2025): legally binding on 197 countries including all 194 WHO member states; countries notify WHO of events that may have international implications. WHO cholera fact sheet, 29 Sep 2026: spread by contaminated food or water; linked to limited safe water and sanitation; conflict and displacement disrupt water and sanitation; cyclones, floods and drought damage them. The 90-day rule is the app's (C11). The WHO regional outbreak page (emro.who.int) gave 502 and was not used.
- Status: **approved** on 25 Nov 2026

### `displacement`: People forced from home: who are they, and why do they flee?

Text (214 words):

> People are forced from home when staying would put their lives or freedom at risk. The panel shows three groups that the UN Refugee Agency (UNHCR) counts, with a figure for each.
>
> Refugees have fled their own country and found safety in another. They cannot go back because they fear persecution for who they are or what they believe, or because of war, violence or serious public disorder. A UN treaty of 1951, the Refugee Convention, defines who is a refugee, and refugees have rights under international law.
>
> Asylum-seekers have asked another country for protection and are waiting for a decision. Some will be recognised as refugees and some will not. Every refugee starts out this way.
>
> Internally displaced people have fled their homes but are still inside their own country. Their own government is responsible for protecting them, but it may be unable or unwilling to do so.
>
> Why do people flee? War and other violence, persecution, abuses of human rights, and disasters force people from home. Many cannot return for years, sometimes decades, because it is still not safe. Where they settle, shelter, food, clean water and health care may be scarce.
>
> The panel gives UNHCR's figures for the country, with their date. People are counted under the country they fled from.

- Sources:
  - [Refugees](https://www.unhcr.org/about-unhcr/who-we-protect/refugees) (UN Refugee Agency (UNHCR), 24 Nov 2026)
  - [Asylum-seekers](https://www.unhcr.org/about-unhcr/who-we-protect/asylum-seekers) (UN Refugee Agency (UNHCR), 24 Nov 2026)
  - [Internally Displaced People](https://www.unhcr.org/about-unhcr/who-we-protect/internally-displaced-people) (UN Refugee Agency (UNHCR), 24 Nov 2026)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24 in the built-in browser (unhcr.org gives scripts HTTP 403); the pages show no date, so each is dated the day it was read. Refugees: fled their country to escape conflict, violence or persecution; unable to return for fear of persecution or because of armed conflict, violence or serious public disorder; the 1951 Refugee Convention definition; rights and protections under international law; often unsafe to return for years if not decades. Asylum-seekers: seeking international protection, claim not yet decided; not all are found to be refugees, but all refugees were once asylum-seekers (paraphrased). Internally displaced people: forced to flee by conflict, violence, persecution or disasters but still inside their country; the national government has the primary responsibility and may be unable or unwilling; places of refuge may lack shelter, food, clean water or health care; protracted displacement for years or decades. The pages' global totals are not used (they go stale). Counting by country of origin is the panel's own (UNHCR population data).
- Status: **approved** on 25 Nov 2026

### `access`: Getting help in: what is humanitarian access?

Text (263 words):

> Humanitarian access is about two things. Can people in need reach aid and basic services? And can aid workers reach the people who need them? When either is blocked, help is harder to deliver.
>
> The Getting help in section uses scores from ACAPS, an organisation that analyses humanitarian crises. ACAPS rates access in each country it covers on nine indicators, grouped in three pillars.
>
> The first pillar asks whether people can get aid. For example, do the authorities deny that needs exist, or block people from services? The second asks whether aid groups can work. Can they enter the country and move around in it? Is their work interfered with, or are their staff and supplies attacked? The third covers dangers and obstacles: fighting, landmines and other explosives, and hard terrain, climate or poor roads.
>
> Each indicator is scored from 0 to 3. The scores are combined into one score from 0 to 5. ACAPS names the levels from No constraints at 0 to Extreme at 5. Where ACAPS had too little information, the panel says there is no score.
>
> Why is access blocked? ACAPS points to red tape, such as permits and many separate approvals. It also points to insecurity and fighting, attacks on aid workers, explosive remnants of war, and the land and climate. Some authorities also deregister aid groups or prosecute their staff.
>
> Each ACAPS edition covers six months. The panel shows the edition the app has, with its date. It also lists the nearest airport, port and UN depot. Those distances are straight lines, not road or sea routes.

- Sources:
  - [Humanitarian access overview (July 2026 edition)](https://www.acaps.org/en/thematics/all-topics/humanitarian-access-overview) (ACAPS, 10 Aug 2026)
  - [Humanitarian Access Overview, December 2025: Spotlight on fragmented contexts: Lebanon, Palestine, Syria and Yemen](https://www.acaps.org/fileadmin/Data_Product/Main_media/20260210_ACAPS_Humanitarian_Access_Overview_December_2025.pdf) (ACAPS, 10 Feb 2026)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24. ACAPS Humanitarian access overview page (the July 2026 edition, covering December 2025 to May 2026, published 10 Aug 2026 per docs/data-sources.md): access means the barriers people face in reaching assistance and the barriers organisations face in reaching them; nine indicators in three pillars (people's access to aid; responders' access to people; physical, environmental and security constraints), each indicator scored 0 to 3 or X when information is missing, combined into a 0 to 5 score; causes named: bureaucratic and administrative impediments (border restrictions, documentation, multiple sequential approvals), insecurity, violence against aid workers, landmines and explosive remnants of war, physical and environmental obstacles, and policy measures such as deregistration of organisations and prosecution of aid staff. December 2025 overview PDF (the edition the app shows; the file name gives 10 Feb 2026): the same three pillars and nine indicators, the 0-3 and 0-5 scales, the six levels (the PDF's wording runs from no significant to extreme access constraints; the explainer uses ACAPS's level names No constraints to Extreme, as the panel and legend do, ruling R5), and the fixed six-month data window (June to November 2025). The nearest-hub lines are the app's own (C3).
- Status: **approved** on 25 Nov 2026

### `why-these`: Why these? How the app chooses what to list

Text (292 words):

> Your one small step lists ways to give and people to contact. The app never takes money and never ranks charities. The UN's fund for the country comes first, or the UN's global emergency fund, CERF, where there is none. Charities follow in alphabetical order.
>
> Each charity must pass three checks. First, gifts to it must be tax-deductible. Before each release, the list is checked against the IRS list called Publication 78, which the IRS Tax Exempt Organization Search shows. It is also checked against the IRS list of groups that have lost their tax-exempt status.
>
> Second, an evaluator must rate it well. Charity Navigator must give it 3 stars or more, or the BBB Wise Giving Alliance must find that it meets all 20 of its Standards for Charity Accountability. Those standards cover the board, measuring results, finances and honest appeals. The app links to the evaluators' own reports and never copies their ratings.
>
> Third, there must be evidence that the charity is working in that country, such as UN records of who works where, or the charity's own programme page. The panel shows that evidence and the date it was checked.
>
> An entry expires 12 months after its last review. It is then hidden, never shown out of date.
>
> The panel also gives each charity's share of spending on programmes, from its own IRS Form 990. Read it with care, because a programme share alone can mislead. Groups working in hard-to-reach places spend more on logistics and staff, and that is the real work. The evaluators themselves have urged donors not to judge a charity by its overhead alone.
>
> The Federal Trade Commission gives the same advice before you give: check the IRS search, and look at what evaluators say.

- Sources:
  - [Tax Exempt Organization Search](https://www.irs.gov/charities-non-profits/tax-exempt-organization-search) (Internal Revenue Service (IRS), 18 Nov 2026)
  - [Before Giving to a Charity](https://consumer.ftc.gov/articles/giving-charity) (Federal Trade Commission (FTC), Consumer Advice, 1 Sep 2025)
  - [How We Rate Charities (Ratings)](https://www.charitynavigator.org/about-us/our-methodology/ratings/) (Charity Navigator, 24 Nov 2026)
  - [BBB Standards for Charity Accountability](https://give.org/bbb-standards-for-charity-accountability) (BBB Wise Giving Alliance, 24 Nov 2026)
  - [Charities Urged to Crush Overhead Myth and Take Actions Toward an Overhead Solution](https://give.org/news/charities-urged-to-crush-overhead-myth-and-take-actions-toward-an-overhead-solution) (BBB Wise Giving Alliance, 17 Jul 2020)
- Text checked against its sources on: 24 Nov 2026
- What was opened, and when: Opened on 2026-11-24. IRS Tax Exempt Organization Search page (built-in browser; scripts get 403; last reviewed or updated 18 Nov 2026): check eligibility to receive tax-deductible contributions (Pub. 78 data) and the Automatic Revocation of Exemption List. FTC Before Giving to a Charity (consumer.ftc.gov, dated September 2025; the day is not given, so the 1st is used): confirm tax-deductibility with the IRS Tax Exempt Organization Search; evaluators BBB Wise Giving Alliance and Charity Watch keep lists of well-rated charities; ask how much goes to programmes. Evaluators' method pages only, each opened once in the built-in browser as a normal page view (never a charity profile; the plan's rule P4-8 bars scripts from their sites): Charity Navigator How We Rate Charities (zero to four stars from a 0-100 score; only US 501(c)(3) public charities with three years of Form 990 filings), BBB Standards for Charity Accountability (20 standards, each met, not met or unable to verify; governance and oversight, measuring effectiveness, finances, solicitations and informational materials), and give.org's Overhead Myth news item (posted 17 Jul 2020, the joint letter of 21 Oct 2014 by GuideStar, Charity Navigator and BBB Wise Giving Alliance denouncing the overhead ratio as a primary measure of performance). The thresholds (3 stars or more; all 20 standards) are the app's rule (spec Q1, profile.json). The hard-to-reach sentence is the spec's own caution (section 9.3), not a quotation from these sources. The 12-month expiry, the evidence rule and the order are the app's (spec 9.2, P4-9, P4-11).
- Status: **approved** on 25 Nov 2026

## Awaiting confirmation / not entered

Researched for this table but not entered. Links marked *lead* (news, studies) are never citations: a row is entered only
from the authority's own site.

### `icj-gambia-v-myanmar-merits-2026`: ICJ, The Gambia v. Myanmar (11 States intervening): Judgment on the merits of 19 November 2026

- Status: **awaiting confirmation**
- Why: The case page on icj-cij.org lists the Judgment of 19 November 2026, press release 2026/30 ('The Court delivers its Judgment on the merits'), Summary 2026/3 and 17 opinions and declarations, among them a joint dissenting opinion of Vice-President Sebutinde, Judges Tomka, Abraham, Xue, Nolte and Judge ad hoc Kress. The judgment, summary and press-release PDFs are behind icj-cij.org's bot check (HTTP 403 for automated requests; the built-in browser stopped at 'Performing security verification'), which was not bypassed. So what the Court found could not be read on the Court's own site. News reports (leads only) say that the Court, by nine votes to six, found Myanmar responsible for genocide against the Rohingya in the 2016-2017 clearance operations.
- Owner: Confirm the operative paragraph from the Court's text (summary or judgment PDF, in a normal browser). If it confirms, add a CourtJudgment row for MMR dated 2026-11-19 with the judgment's title and URL, the term and group as the operative paragraph words them; Claude can do this from the text.
- Links:
  - [ICJ case page: Application of the Convention on the Prevention and Punishment of the Crime of Genocide (The Gambia v. Myanmar: 11 States intervening)](https://www.icj-cij.org/case/178) (authority)
  - [Judgment of 19 November 2026 (PDF)](https://www.icj-cij.org/sites/default/files/case-related/178/178-20261119-jud-01-00-en.pdf) (authority)
  - [Press release 2026/30: The Court delivers its Judgment on the merits (PDF)](https://www.icj-cij.org/sites/default/files/case-related/178/178-20261119-pre-01-00-en.pdf) (authority)
  - [Summary 2026/3 of the Judgment of 19 November 2026 (PDF)](https://www.icj-cij.org/sites/default/files/case-related/178/178-20261119-sum-01-00-en.pdf) (authority)
  - [UN News: World's top court finds Myanmar guilty of genocide against Rohingya](https://news.un.org/en/story/2026/11/1168836) (*lead*, not a citation)

### `us-earlier-genocide-conclusions`: United States: the four earlier genocide conclusions (before 2001) that the State Department counts

- Status: **not entered**
- Why: The State Department's Burma page says that since the Holocaust 'the United States has concluded only seven other times that genocide was committed'. Three of the seven are in the table (Darfur 2004, ISIS 2016, Xinjiang 2021), so four are earlier. The State Department does not name them, and none is on state.gov or its archives (2001 onward). A 2019 USHMM study (a lead, not a citation) names Bosnia (1993), Rwanda (1994) and Iraq (1995, the campaign against the Kurds), describes a 1989 State Department statement on Cambodia (the Khmer Rouge, testimony of the deputy legal adviser in November 1989), and says a mid-1990s finding on Burundi is suggested by State records but unconfirmed. Which four the Department counts is therefore not certain; Cambodia, Bosnia, Rwanda and Iraq are the likeliest. These were 'acts of genocide' statements, not formal written determinations. governments.json's scope says they are not listed.
- Owner: Decide whether the United States still counts as curated completely (C15) with this scope, or whether these four should be researched in another official archive.
- Links:
  - [US Department of State: Burma Genocide ('only seven other times')](https://2021-2025.state.gov/burma-genocide/) (authority)
  - [Todd Buchwald and Adam Keith, By Any Other Name: How, When, and Why the US Government Has Made Genocide Determinations (USHMM, 2019)](https://www.ushmm.org/m/pdfs/Todd_Buchwald_Report_031819.pdf) (*lead*, not a citation)

### `us-isis-reaffirmation-2017`: United States: Secretary Tillerson's reaffirmation of 15 August 2017 that ISIS committed genocide

- Status: **not entered**
- Why: The page on 2017-2021.state.gov returned 'Technical Difficulties ... Exception: forbidden' on every try, so it could not be read on the State Department's site. It reaffirms the 2016 determination, which is entered (us-isis-2016-irq, us-isis-2016-syr).
- Owner: Optional: open the page and, if it reads as news reports say, add it as a determination dated 2017-08-15.
- Links:
  - [Remarks at the Release of the 2016 International Religious Freedom Report](https://2017-2021.state.gov/remarks-at-the-release-of-the-2016-international-religious-freedom-report/) (authority)

### `icc-other-warrants`: ICC arrest warrants other than the Palestine ones (Darfur, including Omar Al Bashir's 2010 warrant for genocide; Afghanistan, 2025; Ukraine, 2024 warrants charging crimes against humanity; Libya) and the ICC's October 2025 Darfur trial judgment

- Status: **not entered**
- Why: icc-cpi.int answered one page (the 21 November 2024 Palestine press release, which is entered) and then its Gcore security service blocked every request, including from the built-in browser ('Sorry, you have been blocked'). It was not bypassed. Nothing that could not be read on the Court's site was entered. For Myanmar, the IIMM report A/HRC/63/19 (para. 49) says only that the ICC Prosecutor requested a warrant for Senior General Min Aung Hlaing in November 2024; no warrant issued was found, so there is no row.
- Owner: Research again later from icc-cpi.int (one request at a time).
- Links:
  - [ICC: Al Bashir case](https://www.icc-cpi.int/darfur/albashir) (authority)
  - [Report of the Independent Investigative Mechanism for Myanmar (A/HRC/63/19), para. 49](https://iimm.un.org/sites/default/files/2026/08/2026 IIMM Annual Report.pdf) (authority)

### `ictr-akayesu-appeal-2001`: ICTR, Akayesu: appeal judgement (2001)

- Status: **not entered**
- Why: The ICTR case page refused automated access (HTTP 403), so the appeal could not be checked. The trial judgement is entered.
- Links:
  - [ICTR case page ICTR-96-4 (Akayesu)](https://unictr.irmct.org/en/cases/ictr-96-4) (authority)

### `eccc-002-02-appeal`: ECCC, Case 002/02: the Supreme Court Chamber's appeal judgement (reported for 2022)

- Status: **not entered**
- Why: Not opened on eccc.gov.kh in this task; a search result (a lead) put the appeal judgement in September 2022 and says it upheld Khieu Samphan's conviction for genocide of the Vietnamese. The trial judgement is entered.
- Links:
  - [ECCC home page](https://www.eccc.gov.kh/en) (authority)

### `dprk-continuing-hrc-res-61-27`: A continuing statement for the DPRK Commission of Inquiry row (Human Rights Council resolution 61/27, 30 March 2026)

- Status: **not entered**
- Why: The resolution 'condemns ... the long-standing and ongoing systematic, widespread and gross human rights violations' and recalls the commission's findings, but it is a resolution, not a report, and does not itself say the crimes against humanity continue. So the DPRK row is history only; the camps still show.
- Owner: Decide whether this resolution qualifies as a continuing statement (C14).
- Links:
  - [A/HRC/RES/61/27, Situation of human rights in the Democratic People's Republic of Korea](https://documents.un.org/doc/UNDOC/GEN/G26/048/69/PDF/G2604869.PDF) (authority)

### `ffm-sudan-later-reports`: Fact-Finding Mission for the Sudan: the July 2026 paper (reported as A/HRC/62/CRP.4) and the September 2026 report A/HRC/63/23

- Status: **not entered**
- Why: The ohchr.org link to A/HRC/62/CRP.4 given by UN News returned 'page not found'. documents.un.org redirected every request for A/HRC/63/23 ('Fuelling Sudan's conflict: weapons, fighters and external support networks') to its error page, and no ohchr.org copy was found. Neither could be opened in this task. The Task 5 code reviewer read A/HRC/63/23 on 2026-11-23 and found that it does not say atrocity crimes continue (the reviewer's check, not this task's).
- Links:
  - [A/HRC/63/23 in the UN Document System viewer](https://docs.un.org/en/A/HRC/63/23) (authority)
  - [UN News, July 2026, on the mission's new report](https://news.un.org/en/story/2026/07/1167897) (*lead*, not a citation)

### `not-researched`: Not researched in this task: the Syria Commission of Inquiry's later reports (including its 2016 finding of genocide by ISIS against the Yazidis), the Commission of Inquiry on Ukraine (a possible continuing statement for the US Russia row), the Ethiopia experts' 2023 report, and continuing statements for Xinjiang, Eritrea and Burundi

- Status: **not entered**
- Why: Outside the brief's list or its time; each would need checking on the authority's own site.
