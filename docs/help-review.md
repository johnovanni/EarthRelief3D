# Help catalog: UN funds, official contacts and charities, for the owner's review

Generated from `data/help/pooled-funds.json` and `data/countries/<ISO2>/*.json` by `HelpTablesTests.ReviewDocumentIsCurrent`
(run the tests with `ER3D_WRITE_REVIEW=1` to rewrite it). Do not edit by hand: correct the JSON, then regenerate.

Every row starts as a **draft** (ruling P4-13). The app shows drafts only with the diagnostic `--show-drafts`, labelled
"Draft: not yet reviewed". To approve a row, set its `review` to `{ "status": "approved", "approvedOn": "<date>" }` in the
JSON. To correct it, change the JSON (or tell Claude what to change).

How the panel uses them ("Your one small step"):
- A country with an approved UN pooled fund shows it first, with OCHA's page and OCHA's "how to give" page; every other country,
  and every country while the funds are drafts, shows CERF (P4-11). The app never takes a gift: every line links to an official page.
- Charities follow, alphabetically by name, never ranked. A charity shows only when every check holds (P4-8, P4-9): approved by you;
  an IRS Publication 78 check; a confirmed rating from a listed evaluator; evidence it works in that country; and its review and
  evidence no more than 12 months old. The app shows "Rated by <evaluators>" with links to their reports, never stars or scores.
  With none, the panel says "No vetted charities listed yet".
- Official contacts need your approval too. The panel links to the House's own finder (the app never asks for an address or ZIP
  code), and to the UN Blue Book for each country's mission (one link: the Blue Book's terms forbid compiling its list).

