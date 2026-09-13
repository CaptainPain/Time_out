# TIME OUT

A cinematic story-driven motorcycle game about a bored 21-year-old, his father's
pendant, and three timelines sharing one small American town.

**Genre:** drama-comedy. Dramatic or funny cutscenes full of story, each leading
into action. The rhythm of the game is story beat → action sequence → story beat.

> **Core thesis:** *Time travel isn't a puzzle mechanic that interrupts the action.
> Time travel IS the action.*

**Direction lock:** there are no boosts and no gates in this game. Escape is pure
riding; upgrades unlock routes, never speed.

---

## The game in 30 seconds

Jack is 21, stuck in 1999, working at **Lackluster Video** and eating every day at
his mother's restaurant. He hates his life and wants something to happen. Then his
late father's pendant hurls him through time — and his boring little town turns out
to sit at the center of a temporal conspiracy spanning a thousand years in both
directions. One geography, three simultaneous versions: **1999 / 1000 years ago /
1000 years hence.** The player builds a mental map of all three and asks, of every
place: *what is / was / will be HERE?*

Jack is obsessed with *Back to the Future*. Real time travel happening to him is
hilariously ironic. His first shift:

> "Okay. The movie made this look way easier."

---

## Story

- **Jack** — 21, restless, works at Lackluster Video. Opening motivation: *"I hate
  my life and I want something to happen."* What he really hates is having no choice.
- **Mom** — runs the town restaurant, the fixed point across all three timelines.
  She has a secret connection to the pendant.
- **The pendant** — Jack's father's. Dark, distorted guitar motif. Linked to a
  figure known as the **Curator**.
- **Emotional foundation:** *"Having the ability to change your past doesn't mean
  you have to."* Jack starts trying to escape his life and ends by choosing it.

**Escalation arc:** small town → time travel → survival → exploration → mystery →
family history → temporal conspiracy → collapsing timelines → final race.

The full screenplay lives in [STORY.md](STORY.md).

---

## The three timelines

| Timeline | Era | Palette | Guitar voice | Role |
|---|---|---|---|---|
| **Present** | 1999 | Warm, nostalgic | Soft clean (Jack/mom), fast blues-rock (bike) | Modern components, motorcycle upgrades |
| **Past** | ~1000 years ago (medieval) | Earthy, painterly | Acoustic/electric hybrid | Materials, fuel, repairs, survival |
| **Future** | ~1000 years hence (~2999) | Cold, processed | Processed, alien | Advanced technology, major upgrades |

**Locked rule:** all three timelines share the same physical coordinates. Time travel
can trigger mid-gameplay — even mid-air.

**Timeline wind** (prototype): medieval −4.5 headwind, future +2.5 tailwind, present calm.

---

## Locked design rules

- **Drama-comedy first.** Dramatic or funny cutscenes full of story, each leading
  into action. Story beat → action sequence → story beat.
- **No boosts, no gates.** Shift rings restore pendant charges only. Upgrades
  improve the bike and unlock routes; they never add speed.
- **Every scene is interactive.** Dexterity — movement, jump, balance, ride,
  timing — is the core. Difficulty comes from *combining* mechanics, not bigger
  numbers.
- **Time travel can happen during gameplay, including mid-air.** Mid-air shifts
  preserve position and velocity.
- **One world, three versions.** Same geography in 1999 / medieval / future.
- **Timeline roles:** present = modern components & bike upgrades; medieval =
  materials, fuel, repairs, survival; past/future feed the bike's evolution.
- **The motorcycle evolves all game.** The bicycle covers precision and tight spaces.
- **The freeway jump is the signature move AND the final test.** Launch from the
  terrain hill *beside* the on-ramp, clear the whole freeway — Jack never rides down
  the ramp. The finale re-tests the same maneuver learned dozens of hours earlier,
  while the world changes underneath the player. Bookend design.
- **Learning ladder:** environment → motorcycle → timelines → combine all three.

---

## Gameplay beats (planned)

- Free-ride town with no tutorial UI — learn by riding.
- Rooftop routes above the town.
- The freeway jump stunt (signature early move).
- Police chase (escape).
- Slower on-foot video-store section.
- Late-game chained shifts: embankment → launch → rotate → pendant mid-air →
  medieval ravine → dodge tower → pendant → future megastructure → moving-platform
  landing.

---

## Platform & visual language

