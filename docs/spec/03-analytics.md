# 03. Analytics

This is the core of the thesis. Every number here must be reproducible by a unit test.

## 1. Design

Analytics run in two steps:

1. **Load**: one query per endpoint projects the minimum needed rows into small immutable records (for example `RoundRow(Date, Result, TechniqueId, CategoryId, PositionId, DisciplineId, FormatId)`).
2. **Calculate**: pure classes (no database, no clock) turn those records into the response. Unit tests exercise only this step, with in-memory lists.

Insight rules are separate classes implementing `IInsightRule` (one rule = one class), registered in DI. Adding a rule never edits existing code (open/closed). This is the one place where an interface with many implementations is justified.

## 2. Common definitions

| Term | Definition |
|---|---|
| Period | Inclusive range `[from, to]`; `days = (to - from) + 1`. |
| Previous period | `[from - days, from - 1]`: same length, directly before. |
| Share | `count / denominator`, a number in `[0, 1]`, **rounded to 4 decimals** (`MidpointRounding.AwayFromZero`). The denominator is stated per metric. |
| Ordering of lists | `count` descending, then `name` ascending. |
| Week | ISO week, starting Monday. |
| `winRate` | `wins / rounds` (draws stay in the denominator, D-09). `null` when `rounds = 0`. |
| `balance` | `wins - losses`. |
| "Other" entries | `other_finish` (technique), `other` (category), and `other_position` (position) are **counted** in distributions but **ignored by insight rules**. |

Grappling-only metrics use sessions with `Kind = Grappling`. Rounds of sessions outside the period are never counted. Rounds can only exist in sessions whose character allows rounds (D-23, I-09), so every stored round counts.

## 3. Overview (`GET /api/analytics/overview`)

```json
{
  "period": { "from": "2026-09-01", "to": "2026-09-28", "days": 28 },
  "sessions": { "count": 10, "totalMinutes": 840, "avgFeeling": 3.9 },
  "bySection": [ { "kind": "Grappling", "count": 8, "minutes": 720 },
                 { "kind": "Other", "count": 2, "minutes": 120 } ],
  "byDiscipline": [ { "id": 1, "name": "Brazilian Jiu-Jitsu", "count": 8, "minutes": 720, "share": 1.0 } ],
  "byFormat":     [ { "id": 1, "name": "Gi", "disciplineId": 1, "count": 6, "minutes": 540, "share": 0.75 } ],
  "byClub":       [ { "id": 3, "name": "Club A", "count": 5, "minutes": 450, "share": 0.625 },
                    { "id": null, "name": null, "count": 1, "minutes": 90, "share": 0.125 } ],
  "byCharacter":  [ { "id": 4, "name": "Sparring", "count": 3, "minutes": 270, "share": 0.375 } ],
  "weekly": [ { "weekStart": "2026-08-31", "count": 3, "minutes": 270 } ],
  "avgSessionsPerWeek": 2.5,
  "activeWeeksShare": 1.0
}
```

| Metric | Definition |
|---|---|
| `avgFeeling` | Mean of `Feeling` over all sessions, 1 decimal. `null` if no sessions. |
| `byDiscipline`, `byFormat`, `byClub`, `byCharacter` | Over **grappling sessions only**. `share = count / grapplingSessionCount`. Club `id: null` = "Other club" (UI translates `null`). |
| `weekly` | One entry for **every** ISO week that overlaps the period (including weeks with `count = 0`). Only sessions inside the period are counted; `weekStart` may be before `from`. |
| `avgSessionsPerWeek` | `sessionsCount * 7 / days`, 1 decimal. |
| `activeWeeksShare` | Weeks with `count ≥ 1` divided by the number of weeks in `weekly`. |

## 4. Sparring (`GET /api/analytics/sparring`)

Optional filters `disciplineId`, `formatId` restrict to matching grappling sessions.

