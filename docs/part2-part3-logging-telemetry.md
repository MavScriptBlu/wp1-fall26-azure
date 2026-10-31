# Parts 2 & 3: Structured Database Logging & Failure Diagnostics

## Objective

Instrument ASP.NET Core endpoints with structured `ILogger<Program>` telemetry across four standard severity levels, enforce strict credential sanitization, and capture an authentic database outage excerpt for submission.

---

## 1. Structured Logging Severity Levels

In `api/ServiceEndpoints.cs`, logging is implemented across all four required levels:

| Severity | Operational Purpose | Implemented Code Pattern |
| :--- | :--- | :--- |
| **Debug** | Internal diagnostics, connection checkout from pool. | `logger.LogDebug("Renting connection from pool for host {DbHost}. Querying environment: {Environment}", dbHost, targetEnv);` |
| **Information** | Business milestone (successful creation/retrieval). | `logger.LogInformation("Successfully created service record {ServiceName} with ID {ServiceId} in {ElapsedMs}ms", dto.ServiceName, newId, sw.ElapsedMilliseconds);` |
| **Warning** | Performance degradation (queries > 150ms). | `logger.LogWarning("Slow query detected on {Endpoint}: took {ElapsedMs}ms", "/api/services", sw.ElapsedMilliseconds);` |
| **Error** | Unrecoverable failures & DB connection exceptions. | `logger.LogError(ex, "Database operation failed against {DbHost} for service {ServiceName}: {ErrorMessage}", dbHost, dto.ServiceName, ex.Message);` |

### Strict Sanitization Rules

- **No string interpolation (`$"..."`)**: All placeholders use named curly-bracket arguments (`{DbHost}`, `{ServiceName}`).
- **No secrets in logs**: Connection strings and passwords are never passed to the logger.

---

## 2. Intentional Outage & Telemetry Capture (Part 3)

### Step 1: Inject authentication failure

Run the task command:

```powershell
task break-db
```

*(This replaces `localdevpassword123` with `wrongpassword_999` in `api/appsettings.json` and restarts the API container).*

### Step 2: Trigger the outage

Send a POST request to the API:

```powershell
task post
```

Response will return `503 Service Unavailable`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",
  "title": "Database Service Unavailable",
  "status": 503,
  "detail": "A persistence error occurred while processing the request."
}
```

### Step 3: Extract the error telemetry excerpt

View the logs:

```powershell
task logs-tail
```

Sample captured telemetry output showing PostgreSQL error code `28P01`:

```text
fail: Program[0]
      Database operation failed against db for service AuthMicroservice: 28P01: password authentication failed for user "postgres"
      Npgsql.PostgresException (0x80004005): 28P01: password authentication failed for user "postgres"
         at Npgsql.Internal.NpgsqlConnector.Authenticate(String username, NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
         at Npgsql.Internal.NpgsqlConnector.Open(NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
         at Npgsql.PoolingDataSource.OpenNewConnector(NpgsqlConnection conn, NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
         ...
```

*(Save this excerpt for your Blackboard submission).*

### Step 4: Revert to valid credentials

```powershell
task fix-db
```

Verify the API is working again:

```powershell
task get
```
