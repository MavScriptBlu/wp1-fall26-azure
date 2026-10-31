# Part 1: Multi-Container Local Parity & Volume Testing

## Objective

Orchestrate the ASP.NET Core API container alongside a local PostgreSQL 17 container using Docker/Podman Compose, and prove that data survives container destruction using the `pgdata` named volume.

---

## 1. Multi-Container Topology (`compose.yaml`)

The environment defines:

- **`api` service**: ASP.NET Core 10 Web API built from `./api/Dockerfile`, listening on port `8080`.
- **`db` service**: `postgres:17-alpine` listening on port `5432`, initialized with `./init-schema.sql`.
- **`pgdata` volume**: Persistent volume mounted to `/var/lib/postgresql/data`.
- **`app-network`**: Isolated bridge network for inter-container DNS communication (`Host=db`).
- **Healthcheck**: API container waits for `db` to pass `pg_isready` before starting.

---

## 2. Verification Steps (Proving Persistence)

### Step 1: Build and start the containers

```powershell
task up-build
# Or: podman compose -f compose.yaml up -d --build
```

### Step 2: Submit two test service records

```powershell
task post
```

Or manually using `curl`:

```bash
curl -X POST http://localhost:8080/api/services \
  -H "Content-Type: application/json" \
  -d '{"serviceName": "AuthMicroservice", "environment": "production", "status": "active"}'

curl -X POST http://localhost:8080/api/services \
  -H "Content-Type: application/json" \
  -d '{"serviceName": "PaymentService", "environment": "production", "status": "active"}'
```

### Step 3: Tear down containers completely

```powershell
task down
# Or: podman compose -f compose.yaml down
```

> **Caution:** Do NOT pass the `-v` flag. Passing `-v` deletes persistent volumes!

### Step 4: Relaunch containers without rebuilding

```powershell
task up
# Or: podman compose -f compose.yaml up -d
```

### Step 5: Verify records survived

```powershell
task get
# Or: curl http://localhost:8080/api/services
```

Expected output:

```json
[
  {
    "id": 1,
    "serviceName": "AuthMicroservice",
    "environment": "production",
    "status": "active",
    "createdAt": "2026-10-31T12:00:00Z"
  },
  {
    "id": 2,
    "serviceName": "PaymentService",
    "environment": "production",
    "status": "active",
    "createdAt": "2026-10-31T12:00:00Z"
  }
]
```

This confirms that data was stored inside `pgdata` on disk and mounted cleanly into the new container instance.
