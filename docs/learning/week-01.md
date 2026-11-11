# Learning log: Week 1 (Stage 1, Umbraco and the site)

One page per week. For each topic: what we learned, where it is in the code, and a question for the team session.

## 1. Project setup that the whole team can reproduce (PR #2)

- **`global.json` with `rollForward: latestFeature`** selects the SDK. We allow any .NET 10 SDK from `10.0.100` up, so nobody is blocked by an older patch. CI always uses the newest one.
- **Central Package Management** (`Directory.Packages.props`): every version lives in one file. Projects say *what* they use, not *which version*.
- **`Directory.Build.props`**: shared settings for every project (`net10.0`, nullable, warnings as errors).
- **`.slnx`**: the new XML solution format in .NET 10. It is short and easy to merge.
- **Secrets:** `.env` holds the Docker password (git-ignored) and `dotnet user-secrets` holds the app secrets. Nothing secret is committed. In Stage 5 Key Vault replaces both.
- 📍 `global.json`, `Directory.*.props`, `docker-compose.yml`, `README.md`
- ❓ Why `rollForward: latestFeature` and not an exact SDK version? What breaks for a new team member in each case?

## 2. Umbraco content model as code (PR #2, ADR-0004)

- Umbraco stores document types, templates and content **in the database**. To make the demo reproducible we create them in code at startup (`DemoContentSeeder`), through the **same editing services the backoffice uses**. Umbraco validates our model exactly as if an editor had built it.
- **Fixed GUID keys**: every environment gets the same IDs. The event key becomes the stable ID for the Booking module later (ADR-0002).
- **Idempotent seeding**: every step looks the item up by key first, so a restart changes nothing. CI checks this with a restart.
- **Startup timing pitfall:** Umbraco answers HTTP **before** our `UmbracoApplicationStartedNotification` handler has finished. CI waited for the first `200` and failed. Fix: wait for a "ready" log line. Lesson: *"the server responds" is not the same as "the app is ready"* (think of health checks later).
- 📍 `src/GeekPass.Web/Cms/DemoContent/`
- ❓ When does code-based seeding stop being the right tool, and what would replace it?

## 3. Changing a model that already exists (PR #7)

- The seeder only **creates**. When Home changed shape, old databases would silently keep the old type.
- Small fix: a **model version guard**. We store `ContentModel.Version` in Umbraco's key-value table and stop with a clear "reset your database" message when it is outdated.
- This works only because all data is synthetic. With real content we need real **migrations** (Umbraco migration plans, or uSync). That is the trade-off recorded in ADR-0004.
- ❓ PR #2's review question: what happens when someone renames a property alias? (Now you can answer it with the version guard in mind.)

## 4. Block Grid and theme (PR #7)

- **Block Grid** = editors compose a page from **element types** (blocks) on a 12-column grid. In code: element types, a data type that lists the allowed blocks and their column spans, and one Razor view per block in `Views/Partials/blockgrid/Components/{alias}.cshtml`.
- The value is JSON with `layout` (where), `contentData` (what) and `expose` (visible for which culture). `HomeBlocks.cs` builds it in the same format the backoffice saves.
- Umbraco puts CSS variables on every grid item (`--umb-block-grid--item-column-span`). **Our CSS** decides the layout: thirds, then halves at 900 px, then one column at 600 px.
- **Theme in one `:root`**, with contrast **measured**, not guessed. Pure violet is for decoration only, and text uses a lighter violet.
- **Compiler pitfall:** our class `ContentModel.Content` and `System.IO.File` collided with Umbraco types of the same name (`CS0104`). Naming matters in big frameworks.
- ❓ Which block settings should editors control, and which stay fixed in code?

## 5. Time zones and testable rules (PR #8)

- Editors write the programme in **festival local time**, and Umbraco's date picker stores it **without a zone**. `FestivalClock` is **the one place** that attaches `Asia/Tbilisi` and returns `DateTimeOffset`. From there, UTC is always one call away.
- **`TimeProvider`** (built into .NET 8+) replaces `DateTime.Now`, so tests can freeze time (`FixedTimeProvider`).
- **Split mapping from rules:** `ProgrammeReader` maps Umbraco content to a plain `ProgrammeEvent` record. `ProgrammeSchedule` holds the pure rules (group by day, upcoming). Views only display. Pure rules are trivial to unit test, with no Umbraco and no database.
- The test that explains why: an event at **01:00 on 16 May in Tbilisi is still 15 May in UTC**. Group by the UTC date and the event shows on the wrong day.
- **Razor pitfall:** Razor HTML-encodes `+` as `&#x2B;`. The page showed "GMT+4" correctly, but our `grep` on the HTML did not find it. Test what the user sees, not raw bytes (Playwright will do this in a later stage).
- 📍 `src/GeekPass.Web/Site/`, `tests/GeekPass.Web.UnitTests/`
- ❓ Where should time zone conversion live, and why not in the Razor view?

## What CI checks now

Build (0 warnings) → 7 unit tests → boot the site on SQL Server → check `/`, `/programme/`, an event page and `/umbraco` → restart → check again.

## Coming next (not started)

- **#5 Speakers:** multi-node picker, reverse lookup (event ↔ speaker), Speakers Strip block.
- **#6 ADRs 0001–0003 + Booking module skeleton:** the module boundary, enforced by architecture tests. This is the bridge to Stage 2 (booking, concurrency, idempotency).
