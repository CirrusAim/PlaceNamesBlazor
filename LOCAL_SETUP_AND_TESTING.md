# Local Setup and Testing — PlaceNames Blazor

Short guide for running and testing the Blazor app locally. For system design, see [BLAZOR_SYSTEM_ARCHITECTURE.md](./BLAZOR_SYSTEM_ARCHITECTURE.md).

---

## Prerequisites

- **.NET 10** SDK
- **PostgreSQL** (local instance)
- (Optional) **Cloudflare R2** credentials if you want cloud image storage locally

---

## Config from example (for new developers)

The repo includes **placeholder** appsettings so you never commit real secrets:

- **`src/PlaceNamesBlazor/appsettings.Example.json`** — same structure as `appsettings.json` with placeholders.
- **`src/PlaceNamesBlazor/appsettings.Development.Example.json`** — same structure as `appsettings.Development.json` with placeholders.

**Setup for local testing:**

1. Copy the example files into the actual config files (in the same folder):
   - `appsettings.Example.json` → `appsettings.json`
   - `appsettings.Development.Example.json` → `appsettings.Development.json`
2. Replace the placeholders (e.g. `YOUR_DB_PASSWORD`, `YOUR_ADMIN_EMAIL`, `YOUR_ADMIN_PASSWORD`). For Cloudflare, only fill those values if you use `ImageStorage:Provider: Cloudflare`; otherwise leave `Provider` as `Local`.
3. Follow the rest of this guide (create database, run the app, etc.).

Do **not** commit `appsettings.json` or `appsettings.Development.json` after filling in real values; keep them in `.gitignore` or use User Secrets / env vars (see §1 and [SECURITY_REFERENCE.md](./SECURITY_REFERENCE.md)).

---

## 0. Schema created by the app (no Flask/Alembic)

The Blazor app **creates all tables and seed data** at startup. You do **not** need to run Flask or Alembic migrations.

- **You must create the empty database once** (PostgreSQL does not allow creating a database from inside another database). Example:
  ```bash
  createdb -U postgres place_names_db
  ```
  Or in `psql`: `CREATE DATABASE place_names_db;`
- **Then** set `ConnectionStrings:Local` (or `DB_*` env) to point at that database and run the app. On first run, the app applies the full schema (tables, indexes, fylker/stempeltyper seed, audit_logs, rapportoer.status, etc.). Idempotent: safe to run on every startup.

---

## 1. Database: local vs remote

The app chooses the database source via **appsettings** or **environment variables**.

### appsettings (e.g. `appsettings.json` or `appsettings.Development.json`)

| Section | Key | Values | Effect |
|--------|-----|--------|--------|
| `Database` | `UseLocalDatabase` | `true` / `false` | `true` → use `ConnectionStrings:Local` or `DB_*` env. `false` → use `DATABASE_URL`. |
| `Database` | `Mode` | `"Local"` / `"Remote"` | Overrides intent: `Local` = local DB, `Remote` = `DATABASE_URL` (must be set). |
| `ConnectionStrings` | `Local` or `DefaultConnection` | Npgsql connection string | Used when running in local mode. |

**Resolution order:** `Database:Mode` wins; then `Database:UseLocalDatabase`; if unset and `DATABASE_URL` is set → remote; otherwise local.

### Local connection string

Set either:

- **Option A:** `ConnectionStrings:Local` (or `DefaultConnection`) in appsettings:
  ```json
  "ConnectionStrings": {
    "Local": "Host=localhost;Database=place_names_db;Username=postgres;Password=YOUR_PASSWORD;Port=5432;Ssl Mode=Prefer"
  }
  ```
- **Option B:** Environment variables (same as Flask): `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_PORT`, `DB_SSLMODE` (optional; default `Prefer` for localhost).

### Remote (e.g. another server)

- Set `Database:Mode` to `"Remote"` (or `UseLocalDatabase` to `false`) and set **`DATABASE_URL`** to a `postgresql://user:password@host:port/database` URL. Required when not using local DB.

**Tip:** Do not commit real passwords. Use User Secrets (see below) or env vars for local dev.

---

## 2. Image storage: local vs Cloudflare

Controlled by **`ImageStorage:Provider`**.

| Value | Behaviour |
|-------|-----------|
| `Local` | Images stored under `wwwroot/images` (or `ImageStorage:Local:BasePath` if set). No cloud needed. |
| `Cloudflare` | Images stored in Cloudflare R2. Requires `ImageStorage:Cloudflare:*` to be set. |

### appsettings (Cloudflare)

```json
"ImageStorage": {
  "Provider": "Local",
  "Local": { "BasePath": "" },
  "Cloudflare": {
    "AccountId": "...",
    "AccessKeyId": "...",
    "SecretAccessKey": "...",
    "BucketName": "your-bucket",
    "PublicBaseUrl": "https://..."
  }
}
```

- **Local:** Set `Provider` to `"Local"`. `BasePath` empty → uses `wwwroot/images`; or set a relative/absolute path.
- **Cloudflare:** Set `Provider` to `"Cloudflare"` and fill all Cloudflare keys. Prefer env vars or User Secrets for `SecretAccessKey` (see [SECURITY_REFERENCE.md](./SECURITY_REFERENCE.md)).

