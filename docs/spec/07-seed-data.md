# 07. Seed data

Contents of the read-only dictionaries (D-08). The seeder reads JSON files derived from these tables (`Code` is the stable key, and for belts and formats the pair `DisciplineId` + `Code`; `Name` and `SortOrder` may change later; codes never change).

**Review request for the author:** go through the technique and position lists as a practitioner and mark what to add, remove, or rename. Order in each table is the `SortOrder`.

## Disciplines and formats

| Discipline (`code`) | Formats (`code`) |
|---|---|
| Brazilian Jiu-Jitsu (`bjj`) | Gi (`gi`), No-Gi (`nogi`) |
| Wrestling (`wrestling`) | Freestyle (`freestyle`), Greco-Roman (`greco_roman`) |
| Judo (`judo`) | Standard (`standard`) |
| Sambo (`sambo`) | Sport Sambo (`sport`), Combat Sambo (`combat`) |

## Belts

| Discipline | Belts in order (`rank` 1..n) | Degrees (`MinDegree..MaxDegree`) |
|---|---|---|
| `bjj` | White (`white`), Blue (`blue`), Purple (`purple`), Brown (`brown`) | 0..4 stripes each |
| `bjj` | Black (`black`) | 0..6 degrees |
| `judo` | White (`white`), Yellow (`yellow`), Orange (`orange`), Green (`green`), Blue (`blue`), Brown (`brown`) (6th to 1st kyu) | 0..0 |
| `judo` | Black (`black`), dan grades | 1..10 |
| `wrestling`, `sambo` | no belts | |

The user's stripes / degrees are stored in `UserBelt.Degree` and validated against the belt's `MinDegree` / `MaxDegree`. The numbers follow the IBJJF system (coloured belts up to 4 stripes; black belt 1st to 6th degree) and the usual judo system (kyu belts, then dan grades 1 to 10). **Verify against the rules of your federation.**

## Training characters (grappling sessions)

| Character (`code`) | Allows rounds | Meaning |
|---|---|---|
| Technique (`technique`) | no | Learning and repeating techniques |
| Drilling (`drilling`) | no | Repetition drills |
| Situational Drills (`situational_drills`) | no | Tasks from a set starting position (attack, defense, takedowns) to sharpen skills; not sparring |
| Sparring (`sparring`) | yes | Free rolling |
| Mixed (`mixed`) | yes | Class with technique work and free sparring |
| Open Mat (`open_mat`) | yes | Open training without a class structure, mostly free sparring (the place goes in the club field) |
| Seminar (`seminar`) | no | Guest or special seminar |
| Yoga and Mobility (`yoga_mobility`) | no | Mobility and recovery work |

## Activity types (other sessions)

Strength (`strength`), Conditioning (`conditioning`), Running (`running`), Cycling (`cycling`), Swimming (`swimming`), Other (`other`). Yoga and mobility exist only as a grappling character, so the same activity is never counted in two sections.

## Technique categories

Choke (`choke`), Arm Lock (`arm_lock`), Shoulder Lock (`shoulder_lock`), Leg Lock (`leg_lock`), Neck and Spine Crank (`neck_crank`), Compression Lock (`compression_lock`), Other (`other`).

## Techniques (submissions)

| Category | Techniques (`code`) |
|---|---|
| `choke` | Rear Naked Choke (`rear_naked_choke`), Triangle Choke (`triangle`), Guillotine (`guillotine`), Arm Triangle (`arm_triangle`), D'Arce Choke (`darce`), Anaconda Choke (`anaconda`), Ezekiel Choke (`ezekiel`), Cross Collar Choke (`cross_collar_choke`), Bow and Arrow Choke (`bow_and_arrow`), Loop Choke (`loop_choke`), Baseball Bat Choke (`baseball_bat_choke`), Clock Choke (`clock_choke`), Paper Cutter Choke (`paper_cutter_choke`), North-South Choke (`north_south_choke`), Von Flue Choke (`von_flue_choke`), Peruvian Necktie (`peruvian_necktie`), Japanese Necktie (`japanese_necktie`), Gogoplata (`gogoplata`) |
| `arm_lock` | Armbar (`armbar`), Wristlock (`wristlock`) |
| `shoulder_lock` | Kimura (`kimura`), Americana (`americana`), Omoplata (`omoplata`), Baratoplata (`baratoplata`) |
| `leg_lock` | Straight Ankle Lock (`straight_ankle_lock`), Inside Heel Hook (`inside_heel_hook`), Outside Heel Hook (`outside_heel_hook`), Toe Hold (`toe_hold`), Kneebar (`kneebar`), Estima Lock (`estima_lock`) |
| `neck_crank` | Neck Crank (`neck_crank`), Twister (`twister`), Can Opener (`can_opener`) |
| `compression_lock` | Bicep Slicer (`bicep_slicer`), Calf Slicer (`calf_slicer`) |
| `other` | Other Submission (`other_submission`) |

36 entries. Variants of the same technique from different situations are **not** separate entries; the situation is captured by the position.

## Positions (named from the finisher's perspective, D-07)

Standing (`standing`), Front Headlock (`front_headlock`), Scramble (`scramble`), Turtle (`turtle`), Closed Guard (`closed_guard`), Half Guard (`half_guard`), Open Guard (`open_guard`), Butterfly Guard (`butterfly_guard`), Side Control (`side_control`), Knee on Belly (`knee_on_belly`), North-South (`north_south`), Mount (`mount`), Back Control (`back_control`), Leg Entanglement (`leg_entanglement`), Other (`other_position`).

## Material categories

Submissions (`submissions`), Guard (`guard`), Guard Passing (`guard_passing`), Sweeps and Reversals (`sweeps_reversals`), Takedowns (`takedowns`), Escapes (`escapes`), Back Control (`back_control`), Top Control (`top_control`), Leg Locks (`leg_locks`), Transitions and Scrambles (`transitions_scrambles`), Defense (`defense`), Other (`other`).

## Changing the dictionaries

- **Add an entry** (technique, position, category, ...): add a row with a **new unique `Code`**. Nothing else is needed.
- **Add a new category:** add the category row, then add techniques to it or move existing ones into it.
- **Move a technique to another category, or split a category into smaller ones:** change the technique's category in the seed. Past rounds point to the technique, so statistics regroup automatically.
- **Rename:** change `Name`; the code stays.
- **Never change or delete an existing `Code`.** The seeder finds rows by `Code`; a changed code would create a new row and leave old data on the old one. To retire an entry, keep it and stop using it.
- **Split one technique into two:** add the new ones. Old rounds stay on the old technique (re-mapping them would be a one-off manual data fix).
- The seeder updates every field of an existing row except `Code` (name, sort order, category, flags, degree limits).

## Other enumerations (in code, not seeded)

| Enum | Values |
|---|---|
| Round result | Win, Loss, Draw |
| Fight result | Win, Loss, Draw |
| Fight method | Submission, Points, Decision, Disqualification, Walkover |
| Goal type | TrainingCount, SparringRoundCount, CompetitionPrep, Manual |
| Material source | YouTube, Instagram, TikTok, Other |
| Material status | Saved, Watched, Practiced, Mastered |
| Session kind | Grappling, Other |
