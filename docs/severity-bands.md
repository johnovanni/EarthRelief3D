# Severity bands

Every crisis marker on the globe has a **band from 1 to 5**. The band only decides how big and how dark the marker
is, so that the worst places stand out. It is not a measurement and it is not a ranking across different kinds of
crisis: a band 3 food crisis and a band 3 conflict are not "equally bad".

**The source's own (native) scale is always shown alongside the band** (in the legend, the tooltip and the panel),
with the source name and its as-of date. If the two ever seem to disagree, the native figure is the one to trust.

## Armed conflict (UCDP)

The native measure is the **best-estimate number of deaths** UCDP records for the conflict. The band uses deaths in the data window (the newest quarterly file plus later monthly files, about 9 to 12 months, counted since the first month of the window; the panel says "since <month>"), which approximates UCDP's own crisis rule (25 or more best-estimate deaths in a calendar year).

| Deaths in the window | Band |
|---|---|
| fewer than 25 | none: not shown as a crisis |
| 25 to 999 | 1 |
| 1,000 to 9,999 | 2 |
| 10,000 to 49,999 | 3 |
| 50,000 or more | 4 |

Band 5 is not used for conflict. The figures are UCDP's candidate (not yet final) estimates and are often revised.

## Food crisis (IPC)

The native measure is the **IPC acute food insecurity phase**, from 1 (Minimal) to 5 (Catastrophe/Famine). A country is
a crisis only when Phase 3 or higher has people in it. The band **is** the highest such phase: Phase 3 is band 3,
Phase 4 is band 4, Phase 5 is band 5. Markers use the IPC's own phase colours and the phase number is always written
out.

## Outbreak (WHO Disease Outbreak News)

WHO Outbreak News reports have no graded scale, so there is no native severity to map. Every outbreak report from the
last 90 days is **band 3**, and the marker is a cross shape. The band says nothing about how dangerous the disease is.
The tooltip shows the disease, the report's title and its publication date.

## Displacement (UNHCR)

Displacement figures are layer values, not crises, and have no band.

## How a band is drawn

Conflict dots keep a fixed size on screen: radius 2.5, 3.5, 4.5 and 5.5 pixels for bands 1 to 4, in one hue whose lightness
shows the event's age (last 3 months, 3 to 6 months, older). Food shading uses the IPC phase colour (band = phase).
Outbreak crosses are one size. Displacement rings are sized by the number of people, not by a band.
