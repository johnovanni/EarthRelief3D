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
  one on their own; when the country has a triangle, those with a recent statement are listed with it. The rest is
  listed under the panel's history.
- A court judgment whose outcome is "not established" is never drawn and never counts as a crisis; the panel lists it
  under "Cases decided: not established".
- A camp keeps its marker unless its newest cited report says it closed (`reportedClosed`, ruling R14b); a closed camp
  is listed as history. Where sources differ on a camp's status, each statement is shown with its source (`statusNotes`).

Totals: 31 designations, 5 camp sites, 1 government(s), 9 candidates awaiting confirmation or not entered (last section).

## Governments offered in Settings (C15)

- **United States government** (`USA`): curated completely: yes; checked 23 Nov 2026.
  Scope: Secretary of State determinations published on state.gov and its archives, 2001 onward. Not listed: four earlier genocide conclusions (1989–1995) that the State Department counts but that are not on those sites

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- **Question for the owner:** Should this genocide determination count toward Sudan's triangle? Its continuing statement (A/HRC/61/77, paras 123-124) says RSF atrocities show 'continuity and escalation' and that 'the risk of further genocidal violence remains acute'; it supports continuing RSF atrocities, not that the genocide itself continues. If not, remove the continuing statement from this row and it becomes panel history (the FFM and the 2023 determination still give Sudan its triangle).
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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
- **Question for the owner:** The sources differ: the UN Commission of Inquiry (2014, A/HRC/25/CRP.1 para. 739) reported the site closed in 2006; HRNK's newer report (2024) says smaller sections 'remain active as a detention facility'. Under ruling R14b the newest report decides, so the camp keeps its marker and counts as a current crisis, with both statements shown. Approve this, or correct it?
- Status: **draft**
- Owner: approve / correct: 

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
- Status: **draft**
- Owner: approve / correct: 

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

### `us-earlier-genocide-conclusions`: United States: the four earlier genocide conclusions (1989-1995) that the State Department counts

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
