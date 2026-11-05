# 00. Overview

## 1. Purpose

A web application in which a grappling athlete (BJJ, wrestling, judo, sambo) records training, sparring rounds, and competitions, and receives **analysis of progress**: where they win, where they lose, which techniques they finish with, which they get caught by, and how this changes over time.

The core of the thesis is the **analytics module** (`03-analytics.md`). Everything else exists to feed it with clean, consistent data.

## 2. Users

One role: **athlete**. Each athlete sees only their own data. There is no admin role and no admin panel (see D-02).

## 3. Scope and priorities

| Priority | Feature |
|---|---|
| MUST | Accounts (register, login, logout), profile (name, clubs, belts) |
| MUST | Dictionaries (read-only) served to the client |
| MUST | Training sessions: grappling and other; calendar; sparring rounds |
| MUST | Competitions with fights |
| MUST | Analytics: overview, sparring, competitions, insights |
| MUST | Docker Compose run (db + app), responsive UI |
| MUST | Goals with automatic progress |
| MUST | Materials library |
| MUST | Body measurements |
| SHOULD | Demo data seeder (for the thesis defense) |
| SHOULD | Delete own account (a small option in the profile) |
| SHOULD | Thesis material and a short root README: ERD and architecture diagrams, endpoint list, run instructions (T21) |
| COULD | Everything in `08-optional.md` (CI, dark scheme, Polish UI, ...) |

**Cut order when behind schedule (first to cut first):** everything in `08-optional.md` → thesis material (T21) → delete account → demo data. All MUST features come from the thesis concept and should stay.

### Out of scope

Admin panel, e-mail sending (confirmation, password reset), social features, coach accounts, payments, native mobile apps, offline mode, import/export, tracking of individual techniques drilled in class, scoring details (points/advantages) of rounds, and the concept's further-development items (machine learning, event diary, PDF reports, technical tags on sessions).

## 4. Decision log

Every decision below was made to keep implementation simple and fast. Change any of them by editing this table first.