Totals: 21 pooled funds (including CERF), 4 official contacts, 16 charities, 100 senators (a list copied from the Senate's own file, as of 18 Nov 2026; not reviewed row by row).

## UN pooled funds and CERF

Each fund's page is the row's citation. OCHA has no per-fund donation page: its "how to give" page is shared by all its funds. A regional
fund covers the countries OCHA's CBPF data lists as its sub-funds.

### `afghanistan-humanitarian-fund`: Afghanistan Humanitarian Fund

- Covers: AFG
- Page: [Afghanistan Humanitarian Fund](https://www.unocha.org/afghanistan-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `central-african-republic-humanitarian-fund`: Central African Republic Humanitarian Fund

- Covers: CAF
- Page: [Central African Republic Humanitarian Fund](https://www.unocha.org/central-african-repulic/about-central-african-repulic-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `democratic-republic-congo-humanitarian-fund`: Democratic Republic of the Congo Humanitarian Fund

- Covers: COD
- Page: [Democratic Republic of the Congo Humanitarian Fund](https://www.unocha.org/democratic-republic-congo-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `ethiopia-humanitarian-fund`: Ethiopia Humanitarian Fund

- Covers: ETH
- Page: [Ethiopia Humanitarian Fund](https://www.unocha.org/ethiopia-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `lebanon-humanitarian-fund`: Lebanon Humanitarian Fund

- Covers: LBN
- Page: [Lebanon Humanitarian Fund](https://www.unocha.org/lebanon-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `myanmar-humanitarian-fund`: Myanmar Humanitarian Fund

- Covers: MMR
- Page: [Myanmar Humanitarian Fund](https://www.unocha.org/myanmar-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `nigeria-humanitarian-fund`: Nigeria Humanitarian Fund

- Covers: NGA
- Page: [Nigeria Humanitarian Fund](https://www.unocha.org/nigeria-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `occupied-palestinian-territory-humanitarian-fund`: Occupied Palestinian Territory Humanitarian Fund

- Covers: PSE
- Page: [Occupied Palestinian Territory Humanitarian Fund](https://www.unocha.org/occupied-palestinian-territory-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `regional-humanitarian-fund-latin-america-and-caribbean`: Regional Humanitarian Fund for Latin America and the Caribbean

- Covers: COL, GTM, HND, HTI, SLV
- Page: [Regional Humanitarian Fund for Latin America and the Caribbean](https://www.unocha.org/regional-humanitarian-fund-latin-america-and-caribbean)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `regional-humanitarian-pooled-fund-asia-pacific`: Regional Humanitarian Pooled Fund for Asia-Pacific

- Covers: BGD, FJI, LAO, LKA, NPL, PAK, PHL, PNG, SLB, TON, VUT
- Page: [Regional Humanitarian Pooled Fund for Asia-Pacific](https://www.unocha.org/regional-humanitarian-pooled-fund-asia-pacific)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `regional-humanitarian-pooled-fund-eastern-and-southern-africa`: Regional Humanitarian Pooled Fund for Eastern and Southern Africa

- Covers: KEN, UGA
- Page: [Regional Humanitarian Pooled Fund for Eastern and Southern Africa](https://www.unocha.org/regional-humanitarian-pooled-fund-eastern-and-southern-africa)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `regional-humanitarian-pooled-fund-west-and-central-africa`: Regional Humanitarian Pooled Fund for West and Central Africa

- Covers: BFA, MLI, NER, TCD
- Page: [Regional Humanitarian Pooled Fund for West and Central Africa](https://www.unocha.org/regional-humanitarian-pooled-fund-west-and-central-africa)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `somalia-humanitarian-fund`: Somalia Humanitarian Fund

- Covers: SOM
- Page: [Somalia Humanitarian Fund](https://www.unocha.org/somalia-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `south-sudan-humanitarian-fund`: South Sudan Humanitarian Fund

- Covers: SSD
- Page: [South Sudan Humanitarian Fund](https://www.unocha.org/south-sudan-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `sudan-humanitarian-fund`: Sudan Humanitarian Fund

- Covers: SDN
- Page: [Sudan Humanitarian Fund](https://www.unocha.org/sudan-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `syria-cross-border-humanitarian-fund`: Syria Cross-border Humanitarian Fund

- Covers: SYR
- Page: [Syria Cross-border Humanitarian Fund](https://www.unocha.org/syria-cross-border-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `syria-humanitarian-fund`: Syria Humanitarian Fund

- Covers: SYR
- Page: [Syria Humanitarian Fund](https://www.unocha.org/syria-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `ukraine-humanitarian-fund`: Ukraine Humanitarian Fund

- Covers: UKR
- Page: [Ukraine Humanitarian Fund](https://www.unocha.org/ukraine-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `venezuela-humanitarian-fund`: Venezuela Humanitarian Fund

- Covers: VEN
- Page: [Venezuela Humanitarian Fund](https://www.unocha.org/venezuela-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `yemen-humanitarian-fund`: Yemen Humanitarian Fund

- Covers: YEM
- Page: [Yemen Humanitarian Fund](https://www.unocha.org/yemen-humanitarian-fund)
- How to give: [https://www.unocha.org/our-funding](https://www.unocha.org/our-funding): "OCHA's page explains how to give to its humanitarian funds"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `cerf`: Central Emergency Response Fund (CERF)

- Covers: every country with no pooled fund of its own
- Page: [Central Emergency Response Fund (CERF)](https://cerf.un.org/)
- How to give: [https://cerf.un.org/donate](https://cerf.un.org/donate): "US tax-deductible gifts go through the UN Foundation; CERF's page explains how"
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

## Official contacts

United States (`US`): charities are checked in the IRS Tax Exempt Organization Search (Publication 78) ([search page](https://apps.irs.gov/app/eos/)); evaluators accepted:
- [Charity Navigator](https://www.charitynavigator.org/): qualifies at: 3 stars or more
- [BBB Wise Giving Alliance](https://give.org/): qualifies at: meets all 20 Standards for Charity Accountability

### `senators`: Your two senators

- Kind: `Senators`
- Link: none (the senators are listed from senators.json by the state chosen in Settings)
- Note: Each senator's own contact form
- Source: [Contact information for US senators (senators_cfm.xml)](https://www.senate.gov/general/contact_information/senators_cfm.xml) (United States Senate, 18 Nov 2026)
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `representative`: Your representative in the House

- Kind: `Representative`
- Link: [https://www.house.gov/representatives/find-your-representative](https://www.house.gov/representatives/find-your-representative)
- Note: The House's own page finds your representative; this app never asks for an address or ZIP code
- Source: [Find Your Representative | house.gov](https://www.house.gov/representatives/find-your-representative) (US House of Representatives, 24 Nov 2026)
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `state-department`: The State Department

- Kind: `Link`
- Link: [https://register.state.gov/contactus/contactusform](https://register.state.gov/contactus/contactusform)
- Note: Its general contact form is at the end of the page
- Source: [U.S. Department of State - Contact Us](https://register.state.gov/contactus/contactusform) (US Department of State, 24 Nov 2026)
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

### `un-mission`: A country's mission to the UN

- Kind: `UnMission`
- Link: [https://www.un.org/dgacm/en/content/protocol/blue-book](https://www.un.org/dgacm/en/content/protocol/blue-book)
- Note: The UN Blue Book lists the missions to the United Nations
- Source: [The Blue Book | Department for General Assembly and Conference Management](https://www.un.org/dgacm/en/content/protocol/blue-book) (United Nations, 24 Nov 2026)
- Checked on: 24 Nov 2026
- Status: **approved** on 25 Nov 2026

## Charities

### `action-against-hunger-usa`: Action Against Hunger USA

- EIN: 13-3327220; website: [https://www.actionagainsthunger.org/](https://www.actionagainsthunger.org/); donate page: [https://www.actionagainsthunger.org/take-action/donate/](https://www.actionagainsthunger.org/take-action/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 90.4% (Form 990, tax year 2024, object 202543149349301514, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/133327220), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/133327220](https://www.charitynavigator.org/ein/133327220)
- Evaluator page to open: [https://www.give.org/search?term=Action+Against+Hunger+USA](https://www.give.org/search?term=Action+Against+Hunger+USA)
- Works in AFG: [Hunger Relief in Afghanistan | Action Against Hunger](https://www.actionagainsthunger.org/location/asia/afghanistan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [Hunger Relief in Burkina Faso | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/burkina-faso/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [Hunger Relief in Central African Republic | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/central-african-republic/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [Hunger Relief in Cameroon | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/cameroon/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [Hunger Relief in Democratic Republic of Congo | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/democratic-republic-congo/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COL: [Hunger Relief in Colombia | Action Against Hunger](https://www.actionagainsthunger.org/location/americas/colombia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [Hunger Relief in Ethiopia | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/ethiopia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [Hunger Relief in Haiti | Action Against Hunger](https://www.actionagainsthunger.org/location/americas/haiti/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [Hunger Relief in Mozambique | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/mozambique/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [Hunger Relief in Nigeria | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/nigeria/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [Hunger Relief in Sudan | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/sudan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [Hunger Relief in Somalia | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/somalia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [Hunger Relief in Syria | Action Against Hunger](https://www.actionagainsthunger.org/location/middle-east/syria/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [Hunger Relief in Chad | Action Against Hunger](https://www.actionagainsthunger.org/location/africa/chad/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `american-red-cross`: American Red Cross

- EIN: 53-0196605; website: [https://www.redcross.org/](https://www.redcross.org/); donate page: [https://www.redcross.org/donate/donation.html](https://www.redcross.org/donate/donation.html)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 90.9% (Form 990, tax year 2024, object 202640829349300109, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/530196605), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/530196605](https://www.charitynavigator.org/ein/530196605)
- Evaluator page to open: [https://www.give.org/search?term=American+Red+Cross](https://www.give.org/search?term=American+Red+Cross)
- Works in COL: [Colombia: Red Cross response to earthquake ongoing](https://www.redcross.org/about-us/news-and-events/news/2026/colombia--red-cross-response-to-earthquake-ongoing.html) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [Helping All Around the World: American Red Cross contribution to the IFRC Mozambique flood appeal (multi-country International Services Activity Update, 20 Mar 2026)](https://www.redcross.org/about-us/news-and-events/news/2026/helping-all-around-the-world.html) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [How Dr. Felix Amate Brings Lifesaving Vaccines to Chad’s Most Remote Regions](https://www.redcross.org/about-us/news-and-events/news/2026/bringing-lifesaving-vaccines-to-chad-s-most-remote-regions.html) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in VEN: [Venezuela: Red Cross Movement Responds in the Aftermath of Back-to-Back Earthquakes](https://www.redcross.org/about-us/news-and-events/news/2026/red-cross-movement-responds-to-venezuela-earthquakes.html) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: Drafted under OR-5 (named by the owner). OCHA 3W is not used: the 3W file never names the American Red Cross (OR-4's Red Cross and Red Crescent Movement count of 17 countries comes from the ICRC, the IFRC and other national societies). redcross.org has no current country pages for its international work: its Haiti page describes past work and its Ukraine crisis page dates from 2024, so neither is used. Each row is a 2026 page on redcross.org about the American Red Cross's own work in that country: news articles for Colombia, Chad and Venezuela, and the International Services Activity Update of 20 March 2026 for Mozambique (a page covering several countries). EIN matched by name in Publication 78 (American National Red Cross); the redcross.org pages opened did not show the EIN.
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `care-usa`: CARE USA

- EIN: 13-1685039; website: [https://www.care.org/](https://www.care.org/); donate page: [https://www.care.org/donate/](https://www.care.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 89.8% (Form 990, tax year 2024, object 202523579349300712, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/131685039), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/131685039](https://www.charitynavigator.org/ein/131685039)
- Evaluator page to open: [https://www.give.org/search?term=CARE+USA](https://www.give.org/search?term=CARE+USA)
- Works in AFG: [CARE's Humanitarian Work in Afghanistan - CARE](https://www.care.org/our-work/where-we-work/afghanistan/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [CARE's Humanitarian Work in Cameroon - CARE](https://www.care.org/our-work/where-we-work/cameroon/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [CARE's Humanitarian Work in Democratic Republic of the Congo - CARE](https://www.care.org/our-work/where-we-work/democratic-republic-of-the-congo/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [CARE's Humanitarian Work in Colombia - CARE](https://www.care.org/our-work/where-we-work/colombia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [CARE's Humanitarian Work in Ethiopia - CARE](https://www.care.org/our-work/where-we-work/ethiopia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [CARE's Humanitarian Work in Haiti - Haiti Relief - CARE](https://www.care.org/our-work/where-we-work/haiti/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [CARE's humanitarian work in Mali - CARE](https://www.care.org/our-work/where-we-work/mali/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [CARE's humanitarian work in Mozambique - CARE](https://www.care.org/our-work/where-we-work/mozambique/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [CARE's humanitarian work in Niger - CARE](https://www.care.org/our-work/where-we-work/niger/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [CARE's humanitarian work in Sudan - CARE](https://www.care.org/our-work/where-we-work/sudan/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [CARE's humanitarian work in Somalia - CARE](https://www.care.org/our-work/where-we-work/somalia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [CARE's Humanitarian Work in Syria - Donate to Syria - CARE](https://www.care.org/our-work/where-we-work/syria/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [CARE's Humanitarian Work in Chad - CARE](https://www.care.org/our-work/where-we-work/chad/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [CARE's Humanitarian Work in Yemen - CARE](https://www.care.org/our-work/where-we-work/yemen/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: EIN from care.org (its FAQ page and page footer give 13-1685039); Publication 78 lists that EIN as "C A R E, Inc.". CARE International's "How we're run" page (care-international.org, which care.org's CARE International Offices page gives as CARE International's address) lists CARE USA among the confederation's members. OCHA 3W rows name CARE International, its country offices or CARE Switzerland, not CARE USA by name (OR-4 counts the CARE confederation as one organisation).
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `doctors-of-the-world-usa`: Doctors of the World USA

- EIN: 35-2426718; website: [https://doctorsoftheworld.org/](https://doctorsoftheworld.org/); donate page: [https://doctorsoftheworld.org/donate/](https://doctorsoftheworld.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 96.7% (Form 990, tax year 2024, object 202533159349305398, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/352426718), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/352426718](https://www.charitynavigator.org/ein/352426718)
- Evaluator page to open: [https://www.give.org/search?term=Doctors+of+the+World+USA](https://www.give.org/search?term=Doctors+of+the+World+USA)
- Works in AFG: [Afghanistan | Doctors of the World](https://doctorsoftheworld.org/projects/afghanistan/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in BFA: [Burkina Faso | Doctors of the World](https://doctorsoftheworld.org/projects/burkina-faso/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [Central African Republic | Doctors of the World](https://doctorsoftheworld.org/projects/central-african-republic/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [Democratic Republic of Congo | Doctors of the World](https://doctorsoftheworld.org/projects/democratic-republic-of-congo/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [Ethiopia | Doctors of the World](https://doctorsoftheworld.org/projects/ethiopia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [Haiti | Doctors of the World](https://doctorsoftheworld.org/projects/haiti/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [Mali | Doctors of the World](https://doctorsoftheworld.org/projects/mali/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [Niger | Doctors of the World](https://doctorsoftheworld.org/projects/niger/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [Nigeria | Doctors of the World](https://doctorsoftheworld.org/projects/nigeria/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [Syria | Doctors of the World](https://doctorsoftheworld.org/projects/syria/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in VEN: [Progress made against Malaria in Venezuela | Doctors of the World](https://doctorsoftheworld.org/blog/progress-made-against-malaria-in-venezuela/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [Yemen | Doctors of the World](https://doctorsoftheworld.org/projects/yemen/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: Affiliate statement: doctorsoftheworld.org's home page (https://doctorsoftheworld.org/, opened 25 Nov 2026) says Doctors of the World USA is backed by the Médecins du Monde international network, and gives EIN 35-242-6718. OR-7: each country rests on a page on its own site that describes the network's work in that country, reached from the photo links on its regional pages (Africa, Asia, Americas & The Caribbean, Middle East): a project page (doctorsoftheworld.org/projects/<country>/) for AFG, BFA, CAF, COD, ETH, HTI, MLI, NER, NGA, SYR and YEM, and for VEN a post of 25 January 2024 on Doctors of the World Spain's malaria work in Sucre state since 2021 (the Venezuela photo links there; there is no Venezuela project page). These pages are not recent: where they give dates, the work they describe is from 2021 to 2023 (Venezuela January 2024), and none describes work in 2025 or 2026. OCHA 3W rows (2026) name Médecins du Monde sections (France, Belgium, Spain, Switzerland, Germany, Argentina) or Doctors of the World, not Doctors of the World USA by name (OR-4 counts the network as one organisation). Cameroon is in 3W but has no page on its site, so it is left out.
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `doctors-without-borders-usa`: Doctors Without Borders USA

- EIN: 13-3433452; website: [https://www.doctorswithoutborders.org/](https://www.doctorswithoutborders.org/); donate page: [https://donate.doctorswithoutborders.org/secure/donate](https://donate.doctorswithoutborders.org/secure/donate)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 83.5% (Form 990, tax year 2024, object 202503179349303040, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/133433452), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/133433452](https://www.charitynavigator.org/ein/133433452)
- Evaluator page to open: [https://www.give.org/search?term=Doctors+Without+Borders+USA](https://www.give.org/search?term=Doctors+Without+Borders+USA)
- Works in AFG: [Afghanistan | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/afghanistan) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in BFA: [Burkina Faso | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/burkina-faso) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [Central African Republic | How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/central-african-republic) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [Cameroon | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/cameroon) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [Democratic Republic of Congo | How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/democratic-republic-congo) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [Colombia | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/colombia) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [Ethiopia | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/ethiopia) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [Haiti | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/haiti) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in LBN: [Lebanon | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/lebanon) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [Mali | How to Help & FAQs | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/mali) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MMR: [Myanmar | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/myanmar) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [Mozambique | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/mozambique) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [Niger | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/niger) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [Nigeria | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/nigeria) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in PSE: [Palestine | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/palestine) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [Sudan | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/sudan) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [Somalia | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/somalia-and-somaliland) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SSD: [South Sudan | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/south-sudan) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [Syria | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/syria) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [Chad | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/chad) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in UKR: [Ukraine | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/ukraine) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in VEN: [Venezuela | Our Work & How to Help | Doctors Without Borders](https://www.doctorswithoutborders.org/what-we-do/where-we-work/venezuela) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [Yemen | Our Work & How to Help | Doctors Without Borders - USA](https://www.doctorswithoutborders.org/what-we-do/where-we-work/yemen) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: Drafted under OR-5 (named by the owner) and OR-4 (MSF's sections together are in 14 of the 3W countries). Country evidence is on doctorswithoutborders.org, MSF USA's own site (its footer gives federal tax ID 13-3433452); the pages were opened in the built-in browser because the site answers scripts with a "Checking connection" page. OCHA 3W rows name MSF's operational sections (France, Spain, Switzerland, Belgium, Holland, West and Central Africa), not MSF USA by name; COL, ETH, SOM, SSD and VEN rest on MSF USA's own pages only (OR-5). LBN, MMR, PSE and UKR are added because each has a 2026 humanitarian response plan or flash appeal in FTS and the page shows current work there; Honduras, Madagascar and Nepal were left out (not sure the pages show current crisis work). Earlier this week a give.org page was seen to list MSF USA as not meeting BBB's standards; this was not re-checked (no script or agent opens give.org), so BBB may not qualify it: please check Charity Navigator.
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `international-medical-corps`: International Medical Corps

- EIN: 95-3949646; website: [https://internationalmedicalcorps.org/](https://internationalmedicalcorps.org/); donate page: [https://internationalmedicalcorps.org/donate/](https://internationalmedicalcorps.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 87.1% (Form 990, tax year 2024, object 202611289349300921, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/953949646), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/953949646](https://www.charitynavigator.org/ein/953949646)
- Evaluator page to open: [https://www.give.org/search?term=International+Medical+Corps](https://www.give.org/search?term=International+Medical+Corps)
- Works in AFG: [Afghanistan: Humanitarian Aid, Healthcare and Emergency Relief](https://internationalmedicalcorps.org/country/afghanistan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [Central African Republic | International Medical Corps](https://internationalmedicalcorps.org/country/central-african-republic/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [Cameroon | International Medical Corps](https://internationalmedicalcorps.org/country/cameroon/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [Democratic Republic of the Congo | International Medical Corps](https://internationalmedicalcorps.org/country/democratic-republic-of-congo/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [Ethiopia: Drought, Displaced People and Humanitarian Aid](https://internationalmedicalcorps.org/country/ethiopia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [Mali | International Medical Corps](https://internationalmedicalcorps.org/country/mali/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [Nigeria | International Medical Corps](https://internationalmedicalcorps.org/country/nigeria/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [Sudan: Medical Care, Nutrition and Clean Water Programs](https://internationalmedicalcorps.org/country/sudan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [Somalia: Healthcare, Training and Local Partnerships](https://internationalmedicalcorps.org/country/somalia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [Syria: Rebuilding with Hope and Health](https://internationalmedicalcorps.org/country/syria/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [Chad | International Medical Corps](https://internationalmedicalcorps.org/country/chad/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [Venezuela | International Medical Corps](https://internationalmedicalcorps.org/country/venezuela/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [Yemen | International Medical Corps](https://internationalmedicalcorps.org/country/yemen/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `international-rescue-committee`: International Rescue Committee

- EIN: 13-5660870; website: [https://www.rescue.org/](https://www.rescue.org/); donate page: [https://help.rescue.org/donate](https://help.rescue.org/donate)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 86.8% (Form 990, tax year 2024, object 202601529349301355, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/135660870), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/135660870](https://www.charitynavigator.org/ein/135660870)
- Evaluator page to open: [https://www.give.org/search?term=International+Rescue+Committee](https://www.give.org/search?term=International+Rescue+Committee)
- Works in AFG: [Donate to help Afghanistan | IRC](https://www.rescue.org/country/afghanistan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [Donate to help Burkina Faso | IRC](https://www.rescue.org/country/burkina-faso) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [Central African Republic | International Rescue Committee](https://www.rescue.org/country/central-african-republic) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [Donate to help Cameroon | IRC](https://www.rescue.org/country/cameroon) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [Donate to help Democratic Republic of Congo | IRC](https://www.rescue.org/country/democratic-republic-congo) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [Donate to help Ethopia | IRC](https://www.rescue.org/country/ethiopia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [Donate to help Haiti | IRC](https://www.rescue.org/country/haiti) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [Donate to help Mali | IRC](https://www.rescue.org/country/mali) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [Donate to help Niger | IRC](https://www.rescue.org/country/niger) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [Donate to help Nigeria | IRC](https://www.rescue.org/country/nigeria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [Donate now | Sudan | International Rescue Committee (IRC)](https://www.rescue.org/country/sudan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [Donate to help Somalia | IRC](https://www.rescue.org/country/somalia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [Donate to help Syria | IRC](https://www.rescue.org/country/syria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [Donate to help Chad | IRC](https://www.rescue.org/country/chad) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [Donate to help Yemen | IRC](https://www.rescue.org/country/yemen) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `norwegian-refugee-council-usa`: Norwegian Refugee Council USA

- EIN: 47-5342860; website: [https://www.nrc.no/usa](https://www.nrc.no/usa); donate page: [https://support.nrcusa.org/global/~my-donation](https://support.nrcusa.org/global/~my-donation)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 85.8% (Form 990, tax year 2025, object 202632449349300733, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/475342860), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/475342860](https://www.charitynavigator.org/ein/475342860)
- Evaluator page to open: [https://www.give.org/search?term=Norwegian+Refugee+Council+USA](https://www.give.org/search?term=Norwegian+Refugee+Council+USA)
- Works in AFG: [NRC in Afghanistan | NRC](https://www.nrc.no/countries/asia/afghanistan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [NRC in Burkina Faso | NRC](https://www.nrc.no/countries/africa/burkina-faso) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [NRC in the Central African Republic | NRC](https://www.nrc.no/countries/africa/central-african-republic) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [NRC in Cameroon | NRC](https://www.nrc.no/countries/africa/cameroon) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [NRC in the Democratic Republic of the Congo | NRC](https://www.nrc.no/countries/africa/dr-congo) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [NRC in Ethiopia | NRC](https://www.nrc.no/countries/africa/ethiopia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [NRC in Mali | NRC](https://www.nrc.no/countries/africa/mali) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [NRC in Mozambique | NRC](https://www.nrc.no/countries/africa/mozambique) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [NRC in Nigeria | NRC](https://www.nrc.no/countries/africa/nigeria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [NRC in Sudan | NRC](https://www.nrc.no/countries/africa/sudan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [NRC in Somalia | NRC](https://www.nrc.no/countries/africa/somalia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [NRC in South Sudan | NRC](https://www.nrc.no/countries/africa/south-sudan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [OCHA 3W operational presence (HDX HAPI), South Sudan, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [NRC in Syria | NRC](https://www.nrc.no/countries/middle-east/syria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [NRC in Chad | NRC](https://www.nrc.no/countries/africa/chad) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [NRC in Venezuela | NRC](https://www.nrc.no/countries/south-america/venezuela) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [NRC in Yemen | NRC](https://www.nrc.no/countries/middle-east/yemen) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Note: EIN matched by name in Publication 78 only. Country evidence is on nrc.no, the international organisation's site; NRC USA's own page is itself part of nrc.no (www.nrc.no/usa).
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `oxfam-america`: Oxfam America

- EIN: 23-7069110; website: [https://www.oxfamamerica.org/](https://www.oxfamamerica.org/); donate page: [https://www.oxfamamerica.org/donate/](https://www.oxfamamerica.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 71.2% (Form 990, tax year 2025, object 202602269349303035, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/237069110), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/237069110](https://www.charitynavigator.org/ein/237069110)
- Evaluator page to open: [https://www.give.org/search?term=Oxfam+America](https://www.give.org/search?term=Oxfam+America)
- Works in CAF: [Central African Republic | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/central-african-republic) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [Democratic Republic of Congo | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/democratic-republic-congo) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [Colombia | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/colombia) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [Ethiopia crisis: Donate to save lives | Oxfam America | Oxfam](https://www.oxfamamerica.org/explore/countries/ethiopia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [Mali | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/mali) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [Mozambique | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/mozambique) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [Niger | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/niger) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [Somalia | Oxfam International](https://www.oxfam.org/en/what-we-do/countries/somalia) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [Donate to Syria | Oxfam America | Oxfam](https://www.oxfamamerica.org/explore/countries/syria/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [Yemen humanitarian crisis: Donate now | Oxfam America | Oxfam](https://www.oxfamamerica.org/explore/countries/yemen/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: EIN from oxfamamerica.org (its page footer gives EIN/tax ID 23-7069110). Affiliate statement (OR-6): its Keeping people safe page (https://www.oxfamamerica.org/about-us/safeguarding/, opened 25 Nov 2026) says Oxfam America is part of the Oxfam confederation, made up of more than 20 affiliate organisations; oxfam.org's How We Are Organized page lists Oxfam America (USA) among the current affiliates. oxfamamerica.org's home page and its Countries we work in page do not link to oxfam.org (an earlier note said they did; corrected 25 Nov 2026). Country evidence for CAF, COD, COL, MLI, MOZ, NER and SOM is Oxfam International's country pages on oxfam.org, each describing Oxfam's work there in the present tense; the Colombia page carries no date and the newest item it links to is from 2019, so it is the weakest. Burkina Faso, Chad and Nigeria were removed: their oxfam.org pages date themselves to 2018-2019 (2018 Human Development Index rankings; Nigeria also a 2019 inequality index) and show no later work. ETH, SYR and YEM are on oxfamamerica.org. OCHA 3W rows name Oxfam or its affiliates (Oxfam GB, Oxfam Novib, Oxfam Intermón), not Oxfam America by name (OR-4 counts the confederation as one organisation).
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `plan-international-usa`: Plan International USA

- EIN: 13-5661832; website: [https://planusa.org/](https://planusa.org/); donate page: [https://planusa.org/donate/](https://planusa.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 68.9% (Form 990, tax year 2024, object 202503369349300805, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/135661832), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/135661832](https://www.charitynavigator.org/ein/135661832)
- Evaluator page to open: [https://www.give.org/search?term=Plan+International+USA](https://www.give.org/search?term=Plan+International+USA)
- Works in CAF: [Plan International Central African Republic](https://plan-international.org/central-african-republic/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [Plan International Cameroon](https://plan-international.org/cameroon/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [Plan International Colombia](https://plan-international.org/colombia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [Plan International Ethiopia](https://plan-international.org/ethiopia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [Plan International Haiti](https://plan-international.org/haiti/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [Plan International Mali](https://plan-international.org/mali/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [Plan International Mozambique](https://plan-international.org/mozambique/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [Plan International Niger](https://plan-international.org/niger/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [Plan International Nigeria](https://plan-international.org/nigeria/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [Plan International Sudan](https://plan-international.org/sudan/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [Plan International Somalia](https://plan-international.org/somalia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [Plan International Chad](https://plan-international.org/chad/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: EIN matched by name in Publication 78 (Plan International USA Inc.); planusa.org links to a GuideStar profile with the same EIN. Affiliate statement (OR-6): planusa.org's Who we are page (https://planusa.org/who-we-are/, opened 25 Nov 2026) says Plan International USA is the U.S. member of the global organization (Plan International). planusa.org has regional pages only, so country evidence is Plan International's country-office pages on plan-international.org, which count because of that statement (the planusa.org pages opened, home, Where we work and Who we are, do not link to plan-international.org). Syria is in 3W but plan-international.org has no Syria page, so it is left out. OCHA 3W rows name Plan International, not Plan International USA by name. planusa.org/donate/ forwards to donate.planusa.org, which showed a bot check in the built-in browser, so the form itself was not seen.
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `save-the-children-us`: Save the Children US

- EIN: 06-0726487; website: [https://www.savethechildren.org/](https://www.savethechildren.org/); donate page: [https://www.savethechildren.org/us/donate](https://www.savethechildren.org/us/donate)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 77.6% (Form 990, tax year 2025, object 202642759349301919, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/060726487), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/060726487](https://www.charitynavigator.org/ein/060726487)
- Evaluator page to open: [https://www.give.org/search?term=Save+the+Children+US](https://www.give.org/search?term=Save+the+Children+US)
- Works in AFG: [Afghanistan Earthquake Relief - Donate to Help Afghan Children | Save The Children](https://www.savethechildren.org/us/where-we-work/afghanistan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [Donate to Help Children in Burkina Faso | Save The Children](https://www.savethechildren.org/us/where-we-work/burkina-faso) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [Help the Democratic Republic of Congo (DRC) | Save The Children](https://www.savethechildren.org/us/where-we-work/democratic-republic-of-congo) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COL: [Donate to Help Children in Colombia | Save The Children](https://www.savethechildren.org/us/where-we-work/colombia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [Donate to Children and Refugees in Ethiopia | Save The Children](https://www.savethechildren.org/us/where-we-work/ethiopia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [Donate to Help Children and Refugees in Haiti | Save The Children](https://www.savethechildren.org/us/where-we-work/haiti) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [Donate to Help Children in Mali | Save the Children](https://www.savethechildren.org/us/where-we-work/mali) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [Donate to Help Children in Mozambique | Save The Children](https://www.savethechildren.org/us/where-we-work/mozambique) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [Donate to Help Children in Niger | Save The Children](https://www.savethechildren.org/us/where-we-work/niger) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [Donate to Help Children in Nigeria | Save The Children](https://www.savethechildren.org/us/where-we-work/nigeria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [Donate to Help Children and Refugees in Sudan | Save The Children](https://www.savethechildren.org/us/where-we-work/sudan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [Donate to Help Children in Somalia Hunger Crisis | Save The Children](https://www.savethechildren.org/us/where-we-work/somalia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [Donate to Help Children and Refugees in Syria | Save The Children](https://www.savethechildren.org/us/where-we-work/syria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [Donate to Help Children in Venezuela | Save the Children](https://www.savethechildren.org/us/where-we-work/venezuela) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [Donate to Help Children in Yemen Crisis | Save The Children](https://www.savethechildren.org/us/where-we-work/yemen) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Note: Its US site does not state membership of Save the Children International in so many words: its FAQ sends donors outside the US to Save the Children International for their local Save the Children office, and savethechildren.net describes Save the Children as a global membership organisation of Save the Children International and its national members. Please confirm it is the US member.
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `unicef-usa`: UNICEF USA

- EIN: 13-1760110; website: [https://www.unicefusa.org/](https://www.unicefusa.org/); donate page: [https://www.unicefusa.org/how-help/donate](https://www.unicefusa.org/how-help/donate)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 83.1% (Form 990, tax year 2024, object 202630309349300223, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/131760110), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/131760110](https://www.charitynavigator.org/ein/131760110)
- Evaluator page to open: [https://www.give.org/search?term=UNICEF+USA](https://www.give.org/search?term=UNICEF+USA)
- Works in AFG: [Support Children in Afghanistan with UNICEF | UNICEF USA](https://www.unicefusa.org/where-unicef-works/asia/afghanistan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [UNICEF in Burkina Faso | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/africa/burkina-faso) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [UNICEF in the Central African Republic | UNICEF USA](https://www.unicefusa.org/where-unicef-works/africa/central-african-republic) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [UNICEF in Cameroon | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/africa/cameroon) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [Help Children in Crisis in DR Congo: Donate Now | UNICEF USA](https://www.unicefusa.org/where-unicef-works/africa/democratic-republic-congo) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [UNICEF Responds to Hunger and Violence in Haiti | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/north-america/haiti) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [UNICEF Mali](https://www.unicef.org/mali/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [UNICEF Mozambique](https://www.unicef.org/mozambique/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [UNICEF Niger](https://www.unicef.org/niger/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [How UNICEF Supports Children in Nigeria | UNICEF USA](https://www.unicefusa.org/where-unicef-works/africa/nigeria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [How UNICEF Supports Children in Sudan | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/africa/sudan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [How UNICEF Supports Children in Somalia | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/africa/somalia) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [Donate to Help South Sudan's Children | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/africa/south-sudan) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [OCHA 3W operational presence (HDX HAPI), South Sudan, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [UNICEF's Support for Syria | Middle East Crisis | UNICEF USA](https://www.unicefusa.org/where-unicef-works/middle-east/syria) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [How UNICEF Supports Children in Chad | UNICEF USA](https://www.unicefusa.org/what-unicef-does/where-unicef-works/africa/chad) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [UNICEF Venezuela](https://www.unicef.org/venezuela/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [UNICEF Yemen](https://www.unicef.org/yemen/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Note: Country evidence for MLI, MOZ, NER, VEN and YEM is on unicef.org, the international organisation's site (unicefusa.org answered scripts with a bot check for those pages). The unicefusa.org home page does not link to unicef.org; its About UNICEF USA page does (opened 24 Nov 2026).
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `usa-for-iom`: USA for IOM

- EIN: 52-1525929; website: [https://usaforiom.org/](https://usaforiom.org/); donate page: [https://usaforiom.org/donate/](https://usaforiom.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 96.4% (Form 990, tax year 2024, object 202542399349300304, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/521525929), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/521525929](https://www.charitynavigator.org/ein/521525929)
- Evaluator page to open: [https://www.give.org/search?term=USA+for+IOM](https://www.give.org/search?term=USA+for+IOM)
- Works in AFG: [International Organization for Migration | IOM, Afghanistan](https://afghanistan.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [Central African Republic | IOM Regional Office for West and Central Africa](https://wca.iom.int/central-african-republic) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [Cameroon | International Organization for Migration](https://www.iom.int/countries/cameroon) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [IOM DRC](https://drcongo.iom.int/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [IOM Ethiopia](https://ethiopia.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [IOM Haiti](https://haiti.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OIM au Mali](https://mali.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [IOM Mozambique](https://mozambique.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [IOM Niger](https://niger.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [IOM Nigeria](https://nigeria.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [International Organization for Migration | IOM, Sudan](https://sudan.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [IOM Somalia](https://somalia.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [IOM South Sudan | International Organization for Migration](https://southsudan.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [OCHA 3W operational presence (HDX HAPI), South Sudan, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [IOM Syria](https://syria.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [Chad | IOM Regional Office for West and Central Africa](https://wca.iom.int/chad) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [República Bolivariana de Venezuela | ONU Migración Americas](https://americas.iom.int/es/countries/venezuela) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [IOM Yemen](https://yemen.iom.int/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Note: Country evidence is on iom.int, the international organisation's site, which usaforiom.org links to from its home page.
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `usa-for-unfpa`: USA for UNFPA

- EIN: 13-3996346; website: [https://www.usaforunfpa.org/](https://www.usaforunfpa.org/); donate page: [https://www.usaforunfpa.org/donate/](https://www.usaforunfpa.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 82.8% (Form 990, tax year 2025, object 202632699349300303, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/133996346), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/133996346](https://www.charitynavigator.org/ein/133996346)
- Evaluator page to open: [https://www.give.org/search?term=USA+for+UNFPA](https://www.give.org/search?term=USA+for+UNFPA)
- Works in AFG: [UNFPA Afghanistan](https://afghanistan.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [Central African Republic | United Nations Population Fund](https://www.unfpa.org/data/CF) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [UNFPA Republic of Cameroon](https://cameroon.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [UNFPA Democratic Republic of Congo](https://drc.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [UNFPA Haiti](https://haiti.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [Mali | United Nations Population Fund](https://www.unfpa.org/data/ML) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [UNFPA Mozambique](https://mozambique.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [UNFPA Nigeria](https://nigeria.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [UNFPA Sudan](https://sudan.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [UNFPA Somalia](https://somalia.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [UNFPA Syria](https://syria.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [Chad | United Nations Population Fund](https://www.unfpa.org/data/TD) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [UNFPA Venezuela](https://venezuela.unfpa.org/es) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [UNFPA Yemen](https://yemen.unfpa.org/en) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Note: Country evidence is on unfpa.org, the international organisation's site. The usaforunfpa.org home page does not link to unfpa.org; its FAQ page does (opened 24 Nov 2026).
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026

### `usa-for-unhcr`: USA for UNHCR

- EIN: 52-1662800; website: [https://www.unrefugees.org/](https://www.unrefugees.org/); donate page: [https://www.unrefugees.org/donate/](https://www.unrefugees.org/donate/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 25 Nov 2026
- Programme spending: 66.3% (Form 990, tax year 2025, object 202622319349300112, worked out 25 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/521662800), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/521662800](https://www.charitynavigator.org/ein/521662800)
- Evaluator page to open: [https://www.give.org/search?term=USA+for+UNHCR](https://www.give.org/search?term=USA+for+UNHCR)
- Works in AFG: [Afghanistan Emergency: What is Happening and How to Help| USA for UNHCR](https://www.unrefugees.org/emergencies/afghanistan/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in BFA: [Burkina Faso | UNHCR](https://www.unhcr.org/where-we-work/countries/burkina-faso) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [Central African Republic Refugee Crisis | USA for UNHCR](https://www.unrefugees.org/emergencies/central-african-republic/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [Cameroon | UNHCR](https://www.unhcr.org/where-we-work/countries/cameroon) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [Democratic Republic of the Congo | UNHCR](https://www.unhcr.org/where-we-work/countries/democratic-republic-congo) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [Colombia | UNHCR](https://www.unhcr.org/where-we-work/countries/colombia) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [Ethiopia Refugee Crisis: Aid, Statistics and News | USA for UNHCR](https://www.unrefugees.org/emergencies/ethiopia/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in ETH: [OCHA 3W operational presence (HDX HAPI), Ethiopia, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [Mali | UNHCR](https://www.unhcr.org/where-we-work/countries/mali) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [Niger | UNHCR](https://www.unhcr.org/where-we-work/countries/niger) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [Nigeria | UNHCR](https://www.unhcr.org/where-we-work/countries/nigeria) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [Sudan Emergency: What is Happening and How to Help | USA for UNHCR](https://www.unrefugees.org/emergencies/sudan/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [Somalia | UNHCR](https://www.unhcr.org/where-we-work/countries/somalia) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [Syrian Refugee Crisis: What is Happening and How to Help | USA for UNHCR](https://www.unrefugees.org/emergencies/syria/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [Chad | UNHCR](https://www.unhcr.org/where-we-work/countries/chad) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in VEN: [Venezuela Crisis: What is Happening and How to Help | USA for UNHCR](https://www.unrefugees.org/emergencies/venezuela/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in VEN: [OCHA 3W operational presence (HDX HAPI), Venezuela, 2026-01-01 to 2026-07-02](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [Yemen Emergency: What is Happening and How to Help | USA for UNHCR](https://www.unrefugees.org/emergencies/yemen/) (charity's own page, checked 25 Nov 2026; expires 25 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 25 Nov 2026; expires 25 Nov 2027)
- Note: EIN from unrefugees.org's page footer (52-1662800). Affiliate statement (OR-6): its FAQ page (https://www.unrefugees.org/about-us/frequently-asked-questions/, opened 25 Nov 2026) says USA for UNHCR is UNHCR's independent U.S. national partner, raising funds and awareness to support UNHCR's work. Evidence for AFG, CAF, ETH, SDN, SYR, VEN and YEM is USA for UNHCR's own emergency pages; for BFA, CMR, COD, COL, MLI, NER, NGA, SOM and TCD it is on unhcr.org, UNHCR's own site, which counts because of that statement (the unrefugees.org pages opened, including About and FAQ, do not link to unhcr.org). Both sites answer scripts with a bot check; the pages were opened in the built-in browser. The donate link opens a form titled for monthly giving.
- Reviewed 25 Nov 2026 by Claude (draft); the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **draft**
- Owner: approve / correct: 

### `world-food-program-usa`: World Food Program USA

- EIN: 13-3843435; website: [https://wfpusa.org/](https://wfpusa.org/); donate page: [https://wfpusa.org/give/](https://wfpusa.org/give/)
- IRS Publication 78: deductibility code `PC`, from `data-download-pub78.zip (IRS file of 2026-11-11)`, checked 24 Nov 2026
- Programme spending: 88.2% (Form 990, tax year 2024, object 202611169349300006, worked out 24 Nov 2026)
- Rating confirmed: Charity Navigator: [report](https://www.charitynavigator.org/ein/133843435), read 25 Nov 2026 by Claude in the built-in browser, owner supervising
- Evaluator page to open: [https://www.charitynavigator.org/ein/133843435](https://www.charitynavigator.org/ein/133843435)
- Evaluator page to open: [https://www.give.org/search?term=World+Food+Program+USA](https://www.give.org/search?term=World+Food+Program+USA)
- Works in AFG: [Hunger in Afghanistan Conflict & Natural Disasters](https://wfpusa.org/place/afghanistan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in AFG: [OCHA 3W operational presence (HDX HAPI), Afghanistan, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [Burkina Faso: Hunger & Food Security - World Food Program USA](https://wfpusa.org/place/burkina-faso/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in BFA: [OCHA 3W operational presence (HDX HAPI), Burkina Faso, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [Central African Republic - World Food Program USA](https://wfpusa.org/place/central-african-republic/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CAF: [OCHA 3W operational presence (HDX HAPI), Central African Republic, 2026-01-01 to 2026-03-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [Cameroon - World Food Program USA](https://wfpusa.org/place/cameroon/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in CMR: [OCHA 3W operational presence (HDX HAPI), Cameroon, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [Democratic Republic of the Congo - World Food Program USA](https://wfpusa.org/place/democratic-republic-of-the-congo/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COD: [OCHA 3W operational presence (HDX HAPI), Democratic Republic of the Congo, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COL: [Colombia - World Food Program USA](https://wfpusa.org/place/colombia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in COL: [OCHA 3W operational presence (HDX HAPI), Colombia, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [Haiti - Help People in Earthquakes, Hurricanes, Hunger & Poverty](https://wfpusa.org/place/haiti/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in HTI: [OCHA 3W operational presence (HDX HAPI), Haiti, 2026-07-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [Mali - World Food Program USA](https://wfpusa.org/place/mali/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MLI: [OCHA 3W operational presence (HDX HAPI), Mali, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [Mozambique - Hunger from Conflict & Climate](https://wfpusa.org/place/mozambique/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in MOZ: [OCHA 3W operational presence (HDX HAPI), Mozambique, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [Niger - World Food Program USA](https://wfpusa.org/place/niger/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NER: [OCHA 3W operational presence (HDX HAPI), Niger, 2026-06-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [Nigeria - World Food Program USA](https://wfpusa.org/place/nigeria/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in NGA: [OCHA 3W operational presence (HDX HAPI), Nigeria, 2026-04-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [Hunger in Sudan Causes, Effects & Humanitarian Aid](https://wfpusa.org/place/sudan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SDN: [OCHA 3W operational presence (HDX HAPI), Sudan, 2026-01-01 to 2026-07-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [Somalia - World Food Program USA](https://wfpusa.org/place/somalia/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SOM: [OCHA 3W operational presence (HDX HAPI), Somalia, 2026-01-01 to 2026-06-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [South Sudan Hunger Crisis & Humanitarian Aid](https://wfpusa.org/place/south-sudan/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SSD: [OCHA 3W operational presence (HDX HAPI), South Sudan, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [Crisis in Syria: How to Help & Donate Food to Syrian Refugees](https://wfpusa.org/place/syria/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in SYR: [OCHA 3W operational presence (HDX HAPI), Syria, 2026-01-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [Chad, Africa - People in the Country Are Suffering from Mass Hunger](https://wfpusa.org/place/chad/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in TCD: [OCHA 3W operational presence (HDX HAPI), Chad, 2026-09-01 to 2026-09-30](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [Yemen Hunger Crisis from Civil War](https://wfpusa.org/place/yemen/) (charity's own page, checked 24 Nov 2026; expires 24 Nov 2027)
- Works in YEM: [OCHA 3W operational presence (HDX HAPI), Yemen, 2026-01-01 to 2026-08-31](https://data.humdata.org/dataset/hdx-hapi-operational-presence) (OCHA 3W, checked 24 Nov 2026; expires 24 Nov 2027)
- Reviewed 25 Nov 2026 by Owner (approved 2026-11-25); drafted by Claude; the review expires 25 Nov 2027
- Checks: every check holds once it is approved
- Status: **approved** on 25 Nov 2026
