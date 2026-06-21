# Docker Deployment Guide — LocalAIFactory

The container-based deployment path, using the committed compose files under `deploy/`.

> **Honest status — read first.** **Docker is NOT installed on the build host used for this release.**
> The `deploy/Dockerfile`, `deploy/docker-compose.cpu.yml`, and `deploy/docker-compose.gpu.yml` files
> **exist as references and were NOT executed here.** This guide gives the exact commands and
> prerequisites, but the container path is **unverified on this host** — treat it as a path to
> validate in your own environment, not a proven one. This matches
> [`Known-Limitations.md`](Known-Limitations.md) §5 and the IIS guide's Docker note.

---

## 1. What exists in the repo

| File | Purpose |
|---|---|
| `deploy/Dockerfile` | Builds the LocalAIFactory Web image. |
| `deploy/docker-compose.cpu.yml` | CPU-only compose (no GPU passthrough). |
| `deploy/docker-compose.gpu.yml` | GPU compose (NVIDIA runtime passthrough for optional Ollama). |

These are committed as a reference container path. They are **not** invoked by any installer in this
release and were not run on the build host.

---

## 2. Prerequisites (on a host where Docker IS installed)

- **Docker Desktop** (Windows/macOS) **or Docker Engine** (Linux).
- On Windows, **WSL2** is required for Docker Desktop's Linux containers (the
  Docker-Desktop / WSL2 prerequisite). Enable WSL2 and the Docker Desktop WSL2 backend before
  building.
- For the **GPU** compose file: the **NVIDIA Container Toolkit** and a supported NVIDIA driver on the
  host, plus `--gpus` support in your Docker runtime. The GPU is **optional** — it only accelerates
  optional Ollama inference; the system of record never depends on it.
- A reachable **MSSQL** instance. MSSQL remains the source of truth. You can either run SQL Server in
  a container or point the app at an external MSSQL host via the connection string.

---

## 3. Exact commands (to run on a Docker-enabled host)

> These are the intended commands. They were **not executed on the build host** (no Docker here).

### Build the image

```bash
docker build -f deploy/Dockerfile -t localaifactory-web:latest .
```

### CPU-only compose

```bash
docker compose -f deploy/docker-compose.cpu.yml up --build
```

### GPU compose (optional Ollama acceleration)

```bash
docker compose -f deploy/docker-compose.gpu.yml up --build
```

### Tear down

```bash
docker compose -f deploy/docker-compose.cpu.yml down      # or the gpu file
```

---

## 4. Configuration in containers

- **Connection string** — supply `ConnectionStrings__DefaultConnection` as an environment variable to
  the Web container; never bake credentials into the image or compose file. For an external MSSQL,
  prefer `Encrypt=True;TrustServerCertificate=False` with a trusted certificate
  ([`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md) §5).
- **Optional services** — set `Ollama.Enabled` / `Qdrant.Enabled` only if you wire those services
  into the compose network. With them off (the default), the app runs MSSQL-only.
- **Data Protection keys** — mount a **persistent volume** for `./keys` so encrypted secrets and auth
  cookies survive container restarts/recreates. If the keys volume is lost, encrypted API keys cannot
  be decrypted and users are logged out (same rule as the IIS guide §6).
- **Knowledge packs** — on first start the app auto-seeds all 4 packs (438 items) idempotently,
  exactly as in the non-container modes.

---

## 5. First-run and verification (inside the container path)

The app migrates, seeds, and installs the packs on startup. To verify, run the read-only checks
against the database the container uses:

```bash
# From a host with sqlcmd / pwsh and access to the same MSSQL instance:
pwsh database/verify-knowledge-base.ps1 -ServerInstance "<sql-host-or-container>" -Database "LocalAIFactory"
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:8080"
```

`post-install-healthcheck.ps1` GETs the core pages and asserts 200/302; it changes nothing.

---

## 6. Documented blocker

- **Docker is not installed on the build host**, so none of the commands above were executed and no
  image was built or run here. The container path therefore has **no live evidence** in this release.
- **Close with:** building the image and running the compose stack on a Docker-enabled host, then
  executing the same smoke + knowledge-base verification used in the other modes, with the output
  captured in logs. Until then, the supported, evidenced paths are LocalDB, and the
  documented-but-unexecuted SQL Express / IIS / full-SQL paths in
  [`Deployment-Guide.md`](Deployment-Guide.md).

---

## 7. Where to go instead (verified-or-documented alternatives)

- Fastest evidenced path → [`FINAL_LOCAL_DEPLOYMENT_GUIDE.md`](FINAL_LOCAL_DEPLOYMENT_GUIDE.md) (LocalDB).
- Pilot → [`SQL-Express-Pilot-Deployment.md`](SQL-Express-Pilot-Deployment.md).
- Production-style → [`Full-SQL-Server-Deployment.md`](Full-SQL-Server-Deployment.md).
- Behind IIS → [`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md).
