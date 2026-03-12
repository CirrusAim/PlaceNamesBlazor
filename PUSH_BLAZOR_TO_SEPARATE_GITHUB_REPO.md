# Push Blazor app to a separate GitHub repo

Use a **separate repo** so that on Render you can link the repo and use the **default root** (no Root Directory setting). The Blazor app is then the whole repo.

---

## Recommended: Copy PlaceNamesBlazor contents as the new repo root

This gives you a repo whose root is exactly the Blazor app (Dockerfile, `src/`, docs at top level). Render will build with **Root Directory** left blank.

### 1. Create the new repo on GitHub

- **New repository**, e.g. `PlaceNamesBlazor` or `norwegian-place-names-blazor`.
- Do **not** add a README, .gitignore, or license (we’ll add them from the copy).

### 2. Copy the contents of `PlaceNamesBlazor` into the new repo

From your machine:

1. **Clone the new (empty) repo** into a new folder:
   ```bash
   git clone https://github.com/YOUR_USER/YOUR_BLAZOR_REPO.git blazor-repo
   cd blazor-repo
   ```

2. **Copy everything from `Project_DB/PlaceNamesBlazor/`** into `blazor-repo/` **except**:
   - `bin/` (anywhere)
   - `obj/` (anywhere)
   - `appsettings.json` and `appsettings.Development.json` (real config with secrets)

   So the **root** of `blazor-repo` should contain:
   - `Dockerfile`
   - `docker-entrypoint.sh`
   - `src/PlaceNamesBlazor/` (the .csproj and app; **exclude** `src/PlaceNamesBlazor/bin/` and `src/PlaceNamesBlazor/obj/`)
   - `create_sample_excel.py`
   - `env.example`
   - `BLAZOR_SYSTEM_ARCHITECTURE.md`, `LOCAL_SETUP_AND_TESTING.md`, `RENDER_DEPLOY.md`, `SECURITY_REFERENCE.md`, `README.md`, `PUSH_BLAZOR_TO_SEPARATE_GITHUB_REPO.md`
   - `appsettings.Example.json` and `appsettings.Development.Example.json` (in `src/PlaceNamesBlazor/`)
   - `scripts/` (if present)
   - `.gitignore` (this folder’s .gitignore so the new repo ignores `bin/`, `obj/`, `appsettings.json`, etc.)

   **PowerShell (from Project_DB root):**
   ```powershell
   $dest = "C:\path\to\blazor-repo"
   robocopy "PlaceNamesBlazor" $dest /E /XD bin obj .git /XF appsettings.json appsettings.Development.json
   ```
   Then delete any `bin` or `obj` folders that still ended up under `src\PlaceNamesBlazor` in the destination, and remove real appsettings if copied.

   **Manual:** Copy the whole `PlaceNamesBlazor` folder into the clone, then delete (from the clone) `PlaceNamesBlazor\src\PlaceNamesBlazor\bin`, `PlaceNamesBlazor\src\PlaceNamesBlazor\obj`, `PlaceNamesBlazor\src\PlaceNamesBlazor\appsettings.json`, and `PlaceNamesBlazor\src\PlaceNamesBlazor\appsettings.Development.json`. Then move the **contents** of `PlaceNamesBlazor` (Dockerfile, src, *.md, etc.) up into the repo root so the clone root has Dockerfile and src/, not a single PlaceNamesBlazor folder.

3. **Commit and push:**
   ```bash
   git add .
   git commit -m "Initial Blazor app (PlaceNamesBlazor)"
   git push -u origin main
   ```

### 3. Deploy on Render

- **New Web Service** → connect **this** repo (the Blazor-only one).
- Leave **Root Directory** blank (repo root = Blazor app).
- **Runtime:** Docker.
- Add env vars (e.g. `DATABASE_URL` after creating Postgres) and deploy.

---

## Optional: Use git subtree (single top-level folder)

If you push with subtree, the new repo will have **one** top-level folder `PlaceNamesBlazor` (not its contents at root). You would then set **Root Directory** to `PlaceNamesBlazor` on Render.

From **Project_DB** root:

```bash
git remote add github-blazor https://github.com/YOUR_USER/YOUR_BLAZOR_REPO.git
git subtree push --prefix=PlaceNamesBlazor github-blazor main
```

The new repo will have a root that contains only the folder `PlaceNamesBlazor`. On Render, set **Root Directory** to `PlaceNamesBlazor`.

---

## Summary

| Approach              | New repo root              | Render Root Directory |
|-----------------------|----------------------------|-------------------------|
| Copy contents (above) | Dockerfile, src/, docs     | Leave blank             |
| git subtree push      | One folder `PlaceNamesBlazor` | Set to `PlaceNamesBlazor` |

Recommendation: use the **copy contents** approach so the Blazor app is the repo root and you don’t need to set Root Directory on Render.

---

## Checklist: what the new repo root must contain

After copying, the **root** of the new repo should have:

| Item | Purpose |
|------|--------|
| `Dockerfile` | Render Docker build |
| `docker-entrypoint.sh` | Sets `ASPNETCORE_URLS` from `PORT` |
| `src/PlaceNamesBlazor/` | Full app (`.csproj`, `Program.cs`, `Components/`, `Services/`, `Data/`, `Contracts/`, `wwwroot/`, etc.) |
| `src/PlaceNamesBlazor/appsettings.Example.json` | Placeholder config (no secrets) |
| `src/PlaceNamesBlazor/appsettings.Development.Example.json` | Placeholder dev config |
| `.gitignore` | Excludes `bin/`, `obj/`, `appsettings.json`, `appsettings.Development.json` |
| `create_sample_excel.py` | Sample Excel for batch import |
| `env.example` | Env var reference |
| `README.md`, `BLAZOR_SYSTEM_ARCHITECTURE.md`, `LOCAL_SETUP_AND_TESTING.md`, `RENDER_DEPLOY.md`, `SECURITY_REFERENCE.md` | Docs |

**Do not include:** `bin/`, `obj/`, `appsettings.json`, `appsettings.Development.json` (real secrets).
