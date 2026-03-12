# Norwegian Place Names Database — Blazor (.NET 10)

Blazor Server re-implementation of the [Norwegian Place Names Database](../README.md) (Flask). Same PostgreSQL schema, role-based access, reporting workflow, and image storage (Local + Cloudflare R2).

## Stack

- **.NET 10** (LTS), **Blazor Web App** (Server interactivity)
- **Entity Framework Core** + **Npgsql** (PostgreSQL)
- **Auth:** Cookie authentication using existing `users` table; BCrypt password hashing
- **Image storage:** `IImageStorageService` — Local (wwwroot/images) or **Cloudflare R2** (full implementation)
- **Services:** Auth, Dropdown, StampSearch, Audit; first-admin seed at startup

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL (same DB as the Flask app, or a copy)

## Run locally

1. Set connection string in `src/PlaceNamesBlazor/appsettings.Development.json` (or env `ConnectionStrings__DefaultConnection`). Optional: `ADMIN_EMAIL` and `ADMIN_PASSWORD` to create the first admin if none exists.
2. From repo root:
   ```bash
   cd PlaceNamesBlazor/src/PlaceNamesBlazor
   dotnet run
   ```
3. Open https://localhost:5xxx (see console). Use **Login** / **Register**; **Search** for stamps; **Admin** (admin/superuser only) for records, reports, users, audit placeholders.

## Configuration

### Database: Local vs Render (like Flask .env)

- **Local (run tests / dev on your machine):** Set `Database:UseLocalDatabase` = `true` (or `Database:Mode` = `Local`) in `appsettings.Development.json`. Set the connection string in `ConnectionStrings:Local` with your **local** PostgreSQL password (or use env vars `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_PORT`, `DB_SSLMODE`).
- **Remote (Render or test against Render DB):** Set `Database:UseLocalDatabase` = `false` (or `Database:Mode` = `Remote`) and set **DATABASE_URL** (e.g. from Render dashboard, or in env). If `DATABASE_URL` is set and you don’t set `UseLocalDatabase`, the app uses the remote DB by default.
- **Force local when DATABASE_URL is set:** Set `Database:UseLocalDatabase` = `true` (same idea as Flask `FORCE_LOCAL_DB=1`).

See `env.example` in this folder for all options.

- **First admin:** `ADMIN_EMAIL`, `ADMIN_PASSWORD` — on startup, if no admin exists, one is created (same as Flask).
- **Image storage:** `ImageStorage:Provider` = `Local` or `Cloudflare`. For Cloudflare R2 set `ImageStorage:Cloudflare:*`. See [BLAZOR_SYSTEM_ARCHITECTURE.md](BLAZOR_SYSTEM_ARCHITECTURE.md) §9.9.

## Deploy on Render

Deploy as a **Web Service** using the **Dockerfile** in this folder. Set `PORT` and PostgreSQL (`ConnectionStrings__DefaultConnection` or `DATABASE_URL`); optionally `ADMIN_EMAIL`, `ADMIN_PASSWORD`, and Cloudflare R2 vars.

## Project layout

- `src/PlaceNamesBlazor/`
  - `Data/` — `PlaceNamesDbContext`, entities (fylker, kommuner, poststeder, stempler, users, etc.)
  - `Contracts/` — DTOs (Auth, Search, Dropdowns, Common)
  - `Services/` — Auth (cookie, BCrypt), Dropdown, StampSearch, Audit, ImageStorage (Local + **Cloudflare R2**), EnsureAdminHostedService
  - `Components/` — Pages (Home, Search, Login, Register, Logout, Admin with tabs), Layout

See [BLAZOR_SYSTEM_ARCHITECTURE.md](BLAZOR_SYSTEM_ARCHITECTURE.md) for full architecture.
