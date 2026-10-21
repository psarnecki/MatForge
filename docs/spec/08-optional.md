# 08. Optional extras

Not part of the MVP. Do these only after all MUST tasks (T01-T22) are finished and only when the author asks. Each item is independent.

## 1. GitHub Actions: build and test on every push
- File `.github/workflows/ci.yml`. Triggers: push and pull request to `main`.
- Steps: checkout, set up .NET 10, `dotnet restore`, `dotnet build`, `dotnet test`; set up Node, `npm ci`, `npm run build` in `frontend/`.
- Accept: a green run on `main`.

## 2. Dark color scheme
- Values from the "Dark" column in `04-ui.md`, applied under `[data-theme="dark"]` in `tokens.css`; toggle in the profile, choice kept in `localStorage`.
- Accept: every screen checked in both schemes at 390 and 1440 px.

## 3. Polish interface
- All UI texts already live in `frontend/src/texts.ts`. Add a second object with the Polish texts and a language switch in the profile (choice kept in `localStorage`).
- Moving later to a library such as react-i18next would be a mechanical change (the object becomes JSON files and `texts.x.y` becomes `t("x.y")`).
- Accept: every visible text is translated; technique and position names stay in English.

## 4. Goal and technique trend (from the concept)
- A goal such as "improve defense against chokes" cannot be counted automatically. For `Manual` goals with a linked technique, show a small chart on the goal card: how often the athlete was submitted by that technique, per month, taken from the sparring analytics.
- This is unrelated to the body-measurement chart (weight, height, body fat for every measurement), which is a MUST feature (`04-ui.md`, section 4.8).
- Needs a nullable `Goal.TechniqueId`, added by a migration when this extra is built.
- Accept: the chart appears on the goal card and uses the same data as the dashboard.

## 5. Competition days on the calendar
- Show a marker on days with a competition; a click opens the competition.

## 6. Analytics filter by training character
- Optional `characterId` filter for the sparring and insights endpoints.

## 7. Positions by discipline
- Offer each position only in the disciplines where it makes sense (a wrestler does not need `closed_guard`), using a link table like the one for technique categories.

## 8. Technique availability by format
- Hide categories that a ruleset forbids (for example chokes in Sport Sambo) by linking categories to formats instead of disciplines.

## 9. Fuller root README with screenshots
- Beyond the short README of T21: requirements, stack, project structure, and screenshots in `docs/design/shots/` linked from it.

## 10. Test code coverage report
- Add the `coverlet.collector` package to both test projects (it is part of the default `dotnet new xunit` template, removed in T01 to keep the MVP minimal).
- Run `dotnet test --collect:"XPlat Code Coverage"`; the Cobertura XML report gives line and branch coverage per project for the thesis text.
- Accept: a coverage report is produced for both test projects; the numbers are quoted in the thesis.

## 11. File logging with NLog
- The MVP logs to the console with the built-in `ILogger` (`docker compose logs app` shows it). NLog plugs in as a provider behind the same `ILogger<T>`, so no class that logs changes.
- Add the `NLog.Web.AspNetCore` package, an `nlog.config` with a console target and a rolling file target (for example `logs/matforge-${shortdate}.log`), and `builder.Host.UseNLog()` in `Program.cs`. In Docker, map the log folder to a volume so the files survive container restarts.
- Accept: logs appear in the console and in daily files; no logging code outside `Program.cs` changes.
