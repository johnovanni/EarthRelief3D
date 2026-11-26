# HDX firewall challenge: fix (2026-11-26)

The owner's diagnosis: HDX's AWS firewall answered the app with `202 Accepted`, `x-amzn-waf-action: challenge` and an
empty body after five launches in two minutes. The four HDX feeds (UN HNO, FTS, ACAPS, IPC) reported "format changed"
and went red.

**Checked again at about 13:30 UTC:** HDX answers the app's own User-Agent normally (200, JSON), so the block was
temporary. Point 4 of the diagnosis (asking HDX to allow the User-Agent) isn't needed for now.

## What changed

GlobeCore, branch `hdx-waf` (in `external/GlobeCore`):

- **Detecting the challenge.** `FeedHttp` treats a response that carries `x-amzn-waf-action`, or a `202` to a GET, as a
  firewall challenge. It throws the new `FeedBlockedException` ("data.humdata.org is refusing requests for now (a
  firewall check); trying again after 26 Nov 13:00 UTC"), so the empty body never reaches a parser.
- **Pausing the host.** The new `HostPauses` leaves a challenged host alone for 1 h, then 2 h, 4 h and 6 h for
  challenges that follow one another. A normal answer clears the count. A challenge 24 h after the last pause starts
  the count again. While a host is paused, `FeedHttp` refuses requests to it without going to the network. The pauses
  are saved to `cache\host-pauses.json`, so a restart doesn't ask again.
- **The status dot.** `FeedScheduler` shows a challenged feed as blue "paused" with that sentence, not yellow or red.
  The data is kept, the failure count isn't raised, and the next try is when the pause ends.
- **Restarts.** `FeedScheduler` can take each feed's last success from an earlier session. A feed checked less than its
  interval ago starts green with that time and first runs when the interval is up.

EarthRelief3D, branch `hdx-waf`:

- **The app.** Live runs use `HostPauses` and the restart rule. Tests, offline samples and pictures don't, so every
  feed runs at once there, as before.
- **Which feeds wait.** A feed waits only when both caches (crises and country values) loaded. The check times come
  from the crisis cache.
- **The IPC area step.** After a challenge, it stops asking HDX for the remaining countries in that run. Those
  countries keep the 20% rule, and the log says why.

## Decisions

| # | Decision | Why | If wrong |
|---|---|---|---|
| W1 | A `202` to a GET is a challenge even without the WAF header | no feed's GET is meant to answer 202; HDX's did with an empty body | a server that answers 202 with data would be paused; none of EarthRelief3D's sources does, and EarthWatch3D's weren't checked |
| W2 | Challenges that arrive while the host is already paused don't add a strike | feeds run side by side, so one burst gives up to four challenges at once (a test caught the pause jumping to 6 h) | a long series of real refusals would grow a little more slowly |
| W3 | A saved pause more than 6 h ahead is ignored | a changed system clock or `--display-time` could otherwise keep HDX paused for days | none known |
| W4 | Point 3 is done as "don't re-run a recently checked feed at start-up", not by caching each `package_show` reply | the burst came from restarts; each feed's data and check time are already cached, so skipping the run is simpler and saves the downloads too | sources update no more often than each feed's interval (FTS daily, HNO weekly), so the data is no older than it would be if the app had stayed open |
| W5 | Paused is blue, the same as "Daily download limit reached" | it isn't a fault of the feed or its data | the owner may prefer amber; it is one line in the scheduler |

## Not verified

- I didn't run the app against HDX. HDX is answering normally now, so I couldn't watch a real challenge reach the app.
  The tests replay the exact reply the diagnosis recorded.
- I didn't check EarthWatch3D with the new GlobeCore. It keeps its own GlobeCore commit until it's re-pinned, and its
  tests haven't been run against this branch.
- Nothing is committed yet.
