# Norwegian Place Names Database — Blazor System Architecture

**Document purpose:** System architecture for the Norwegian Place Names Database Blazor application: features, data model, technology stack, and deployment (Render).

**Stack:** **.NET 10** (LTS), Blazor Server, Entity Framework Core, PostgreSQL, Cloudflare R2 for images.

**Last updated:** February 2026

---

## Table of Contents

1. [Overview & Objectives](#1-overview--objectives)
2. [Technology Stack](#2-technology-stack)
3. [Hosting Model: Blazor Server vs WebAssembly](#3-hosting-model-blazor-server-vs-webassembly)
4. [System Architecture Layers](#4-system-architecture-layers)
5. [Database & Data Access](#5-database--data-access)
6. [Domain Model (Entities)](#6-domain-model-entities)
7. [Application Contracts (DTOs)](#7-application-contracts-dtos)
8. [Authentication & Authorization](#8-authentication--authorization)
9. [Feature Mapping & Implementation](#9-feature-mapping--implementation)  
    - [9.9 Image Storage](#99-image-storage)
10. [API & Communication](#10-api--communication)
11. [Internationalization](#11-internationalization)
12. [Security](#12-security)
13. [File & Project Structure](#13-file--project-structure)
14. [Deployment on Render](#14-deployment-on-render)

---

## 1. Overview & Objectives

### 1.1 Product Scope

The Norwegian Place Names Database is:

- A **full-stack web application** for storing, searching, and managing **Norwegian postmark (stamp) records**.
- **Role-based access control** (Guest, Registered, Superuser, Admin).
- **Reporting workflow**: reporter registration, report submission, approval hierarchy, data migration on approval.
- **Record management**: CRUD, multiple usage periods per stamp, batch import (Excel), bulk delete, image uploads.
- **Audit logging** for administrative actions.
- **Bilingual** interface (Norwegian and English).

### 1.2 Deployment

- **Database:** PostgreSQL; schema ensured at startup via hosted services where needed.
- **Hosting:** Deployable on **Render** via Docker (.NET is not natively supported).

---

## 2. Technology Stack

| Layer | Technology |
|-------|------------|
| **Runtime** | .NET 10 (LTS) |
| **Web** | ASP.NET Core + Blazor Server |
| **UI** | Blazor components (Razor) + C# |
| **ORM** | Entity Framework Core (Npgsql) |
| **Database** | PostgreSQL |
| **Schema** | SQL script at startup (full_schema.sql) + hosted services for audit/rapportoer/admin |
| **Auth** | Cookie auth, BCrypt |
| **Validation** | DataAnnotations, model binding |
| **CSRF** | Antiforgery (built-in) |
| **Excel import** | ClosedXML |
| **i18n** | IStringLocalizer&lt;SharedResource&gt;, SharedResourceLocalizer (in-memory En/No), ICurrentLocaleService, URL locale segment, RequestLocalizationMiddleware |
| **Config** | IConfiguration (appsettings, env, user secrets) |

---

## 3. Hosting Model: Blazor Server vs WebAssembly

### 3.1 Blazor Server (used)

- **Single deployable unit**: One ASP.NET Core app (Blazor Server + Razor Pages for auth).
- **Business logic and data access** run on the server; no REST API required for the UI.
- **Stateful**: SignalR keeps a connection; component state lives on server.
- **Auth**: Cookie-based; entirely server-side.
- **Render**: One Web Service (Docker) + PostgreSQL.

**Caveat:** SignalR and Render free-tier spin-down may cause reconnects; paid instances avoid spin-down.

### 3.2 Alternative: Blazor WebAssembly + Web API

- **Blazor WASM**: SPA in the browser; calls a separate Web API for data and actions.
- **Render**: Web Service for API (Docker) + optional Static Site for WASM; same PostgreSQL.

Use this if you want a SPA + API split or plan to support mobile/non-Blazor clients later. The backend (EF Core, services, auth, audit) could be exposed as a Web API.

---

## 4. System Architecture Layers

The Blazor app uses a **layered architecture**.

```
┌─────────────────────────────────────────────────────────────────┐
│  Presentation Layer                                              │
│  Blazor Pages & Components (Razor), Layouts, Localization        │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│  Application Layer                                               │
│  Blazor components invoke Services; minimal API for file/import   │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│  Business Logic Layer                                            │
│  Application Services: StampService, ReportingService,           │
│  UserService, ReporterService, AuditService, DropdownService     │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│  Data Access Layer                                               │
│  EF Core DbContext, Repositories (optional), Unit of Work         │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│  Data Layer                                                      │
│  PostgreSQL                                                      │
└─────────────────────────────────────────────────────────────────┘
```

- **Presentation:** Blazor Server pages and components, layouts, localization (SharedResourceLocalizer, locale-aware routes, flag-based language switcher).
- **Application:** Page logic invokes services; Razor Pages for auth form POST.
- **Business logic:** StampSearchService, RecordService, BatchImportService, ReportingService, AdminUserService, ReporterService, AuditService, DropdownService, StampTypeService, SubcategoryService, UsagePeriodService, AuthService, ICurrentLocaleService, IImageStorageService (Local + Cloudflare R2).
- **Data access:** EF Core DbContext; optional repositories.
- **Data:** PostgreSQL; schema applied at startup (full_schema.sql + hosted services).

---

## 5. Database & Data Access

### 5.1 Database

- **Engine:** PostgreSQL (unchanged).
- **Connection:** Npgsql; connection string from configuration (`DATABASE_URL` or `DB_*` / `ConnectionStrings:Local`).

### 5.2 ORM and Schema

- **ORM:** Entity Framework Core with Npgsql (**.NET 10**).
- **Schema:** EF entities map to the PostgreSQL schema. Tables and columns (e.g. `audit_logs`, `rapportoer.status`) are ensured at startup via **hosted services** (`EnsureDatabaseSchemaHostedService`, `EnsureAuditLogsTableHostedService`, `EnsureRapportoerSchemaHostedService`, `EnsureAdminHostedService`); no EF migrations are used for schema application.

### 5.3 Data Access Patterns

- **DbContext:** One main `PlaceNamesDbContext` (or equivalent name) with `DbSet<>` for all entities. Registered in DI as **scoped** (one instance per Blazor circuit/request) for normal operations (Search, CRUD, dropdowns). **IDbContextFactory&lt;PlaceNamesDbContext&gt;** is also registered; the factory creates a new, short-lived context when needed.
- **Batch import and concurrency:** Long-running **batch import** (Excel) must not share the circuit’s single scoped DbContext with Search or other operations, or EF Core throws “a second operation was started on this context instance” and related errors. **RecordService.CreateBatchAsync** therefore uses **IDbContextFactory&lt;PlaceNamesDbContext&gt;** to create a dedicated context for the entire batch: it creates the context at the start of the batch, performs all reads/writes (Kommuner, Poststeder, Stempler, Bruksperioder, images) and `SaveChangesAsync` in batches, then disposes that context. Search (`StampSearchService`) and the rest of the app continue to use the scoped `PlaceNamesDbContext`, so the user can run Search or other DB operations while a batch import is in progress without concurrency or disposed-context issues.
- **Repositories (optional):** E.g. `IStampRepository`, `ISearchRepository`, to keep services testable; not required for current structure.
- **Unit of Work (optional):** If repositories are used, a single `IUnitOfWork` can wrap the `DbContext` and commit per operation/request.

---

## 6. Domain Model (Entities)

Entities map to PostgreSQL tables. Names and relationships follow the schema.

### 6.1 Lookup Tables

| Entity | Table | Description |
|--------|-------|-------------|
| **Fylke** | `fylker` | County (fylke_id, fylke_navn) |
| **Kommune** | `kommuner` | Municipality (kommune_id, kommunenavn, fylke_id) |
| **Poststed** | `poststeder` | Postal location (poststed_id, postnummer, poststed_navn, tidligere_navn, kommune_id, etc.) |
| **Stempeltype** | `stempeltyper` | Stamp type (stempeltype_id, hovedstempeltype, stempeltype_full_tekst, maanedsangivelse_type, stempelutfoerelse, skrifttype) |
| **UnderkategoriStempeltype** | `underkategori_stempeltyper` | Subcategory of stamp type (underkategori_id, stempeltype_id, underkategori, underkategori_full_tekst) |

### 6.2 User and Reporter

| Entity | Table | Description |
|--------|-------|-------------|
| **User** | `users` | user_id, first_name, last_name, email, password_hash, role (guest, registered, superuser, admin), username, rapportoer_id, is_active, last_login, etc. |
| **Rapportoer** | `rapportoer` | rapportoer_id, initialer, fornavn_etternavn, epost, status (pending, approved, rejected, deactivated), etc. |

### 6.3 Core Stamp and Usage Period

| Entity | Table | Description |
|--------|-------|-------------|
| **Stempel** | `stempler` | stempel_id, poststed_id, stempeltype_id, underkategori_id, stempeltekst_oppe/nede/midt, stempelgravoer; date fields: dato_fra_gravoer, dato_fra_intendantur_til_overordnet_postkontor, dato_fra_overordnet_postkontor, dato_for_innlevering_til_overordnet_postkontor, dato_innlevert_intendantur; measurements/other: stempeldiameter, bokstavhoeyde, andre_maal, stempelfarge, tapsmelding, reparasjoner, dato_avtrykk_i_pm; kommentar, created_at, updated_at |
| **Bruksperiode** | `bruksperioder` | bruksperiode_id, stempel_id, bruksperiode_fra/til, first/last known dates and reporter FKs |
| **BruksperiodeBilde** | `bruksperioder_bilder` | bilde_id, bruksperiode_id, bilde_path, bilde_nummer (1 or 2), etc. |
| **Stempelbilde** | `stempelbilder` | bilde_id, stempel_id, bilde_path, er_primær, etc. (legacy/stamp-level images) |

### 6.4 Reporting

| Entity | Table | Description |
|--------|-------|-------------|
| **Rapporteringshistorikk** | `rapporteringshistorikk` | rapporteringshistorikk_id, stempel_id, bruksperiode_id, rapportoer_id, rapporteringsdato, godkjent_forkastet (G/F), besluttet_dato, initialer_beslutter, etc. |
| **RapporteringshistorikkBilde** | `rapporteringshistorikk_bilder` | Images attached to reports |

### 6.5 Audit

| Entity | Table | Description |
|--------|-------|-------------|
| **AuditLog** | `audit_logs` | audit_id, actor_id, actor_email, actor_role, action_type, target_type, target_id, target_description, details (JSON), ip_address, created_at |

### 6.6 Relationships (Summary)

- Fylke → Kommune → Poststed → Stempel (with Stempeltype, UnderkategoriStempeltype).
- Stempel → Bruksperiode → BruksperiodeBilde; Stempel → Stempelbilde.
- User ↔ Rapportoer (optional); Rapportoer → Rapporteringshistorikk; Rapporteringshistorikk → RapporteringshistorikkBilde.
- Stempel / Bruksperiode / Rapportoer linked in Rapporteringshistorikk.
- AuditLog → User (actor).

Foreign keys, indexes, and check constraints are reflected in EF configuration (Fluent API or attributes) and in the startup SQL script.

---

## 7. Application Contracts (DTOs)

**Yes — the system uses DTOs (Data Transfer Objects) at all service and API boundaries.** Entities are confined to the Data layer and to services’ internal use; the Presentation layer and any REST API never receive EF entities directly. This keeps UI/API stable when the schema evolves, avoids over-fetching and circular references, and makes validation and API contracts explicit.

### 7.1 Rules

- **Services** accept and return **DTOs** (request/response models), not entities. Services map between entities (via DbContext/repositories) and DTOs internally.
- **Blazor components** bind to DTOs (or small view models that wrap DTOs). They do not reference entity types.
- **REST API** (if present) serializes only DTOs; no entity types in controller action signatures or response bodies.
- **Validation** is applied on DTOs (DataAnnotations and/or FluentValidation), not on entities. Entities are assumed valid once built from validated DTOs.

### 7.2 Request DTOs (inputs to services)

| DTO | Purpose | Key properties |
|-----|---------|-----------------|
| **SearchRequest** | Search/filter criteria | Poststed, Stempeltekst, Kommune, FylkeId?, StempeltypeId?, Page, PageSize |
| **StampCreateRequest** | Create stamp | PoststedId, StempeltypeId, UnderkategoriId?, StempeltekstOppe, StempeltekstNede, StempeltekstMidt, Stempelgravoer, dates, measurements, Kommentar; nested UsagePeriods[], Images |
| **StampUpdateRequest** | Update stamp | Same as create + StempelId; UsagePeriods (add/update/delete by id) |
| **CreateRecordRequest** | Single or batch row | FylkeId, Kommune, Poststed, StempeltypeId, UnderkategoriId?, StempeltekstOppe?, StempeltekstNede?, StempeltekstMidt?, Stempelgravoer?; date strings (yyyy-MM-dd): DatoFraGravoer?, DatoFraIntendanturTilOverordnetPostkontor?, DatoFraOverordnetPostkontor?, DatoForInnleveringTilOverordnetPostkontor?, DatoInnlevertIntendantur?; Stempeldiameter?, Bokstavhoeyde?, AndreMaal?, Stempelfarge?, Tapsmelding?, Reparasjoner?, DatoAvtrykkIPm?; ForsteKjente?, SisteKjente?, BruksperiodeKommentarer?, BildePath?, Kommentar? (used by RecordService create and by batch import per row; batch Excel may include optional columns for the 12 stamp fields) |
| **BulkDeleteRequest** | Bulk delete | StampIds (list of int) |
| **ReportSubmitRequest** | Submit report | StempelId, BruksperiodeId, Rapporteringsdato, RapporteringFørsteSisteDato (F/S), DatoForRapportertAvtrykk, Images (at least one) |
| **ReportApproveRequest** | Approve report | ReportId, InitialerBeslutter, Kommentar? |
| **ReportRejectRequest** | Reject report | ReportId, Kommentar? |
| **ReporterRegisterRequest** | Reporter registration | Initialer, FornavnEtternavn, Epost, Telefon?, Medlemsklubb? |
| **UserUpdateRequest** | Update user (admin) | UserId, FirstName, LastName, Email, Username, Telephone, Role? (for promote/revoke) |
| **UserProfileUpdateRequest** | Update own profile | Email?, Telephone?, CurrentPassword?, NewPassword? |
| **AuditSearchRequest** | Audit log filter | ActionType?, ActorEmail?, FromDate?, ToDate?, Page, PageSize |

### 7.3 Response DTOs (outputs from services)

| DTO | Purpose | Key properties |
|-----|---------|-----------------|
| **SearchResultDto** | Single stamp in search results | StempelId, PoststedNavn, StempeltekstOppe, KommuneNavn, FylkeNavn, StempeltypeFullTekst, FirstKnownDate, LastKnownDate, ThumbnailPath?, … |
| **PagedResultDto&lt;T&gt;** | Paginated list | Items (T[]), TotalCount, Page, PageSize |
| **StampDetailDto** / **RecordDetailDto** | Full stamp for view/edit | All stamp fields: Poststed, Kommune, Fylke, Stempeltype, Underkategori; StempeltekstOppe/Nede/Midt, Stempelgravoer, Kommentar; the 12 extended fields (Stempeldiameter, Bokstavhoeyde, AndreMaal, Stempelfarge, Tapsmelding, Reparasjoner, DatoAvtrykkIPm; DatoFraGravoer, DatoFraIntendanturTilOverordnetPostkontor, DatoFraOverordnetPostkontor, DatoForInnleveringTilOverordnetPostkontor, DatoInnlevertIntendantur); UsagePeriods (BruksperiodeDto[]); Images (paths and ids) |
| **BruksperiodeDto** | One usage period | BruksperiodeId, BruksperiodeFra, BruksperiodeTil, FirstKnownDate, LastKnownDate, ReporterInitials, Kommentarer, ImagePaths |
| **ReportListItemDto** | Report in list | ReportId, StempelId, PoststedNavn, RapportoerInitialer, Status, Rapporteringsdato, DatoForRapportertAvtrykk |
| **ReportDetailDto** | Full report for view/approve | Report fields; Stamp summary; Usage period; Reporter; Images; GodkjentForkastet, BesluttetDato, InitialerBeslutter |
| **ReporterDto** | Reporter info | RapportoerId, Initialer, FornavnEtternavn, Epost, Status, Medlemsklubb |
| **UserListItemDto** | User in admin list | UserId, Email, FullName, Role, RapportoerStatus?, LastLogin, CreatedAt |
| **UserDetailDto** | Full user for admin | User fields; linked ReporterDto if any |
| **AuditLogEntryDto** | One audit entry | AuditId, ActorEmail, ActorRole, ActionType, TargetType, TargetId, TargetDescription, Details (object), IpAddress, CreatedAt |
| **DropdownsDto** | Lookup data for forms | Fylker (id, navn), Stempeltyper (id, code, fullTekst), Underkategorier (by stempeltype), Engravers (optional list) |
| **BatchImportResultDto** | Result of Excel batch import | Imported (int), Skipped (int), Errors (list of strings, e.g. row-level validation or batch failure messages) |
| **AuthResultDto** | After login/register | UserId, Email, Role, Token? (if JWT used) |

### 7.4 Validation

- **Request DTOs** use `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, etc., and/or FluentValidation validators. Services assume input is validated (e.g. by model binding or API filter) before use.
- **Response DTOs** are not validated; they are produced by the application and considered trusted.

### 7.5 Placement in solution

- **Option A:** All DTOs in a single folder, e.g. `PlaceNamesBlazor/Contracts/` or `PlaceNamesBlazor/DTOs/`, with subfolders by area (`Contracts/Search`, `Contracts/Stamp`, `Contracts/Reporting`, …) if the count grows.
- **Option B:** A separate class library `PlaceNamesBlazor.Contracts` referenced by the Blazor app and any future API project, containing only DTOs and validation (no EF, no UI). Recommended if you add a Web API later.

---

## 8. Authentication & Authorization

### 8.1 Authentication

- **Mechanism:** Cookie-based authentication (e.g. `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`). Passwords hashed with BCrypt. Auth cookie uses `SecurePolicy` (Always in production, SameAsRequest in dev) and `SameSite = Strict`.
- **Password policy:** Enforced via `PasswordPolicy` (min 8 characters, at least one uppercase, one lowercase, one digit) on registration, profile password change, and admin seed (`ADMIN_PASSWORD` must comply when creating the first admin).
- **Login lockout:** Email-based lockout after N failed attempts (`ILoginLockoutService`, `Lockout:MaxFailedAttempts`, `Lockout:LockoutDurationMinutes` in config); state in IMemoryCache; lockout message shown on login page.
- **Registration/Login:** Handled by **Razor Pages** (`Pages/Account/Login.cshtml`, `Register.cshtml`) so that form POST can set the auth cookie before the HTTP response is sent; Blazor Server would otherwise hit “headers already sent” when signing in from a component. Blazor pages (Login.razor, Register.razor) can redirect to or from these Razor Pages as needed.
- **Logout:** Razor Page or endpoint that signs out and redirects.
- **Current user:** `IAuthService.GetCurrentUserAsync()` (from cookie/claims) used by layout and pages.

### 8.2 Roles

| Role | Permissions |
|------|-------------|
| **Guest** | Limited search (e.g. 20 per session); view-only records; no reports; no admin. |
| **Registered** | Unlimited search; view records; register as reporter; submit reports if approved reporter; no admin. |
| **Superuser** | Admin panel; full record CRUD; approve/reject reports from Registered only; no user/role management. |
| **Admin** | All of the above; user management; role promote/revoke; audit logs; approve any report; cannot change own role or delete last admin. |

### 8.3 Authorization Implementation

- **Policies:** e.g. `RequireRole("Admin")`, `RequireRole("Admin", "Superuser")`, `RequireApprovedReporter`.
- **Attributes:** `[Authorize(Roles = "Admin,Superuser")]` on pages or endpoints.
- **Programmatic checks:** `IAuthorizationService` or custom helpers in components and services (e.g. report approval hierarchy: only Admin can approve Superuser reports).

### 8.4 First Admin User

- **EnsureAdminHostedService** at startup: ensures at least one admin exists (from `ADMIN_EMAIL` / `ADMIN_PASSWORD` env vars) when no admin is present. No-op if an admin already exists.

---

## 9. Feature Mapping & Implementation

### 9.1 Search and Filtering

- **UI:** Home page (search content): Poststed, Stempeltekst, Kommune, Fylke, Stempeltype filters; pagination; total count.
- **Backend:** `IStampSearchService.SearchAsync(SearchRequest)`; EF query with filters; returns paginated SearchResponseDto (items, total count, page).
- **Guest limit:** Enforced in StampSearchService using session (or cookie-based guest_search_id) so the count persists across refresh; configurable limit (e.g. 20 per guest).

### 9.2 Record Management (Stamps)

- **List/Search:** StampSearchService.SearchAsync; guest limit enforced via session/cookie (e.g. guest_search_id).
- **Create/Edit:** RecordEdit.razor; validation; `RecordService` create/update; Stempel (all fields including the 12 extended: measurements, stamp colour, loss report, repairs, date imprint, and the five date-from/delivery fields), Bruksperiode(s), images via IImageStorageService.
- **Delete:** Confirmation; `RecordService.Delete`; cascade and audit.
- **Bulk delete:** Admin Records tab; checkboxes; `RecordService` or batch delete; audit log.
- **Batch import:** Admin Records tab: user uploads an Excel file (sheet "Fields"). **BatchImportService** (ClosedXML) parses the file, resolves Fylke/Stempeltype from dropdown data, and builds a list of `(RowIndex, CreateRecordRequest)` per row. Required columns: Fylke, Stempletype, Poststed, Kommune; optional: Stempeltekst, Første kjente, Siste kjente, Kommentar, Bilde, plus the 12 extended stamp columns (Stempeldiameter, Bokstavhøyde, Andre mål, Stempelfarge, Tapsmelding, Reparasjoner, Dato avtrykk i PM, Dato fra gravør, Dato fra intendantur, Dato fra overordnet, Dato for innlevering, Dato innlevert intendantur). The script `create_sample_excel.py` generates a sample workbook with all columns. **RecordService.CreateBatchAsync** performs the import: it uses **IDbContextFactory** to create a **dedicated DbContext** for the entire batch (see §5.3), ensures Kommuner and Poststeder exist (or creates them), then inserts Stempler, Bruksperioder, and optional Stempelbilde/BruksperiodeBilde in chunks (e.g. BatchSaveSize 100), with multiple `SaveChangesAsync` calls. Returns **BatchImportResultDto** (Imported count, Skipped count, Errors list). Because the batch runs on its own context, Search and other operations (e.g. LoadRecords) can run concurrently without DbContext concurrency or disposed-context errors.

### 9.3 Usage Period Management

- **Add/Edit/Delete** usage periods within stamp create/edit flow; service methods for add/update/delete; enforce “first/last known” reporter and dates; up to 2 images per period (BruksperiodeBilde).

### 9.4 Subcategory Management

- **Stempeltype CRUD:** Admin-only; list, create, edit, delete with constraint checks (e.g. no delete if in use).
- **UnderkategoriStempeltype CRUD:** Same; unique (stempeltype_id, underkategori); used in dropdowns and stamp form.

### 9.5 Reporter Registration

- **Self-service:** Blazor form; `ReporterService.Register(...)`; create Rapportoer with status `pending`; link to current user if applicable.
- **Admin/Superuser:** Approve/reject (and deactivate) reporters; status updates; audit as needed.

### 9.6 Reporting Workflow

- **Submit report:** Authenticated, approved reporter only; form: stamp, usage period, dates, first/last flag, images (at least one); `ReportingService.SubmitReport(...)`; create Rapporteringshistorikk and RapporteringshistorikkBilde.
- **Approve:** Admin or Superuser; hierarchy: only Admin can approve Superuser reports; on approve, migrate data to Bruksperiode (and images to BruksperiodeBilde); set godkjent_forkastet, besluttet_dato, initialer_beslutter; audit.
- **Reject:** Same roles; set status and optional comment; audit.

### 9.7 User Management (Admin)

- **List users:** With reporter status, role, last login; filter/search.
- **View/Edit user:** Update name, email, username, phone; role promote/revoke (Superuser only; Admin rules: no self, no last admin); audit.
- **Delete (deactivate) user:** Soft delete (is_active); anonymize reports if required; audit.

### 9.8 Audit Logging

- **Write:** From services (user, reporter, stamp, reporting, usage period, bulk ops) via `IAuditService` (actor, action_type, target_type, target_id, target_description, details JSON, IP).
- **Read:** Admin-only Blazor page; filter by action type, actor, date range; paginated table; optional detail modal/export.

### 9.9 Image Storage

Image references are stored as a **string** (path or key) in `bilde_path`. **Contract:** The DB holds a string identifier; the app stores bytes, resolves a URL for display, and deletes when appropriate.


---

#### 9.9.1 Database and flows

All image references are stored as a **string** (path or key) in:

| Table | Column | Usage |
|-------|--------|--------|
| **stempelbilder** | bilde_path | Stamp-level images. |
| **bruksperioder_bilder** | bilde_path | Up to 2 images per usage period. |
| **rapporteringshistorikk_bilder** | bilde_path | Images per report. |

**Flows:** Upload → `UploadAsync` → store returned string in DB. Display: `GetDisplayUrl(storedIdentifier)` in DTOs. Delete: `DeleteAsync(storedIdentifier)` when removing a record. Report approve: report images can be copied into the usage period.  


---

#### 9.9.2 Abstraction and implementations

- **IImageStorageService:** All image read/write goes through this interface.  
- **Two implementations:**  
  1. **LocalImageStorageService** — saves under `wwwroot/images` (or configured base path); returns relative path (e.g. `images/stamp/guid.jpg`); `GetDisplayUrl` returns app-relative URL. Used when `ImageStorage:Provider` is not `Cloudflare`.  
  2. **CloudflareR2ImageStorageService** — uploads to Cloudflare R2 (S3-compatible); stores object key in DB; `GetDisplayUrl` returns public URL from `ImageStorage:Cloudflare:PublicBaseUrl`. Used when `ImageStorage:Provider` is `Cloudflare`.  
- **Database:** No schema change. Existing columns (e.g. `bilde_path`) still store a **single string** per image: for local it remains a path like `images/xyz.jpg`; for Cloudflare it will be an **object key** (R2) or **image ID / delivery URL** (Cloudflare Images), or a full public URL if preferred.  
- **Switching:** Provider is chosen by configuration (e.g. `ImageStorage__Provider=Local` vs `ImageStorage__Provider=Cloudflare`). Same code paths for upload, display, and delete; only the implementation changes.


---

#### 9.9.3 Storage Abstraction: IImageStorageService

**Interface (recommended)**  
- `Task<string> UploadAsync(Stream content, string fileName, string category, CancellationToken cancellationToken = default)`  
  - **content:** File bytes (e.g. from `IFormFile.OpenReadStream()`).  
  - **fileName:** Original or safe filename (extension preserved for content type).  
  - **category:** Logical bucket/folder (e.g. `stamps`, `usage-periods`, `reports`) for organization and optional policy.  
  - **Returns:** The **stored identifier** to persist in DB (path, key, or URL depending on provider).  
- `string GetDisplayUrl(string storedIdentifier)`  
  - **Input:** Value stored in `bilde_path` (or equivalent).  
  - **Returns:** URL to use in `<img src="...">` (relative for local, absolute for Cloudflare).  
- `Task DeleteAsync(string storedIdentifier, CancellationToken cancellationToken = default)`  
  - Deletes the object/file if it exists; no-op or log if already missing.  
- Optional: `Task<string> CopyAsync(string sourceStoredIdentifier, string category, CancellationToken cancellationToken = default)` for “copy report image to usage period” without re-uploading bytes (useful when both are in the same Cloudflare bucket).

**Usage in app**  
- **Upload (stamp/report/usage period):** After validating the file, call `UploadAsync` → save returned string in the entity’s `bilde_path`.  
- **Display (list/detail):** When building DTOs, replace raw `bilde_path` with `GetDisplayUrl(bilde_path)` for the “image URL” field.  
- **Delete (stamp/report/usage period):** Before or after deleting the DB row, call `DeleteAsync(bilde_path)`.  
- **Report approve (copy to usage period):** Either re-upload each report image and save the new identifier, or use `CopyAsync` if the implementation supports it.

---

#### 9.9.4 Cloudflare Options: R2 vs Images

**Option A: Cloudflare R2 (recommended for this app)**  
- **What it is:** S3-compatible object storage; no egress fees; pay for storage and operations.  
- **Flow:** Upload file to a bucket (e.g. `place-names-images`) with a key like `{category}/{stempel_id or guid}/{filename}`. Store the **key** (or a short prefix) in `bilde_path`.  
- **Display:** Either make the bucket **public** and build URL from key (e.g. `https://pub-xxx.r2.dev/key` or custom domain), or use **signed URLs** (via Cloudflare Workers or a small API that generates presigned URLs). For public read-only images, public bucket + custom domain is simplest.  
- **Pros:** Simple, cheap, same “path-like” key in DB; fits existing 255-char column; no per-image fee. **Cons:** No built-in resize/variants (resize in app before upload or add a Worker later).

**Option B: Cloudflare Images**  
- **What it is:** Managed image product: upload via API, Cloudflare stores and serves with variants (e.g. thumbnail, webp).  
- **Flow:** Upload via Images API → receive an **image ID** (and delivery URL). Store **image ID** or the **delivery URL** in `bilde_path`.  
- **Display:** Use delivery URL (e.g. `https://imagedelivery.net/{account_hash}/{image_id}/public`) or variant URL.  
- **Pros:** Built-in optimization and variants. **Cons:** Per-image storage and per-request cost; delivery URL can be long (may need to store only image ID and build URL in `GetDisplayUrl`).

**Recommendation**  
- Use **Cloudflare R2** as the primary Cloudflare backend for the Blazor app: it matches the current “store a path-like string” model, keeps costs predictable, and allows a single implementation (Local + R2).  
- If you later need thumbnails or variants, add a Cloudflare Worker in front of R2 (or use Image Resizing) or introduce Cloudflare Images as a second provider behind the same `IImageStorageService` (e.g. different category or config).

---

#### 9.9.5 Implementation Details (Cloudflare R2)

- **Credentials:** R2 is S3-compatible. Use **Access Key ID** and **Secret Access Key** (R2 API tokens) in configuration; do not commit them.  
- **Bucket:** One bucket (e.g. `place-names-images`) with optional **prefixes** per category: `stamps/`, `usage-periods/`, `reports/`.  
- **Keys:** `{category}/{id_or_guid}/{sanitized_filename}` to avoid collisions and keep listing/debugging simple (e.g. `usage-periods/42/1.jpg`, `reports/abc-guid/photo.png`).  
- **Public access:** Enable “Public bucket” or “Custom domain” for the bucket so `GetDisplayUrl` can return `https://<custom-domain>/<key>` or R2’s public URL. If you need private images, use signed URLs (e.g. generated in a small endpoint or Worker).  
- **Libraries:** Use **AWS SDK for .NET** (S3 client) with R2 endpoint URL (`https://<account_id>.r2.cloudflarestorage.com`) and the bucket name; same API as S3.  
- **Validation:** Keep server-side checks (content type allow-list, max size, extension) in the app before calling `UploadAsync`; R2 does not replace input validation.

---

#### 9.9.6 Configuration

**Configuration (appsettings.json or environment)**  
- `ImageStorage:Provider`: `Local` or `Cloudflare` (case-insensitive).  
- **Local:** Optional `ImageStorage:Local:BasePath` (default `wwwroot/images`).  
- **Cloudflare R2:**  
  - `ImageStorage:Cloudflare:AccountId`  
  - `ImageStorage:Cloudflare:AccessKeyId`  
  - `ImageStorage:Cloudflare:SecretAccessKey`  
  - `ImageStorage:Cloudflare:BucketName` (default `place-names-images`)  
  - `ImageStorage:Cloudflare:PublicBaseUrl` (e.g. `https://images.yourdomain.com`) so `GetDisplayUrl` can build the full URL from the key.

**Registration**  
- In `Program.cs`, `IImageStorageService` is registered as a singleton: if `ImageStorage:Provider` is `Cloudflare`, use `CloudflareR2ImageStorageService`; otherwise `LocalImageStorageService`. Injected into StampSearchService, RecordService, ReportingService, and components that upload (RecordEdit, Search for report submit).

---

#### 9.9.7 Migration of Existing Data

- **Existing DB rows** may contain **local paths** (e.g. `images/old.jpg`). Options for migrating to Cloudflare:  
  1. **Dual-read during transition:** If the stored value looks like a local path (e.g. starts with `images/`), `GetDisplayUrl` serves from local or a legacy URL; if it looks like a key or Cloudflare URL, use Cloudflare.  
  2. **One-time migration job:** Script that (a) reads all `bilde_path` from `stempelbilder`, `bruksperioder_bilder`, `rapporteringshistorikk_bilder`, (b) for each file that exists on disk, calls `UploadAsync` (Cloudflare), (c) updates the row with the new key/URL. After that, switch provider to Cloudflare and optionally remove local files.  
- New uploads in Blazor always go through `IImageStorageService`, so they will use Cloudflare when configured, even during testing.

---

#### 9.9.8 Project Structure (Image Storage)

- **Interface:** `Services/ImageStorage/IImageStorageService.cs` (or under `Contracts` if you prefer).  
- **Implementations:** `Services/ImageStorage/LocalImageStorageService.cs`, `Services/ImageStorage/CloudflareR2ImageStorageService.cs`.
- **Registration:** In `Program.cs`, read `ImageStorage:Provider` from configuration and register the corresponding `IImageStorageService` implementation as a singleton.

This keeps image storage behind a single abstraction, with Cloudflare (R2) provisioned from the start and usable for testing and production without a second “big migration” later.

---

## 10. API & Communication

### 10.1 Blazor Server (Primary)

- **No REST API required** for the main UI: components call C# services directly (in-process). Forms and callbacks are handled by Blazor.
- **File upload / Excel:** Can be done via form post to a minimal API endpoint or a Blazor endpoint that accepts `IFormFile` and calls the same service layer.

### 10.2 Optional REST API (for WASM or external clients)

If a Web API is added:

- **Auth:** POST register, login; POST logout; GET auth/status, user/me; PUT user/profile; PUT user/role (Admin).
- **Stamps:** GET search, GET record/{id}, POST create, POST update/{id}, DELETE delete/{id}, POST bulk-delete, POST upload-image, POST batch-import, GET dropdowns.
- **Reporters:** POST reporters/register, GET reporters/me, PUT reporters/{id}.
- **Reports:** POST reports, GET reports, GET reports/{id}, POST reports/{id}/approve, POST reports/{id}/reject.
- **Admin:** GET/PUT/DELETE admin/users, GET admin/audit (with query params).



---

## 11. Internationalization

- **Locales:** Norwegian (default) and English.
- **URL:** All Blazor routes include a locale segment: `/{locale:regex(no|en)}/...`, e.g. `/no/`, `/en/`, `/no/admin`, `/en/admin/records`, `/no/search`, `/en/record/42`. The locale is preserved when navigating; pages and layout use it to resolve paths and to select strings.
- **Current locale service:** **ICurrentLocaleService** (implementation: **CurrentLocaleService** in `Services/Locale/`) returns the current locale (`"no"` or `"en"`) from the request (e.g. URL or culture). Registered scoped. Used by the localizer and by layout/pages for `GetPath(relativePath)` and `GetPathForLocale(targetLocale)` so links keep or switch the locale.
- **Localization resources:** **SharedResourceLocalizer** (implements `IStringLocalizer<SharedResource>`) uses **in-memory dictionaries** for Norwegian (No) and English (En)—no `.resx` at runtime for these keys. Keys are grouped by feature: `Search_*` (Poststed, Stempeltekst, Fylke, Stempeltype, Button, Clear, …), `Admin_*` (tabs, filters, Batch import, Audit, Users, Reports, …), `Audit_*` (action types, UserAgent, …), `Nav_*`, `Common_*`, `RecordEdit_*`, `ReporterRegister_*`, `Profile_*`, etc. **GetStrings()** selects the dictionary based on `ICurrentLocaleService.GetCurrentLocale()` (e.g. `"no"` → No, else En). Components use `@inject IStringLocalizer<SharedResource> Loc` and `Loc["Key"]` (or `Loc["Key", args]` for formatted strings).
- **Middleware:** `RequestLocalizationMiddleware`; culture can be set from route or cookie; Blazor pages receive `{locale}` as a route parameter where needed.
- **Language switcher (UI):** In **MainLayout.razor**, the switcher uses **bitmap flag images** (not text "NO|EN"): **Norwegian flag** (`/images/flag-no.png`) and **British/English flag** (`/images/flag-en.png`) from `wwwroot/images/`. The current locale is shown as the active flag (CSS: `.locale-flag-active`); the other is a link that navigates to the same path with the other locale (e.g. `/en/admin/records` ↔ `/no/admin/records`). Styles in `app.css`: `.locale-flag` (size 24×18 px), `.locale-flag-link` (inactive opacity), `.locale-flag-active` (full opacity, subtle border).
- **Localized areas:** All user-facing text in Admin (Records filters, tabs, Batch import help, Audit tab and detail modal, Reports, Users), Search, RecordEdit, ReporterRegister, Profile, Nav, modals (confirm delete, audit detail), and error/not-found pages use `Loc["..."]` so the UI is fully Norwegian when locale is `no` and English when `en`.
- **Record form key mappings (RecordEdit / extended stamp fields):** The create/edit record form uses `Record_*` keys. In addition to existing keys (e.g. `Record_StempeltekstTop`, `Record_Engraver`, `Record_Kommentar`), the 12 extended stamp fields use:

| Key | English (En) | Norwegian (No) |
|-----|--------------|----------------|
| Record_Stempeldiameter | Stamp diameter | Stempeldiameter |
| Record_Bokstavhoeyde | Letter height | Bokstavhøyde |
| Record_AndreMaal | Other measurements | Andre mål |
| Record_Stempelfarge | Stamp colour | Stempelfarge |
| Record_Tapsmelding | Loss report | Tapsmelding |
| Record_Reparasjoner | Repairs | Reparasjoner |
| Record_DatoAvtrykkIPm | Date imprint in PM | Dato avtrykk i PM |
| Record_DatoFraGravoer | Date from engraver | Dato fra gravør |
| Record_DatoFraIntendantur | Date from Intendant to General Post Office | Dato fra intendantur til overordnet postkontor |
| Record_DatoFraOverordnet | Date from General Post Office | Dato fra overordnet postkontor |
| Record_DatoForInnlevering | Date of submission to the main post office | Dato for innlevering til overordnet postkontor |
| Record_DatoInnlevertIntendantur | Date submitted to the Intendant | Dato innlevert intendantur |

- **Footer (all pages):** `Footer_AllRightsReserved` → En: "All rights reserved." / No: "Alle rettigheter reservert." (shown in MainLayout footer).

These are defined in **SharedResourceLocalizer** (En and No dictionaries).

---

## 12. Security

- **HTTPS:** Enforced in production.
- **CSRF:** Antiforgery tokens for forms and state-changing operations (built into Blazor/ASP.NET Core).
- **SQL injection:** Avoided by using EF Core parameterized queries only.
- **XSS:** Blazor’s default output encoding; avoid `@((MarkupString)...)` with user input.
- **Passwords:** BCrypt (work factor 10); no plaintext storage. **Password policy:** min 8 characters, uppercase, lowercase, digit (registration, profile change, admin seed).
- **Cookie options:** Auth cookie `SecurePolicy` = Always in production; `SameSite = Strict`.
- **Login lockout:** Email-based; configurable N failed attempts and lockout duration; IMemoryCache; see §8.1.
- **Rate limiting:** Guest search cap enforced; no global/per-route rate limiting for login/register (AspNetCoreRateLimit or custom optional).
- **Audit:** Admin/superuser actions logged with actor, type, target, details, IP, timestamp. **User-Agent** captured in `audit_logs.user_agent`; table is **append-only** (PostgreSQL triggers block UPDATE/DELETE/TRUNCATE). Do not store passwords, tokens, or full request bodies in audit details.
- **File upload:** Image uploads validated by **magic bytes** (JPEG, PNG, GIF, WebP, BMP); **Kestrel** `MaxRequestBodySize` = 10 MB; stored paths built from controlled segments; path traversal rejected (local storage).

---

## 13. File & Project Structure

Current layout of the **Blazor Server** application:

```
PlaceNamesBlazor/
├── BLAZOR_SYSTEM_ARCHITECTURE.md   # This document
├── src/
│   └── PlaceNamesBlazor/
│       ├── PlaceNamesBlazor.csproj  # .NET 10, Npgsql EF Core, ClosedXML, AWSSDK.S3, BCrypt.Net-Next
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Configuration/
│       │   └── DatabaseConnectionFactory.cs
│       ├── Components/
│       │   ├── App.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── ReconnectModal.razor
│       │   ├── Pages/              # Blazor pages
│       │   │   ├── Home.razor       # Search content (guest/registered) or redirect to /admin (admin/superuser)
│       │   │   ├── Admin.razor      # Tabs: Records, Reports, Users, Stamp types (Subcategories), Audit (admin only)
│       │   │   ├── Search.razor     # Inlined into Home; search form, results, submit report, record detail modal
│       │   │   ├── Record.razor     # Record detail by id
│       │   │   ├── RecordEdit.razor # Create/edit record (admin/superuser)
│       │   │   ├── Profile.razor
│       │   │   ├── ReporterRegister.razor
│       │   │   ├── Login.razor, Register.razor, Logout.razor  # Blazor wrappers; auth POST via Razor Pages
│       │   │   ├── NotFound.razor, Error.razor
│       │   │   └── ...
│       │   └── Routes.razor, _Imports.razor
│       ├── Pages/
│       │   └── Account/            # Razor Pages for auth (form POST to avoid response-started issues)
│       │       ├── Login.cshtml (+ .cs)
│       │       ├── Register.cshtml (+ .cs)
│       │       └── Logout.cshtml (+ .cs)
│       ├── Data/
│       │   ├── PlaceNamesDbContext.cs
│       │   └── Entities/           # EF entities (User, Rapportoer, Stempel, Bruksperiode, etc.)
│       ├── Resources/              # Localization
│       │   ├── SharedResource.cs   # Marker type for IStringLocalizer<SharedResource>
│       │   └── SharedResourceLocalizer.cs  # In-memory En/No dictionaries; implements IStringLocalizer<SharedResource>
│       ├── Contracts/              # DTOs (see §7)
│       │   ├── Admin/               # UserListDto, UserUpdateRequest (incl. ApprovedByEmail for reporter)
│       │   ├── Auth/                # AuthResultDto, RegisterRequest
│       │   ├── Dropdowns/
│       │   ├── Record/
│       │   ├── Reporter/
│       │   ├── Reporting/
│       │   ├── Search/
│       │   ├── Subcategory/
│       │   └── UsagePeriod/
│       ├── Services/
│       │   ├── Locale/              # ICurrentLocaleService, CurrentLocaleService (URL-based locale)
│       │   ├── ImageStorage/        # IImageStorageService, LocalImageStorageService, CloudflareR2ImageStorageService
│       │   ├── Admin/               # IAdminUserService, AdminUserService
│       │   ├── Audit/               # IAuditService, AuditService
│       │   ├── Auth/                # IAuthService, AuthService
│       │   ├── Record/              # IRecordService, RecordService (CreateBatchAsync uses IDbContextFactory); IBatchImportService, BatchImportService
│       │   ├── Reporter/            # IReporterService, ReporterService
│       │   ├── Reporting/           # IReportingService, ReportingService
│       │   ├── StampType/           # IStampTypeService, StampTypeService
│       │   ├── Subcategory/         # ISubcategoryService, SubcategoryService
│       │   ├── UsagePeriod/         # IUsagePeriodService, UsagePeriodService
│       │   ├── IStampSearchService.cs, StampSearchService.cs
│       │   ├── IDropdownService.cs, DropdownService.cs
│       │   ├── EnsureAuditLogsTableHostedService.cs
│       │   ├── EnsureRapportoerSchemaHostedService.cs
│       │   └── EnsureAdminHostedService.cs
│       └── wwwroot/
│           ├── app.css             # Includes .locale-flag, .locale-flag-active, .locale-flag-link for language switcher
│           └── images/             # Local image upload target (when Provider is Local); also flag-no.png, flag-en.png for locale switcher
└── Dockerfile                       # For Render
```

- **Authentication:** Login and Register use **Razor Pages** (`Pages/Account/Login.cshtml`, etc.) for form POST so cookies can be set before the response is committed; Blazor components then redirect to or from these as needed.
- **Navigation:** A single **Home** link in the nav: for guest/registered it shows search content; for admin/superuser it redirects to `/admin`. No separate "Search" or "Admin" top-level nav.

---

## 14. Deployment on Render

- **Runtime:** .NET is **not** a natively supported language on Render; deployment is via **Docker**.
- **Service type:** Web Service.
- **Build:** Dockerfile that restores and publishes the app (e.g. `dotnet publish -c Release`), then runs the published app (e.g. `dotnet PlaceNamesBlazor.dll`).
- **Database:** Use existing PostgreSQL on Render; provide `DATABASE_URL` (or equivalent) as environment variable; read in `Program.cs`/`appsettings` for EF connection string.
- **Env vars:** e.g. `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `ASPNETCORE_DataProtection` (or equivalent secrets).
- **SignalR:** On paid plans without spin-down, Blazor Server works without change; on free tier, document possible reconnects after spin-down.

**Dockerfile (minimal):**

- Base: `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime).
- Build stage: `mcr.microsoft.com/dotnet/sdk:10.0`; `COPY` source; `dotnet restore`; `dotnet publish -c Release -o /app/publish`.
- Run stage: `COPY --from=build /app/publish`; `ENTRYPOINT ["dotnet", "PlaceNamesBlazor.dll"]`; expose port (e.g. 8080 or 5000) and set `ASPNETCORE_URLS` if needed.

Render will build from this Dockerfile and run the container; no native .NET buildpack required.

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Feb 2025 | Initial Blazor system architecture |
| 1.1 | Feb 2026 | Aligned with current implementation: file structure, service names (RecordService, CloudflareR2ImageStorageService), auth (Razor Pages), single Home nav, hosted services for schema, config keys, guest search limit |
| 1.2 | Feb 2026 | Batch import implementation: §5.3 DbContext factory and concurrency; §9.2 batch import flow (BatchImportService + RecordService.CreateBatchAsync, dedicated context); §7 CreateRecordRequest, BatchImportResultDto; file structure note for Record/ |
| 1.3 | Feb 2026 | Internationalization: §11 expanded—SharedResourceLocalizer (in-memory En/No), ICurrentLocaleService, URL locale segment; language switcher with flag images (flag-no.png, flag-en.png) in MainLayout; localized Admin (Records filters, Audit, Reports, Users, Batch import), Search, RecordEdit, ReporterRegister, Profile, Nav, modals; §13 Resources/, Services/Locale/, wwwroot/images flags and app.css locale-flag styles |

---

This document is the single source of truth for the Blazor application architecture and remains deployable on Render via Docker.
