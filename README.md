# GeekPass

GeekPass is the website for **GeekPass Fest**, a fantasy, sci-fi and tabletop games festival.
It is also a team learning project: we build a real product step by step and use it to learn
modern .NET, Umbraco, architecture, design patterns, testing and microservices.

> All people, events and data in this repository are synthetic.

## Versions

| Component | Version | Why |
|---|---|---|
| Umbraco CMS | **18.3.1** (`Directory.Packages.props`) | Latest stable release (5 Nov 2026). Umbraco 18 is a Standard-Term Support release. |
| .NET SDK | **10.0.x**, `10.0.403` or later recommended (`global.json`) | Umbraco 18 runs on .NET 10 (LTS). `global.json` allows any 10.0 SDK from `10.0.100` up (`rollForward: latestFeature`). Install the newest patch to get security fixes. |
| SQL Server | 2022 (Docker image `mcr.microsoft.com/mssql/server:2022-latest`) | Umbraco and, later, the Booking module share one SQL Server with separate schemas (ADR-0001, coming in Issue #5). |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker Desktop (or Docker Engine with Compose v2). On Apple Silicon the SQL Server image runs through Rosetta.
- A trusted ASP.NET Core dev certificate: `dotnet dev-certs https --trust`

## Run locally

```bash
git clone https://github.com/SaloWho/geekpass.git
cd geekpass

# 1. Database: copy the example settings and pick your own local password
cp .env.example .env
docker compose up -d            # starts SQL Server and creates the GeekPass database

# 2. Secrets for the web app (stored outside the repo with dotnet user-secrets)
#    Use the same password you put in .env as GEEKPASS_DB_PASSWORD.
dotnet user-secrets --project src/GeekPass.Web set "ConnectionStrings:umbracoDbDSN" \
  "Server=localhost,1433;Database=GeekPass;User Id=sa;Password=<GEEKPASS_DB_PASSWORD>;TrustServerCertificate=true"
#    Backoffice admin password (at least 10 characters):
dotnet user-secrets --project src/GeekPass.Web set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<choose-an-admin-password>"

# 3. Run
dotnet run --project src/GeekPass.Web
```

On the first run Umbraco installs itself (unattended install). The app then creates the content
model and the demo content (`Cms/DemoContent/DemoContentSeeder.cs`). Wait for
`GeekPass demo content is ready` in the console.

The site runs on **https://localhost:44380**.

## Demo: publish the Catan tournament

1. Open **https://localhost:44380/umbraco** and sign in as `admin@geekpass.local` with your admin password.
2. Go to **Content → GeekPass Fest 2027 → Programme → Catan Championship**. It is a **draft**.
3. Review the fields (time, venue, *Requested capacity* = 4) and click **Save and publish**.
4. Open **https://localhost:44380/programme/**. The event is listed.
5. Open **https://localhost:44380/programme/catan-championship/**. You see the details and a
   "Booking opens soon" placeholder (booking arrives in Stage 2).
6. In the backoffice, use **Unpublish** on the event. The event page now returns 404 and the event
   disappears from the programme.

## Demo: compose the Home page (Issue #3)

1. In the backoffice open **Content → GeekPass Fest 2027**. The **Blocks** property is a Block Grid with
   a Hero, a Featured Events block and three Track Cards.
2. Drag a Track Card to another position, or change its width (4, 6 or 12 of 12 columns), then **Save and publish**.
3. Reload **https://localhost:44380/**. The order and widths follow your change.
4. In browser dev tools switch to a 360 px wide device. All blocks stack in one column.
5. Featured Events shows the next upcoming *published* events, so publish the Catan event first.

## Reset

```bash
docker compose down -v   # deletes the database volume
dotnet run --project src/GeekPass.Web   # installs and seeds again
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Login failed for user 'sa'` | The password in user-secrets differs from `.env`. If you changed `.env` after the first start, run `docker compose down -v` (the old password lives in the volume). |
| `Cannot open database "GeekPass"` | The `db-init` job did not run. Run `docker compose run --rm db-init`. |
| Port 1433 is in use | Set `GEEKPASS_DB_PORT=14330` in `.env` and use `localhost,14330` in the connection string. |
| Install fails with a password error | The admin password must be at least 10 characters. |
| "GeekPass demo content model is outdated" | The content model changed since your database was seeded (the seeder only creates, see ADR-0004). Run `docker compose down -v && docker compose up -d` and start the site again. |
| "GeekPass demo content: creating ... failed" | Check the full error in the console and open an issue. Seeding stops on the first error on purpose. |

## Repository structure

```
GeekPass.slnx                   solution (new .slnx format)
global.json                     .NET SDK selection
Directory.Build.props           shared build settings (net10.0, nullable, warnings as errors)
Directory.Packages.props        central package versions
docker-compose.yml              local SQL Server
docs/adr/                       architecture decision records
src/GeekPass.Web/               Umbraco host (the single deployable)
  Cms/DemoContent/              content model + synthetic demo content, as code
  Views/                        Razor templates (home, programme, festivalEvent)
  Views/Partials/blockgrid/     Block Grid rendering; Components/ has one view per block type
  wwwroot/css/                  styles
```

## How we work

- One GitHub Issue becomes one small pull request. Every Issue has a goal, acceptance criteria, demo steps and one review question.
- CI (`.github/workflows/ci.yml`) builds with warnings as errors, then boots the site on SQL Server and checks the demo pages.
- Important decisions are recorded as ADRs in [`docs/adr`](docs/adr).