- Unity side-scroller. Landscape on smartphones; also PC and Mac.
- Stylized 2.5D in the spirit of **Prince of Persia: The Lost Crown**: 3D environments
  and characters, side-scrolling movement, anime/comic energy, painterly parallax
  depth, momentum-heavy animation, dramatic swooping camera on big moments, 2D graphic
  effects layered over 3D. Match the *visual language*, not a AAA asset count.
- Timeline palettes echo the guitar motifs (warm / earthy / cold).

## Guitar language

- Jack / mother: soft clean.
- Escape / leaving: lonely sustained line.
- Motorcycle: fast blues-rock.
- Pendant / Curator: dark distorted.
- Medieval: acoustic/electric hybrid.
- Future: processed and alien.
- **Time travel = all three combined: "the sound of time itself."**
- Guitar enters at important emotional moments — not constant background music.

---

## Prototype — how to run

1. Open the Unity project (`C:\Users\brian\My project`), **stop Play mode**.
2. Extract the latest `time-out-vN.zip` into the project root, overwriting files
   (never delete first — deleting breaks `.meta` references).
3. Let Unity compile. Attach `GameBootstrap` to an empty GameObject if the scene
   is fresh.
4. Press Play. Tap through the intro cards.

### Controls

| Key | Action |
|---|---|
| W / Up | Throttle |
| S / Down | Brake |
| A / Left | Lean back |
| D / Right | Lean forward |
| Space | Time shift |
| R | Restart run |

### Prototype systems (current)

- **Police chase** — a cruiser pursues Jack before the freeway gap; contact = BUSTED.
  It can't follow across the gap. Procedural siren + flashing lights.
- **Pendant charges** — shifting costs one charge; start with 3, max 5; empty pendant
  plays a denied buzz.
- **Shift rings** — one per timeline; riding through one restores one pendant charge
  (boost removed in v8 — there are no boosts in this game). The future ring hangs
  inside a neon arch over the gap.
- **Timeline wind** — medieval headwind / future tailwind / present calm changes the jump.
- **Medieval watchtower** at x=100 — weak jumps clip it; shifting timelines avoids it.
- **Coins** — an arc over the gap plus a landing line.
- **Mom's Diner** — the fixed point; holographic signs and neon arch in the future;
  stakes, torches, dead trees in the medieval past.
- **Intro cards** — tap-through; Jack's first-shift BTTF quip included.
- **Win stats** — run time, shift count, coins, top speed. Crashes and busts restart
  the run.

---

## Release history

- **v1** — Core prototype: terrain, bike physics, gap jump, three timelines, HUD.
- **v2** — Fixed `CS0029` in `Hud.cs` (`AddBar` return type).
- **v3/v4** — Unity 6 new Input System: `KeyPoll` keyboard polling, `KeyControl`
  import, UI input module (fixed 999+ `InvalidOperationException`s).
- **v5** — OS Arial fallback for HUD text; NaN terrain guards at the gap edge.
  First confirmed working build ("ok, its working").
- **v6 "Chase & Timelines"** — police chase + BUSTED, pendant charges, shift rings,
  coins, watchtower hazard, timeline wind, Mom's Diner, intro cards, win stats.
- **v7** — Fixed `MissingComponentException`: palisade stakes now get a `MeshRenderer`.

---

## Release plan

Planned releases are tracked as GitHub milestones; every task is an issue, and every
shipped task carries its release notes so the full history is always look-up-able.

| Release | Theme | Headline tasks |
|---|---|---|
| **v8 — Ride Right** | The bike | Offroad motorcycle visual (replaces quad), shock physics, rider animation, engine audio, boost removal |
| **v9 — Story First** | Drama-comedy | Cutscene system, funny Lackluster Video scenes, dramatic diner scenes, NPC comedy bits, present-day roads |
| **v10 — Escape!** | The chase | Rooftop routes, multi-cruiser chase as story payoff, on-foot store section |
| **v11 — The Past Demands** | Survival | Fuel & repair mechanics, material pickups, tower variants, medieval terrain + era obstacles |
| **v12 — Future Tech** | Upgrades | Bike upgrade tiers (no boosts), megastructure segment, moving platforms, holo-signs, future surfaces |
| **v13 — The Final Race** | The finale | Collapsing-timelines prototype, chained mid-air shifts, final jump bookend |

See **Milestones** and **Issues** on GitHub for the detailed task lists, and
**Releases** for per-version notes.

---

## Project layout

```text
Assets/
  Scripts/
    *.cs            # all prototype code, built from Unity primitives (no assets)
README.md           # this file — the game bible
STORY.md            # full screenplay
```

All prototype geometry and audio are procedural — zero external assets.
