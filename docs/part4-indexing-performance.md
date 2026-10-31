# Part 4: Query Performance Baseline & Schema Indexing

## Objective

Verify schema indexing on the `environment` column and record the query execution plan and timing baselines using `EXPLAIN ANALYZE`.

---

## 1. Index Definition

The schema index is defined in `init-schema.sql`:

```sql
CREATE INDEX IF NOT EXISTS idx_services_env ON service_records(environment);
```

---

## 2. Running `EXPLAIN ANALYZE`

You can run the query using the task command:

```powershell
task explain
```

Or by entering `psql` directly:

```powershell
task psql
# Then inside psql:
EXPLAIN ANALYZE SELECT * FROM service_records WHERE environment = 'production';
```

---

## 3. Sample Output & Key Metrics to Record

Sample execution output:

```text
                                              QUERY PLAN
------------------------------------------------------------------------------------------------------
 Seq Scan on service_records  (cost=0.00..1.02 rows=1 width=216) (actual time=0.012..0.013 rows=2 loops=1)
   Filter: ((environment)::text = 'production'::text)
 Planning Time: 0.128 ms
 Execution Time: 0.031 ms
(4 rows)
```

### Metrics for Submission Deliverables

1. **Scan Type**: `Seq Scan` (or `Bitmap Index Scan on idx_services_env` when row count is large).  
   *Note:* On small datasets (< 100 rows), PostgreSQL's cost-based query planner chooses `Seq Scan` because sequentially reading a single 8KB disk page is faster than traversing a B-Tree index.
2. **Planning Time**: Time taken by PostgreSQL optimizer to plan execution (e.g. `0.128 ms`).
3. **Execution Time**: Actual CPU and disk runtime to retrieve matching rows (e.g. `0.031 ms`).
