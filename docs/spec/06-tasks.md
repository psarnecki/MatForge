# 06. Tasks

Work in order. Each task is a small, testable increment with its own Definition of Done (`05-conventions.md`, section 7). Spec references in brackets. Priorities as in `00-overview.md`. After each finished task the agent hands over a `git add` command and a commit title (`05-conventions.md`, section 5); the author commits manually.

## Phase 1: Skeleton

**T01 Repository and tooling** (MUST)
- Solution file in the repository root (whatever `dotnet new sln` creates; `.slnx` in the .NET 10 SDK, or `--format sln` if the IDE cannot open it) with 3 projects (`MatForge.Api`, `.Application`, `.Data`) and 2 test projects (`MatForge.UnitTests`, `MatForge.IntegrationTests`), the `Dockerfile` and `.dockerignore` in the root, the API's `wwwroot/` git-ignored, `.editorconfig`, `Directory.Build.props` (nullable, warnings as errors), `.gitignore`, `.git/info/exclude` entries for the agent files (`CLAUDE.md`, `.claude/`, `.cursor/`).
- `docker-compose.yml` with `db` and `app`; `.env.example`; `/health` endpoint. The app's Dockerfile builds the API now; the frontend stage is added in T04.
- Accept: `docker compose up --build` starts everything; `GET /health` returns 200.

**T02 Data model, database, seeding** [01, 07] (MUST)
- All entities (including the `TechniqueCategoryDiscipline` link table), enums, EF configurations (string enums, precision, indexes, check constraint, delete behavior), first migration.
- Dictionary seeder from JSON files per `07-seed-data.md`; idempotent; it also syncs the category-discipline links.
- Extend `GET /health` (from T01) with a database check (`AppDbContext.Database.CanConnectAsync()`): it returns 200 only when the database is reachable.
- Integration test setup (first use): the `MatForge.IntegrationTests` project with one SQL Server container per test run (Testcontainers) that applies the production migrations. No `WebApplicationFactory` yet.
- Accept: fresh database gets all dictionaries; running the seeder twice changes nothing; an integration test proves idempotence and unique codes. If Testcontainers cannot be made to work in reasonable time, ask the author; the fallback is SQLite in memory.

**T03 Auth, current user, isolation filter** [02 §2, 01 §1] (MUST)
- Identity with cookie auth, register/login/logout, `401` for unauthenticated, `ICurrentUser`, global query filters, `GET/PUT/DELETE /api/me`.
- Global exception handler and validation filter (ProblemDetails formats from `02-api.md`).
- Integration test setup: extend the T02 fixture with `WebApplicationFactory` and a helper that creates two logged-in users.
- Accept: integration tests pass for register → login → `GET /api/me` → logout → `401`, and a DbContext-level test (also in `MatForge.IntegrationTests`, on the container database) proves the isolation filter (two stub `ICurrentUser`s: a row added for user A is invisible to user B). Endpoint-level isolation tests start in T05 (clubs).

**T04 Frontend skeleton** [04 §1-3] (MUST)
- Vite + React + TypeScript project, `tokens.css` with the design tokens, fonts, `apiFetch`, hand-written DTO types (`src/types.ts`), UI texts in `src/texts.ts`, React Router, a few own small components (Button, Field, Select, Dialog, Toast), responsive app shell (black top bar on desktop / bottom tab bar on phone), login and register screens, auth guard.
- Accept: user can register and log in from the UI at 390 px and 1440 px; unauthenticated users are redirected to `/login`; the Dockerfile builds the frontend and `docker compose up --build` serves it from the API at `localhost:8080`.

## Phase 2: Profile and sessions

**T05 Lookups, clubs, belts, profile** [02 §3-4, 04 §4.9] (MUST)
- `GET /api/lookups`, clubs CRUD (409 rules), `PUT /api/me/belts`, profile screen.
- Accept: club in use cannot be deleted (409, message shown in UI); belt must match discipline (400); isolation tests for clubs.

**T06 Sessions API** [01 §4.4-4.5, 02 §5] (MUST)
- Service, DTOs, validators (I-01, I-03, I-05, I-10, kind rules), Mapperly mapper, controller, range query.
- Accept: create/read/update/delete with rounds; every invariant has a failing-input unit test (validators); integration tests for the endpoints, including isolation; list returns summaries with round counts; rounds are rejected when the character does not allow them (I-09).

**T07 Calendar and session form (without rounds)** [04 §4.3-4.4] (MUST)
- Month calendar with markers, day list, session form for both kinds, edit and delete.
- Accept: a session can be logged on a phone-width screen in under 60 seconds; defaults from the last session work; server errors appear on fields.

## Phase 3: Rounds, competitions, demo data

**T08 Rounds quick entry** [04 §4.4 step 4] (MUST)
- Win/Loss/Draw buttons, round chips, bottom sheet for technique/position/notes, live summary. The section is visible only for characters with `allowsRounds`.
- Accept: 8 rounds can be logged with one tap each; technique selector is a select grouped by category and limited to the categories offered in the session's discipline; Draw hides technique/position; saved data round-trips.

