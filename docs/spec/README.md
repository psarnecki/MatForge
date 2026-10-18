# MatForge: Specification

> Name: **MatForge**.
> Engineering thesis project: *A web system supporting progress analysis of grappling-sport athletes.*
> Version: 0.1 (17 Oct 2026). Author: Piotr Sarnecki.

## How to use this specification

1. **The spec is the source of truth.** Code follows the spec. If reality disagrees with the spec, change the spec first, as part of the same task.
2. **One task at a time**, in the order of `06-tasks.md`. Each task is a small, testable increment.
3. **Do not add anything the spec does not ask for.** No extra layers, patterns, libraries, or features.
4. **Ambiguity: ask, do not guess.** Record the answer in the decision log (`00-overview.md`).
5. **Priorities:** `MUST` / `SHOULD` / `COULD`. When time runs out, cut from the bottom (see cut order in `00-overview.md`).

## Document map

| File | Content |
|---|---|
| `00-overview.md` | Purpose, scope, priorities, decision log, glossary |
| `01-domain-model.md` | Entities, fields, enums, constraints, ERD |
| `02-api.md` | Conventions, endpoints, request/response shapes, error handling |
| `03-analytics.md` | Definitions, formulas, insight rules, thresholds, **golden test cases** |
| `04-ui.md` | Design tokens, layout, screens, mobile behavior, copy rules |
| `05-conventions.md` | Architecture, structure, SOLID/DRY/KISS rules, testing, Git, Definition of Done |
| `06-tasks.md` | Ordered task list with acceptance criteria |
| `07-seed-data.md` | Dictionary contents (disciplines, techniques, positions, ...) |
| `08-optional.md` | Optional extras, only after all MUST tasks are done |
| `09-traceability.md` | Concept → specification table, and what the specification adds |
| `../design/` (`docs/design/`) | Five HTML mockups (open in a browser). Add your own references here, for example a Figma PDF or screenshots |

## Reading order for an agent starting a task

`00` (decisions) → the spec file(s) named in the task → `05` (conventions) → the task in `06`.

## Target repository layout

```
/
├─ MatForge.slnx            solution (the default format of the .NET 10 SDK)
├─ Directory.Build.props, .editorconfig, .gitignore, .dockerignore, .env.example
├─ Dockerfile                       multi-stage: builds the frontend, then the API
├─ docker-compose.yml               services: db, app
├─ docs/spec/                       this specification
├─ docs/design/                     mockups and other visual references (for example a Figma PDF)
├─ src/
│  ├─ MatForge.Data/        Entities/, Configurations/, Migrations/, Seed/ (JSON), AppDbContext, ICurrentUser
│  ├─ MatForge.Application/ Features/ (per feature: service + interface, DTOs, validators, mapper), Analytics/ (Calculators/, Rules/)
│  └─ MatForge.Api/         Controllers/, Infrastructure/ (exception handler, validation filter, CurrentUser), Program.cs, wwwroot/ (built frontend, git-ignored)
├─ tests/
│  ├─ MatForge.UnitTests/         pure logic (calculators, rules, validators)
│  └─ MatForge.IntegrationTests/  whole app over HTTP and DbContext tests (seeding, isolation filter), real SQL Server (Testcontainers)
├─ frontend/                        Vite + React + TypeScript (src/features, src/components, src/styles, ...)
└─ README.md                        short project description (a fuller one is optional, see `08-optional.md`)
```
