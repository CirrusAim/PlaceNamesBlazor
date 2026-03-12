# Deploy Blazor app on Render (Docker)

Deploy the app as a **Web Service** using Docker. You can deploy first and add the PostgreSQL database and environment variables afterward.

---

## 1. Prepare the repo

- **Option A (Blazor-only repo, recommended):** Push the Blazor app to its own GitHub repo so the **repo root** is the Blazor app (Dockerfile, `src/`, docs at top level). See [PUSH_BLAZOR_TO_SEPARATE_GITHUB_REPO.md](./PUSH_BLAZOR_TO_SEPARATE_GITHUB_REPO.md). On Render you connect that repo and leave **Root Directory** blank.
- **Option B (Project_DB repo):** Use the full `Project_DB` repo. Set **Root Directory** to **`PlaceNamesBlazor`** so the build runs from that folder.

Below, **Option A** uses Root Directory blank; **Option B** uses Root Directory `PlaceNamesBlazor`.

---

## 2. Create the Web Service on Render

1. **Dashboard** → **New** → **Web Service**.
2. **Connect** your GitHub account and select the repository (e.g. `Project_DB`).
3. **Settings:**
   - **Name:** e.g. `placenames-blazor`
   - **Region:** choose one
   - **Branch:** e.g. `main` or `master`
   - **Root Directory:** leave **blank** if you use the Blazor-only repo (repo root = Blazor app). If you use the full Project_DB repo, set to **`PlaceNamesBlazor`**.
   - **Runtime:** **Docker**.
   - **Dockerfile path:** leave default **`Dockerfile`** (at repo root for Blazor-only repo, or at `PlaceNamesBlazor/Dockerfile` when Root Directory is set).
   - **Instance type:** Free or paid.

4. **Environment variables (optional for first deploy):**  
   You can leave env vars empty for the first deploy. The app will start but will fail when it tries to use the database until you add Postgres and `DATABASE_URL`. To avoid startup errors you can set a dummy for now, or add the real ones after linking the DB:
   - **`PORT`** — Render sets this automatically; no need to add it.
   - **`DATABASE_URL`** — Add after you create a PostgreSQL instance and link it to this service (Render will suggest adding the internal URL).
   - **`ADMIN_EMAIL`** / **`ADMIN_PASSWORD`** — For creating the first admin user (optional; add when you have the DB).
   - **Cloudflare R2** — Only if you use cloud image storage: `ImageStorage__Provider=Cloudflare`, `ImageStorage__Cloudflare__AccountId`, etc.

5. Click **Create Web Service**. Render will build the Docker image (using the Dockerfile and `docker-entrypoint.sh`) and start the container.

---

## 3. After the first deploy

1. **Create a PostgreSQL** database on Render (Dashboard → New → PostgreSQL) if you have not already.
2. **Link the database** to the Web Service (in the service’s **Environment** tab, use “Add from Render” or paste the **Internal Database URL** into `DATABASE_URL`).
3. Add **`ADMIN_EMAIL`** and **`ADMIN_PASSWORD`** (and any Cloudflare R2 vars if needed).
4. **Redeploy** the service so the app starts with the correct connection string and creates the schema (hosted services run on startup).

---

## 4. Dockerfile summary

- **Build context:** `PlaceNamesBlazor` (Root Directory on Render).
- **Build:** .NET 10 SDK; restore and publish `src/PlaceNamesBlazor/PlaceNamesBlazor.csproj`.
- **Run:** .NET 10 ASP.NET runtime; `docker-entrypoint.sh` sets `ASPNETCORE_URLS=http://0.0.0.0:$PORT` and runs `dotnet PlaceNamesBlazor.dll` so the app listens on the port Render provides.

If the build fails, check that the repo root (or Root Directory) contains `Dockerfile`, `docker-entrypoint.sh`, and `src/PlaceNamesBlazor/` (with the .csproj and app code).
