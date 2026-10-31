# Project Documentation: Relational Persistence, Multi-Container Orchestration, and Telemetry

This documentation covers the architecture, setup, testing, and operations for the **Week 10 Assignment** (Web Programming 1, Mid-State Technical College).

---

## Architecture Overview

```
                      +---------------------------------------+
                      |       Podman / Docker Network         |
                      |            (app-network)              |
                      |                                       |
  [ Client / curl ] ----> [ ASP.NET Core API ] (Port 8080)   |
                      |          |                            |
                      |          | (Npgsql / Connection Pool) |
                      |          v                            |
                      |   [ PostgreSQL 17 ] (Port 5432)       |
                      |          |                            |
                      +----------|----------------------------+
                                 v
                       [ pgdata Named Volume ]
                       (Persistent Storage)
```

---

## Documentation Index

- [Part 1: Local Docker/Podman Parity & Volume Testing](./part1-local-parity.md)  
  *Multi-container orchestration, persistent volume verification, and container lifecycle testing.*
- [Parts 2 & 3: Structured Logging & Failure Diagnostics](./part2-part3-logging-telemetry.md)  
  *ILogger telemetry across 4 levels, credential sanitization rules, and intentional outage capture.*
- [Part 4: Schema Indexing & EXPLAIN ANALYZE](./part4-indexing-performance.md)  
  *Index creation on `environment` and capturing query execution plans and timing baselines.*
- [Part 5: Azure Disaster Recovery (Backup & Restore)](./part5-azure-disaster-recovery.md)  
  *Scoped logical export via `pg_dump`, restoration into isolated test DB via `pg_restore`, and MD5 checksum verification.*
- [Task Runner Cheat Sheet](./task-runner-cheat-sheet.md)  
  *Quick reference for all `task` commands (`task up-build`, `task post`, `task break-db`, etc.).*

---

## Key Files in Repository

- `compose.yaml`: Multi-container topology for local development (API + PostgreSQL 17 + `pgdata` volume).
- `init-schema.sql`: Database initialization script mounted into `/docker-entrypoint-initdb.d/01-init.sql`.
- `Taskfile.yml`: Task runner commands for quick workflows.
- `api/ServiceEndpoints.cs`: Minimal API endpoint handlers with structured `ILogger` telemetry.
- `api/Program.cs`: ASP.NET Core bootstrap and `NpgsqlDataSource` DI registration.