```json
{
  "totals": { "rounds": 20, "wins": 12, "losses": 6, "draws": 2, "winRate": 0.6, "balance": 6 },
  "finishesFor": {
    "total": 8,
    "byTechnique": [ { "id": 1, "name": "Rear Naked Choke", "count": 4, "share": 0.5 } ],
    "byCategory":  [ { "id": 1, "name": "Choke", "count": 6, "share": 0.75 } ],
    "byPosition":  { "recorded": 5, "items": [ { "id": 9, "name": "Back Control", "count": 3, "share": 0.6 } ] }
  },
  "finishesAgainst": { "total": 4, "byTechnique": [], "byCategory": [], "byPosition": { "recorded": 3, "items": [] } },
  "trend": {
    "bucket": "Week",
    "points": [ { "start": "2026-08-31", "rounds": 6, "wins": 3, "losses": 2, "draws": 1, "winRate": 0.5 } ],
    "previous": { "rounds": 15, "winRate": 0.4667 },
    "winRateDelta": 0.1333
  }
}
```

| Metric | Definition |
|---|---|
| `finishesFor.total` | Rounds with `Result = Win` **and** a technique (any finishing action: submission, takedown, throw, or pin). Without a discipline filter, an athlete who trains several disciplines sees them mixed. |
| `finishesAgainst.total` | Rounds with `Result = Loss` **and** a technique. |
| `byTechnique`, `byCategory` | `share = count / total` of the same block. |
| `byPosition.recorded` | Finishes in that block that **have a position**. `share = count / recorded`. Finishes without a position are excluded from this distribution. |
| `trend.bucket` | `Week` if `days ≤ 92`, else `Month` (calendar months). |
| `trend.points` | One point per bucket overlapping the period (zero-round buckets included, `winRate = null`). Only rounds inside the period are counted. |
| `trend.previous` | `rounds` and `winRate` for the previous period (same filters). |
| `trend.winRateDelta` | `winRate(current) - winRate(previous)` computed from the unrounded rates, then rounded to 4 decimals. `null` if either period has `rounds < MinSamples` (section 6). |

## 5. Competitions (`GET /api/analytics/competitions`)

```json
{
  "totals": { "competitions": 3, "fights": 9, "wins": 5, "losses": 4, "draws": 0, "winRate": 0.5556,
              "gold": 1, "silver": 1, "bronze": 0 },
  "methods": {
    "wins":   [ { "method": "Submission", "count": 3, "share": 0.6 } ],
    "losses": [ { "method": "Points", "count": 3, "share": 0.75 } ]
  },
  "finishesFor":     { "total": 3, "byTechnique": [], "byCategory": [] },
  "finishesAgainst": { "total": 1, "byTechnique": [], "byCategory": [] },
  "timeline": [ { "competitionId": 7, "date": "2026-09-12", "name": "Warsaw Open",
                  "place": 2, "fights": 3, "wins": 2, "losses": 1 } ],
  "previous": { "fights": 6, "winRate": 0.3333 },
  "winRateDelta": 0.2222
}
```

- A competition belongs to the period by its `Date`. The optional `disciplineId` / `formatId` filters apply to the competition's own discipline and format.
- `winRate = wins / fights` (draws stay in the denominator, as in sparring).
- `methods.wins/losses`: distribution of `Method` among won/lost fights; `share = count / wins` (or `/ losses`).
- `finishesFor.total` = won fights with a technique (`Method` is `Submission`, `Throw`, or `Pin`); `finishesAgainst.total` analogously for lost fights. Technique and category shares use these totals.
- Medal counts are derived from `Place` (1 = gold, 2 = silver, 3 = bronze); other places and `null` count as no medal.
- `winRateDelta` rule as in section 4, with `MinSamples` of the competition profile.

## 6. Insights (`GET /api/analytics/insights`)

```json
{
  "training":    { "status": "Ok", "items": [ { "rule": "DominantFinisherTechnique", "type": "Strength",
                    "params": { "id": 1, "name": "Rear Naked Choke", "count": 4, "share": 0.5 } } ] },
  "competition": { "status": "InsufficientData", "items": [] },
  "comparison":  [ { "rule": "CompetitionVsTrainingGap", "type": "Info",
                     "params": { "trainingWinRate": 0.6, "competitionWinRate": 0.3, "delta": -0.3 } } ]
}
```