| ID | Decision | Reason |
|---|---|---|
| D-01 | Spec, code, commits, API: English. UI: English; all UI texts live in one file, `frontend/src/texts.ts` (a plain object, no i18n library). A Polish version later means a second object and a language switch. | Technique names are English; simplest option that keeps Polish possible. |
| D-02 | Single role, no admin. Dictionaries are changed via seed files in Git. User deletes own account in the app. | Each user's data is private; no one manages other people's data. |
| D-03 | Auth: ASP.NET Core Identity API endpoints with **cookies**; custom `POST /logout`; no e-mail confirmation. | Least code; no token handling in JavaScript. |
| D-04 | Layers: `Api → Application → Data`, matching the three layers named in the concept (API, business logic, data access). Entities live in `Data`. `AppDbContext` is used directly by services (no repository layer). Every service has an interface (`IXxxService`) and controllers depend on it. | EF Core's DbContext already is a repository + unit of work. |
| D-05 | One `TrainingSession` table with a `Kind` (Grappling / Other); rules enforced by validator + DB check constraint. | Simplest schema, simplest analytics queries. |
| D-06 | One sparring round = one row. Result: Win / Loss / Draw. Who finished is **derived from the result** (no separate field). | Avoids contradictory data. |
| D-07 | Position is named **from the perspective of the person who applied the finish**. Win by triangle from closed guard → `closed_guard`. Loss to a rear-naked choke → `back_control` (opponent had your back). | Unambiguous analytics. |
| D-08 | Dictionaries are fixed and seeded from JSON files; users cannot add entries. Technique and position dictionaries contain an `other_*` entry. | Consistent statistics. |
| D-09 | `winRate = wins / rounds` (draws stay in the denominator). | One simple, explainable definition. |
| D-10 | Analytics are computed on the server. Insights are returned as **structured data** (rule code + parameters); the UI builds the sentences in `texts.ts`. | Language-free backend, testable rules. |
| D-11 | Goal type `TrainingCount` counts **all** sessions (grappling + other) in the period. | Simplest; can be refined later. |
| D-12 | Dates are `DateOnly`; durations in minutes; metric units only. "Today" is `DateOnly.FromDateTime(DateTime.Now)` taken in the services, with the container time zone set to `Europe/Warsaw` (`TZ`); calculators receive `today` as a parameter. | No time-zone bugs; calculators are testable with a fixed date and need no clock abstraction. |
| D-13 | Competition age / skill / weight categories are free text (max 50 chars). | Not used in analytics. |
| D-14 | Tests: xUnit + FluentAssertions (version 8+ is free for non-commercial use, which covers this thesis) + Moq where a fake is needed. Unit tests (analytics, rules, validators) and integration tests are written with each feature. Integration tests run against a real SQL Server in a container (Testcontainers), container set up in T02, `WebApplicationFactory` in T03; fallback: SQLite in memory. EF Core InMemory is not used for relational behavior. Prefer real objects. | Concept stack; realistic tests from the start, isolation checked early. |
| D-15 | Frontend: React with TypeScript, built with Vite, routed with React Router, charts with **Recharts** (named in the concept). Own CSS (no UI library), own calendar component, plain `fetch` helper, DTO types written by hand, no state or data-fetching library. Light theme first (black top bar, chosen from mockups); a dark scheme is a COULD. | Only technologies from the concept plus the minimum needed to build and route a React app. |
| D-16 | One origin: the ASP.NET Core app serves the built frontend (static files with a fallback to `index.html`) and the API. Docker Compose has two services, `db` and `app`; the frontend is built inside the app's Dockerfile (Node is used only at build time). In development the Vite server proxies `/api` to the API. | Required for cookie auth without CORS; no extra nginx container. |
| D-17 | Dictionaries: JSON seed files + idempotent seeder (upsert by `Code`, or by `(DisciplineId, Code)` for belts and formats, see `01-domain-model.md` §3; never delete dictionary rows, except the category-discipline and position-discipline link rows). Demo data: optional seeder, deterministic. | Easy to review and extend; stable IDs. |
| D-18 | No pagination in MVP (data volume per user is small). Sessions are queried by date range. | KISS. |
| D-19 | .NET 10 (LTS), EF Core, SQL Server 2022 in Docker (not 2025). | Current .NET LTS. SQL Server 2022 is the more mature release with years of fixes; the 2025 additions (AI and vector features) are not used here. The same image is used in Docker Compose and in the integration tests. |
| D-20 | The technique dictionary lists **finishing actions**: submissions, takedowns, throws, and pins/hold-downs. It is one global table. A round or fight may name the one action that decided it (always optional). Sweeps, guard passes, and point-scoring details stay out. | The concept lists wrestling, judo, and sambo, but their rounds mostly end by a throw, takedown, or pin, not a submission. With submissions only, their finish statistics would be empty. |
| D-21 | Account deletion removes all user data in one transaction. | Privacy (GDPR right to erasure). |
| D-22 | Hosting: local Docker Compose only (defense demo). | Simplest. |
| D-23 | Sparring rounds can be recorded only in sessions whose character has `AllowsRounds` (Sparring, Open Mat, Mixed). Technique, Drilling, Situational Drills, Seminar, and Yoga sessions have no rounds. Every recorded round counts in the statistics. | Drills from a set position are practice, not sparring, and would skew win rate. The rule lives in the dictionary, not in code. |
| D-24 | Belt degree limits live in the belt dictionary (`MinDegree` / `MaxDegree`): BJJ white to brown 0..4, BJJ black 0..6, judo kyu belts 0, judo black belt 1..10 (dan). To be verified against federation rules. | Correct per discipline; extensible without code. |
| D-25 | Body measurements hold only weight, height, and body fat (the concept's "other parameters" were decided as none). | Simplest; enough for the progress charts. |
| D-26 | The concept's "dominant action types" are the technique categories (and positions): `DominantFinisherCategory`, `byCategory`, `byPosition`. | Gives the concept item a concrete definition. |
| D-27 | Yoga and mobility exist only as a grappling character, not as an "other" activity type. | One activity never lands in two statistics sections. |
| D-28 | `Goal` has no technique link now. If the optional goal-trend extra (`08-optional.md`) is built, a nullable `TechniqueId` is added by a migration. | No dead fields. |
| D-29 | Technique categories are linked to disciplines (many-to-many, seeded). A session or competition accepts only techniques whose category is linked to its discipline, and the UI offers only those. Each discipline sees its own categories plus the shared ones (takedown, throw, other); the exact marks are in `07-seed-data.md`. | Each athlete sees only techniques that fit the discipline, while shared categories still let them record a takedown or a throw. Done per category, not per technique, to keep the seed small; it can be refined later. |
| D-30 | The development connection string (used when the API runs from the IDE) is kept in `dotnet user-secrets` of `MatForge.Api`, set with one command or in Rider (Tools → .NET User Secrets). In Docker the app gets it from `.env`. The run instructions in the README say how to set it. | The SQL Server in Docker accepts only a login and password (no Windows login), so the connection string contains a secret, and secrets never go to Git. User secrets are built into .NET and need no package. |
| D-31 | `dotnet-ef` (the command-line tool for creating migrations) is installed globally on the author's machine in the version matching EF Core 10 (`dotnet tool update -g dotnet-ef`); no tool manifest in the repository. | Only the author creates migrations. Running the app needs no tool: migrations are applied at startup. |
| D-32 | The seed JSON files are embedded in the `MatForge.Data` assembly (`EmbeddedResource`) and read by `DictionarySeeder` from there, so no file paths are involved (Rider, Docker, tests). | Nothing depends on the working directory or on copying files to the output folder. |
| D-33 | Positions are linked to disciplines (many-to-many, seeded), like technique categories (D-29). A round accepts only a position offered in its session's discipline, and the UI offers only those. Leg entanglements are named specifically (outside and inside ashi garami, saddle, 50/50, ...) instead of one generic entry. | Each athlete sees only positions that fit the discipline (a wrestler never sees closed guard or saddle). Specific positions keep the statistics precise; `other_position` stays the last resort. |
| D-34 | Frontend linting uses the linter that ships with the Vite template (oxlint, `npm run lint`). No Prettier: formatting follows `.editorconfig`. | Fewest packages; the template's default works without extra setup. |
| D-35 | Rules that make a request invalid (`400`) and need the database are FluentValidation validators that inject `AppDbContext` (one `AddScoped<IValidator<XxxRequest>, XxxValidator>()` line each; the existing validation filter runs them). Conflicts that depend on the current state of the data (`409`: duplicate club name, duplicate measurement date, club or competition in use) stay in the services. **Consequences:** validator tests that need the database live in `MatForge.IntegrationTests` and call the validator directly against the container database (not over HTTP); pure unit tests only for rules without the database; a validator loads the IDs it checks once, not one query per item; async rules require `ValidateAsync` (the filter already uses it); with `[ApiController]` a DataAnnotations failure returns `400` before the filter runs, so such a response shows only the DataAnnotations errors (known limitation). | **Context:** several rules depend on the database: I-02 (belts); I-01, I-03, I-05, I-09, I-10, I-11 (sessions); I-01, I-04, I-10 (competitions and fights). Sessions and competitions share rules (format belongs to the discipline, technique offered in the discipline). **Options:** A) validators that query the database; B) the same rules inside the services, throwing `BadRequestException`. **Why A:** one place and one error format for every invalid-request rule, with field keys such as `belts[0].degree` produced by the existing camelCase helper; services stay short and only do the use case; the shared rules of sessions and competitions are written once. B would need a helper class shared by the services for the same rules, which in practice is A under another name. **VetClinic / MartialHub comparison:** VetClinic (MVC, EF Core) has no FluentValidation; DataAnnotations on DTOs plus `ModelState.IsValid` in controllers, and the database rules live in services that return `bool`/`null` or throw `UnauthorizedAccessException`/`KeyNotFoundException` (`AnimalService.DeleteAnimalAsync`, `MedicationService.DeleteMedicationAsync`, `VisitUpdateService`), which controllers turn into `TempData` messages, `Forbid()` or `NotFound()`; referenced IDs are not checked before saving (select lists and foreign keys only); tests are unit tests with Moq and EF Core InMemory. MartialHub (plain PHP, PDO, PostgreSQL) has no service layer: rules sit in controllers (`SecurityController`) and in the database (`init.sql`: foreign keys, `CHECK` and `UNIQUE` constraints, a trigger); errors are messages re-rendered on the page; no tests. MatForge differs because it is a JSON API with one error format for the client (ProblemDetails with field keys), returns `404` instead of `403` for other users' data (no roles; existence is not revealed), and avoids EF Core InMemory (D-14), which ignores constraints and SQL Server's case-insensitive comparison. Database constraints stay as a safety net, as in MartialHub. |

