using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register NpgsqlDataSource
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var host = builder.Configuration["Database:Host"] ?? "localhost";
    var port = int.TryParse(builder.Configuration["Database:Port"], out var p) ? p : 5432;
    var database = builder.Configuration["Database:Name"] ?? "student_db";
    var username = builder.Configuration["Database:Username"] ?? "postgres";
    var password = builder.Configuration["Database:Password"] ?? "localdevpassword123";

    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "api" }));

// Map modular endpoint handlers
app.MapServiceEndpoints();

app.Run();
