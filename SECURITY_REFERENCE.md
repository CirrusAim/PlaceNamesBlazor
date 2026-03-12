# Security Reference — PlaceNames Blazor

**Purpose:** Reference for improving and hardening the Blazor app’s security. Use when configuring secrets, deployment, or adding safeguards.  
**Context:** See [BLAZOR_SYSTEM_ARCHITECTURE.md](./BLAZOR_SYSTEM_ARCHITECTURE.md) §12 Security for current posture. Implemented items are listed in §4 below; areas to improve are in §1–§3.

---

## 1. Secrets and configuration

| Area | Recommendation |
|------|----------------|
| **Secrets in appsettings** | `ConnectionStrings`, `ImageStorage:Cloudflare:SecretAccessKey` (and AccessKeyId) must not be in committed JSON. Use **environment variables**, **User Secrets** (dev), or a secrets store (e.g. Render env, Azure Key Vault) in production. Add `env.example` / docs for required vars. |
| **AllowedHosts: "\*"** | Restrict to actual host(s) in production (e.g. `AllowedHosts: "yourdomain.com;www.yourdomain.com"`) to reduce host-header abuse. |

---

## 2. Rate limiting

| Area | Recommendation |
|------|----------------|
| **Only guest search limited** | No global or per-route rate limiting for login, register, or other sensitive endpoints. Add **AspNetCoreRateLimit** (or equivalent) for login/register and high-value endpoints to limit abuse and DoS. |

---

## 3. Data protection and deployment

| Area | Recommendation |
|------|----------------|
| **Data Protection keys** | No explicit `AddDataProtection()` with key persistence. For multi-instance or restart-safe cookie/token encryption, configure key storage (e.g. database or shared store) and set `ApplicationName` consistently. |

---

## 4. Already in good shape (no change unless scaling)

- **HTTPS / HSTS** — `UseHttpsRedirection()` and HSTS in non-dev.
- **Antiforgery** — `UseAntiforgery()`; Blazor uses tokens for state-changing operations.
- **SQL injection** — EF Core parameterized queries only.
- **XSS** — Blazor default encoding; no `MarkupString` with user input found.
- **Passwords** — BCrypt (work factor 10); no plaintext storage. **Password policy** enforced: min 8 characters, at least one uppercase, one lowercase, one digit (registration, profile change, and admin seed).
- **Cookie options** — Auth cookie `SecurePolicy` = Always in production (SameAsRequest in dev); `SameSite = Strict`.
- **Login lockout** — Email-based lockout after N failed attempts (configurable: `Lockout:MaxFailedAttempts`, `Lockout:LockoutDurationMinutes`); state in IMemoryCache; lockout message shown on login page.
- **AuthZ** — Policies `Admin`, `AdminOrSuperuser`; `[Authorize]` on admin routes.
- **Audit** — Admin/superuser actions logged with actor, type, target, IP, timestamp. **User-Agent** stored in `audit_logs.user_agent`; **append-only** enforced via PostgreSQL triggers (no UPDATE/DELETE/TRUNCATE). Callers must not put passwords, tokens, or full request bodies in `DetailsJson`.
- **File upload** — Image uploads validated by **magic bytes** (JPEG, PNG, GIF, WebP, BMP) via `ImageMagicBytesValidator`; **Kestrel** `MaxRequestBodySize` = 10 MB; stored paths built from controlled segments only; **path traversal** rejected in local storage (GetDisplayUrl, DeleteAsync).

---

*Address items in §1–§3 in priority order (secrets and host configuration first, then rate limiting, then data protection).*
