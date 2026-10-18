# 05. Conventions

## 1. Principles in practice

**KISS comes first.** The simplest code that meets the spec wins. A new abstraction needs a concrete present need, not a possible future one.

| Principle | What it means here |
|---|---|
| **S**ingle responsibility | Controller: HTTP in/out only. Service: one use case per method. Calculator: pure math over records. Validator: one DTO. A class that needs "and" to describe it gets split. |
| **O**pen/closed | New insight rule = new `IInsightRule` class; no edits to existing rules or a central `switch`. New dictionary entry = seed edit, no code. |
| **L**iskov | Rules are interchangeable behind `IInsightRule`; no rule throws for valid input. |
| **I**nterface segregation | Every service has a small interface (`ISessionService`, ...), as in the author's usual structure. Other interfaces exist only when there are 2+ implementations or a real test seam (`IInsightRule`, `ICurrentUser`). Calculators, mappers, validators, and helpers have **no** interfaces. |
| **D**ependency inversion | Controllers depend on service interfaces; services get collaborators via constructor injection. Concrete `AppDbContext` is injected directly (D-04). |
| **DRY** | One validation filter, one exception handler, one `lookups` endpoint, one `apiFetch` helper on the frontend, one `Share(count, total)` helper, one `DateRange` type. **Rule of three:** duplicate once, abstract on the third occurrence. Duplication beats a wrong abstraction. |

### Explicitly not allowed (YAGNI list)

CQRS, MediatR, generic repository / unit-of-work, AutoMapper, generic base controllers or services, domain events, specification pattern, microservices, message queues, caching layers (beyond the one HTTP cache header on lookups), feature flags, background jobs, GraphQL.

## 2. Backend

- **.NET 10**, C#, `Nullable` enabled, `TreatWarningsAsErrors` true, file-scoped namespaces, `.editorconfig` committed.
- Dependency direction: `Api → Application → Data` (the three layers from the concept: API, business logic, data access). Entities, enums, `AppDbContext`, EF configurations, migrations, and seeding live in `Data`.
- Packages: EF Core (SQL Server provider), ASP.NET Core Identity, FluentValidation, **Mapperly** (entity → DTO and create request → entity; on update the service copies the scalar fields and rebuilds child lists by hand; analytics results are built by calculators, not mapped), Serilog is **not** used (built-in `ILogger`), `Microsoft.AspNetCore.OpenApi` for the OpenAPI document and `Swashbuckle.AspNetCore.SwaggerUI` for the UI.
- Structure by feature inside `Application`: `Features/Sessions/{ISessionService.cs, SessionService.cs, SessionDtos.cs, SessionValidators.cs, SessionMapper.cs}`; same for Competitions, Goals, Materials, Measurements, Profile, Analytics (`Calculators/`, `Rules/`).
- Naming: `XxxService` with its interface `IXxxService` in the same folder, `XxxDto`, `CreateXxxRequest` / `UpdateXxxRequest`, `XxxValidator`, `XxxMapper`, controllers `XxxController`. Async methods end with `Async` and accept a `CancellationToken`. Tables plural; enums stored as strings.
- **Current user:** the `ICurrentUser` interface lives in `Data` (the dependency direction `Api → Application → Data` allows `Data` to define it). Its implementation, `CurrentUser`, lives in `Api/Infrastructure`, reads `UserId` from the cookie claims (`HttpContext`), and is registered as scoped. `AppDbContext` takes `ICurrentUser` in its constructor and applies the global query filters (`01-domain-model.md` §1) with `currentUser.UserId`. Seeders and tests that need all rows use `IgnoreQueryFilters()`. `UserId` is `Guid?`: `CurrentUser` returns `null` and never throws when there is no `HttpContext` or no user claim (startup seeder, `dotnet ef`, login and registration before a user exists); the filters then return no rows.
- **Time:** services take today's date with `DateOnly.FromDateTime(DateTime.Now)` and pass it to the calculators as a parameter; calculators never read the clock. The container time zone is `Europe/Warsaw` (`TZ` in `docker-compose.yml`). The app image is the standard (not chiseled) `aspnet` image; if `TZ` has no effect, install `tzdata` in the Dockerfile.
- **Configuration:** `appsettings.json` for defaults, environment variables (via `.env` in Docker) for secrets. Analytics thresholds in `Analytics:Rules` (typed options).
- **Seeding:** on startup, apply migrations, then run the idempotent dictionary seeder (upsert by `Code`, or by `(DisciplineId, Code)` for belts and formats, see `01-domain-model.md` §3; never delete; JSON files embedded in `Data`). The demo-data seeder runs only when `Seed:DemoData=true`. It creates user `demo@matforge.com` (password `Demo1234!`) with about 6 months of realistic history ending on the day the app starts, laid out the same way every time (`new Random(42)`), and does nothing if that user already exists.
- No comments that restate code. Comment only the "why". No commented-out code. No TODOs without a task reference.