- `status` is `InsufficientData` (and `items` empty) when the sample is smaller than `MinSamples`; otherwise `Ok` (possibly with no items).
- Each insight: `rule` (code), `type` (`Strength`, `Weakness`, `Trend`, `Info`), `params` (object). **No text**: the UI builds the sentences from `rule` + `params` (in `texts.ts`).
- Insights are computed **separately** for training (the sparring data) and competitions (the fights data). The optional `disciplineId` / `formatId` filters apply to both sides, so the comparison always uses matching data.

### Thresholds

Stored in configuration (`appsettings.json`, section `Analytics:Rules`), bound to one typed options class. Never hard-coded in rule classes.

| Setting | Training | Competition | Meaning |
|---|---|---|---|
| `MinSamples` | 10 | 5 | Rounds / fights needed in the period for any insight |
| `MinFinishes` | 5 | 3 | Finishes in the block (for or against) needed for technique/category rules; **recorded positions** needed for position rules |
| `MinCount` | 3 | 2 | Occurrences of the single item |
| `TechniqueShare` | 0.30 | 0.30 | Min share for technique and category rules |
| `PositionShare` | 0.40 | n/a | Min share for position rules (training only) |
| `TrendDelta` | 0.10 | 0.10 | Min absolute win-rate change |
| `GapDelta` | 0.20 | | Min training-vs-competition win-rate gap (comparison) |

### Rule catalog

| Rule code | Type | Fires when | Output | Limit |
|---|---|---|---|---|
| `DominantFinisherTechnique` | Strength | In `finishesFor`: `total ≥ MinFinishes`, technique `count ≥ MinCount` and `share ≥ TechniqueShare` | `id, name, count, share` | top 3 |
| `DominantFinisherCategory` | Strength | Same, on categories | same | top 1 |
| `RecurringLossTechnique` | Weakness | Same, on `finishesAgainst` techniques | same | top 3 |
| `RecurringLossCategory` | Weakness | Same, on `finishesAgainst` categories | same | top 1 |
| `StrongPosition` | Strength | `finishesFor.byPosition.recorded ≥ MinFinishes`, position `count ≥ MinCount` and `share ≥ PositionShare` (training only) | `id, name, count, share` | top 1 |
| `VulnerablePosition` | Weakness | Same, on `finishesAgainst` positions (training only) | same | top 1 |
| `ImprovingWinRate` | Trend | `winRateDelta ≥ TrendDelta` | `winRate, previousWinRate, delta` | 1 |
| `DecliningWinRate` | Trend | `winRateDelta ≤ -TrendDelta` | same | 1 |
| `CompetitionVsTrainingGap` | Info | Both sides `status = Ok` and `abs(compWinRate - trainWinRate) ≥ GapDelta` | `trainingWinRate, competitionWinRate, delta` (`comp - train`) | 1 (in `comparison`) |

Selection among candidates: highest `share`, then highest `count`, then name ascending. "Other" entries are skipped (section 2). The concept's "dominant action types" are covered by `DominantFinisherCategory` and by the `byCategory` and `byPosition` distributions.

## 7. Reference numbers

Use these as test fixtures (golden tests). Class names are suggestions.

### GT-01: Sparring distribution and insights (`SparringCalculatorTests`)

Period 2026-09-01..2026-09-30, filters none, training thresholds. 20 rounds:

| Result | Technique (category) | Position | Count |
|---|---|---|---|
| Win | Rear Naked Choke (choke) | back_control | 3 |
| Win | Rear Naked Choke (choke) | none | 1 |
| Win | Triangle (choke) | closed_guard | 2 |
| Win | Kimura (shoulder_lock) | none | 2 |
| Win | none | none | 4 |
| Loss | Inside Heel Hook (leg_lock) | leg_entanglement | 2 |
| Loss | Guillotine (choke) | none | 1 |
| Loss | Rear Naked Choke (choke) | back_control | 1 |
| Loss | none | none | 2 |
| Draw | none | none | 2 |

Expected:

