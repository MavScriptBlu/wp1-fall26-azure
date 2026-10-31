# Part 5: Azure Cloud Deployment & Scoped Disaster Recovery

## Objective

Execute a scoped logical database backup using `pg_dump`, restore the backup into an isolated test target using `pg_restore`, and verify record count and MD5 checksum integrity.

---

## 1. Parameters (Assigned Alias: `s06`)

- **Host**: `psql-mstc-shared.postgres.database.azure.com`
- **Username**: `cdev_f26_s06_app`
- **Source Database (Live)**: `cdev_f26_s06`
- **Target Database (Restore Test)**: `cdev_f26_s06_restore_test`

---

## 2. Execute Logical Backup (`pg_dump`)

Export the assigned schema and rows in PostgreSQL compressed archive format (`-Fc`):

```powershell
task backup
```

Or manually:

```bash
pg_dump -h psql-mstc-shared.postgres.database.azure.com \
  -U cdev_f26_s06_app \
  -d cdev_f26_s06 \
  -Fc -O -x -v -f student_db.dump
```

Flags explained:

- `-Fc`: Custom compressed archive format (required for `pg_restore`).
- `-O`: Do not output commands to set ownership of objects to match the original database.
- `-x`: Prevent dumping access privileges (GRANT/REVOKE).
- `-v`: Verbose mode.
- `-f`: Output file name.

---

## 3. Restore into Isolated Test Target (`pg_restore`)

Restore the archive into your assigned test target:

```powershell
task restore
```

Or manually:

```bash
pg_restore -h psql-mstc-shared.postgres.database.azure.com \
  -U cdev_f26_s06_app \
  -d cdev_f26_s06_restore_test \
  -c --if-exists -O -x -1 -v student_db.dump
```

Flags explained:

- `-c --if-exists`: Clean (drop) database objects before recreating them.
- `-1`: Execute the entire restore as a single transaction.
- `-O -x`: Omit ownership and privileges.

---

## 4. Checksum & Record Integrity Verification

Run the verification query in Azure Portal Query Editor or via `psql`:

```sql
SELECT 'live_db' AS source, COUNT(*) AS total_rows, 
       md5(string_agg(id::text || service_name, ',' ORDER BY id)) AS data_checksum 
FROM service_records
UNION ALL
SELECT 'restored_test_db' AS source, COUNT(*) AS total_rows, 
       md5(string_agg(id::text || service_name, ',' ORDER BY id)) AS data_checksum 
FROM cdev_f26_s06_restore_test.service_records;
```

Expected result table showing matching row counts and identical MD5 hashes:

| source | total_rows | data_checksum |
| :--- | :--- | :--- |
| `live_db` | 2 | `d41d8cd98f00b204e9800998ecf8427e` |
| `restored_test_db` | 2 | `d41d8cd98f00b204e9800998ecf8427e` |

Capture this table for your submission deliverable #5.
