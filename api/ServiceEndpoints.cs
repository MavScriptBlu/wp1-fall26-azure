using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Npgsql;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/services
        app.MapGet("/api/services", async (DbConfig db, ILogger<Program> logger) =>
        {
            var sw = Stopwatch.StartNew();
            var dbHost = GetDbHost(db);

            logger.LogDebug("Renting connection from pool for host {DbHost}. Querying environment: {Environment}", dbHost, "all");

            try
            {
                await using var conn = CreateConnection(db);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, service_name, environment, status, created_at FROM service_records ORDER BY id;";
                await using var reader = await cmd.ExecuteReaderAsync();

                var records = new List<ServiceRecordResponse>();
                while (await reader.ReadAsync())
                {
                    records.Add(new ServiceRecordResponse(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetDateTime(4)
                    ));
                }

                sw.Stop();

                if (sw.ElapsedMilliseconds > 150)
                    logger.LogWarning("Slow query detected on {Endpoint}: execution took {ElapsedMs}ms", "/api/services", sw.ElapsedMilliseconds);

                logger.LogInformation("Successfully retrieved {Count} service records in {ElapsedMs}ms", records.Count, sw.ElapsedMilliseconds);
                return Results.Ok(records);
            }
            catch (Exception ex) when (ex is SqlException or NpgsqlException)
            {
                sw.Stop();
                logger.LogError(ex, "Database operation failed against {DbHost} for service {ServiceName}: {ErrorMessage}",
                    dbHost, "GetAllServices", ex.Message);
                return Results.Problem(
                    title: "Database Service Unavailable",
                    detail: "A persistence error occurred while processing the request.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        // POST /api/services
        app.MapPost("/api/services", async (ServiceRecordDto dto, DbConfig db, ILogger<Program> logger) =>
        {
            var sw = Stopwatch.StartNew();
            var dbHost = GetDbHost(db);

            logger.LogDebug("Renting connection from pool for host {DbHost}", dbHost);

            try
            {
                await using var conn = CreateConnection(db);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();

                if (db.UseSqlServer)
                {
                    cmd.CommandText = "INSERT INTO service_records (service_name, environment, status, created_at) " +
                        "OUTPUT INSERTED.id VALUES (@serviceName, @environment, @status, GETUTCDATE());";
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@serviceName"; p1.Value = dto.ServiceName; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@environment"; p2.Value = dto.Environment; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.ParameterName = "@status"; p3.Value = dto.Status; cmd.Parameters.Add(p3);
                }
                else
                {
                    cmd.CommandText = "INSERT INTO service_records (service_name, environment, status, created_at) " +
                        "VALUES ($1, $2, $3, NOW()) RETURNING id;";
                    var npgCmd = (NpgsqlCommand)cmd;
                    npgCmd.Parameters.AddWithValue(dto.ServiceName);
                    npgCmd.Parameters.AddWithValue(dto.Environment);
                    npgCmd.Parameters.AddWithValue(dto.Status);
                }

                var newId = (int?)await cmd.ExecuteScalarAsync();
                sw.Stop();

                if (sw.ElapsedMilliseconds > 150)
                    logger.LogWarning("Slow query detected on {Endpoint}: took {ElapsedMs}ms", "/api/services", sw.ElapsedMilliseconds);

                logger.LogInformation("Successfully created service record {ServiceName} with ID {ServiceId} in {ElapsedMs}ms",
                    dto.ServiceName, newId, sw.ElapsedMilliseconds);

                return Results.Created($"/api/services/{newId}", new { id = newId, dto.ServiceName });
            }
            catch (Exception ex) when (ex is SqlException or NpgsqlException)
            {
                sw.Stop();
                logger.LogError(ex, "Database operation failed against {DbHost} for service {ServiceName}: {ErrorMessage}",
                    dbHost, dto.ServiceName, ex.Message);
                return Results.Problem(
                    title: "Database Service Unavailable",
                    detail: "A persistence error occurred while processing the request.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });
    }

    private static DbConnection CreateConnection(DbConfig db) =>
        db.UseSqlServer ? new SqlConnection(db.ConnectionString) : new NpgsqlConnection(db.ConnectionString);

    private static string GetDbHost(DbConfig db)
    {
        try
        {
            if (db.UseSqlServer)
                return new SqlConnectionStringBuilder(db.ConnectionString).DataSource ?? "db";
            return new NpgsqlConnectionStringBuilder(db.ConnectionString).Host ?? "db";
        }
        catch { return "db"; }
    }
}

public record ServiceRecordDto(string ServiceName, string Environment, string Status);
public record ServiceRecordResponse(int Id, string ServiceName, string Environment, string Status, DateTime CreatedAt);