---

## 3. First admin user

On first run, a hosted service creates an admin if none exists. Configure via appsettings or env:

- **`ADMIN_EMAIL`** — e.g. `admin@example.com`
- **`ADMIN_PASSWORD`** — plain text; only used once to create the user (stored hashed). Use strong password and do not commit.

`appsettings.Development.json` can hold these for local testing; for shared repos use User Secrets or env.

---

## 4. Running the app

From repo root:

```bash
cd PlaceNamesBlazor/src/PlaceNamesBlazor
dotnet run
```

Or open the solution in Visual Studio / Rider and run the Blazor project. The app listens on the URLs shown in the console (e.g. `https://localhost:5xxx`).

- **Development:** `ASPNETCORE_ENVIRONMENT=Development` (or launch from IDE) uses `appsettings.Development.json` and optional User Secrets.
- **Create DB/schema:** Ensure the PostgreSQL database exists. The app runs migrations and ensures audit/reporter schema on startup; no separate migration step needed for a fresh DB that matches the expected schema.

---

## 5. Optional: User Secrets (sensitive config)

To keep secrets out of appsettings and out of git:

```bash
cd PlaceNamesBlazor/src/PlaceNamesBlazor
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Local" "Host=localhost;Database=place_names_db;Username=postgres;Password=YOUR_PASS;Port=5432;Ssl Mode=Prefer"
dotnet user-secrets set "ADMIN_EMAIL" "admin@example.com"
dotnet user-secrets set "ADMIN_PASSWORD" "YourSecurePassword"
# If using Cloudflare:
dotnet user-secrets set "ImageStorage:Cloudflare:SecretAccessKey" "your-secret-key"
```

User Secrets override appsettings when `ASPNETCORE_ENVIRONMENT=Development`.

---

## 6. Quick local-test checklist

| Item | Action |
|------|--------|
| DB exists | Create the database once: `createdb -U postgres place_names_db` (or equivalent). |
| DB config | `Database:UseLocalDatabase: true` and `ConnectionStrings:Local` (or `DB_*` env) pointing at that database. |
| Schema | No separate step — the app creates all tables and seed data on startup. |
| Images | `ImageStorage:Provider: "Local"` to avoid Cloudflare. |
| Admin | Set `ADMIN_EMAIL` and `ADMIN_PASSWORD` (appsettings.Development.json or User Secrets). |
| Run | `dotnet run` from `PlaceNamesBlazor/src/PlaceNamesBlazor`. |
| Login | Use ADMIN_EMAIL / ADMIN_PASSWORD at `/Account/Login`; then open `/admin` or `/search`. |

---

## 7. Testing (manual)

The solution currently has **no automated test project** (no `dotnet test`). “Testing” means **manual verification** after running the app. Use the checklist above to get the app running, then work through the scenarios below to confirm behaviour before continuing development.

### 7.1 Manual test scenarios (smoke test)

After logging in as admin (or superuser), open **Admin** and verify:

| Area | What to check |
|------|----------------|
| **Records** | Search (filters + Search button), pagination (15 per page; First/Prev/Next/Last), single-record Delete (confirmation modal), Delete selected (confirmation modal), batch import (Excel with sheet "Fields"). |
| **Reports** | Status filter, pagination, View report, Approve/Reject (with confirmation flow). |
| **Users** | Role filter, Search (by name, email, username; full name and partial), Reporter status filter, Apply/Clear, pagination, Edit user, Delete (confirm), manage reporter (Approve/Reject/Revoke). |
| **Audit** | Filter by action type, actor, date from/to, Apply/Clear, pagination, View details. |
| **Stamp types / Subcategories** | List, Add subcategory, edit/delete if applicable. |

Optional: test as **guest** (limited search), **registered** (search, reporter register), and **superuser** (admin except user management) to confirm roles.

---

## 8. Reference: env / config keys (Blazor)

Config can be set in **appsettings.json** / **appsettings.Development.json** or via **environment variables**. Env vars use double underscore for nesting (e.g. `Database__UseLocalDatabase`).

| Purpose | Appsettings key / env example |
|--------|--------------------------------|
| Local vs remote DB | `Database:UseLocalDatabase` (true/false), `Database:Mode` ("Local"/"Remote"); env: `Database__UseLocalDatabase`, `Database__Mode`. |
| Local connection | `ConnectionStrings:Local` or `ConnectionStrings:DefaultConnection`; or env: `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_PORT`, `DB_SSLMODE`. |
| Remote DB | `DATABASE_URL` (postgresql://…). |
| First admin | `ADMIN_EMAIL`, `ADMIN_PASSWORD`. |
| Image storage | `ImageStorage:Provider` ("Local"/"Cloudflare"); `ImageStorage:Local:BasePath`; `ImageStorage:Cloudflare:*` (AccountId, AccessKeyId, SecretAccessKey, BucketName, PublicBaseUrl). Env: `ImageStorage__Provider`, etc. |
| Login lockout (optional) | `Lockout:MaxFailedAttempts`, `Lockout:LockoutDurationMinutes`. |
| Logging (optional) | `Logging:LogLevel:Default`, `Logging:LogLevel:Microsoft.AspNetCore`, etc. |
