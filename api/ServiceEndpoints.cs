using System.Diagnostics;
using Npgsql;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/services
        app.MapGet("/api/services", async (NpgsqlDataSource dataSource, ILogger<Program> logger) =>
        {
            // Start timer to measure query duration, and extract db host for safe logging
            var sw = Stopwatch.StartNew();
            var dbHost = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Host ?? "db";

            // Log connection checkout from pool (Debug level)
            logger.LogDebug("Renting connection from pool for host {DbHost}. Querying environment: {Environment}", dbHost, "all");

            try
            {
                // Query all records ordered by ID
                await using var cmd = dataSource.CreateCommand(
                    "SELECT id, service_name, environment, status, created_at FROM service_records ORDER BY id;");
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

                // Log a warning if the query was slow (>150ms threshold)
                if (sw.ElapsedMilliseconds > 150)
                {
                    logger.LogWarning("Slow query detected on {Endpoint}: execution took {ElapsedMs}ms", "/api/services", sw.ElapsedMilliseconds);
                }

                // Log successful query completion (Information level)
                logger.LogInformation("Successfully retrieved {Count} service records in {ElapsedMs}ms", records.Count, sw.ElapsedMilliseconds);
                return Results.Ok(records);
            }
            catch (NpgsqlException ex)
            {
                sw.Stop();

                // Log failure with full exception details and return 503 without leaking credentials
                logger.LogError(ex, "Database operation failed against {DbHost} for service {ServiceName}: {ErrorMessage}",
                    dbHost, "GetAllServices", ex.Message);

                return Results.Problem(
                    title: "Database Service Unavailable",
                    detail: "A persistence error occurred while processing the request.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        // POST /api/services
        app.MapPost("/api/services", async (
            ServiceRecordDto dto,
            NpgsqlDataSource dataSource,
            ILogger<Program> logger) =>
        {
            // Start timer to measure insertion latency, and extract db host for safe logging
            var sw = Stopwatch.StartNew();
            var dbHost = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Host ?? "db";

            // Log connection checkout from pool (Debug level)
            logger.LogDebug("Renting connection from pool for host {DbHost}", dbHost);

            try
            {
                // Parameterized insert to prevent SQL injection; RETURNING id fetches the new sequence key
                await using var cmd = dataSource.CreateCommand(
                    "INSERT INTO service_records (service_name, environment, status, created_at) " +
                    "VALUES ($1, $2, $3, NOW()) RETURNING id;");

                cmd.Parameters.AddWithValue(dto.ServiceName);
                cmd.Parameters.AddWithValue(dto.Environment);
                cmd.Parameters.AddWithValue(dto.Status);

                var newId = (int?)await cmd.ExecuteScalarAsync();
                sw.Stop();

                // Log a warning if the insert exceeded the 150ms performance threshold
                if (sw.ElapsedMilliseconds > 150)
                {
                    logger.LogWarning("Slow query detected on {Endpoint}: took {ElapsedMs}ms", "/api/services", sw.ElapsedMilliseconds);
                }

                // Log successful creation with generated ID (Information level)
                logger.LogInformation("Successfully created service record {ServiceName} with ID {ServiceId} in {ElapsedMs}ms",
                    dto.ServiceName, newId, sw.ElapsedMilliseconds);

                return Results.Created($"/api/services/{newId}", new { id = newId, dto.ServiceName });
            }
            catch (NpgsqlException ex)
            {
                sw.Stop();

                // Log database exception with PostgreSQL error code (e.g. 28P01) and return 503
                logger.LogError(ex, "Database operation failed against {DbHost} for service {ServiceName}: {ErrorMessage}",
                    dbHost, dto.ServiceName, ex.Message);

                return Results.Problem(
                    title: "Database Service Unavailable",
                    detail: "A persistence error occurred while processing the request.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });
    }
}

public record ServiceRecordDto(string ServiceName, string Environment, string Status);
public record ServiceRecordResponse(int Id, string ServiceName, string Environment, string Status, DateTime CreatedAt);