- `totals`: rounds 20, wins 12, losses 6, draws 2, `winRate` 0.6, `balance` 6.
- `finishesFor.total` 8. `byTechnique`: RNC 4 (0.5), Triangle 2 (0.25), Kimura 2 (0.25). `byCategory`: choke 6 (0.75), shoulder_lock 2 (0.25). `byPosition`: recorded 5; back_control 3 (0.6), closed_guard 2 (0.4).
- `finishesAgainst.total` 4. `byTechnique`: Inside Heel Hook 2 (0.5), Guillotine 1 (0.25), RNC 1 (0.25). `byCategory`: leg_lock 2 (0.5), choke 2 (0.5). `byPosition`: recorded 3; leg_entanglement 2 (0.6667), back_control 1 (0.3333).
- Insights (training): `DominantFinisherTechnique` = RNC only (Triangle 0.25 < 0.30); `DominantFinisherCategory` = choke (0.75); `StrongPosition` = back_control (closed_guard has share 0.4 but count 2 < 3); **no** `RecurringLoss*` (finishesAgainst.total 4 < 5); **no** `VulnerablePosition` (recorded 3 < 5).

### GT-02: Insufficient data

9 rounds in the period ⇒ `training.status = InsufficientData`, `items = []`.

### GT-03: Trend

Current period: 20 rounds, 12 wins (`winRate` 0.6). Previous period: 15 rounds, 7 wins (0.4667). `winRateDelta = 0.1333` ⇒ `ImprovingWinRate` with `params.delta = 0.1333`. If the previous period had 8 rounds: `winRateDelta = null`, no trend rule.

### GT-04: Overview

Period 2026-09-01..2026-09-28 (28 days). 10 sessions: 8 grappling BJJ of 90 min (6 Gi, 2 No-Gi; club A ×5, club B ×2, no club ×1) and 2 `Other` of 60 min.
Expected: `sessions.count` 10, `totalMinutes` 840, `bySection` Grappling 8/720 and Other 2/120, `byFormat` Gi 6 (0.75) and No-Gi 2 (0.25), `byClub` A 5 (0.625), B 2 (0.25), null 1 (0.125), `avgSessionsPerWeek` 2.5.

### GT-05: Goal progress

See section 8; cases are listed there.

### GT-06: Competitions

Fights: 9 over 3 competitions; 5 wins (3 by Submission with technique, 1 Points, 1 Decision), 4 losses (1 by Submission with technique, 3 Points). Expected: `winRate` 0.5556; `methods.wins`: Submission 3 (0.6), Points 1 (0.2), Decision 1 (0.2); `methods.losses`: Points 3 (0.75), Submission 1 (0.25); `finishesFor.total` 3; `finishesAgainst.total` 1.

## 8. Goals: computed fields

`today` is a parameter of the goal calculator: the service passes the current date, tests pass a fixed date.

| Type | `currentValue` | `progress` | `daysRemaining` |
|---|---|---|---|
| `TrainingCount` | sessions with `Date` in `[StartDate, EndDate]` | `min(1, currentValue / TargetValue)` | `max(0, EndDate - today)` |
| `SparringRoundCount` | rounds of sessions in `[StartDate, EndDate]` | `min(1, currentValue / TargetValue)` | as above |
| `CompetitionPrep` | sessions in `[StartDate, competition.Date]` | `min(1, currentValue / TargetValue)` if `TargetValue` set, else `null` | `max(0, competition.Date - today)` |
| `Manual` | `null` | `ManualProgressPercent / 100` | `max(0, EndDate - today)` if `EndDate` set, else `null` |

`state`: `Achieved` if `progress ≥ 1` (for `CompetitionPrep` without target: when `today > competition.Date`); otherwise `Expired` if the end date exists and `today > end date`; otherwise `InProgress`.

Golden cases (GT-05): `TrainingCount`, target 20, 2026-10-01..2026-10-31, 12 sessions:
- today 2026-10-14 → `currentValue` 12, `progress` 0.6, `InProgress`, `daysRemaining` 17;
- today 2026-11-01 → `Expired`, `daysRemaining` 0;
- 20 sessions, today 2026-10-20 → `progress` 1, `Achieved`.

## 9. Performance and correctness notes

- Analytics must answer in under 500 ms for 5 years of data of one user (about 1500 sessions) on a laptop. Load only needed columns (`Select` projection); no entity tracking.
- All percentages in the API are fractions; only the UI formats them.
- Empty data never throws: empty lists, zero totals, `null` rates.