## 5. Glossary

| Term | Meaning |
|---|---|
| Session | One training event on a given date. Kind = Grappling or Other. |
| Round | One sparring round inside a grappling session. |
| Finish | A round or fight decided by one decisive action: a submission, a takedown or throw, or a pin (D-20). |
| Finish for / against | Finish by the athlete (Win) / by the opponent (Loss). |
| Technique | A finishing action from the dictionary (e.g. Rear Naked Choke, Double Leg, Uchi Mata). |
| Category | Group of techniques (Choke, Arm Lock, Leg Lock, Takedown, Throw, Pin, ...). Each category is offered in chosen disciplines (D-29). |
| Position | Where the finish was applied (Mount, Back Control, Saddle, ...). See D-07. Each position is offered in chosen disciplines (D-33). |
| Format | Variant of a discipline (BJJ Gi / No-Gi, Freestyle / Greco-Roman, ...). |
| Character | Dominant nature of a grappling session (Technique, Drilling, Sparring, ...). |
| Period | Inclusive date range `[from, to]` used by analytics. |
| Share | A fraction 0..1 (UI shows percent). |

## 6. What the thesis shows (for the write-up)

Requirements analysis (this spec) → data model (ERD, normalization, integrity rules) → layered architecture with justified simplicity → rule-based analytics with explicit thresholds → automated tests including golden datasets → containerized deployment.
