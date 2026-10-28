# 09. Traceability: concept → specification

Maps every point of the thesis concept to the place in this specification where it is covered. The "Notes" column marks interpretations and deviations (decision IDs refer to `00-overview.md`). Section numbers: `01` domain model, `02` API, `03` analytics, `04` UI, `05` conventions, `06` tasks.

## 1. Goals of the project

| Concept | Specification | Notes |
|---|---|---|
| Account with an athlete profile | 01 §4.1-4.3; 02 §2, §4; 04 §4.1, §4.9; T03, T05 | |
| Training diary as a calendar | 01 §4.4; 02 §5; 04 §4.3-4.4; T06-T07 | |
| Training character and results (sparring balance) | 01 §4.4-4.5; 04 §4.4; T08 | |
| Competition results | 01 §4.6-4.7; 02 §6; 04 §4.5; T09 | |
| Training goals | 01 §4.8; 02 §7; 03 §8; 04 §4.6; T16 | |
| Library of technical materials | 01 §4.9; 02 §8; 04 §4.7; T17 | |
| Body measurements with charts over time | 01 §4.10; 02 §9; 04 §4.8; T18 | |
| Statistical and rule-based analysis | 03; 04 §4.2; T11-T15 | |

## 2. Functional scope

| Concept | Specification | Notes |
|---|---|---|
| **1. Profile:** first name, last name | 01 §4.1 | Filled on the profile page after registration (02 §2) |
| Several clubs | 01 §4.2; 02 §4 | |
| Several belts, per discipline | 01 §4.3; 07 | Added: stripes / degrees (D-24) |
| **2. Diary:** two sections (grappling / other) | 01 §4.4 (`Kind`) | |
| Character of the training | 01 §3 (`TrainingCharacter`); 07 | List extended (Drilling, Mixed, Open Mat, Seminar); `AllowsRounds` flag (D-23); "zadaniówki" = Situational Drills |
| Discipline and format as linked dictionaries | 01 §3 (`Discipline`, `DisciplineFormat`), I-01 | |
| Club from the profile or "other" without a name | 01 §4.4 (`ClubId` null = other) | |
| Duration, feeling, notes | 01 §4.4 | Feeling is a 1-5 scale |
| Other trainings: activity type, duration, feeling, notes | 01 §4.4 (`ActivityTypeId`); 07 | |
| Further details only in notes | 01 §4.4 (`Notes`); 00 out of scope | |
| **3. Sparring register:** number of rounds, balance | 01 §4.5 (one row per round); 03 §4 `totals` | Count and balance are derived from the rows |
| Round result win / loss / draw or no result | 01 §4.5 (`Result`) | `Draw` covers "no result" |
| Finishing technique, if any | 01 §4.5 (`TechniqueId`), I-03, I-10 | From a dictionary (D-08). The concept says "submission technique"; extended to takedowns, throws, and pins so that wrestling, judo, and sambo have meaningful finishes (D-20). Offered per discipline (D-29) |
| Side of the finish (who submitted whom) | Derived from `Result` | **Deviation (D-06):** no separate field, so data cannot contradict itself |
| Optional position of the finish | 01 §4.5 (`PositionId`); D-07 | |
| Optional note | 01 §4.5 (`Notes`) | |
| Technique and position from dictionaries | 01 §3; 07 | |
| **4. Analysis:** number of trainings | 03 §3 `sessions.count` | |
| Total time and split by section | 03 §3 `totalMinutes`, `bySection` | |
| Share by discipline and format | 03 §3 `byDiscipline`, `byFormat` | |
| Distribution by clubs plus "other" | 03 §3 `byClub` (`id: null` = other) | |
| Sparring balance | 03 §4 `totals` | |
| Most used finishing techniques (with %) | 03 §4 `finishesFor.byTechnique` | |
| Techniques that beat the athlete most often | 03 §4 `finishesAgainst.byTechnique` | |
| Change of effectiveness over time | 03 §4 `trend`; §5 `winRateDelta` | |
| Dominant action types | 03 §4 `byCategory`, `byPosition`; rule `DominantFinisherCategory` | Interpretation (D-26) |
| Strengths and weaknesses from rules (percentage threshold) | 03 §6 (rule catalog, thresholds) | Concrete rules and thresholds are this project's design |
| Training and competitions analysed separately, then compared | 03 §4 vs §5; rule `CompetitionVsTrainingGap` | |
| Period, e.g. monthly | 04 §4.2 (period selector); 03 §4 (`Month` buckets) | Any date range, with presets |
| Aggregation, shares, trends, simple rules (no ML) | 03 §1-2 | |
| **5. Goals:** number of trainings, number of sparrings | 01 §4.8 (`TrainingCount`, `SparringRoundCount`); 03 §8 | Automatic progress |
| Preparation for a competition (countdown, related activity) | 01 §4.8 (`CompetitionPrep`); 03 §8 | |
| Practice a technique, improve defense, learn a submission | 01 §4.8 (`Manual`) | Updated by hand |
| Indirect support for hard goals (losses by a submission type over time) | `08-optional.md` §4 | **Not in the MVP** (D-28) |
| **6. Library:** title, URL | 01 §4.9 | |
| Source | 01 §4.9 (`Source`); 02 §8 | Detected from the URL |
| Technique category | 01 §3 (`MaterialCategory`) | Interpretation: a separate dictionary (guard, passing, ...), not the submission categories |
| Status: saved, watched, practiced, mastered | 01 §4.9 (`Status`) | |
| **7. Competitions:** name, date, location | 01 §4.6 | |
| Categories (age, skill, weight) | 01 §4.6 | Free text (D-13) |
| Discipline and format | 01 §4.6, I-01 | |
| Overall result | 01 §4.6 (`Place`, `ResultNote`) | Place as a number, 1-3 shown as medals |
| Fight: opponent, notes | 01 §4.7 | |
| Fight result win / loss | 01 §4.7 (`Result`) | `Draw` added |
| Fight finish: submission, points, decision, etc. | 01 §4.7 (`Method`, `TechniqueId`), I-04 | Fixed list of methods; `Throw` and `Pin` added (D-20) |
| Fight duration | 01 §4.7 (`DurationSeconds`) | |
| No external API for competitions | n/a | Manual entry only |
| **8. Body:** weight, height, body fat | 01 §4.10 | |
| Other parameters (to be decided) | D-25 | Decided: none |
| Charts over time | 04 §4.8 | |

