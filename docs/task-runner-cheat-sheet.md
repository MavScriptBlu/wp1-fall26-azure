# Task Runner Cheat Sheet

All commands are configured in `Taskfile.yml` and run against Podman Compose.

---

## Command Reference

| Command | Description | What it does |
| :--- | :--- | :--- |
| `task up-build` | **Build & Start** | Runs `podman compose -f compose.yaml up -d --build` |
| `task up` | **Start** | Runs `podman compose -f compose.yaml up -d` (without rebuilding) |
| `task down` | **Stop & Tear Down** | Runs `podman compose -f compose.yaml down` (keeps `pgdata` volume) |
| `task ps` | **Container Status** | Runs `podman compose -f compose.yaml ps` |
| `task restart` | **Restart API** | Restarts the API container |
| `task logs` | **Follow Logs** | Runs `podman compose -f compose.yaml logs -f api` |
| `task logs-tail` | **Recent Logs** | Dumps the last 50 lines of API container logs |
| `task post` | **POST Sample Data** | Submits `AuthMicroservice` and `PaymentService` via curl |
| `task get` | **GET Records** | Queries all records from `http://localhost:8080/api/services` |
| `task break-db` | **Simulate Outage** | Changes DB password to `wrongpassword_999` and restarts API |
| `task fix-db` | **Restore Outage** | Restores DB password to `localdevpassword123` and restarts API |
| `task psql` | **Interactive PSQL** | Opens an interactive `psql` session inside the database container |
| `task explain` | **EXPLAIN ANALYZE** | Runs the index execution plan query in the database container |
| `task backup` | **Azure Backup** | Runs `pg_dump` against your Azure database (`s06`) |
| `task restore` | **Azure Restore** | Runs `pg_restore` into `cdev_f26_s06_restore_test` |
| `task verify` | **Verification Query** | Prints the SQL query for checksum and row count comparison |
| `task --list` | **List All Tasks** | Displays all available tasks with descriptions |

---

## Typical Part 1-4 Workflow

```powershell
# 1. Build and start containers
task up-build

# 2. Part 1: Submit records & verify volume persistence
task post
task get
task down
task up
task get

# 3. Part 2 & 3: Outage simulation & error capture
task break-db
task post
task logs-tail   # Copy 28P01 error for submission!
task fix-db
task get

# 4. Part 4: Explain analyze
task explain
```
