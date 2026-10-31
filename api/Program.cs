using Microsoft.Data.SqlClient;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Get connection string from environment (Azure) or build from config (local Docker)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Local dev fallback - Docker Compose PostgreSQL
    var host = builder.Configuration["Database:Host"] ?? "localhost";
    var port = int.TryParse(builder.Configuration["Database:Port"], out var p) ? p : 5432;
    var database = builder.Configuration["Database:Name"] ?? "student_db";
    var username = builder.Configuration["Database:Username"] ?? "postgres";
    var password = builder.Configuration["Database:Password"] ?? "localdevpassword123";
    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

// Detect SQL Server vs PostgreSQL based on connection string format
var useSqlServer = connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddSingleton(new DbConfig(connectionString, useSqlServer));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "api" }));
app.MapServiceEndpoints();
app.Run();

public record DbConfig(string ConnectionString, bool UseSqlServer);