## 3. Technology stack

| Concept | Specification | Notes |
|---|---|---|
| ASP.NET Core Web API | 05 §2; D-19 | .NET 10 |
| Entity Framework Core, SQL Server | 01; 05 §2, §6 | SQL Server runs in Docker |
| Mapperly | 05 §2 | Entity → DTO and create request → entity |
| FluentValidation + DataAnnotations | 02 §1 | |
| React with a chart library | 04 §1; D-15 | Recharts. Added: TypeScript, Vite, React Router |
| ASP.NET Core Identity; session mechanism to be decided | 02 §2; D-03 | Decided: Identity API endpoints with cookies |
| Swagger / OpenAPI | 02 §1; 05 §2 | |
| xUnit / FluentAssertions / Moq | 05 §4; D-14 | Added: Testcontainers (integration tests) |
| Docker / Docker Compose | 05 §6; D-16 | Two services: `db` and `app` |
| Git / GitHub | 05 §5 | CI is optional (`08-optional.md`) |
| Layers: API, business logic, data access | 05 §2; D-04 | `Api`, `Application`, `Data` |

## 4. Engineering value

| Concept | Specification |
|---|---|
| Consistent data model for a complex domain | 01 (ERD, constraints, invariants I-01 to I-11) |
| Multi-layer web system with authentication and authorization | 05 §2; 02 §2, §11; 01 §1 (isolation) |
| Diary, sparring and fight registers | 01 §4.4-4.7; T06-T09 |
| Rule-based analysis module | 03; T11-T13 |
| Automatic goal progress | 03 §8; T16 |
| Dashboards and charts | 04 §4.2; T14-T15 |
| Automated tests of the business logic | 05 §4; 03 §7 (golden tests GT-01 to GT-06); T03, T20 |

## 5. Concept's "further development" (not implemented)

Machine learning, event diary, PDF reports, technical tags on sessions. None is in the MVP; they are listed under "Out of scope" in `00-overview.md`.

## 6. Specification elements that are not in the concept

| Element | Where | Why |
|---|---|---|
| Stripes / degrees on belts | 01 §4.3; 07 | Author's request |
| `Draw` result for fights | 01 §4.7 | Real events can end in a draw |
| `AllowsRounds` rule per character | 01 §3; D-23 | Drills are not sparring |
| Fixed fight methods and medal shortcuts | 01 §4.6-4.7 | Simplest consistent model |
| Finishing actions beyond submissions (takedowns, throws, pins) and technique categories linked to disciplines | 01 §3, I-10; 07; D-20, D-29 | Wrestling and judo rounds rarely end by a submission; each athlete sees only relevant techniques |
| Positions linked to disciplines, specific leg entanglement positions, judo and sambo hold-downs as techniques | 01 §3, I-11; 07; D-33 | Each athlete sees only relevant positions; precise statistics without a vague catch-all entry |
| Account deletion | 02 §2 | Privacy; small profile option |
| Demo data seeder | 05 §2; T10 | Defense demo and analytics checks |
| `/health` endpoint | 02 §1 | Quick check that the app (and from T02 the database) is up |
| English UI with texts in one file | D-01 | Simplest option that keeps Polish possible |
| Vite, React Router, TypeScript | D-15 | Minimum tooling for a React app |
| Testcontainers | D-14; T02, T03 | Realistic integration tests |
| Optional extras (CI, dark scheme, Polish UI, ...) | 08 | Only after all MUST tasks |