**T09 Competitions API and UI** [01 §4.6-4.7, 02 §6, 04 §4.5] (MUST)
- Service, validators (I-01, I-04, I-10), controller, list, form with fights, delete with 409 rule.
- Accept: CRUD with fights; technique only for Submission, Throw, or Pin, and only from categories offered in the competition's discipline; integration tests including isolation; usable at 390 px.

**T10 Demo data seeder** [05 §2] (SHOULD)
- Deterministic generator: user `demo@matforge.com` with the password `Demo1234!` (it meets the default Identity password rules); a history of about 6 months that **ends on the day the app starts**, so the dashboard's default 90 days always has data; 3-5 sessions per week; sparring rounds with a realistic distribution (a favorite finish, one recurring weakness, a gradual win-rate improvement); 3 competitions; 2 goals. The layout of sessions and results is the same every time, relative to the end date (`new Random(42)`). The seeder does nothing if the demo user already exists.
- Accept: with `Seed__DemoData=true`, a fresh start produces the same layout relative to the end date; a second start duplicates nothing; analytics later produce at least one strength and one weakness insight.

## Phase 4: Analytics backend

**T11 Overview and sparring calculators** [03 §2-4] (MUST)
- Row records, loaders, pure calculators, `Share` helper, date helpers (weeks, previous period, buckets), endpoints `overview` and `sparring`.
- Accept: **GT-01 (distribution part), GT-03, GT-04** pass as unit tests; integration tests for the endpoints, with data created inside the tests (not by the demo generator); empty data returns zeros and nulls.

**T12 Competition analytics and insight rules** [03 §5-6] (MUST)
- Competition calculator; `IInsightRule` and all rules from the catalog; options class bound from `Analytics:Rules`; endpoints `competitions` and `insights`.
- Accept: **GT-01 (insights part), GT-02, GT-06** pass; one unit test per rule (fires / does not fire at the boundary values); thresholds are only read from configuration.

**T13 Analytics hardening** [03 §9] (MUST)
- Filters (`disciplineId`, `formatId`), validation of `from`/`to`, isolation tests for all four endpoints, performance check with 5 years of generated data (under 500 ms).
- Accept: all analytics tests green; query plan reviewed (projections, no tracking).

## Phase 5: Dashboard and goals

**T14 Dashboard: overview and sparring** [04 §4.2] (MUST)
- Period selector with URL state, overview charts, record strip, trend line, finish lists, filters.
- Accept: matches the visual direction; works at 390 px; loading skeletons; empty state.

**T15 Dashboard: competitions and insights** [04 §4.2] (MUST)
- Competition tab, insight sentences built from `rule` + `params` in `texts.ts`, `InsufficientData` message.
- Accept: every rule code has a sentence in `texts.ts` and renders correctly with its params; demo data shows strengths and weaknesses.

**T16 Goals** [01 §4.8, 02 §7, 03 §8, 04 §4.6] (MUST)
- Goal calculator taking `today` as a parameter, service, validators (I-06), controller, UI.
- Accept: **GT-05** passes; competition used by a goal cannot be deleted (409); archive works.

## Phase 6: Remaining modules and polish

**T17 Materials library** [01 §4.9, 02 §8, 04 §4.7] (MUST)
- Source detection with unit tests (including subdomains such as `www.`, `m.`, `vm.tiktok.com`), CRUD, status quick change, filters.

**T18 Body measurements** [01 §4.10, 02 §9, 04 §4.8] (MUST)
- CRUD with unique date rule, chart.

**T19 Mobile and design polish** [04] (MUST)
- Review every screen at 360, 390, 768, and 1440 px against `docs/design/` and the "do not do this" list; keyboard focus; contrast check; `prefers-reduced-motion`.
- Accept: no horizontal scrolling at 360 px.

## Phase 7: Hardening and documentation

**T20 Integration test sweep** [05 §4, 02 §11] (MUST)
- Fill the gaps left by the per-task tests: validation (400), not found (404), and conflicts (409) for every resource; a parameterized **isolation** test that for every endpoint proves user B cannot read, update, or delete user A's data and cannot reference A's clubs or competitions; account deletion (`DELETE /api/me`: all data gone, other users untouched).
- Accept: all integration tests pass on a clean checkout with Docker running.

**T21 Material for the thesis** (SHOULD)
- ERD and an architecture diagram exported as images (from the Mermaid ERD in `01-domain-model.md` and the layers in `05-conventions.md`), and the list of endpoints from Swagger UI, for use in the thesis text.
- A short root `README.md` (a few lines): what the project is, `docker compose up --build`, the demo login, how to run the tests, and how to set the development connection string with `dotnet user-secrets` for running the API from the IDE (D-30). A fuller README with screenshots is optional (`08-optional.md`).

**T22 Buffer and release** (MUST)
- Bug fixes only. Fresh clone → `docker compose up --build` → demo works. Before the defense, build the images while online (`docker compose build`), then check the demo with the network disconnected. Tag `v1.0`.