## 3. Frontend

- TypeScript `strict`, ESLint + Prettier configured, no `any`. Types stay simple: interfaces for DTOs and props, `useState<Type>`; no advanced generics, conditional, or mapped types. UI texts come from `src/texts.ts`.
- Components are function components with hooks. State is local (`useState`, `useEffect`); data comes from the API through one `apiFetch` helper and small hooks. No state-management or data-fetching library.
- No business rules in the UI beyond input convenience; the server validates and calculates.
- One component per file; feature code stays in its feature folder; shared pieces only when used by two or more features.
- Styling: plain CSS, **all in `src/styles/`**: `tokens.css` (the only place with color values), `base.css`, `components.css` (shared components), and one `<feature>.css` per feature with class names prefixed by the feature (`sessions-...`). No CSS modules, no UI library, no Tailwind, no inline style blobs.

## 4. Testing

| Level | Tooling | What |
|---|---|---|
| Unit | xUnit, FluentAssertions, Moq (only where a fake is needed) | Calculators, insight rules, goal progress, validators, source detection. **Golden tests GT-01..GT-06** from `03-analytics.md` exist as named tests. |
| Integration | xUnit, `WebApplicationFactory`, **Testcontainers (SQL Server)** | The SQL Server container fixture is set up in **T02**, `WebApplicationFactory` and the logged-in-user helper in **T03**; each task adds the integration tests of its own endpoints (CRUD, validation, isolation), and **T20** closes the gaps. Covers: auth flow, CRUD per resource, validation errors (400), not found (404), conflicts (409), **isolation** (user B cannot see or change user A's data, for every resource), account deletion. One shared container per test run. |
| Frontend | none in the MVP | Manual checks via the design review loop. |

Rules: a bug fix starts with a failing test. Tests name behavior (`Returns_404_when_session_belongs_to_another_user`). No mocks unless a real object is impractical; calculators receive a fixed `today` in tests.

## 5. Git

- Private GitHub repository. `main` is always green (build + tests pass).
- **The author makes every commit manually.** The agent never runs `git add`, `git commit`, `git push`, or any branch command. Branching is the author's choice.
- **The handoff after every finished task and the commit-title style are defined in `CLAUDE.md`** (a local instruction file for the AI agent, kept out of Git), the single source for agent behavior. In short: the agent prints one `git add` command with explicit paths and a short commit title without a prefix; the author commits.
- Tag `v1.0` at release; other tags are optional.
- `docs/` (specification and mockups) is part of the repository. Agent files (`CLAUDE.md`, `.claude/`, `.cursor/`) stay local, listed in `.git/info/exclude`.

## 6. Docker

`docker-compose.yml` services: `db` (SQL Server 2022, volume, healthcheck) and `app` (multi-stage Dockerfile: one stage builds the frontend with Node, the next builds the API and copies the frontend into `wwwroot`; the app waits for the db, migrates and seeds on start, and serves `/api` plus the static frontend with a fallback to `index.html`). One command: `docker compose up --build`. App at `http://localhost:8080`. Variables in `.env` (template `.env.example`: `MSSQL_SA_PASSWORD`, `Seed__DemoData`, `TZ`).

**Daily development (fast loop):** start only the database with `docker compose up db`, run the API from the IDE (debugger, hot reload) and the frontend with `npm run dev` (Vite proxy of `/api` to the API port). The full `docker compose up --build` is for end-to-end checks and the defense demo.

## 7. Definition of Done (every task)

- [ ] Behavior matches the referenced spec sections; spec updated if a decision changed.
- [ ] Acceptance criteria in the task are met.
- [ ] Unit and integration tests named in the task written and passing (`dotnet test`); build has no warnings.
- [ ] Frontend: `npm run lint` and `npm run build` pass; the new screen was checked at 390 px and 1440 px against `docs/design/` (the full review loop is T19).
- [ ] No code outside the task's scope; nothing from the YAGNI list.
- [ ] The agent has printed the `git add` command and the commit title for the author (see `CLAUDE.md`).

## 8. Working agreement for AI agents

See `CLAUDE.md` in the repository root. It is a local instruction file for the AI agent and is not committed; the project conventions that matter for the code are in this document. It is the single source for agent rules (reading order, one task at a time, no extras, verification, Git handoff).
